using StockManagement.Kernel.Model;

namespace StockManagement.Kernel.Database.Interfaces;


public interface ILicenseServiceProvider
{
	/// <returns>The stored license state row, or <see langword="null"/> if none was stored yet</returns>
	public Task<LicenseState?> GetAsync(CancellationToken cancellationToken = default);

	public Task AddAsync(LicenseState state, CancellationToken cancellationToken = default);

	/// <returns>Rows affected; 1 on success</returns>
	public Task<int> UpdateAsync(LicenseState state, CancellationToken cancellationToken = default);
}
