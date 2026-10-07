using StockManagement.Kernel.Model.Types;

namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// Builds the KuDE (printable representation of a DTE) as a self-contained HTML page - no PDF library, the
/// browser's own print-to-PDF covers that (#183, ADR-0035). Never persisted, same as the DTE XML itself.
/// </summary>
public interface IKudeHtmlBuilder
{
	/// <param name="data">Same data <see cref="IDteXmlBuilder.BuildInvoice"/> consumes; <see cref="DteInvoiceData.Cdc"/> must already be assigned.</param>
	/// <param name="qrDataUri">A <c>data:image/png;base64,...</c> URI, see <see cref="IKudeQrCodeGenerator"/>.</param>
	/// <param name="format">Layout to print in (#220); defaults to <see cref="KudeFormat.A4"/>.</param>
	/// <param name="paperWidthMm">Paper width for <see cref="KudeFormat.Ticket"/>, in mm; ignored for <see cref="KudeFormat.A4"/>.</param>
	string BuildInvoice(DteInvoiceData data, string qrDataUri, KudeFormat format = KudeFormat.A4, int paperWidthMm = 80);

	/// <param name="data">Same data <see cref="IDteXmlBuilder.BuildRemision"/> consumes; <see cref="DteRemisionData.Cdc"/> must already be assigned.</param>
	/// <param name="qrDataUri">A <c>data:image/png;base64,...</c> URI, see <see cref="IKudeQrCodeGenerator"/>.</param>
	string BuildRemision(DteRemisionData data, string qrDataUri);
}
