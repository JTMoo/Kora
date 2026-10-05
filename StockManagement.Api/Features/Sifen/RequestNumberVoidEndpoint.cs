using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.Sifen;


public sealed record RequestNumberVoidRequest(int RangeStart, int RangeEnd, string Reason);


public sealed record NumberVoidNotAllowedResponse(string Reason);


public sealed record NumberVoidResponse(int RangeStart, int RangeEnd, string Reason, DateTime RequestedAt, TransmissionStatus TransmissionStatus)
{
	public static NumberVoidResponse From(InvoiceNumberVoid numberVoid) => new(numberVoid.RangeStart, numberVoid.RangeEnd, numberVoid.Reason, numberVoid.RequestedAt, numberVoid.TransmissionStatus);
}


public class RequestNumberVoidValidator : Validator<RequestNumberVoidRequest>
{
	public RequestNumberVoidValidator()
	{
		this.RuleFor(request => request.RangeStart).GreaterThan(0).WithMessage("rangeStartNotPositive");
		this.RuleFor(request => request.RangeEnd).GreaterThanOrEqualTo(request => request.RangeStart).WithMessage("rangeEndBeforeStart");
		this.RuleFor(request => request.Reason).NotEmpty().MaximumLength(150).WithMessage("reasonInvalid");
	}
}


/// <remarks>
/// SIFEN Inutilización (#206): voids up to 1000 unused sequential invoice numbers. Queues the event for
/// transmission (<see cref="Sifen.Core.SifenTransmissionWorker"/>) - does not call SIFEN inline.
/// </remarks>
public class RequestNumberVoidEndpoint(ISifenEventService sifenEventService)
	: Endpoint<RequestNumberVoidRequest, Results<Created<NumberVoidResponse>, UnprocessableEntity<NumberVoidNotAllowedResponse>>>
{
	private readonly ISifenEventService _sifenEventService = sifenEventService;


	public override void Configure()
	{
		this.Post("/sifen/number-voids");
		this.Permissions(Permission.SalesWrite);
	}

	public override async Task<Results<Created<NumberVoidResponse>, UnprocessableEntity<NumberVoidNotAllowedResponse>>> ExecuteAsync(RequestNumberVoidRequest request, CancellationToken cancellationToken)
	{
		try
		{
			var numberVoid = await _sifenEventService.RequestNumberVoidAsync(request.RangeStart, request.RangeEnd, request.Reason, DateTime.Now, cancellationToken);
			return TypedResults.Created($"/api/sifen/number-voids/{numberVoid.Id}", NumberVoidResponse.From(numberVoid));
		}
		catch (ArgumentException ex)
		{
			return TypedResults.UnprocessableEntity(new NumberVoidNotAllowedResponse(ex.Message));
		}
	}
}
