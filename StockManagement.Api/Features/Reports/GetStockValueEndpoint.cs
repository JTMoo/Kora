using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.Reports;


/// <param name="NextCursor">Opaque cursor for the next page of <see cref="Items"/>; <see langword="null"/> on the last page</param>
public sealed record StockValueResponse(decimal TotalValue, int TotalUnits, IReadOnlyList<StockValueLineResponse> Items, string? NextCursor);


public sealed record StockValueLineResponse(string Code, string Name, string Manufacturer, int Amount, decimal Price, decimal Value);


public sealed record GetStockValueRequest(string? Cursor, int? PageSize);


/// <remarks>Current valuation of stock on hand (#163); per-item breakdown is the same page used to verify imported totals against a legacy system</remarks>
public class GetStockValueEndpoint(IReportServiceProvider reportServiceProvider, IStockItemServiceProvider stockItemServiceProvider) : Endpoint<GetStockValueRequest, StockValueResponse>
{
	private readonly IReportServiceProvider _reportServiceProvider = reportServiceProvider;
	private readonly IStockItemServiceProvider _stockItemServiceProvider = stockItemServiceProvider;


	public override void Configure()
	{
		this.Get("/reports/stock-value");
		this.Permissions(Permission.ReportsRead);
	}

	public override async Task<StockValueResponse> ExecuteAsync(GetStockValueRequest request, CancellationToken cancellationToken)
	{
		var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);
		var totals = await _reportServiceProvider.GetStockValueTotalsAsync(cancellationToken);
		var page = await _stockItemServiceProvider.GetStockItemsAsync(request.Cursor, pageSize, cancellationToken);

		var lines = page.Items.Select(item => new StockValueLineResponse(item.Code, item.Name, item.Manufacturer, item.Amount, item.Price, item.Amount * item.Price)).ToList();
		return new(totals.TotalValue, totals.TotalUnits, lines, page.NextCursor);
	}
}
