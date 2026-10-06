using StockManagement.Kernel.Model.Types;
using StockManagement.Sifen.Core;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests.Sifen;


[TestClass]
public sealed class KudeHtmlBuilderTests
{
	private static readonly string ValidCdc = new('1', 44);

	private static readonly DteEmisor Emisor = new(
		RucBase: "1946520",
		RucCheckDigit: 3,
		RazonSocial: "Acme S.A.",
		EstablishmentCode: "001",
		PointOfSaleCode: "001",
		EstablishmentAddress: "Avda. Mcal. López 1234",
		TimbradoNumber: "12345678",
		TimbradoValidSince: new DateOnly(2026, 1, 1));

	[TestMethod]
	public void BuildInvoice_ValidData_ContainsCdcAndItemAndQr()
	{
		// Arrange
		var data = new DteInvoiceData(
			ValidCdc, Emisor, new DteReceptor("Juan Perez", "80012345", 6, null),
			new DateTime(2026, 10, 3, 9, 30, 0), 123,
			[new DteItem("SKU-1", "Widget <script>", 2, 1000, 10)], CurrencyDecimalDigits: 0);

		// Act
		var html = new KudeHtmlBuilder().BuildInvoice(data, "data:image/png;base64,ABC");

		// Assert
		StringAssert.Contains(html, "CDC: 1111 1111");
		StringAssert.Contains(html, "SKU-1");
		StringAssert.Contains(html, "data:image/png;base64,ABC");
		StringAssert.Contains(html, "Acme S.A.");
		Assert.IsFalse(html.Contains("<script>"), "Item description must be HTML-encoded.");
	}

	[TestMethod]
	public void BuildInvoice_TicketFormat_UsesPaperWidth()
	{
		// Arrange
		var data = new DteInvoiceData(
			ValidCdc, Emisor, new DteReceptor("Juan Perez", "80012345", 6, null),
			new DateTime(2026, 10, 3, 9, 30, 0), 123,
			[new DteItem("SKU-1", "Widget", 2, 1000, 10)], CurrencyDecimalDigits: 0);

		// Act
		var html = new KudeHtmlBuilder().BuildInvoice(data, "data:image/png;base64,ABC", KudeFormat.Ticket, 58);

		// Assert
		StringAssert.Contains(html, "width: 58mm");
	}

	[TestMethod]
	public void BuildRemision_ValidData_ContainsCdcAndItem()
	{
		// Arrange
		var data = new DteRemisionData(
			ValidCdc, Emisor, new DteReceptor("Juan Perez", "80012345", 6, null),
			new DateTime(2026, 10, 3, 9, 30, 0), 123, RemissionReason.TrasladoEntreLocales,
			"Avda. España 500", [new DteRemisionItem("SKU-1", "Widget", 2)]);

		// Act
		var html = new KudeHtmlBuilder().BuildRemision(data, "data:image/png;base64,ABC");

		// Assert
		StringAssert.Contains(html, "CDC: 1111 1111");
		StringAssert.Contains(html, "SKU-1");
		StringAssert.Contains(html, "Destino: Avda.");
	}
}
