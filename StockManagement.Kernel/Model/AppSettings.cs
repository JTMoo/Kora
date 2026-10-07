using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


/// <summary>
/// Single row of application-wide settings
/// </summary>
public class AppSettings : BaseDocument
{
	private AvailableLanguages _language = AvailableLanguages.German;
	private string _companyName = "";
	private string _taxId = "";
	private string _currency = "";
	private decimal _vatRatePercent = 10m;
	private int _paymentTermInDays = 30;
	private int _firstInvoiceNumber = 1;
	private int _firstCustomerId = 1001;
	private int _currencyDecimalDigits = 0;
	private string _ruc = "";
	private string _timbradoNumber = "";
	private DateTime? _timbradoValidFrom;
	private DateTime? _timbradoValidTo;
	private string _establishmentCode = "001";
	private string _pointOfSaleCode = "001";
	private string _establishmentAddress = "";
	private string _defaultPrinterName = "";
	private int _receiptPaperWidthMm = 80;
	private KudeFormat _kudeFormat = KudeFormat.Ticket;
	private bool _printOnSaleComplete;


	public AvailableLanguages Language
	{
		get => this._language;
		set => this.SetField(ref this._language, value);
	}

	public string CompanyName
	{
		get => this._companyName;
		set => this.SetField(ref this._companyName, value);
	}

	public string TaxId
	{
		get => this._taxId;
		set => this.SetField(ref this._taxId, value);
	}

	/// <summary>
	/// ISO 4217 currency code (e.g. "PYG"); empty until set
	/// </summary>
	/// <remarks>Display only; not tied to <see cref="CurrencyDecimalDigits"/>.</remarks>
	public string Currency
	{
		get => this._currency;
		set => this.SetField(ref this._currency, value);
	}

	/// <summary>
	/// Decimal digits invoice totals/tax round to (0 for a zero-decimal currency like PYG)
	/// </summary>
	public int CurrencyDecimalDigits
	{
		get => this._currencyDecimalDigits;
		set => this.SetField(ref this._currencyDecimalDigits, value);
	}

	/// <summary>
	/// VAT rate as a percentage (e.g. 10 for 10 %), included in stock item prices
	/// </summary>
	public decimal VatRatePercent
	{
		get => this._vatRatePercent;
		set => this.SetField(ref this._vatRatePercent, value);
	}

	public int PaymentTermInDays
	{
		get => this._paymentTermInDays;
		set => this.SetField(ref this._paymentTermInDays, value);
	}

	/// <summary>
	/// Seed for the next invoice number when no invoice exists yet
	/// </summary>
	public int FirstInvoiceNumber
	{
		get => this._firstInvoiceNumber;
		set => this.SetField(ref this._firstInvoiceNumber, value);
	}

	/// <summary>
	/// Seed for the next customer id when no customer exists yet
	/// </summary>
	public int FirstCustomerId
	{
		get => this._firstCustomerId;
		set => this.SetField(ref this._firstCustomerId, value);
	}

	/// <summary>
	/// SIFEN taxpayer RUC (with check digit, e.g. "1234567-8")
	/// </summary>
	public string Ruc
	{
		get => this._ruc;
		set => this.SetField(ref this._ruc, value);
	}

	/// <summary>
	/// DNIT-issued timbrado number
	/// </summary>
	public string TimbradoNumber
	{
		get => this._timbradoNumber;
		set => this.SetField(ref this._timbradoNumber, value);
	}

	/// <summary>
	/// First day the timbrado may be used
	/// </summary>
	public DateTime? TimbradoValidFrom
	{
		get => this._timbradoValidFrom;
		set => this.SetField(ref this._timbradoValidFrom, value);
	}

	/// <summary>
	/// Last day the timbrado may be used
	/// </summary>
	public DateTime? TimbradoValidTo
	{
		get => this._timbradoValidTo;
		set => this.SetField(ref this._timbradoValidTo, value);
	}

	/// <summary>
	/// DNIT establishment code (3 digits), part of the composite invoice number
	/// </summary>
	public string EstablishmentCode
	{
		get => this._establishmentCode;
		set => this.SetField(ref this._establishmentCode, value);
	}

	/// <summary>
	/// DNIT point-of-sale code (3 digits), part of the composite invoice number
	/// </summary>
	public string PointOfSaleCode
	{
		get => this._pointOfSaleCode;
		set => this.SetField(ref this._pointOfSaleCode, value);
	}

	/// <summary>
	/// Street address of the establishment issuing the DTE (SIFEN <c>dDirEmi</c>)
	/// </summary>
	public string EstablishmentAddress
	{
		get => this._establishmentAddress;
		set => this.SetField(ref this._establishmentAddress, value);
	}

	/// <summary>
	/// Name of the printer to use for receipts; informational only, the browser's print dialog picks the actual printer
	/// </summary>
	public string DefaultPrinterName
	{
		get => this._defaultPrinterName;
		set => this.SetField(ref this._defaultPrinterName, value);
	}

	/// <summary>
	/// Paper width the receipt layout is formatted for, in mm (e.g. 80 or 58 for a ticket printer)
	/// </summary>
	public int ReceiptPaperWidthMm
	{
		get => this._receiptPaperWidthMm;
		set => this.SetField(ref this._receiptPaperWidthMm, value);
	}

	/// <summary>
	/// Layout the KuDE is printed in
	/// </summary>
	public KudeFormat KudeFormat
	{
		get => this._kudeFormat;
		set => this.SetField(ref this._kudeFormat, value);
	}

	/// <summary>
	/// Whether the receipt print dialog opens automatically once a sale completes
	/// </summary>
	public bool PrintOnSaleComplete
	{
		get => this._printOnSaleComplete;
		set => this.SetField(ref this._printOnSaleComplete, value);
	}
}
