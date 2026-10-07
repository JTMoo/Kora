using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="ILicenseServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class LicenseServiceProvider(AppDbContext db) : ILicenseServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<LicenseState?> GetAsync(CancellationToken cancellationToken = default)
	{
		return _db.LicenseStates.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task AddAsync(LicenseState state, CancellationToken cancellationToken = default)
	{
		_db.LicenseStates.Add(state);
		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task<int> UpdateAsync(LicenseState state, CancellationToken cancellationToken = default)
	{
		_db.LicenseStates.Update(state);
		await _db.SaveChangesAsync(cancellationToken);
		return 1;
	}
}
