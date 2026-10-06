using Microsoft.Extensions.Logging;
using StockManagement.Feedback.Core.Contracts;

namespace StockManagement.Feedback.Core;


/// <summary>Used when <see cref="FeedbackOptions.RelayUrl"/> is empty - logs instead of sending.</summary>
internal sealed class NullReportSink(ILogger<NullReportSink> logger) : IReportSink
{
	private readonly ILogger<NullReportSink> _logger = logger;


	public Task<ReportSubmissionResult> SubmitAsync(UserReport report, CancellationToken cancellationToken = default)
	{
		_logger.LogWarning("Feedback:RelayUrl not configured; dropping {Category} report {CorrelationId}.", report.Category, report.CorrelationId);
		return Task.FromResult(ReportSubmissionResult.Success);
	}
}
