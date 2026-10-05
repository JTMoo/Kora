using System.Globalization;
using System.Xml.Linq;
using StockManagement.Kernel.Util;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


/// <summary>
/// Builds the unsigned <c>rDE</c>/<c>DE</c> XML for a Factura Electrónica or a Nota de Remisión Electrónica (#162).
/// </summary>
/// <remarks>
/// Element/group names (<c>gOpeDE</c>, <c>gTimb</c>, <c>gDatGralOpe</c>, <c>gEmis</c>, <c>gDatRec</c>, <c>gDtipDE</c>,
/// <c>gCamItem</c>, <c>gCamIVA</c>, <c>gTotSub</c>) and field widths reproduce DNIT's publicly documented SIFEN
/// structure, covering the core groups needed for a single-item-type invoice. Optional groups (activity codes,
/// multiple currencies, transport, etc.) are not implemented. This has not been checked against the published
/// DNIT XSD in this session (no network access to dnit.gov.py) — run <see cref="DteXsdValidator"/> against the
/// real schema before any production use.
/// </remarks>
public sealed class DteXmlBuilder : IDteXmlBuilder
{
	private static readonly XNamespace Ns = "http://ekuatia.set.gov.py/sifen/xsd";

	public XDocument BuildInvoice(DteInvoiceData data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (data.Cdc.Length != 44 || !data.Cdc.All(char.IsDigit))
			throw new ArgumentException("Cdc must be 44 digits.", nameof(data));
		if (data.Items.Count == 0)
			throw new ArgumentException("An invoice needs at least one item.", nameof(data));

		// Positions per CdcGenerator's layout: [34]=iTipEmi, [34..43)=dCodSeg (9 digits), [43]=check digit.
		var emissionTypeDigit = data.Cdc[34];
		var securityCode = data.Cdc.Substring(34, 9);

		var de = new XElement(Ns + "DE",
			// "DE" + cdc, not the bare cdc: an XML Id must be a valid NCName (can't start with a digit),
			// and the CDC is all-digit — same reason DNIT's own examples prefix it this way.
			new XAttribute("Id", "DE" + data.Cdc),
			new XElement(Ns + "dDVId", data.Cdc[^1]),
			new XElement(Ns + "gOpeDE",
				new XElement(Ns + "iTipEmi", emissionTypeDigit),
				new XElement(Ns + "dCodSeg", securityCode)),
			BuildTimb(data),
			BuildDatGralOpe(data),
			BuildDtipDE(data),
			BuildTotSub(data));

		return new XDocument(
			new XDeclaration("1.0", "UTF-8", null),
			new XElement(Ns + "rDE",
				new XElement(Ns + "dVerFor", "150"),
				de));
	}

	/// <remarks>
	/// Covers the core groups for a goods-movement document: no <c>gCamIVA</c>/<c>gTotSub</c> (no price or tax on a
	/// remission note) but adds <c>gTransp</c> (motivo + destination address). The DNIT XSD for this document type
	/// has not been checked in this session (no network access to dnit.gov.py) - same unverified flag as
	/// <see cref="BuildInvoice"/>; run <see cref="DteXsdValidator"/> against the real schema before production use.
	/// </remarks>
	public XDocument BuildRemision(DteRemisionData data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (data.Cdc.Length != 44 || !data.Cdc.All(char.IsDigit))
			throw new ArgumentException("Cdc must be 44 digits.", nameof(data));
		if (data.Items.Count == 0)
			throw new ArgumentException("A remission note needs at least one item.", nameof(data));

		var emissionTypeDigit = data.Cdc[34];
		var securityCode = data.Cdc.Substring(34, 9);

		var de = new XElement(Ns + "DE",
			new XAttribute("Id", "DE" + data.Cdc),
			new XElement(Ns + "dDVId", data.Cdc[^1]),
			new XElement(Ns + "gOpeDE",
				new XElement(Ns + "iTipEmi", emissionTypeDigit),
				new XElement(Ns + "dCodSeg", securityCode)),
			BuildRemisionTimb(data),
			BuildRemisionDatGralOpe(data),
			BuildRemisionDtipDE(data),
			BuildTransp(data));

		return new XDocument(
			new XDeclaration("1.0", "UTF-8", null),
			new XElement(Ns + "rDE",
				new XElement(Ns + "dVerFor", "150"),
				de));
	}

	private XElement BuildRemisionTimb(DteRemisionData data)
	{
		var emisor = data.Emisor;
		return new XElement(Ns + "gTimb",
			new XElement(Ns + "iTiDE", (int)SifenDocumentType.NotaDeRemisionElectronica),
			new XElement(Ns + "dNumTim", emisor.TimbradoNumber),
			new XElement(Ns + "dEst", emisor.EstablishmentCode),
			new XElement(Ns + "dPunExp", emisor.PointOfSaleCode),
			new XElement(Ns + "dNumDoc", data.DocumentNumber.ToString("D7", CultureInfo.InvariantCulture)),
			new XElement(Ns + "dFeIniT", emisor.TimbradoValidSince.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
	}

	private XElement BuildRemisionDatGralOpe(DteRemisionData data)
	{
		var emisor = data.Emisor;
		var receptor = data.Receptor;
		var hasRuc = receptor.RucBase != null;

		return new XElement(Ns + "gDatGralOpe",
			new XElement(Ns + "dFeEmiDE", data.IssueDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
			new XElement(Ns + "gEmis",
				new XElement(Ns + "dRucEm", emisor.RucBase),
				new XElement(Ns + "dDVEmi", emisor.RucCheckDigit),
				new XElement(Ns + "dNomEmi", emisor.RazonSocial),
				new XElement(Ns + "gDirEmi",
					new XElement(Ns + "dDirEmi", emisor.EstablishmentAddress))),
			new XElement(Ns + "gDatRec",
				new XElement(Ns + "iNatRec", hasRuc ? 1 : 2),
				hasRuc
					? new XElement(Ns + "dRucRec", receptor.RucBase)
					: new XElement(Ns + "dNumIDRec", receptor.DocumentNumber),
				hasRuc ? new XElement(Ns + "dDVRec", receptor.RucCheckDigit) : null,
				new XElement(Ns + "dNomRec", receptor.Name)));
	}

	private XElement BuildRemisionDtipDE(DteRemisionData data)
	{
		return new XElement(Ns + "gDtipDE",
			data.Items.Select(item =>
				new XElement(Ns + "gCamItem",
					new XElement(Ns + "dCodInt", item.Code),
					new XElement(Ns + "dDesProSer", item.Description),
					new XElement(Ns + "dCantProSer", item.Quantity))));
	}

	private XElement BuildTransp(DteRemisionData data)
	{
		return new XElement(Ns + "gTransp",
			new XElement(Ns + "iMotTras", (int)data.Reason),
			new XElement(Ns + "dDirDest", data.DestinationAddress));
	}

	/// <remarks>
	/// Same groups as <see cref="BuildInvoice"/> (itemized <c>gCamItem</c>/<c>gCamIVA</c>/<c>gTotSub</c>) plus
	/// <c>gCamDEAsoc</c> linking back to the original invoice's CDC (#184). DNIT XSD not checked in this session -
	/// same unverified flag as <see cref="BuildInvoice"/>.
	/// </remarks>
	public XDocument BuildDebitNote(DteDebitNoteData data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (data.Cdc.Length != 44 || !data.Cdc.All(char.IsDigit))
			throw new ArgumentException("Cdc must be 44 digits.", nameof(data));
		if (data.Items.Count == 0)
			throw new ArgumentException("A debit note needs at least one item.", nameof(data));

		var emissionTypeDigit = data.Cdc[34];
		var securityCode = data.Cdc.Substring(34, 9);

		var de = new XElement(Ns + "DE",
			new XAttribute("Id", "DE" + data.Cdc),
			new XElement(Ns + "dDVId", data.Cdc[^1]),
			new XElement(Ns + "gOpeDE",
				new XElement(Ns + "iTipEmi", emissionTypeDigit),
				new XElement(Ns + "dCodSeg", securityCode)),
			BuildDebitNoteTimb(data),
			BuildDebitNoteDatGralOpe(data),
			BuildDebitNoteDtipDE(data),
			BuildDebitNoteCamDEAsoc(data),
			BuildDebitNoteTotSub(data));

		return new XDocument(
			new XDeclaration("1.0", "UTF-8", null),
			new XElement(Ns + "rDE",
				new XElement(Ns + "dVerFor", "150"),
				de));
	}

	private XElement BuildDebitNoteTimb(DteDebitNoteData data)
	{
		var emisor = data.Emisor;
		return new XElement(Ns + "gTimb",
			new XElement(Ns + "iTiDE", (int)SifenDocumentType.NotaDeDebitoElectronica),
			new XElement(Ns + "dNumTim", emisor.TimbradoNumber),
			new XElement(Ns + "dEst", emisor.EstablishmentCode),
			new XElement(Ns + "dPunExp", emisor.PointOfSaleCode),
			new XElement(Ns + "dNumDoc", data.DocumentNumber.ToString("D7", CultureInfo.InvariantCulture)),
			new XElement(Ns + "dFeIniT", emisor.TimbradoValidSince.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
	}

	private XElement BuildDebitNoteDatGralOpe(DteDebitNoteData data)
	{
		var emisor = data.Emisor;
		var receptor = data.Receptor;
		var hasRuc = receptor.RucBase != null;

		return new XElement(Ns + "gDatGralOpe",
			new XElement(Ns + "dFeEmiDE", data.IssueDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
			new XElement(Ns + "gEmis",
				new XElement(Ns + "dRucEm", emisor.RucBase),
				new XElement(Ns + "dDVEmi", emisor.RucCheckDigit),
				new XElement(Ns + "dNomEmi", emisor.RazonSocial),
				new XElement(Ns + "gDirEmi",
					new XElement(Ns + "dDirEmi", emisor.EstablishmentAddress))),
			new XElement(Ns + "gDatRec",
				new XElement(Ns + "iNatRec", hasRuc ? 1 : 2),
				hasRuc
					? new XElement(Ns + "dRucRec", receptor.RucBase)
					: new XElement(Ns + "dNumIDRec", receptor.DocumentNumber),
				hasRuc ? new XElement(Ns + "dDVRec", receptor.RucCheckDigit) : null,
				new XElement(Ns + "dNomRec", receptor.Name)));
	}

	private XElement BuildDebitNoteDtipDE(DteDebitNoteData data)
	{
		return new XElement(Ns + "gDtipDE",
			data.Items.Select(item =>
			{
				var vatAmount = VatSplit.VatShare(item.Amount, item.VatRatePercent, data.CurrencyDecimalDigits);
				var vatAffectation = item.VatRatePercent == 0 ? 3 : 1; // 1 = gravado, 3 = exento

				return new XElement(Ns + "gCamItem",
					new XElement(Ns + "dDesProSer", item.Description),
					new XElement(Ns + "dCantProSer", 1),
					new XElement(Ns + "gValorItem",
						new XElement(Ns + "dPUniProSer", item.Amount),
						new XElement(Ns + "dTotBruItem", item.Amount)),
					new XElement(Ns + "gCamIVA",
						new XElement(Ns + "iAfecIVA", vatAffectation),
						new XElement(Ns + "dTasaIVA", item.VatRatePercent),
						new XElement(Ns + "dBasGravIVA", item.Amount - vatAmount),
						new XElement(Ns + "dLiqIVAItem", vatAmount)));
			}));
	}

	private XElement BuildDebitNoteCamDEAsoc(DteDebitNoteData data)
	{
		return new XElement(Ns + "gCamDEAsoc",
			new XElement(Ns + "iTipDocAso", 1), // 1 = electrónico
			new XElement(Ns + "dCdCDERef", data.OriginatingInvoiceCdc));
	}

	private XElement BuildDebitNoteTotSub(DteDebitNoteData data)
	{
		var digits = data.CurrencyDecimalDigits;
		decimal exempt = 0, sub5 = 0, sub10 = 0, iva5 = 0, iva10 = 0;

		foreach (var item in data.Items)
		{
			var vatAmount = VatSplit.VatShare(item.Amount, item.VatRatePercent, digits);

			if (item.VatRatePercent == 0) exempt += item.Amount;
			else if (item.VatRatePercent == 5) { sub5 += item.Amount; iva5 += vatAmount; }
			else { sub10 += item.Amount; iva10 += vatAmount; }
		}

		var total = exempt + sub5 + sub10;

		return new XElement(Ns + "gTotSub",
			new XElement(Ns + "dSubExe", exempt),
			new XElement(Ns + "dSub5", sub5),
			new XElement(Ns + "dSub10", sub10),
			new XElement(Ns + "dTotOpe", total),
			new XElement(Ns + "dTotGralOpe", total),
			new XElement(Ns + "dIVA5", iva5),
			new XElement(Ns + "dIVA10", iva10),
			new XElement(Ns + "dTotIVA", iva5 + iva10));
	}

	private XElement BuildTimb(DteInvoiceData data)
	{
		var emisor = data.Emisor;
		return new XElement(Ns + "gTimb",
			new XElement(Ns + "iTiDE", (int)SifenDocumentType.FacturaElectronica),
			new XElement(Ns + "dNumTim", emisor.TimbradoNumber),
			new XElement(Ns + "dEst", emisor.EstablishmentCode),
			new XElement(Ns + "dPunExp", emisor.PointOfSaleCode),
			new XElement(Ns + "dNumDoc", data.DocumentNumber.ToString("D7", CultureInfo.InvariantCulture)),
			new XElement(Ns + "dFeIniT", emisor.TimbradoValidSince.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
	}

	private XElement BuildDatGralOpe(DteInvoiceData data)
	{
		var emisor = data.Emisor;
		var receptor = data.Receptor;
		var hasRuc = receptor.RucBase != null;

		return new XElement(Ns + "gDatGralOpe",
			new XElement(Ns + "dFeEmiDE", data.IssueDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
			new XElement(Ns + "gEmis",
				new XElement(Ns + "dRucEm", emisor.RucBase),
				new XElement(Ns + "dDVEmi", emisor.RucCheckDigit),
				new XElement(Ns + "dNomEmi", emisor.RazonSocial),
				new XElement(Ns + "gDirEmi",
					new XElement(Ns + "dDirEmi", emisor.EstablishmentAddress))),
			new XElement(Ns + "gDatRec",
				new XElement(Ns + "iNatRec", hasRuc ? 1 : 2),
				hasRuc
					? new XElement(Ns + "dRucRec", receptor.RucBase)
					: new XElement(Ns + "dNumIDRec", receptor.DocumentNumber),
				hasRuc ? new XElement(Ns + "dDVRec", receptor.RucCheckDigit) : null,
				new XElement(Ns + "dNomRec", receptor.Name)));
	}

	private XElement BuildDtipDE(DteInvoiceData data)
	{
		return new XElement(Ns + "gDtipDE",
			data.Items.Select(item =>
			{
				var lineTotal = Math.Round(item.Quantity * item.UnitPrice, data.CurrencyDecimalDigits, MidpointRounding.AwayFromZero);
				var vatAmount = VatSplit.VatShare(lineTotal, item.VatRatePercent, data.CurrencyDecimalDigits);
				var vatAffectation = item.VatRatePercent == 0 ? 3 : 1; // 1 = gravado, 3 = exento

				return new XElement(Ns + "gCamItem",
					new XElement(Ns + "dCodInt", item.Code),
					new XElement(Ns + "dDesProSer", item.Description),
					new XElement(Ns + "dCantProSer", item.Quantity),
					new XElement(Ns + "gValorItem",
						new XElement(Ns + "dPUniProSer", item.UnitPrice),
						new XElement(Ns + "dTotBruItem", lineTotal)),
					new XElement(Ns + "gCamIVA",
						new XElement(Ns + "iAfecIVA", vatAffectation),
						new XElement(Ns + "dTasaIVA", item.VatRatePercent),
						new XElement(Ns + "dBasGravIVA", lineTotal - vatAmount),
						new XElement(Ns + "dLiqIVAItem", vatAmount)));
			}));
	}

	private XElement BuildTotSub(DteInvoiceData data)
	{
		var digits = data.CurrencyDecimalDigits;
		decimal exempt = 0, sub5 = 0, sub10 = 0, iva5 = 0, iva10 = 0;

		foreach (var item in data.Items)
		{
			var lineTotal = Math.Round(item.Quantity * item.UnitPrice, digits, MidpointRounding.AwayFromZero);
			var vatAmount = VatSplit.VatShare(lineTotal, item.VatRatePercent, digits);

			if (item.VatRatePercent == 0) exempt += lineTotal;
			else if (item.VatRatePercent == 5) { sub5 += lineTotal; iva5 += vatAmount; }
			else { sub10 += lineTotal; iva10 += vatAmount; }
		}

		var total = exempt + sub5 + sub10;

		return new XElement(Ns + "gTotSub",
			new XElement(Ns + "dSubExe", exempt),
			new XElement(Ns + "dSub5", sub5),
			new XElement(Ns + "dSub10", sub10),
			new XElement(Ns + "dTotOpe", total),
			new XElement(Ns + "dTotGralOpe", total),
			new XElement(Ns + "dIVA5", iva5),
			new XElement(Ns + "dIVA10", iva10),
			new XElement(Ns + "dTotIVA", iva5 + iva10));
	}
}
