namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// Everything <see cref="IDteXmlBuilder"/> needs to build a Nota de Débito Electrónica DE (#184).
/// </summary>
/// <param name="OriginatingInvoiceCdc">
/// CDC of the invoice this debit note increases - emitted as <c>gCamDEAsoc</c>/<c>dCdCDERef</c>, the "iTiDE + reference" link
/// </param>
public sealed record DteDebitNoteData(
	string Cdc,
	DteEmisor Emisor,
	DteReceptor Receptor,
	DateTime IssueDate,
	long DocumentNumber,
	string OriginatingInvoiceCdc,
	IReadOnlyList<DteDebitNoteItem> Items,
	int CurrencyDecimalDigits);
