using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using StockManagement.Feedback.Core.Contracts;

namespace StockManagement.Feedback.Relay;


/// <summary>Creates a labelled issue in the Kora repo for a submitted <see cref="UserReport"/> (ADR-0042).</summary>
public sealed class GitHubIssueClient(IHttpClientFactory httpClientFactory, IOptions<RelayOptions> options, ILogger<GitHubIssueClient> logger)
{
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
	private readonly RelayOptions _options = options.Value;
	private readonly ILogger<GitHubIssueClient> _logger = logger;

	/// <summary>Cap for the free-text install-reported fields (correlation/install id, app version, OS) - not user-authored prose like the message, just identifiers</summary>
	private const int MaxFieldLength = 200;


	public async Task<bool> CreateIssueAsync(UserReport report, CancellationToken cancellationToken)
	{
		var client = _httpClientFactory.CreateClient(nameof(GitHubIssueClient));
		client.BaseAddress = new Uri("https://api.github.com/");
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.GitHubToken);
		client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("StockManagement.Feedback.Relay", "1.0"));
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

		var title = $"[{report.Category}] {Truncate(report.Message, 80).ReplaceLineEndings(" ")}";
		var body = $"""
			**Category:** {report.Category}
			**Correlation id:** {Truncate(report.CorrelationId, MaxFieldLength).ReplaceLineEndings(" ")}
			**App version:** {Truncate(report.AppVersion, MaxFieldLength).ReplaceLineEndings(" ")}
			**OS:** {Truncate(report.Os, MaxFieldLength).ReplaceLineEndings(" ")}
			**Install id:** {Truncate(report.InstallId, MaxFieldLength).ReplaceLineEndings(" ")}
			**Occurred (UTC):** {report.OccurredAtUtc:O}

			## Message

			{Truncate(report.Message, _options.MaxMessageLength)}

			{(report.LogExcerpt is null ? "" : $"## Log excerpt\n\n```\n{Truncate(report.LogExcerpt, _options.MaxLogExcerptLength)}\n```")}
			""";

		try
		{
			using var response = await client.PostAsJsonAsync(
				$"repos/{_options.GitHubOwner}/{_options.GitHubRepo}/issues",
				new { title, body, labels = new[] { "user-report" } },
				cancellationToken);

			if (response.IsSuccessStatusCode) return true;

			_logger.LogError("GitHub returned {StatusCode} creating an issue for report {CorrelationId}.", (int)response.StatusCode, report.CorrelationId);
			return false;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Failed to create GitHub issue for report {CorrelationId}.", report.CorrelationId);
			return false;
		}
	}

	private static string Truncate(string value, int maxLength)
	{
		return value.Length <= maxLength ? value : value[..maxLength];
	}
}
