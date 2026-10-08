namespace StockManagement.Backup.Core;


/// <summary>
/// Backup config (ADR-0045). <see cref="Directory"/> empty falls back to a per-OS app-data path.
/// </summary>
public sealed class BackupOptions
{
	public const string SectionName = "Backup";

	public string Directory { get; set; } = "";

	/// <summary>Executable name/path for <c>pg_dump</c>; resolved via PATH when just a name</summary>
	public string PgDumpPath { get; set; } = "pg_dump";

	/// <summary>Executable name/path for <c>pg_restore</c>; resolved via PATH when just a name</summary>
	public string PgRestorePath { get; set; } = "pg_restore";
}
