using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockManagement.Feedback.Core.Contracts;

namespace StockManagement.Feedback.Core;


/// <summary>
/// Posts to <c>StockManagement.Feedback.Relay</c>, which holds the GitHub token and creates the issue (ADR-0042).
/// The client/customer install never sees GitHub.
/// </summary>
internal sealed class HttpReportSink(IHttpClientFactory httpClientFactory, IOptions<FeedbackOptions> options, ILogger<HttpReportSink> logger) : IReportSink
{
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
	private readonly FeedbackOptions _options = options.Value;
	private readonly ILogger<HttpReportSink> _logger = logger;


	public async Task<ReportSubmissionResult> SubmitAsync(UserReport report, CancellationToken cancellationToken = default)
	{
		try
		{
			using var client = _httpClientFactory.CreateClient(nameof(HttpReportSink));
			client.BaseAddress = new Uri(_options.RelayUrl);
			client.DefaultRequestHeaders.Add("X-Relay-Secret", _options.RelaySecret);

			using var response = await client.PostAsJsonAsync("/reports", report, cancellationToken);
			if (response.IsSuccessStatusCode) return ReportSubmissionResult.Success;

			_logger.LogError("Relay returned {StatusCode} for report {CorrelationId}.", (int)response.StatusCode, report.CorrelationId);
			return ReportSubmissionResult.Failure;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Failed to submit report {CorrelationId} to relay.", report.CorrelationId);
			return ReportSubmissionResult.Failure;
		}
	}
}
