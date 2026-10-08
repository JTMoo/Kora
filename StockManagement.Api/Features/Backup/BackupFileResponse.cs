using StockManagement.Backup.Core.Contracts;

namespace StockManagement.Api.Features.Backup;


public sealed record BackupFileResponse(string FileName, long SizeBytes, DateTime CreatedAtUtc)
{
	public static BackupFileResponse From(BackupFile file) => new(file.FileName, file.SizeBytes, file.CreatedAtUtc);
}
