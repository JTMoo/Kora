namespace StockManagement.Feedback.Relay;


/// <summary>
/// Holds the GitHub token server-side so no customer install ever sees it (ADR-0042). Never log these values.
/// </summary>
public sealed class RelayOptions
{
	public const string SectionName = "Relay";

	/// <summary>Shared secret customer installs send as <c>X-Relay-Secret</c>.</summary>
	public string SharedSecret { get; set; } = "";

	public string GitHubToken { get; set; } = "";

	public string GitHubOwner { get; set; } = "JTMoo";

	public string GitHubRepo { get; set; } = "Kora";

	/// <summary>Per-IP fixed-window cap; cheap abuse guard, not a real rate limiter.</summary>
	public int MaxRequestsPerMinutePerIp { get; set; } = 10;

	public int MaxMessageLength { get; set; } = 4000;

	public int MaxLogExcerptLength { get; set; } = 8000;
}
