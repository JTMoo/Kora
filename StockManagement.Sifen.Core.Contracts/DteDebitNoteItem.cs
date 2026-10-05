namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// One charge line of a <see cref="DteDebitNoteData"/> (#184). <see cref="Amount"/> is the VAT-inclusive line
/// total, same convention as <see cref="DteItem.UnitPrice"/> for a single unit.
/// </summary>
public sealed record DteDebitNoteItem(string Description, decimal Amount, int VatRatePercent);
