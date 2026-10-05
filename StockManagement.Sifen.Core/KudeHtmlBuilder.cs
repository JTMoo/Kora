using System.Globalization;
using System.Net;
using System.Text;
using StockManagement.Kernel.Util;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


/// <inheritdoc cref="IKudeHtmlBuilder"/>
public sealed class KudeHtmlBuilder : IKudeHtmlBuilder
{
	public string BuildInvoice(DteInvoiceData data, string qrDataUri)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentException.ThrowIfNullOrEmpty(qrDataUri);

		var digits = data.CurrencyDecimalDigits;
		var rows = new StringBuilder();
		decimal total = 0, iva = 0;

		foreach (var item in data.Items)
		{
			var lineTotal = Math.Round(item.Quantity * item.UnitPrice, digits, MidpointRounding.AwayFromZero);
			var vatAmount = VatSplit.VatShare(lineTotal, item.VatRatePercent, digits);
			total += lineTotal;
			iva += vatAmount;

			rows.Append("<tr>")
				.Append("<td>").Append(Enc(item.Code)).Append("</td>")
				.Append("<td>").Append(Enc(item.Description)).Append("</td>")
				.Append("<td class=\"num\">").Append(item.Quantity.ToString(CultureInfo.InvariantCulture)).Append("</td>")
				.Append("<td class=\"num\">").Append(FormatAmount(item.UnitPrice, digits)).Append("</td>")
				.Append("<td class=\"num\">").Append(item.VatRatePercent.ToString(CultureInfo.InvariantCulture)).Append("%</td>")
				.Append("<td class=\"num\">").Append(FormatAmount(lineTotal, digits)).Append("</td>")
				.Append("</tr>");
		}

		var body = $"""
			<div class="parties">
				<div><strong>{Enc(data.Receptor.Name)}</strong><br/>{ReceptorId(data.Receptor)}</div>
				<div>Fecha emisión: {data.IssueDate:dd/MM/yyyy HH:mm}</div>
			</div>
			<table>
				<thead><tr><th>Código</th><th>Descripción</th><th class="num">Cant.</th><th class="num">P. Unit.</th><th class="num">IVA</th><th class="num">Total</th></tr></thead>
				<tbody>{rows}</tbody>
			</table>
			<div class="totals">
				<div>Total IVA: {FormatAmount(iva, digits)}</div>
				<div><strong>Total: {FormatAmount(total, digits)}</strong></div>
			</div>
			""";

		return Wrap("Factura Electrónica", data.Cdc, data.Emisor, body, qrDataUri);
	}

	public string BuildRemision(DteRemisionData data, string qrDataUri)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentException.ThrowIfNullOrEmpty(qrDataUri);

		var rows = new StringBuilder();
		foreach (var item in data.Items)
		{
			rows.Append("<tr>")
				.Append("<td>").Append(Enc(item.Code)).Append("</td>")
				.Append("<td>").Append(Enc(item.Description)).Append("</td>")
				.Append("<td class=\"num\">").Append(item.Quantity.ToString(CultureInfo.InvariantCulture)).Append("</td>")
				.Append("</tr>");
		}

		var body = $"""
			<div class="parties">
				<div><strong>{Enc(data.Receptor.Name)}</strong><br/>{ReceptorId(data.Receptor)}</div>
				<div>Fecha emisión: {data.IssueDate:dd/MM/yyyy HH:mm}</div>
			</div>
			<div>Motivo: {Enc(data.Reason.ToString())}</div>
			<div>Destino: {Enc(data.DestinationAddress)}</div>
			<table>
				<thead><tr><th>Código</th><th>Descripción</th><th class="num">Cant.</th></tr></thead>
				<tbody>{rows}</tbody>
			</table>
			""";

		return Wrap("Nota de Remisión Electrónica", data.Cdc, data.Emisor, body, qrDataUri);
	}

	private const string Css = """
		body { font-family: Arial, sans-serif; font-size: 12px; color: #111; margin: 24px; }
		h1 { font-size: 16px; margin: 0 0 4px; }
		.header { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #111; padding-bottom: 8px; margin-bottom: 8px; }
		.header img { width: 110px; height: 110px; }
		.parties { display: flex; justify-content: space-between; margin-bottom: 8px; }
		table { width: 100%; border-collapse: collapse; margin-bottom: 8px; }
		th, td { border: 1px solid #999; padding: 4px 6px; text-align: left; }
		.num { text-align: right; }
		.totals { display: flex; flex-direction: column; align-items: flex-end; gap: 2px; }
		.cdc { font-family: monospace; word-break: break-all; }
		@media print { body { margin: 0; } }
		""";

	private static string Wrap(string docTypeLabel, string cdc, DteEmisor emisor, string bodyHtml, string qrDataUri)
	{
		var cdcGrouped = string.Join(" ", Enumerable.Range(0, cdc.Length / 4 + (cdc.Length % 4 == 0 ? 0 : 1))
			.Select(i => cdc.Substring(i * 4, Math.Min(4, cdc.Length - i * 4))));

		return $"""
			<!doctype html>
			<html lang="es">
			<head>
			<meta charset="utf-8"/>
			<title>{Enc(docTypeLabel)}</title>
			<style>
			{Css}
			</style>
			</head>
			<body>
				<div class="header">
					<div>
						<h1>{Enc(emisor.RazonSocial)}</h1>
						<div>RUC: {emisor.RucBase}-{emisor.RucCheckDigit}</div>
						<div>{Enc(emisor.EstablishmentAddress)}</div>
						<div><strong>{Enc(docTypeLabel)}</strong></div>
						<div>Timbrado: {Enc(emisor.TimbradoNumber)}</div>
						<div>{emisor.EstablishmentCode}-{emisor.PointOfSaleCode}</div>
					</div>
					<img src="{qrDataUri}" alt="QR SIFEN"/>
				</div>
				<div class="cdc">CDC: {cdcGrouped}</div>
				{bodyHtml}
			</body>
			</html>
			""";
	}

	private static string ReceptorId(DteReceptor receptor)
	{
		return receptor.RucBase is not null
			? $"RUC: {receptor.RucBase}-{receptor.RucCheckDigit}"
			: $"CI/Doc: {Enc(receptor.DocumentNumber ?? "")}";
	}

	private static string FormatAmount(decimal amount, int digits)
	{
		return amount.ToString("N" + digits, CultureInfo.InvariantCulture);
	}

	private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
