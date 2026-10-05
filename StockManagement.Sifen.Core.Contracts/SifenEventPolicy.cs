namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// Deadlines for SIFEN events (#206) shared between the outbox retry policy (<c>Sifen.Core</c>) and the request
/// validation that runs before a row is ever queued (<c>Sales.Core</c>) - kept here so neither needs a project
/// reference to the other's implementation assembly.
/// </summary>
public static class SifenEventPolicy
{
	/// <summary>Cancelación's window - 48h from SIFEN's acceptance of the DE being cancelled</summary>
	public static readonly TimeSpan CancellationDeadline = TimeSpan.FromHours(48);
}
