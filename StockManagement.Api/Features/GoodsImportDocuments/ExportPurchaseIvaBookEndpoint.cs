using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.GoodsImportDocuments;


public sealed record ExportPurchaseIvaBookRequest(DateTime From, DateTime To);


/// <remarks>CSV handoff for Hechauka's monthly purchases IVA filing (#186); column layout is unverified against DNIT's spec - see <see cref="PurchaseIvaBookRow"/>.</remarks>
public class ExportPurchaseIvaBookEndpoint(IPurchaseIvaBookExportService purchaseIvaBookExportService) : Endpoint<ExportPurchaseIvaBookRequest, Results<FileContentHttpResult, BadRequest>>
{
	private readonly IPurchaseIvaBookExportService _purchaseIvaBookExportService = purchaseIvaBookExportService;


	public override void Configure()
	{
		this.Get("/goods-import-documents/iva-book");
		this.Permissions(Permission.GoodsImportsRead);
	}

	public override async Task<Results<FileContentHttpResult, BadRequest>> ExecuteAsync(ExportPurchaseIvaBookRequest request, CancellationToken cancellationToken)
	{
		if (request.From > request.To) return TypedResults.BadRequest();

		var rows = await _purchaseIvaBookExportService.GetRowsAsync(request.From, request.To, cancellationToken);
		var csv = Encoding.UTF8.GetBytes(PurchaseIvaBookCsvWriter.BuildCsv(rows));
		var fileName = $"purchase-iva-book-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.csv";
		return TypedResults.File(csv, "text/csv", fileName);
	}
}
