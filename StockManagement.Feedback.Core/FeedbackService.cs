using Microsoft.Extensions.Options;
using StockManagement.Feedback.Core.Contracts;

namespace StockManagement.Feedback.Core;


internal sealed class FeedbackService(IReportSink reportSink, IOptions<FeedbackOptions> options) : IFeedbackService
{
	private readonly IReportSink _reportSink = reportSink;
	private readonly FeedbackOptions _options = options.Value;


	public Task<ReportSubmissionResult> SubmitAsync(ReportCategory category, string message, string? logExcerpt, string correlationId, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(message);
		ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

		var report = new UserReport
		{
			Category = category,
			Message = Truncate(message, _options.MaxMessageLength),
			LogExcerpt = logExcerpt is null ? null : Truncate(logExcerpt, _options.MaxLogExcerptLength),
			CorrelationId = correlationId,
			AppVersion = typeof(FeedbackService).Assembly.GetName().Version?.ToString() ?? "unknown",
			Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
			InstallId = InstallId.Current,
			OccurredAtUtc = DateTimeOffset.UtcNow
		};

		return _reportSink.SubmitAsync(report, cancellationToken);
	}

	private static string Truncate(string value, int maxLength)
	{
		return value.Length <= maxLength ? value : value[..maxLength];
	}
}
