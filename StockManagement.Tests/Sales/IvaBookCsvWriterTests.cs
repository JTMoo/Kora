using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class IvaBookCsvWriterTests
{
	[TestMethod]
	public void BuildCsv_NoRows_HeaderOnly()
	{
		// Act
		var csv = IvaBookCsvWriter.BuildCsv([]);

		// Assert
		Assert.AreEqual("TipoComprobante,Numero,Fecha,RucCliente,NombreCliente,Gravado10,Iva10,Gravado5,Iva5,Exento,Total\r\n", csv);
	}

	[TestMethod]
	public void BuildCsv_PlainRow_FormatsDateAndAmounts()
	{
		// Arrange
		var row = new IvaBookRow("1", "001-001-0000001", new DateTime(2026, 9, 1), "80012345-6", "Acme SA", 1000m, 100m, 0m, 0m, 0m, 1100m);

		// Act
		var csv = IvaBookCsvWriter.BuildCsv([row]);

		// Assert
		Assert.AreEqual(
			"TipoComprobante,Numero,Fecha,RucCliente,NombreCliente,Gravado10,Iva10,Gravado5,Iva5,Exento,Total\r\n" +
			"1,001-001-0000001,01/09/2026,80012345-6,Acme SA,1000,100,0,0,0,1100\r\n",
			csv);
	}

	[TestMethod]
	public void CsvField_ValueWithComma_IsQuoted()
	{
		// Act
		var field = IvaBookCsvWriter.CsvField("Doe, John");

		// Assert
		Assert.AreEqual("\"Doe, John\"", field);
	}

	[TestMethod]
	public void CsvField_ValueWithQuote_IsQuotedAndEscaped()
	{
		// Act
		var field = IvaBookCsvWriter.CsvField("5\" Pipe Co");

		// Assert
		Assert.AreEqual("\"5\"\" Pipe Co\"", field);
	}

	[TestMethod]
	public void CsvField_PlainValue_IsNotQuoted()
	{
		// Act
		var field = IvaBookCsvWriter.CsvField("Acme SA");

		// Assert
		Assert.AreEqual("Acme SA", field);
	}

	[TestMethod]
	public void BuildCsv_CustomerNameWithComma_DoesNotShiftColumns()
	{
		// Arrange (#170): the exact bug this coverage closes
		var row = new IvaBookRow("1", "001-001-0000002", new DateTime(2026, 9, 2), "80012345-6", "Doe, Jane", 0m, 0m, 0m, 0m, 500m, 500m);

		// Act
		var csv = IvaBookCsvWriter.BuildCsv([row]);
		var dataLine = csv.Split("\r\n")[1];

		// Assert
		Assert.AreEqual("1,001-001-0000002,02/09/2026,80012345-6,\"Doe, Jane\",0,0,0,0,500,500", dataLine);
	}
}
