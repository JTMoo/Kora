namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// Builds the KuDE (printable representation of a DTE) as a self-contained HTML page - no PDF library, the
/// browser's own print-to-PDF covers that (#183, ADR-0035). Never persisted, same as the DTE XML itself.
/// </summary>
public interface IKudeHtmlBuilder
{
	/// <param name="data">Same data <see cref="IDteXmlBuilder.BuildInvoice"/> consumes; <see cref="DteInvoiceData.Cdc"/> must already be assigned.</param>
	/// <param name="qrDataUri">A <c>data:image/png;base64,...</c> URI, see <see cref="IKudeQrCodeGenerator"/>.</param>
	string BuildInvoice(DteInvoiceData data, string qrDataUri);

	/// <param name="data">Same data <see cref="IDteXmlBuilder.BuildRemision"/> consumes; <see cref="DteRemisionData.Cdc"/> must already be assigned.</param>
	/// <param name="qrDataUri">A <c>data:image/png;base64,...</c> URI, see <see cref="IKudeQrCodeGenerator"/>.</param>
	string BuildRemision(DteRemisionData data, string qrDataUri);
}
