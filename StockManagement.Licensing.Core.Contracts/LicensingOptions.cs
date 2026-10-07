namespace StockManagement.Licensing.Core.Contracts;


/// <summary>
/// Bound from the <c>Licensing</c> config section (see ADR-0041)
/// </summary>
public sealed class LicensingOptions
{
	public const string SectionName = "Licensing";

	public int TrialDays { get; set; } = 10;

	/// <summary>
	/// Extra days a lapsed/expired subscription still works, to absorb a late payment without an instant lockout
	/// </summary>
	public int GraceDays { get; set; } = 3;

	/// <summary>
	/// Monthly price in whole PYG; the yearly price is always 10x this (owner decision)
	/// </summary>
	public int MonthlyPricePyg { get; set; } = 150_000;

	/// <summary>
	/// SubjectPublicKeyInfo of the license server's signing key, base64. Empty disables activation (trial/locked only).
	/// </summary>
	public string PublicKey { get; set; } = "";

	public int YearlyPricePyg => this.MonthlyPricePyg * 10;
}
