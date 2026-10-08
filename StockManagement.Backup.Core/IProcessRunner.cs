namespace StockManagement.Backup.Core;


public sealed record ProcessResult(int ExitCode, string StandardError);


/// <summary>Thin wrapper around <see cref="System.Diagnostics.Process"/> so <c>pg_dump</c>/<c>pg_restore</c> invocations are mockable in tests</summary>
public interface IProcessRunner
{
	Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken);
}
