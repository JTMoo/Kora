using System.Globalization;

namespace StockManagement.Sales.Core.Contracts;


/// <summary>CSV serialization for the Marangatu IVA book export (#123), split out for unit coverage (#170).</summary>
public static class IvaBookCsvWriter
{
	public static string BuildCsv(IReadOnlyList<IvaBookRow> rows)
	{
		var lines = new List<string> { "TipoComprobante,Numero,Fecha,RucCliente,NombreCliente,Gravado10,Iva10,Gravado5,Iva5,Exento,Total" };
		lines.AddRange(rows.Select(row => string.Join(",",
			row.DocumentTypeCode, CsvField(row.Number), row.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), CsvField(row.CustomerRuc), CsvField(row.CustomerName),
			Amount(row.Taxed10), Amount(row.Vat10), Amount(row.Taxed5), Amount(row.Vat5), Amount(row.Exempt), Amount(row.Total))));
		return string.Join("\r\n", lines) + "\r\n";
	}

	public static string CsvField(string value)
	{
		return value.IndexOfAny([',', '"', '\r', '\n']) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";
	}

	private static string Amount(decimal value)
	{
		return value.ToString(CultureInfo.InvariantCulture);
	}
}
