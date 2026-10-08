namespace StockManagement.Backup.Core.Contracts;


/// <summary>
/// Creates, lists and restores Postgres backups via <c>pg_dump</c>/<c>pg_restore</c> (ADR-0045)
/// </summary>
public interface IBackupService
{
	Task<BackupFile> CreateBackupAsync(CancellationToken cancellationToken = default);

	Task<IReadOnlyList<BackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default);

	/// <exception cref="FileNotFoundException"><paramref name="fileName"/> is not a known backup</exception>
	Task<Stream> OpenBackupAsync(string fileName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Snapshots the current DB (<c>pre-restore-*</c>), terminates other backends on it, then restores
	/// <paramref name="backupData"/> over it with <c>pg_restore --clean --if-exists</c>.
	/// </summary>
	Task RestoreAsync(Stream backupData, CancellationToken cancellationToken = default);
}
