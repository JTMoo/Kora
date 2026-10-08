using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using StockManagement.Feedback.Core.Contracts;
using StockManagement.Feedback.Relay;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RelayOptions>(builder.Configuration.GetSection(RelayOptions.SectionName));
builder.Services.AddHttpClient(nameof(GitHubIssueClient));
builder.Services.AddSingleton<GitHubIssueClient>();
builder.Services.AddHealthChecks();

// Only honor X-Forwarded-For from configured reverse proxies (RelayOptions.TrustedProxies); unconfigured (default) keeps RemoteIpAddress, so a client can't spoof its own rate-limit bucket.
var trustedProxies = (builder.Configuration.GetSection(RelayOptions.SectionName).Get<RelayOptions>()?.TrustedProxies ?? []).Select(IPAddress.Parse).ToList();
if (trustedProxies.Count > 0)
{
	builder.Services.Configure<ForwardedHeadersOptions>(options =>
	{
		options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
		foreach (var proxy in trustedProxies) options.KnownProxies.Add(proxy);
	});
}

var app = builder.Build();

if (trustedProxies.Count > 0) app.UseForwardedHeaders();

app.MapHealthChecks("/health");

// Fixed-window per-IP cap; cheap abuse guard, not meant to survive a restart.
var requestsByIp = new ConcurrentDictionary<string, (int Count, DateTimeOffset WindowStart)>();

app.MapPost("/reports", async (UserReport report, HttpContext context, GitHubIssueClient issueClient, IOptions<RelayOptions> options, CancellationToken cancellationToken) =>
{
	var relayOptions = options.Value;

	if (!IsSecretValid(context.Request.Headers["X-Relay-Secret"].ToString(), relayOptions.SharedSecret))
		return Results.Unauthorized();

	// Trusted only when ForwardedHeaders middleware above already rewrote it from a known proxy; otherwise this is the real connection.
	var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
	var now = DateTimeOffset.UtcNow;
	var window = requestsByIp.AddOrUpdate(ip,
		_ => (1, now),
		(_, current) => now - current.WindowStart > TimeSpan.FromMinutes(1) ? (1, now) : (current.Count + 1, current.WindowStart));

	if (window.Count > relayOptions.MaxRequestsPerMinutePerIp)
		return Results.StatusCode(StatusCodes.Status429TooManyRequests);

	var succeeded = await issueClient.CreateIssueAsync(report, cancellationToken);
	return succeeded ? Results.Ok() : Results.StatusCode(StatusCodes.Status502BadGateway);
});

await app.RunAsync();

/// <summary>Constant-time compare so timing can't be used to narrow down <see cref="RelayOptions.SharedSecret"/> byte-by-byte.</summary>
static bool IsSecretValid(string provided, string expected)
{
	var providedBytes = Encoding.UTF8.GetBytes(provided);
	var expectedBytes = Encoding.UTF8.GetBytes(expected);
	return providedBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
}
