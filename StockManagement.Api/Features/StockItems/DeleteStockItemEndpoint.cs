using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Api.Features.StockItems;


public sealed record DeleteStockItemRequest(string Id);


public sealed record StockItemInUseResponse(string Reason);


/// <remarks>Matches the stored item by <c>Id</c> (docs/decisions.md: update by Id, never by business key).</remarks>
public class DeleteStockItemEndpoint(IStockItemServiceProvider stockItemServiceProvider) : Endpoint<DeleteStockItemRequest, Results<NoContent, NotFound, Conflict<StockItemInUseResponse>>>
{
	private readonly IStockItemServiceProvider _stockItemServiceProvider = stockItemServiceProvider;


	public override void Configure()
	{
		this.Delete("/stock-items/{Id}");
		this.Permissions(Permission.StockItemsWrite);
	}

	public override async Task<Results<NoContent, NotFound, Conflict<StockItemInUseResponse>>> ExecuteAsync(DeleteStockItemRequest request, CancellationToken cancellationToken)
	{
		if (await _stockItemServiceProvider.GetStockItemByIdAsync(request.Id, cancellationToken) is not StockItem stockItem) return TypedResults.NotFound();

		try
		{
			await _stockItemServiceProvider.DeleteStockItemAsync(stockItem, cancellationToken);
		}
		catch (StockItemInUseException)
		{
			return TypedResults.Conflict(new StockItemInUseResponse(global::StockManagement.Language.StockItems.stockItemInUse));
		}

		return TypedResults.NoContent();
	}
}
