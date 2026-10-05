using System.Xml.Linq;
using StockManagement.Sifen.Core;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests.Sifen;


[TestClass]
public sealed class DteXmlBuilderDebitNoteTests
{
	private static readonly XNamespace Ns = "http://ekuatia.set.gov.py/sifen/xsd";
	private static readonly string ValidCdc = new('1', 44);
	private static readonly string OriginatingInvoiceCdc = new('2', 44);

	private static readonly DteEmisor Emisor = new(
		RucBase: "1946520",
		RucCheckDigit: 3,
		RazonSocial: "Acme S.A.",
		EstablishmentCode: "001",
		PointOfSaleCode: "001",
		EstablishmentAddress: "Avda. Mcal. López 1234",
		TimbradoNumber: "12345678",
		TimbradoValidSince: new DateOnly(2026, 1, 1));

	private static DteDebitNoteData BuildData(IReadOnlyList<DteDebitNoteItem>? items = null) => new(
		Cdc: ValidCdc,
		Emisor: Emisor,
		Receptor: new DteReceptor("Juan Perez", "80012345", 6, null),
		IssueDate: new DateTime(2026, 10, 4, 9, 30, 0),
		DocumentNumber: 51,
		OriginatingInvoiceCdc: OriginatingInvoiceCdc,
		Items: items ?? [new DteDebitNoteItem("Intereses por mora", 11000m, 10)],
		CurrencyDecimalDigits: 0);

	[TestMethod]
	public void BuildDebitNote_ValidData_SetsDeIdToDePrefixedCdc()
	{
		// Act
		var doc = new DteXmlBuilder().BuildDebitNote(BuildData());

		// Assert
		var de = doc.Root!.Element(Ns + "DE")!;
		Assert.AreEqual("DE" + ValidCdc, de.Attribute("Id")!.Value);
	}

	[TestMethod]
	public void BuildDebitNote_ValidData_SetsITiDEToDebitNoteCode()
	{
		// Act
		var doc = new DteXmlBuilder().BuildDebitNote(BuildData());

		// Assert
		var timb = doc.Descendants(Ns + "gTimb").Single();
		Assert.AreEqual(((int)SifenDocumentType.NotaDeDebitoElectronica).ToString(), timb.Element(Ns + "iTiDE")!.Value);
	}

	[TestMethod]
	public void BuildDebitNote_ValidData_ReferencesOriginatingInvoiceCdc()
	{
		// Act
		var doc = new DteXmlBuilder().BuildDebitNote(BuildData());

		// Assert
		var camDEAsoc = doc.Descendants(Ns + "gCamDEAsoc").Single();
		Assert.AreEqual("1", camDEAsoc.Element(Ns + "iTipDocAso")!.Value);
		Assert.AreEqual(OriginatingInvoiceCdc, camDEAsoc.Element(Ns + "dCdCDERef")!.Value);
	}

	[TestMethod]
	public void BuildDebitNote_MixedVatRates_SplitsTotalsPerRate()
	{
		// Act
		var doc = new DteXmlBuilder().BuildDebitNote(BuildData(items: [
			new DteDebitNoteItem("10% charge", 11000m, 10), // VAT = 1000
			new DteDebitNoteItem("5% charge", 10500m, 5),   // VAT = 500
			new DteDebitNoteItem("exempt charge", 2000m, 0),
		]));

		// Assert
		var totSub = doc.Descendants(Ns + "gTotSub").Single();
		Assert.AreEqual("11000", totSub.Element(Ns + "dSub10")!.Value);
		Assert.AreEqual("10500", totSub.Element(Ns + "dSub5")!.Value);
		Assert.AreEqual("2000", totSub.Element(Ns + "dSubExe")!.Value);
		Assert.AreEqual("1000", totSub.Element(Ns + "dIVA10")!.Value);
		Assert.AreEqual("500", totSub.Element(Ns + "dIVA5")!.Value);
		Assert.AreEqual("1500", totSub.Element(Ns + "dTotIVA")!.Value);
		Assert.AreEqual("23500", totSub.Element(Ns + "dTotOpe")!.Value);
	}

	[TestMethod]
	public void BuildDebitNote_NoItems_Throws()
	{
		// Act + Assert
		Assert.ThrowsException<ArgumentException>(() => new DteXmlBuilder().BuildDebitNote(BuildData(items: [])));
	}

	[TestMethod]
	public void BuildDebitNote_CdcNot44Digits_Throws()
	{
		// Act + Assert
		Assert.ThrowsException<ArgumentException>(() => new DteXmlBuilder().BuildDebitNote(BuildData() with { Cdc = "123" }));
	}
}
