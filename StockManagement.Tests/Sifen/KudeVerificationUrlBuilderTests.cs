using StockManagement.Kernel.Model.Types;
using StockManagement.Sifen.Core;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests.Sifen;


[TestClass]
public sealed class KudeVerificationUrlBuilderTests
{
	private static readonly string ValidCdc = new('1', 44);

	private static readonly DteEmisor Emisor = new(
		"1946520", 3, "Acme S.A.", "001", "001", "Avda. Mcal. López 1234", "12345678", new DateOnly(2026, 1, 1));

	[TestMethod]
	public void BuildForInvoice_ValidData_IncludesCdcAndTotals()
	{
		// Arrange
		var data = new DteInvoiceData(
			ValidCdc, Emisor, new DteReceptor("Juan Perez", "80012345", 6, null),
			new DateTime(2026, 10, 3, 9, 30, 0), 123,
			[new DteItem("SKU-1", "Widget", 2, 1100, 10)], CurrencyDecimalDigits: 0);

		// Act
		var url = new KudeVerificationUrlBuilder().BuildForInvoice(data);

		// Assert
		StringAssert.StartsWith(url, "https://ekuatia.set.gov.py/consultas-kude/qr?");
		StringAssert.Contains(url, $"Id={ValidCdc}");
		StringAssert.Contains(url, "dRucRec=80012345");
		StringAssert.Contains(url, "dTotGralOpe=2200");
		StringAssert.Contains(url, "cItems=1");
	}

	[TestMethod]
	public void BuildForRemision_ValidData_OmitsAmounts()
	{
		// Arrange
		var data = new DteRemisionData(
			ValidCdc, Emisor, new DteReceptor("Juan Perez", "80012345", 6, null),
			new DateTime(2026, 10, 3, 9, 30, 0), 123, RemissionReason.TrasladoEntreLocales,
			"Avda. España 500", [new DteRemisionItem("SKU-1", "Widget", 2)]);

		// Act
		var url = new KudeVerificationUrlBuilder().BuildForRemision(data);

		// Assert
		StringAssert.Contains(url, $"Id={ValidCdc}");
		Assert.IsFalse(url.Contains("dTotGralOpe"));
		Assert.IsFalse(url.Contains("dTotIVA"));
	}
}
