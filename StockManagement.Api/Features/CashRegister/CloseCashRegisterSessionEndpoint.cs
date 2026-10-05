using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.CashRegister;


public sealed record CloseCashRegisterSessionRequest(string Id, decimal CountedAmount, string? Note);


public sealed record CashRegisterCloseReportResponse(CashRegisterSessionResponse Session, decimal ExpectedAmount, decimal CountedAmount, decimal Difference)
{
	public static CashRegisterCloseReportResponse From(CashRegisterCloseReport report)
	{
		return new(CashRegisterSessionResponse.From(report.Session, report.ExpectedAmount), report.ExpectedAmount, report.CountedAmount, report.Difference);
	}
}


public class CloseCashRegisterSessionValidator : Validator<CloseCashRegisterSessionRequest>
{
	public CloseCashRegisterSessionValidator()
	{
		this.RuleFor(request => request.CountedAmount).GreaterThanOrEqualTo(0).WithMessage("negativeCountedAmount");
	}
}


public class CloseCashRegisterSessionEndpoint(ICashRegisterService cashRegisterService) : Endpoint<CloseCashRegisterSessionRequest, Results<Ok<CashRegisterCloseReportResponse>, NotFound, Conflict<CashRegisterConflictResponse>>>
{
	private readonly ICashRegisterService _cashRegisterService = cashRegisterService;


	public override void Configure()
	{
		this.Post("/cash-register/sessions/{Id}/close");
		this.Permissions(Permission.CashRegisterWrite);
	}

	public override async Task<Results<Ok<CashRegisterCloseReportResponse>, NotFound, Conflict<CashRegisterConflictResponse>>> ExecuteAsync(CloseCashRegisterSessionRequest request, CancellationToken cancellationToken)
	{
		if (await _cashRegisterService.GetOpenSessionAsync(cancellationToken) is not { } open || open.Id != request.Id) return TypedResults.NotFound();

		var userId = this.User.FindFirst("sub")?.Value ?? "";
		var result = await _cashRegisterService.CloseSessionAsync(request.CountedAmount, request.Note ?? "", userId, cancellationToken);
		if (!result.Succeeded || result.Report is null)
		{
			return TypedResults.Conflict(new CashRegisterConflictResponse("negativeCountedAmount"));
		}

		return TypedResults.Ok(CashRegisterCloseReportResponse.From(result.Report));
	}
}
