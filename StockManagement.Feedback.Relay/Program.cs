using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using StockManagement.Feedback.Core.Contracts;
using StockManagement.Feedback.Relay;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RelayOptions>(builder.Configuration.GetSection(RelayOptions.SectionName));
builder.Services.AddHttpClient(nameof(GitHubIssueClient));
builder.Services.AddSingleton<GitHubIssueClient>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

// Fixed-window per-IP cap; cheap abuse guard, not meant to survive a restart.
var requestsByIp = new ConcurrentDictionary<string, (int Count, DateTimeOffset WindowStart)>();

app.MapPost("/reports", async (UserReport report, HttpContext context, GitHubIssueClient issueClient, IOptions<RelayOptions> options, CancellationToken cancellationToken) =>
{
	var relayOptions = options.Value;

	if (context.Request.Headers["X-Relay-Secret"] != relayOptions.SharedSecret)
		return Results.Unauthorized();

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
