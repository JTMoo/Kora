using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.Invoices;


public sealed record ExportIvaBookRequest(DateTime From, DateTime To);


/// <remarks>CSV handoff for Marangatu's monthly IVA filing (#123); column layout is unverified against DNIT's spec - see <see cref="IvaBookRow"/>.</remarks>
public class ExportIvaBookEndpoint(IIvaBookExportService ivaBookExportService) : Endpoint<ExportIvaBookRequest, Results<FileContentHttpResult, BadRequest>>
{
	private readonly IIvaBookExportService _ivaBookExportService = ivaBookExportService;


	public override void Configure()
	{
		this.Get("/invoices/iva-book");
		this.Permissions(Permission.SalesRead);
	}

	public override async Task<Results<FileContentHttpResult, BadRequest>> ExecuteAsync(ExportIvaBookRequest request, CancellationToken cancellationToken)
	{
		if (request.From > request.To) return TypedResults.BadRequest();

		var rows = await _ivaBookExportService.GetRowsAsync(request.From, request.To, cancellationToken);
		var csv = Encoding.UTF8.GetBytes(IvaBookCsvWriter.BuildCsv(rows));
		var fileName = $"iva-book-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}.csv";
		return TypedResults.File(csv, "text/csv", fileName);
	}
}
