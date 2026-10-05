namespace StockManagement.Kernel.Model;


/// <summary>
/// One charge line on a <see cref="DebitNote"/> (#184) - a debit note adds money (interest, surcharges, price
/// corrections), not stock, so it has no <see cref="StockItem"/> reference.
/// </summary>
public class DebitNoteItem : NotificationBase
{
	private string description = "";
	private decimal amount;
	private int vatRatePercent;


	/// <summary>
	/// VAT-inclusive line total, same convention as <see cref="ShoppingCartItem"/>'s price
	/// </summary>
	public decimal Amount
	{
		get { return this.amount; }
		set { this.SetField(ref this.amount, value); }
	}

	public string Description
	{
		get { return this.description; }
		set { this.SetField(ref this.description, value); }
	}

	public int VatRatePercent
	{
		get { return this.vatRatePercent; }
		set { this.SetField(ref this.vatRatePercent, value); }
	}
}
