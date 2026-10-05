using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.DebitNotes;


public sealed record CreateDebitNoteRequest(string InvoiceNumber, string Reason, IReadOnlyList<DebitNoteItemRequest> Items);


public sealed record DebitNoteItemRequest(string Description, decimal Amount, int VatRatePercent);


public sealed record InvoiceNotTransmittedResponse(string Reason);


public class CreateDebitNoteValidator : Validator<CreateDebitNoteRequest>
{
	public CreateDebitNoteValidator()
	{
		this.RuleFor(request => request.InvoiceNumber).NotEmpty().WithMessage("invoiceNumberRequired");
		this.RuleFor(request => request.Reason).NotEmpty().WithMessage("reasonRequired");
		this.RuleFor(request => request.Items).NotEmpty().WithMessage("itemsRequired");
		this.RuleForEach(request => request.Items).ChildRules(item =>
		{
			item.RuleFor(line => line.Description).NotEmpty().WithMessage("descriptionRequired");
			item.RuleFor(line => line.Amount).GreaterThan(0).WithMessage("amountNotPositive");
		});
	}
}


/// <remarks>
/// Does not change the invoice or its stock - a debit note only adds charges (#184). The invoice must already
/// carry a SIFEN <c>Cdc</c> (transmitted) before it can be referenced; a debit note can't point at a DE DNIT never saw.
/// </remarks>
public class CreateDebitNoteEndpoint(IDebitNoteService debitNoteService, IInvoiceServiceProvider invoiceServiceProvider)
	: Endpoint<CreateDebitNoteRequest, Results<Created<DebitNoteResponse>, NotFound, UnprocessableEntity<InvoiceNotTransmittedResponse>>>
{
	private readonly IDebitNoteService _debitNoteService = debitNoteService;
	private readonly IInvoiceServiceProvider _invoiceServiceProvider = invoiceServiceProvider;


	public override void Configure()
	{
		this.Post("/debit-notes");
		this.Permissions(Permission.SalesWrite);
	}

	public override async Task<Results<Created<DebitNoteResponse>, NotFound, UnprocessableEntity<InvoiceNotTransmittedResponse>>> ExecuteAsync(CreateDebitNoteRequest request, CancellationToken cancellationToken)
	{
		if (await _invoiceServiceProvider.GetInvoiceAync(request.InvoiceNumber, cancellationToken) is not Invoice invoice) return TypedResults.NotFound();

		var items = request.Items.Select(line => (line.Description, line.Amount, line.VatRatePercent)).ToList();

		try
		{
			var debitNote = await _debitNoteService.CreateAsync(invoice, request.Reason, items, DateTime.Now, cancellationToken);
			return TypedResults.Created($"/api/debit-notes/{debitNote.Number}", DebitNoteResponse.From(debitNote));
		}
		catch (InvalidOperationException ex)
		{
			return TypedResults.UnprocessableEntity(new InvoiceNotTransmittedResponse(ex.Message));
		}
	}
}
