using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.DebitNotes;


public sealed record DebitNoteListResponse(IReadOnlyList<DebitNoteResponse> Items);


public class ListDebitNotesEndpoint(IDebitNoteServiceProvider debitNoteServiceProvider) : EndpointWithoutRequest<DebitNoteListResponse>
{
	private readonly IDebitNoteServiceProvider _debitNoteServiceProvider = debitNoteServiceProvider;


	public override void Configure()
	{
		this.Get("/debit-notes");
		this.Permissions(Permission.SalesRead);
	}

	public override async Task<DebitNoteListResponse> ExecuteAsync(CancellationToken cancellationToken)
	{
		var debitNotes = await _debitNoteServiceProvider.GetDebitNotesAsync(cancellationToken);
		return new(debitNotes.Select(DebitNoteResponse.From).ToList());
	}
}
