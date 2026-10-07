namespace StockManagement.Feedback.Core;


/// <summary>
/// Feedback/error reporting config (ADR-0042). <see cref="RelayUrl"/> empty means reporting is disabled - reports
/// are logged locally via <see cref="NullReportSink"/> instead of sent.
/// </summary>
public sealed class FeedbackOptions
{
	public const string SectionName = "Feedback";

	/// <summary>Base URL of <c>StockManagement.Feedback.Relay</c>, e.g. https://feedback.example.com</summary>
	public string RelayUrl { get; set; } = "";

	/// <summary>Shared secret sent as the <c>X-Relay-Secret</c> header; must match the relay's config.</summary>
	public string RelaySecret { get; set; } = "";

	public int MaxMessageLength { get; set; } = 4000;

	public int MaxLogExcerptLength { get; set; } = 8000;
}
