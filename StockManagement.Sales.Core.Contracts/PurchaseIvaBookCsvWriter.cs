using System.Globalization;

namespace StockManagement.Sales.Core.Contracts;


/// <summary>CSV serialization for the Hechauka purchases IVA book export (#186); counterpart to <see cref="IvaBookCsvWriter"/>.</summary>
public static class PurchaseIvaBookCsvWriter
{
	public static string BuildCsv(IReadOnlyList<PurchaseIvaBookRow> rows)
	{
		var lines = new List<string> { "TipoComprobante,Numero,Fecha,RucProveedor,NombreProveedor,Gravado10,Iva10,Gravado5,Iva5,Exento,Total" };
		lines.AddRange(rows.Select(row => string.Join(",",
			row.DocumentTypeCode, IvaBookCsvWriter.CsvField(row.Number), row.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
			IvaBookCsvWriter.CsvField(row.SupplierRuc), IvaBookCsvWriter.CsvField(row.SupplierName),
			Amount(row.Taxed10), Amount(row.Vat10), Amount(row.Taxed5), Amount(row.Vat5), Amount(row.Exempt), Amount(row.Total))));
		return string.Join("\r\n", lines) + "\r\n";
	}

	private static string Amount(decimal value)
	{
		return value.ToString(CultureInfo.InvariantCulture);
	}
}
