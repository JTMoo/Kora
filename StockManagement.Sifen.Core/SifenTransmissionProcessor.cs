using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


/// <summary>
/// Applies one <see cref="SifenTransmissionResult"/> to its outbox row: terminal outcomes update the invoice and
/// remove the row; <see cref="SifenTransmissionOutcome.Error"/> goes through <see cref="TransmissionRetryPolicy"/>.
/// Split out from <see cref="SifenTransmissionWorker"/> so the decision is testable without a host or a database.
/// </summary>
public static class SifenTransmissionProcessor
{
	public static Task ApplyAsync(IPendingTransmissionServiceProvider pendingTransmissions, PendingTransmission transmission, SifenTransmissionResult result, DateTime now, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(pendingTransmissions);
		ArgumentNullException.ThrowIfNull(transmission);
		ArgumentNullException.ThrowIfNull(result);

		return result.Outcome switch
		{
			SifenTransmissionOutcome.Accepted => pendingTransmissions.MarkTerminalAsync(transmission, TransmissionStatus.Accepted, result.Cdc!, cancellationToken),
			SifenTransmissionOutcome.Rejected => pendingTransmissions.MarkTerminalAsync(transmission, TransmissionStatus.Rejected, result.Cdc!, cancellationToken),
			SifenTransmissionOutcome.Error => pendingTransmissions.MarkErrorAsync(transmission, result.Message, TransmissionRetryPolicy.NextAttempt(transmission.Invoice.Date, transmission.Attempts, now), cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
		};
	}

	/// <summary>
	/// Same as <see cref="ApplyAsync"/>, for a <see cref="PendingRemisionTransmission"/> (#162).
	/// </summary>
	public static Task ApplyRemisionAsync(IPendingRemisionTransmissionServiceProvider pendingTransmissions, PendingRemisionTransmission transmission, SifenTransmissionResult result, DateTime now, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(pendingTransmissions);
		ArgumentNullException.ThrowIfNull(transmission);
		ArgumentNullException.ThrowIfNull(result);

		return result.Outcome switch
		{
			SifenTransmissionOutcome.Accepted => pendingTransmissions.MarkTerminalAsync(transmission, TransmissionStatus.Accepted, result.Cdc!, cancellationToken),
			SifenTransmissionOutcome.Rejected => pendingTransmissions.MarkTerminalAsync(transmission, TransmissionStatus.Rejected, result.Cdc!, cancellationToken),
			SifenTransmissionOutcome.Error => pendingTransmissions.MarkErrorAsync(transmission, result.Message, TransmissionRetryPolicy.NextAttempt(transmission.RemissionNote.Date, transmission.Attempts, now), cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
		};
	}

	/// <summary>
	/// Same as <see cref="ApplyAsync"/>, for a <see cref="PendingDebitNoteTransmission"/> (#184).
	/// </summary>
	public static Task ApplyDebitNoteAsync(IPendingDebitNoteTransmissionServiceProvider pendingTransmissions, PendingDebitNoteTransmission transmission, SifenTransmissionResult result, DateTime now, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(pendingTransmissions);
		ArgumentNullException.ThrowIfNull(transmission);
		ArgumentNullException.ThrowIfNull(result);

		return result.Outcome switch
		{
			SifenTransmissionOutcome.Accepted => pendingTransmissions.MarkTerminalAsync(transmission, TransmissionStatus.Accepted, result.Cdc!, cancellationToken),
			SifenTransmissionOutcome.Rejected => pendingTransmissions.MarkTerminalAsync(transmission, TransmissionStatus.Rejected, result.Cdc!, cancellationToken),
			SifenTransmissionOutcome.Error => pendingTransmissions.MarkErrorAsync(transmission, result.Message, TransmissionRetryPolicy.NextAttempt(transmission.DebitNote.Date, transmission.Attempts, now), cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
		};
	}
}
