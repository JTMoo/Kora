using StockManagement.Kernel.Database;

namespace StockManagement.Kernel.Model;


/// <summary>
/// Outbox row for one SIFEN transmission attempt of a <see cref="DebitNote"/> (#184) - same shape and retry
/// semantics as <see cref="PendingTransmission"/>/<see cref="PendingRemisionTransmission"/>, kept separate because
/// it points at a different document entity.
/// </summary>
public class PendingDebitNoteTransmission : BaseDocument
{
	private int attempts;
	private DateTime nextAttemptAt;
	private string lastError = "";


	public PendingDebitNoteTransmission()
	{
	}

	public PendingDebitNoteTransmission(DebitNote debitNote, DateTime nextAttemptAt)
	{
		this.DebitNote = debitNote;
		this.NextAttemptAt = nextAttemptAt;
	}


	public DebitNote DebitNote { get; set; } = null!;

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
