using StockManagement.Kernel.Model;

namespace StockManagement.Api.Features.StockItems;


/// <summary>
/// Article with its units in stock
/// </summary>
public sealed record StockItemResponse(string Id, string Code, string Name, string Description, string Location, int Amount, decimal Price, string Manufacturer, decimal Factor, decimal PurchasePrice, decimal PurchaseExchangeRate, decimal AdditionalPurchaseCost, string? SupplierId, string? SupplierName, int MinimumStock, decimal VatRatePercent, string Barcode)
{
	public static StockItemResponse From(StockItem stockItem)
	{
		return new(stockItem.Id, stockItem.Code, stockItem.Name, stockItem.Description, stockItem.Location, stockItem.Amount, stockItem.Price, stockItem.Manufacturer, stockItem.Factor, stockItem.PurchasePrice, stockItem.PurchaseExchangeRate, stockItem.AdditionalPurchaseCost,
			stockItem.SupplierId, stockItem.Supplier?.Name, stockItem.MinimumStock, stockItem.VatRatePercent, stockItem.Barcode);
	}
}
