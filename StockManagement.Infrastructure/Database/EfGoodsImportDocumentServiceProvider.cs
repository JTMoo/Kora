using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IGoodsImportDocumentServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
/// <remarks>Checks in every item's stock in the same transaction the document is inserted in (atomic multi-step write)</remarks>
public class EfGoodsImportDocumentServiceProvider(AppDbContext db) : IGoodsImportDocumentServiceProvider
{
	private readonly AppDbContext _db = db;


	public Task<GoodsImportDocument> GetGoodsImportDocumentByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		return _db.GoodsImportDocuments.SingleOrDefaultAsync(document => document.Id == id, cancellationToken)!;
	}

	/// <summary>Keyset page by <see cref="GoodsImportDocument.Date"/> then <see cref="BaseDocument.Id"/> (ADR-0029)</summary>
	public async Task<CursorPage<GoodsImportDocument>> GetGoodsImportDocumentsAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		var query = _db.GoodsImportDocuments.AsQueryable();
		if (Cursor.TryDecode(cursor, 2) is [var lastDateRaw, var lastId] && DateTime.TryParse(lastDateRaw, out var lastDate))
		{
			query = query.Where(document => document.Date > lastDate || (document.Date == lastDate && document.Id.CompareTo(lastId) > 0));
		}

		var page = await query.OrderBy(document => document.Date).ThenBy(document => document.Id).Take(pageSize + 1).ToListAsync(cancellationToken);

		var items = page.Take(pageSize).ToList();
		var nextCursor = page.Count > pageSize ? Cursor.Encode(items[^1].Date.ToString("O"), items[^1].Id) : null;
		return new(items, nextCursor);
	}

	/// <exception cref="GoodsImportDocumentProformaNumberAlreadyExistsException">Proforma number already in use</exception>
	public async Task AddGoodsImportDocumentAsync(GoodsImportDocument document, CancellationToken cancellationToken = default)
	{
		await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

		_db.GoodsImportDocuments.Add(document);

		foreach (var item in document.Items)
		{
			// Track the StockItem's own state first: Add() on the Transaction below would otherwise graph-fixup it as Added too
			_db.StockItems.Attach(item.StockItem);
			await _db.StockItems
				.Where(stockItem => stockItem.Id == item.StockItem.Id)
				.ExecuteUpdateAsync(setters => setters.SetProperty(stockItem => stockItem.Amount, stockItem => stockItem.Amount + item.Amount), cancellationToken);

			_db.Transactions.Add(new Transaction(item.StockItem, DateTime.Now, Transaction.Kind.Amount, item.Amount, $"Goods receipt - import {document.ProformaNumber}"));
		}

		try
		{
			await this.SaveChangesAsync(cancellationToken);
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}

		await transaction.CommitAsync(cancellationToken);
		foreach (var item in document.Items) item.StockItem.Amount += item.Amount;
	}

	private async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		try
		{
			await _db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
		{
			throw new GoodsImportDocumentProformaNumberAlreadyExistsException();
		}
	}
}
