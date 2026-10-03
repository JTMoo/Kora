using StockManagement.Kernel.Model;

namespace StockManagement.Kernel.Database.Interfaces;


public interface IGoodsImportDocumentServiceProvider
{
	public Task<GoodsImportDocument> GetGoodsImportDocumentByIdAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>Goods import documents by <see cref="GoodsImportDocument.Date"/> then <see cref="BaseDocument.Id"/>, one page at a time</summary>
	public Task<CursorPage<GoodsImportDocument>> GetGoodsImportDocumentsAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds the document and checks in every <see cref="GoodsImportDocumentItem.Amount"/> as stock, atomically
	/// </summary>
	/// <exception cref="Exceptions.GoodsImportDocumentProformaNumberAlreadyExistsException">Proforma number already in use</exception>
	public Task AddGoodsImportDocumentAsync(GoodsImportDocument document, CancellationToken cancellationToken = default);
}
