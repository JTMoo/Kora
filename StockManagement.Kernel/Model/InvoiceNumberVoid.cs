using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


/// <summary>
/// SIFEN Inutilización event (#206): voids a range of unused sequential <see cref="Invoice.Number"/> sequences
/// (e.g. a gap left by errors/restarts), up to 1000 numbers, scoped to the configured establishment/point-of-sale.
/// Kept permanently as the record of the event, same reasoning as <see cref="CancellationRequest"/>.
/// </summary>
public class InvoiceNumberVoid : BaseDocument
{
	private int rangeStart;
	private int rangeEnd;
	private string reason = "";
	private DateTime requestedAt;
	private TransmissionStatus transmissionStatus = TransmissionStatus.Pending;
	private int attempts;
	private DateTime nextAttemptAt;
	private string lastError = "";


	public InvoiceNumberVoid()
	{
	}

	public InvoiceNumberVoid(int rangeStart, int rangeEnd, string reason, DateTime requestedAt)
	{
		this.RangeStart = rangeStart;
		this.RangeEnd = rangeEnd;
		this.Reason = reason;
		this.RequestedAt = requestedAt;
		this.NextAttemptAt = requestedAt;
	}


	/// <summary>Sequence component only (not the composite "001-001-0000001" form), inclusive</summary>
	public int RangeStart
	{
		get { return this.rangeStart; }
		set { this.SetField(ref this.rangeStart, value); }
	}

	/// <summary>Inclusive</summary>
	public int RangeEnd
	{
		get { return this.rangeEnd; }
		set { this.SetField(ref this.rangeEnd, value); }
	}

	/// <summary>Free text, max 150 chars per DNIT's Inutilización rules</summary>
	public string Reason
	{
		get { return this.reason; }
		set { this.SetField(ref this.reason, value); }
	}

	public DateTime RequestedAt
	{
		get { return this.requestedAt; }
		set { this.SetField(ref this.requestedAt, value); }
	}

	public TransmissionStatus TransmissionStatus
	{
		get { return this.transmissionStatus; }
		set { this.SetField(ref this.transmissionStatus, value); }
	}

	public int Attempts
	{
		get { return this.attempts; }
		set { this.SetField(ref this.attempts, value); }
	}

	public DateTime NextAttemptAt
	{
		get { return this.nextAttemptAt; }
		set { this.SetField(ref this.nextAttemptAt, value); }
	}

	public string LastError
	{
		get { return this.lastError; }
		set { this.SetField(ref this.lastError, value); }
	}
}
