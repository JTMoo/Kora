using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Model;


/// <summary>
/// SIFEN Cancelación event (#206): retracts an already-<see cref="TransmissionStatus.Accepted"/> <see cref="Invoice"/>'s
/// DE, up to 48h after acceptance. Kept permanently (not removed on a terminal outcome) as the record of the event,
/// same reasoning as DNIT's own 5y retention. Own <see cref="TransmissionStatus"/>, separate from
/// <see cref="Invoice.TransmissionStatus"/> - the invoice stays <see cref="TransmissionStatus.Accepted"/> while a
/// cancellation is in flight and only moves to <see cref="TransmissionStatus.Cancelled"/> once SIFEN confirms it.
/// </summary>
public class CancellationRequest : BaseDocument
{
	private string reason = "";
	private DateTime requestedAt;
	private TransmissionStatus transmissionStatus = TransmissionStatus.Pending;
	private int attempts;
	private DateTime nextAttemptAt;
	private string lastError = "";


	public CancellationRequest()
	{
	}

	public CancellationRequest(Invoice invoice, string reason, DateTime requestedAt)
	{
		this.Invoice = invoice;
		this.Reason = reason;
		this.RequestedAt = requestedAt;
		this.NextAttemptAt = requestedAt;
	}


	public Invoice Invoice { get; set; } = null!;

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
