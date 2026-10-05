using System.Xml.Linq;

namespace StockManagement.Sifen.Core.Contracts;


public interface IDteXmlBuilder
{
	/// <summary>
	/// Builds the unsigned <c>rDE</c> XML document for a Factura Electrónica.
	/// </summary>
	XDocument BuildInvoice(DteInvoiceData data);

	/// <summary>
	/// Builds the unsigned <c>rDE</c> XML document for a Nota de Remisión Electrónica (#162).
	/// </summary>
	XDocument BuildRemision(DteRemisionData data);

	/// <summary>
	/// Builds the unsigned <c>rDE</c> XML document for a Nota de Débito Electrónica (#184).
	/// </summary>
	XDocument BuildDebitNote(DteDebitNoteData data);

	/// <summary>
	/// Builds the unsigned <c>rEnviEvento</c> XML document for a Cancelación event (#206).
	/// </summary>
	XDocument BuildCancellationEvent(DteCancellationEventData data);

	/// <summary>
	/// Builds the unsigned <c>rEnviEvento</c> XML document for an Inutilización event (#206).
	/// </summary>
	XDocument BuildInutilizacionEvent(DteInutilizacionEventData data);
}
