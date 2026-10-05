using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="ISettingsServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class SettingsServiceProvider(AppDbContext db) : ISettingsServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<AppSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
	{
		return _db.AppSettings.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task AddSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
	{
		_db.AppSettings.Add(settings);
		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task<int> UpdateSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
	{
		_db.AppSettings.Update(settings);
		await _db.SaveChangesAsync(cancellationToken);
		return 1;
	}
}
