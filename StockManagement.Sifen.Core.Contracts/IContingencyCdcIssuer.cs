namespace StockManagement.Sifen.Core.Contracts;


/// <summary>
/// Local, offline CDC issuance from a pre-assigned DNIT contingency range (#149) - used when a sale must be
/// legally issued with zero connectivity to SIFEN; transmission through the normal outbox happens once
/// connectivity is restored, within the same 72h deadline as any other document.
/// </summary>
public interface IContingencyCdcIssuer
{
	public Task<ContingencyStatus> GetStatusAsync(CancellationToken cancellationToken = default);

	/// <exception cref="ArgumentOutOfRangeException"><paramref name="rangeEnd"/> is not after <paramref name="rangeStart"/></exception>
	public Task ConfigureRangeAsync(long rangeStart, long rangeEnd, CancellationToken cancellationToken = default);

	/// <exception cref="InvalidOperationException">No range configured yet</exception>
	public Task SetActiveAsync(bool active, CancellationToken cancellationToken = default);

	/// <summary>
	/// Reserves the next contingency document number and builds its 44-digit CDC (<see cref="EmissionType.Contingencia"/>)
	/// for the given <paramref name="documentType"/>, or <see langword="null"/> when contingency mode is off or the
	/// range is exhausted. The reserved-number sequence is shared across document types (#188) - one DNIT-granted range.
	/// </summary>
	/// <exception cref="InvalidOperationException">Company settings are missing data a CDC needs</exception>
	public Task<string?> TryIssueAsync(SifenDocumentType documentType, CancellationToken cancellationToken = default);
}
