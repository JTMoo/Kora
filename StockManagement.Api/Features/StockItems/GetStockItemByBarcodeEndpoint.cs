using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Api.Features.StockItems;


public sealed record GetStockItemByBarcodeRequest(string Barcode);


/// <remarks>Scan-to-sell lookup (#165); distinct from <see cref="GetStockItemEndpoint"/>'s by-code lookup since a scanner reads Barcode, not Code</remarks>
public class GetStockItemByBarcodeEndpoint(IStockItemServiceProvider stockItemServiceProvider) : Endpoint<GetStockItemByBarcodeRequest, Results<Ok<StockItemResponse>, NotFound>>
{
	private readonly IStockItemServiceProvider _stockItemServiceProvider = stockItemServiceProvider;


	public override void Configure()
	{
		this.Get("/stock-items/by-barcode/{Barcode}");
		this.Permissions(Permission.StockItemsRead);
	}

	public override async Task<Results<Ok<StockItemResponse>, NotFound>> ExecuteAsync(GetStockItemByBarcodeRequest request, CancellationToken cancellationToken)
	{
		if (await _stockItemServiceProvider.GetStockItemByBarcodeAsync(request.Barcode, cancellationToken) is not StockItem stockItem) return TypedResults.NotFound();

		return TypedResults.Ok(StockItemResponse.From(stockItem));
	}
}
