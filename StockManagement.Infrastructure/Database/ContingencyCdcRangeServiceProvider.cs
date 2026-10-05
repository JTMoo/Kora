using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IContingencyCdcRangeServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class ContingencyCdcRangeServiceProvider(AppDbContext db) : IContingencyCdcRangeServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<ContingencyCdcRange?> GetAsync(CancellationToken cancellationToken = default)
	{
		return _db.ContingencyCdcRanges.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task SetRangeAsync(long rangeStart, long rangeEnd, CancellationToken cancellationToken = default)
	{
		var range = await _db.ContingencyCdcRanges.FirstOrDefaultAsync(cancellationToken);
		if (range is null)
		{
			_db.ContingencyCdcRanges.Add(new ContingencyCdcRange { RangeStart = rangeStart, RangeEnd = rangeEnd, NextNumber = rangeStart, IsActive = false });
		}
		else
		{
			range.RangeStart = rangeStart;
			range.RangeEnd = rangeEnd;
			range.NextNumber = rangeStart;
			range.IsActive = false;
		}

		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task SetActiveAsync(bool active, CancellationToken cancellationToken = default)
	{
		var range = await _db.ContingencyCdcRanges.FirstOrDefaultAsync(cancellationToken)
			?? throw new InvalidOperationException("No contingency CDC range configured.");

		range.IsActive = active;
		await _db.SaveChangesAsync(cancellationToken);
	}

	/// <remarks>
	/// Conditional <c>UPDATE ... WHERE NextNumber = @reserved</c>, same guarded-update pattern as the stock
	/// decrement in <see cref="InvoiceServiceProvider"/> - no two sales reserve the same contingency number
	/// under concurrency.
	/// </remarks>
	public async Task<long?> TryReserveNextAsync(CancellationToken cancellationToken = default)
	{
		for (var attempt = 0; attempt < 3; attempt++)
		{
			var range = await _db.ContingencyCdcRanges.FirstOrDefaultAsync(cancellationToken);
			if (range is null || !range.IsActive || range.NextNumber > range.RangeEnd) return null;

			var reserved = range.NextNumber;
			var affected = await _db.ContingencyCdcRanges
				.Where(r => r.Id == range.Id && r.NextNumber == reserved)
				.ExecuteUpdateAsync(setters => setters.SetProperty(r => r.NextNumber, reserved + 1), cancellationToken);

			if (affected > 0) return reserved;
		}

		return null;
	}
}
