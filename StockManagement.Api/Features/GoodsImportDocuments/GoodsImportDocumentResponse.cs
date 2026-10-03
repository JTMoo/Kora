using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Features.GoodsImportDocuments;


/// <summary>
/// Stored goods-import document (#164)
/// </summary>
public sealed record GoodsImportDocumentResponse(string Id, string ProformaNumber, Incoterm Incoterm, string BrokerName, string DuaReference, DateTime Date, string SupplierId, string SupplierName, IReadOnlyList<GoodsImportDocumentLineResponse> Items)
{
	public static GoodsImportDocumentResponse From(GoodsImportDocument document)
	{
		var lines = (document.Items ?? []).Select(item => new GoodsImportDocumentLineResponse(item.StockItem.Code, item.StockItem.Name, item.Amount)).ToList();
		return new(document.Id, document.ProformaNumber, document.Incoterm, document.BrokerName, document.DuaReference, document.Date, document.Supplier?.Id ?? "", document.Supplier?.Name ?? "", lines);
	}
}


public sealed record GoodsImportDocumentLineResponse(string Code, string Name, int Amount);
