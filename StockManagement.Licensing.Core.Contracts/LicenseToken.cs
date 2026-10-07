namespace StockManagement.Licensing.Core.Contracts;


/// <summary>
/// Decoded, signature-verified payload of an activation key issued by the (future, out of scope) license server
/// </summary>
/// <param name="Licensee">Free-text identifying who the key was issued to (company name/email); display only</param>
public sealed record LicenseToken(string Licensee, LicensePlan Plan, DateTime IssuedAtUtc, DateTime ExpiresAtUtc);
