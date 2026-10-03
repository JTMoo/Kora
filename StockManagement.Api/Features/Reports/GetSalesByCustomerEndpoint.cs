using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.Reports;


public sealed record GetSalesByCustomerRequest(DateTime From, DateTime To, string? Cursor, int? PageSize);


/// <param name="NextCursor">Opaque cursor for the next page; <see langword="null"/> on the last page</param>
public sealed record SalesByCustomerResponse(IReadOnlyList<SalesByCustomerRowResponse> Items, string? NextCursor);


public sealed record SalesByCustomerRowResponse(int CustomerId, string CustomerName, int InvoiceCount, decimal Total);


/// <remarks>Sales grouped by customer in [From, To] (#163), highest total first, excluding cancelled invoices</remarks>
public class GetSalesByCustomerEndpoint(IReportServiceProvider reportServiceProvider) : Endpoint<GetSalesByCustomerRequest, Results<Ok<SalesByCustomerResponse>, BadRequest>>
{
	private readonly IReportServiceProvider _reportServiceProvider = reportServiceProvider;


	public override void Configure()
	{
		this.Get("/reports/sales-by-customer");
		this.Permissions(Permission.ReportsRead);
	}

	public override async Task<Results<Ok<SalesByCustomerResponse>, BadRequest>> ExecuteAsync(GetSalesByCustomerRequest request, CancellationToken cancellationToken)
	{
		if (request.From > request.To) return TypedResults.BadRequest();

		var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);
		var page = await _reportServiceProvider.GetSalesByCustomerAsync(request.From, request.To, request.Cursor, pageSize, cancellationToken);
		return TypedResults.Ok(new SalesByCustomerResponse(page.Items.Select(row => new SalesByCustomerRowResponse(row.CustomerId, row.CustomerName, row.InvoiceCount, row.Total)).ToList(), page.NextCursor));
	}
}
