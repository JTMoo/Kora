namespace StockManagement.Feedback.Core.Contracts;


/// <summary>
/// A feedback or bug report, manually submitted or offered by global exception handling. See ADR-0042.
/// </summary>
public record UserReport
{
	public required ReportCategory Category { get; init; }
	public required string Message { get; init; }
	public string? LogExcerpt { get; init; }
	public required string CorrelationId { get; init; }
	public required string AppVersion { get; init; }
	public required string Os { get; init; }
	public required string InstallId { get; init; }
	public required DateTimeOffset OccurredAtUtc { get; init; }
}
