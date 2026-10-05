using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core;

namespace StockManagement.Tests.Sifen;


[TestClass]
public sealed class KudeDataMapperTests
{
	private static readonly CompanySettings Settings = new(
		CompanyName: "Acme S.A.", TaxId: "", Currency: "PYG", VatRatePercent: 10m, PaymentTermInDays: 30,
		FirstInvoiceNumber: 1, FirstCustomerId: 1001, CurrencyDecimalDigits: 0,
		Ruc: "1946520-3", TimbradoNumber: "12345678", TimbradoValidFrom: new DateTime(2026, 1, 1),
		EstablishmentCode: "001", PointOfSaleCode: "001", EstablishmentAddress: "Avda. Mcal. López 1234");

	private static readonly string ValidCdc = "01" + "01946520" + "3" + "001" + "001" + "0000123" + "1" + "20261003" + "1" + "123456789" + "0";

	[TestMethod]
	public void ToInvoiceData_NoCdc_Throws()
	{
		// Arrange
		var invoice = new Invoice { Customer = new Customer(), Items = [] };

		// Act / Assert
		Assert.ThrowsException<InvalidOperationException>(() => KudeDataMapper.ToInvoiceData(invoice, Settings));
	}

	[TestMethod]
	public void ToInvoiceData_CdcSet_UsesExistingCdcAndParsesDocumentNumber()
	{
		// Arrange
		var customer = new Customer { Name = "Juan", Lastname = "Perez", IdentificationNumber = "80012345" };
		var item = new ShoppingCartItem(new StockItem("Widget", code: "SKU-1", amount: 10, price: 1000) { VatRatePercent = 10m }) { Amount = 2 };
		var invoice = new Invoice { Customer = customer, Items = [item], Cdc = ValidCdc, Date = new DateTime(2026, 10, 3, 9, 0, 0) };

		// Act
		var data = KudeDataMapper.ToInvoiceData(invoice, Settings);

		// Assert
		Assert.AreEqual(ValidCdc, data.Cdc);
		Assert.AreEqual(123, data.DocumentNumber);
		Assert.AreEqual(1, data.Items.Count);
		Assert.AreEqual("SKU-1", data.Items[0].Code);
		Assert.AreEqual("Acme S.A.", data.Emisor.RazonSocial);
		Assert.AreEqual("Avda. Mcal. López 1234", data.Emisor.EstablishmentAddress);
	}

	[TestMethod]
	public void ToRemisionData_NoCdc_Throws()
	{
		// Arrange
		var remissionNote = new RemissionNote { Customer = new Customer() };

		// Act / Assert
		Assert.ThrowsException<InvalidOperationException>(() => KudeDataMapper.ToRemisionData(remissionNote, Settings));
	}

	[TestMethod]
	public void ToRemisionData_CdcSet_UsesExistingCdcAndParsesDocumentNumber()
	{
		// Arrange
		var customer = new Customer { Name = "Juan", Lastname = "Perez", IdentificationNumber = "80012345" };
		var item = new RemissionNoteItem(new StockItem("Widget", code: "SKU-1", amount: 10, price: 1000)) { Amount = 2 };
		var remissionNote = new RemissionNote
		{
			Customer = customer,
			Items = [item],
			Cdc = ValidCdc,
			Date = new DateTime(2026, 10, 3, 9, 0, 0),
			Reason = RemissionReason.TrasladoEntreLocales,
			DestinationAddress = "Avda. España 500"
		};

		// Act
		var data = KudeDataMapper.ToRemisionData(remissionNote, Settings);

		// Assert
		Assert.AreEqual(ValidCdc, data.Cdc);
		Assert.AreEqual(123, data.DocumentNumber);
		Assert.AreEqual(RemissionReason.TrasladoEntreLocales, data.Reason);
		Assert.AreEqual("Avda. España 500", data.DestinationAddress);
	}
}
