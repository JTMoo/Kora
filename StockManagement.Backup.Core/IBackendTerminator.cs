namespace StockManagement.Backup.Core;


/// <summary>Terminates other open connections to a DB before <c>pg_restore</c> runs, so restore isn't blocked by them; mockable in tests</summary>
public interface IBackendTerminator
{
	Task TerminateOtherBackendsAsync(string connectionString, CancellationToken cancellationToken);
}
