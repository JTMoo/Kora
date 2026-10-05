using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.CashRegister;


public class GetCurrentCashRegisterSessionEndpoint(ICashRegisterService cashRegisterService) : EndpointWithoutRequest<Results<Ok<CashRegisterSessionResponse>, NotFound>>
{
	private readonly ICashRegisterService _cashRegisterService = cashRegisterService;


	public override void Configure()
	{
		this.Get("/cash-register/sessions/current");
		this.Permissions(Permission.CashRegisterRead);
	}

	public override async Task<Results<Ok<CashRegisterSessionResponse>, NotFound>> ExecuteAsync(CancellationToken cancellationToken)
	{
		if (await _cashRegisterService.GetOpenSessionAsync(cancellationToken) is not { } session) return TypedResults.NotFound();

		var expected = await _cashRegisterService.GetExpectedAmountAsync(session, cancellationToken);
		return TypedResults.Ok(CashRegisterSessionResponse.From(session, expected));
	}
}
