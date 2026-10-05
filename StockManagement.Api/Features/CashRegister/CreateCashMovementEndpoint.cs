using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.CashRegister;


public sealed record CreateCashMovementRequest(string Id, CashMovementType Type, decimal Amount, string Reason);


public class CreateCashMovementValidator : Validator<CreateCashMovementRequest>
{
	public CreateCashMovementValidator()
	{
		this.RuleFor(request => request.Amount).GreaterThan(0).WithMessage("amountNotPositive");
		this.RuleFor(request => request.Reason).NotEmpty().WithMessage("reasonRequired");
	}
}


public class CreateCashMovementEndpoint(ICashRegisterService cashRegisterService) : Endpoint<CreateCashMovementRequest, Results<Created<CashMovementResponse>, NotFound, Conflict<CashRegisterConflictResponse>>>
{
	private readonly ICashRegisterService _cashRegisterService = cashRegisterService;


	public override void Configure()
	{
		this.Post("/cash-register/sessions/{Id}/movements");
		this.Permissions(Permission.CashRegisterWrite);
	}

	public override async Task<Results<Created<CashMovementResponse>, NotFound, Conflict<CashRegisterConflictResponse>>> ExecuteAsync(CreateCashMovementRequest request, CancellationToken cancellationToken)
	{
		if (await _cashRegisterService.GetOpenSessionAsync(cancellationToken) is not { } open || open.Id != request.Id) return TypedResults.NotFound();

		var userId = this.User.FindFirst("sub")?.Value ?? "";
		var result = await _cashRegisterService.AddMovementAsync(request.Type, request.Amount, request.Reason, userId, cancellationToken);
		if (!result.Succeeded || result.Movement is null)
		{
			return TypedResults.Conflict(new CashRegisterConflictResponse("amountNotPositive"));
		}

		return TypedResults.Created($"/cash-register/sessions/{request.Id}/movements", CashMovementResponse.From(result.Movement));
	}
}
