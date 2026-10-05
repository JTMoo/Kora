using StockManagement.Kernel.Model;

namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// The only transmission abstraction application code depends on (ADR-0031). The implementation (direct DNIT vs.
/// a PSE) is picked by DI registration, not by callers.
/// </summary>
public interface ISifenGateway
{
	/// <summary>
	/// Builds, signs and transmits <paramref name="invoice"/>'s DE. Never throws for a SIFEN-level rejection or a
	/// transport error - both come back as <see cref="SifenTransmissionResult"/> so the outbox worker can apply its
	/// retry policy; only a programming error (e.g. a malformed invoice) throws.
	/// </summary>
	Task<SifenTransmissionResult> SendAsync(Invoice invoice, CancellationToken cancellationToken = default);

	/// <summary>
	/// Builds, signs and transmits <paramref name="remissionNote"/>'s DE (#162). Same never-throws-for-SIFEN-outcomes
	/// contract as <see cref="SendAsync(Invoice, CancellationToken)"/>.
	/// </summary>
	Task<SifenTransmissionResult> SendRemisionAsync(RemissionNote remissionNote, CancellationToken cancellationToken = default);

	/// <summary>
	/// Builds, signs and transmits <paramref name="debitNote"/>'s DE (#184). Same never-throws-for-SIFEN-outcomes
	/// contract as <see cref="SendAsync(Invoice, CancellationToken)"/>.
	/// </summary>
	Task<SifenTransmissionResult> SendDebitNoteAsync(DebitNote debitNote, CancellationToken cancellationToken = default);

	/// <summary>
	/// Builds, signs and transmits a Cancelación event against <paramref name="request"/>'s invoice (#206). Same
	/// never-throws-for-SIFEN-outcomes contract as <see cref="SendAsync(Invoice, CancellationToken)"/>;
	/// <see cref="SifenTransmissionResult.Cdc"/> is unused for an event and always null.
	/// </summary>
	Task<SifenTransmissionResult> SendCancellationEventAsync(CancellationRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Builds, signs and transmits an Inutilización event for <paramref name="numberVoid"/>'s range (#206). Same
	/// contract as <see cref="SendCancellationEventAsync"/>.
	/// </summary>
	Task<SifenTransmissionResult> SendInutilizacionEventAsync(InvoiceNumberVoid numberVoid, CancellationToken cancellationToken = default);
}
