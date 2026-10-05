using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


public class Payment : NotificationBase
{
	private DateTime date;
	private decimal amount;
	private PaymentMethod method;
	private string? cashRegisterSessionId;


	public DateTime Date
	{
		get { return this.date; }
		set { this.SetField(ref this.date, value); }
	}
	public decimal Amount
	{
		get { return this.amount; }
		set { this.SetField(ref this.amount, value); }
	}
	public PaymentMethod Method
	{
		get { return this.method; }
		set { this.SetField(ref this.method, value); }
	}
	/// <summary>Cash register session open at the time this payment was recorded; <see langword="null"/> for non-cash methods or when no session was open</summary>
	public string? CashRegisterSessionId
	{
		get { return this.cashRegisterSessionId; }
		set { this.SetField(ref this.cashRegisterSessionId, value); }
	}
}
