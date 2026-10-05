using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


public class CashRegisterSession : BaseDocument
{
	private DateTime openedAt;
	private string openedByUserId = "";
	private decimal openingFloat;
	private DateTime? closedAt;
	private string? closedByUserId;
	private decimal? countedAmount;
	private string note = "";
	private CashRegisterSessionStatus status = CashRegisterSessionStatus.Open;


	public DateTime OpenedAt
	{
		get { return this.openedAt; }
		set { this.SetField(ref this.openedAt, value); }
	}
	public string OpenedByUserId
	{
		get { return this.openedByUserId; }
		set { this.SetField(ref this.openedByUserId, value); }
	}
	public decimal OpeningFloat
	{
		get { return this.openingFloat; }
		set { this.SetField(ref this.openingFloat, value); }
	}
	public DateTime? ClosedAt
	{
		get { return this.closedAt; }
		set { this.SetField(ref this.closedAt, value); }
	}
	public string? ClosedByUserId
	{
		get { return this.closedByUserId; }
		set { this.SetField(ref this.closedByUserId, value); }
	}
	/// <summary>Physically counted cash, recorded at close</summary>
	public decimal? CountedAmount
	{
		get { return this.countedAmount; }
		set { this.SetField(ref this.countedAmount, value); }
	}
	public string Note
	{
		get { return this.note; }
		set { this.SetField(ref this.note, value); }
	}
	public CashRegisterSessionStatus Status
	{
		get { return this.status; }
		set { this.SetField(ref this.status, value); }
	}
	public List<CashMovement> Movements { get; set; } = [];
}
