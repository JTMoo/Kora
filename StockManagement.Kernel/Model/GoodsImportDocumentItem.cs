namespace StockManagement.Kernel.Model;


/// <summary>
/// One stock item received under a <see cref="GoodsImportDocument"/>. Receiving posts a stock check-in for
/// <see cref="Amount"/>, feeding the existing landed-cost allocation (#120/#143) afterwards.
/// </summary>
public class GoodsImportDocumentItem(StockItem item) : NotificationBase
{
	private int amount = 1;


	/// <summary>
	/// For EF Core materialization
	/// </summary>
	private GoodsImportDocumentItem() : this(null!)
	{
	}


	public StockItem StockItem { get; set; } = item;

	public int Amount
	{
		get { return this.amount; }
		set { this.SetField(ref this.amount, value); }
	}
}
