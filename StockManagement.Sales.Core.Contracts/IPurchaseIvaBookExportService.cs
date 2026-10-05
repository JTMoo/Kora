namespace StockManagement.Sales.Core.Contracts;


public interface IPurchaseIvaBookExportService
{
	/// <summary>
	/// Goods import documents dated in [<paramref name="from"/>, <paramref name="to"/>], newest first, as purchases IVA book rows
	/// </summary>
	public Task<IReadOnlyList<PurchaseIvaBookRow>> GetRowsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
