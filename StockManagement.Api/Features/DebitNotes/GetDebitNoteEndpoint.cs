using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Api.Features.DebitNotes;


public sealed record GetDebitNoteRequest(string Number);


public class GetDebitNoteEndpoint(IDebitNoteServiceProvider debitNoteServiceProvider) : Endpoint<GetDebitNoteRequest, Results<Ok<DebitNoteResponse>, NotFound>>
{
	private readonly IDebitNoteServiceProvider _debitNoteServiceProvider = debitNoteServiceProvider;


	public override void Configure()
	{
		this.Get("/debit-notes/{Number}");
		this.Permissions(Permission.SalesRead);
	}

	public override async Task<Results<Ok<DebitNoteResponse>, NotFound>> ExecuteAsync(GetDebitNoteRequest request, CancellationToken cancellationToken)
	{
		if (await _debitNoteServiceProvider.GetDebitNoteAsync(request.Number, cancellationToken) is not DebitNote debitNote) return TypedResults.NotFound();

		return TypedResults.Ok(DebitNoteResponse.From(debitNote));
	}
}
