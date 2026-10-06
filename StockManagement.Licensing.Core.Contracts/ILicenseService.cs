namespace StockManagement.Licensing.Core.Contracts;


public interface ILicenseService
{
	/// <summary>
	/// Current license state; starts the trial on first call if none is stored yet (fresh install or pre-licensing upgrade)
	/// </summary>
	public Task<LicenseSnapshot> GetStatusAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Verifies <paramref name="licenseKey"/> and, if valid, stores it as the active license
	/// </summary>
	/// <returns><see langword="true"/> and the new status on a valid, not-yet-expired key; <see langword="false"/> otherwise</returns>
	public Task<(bool Success, LicenseSnapshot Status)> TryActivateAsync(string licenseKey, CancellationToken cancellationToken = default);
}
