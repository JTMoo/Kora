using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IImportBatchServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class ImportBatchServiceProvider(AppDbContext db) : IImportBatchServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<ImportBatch?> GetImportBatchAsync(string id, CancellationToken cancellationToken = default)
	{
		return _db.ImportBatches.SingleOrDefaultAsync(batch => batch.Id == id, cancellationToken);
	}

	public async Task AddImportBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default)
	{
		_db.ImportBatches.Add(batch);
		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task<int> UpdateImportBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default)
	{
		_db.ImportBatches.Update(batch);
		await _db.SaveChangesAsync(cancellationToken);
		return 1;
	}
}
