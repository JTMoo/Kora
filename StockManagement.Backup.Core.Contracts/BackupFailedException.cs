namespace StockManagement.Backup.Core.Contracts;


/// <summary>
/// <c>pg_dump</c>/<c>pg_restore</c> exited non-zero; <see cref="ProcessOutput"/> is its stderr for diagnostics
/// </summary>
public sealed class BackupFailedException(string message, string processOutput) : Exception(message)
{
	public string ProcessOutput { get; } = processOutput;
}
