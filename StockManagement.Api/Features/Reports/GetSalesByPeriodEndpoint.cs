using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.Reports;


public sealed record GetSalesByPeriodRequest(DateTime From, DateTime To);


public sealed record SalesByPeriodResponse(IReadOnlyList<SalesByPeriodRowResponse> Items);


public sealed record SalesByPeriodRowResponse(DateTime Date, int InvoiceCount, decimal Total, decimal Tax);


/// <remarks>Sales grouped by day in [From, To] (#163), excluding cancelled invoices</remarks>
public class GetSalesByPeriodEndpoint(IReportServiceProvider reportServiceProvider) : Endpoint<GetSalesByPeriodRequest, Results<Ok<SalesByPeriodResponse>, BadRequest>>
{
	private readonly IReportServiceProvider _reportServiceProvider = reportServiceProvider;


	public override void Configure()
	{
		this.Get("/reports/sales-by-period");
		this.Permissions(Permission.ReportsRead);
	}

	public override async Task<Results<Ok<SalesByPeriodResponse>, BadRequest>> ExecuteAsync(GetSalesByPeriodRequest request, CancellationToken cancellationToken)
	{
		if (request.From > request.To) return TypedResults.BadRequest();

		var rows = await _reportServiceProvider.GetSalesByPeriodAsync(request.From, request.To, cancellationToken);
		return TypedResults.Ok(new SalesByPeriodResponse(rows.Select(row => new SalesByPeriodRowResponse(row.Date, row.InvoiceCount, row.Total, row.Tax)).ToList()));
	}
}
