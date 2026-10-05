using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.CashRegister;


public sealed record GetCashRegisterSessionRequest(string Id);


public class GetCashRegisterSessionEndpoint(ICashRegisterService cashRegisterService) : Endpoint<GetCashRegisterSessionRequest, Results<Ok<CashRegisterSessionResponse>, NotFound>>
{
	private readonly ICashRegisterService _cashRegisterService = cashRegisterService;


	public override void Configure()
	{
		this.Get("/cash-register/sessions/{Id}");
		this.Permissions(Permission.CashRegisterRead);
	}

	public override async Task<Results<Ok<CashRegisterSessionResponse>, NotFound>> ExecuteAsync(GetCashRegisterSessionRequest request, CancellationToken cancellationToken)
	{
		if (await _cashRegisterService.GetSessionAsync(request.Id, cancellationToken) is not { } session) return TypedResults.NotFound();

		var expected = await _cashRegisterService.GetExpectedAmountAsync(session, cancellationToken);
		return TypedResults.Ok(CashRegisterSessionResponse.From(session, expected));
	}
}
