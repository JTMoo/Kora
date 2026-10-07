namespace StockManagement.Feedback.Core.Contracts;


/// <summary>
/// Where a <see cref="UserReport"/> ends up. The client never talks to this directly - see <see cref="IFeedbackService"/>.
/// </summary>
public interface IReportSink
{
	Task<ReportSubmissionResult> SubmitAsync(UserReport report, CancellationToken cancellationToken = default);
}
