namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// Everything <see cref="IDteXmlBuilder"/> needs to build an Inutilización event (<c>rEve</c>/<c>gInut</c>) (#206).
/// </summary>
/// <param name="EventId">Locally generated event identifier, distinct from any document <c>Cdc</c></param>
/// <param name="RangeStart">Sequence component, inclusive</param>
/// <param name="RangeEnd">Sequence component, inclusive</param>
public sealed record DteInutilizacionEventData(
	string EventId,
	DteEmisor Emisor,
	DateTime SignatureDate,
	SifenDocumentType DocumentType,
	int RangeStart,
	int RangeEnd,
	string Reason);
