using System.ComponentModel.DataAnnotations;
using StockManagement.Kernel.Database;

namespace StockManagement.Kernel.Model;


/// <summary>
/// A supplier bill owed by the company; the payables mirror of <see cref="Invoice"/> (#207)
/// </summary>
[Display(ResourceType = typeof(Language.Suppliers), Name = nameof(Language.Suppliers.supplierInvoice))]
public class SupplierInvoice : BaseDocument
{
	private string number = "";
	private DateTime date;
	private DateTime expirationDate;
	private decimal total;


	public SupplierInvoice()
	{
	}


	[Display(ResourceType = typeof(Language.Suppliers), Name = nameof(Language.Suppliers.supplierInvoiceNumber))]
	public string Number
	{
		get { return this.number; }
		set { this.SetField(ref this.number, value); }
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

	[Display(ResourceType = typeof(Language.Suppliers), Name = nameof(Language.Suppliers.supplier))]
	public Supplier Supplier { get; set; }

	public List<Payment> Payments { get; set; } = [];
}
