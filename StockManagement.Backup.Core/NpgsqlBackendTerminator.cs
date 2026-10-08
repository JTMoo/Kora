using Npgsql;

namespace StockManagement.Backup.Core;


internal sealed class NpgsqlBackendTerminator : IBackendTerminator
{
	public async Task TerminateOtherBackendsAsync(string connectionString, CancellationToken cancellationToken)
	{
		var targetDatabase = new NpgsqlConnectionStringBuilder(connectionString).Database ?? "";
		var maintenanceBuilder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };

		await using var connection = new NpgsqlConnection(maintenanceBuilder.ConnectionString);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @db AND pid <> pg_backend_pid();";
		command.Parameters.AddWithValue("db", targetDatabase);
		await command.ExecuteNonQueryAsync(cancellationToken);
	}
}
