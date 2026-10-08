namespace StockManagement.Licensing.Core.Contracts;


/// <summary>
/// Decoded, signature-verified payload of an activation key issued by the (future, out of scope) license server
/// </summary>
/// <param name="Licensee">Free-text identifying who the key was issued to (company name/email); display only</param>
/// <param name="MachineId">Machine this key was issued for, or <see langword="null"/> if unbound (see ADR-0043); checked against <see cref="Kernel.Model.LicenseState.MachineId"/></param>
/// <param name="DiscountPercent">Owner-granted discount baked in at issuance (0-100), or <see langword="null"/> if none (ADR-0043); display only, never recomputed client-side</param>
/// <param name="EffectivePricePyg">Final price after any discount/override, whole PYG, baked in at issuance; display only</param>
public sealed record LicenseToken(string Licensee, LicensePlan Plan, DateTime IssuedAtUtc, DateTime ExpiresAtUtc, string? MachineId = null, int? DiscountPercent = null, int? EffectivePricePyg = null);
