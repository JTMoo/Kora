namespace StockManagement.Licensing.Core.Contracts;


/// <summary>
/// Verifies license keys offline against the license server's public signing key (see ADR-0041)
/// </summary>
public interface ILicenseTokenVerifier
{
	public bool TryVerify(string licenseKey, out LicenseToken? token);
}
