namespace StockManagement.Feedback.Core.Contracts;


public record ReportSubmissionResult
{
	public required bool Succeeded { get; init; }

	public static ReportSubmissionResult Success { get; } = new() { Succeeded = true };
	public static ReportSubmissionResult Failure { get; } = new() { Succeeded = false };
}
