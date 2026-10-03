using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.GoodsImportDocuments;


public sealed record ListGoodsImportDocumentsRequest(string? Cursor, int? PageSize);


/// <param name="NextCursor">Opaque cursor for the next page; <see langword="null"/> on the last page</param>
public sealed record GoodsImportDocumentListResponse(IReadOnlyList<GoodsImportDocumentResponse> Items, string? NextCursor);


public class ListGoodsImportDocumentsEndpoint(IGoodsImportDocumentServiceProvider goodsImportDocumentServiceProvider) : Endpoint<ListGoodsImportDocumentsRequest, GoodsImportDocumentListResponse>
{
	private readonly IGoodsImportDocumentServiceProvider _goodsImportDocumentServiceProvider = goodsImportDocumentServiceProvider;


	public override void Configure()
	{
		this.Get("/goods-import-documents");
		this.Permissions(Permission.GoodsImportsRead);
	}

	public override async Task<GoodsImportDocumentListResponse> ExecuteAsync(ListGoodsImportDocumentsRequest request, CancellationToken cancellationToken)
	{
		var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);
		var result = await _goodsImportDocumentServiceProvider.GetGoodsImportDocumentsAsync(request.Cursor, pageSize, cancellationToken);
		return new(result.Items.Select(GoodsImportDocumentResponse.From).ToList(), result.NextCursor);
	}
}
