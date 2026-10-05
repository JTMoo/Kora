using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.Sifen;


public sealed record RequestCancellationRequest(string InvoiceNumber, string Reason);


public sealed record CancellationNotAllowedResponse(string Reason);


public sealed record CancellationRequestResponse(string InvoiceNumber, string Reason, DateTime RequestedAt, TransmissionStatus TransmissionStatus)
{
	public static CancellationRequestResponse From(CancellationRequest request) => new(request.Invoice.Number, request.Reason, request.RequestedAt, request.TransmissionStatus);
}


public class RequestCancellationValidator : Validator<RequestCancellationRequest>
{
	public RequestCancellationValidator()
	{
		this.RuleFor(request => request.InvoiceNumber).NotEmpty().WithMessage("invoiceNumberRequired");
		this.RuleFor(request => request.Reason).NotEmpty().WithMessage("reasonRequired");
	}
}


/// <remarks>
/// SIFEN Cancelación (#206): retracts an already-Accepted DE, up to 48h after acceptance. Queues the event for
/// transmission (<see cref="Sifen.Core.SifenTransmissionWorker"/>) - does not call SIFEN inline.
/// </remarks>
public class RequestCancellationEndpoint(ISifenEventService sifenEventService)
	: Endpoint<RequestCancellationRequest, Results<Created<CancellationRequestResponse>, UnprocessableEntity<CancellationNotAllowedResponse>>>
{
	private readonly ISifenEventService _sifenEventService = sifenEventService;


	public override void Configure()
	{
		this.Post("/sifen/cancellations");
		this.Permissions(Permission.SalesWrite);
	}

	public override async Task<Results<Created<CancellationRequestResponse>, UnprocessableEntity<CancellationNotAllowedResponse>>> ExecuteAsync(RequestCancellationRequest request, CancellationToken cancellationToken)
	{
		try
		{
			var cancellationRequest = await _sifenEventService.RequestCancellationAsync(request.InvoiceNumber, request.Reason, DateTime.Now, cancellationToken);
			return TypedResults.Created($"/api/sifen/cancellations/{cancellationRequest.Invoice.Number}", CancellationRequestResponse.From(cancellationRequest));
		}
		catch (InvalidOperationException ex)
		{
			return TypedResults.UnprocessableEntity(new CancellationNotAllowedResponse(ex.Message));
		}
	}
}
