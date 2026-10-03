using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Api.Features.GoodsImportDocuments;


public sealed record GetGoodsImportDocumentRequest(string Id);


public class GetGoodsImportDocumentEndpoint(IGoodsImportDocumentServiceProvider goodsImportDocumentServiceProvider) : Endpoint<GetGoodsImportDocumentRequest, Results<Ok<GoodsImportDocumentResponse>, NotFound>>
{
	private readonly IGoodsImportDocumentServiceProvider _goodsImportDocumentServiceProvider = goodsImportDocumentServiceProvider;


	public override void Configure()
	{
		this.Get("/goods-import-documents/{Id}");
		this.Permissions(Permission.GoodsImportsRead);
	}

	public override async Task<Results<Ok<GoodsImportDocumentResponse>, NotFound>> ExecuteAsync(GetGoodsImportDocumentRequest request, CancellationToken cancellationToken)
	{
		if (await _goodsImportDocumentServiceProvider.GetGoodsImportDocumentByIdAsync(request.Id, cancellationToken) is not GoodsImportDocument document) return TypedResults.NotFound();

		return TypedResults.Ok(GoodsImportDocumentResponse.From(document));
	}
}
