using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


public class CashMovement : NotificationBase
{
	private CashMovementType type;
	private decimal amount;
	private string reason = "";
	private DateTime date;
	private string createdByUserId = "";


	public CashMovementType Type
	{
		get { return this.type; }
		set { this.SetField(ref this.type, value); }
	}
	public decimal Amount
	{
		get { return this.amount; }
		set { this.SetField(ref this.amount, value); }
	}
	public string Reason
	{
		get { return this.reason; }
		set { this.SetField(ref this.reason, value); }
	}
	public DateTime Date
	{
		get { return this.date; }
		set { this.SetField(ref this.date, value); }
	}
	public string CreatedByUserId
	{
		get { return this.createdByUserId; }
		set { this.SetField(ref this.createdByUserId, value); }
	}
}
