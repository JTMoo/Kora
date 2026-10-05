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

	/// <summary>
	/// Same as <see cref="ApplyAsync"/>, for a <see cref="CancellationRequest"/> (#206). Its deadline is 48h from
	/// <see cref="Invoice.AcceptedAt"/>, not 72h from the document date.
	/// </summary>
	public static Task ApplyCancellationAsync(ICancellationRequestServiceProvider cancellationRequests, CancellationRequest request, SifenTransmissionResult result, DateTime now, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(cancellationRequests);
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(result);

		var deadline = (request.Invoice.AcceptedAt ?? request.RequestedAt) + SifenEventPolicy.CancellationDeadline;
		return result.Outcome switch
		{
			SifenTransmissionOutcome.Accepted => cancellationRequests.MarkTerminalAsync(request, TransmissionStatus.Accepted, cancellationToken),
			SifenTransmissionOutcome.Rejected => cancellationRequests.MarkTerminalAsync(request, TransmissionStatus.Rejected, cancellationToken),
			SifenTransmissionOutcome.Error => cancellationRequests.MarkErrorAsync(request, result.Message, TransmissionRetryPolicy.NextAttemptBefore(deadline, request.Attempts, now), cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
		};
	}

	/// <summary>
	/// Same as <see cref="ApplyAsync"/>, for an <see cref="InvoiceNumberVoid"/> (#206). Deadline is 45 days from
	/// <see cref="InvoiceNumberVoid.RequestedAt"/> - approximates "before timbrado expiry", the simplest bound available
	/// without re-reading company settings from a pure decision function.
	/// </summary>
	public static Task ApplyInutilizacionAsync(IInvoiceNumberVoidServiceProvider numberVoids, InvoiceNumberVoid numberVoid, SifenTransmissionResult result, DateTime now, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(numberVoids);
		ArgumentNullException.ThrowIfNull(numberVoid);
		ArgumentNullException.ThrowIfNull(result);

		var deadline = numberVoid.RequestedAt.AddDays(45);
		return result.Outcome switch
		{
			SifenTransmissionOutcome.Accepted => numberVoids.MarkTerminalAsync(numberVoid, TransmissionStatus.Accepted, cancellationToken),
			SifenTransmissionOutcome.Rejected => numberVoids.MarkTerminalAsync(numberVoid, TransmissionStatus.Rejected, cancellationToken),
			SifenTransmissionOutcome.Error => numberVoids.MarkErrorAsync(numberVoid, result.Message, TransmissionRetryPolicy.NextAttemptBefore(deadline, numberVoid.Attempts, now), cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
		};
	}
}
