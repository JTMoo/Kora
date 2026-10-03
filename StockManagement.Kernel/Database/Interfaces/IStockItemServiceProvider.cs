using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model;

namespace StockManagement.Kernel.Database.Interfaces;


public interface IStockItemServiceProvider
{
	public Task<StockItem> GetStockItemAsync(string code, CancellationToken cancellationToken = default);
	public Task<StockItem> GetStockItemByIdAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>Scan-to-sell lookup (#165); <paramref name="barcode"/> empty never matches</summary>
	public Task<StockItem?> GetStockItemByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
	public Task<IEnumerable<StockItem>> GetAllStockItemsAsync(CancellationToken cancellationToken = default);

	/// <summary>Stock items by <see cref="StockItem.Code"/> then <see cref="BaseDocument.Id"/>, one page at a time</summary>
	public Task<CursorPage<StockItem>> GetStockItemsAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default);

	/// <returns>Items with <see cref="StockItem.MinimumStock"/> &gt; 0 and <see cref="StockItem.Amount"/> below it (#57)</returns>
	public Task<IEnumerable<StockItem>> GetStockItemsBelowMinimumAsync(CancellationToken cancellationToken = default);

	/// <summary>Paged version of <see cref="GetStockItemsBelowMinimumAsync(CancellationToken)"/>, by <see cref="StockItem.Code"/> then <see cref="BaseDocument.Id"/></summary>
	public Task<CursorPage<StockItem>> GetStockItemsBelowMinimumAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default);

	/// <returns>Rows affected; 1 on success</returns>
	public Task<int> UpdateStockItemAsync(StockItem stockItem, CancellationToken cancellationToken = default);

	/// <returns>Rows affected; 1 on success</returns>
	public Task<int> DeleteStockItemAsync(StockItem stockItem, CancellationToken cancellationToken = default);
	public Task AddStockItemAsync(StockItem stockItem, CancellationToken cancellationToken = default);
	public Task AddManyStockItemsAsync(IList<StockItem> stockItem, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds units to stock, recording a <see cref="Transaction"/> with the given reason
	/// </summary>
	public Task CheckInStockItemAsync(StockItem stockItem, int amount, string reason, CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes units from stock if enough are in stock, recording a <see cref="Transaction"/> with the given reason
	/// </summary>
	/// <returns>False when fewer than <paramref name="amount"/> units are in stock; nothing written then</returns>
	public Task<bool> TryCheckOutStockItemAsync(StockItem stockItem, int amount, string reason, CancellationToken cancellationToken = default);
}
