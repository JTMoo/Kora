namespace StockManagement.Feedback.Core.Contracts;


/// <summary>
/// Single entry point for both the manual "report a problem" action and global exception handling. See ADR-0042.
/// </summary>
public interface IFeedbackService
{
	Task<ReportSubmissionResult> SubmitAsync(ReportCategory category, string message, string? logExcerpt, string correlationId, CancellationToken cancellationToken = default);
}
