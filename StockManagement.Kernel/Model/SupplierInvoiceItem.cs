namespace StockManagement.Kernel.Model;


/// <summary>
/// One stock item received under a domestic <see cref="SupplierInvoice"/>. Posting the invoice checks in
/// <see cref="Amount"/> as stock (#246), unlike a <see cref="GoodsImportDocumentItem"/>.
/// </summary>
public class SupplierInvoiceItem(StockItem item) : NotificationBase
{
	private int amount = 1;
	private decimal unitPrice;


	/// <summary>
	/// For EF Core materialization
	/// </summary>
	private SupplierInvoiceItem() : this(null!)
	{
	}


	public StockItem StockItem { get; set; } = item;

	public int Amount
	{
		get { return this.amount; }
		set { this.SetField(ref this.amount, value); }
	}

	/// <summary>
	/// Gross unit cost (VAT-inclusive, same convention as <see cref="StockItem.Price"/>)
	/// </summary>
	public decimal UnitPrice
	{
		get { return this.unitPrice; }
		set { this.SetField(ref this.unitPrice, value); }
	}
}
