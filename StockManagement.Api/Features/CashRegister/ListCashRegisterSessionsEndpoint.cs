using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.CashRegister;


public sealed record ListCashRegisterSessionsRequest(string? Cursor, int? PageSize);


/// <param name="NextCursor">Opaque cursor for the next page; <see langword="null"/> on the last page</param>
public sealed record CashRegisterSessionListResponse(IReadOnlyList<CashRegisterSessionResponse> Items, string? NextCursor);


public class ListCashRegisterSessionsEndpoint(ICashRegisterService cashRegisterService) : Endpoint<ListCashRegisterSessionsRequest, CashRegisterSessionListResponse>
{
	private readonly ICashRegisterService _cashRegisterService = cashRegisterService;


	public override void Configure()
	{
		this.Get("/cash-register/sessions");
		this.Permissions(Permission.CashRegisterRead);
	}

	public override async Task<CashRegisterSessionListResponse> ExecuteAsync(ListCashRegisterSessionsRequest request, CancellationToken cancellationToken)
	{
		var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);
		var result = await _cashRegisterService.GetSessionsAsync(request.Cursor, pageSize, cancellationToken);

		var items = new List<CashRegisterSessionResponse>();
		foreach (var session in result.Items)
		{
			var expected = await _cashRegisterService.GetExpectedAmountAsync(session, cancellationToken);
			items.Add(CashRegisterSessionResponse.From(session, expected));
		}

		return new(items, result.NextCursor);
	}
}
