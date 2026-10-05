namespace StockManagement.Kernel.Model.Types;


/// <summary>
/// SIFEN transmission lifecycle of an <see cref="Invoice"/> (ADR-0031)
/// </summary>
public enum TransmissionStatus
{
	Pending = 0,

	Sent,

	Accepted,

	/// <summary>
	/// SIFEN rejected the DE; not auto-retried, needs a corrected re-send
	/// </summary>
	Rejected,

	/// <summary>
	/// Network/timeout error; auto-retried until the 72h deadline
	/// </summary>
	Error,

	/// <summary>
	/// SIFEN accepted a Cancelación event against this document (#206) - terminal, distinct from <see cref="Rejected"/>
	/// </summary>
	Cancelled
}
