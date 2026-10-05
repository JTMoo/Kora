namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// Everything <see cref="IDteXmlBuilder"/> needs to build a Cancelación event (<c>rEve</c>/<c>gCanc</c>) (#206).
/// </summary>
/// <param name="EventId">Locally generated event identifier, distinct from any document <c>Cdc</c></param>
/// <param name="TargetCdc">CDC of the DE being cancelled</param>
public sealed record DteCancellationEventData(
	string EventId,
	DteEmisor Emisor,
	DateTime SignatureDate,
	string TargetCdc,
	string Reason);
