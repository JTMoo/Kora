namespace StockManagement.Backup.Core.Contracts;


/// <summary>
/// Metadata for one backup file on disk (ADR-0045)
/// </summary>
public sealed record BackupFile(string FileName, long SizeBytes, DateTime CreatedAtUtc);
