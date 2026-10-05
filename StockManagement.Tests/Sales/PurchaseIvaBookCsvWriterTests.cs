using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class PurchaseIvaBookCsvWriterTests
{
	[TestMethod]
	public void BuildCsv_NoRows_HeaderOnly()
	{
		// Act
		var csv = PurchaseIvaBookCsvWriter.BuildCsv([]);

		// Assert
		Assert.AreEqual("TipoComprobante,Numero,Fecha,RucProveedor,NombreProveedor,Gravado10,Iva10,Gravado5,Iva5,Exento,Total\r\n", csv);
	}

	[TestMethod]
	public void BuildCsv_PlainRow_FormatsDateAndAmounts()
	{
		// Arrange
		var row = new PurchaseIvaBookRow("1", "PRF-001", new DateTime(2026, 9, 1), "", "Globex SA", 1000m, 100m, 0m, 0m, 0m, 1100m);

		// Act
		var csv = PurchaseIvaBookCsvWriter.BuildCsv([row]);

		// Assert
		Assert.AreEqual(
			"TipoComprobante,Numero,Fecha,RucProveedor,NombreProveedor,Gravado10,Iva10,Gravado5,Iva5,Exento,Total\r\n" +
			"1,PRF-001,01/09/2026,,Globex SA,1000,100,0,0,0,1100\r\n",
			csv);
	}

	[TestMethod]
	public void BuildCsv_SupplierNameWithComma_DoesNotShiftColumns()
	{
		// Arrange
		var row = new PurchaseIvaBookRow("1", "PRF-002", new DateTime(2026, 9, 2), "", "Doe, Jane", 0m, 0m, 0m, 0m, 500m, 500m);

		// Act
		var csv = PurchaseIvaBookCsvWriter.BuildCsv([row]);
		var dataLine = csv.Split("\r\n")[1];

		// Assert
		Assert.AreEqual("1,PRF-002,02/09/2026,,\"Doe, Jane\",0,0,0,0,500,500", dataLine);
	}
}
