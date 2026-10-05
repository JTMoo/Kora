using System.ComponentModel.DataAnnotations;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.invoice))]
public class Invoice : BaseDocument
{
	private SaleCondition saleCondition;
	private DateTime date;
	private DateTime expirationDate;
	private decimal total;
	private decimal tax;
	private string number = "";
	private bool isCancelled;
	private string cdc = "";
	private TransmissionStatus transmissionStatus = TransmissionStatus.Pending;
	private DateTime? acceptedAt;


	public Invoice()
	{
	}


	#region Properties
	/// <summary>
	/// DNIT composite number (establishment-pointOfSale-sequence); see <see cref="Util.InvoiceNumber"/>
	/// </summary>
	[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.invoiceId))]
	public string Number
	{
		get { return this.number; }
		set { this.SetField(ref this.number, value); }
	}
	[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.tax))]
	public decimal Tax
	{
		get { return this.tax; }
		set { this.SetField(ref this.tax, value); }
	}
	[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.creationDate))]
	public DateTime Date
	{
		get { return this.date; }
		set { this.SetField(ref this.date, value); }
	}
	[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.expirationDate))]
	public DateTime ExpirationDate
	{
		get { return this.expirationDate; }
		set { this.SetField(ref this.expirationDate, value); }
	}
	[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.total))]
	public decimal Total
	{
		get { return this.total; }
		set { this.SetField(ref this.total, value); }
	}
	[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.saleCondition))]
	public SaleCondition SaleCondition
	{
		get { return this.saleCondition; }
		set { this.SetField(ref this.saleCondition, value); }
	}
	[Display(ResourceType = typeof(Language.Customers), Name = nameof(Language.Customers.customer))]
	public Customer Customer { get; set; }
	public List<ShoppingCartItem> Items { get; set; }
	/// <summary>
	/// Cancelled by a <see cref="CreditNote"/>; invoices are never deleted (#56)
	/// </summary>
	[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.cancelled))]
	public bool IsCancelled
	{
		get { return this.isCancelled; }
		set { this.SetField(ref this.isCancelled, value); }
	}
	public List<Payment> Payments { get; set; } = [];
	/// <summary>
	/// 44-digit SIFEN control code; empty until the first transmission attempt builds the DE (ADR-0031)
	/// </summary>
	[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.cdc))]
	public string Cdc
	{
		get { return this.cdc; }
		set { this.SetField(ref this.cdc, value); }
	}
	[Display(ResourceType = typeof(Language.Invoices), Name = nameof(Language.Invoices.transmissionStatus))]
	public TransmissionStatus TransmissionStatus
	{
		get { return this.transmissionStatus; }
		set { this.SetField(ref this.transmissionStatus, value); }
	}
	/// <summary>
	/// When SIFEN accepted this DE; null until then. Anchors the 48h Cancelación window (#206) - distinct from
	/// <see cref="Date"/>, which anchors the 72h transmission deadline instead.
	/// </summary>
	public DateTime? AcceptedAt
	{
		get { return this.acceptedAt; }
		set { this.SetField(ref this.acceptedAt, value); }
	}
	#endregion Properties
}
