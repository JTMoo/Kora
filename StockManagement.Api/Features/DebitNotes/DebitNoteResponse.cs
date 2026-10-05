using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Features.DebitNotes;


/// <summary>
/// Stored debit note (#184)
/// </summary>
public sealed record DebitNoteResponse(string Number, DateTime Date, string Reason, string InvoiceNumber, decimal Total, decimal Tax, string Cdc, TransmissionStatus TransmissionStatus, IReadOnlyList<DebitNoteLineResponse> Lines)
{
	public static DebitNoteResponse From(DebitNote debitNote)
	{
		var lines = (debitNote.Items ?? []).Select(item => new DebitNoteLineResponse(item.Description, item.Amount, item.VatRatePercent)).ToList();
		return new(debitNote.Number, debitNote.Date, debitNote.Reason, debitNote.Invoice.Number, debitNote.Total, debitNote.Tax, debitNote.Cdc, debitNote.TransmissionStatus, lines);
	}
}


public sealed record DebitNoteLineResponse(string Description, decimal Amount, int VatRatePercent);
