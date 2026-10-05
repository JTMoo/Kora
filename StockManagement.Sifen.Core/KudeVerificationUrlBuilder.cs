using System.Globalization;
using System.Text;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


/// <inheritdoc cref="IKudeVerificationUrlBuilder"/>
public sealed class KudeVerificationUrlBuilder : IKudeVerificationUrlBuilder
{
	private const string BaseUrl = "https://ekuatia.set.gov.py/consultas-kude/qr";

	public string BuildForInvoice(DteInvoiceData data)
	{
		ArgumentNullException.ThrowIfNull(data);

		decimal total = 0, iva = 0;
		foreach (var item in data.Items)
		{
			var lineTotal = Math.Round(item.Quantity * item.UnitPrice, data.CurrencyDecimalDigits, MidpointRounding.AwayFromZero);
			total += lineTotal;
			iva += Math.Round(lineTotal * item.VatRatePercent / (100 + item.VatRatePercent), data.CurrencyDecimalDigits, MidpointRounding.AwayFromZero);
		}

		return Build(data.Cdc, data.IssueDate, data.Receptor.RucBase ?? data.Receptor.DocumentNumber ?? "", total, iva, data.Items.Count);
	}

	public string BuildForRemision(DteRemisionData data)
	{
		ArgumentNullException.ThrowIfNull(data);

		// A remission note has no price/VAT (#162) - DNIT's dTotGralOpe/dTotIVA params are meaningless here.
		return Build(data.Cdc, data.IssueDate, data.Receptor.RucBase ?? data.Receptor.DocumentNumber ?? "", total: null, iva: null, data.Items.Count);
	}

	private static string Build(string cdc, DateTime issueDate, string receptorId, decimal? total, decimal? iva, int itemCount)
	{
		var query = new StringBuilder()
			.Append("nVersion=150")
			.Append("&Id=").Append(Uri.EscapeDataString(cdc))
			.Append("&dFeEmiDE=").Append(Uri.EscapeDataString(issueDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)))
			.Append("&dRucRec=").Append(Uri.EscapeDataString(receptorId));
		if (total is not null) query.Append("&dTotGralOpe=").Append(Uri.EscapeDataString(total.Value.ToString(CultureInfo.InvariantCulture)));
		if (iva is not null) query.Append("&dTotIVA=").Append(Uri.EscapeDataString(iva.Value.ToString(CultureInfo.InvariantCulture)));
		query.Append("&cItems=").Append(itemCount.ToString(CultureInfo.InvariantCulture));

		return $"{BaseUrl}?{query}";
	}
}
