using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.CashRegister;


public sealed record OpenCashRegisterSessionRequest(decimal OpeningFloat);


public sealed record CashRegisterConflictResponse(string Reason);


public class OpenCashRegisterSessionValidator : Validator<OpenCashRegisterSessionRequest>
{
	public OpenCashRegisterSessionValidator()
	{
		this.RuleFor(request => request.OpeningFloat).GreaterThanOrEqualTo(0).WithMessage("invalidOpeningFloat");
	}
}


public class OpenCashRegisterSessionEndpoint(ICashRegisterService cashRegisterService) : Endpoint<OpenCashRegisterSessionRequest, Results<Created<CashRegisterSessionResponse>, Conflict<CashRegisterConflictResponse>>>
{
	private readonly ICashRegisterService _cashRegisterService = cashRegisterService;


	public override void Configure()
	{
		this.Post("/cash-register/sessions");
		this.Permissions(Permission.CashRegisterWrite);
	}

	public override async Task<Results<Created<CashRegisterSessionResponse>, Conflict<CashRegisterConflictResponse>>> ExecuteAsync(OpenCashRegisterSessionRequest request, CancellationToken cancellationToken)
	{
		var userId = this.User.FindFirst("sub")?.Value ?? "";
		var result = await _cashRegisterService.OpenSessionAsync(request.OpeningFloat, userId, cancellationToken);
		if (!result.Succeeded || result.Session is null)
		{
			return TypedResults.Conflict(new CashRegisterConflictResponse(result.Error == OpenCashRegisterSessionError.AlreadyOpen ? "cashRegisterSessionAlreadyOpen" : "invalidOpeningFloat"));
		}

		return TypedResults.Created($"/cash-register/sessions/{result.Session.Id}", CashRegisterSessionResponse.From(result.Session, result.Session.OpeningFloat));
	}
}
