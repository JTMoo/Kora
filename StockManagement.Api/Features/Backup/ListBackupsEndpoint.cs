using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Backup.Core.Contracts;

namespace StockManagement.Api.Features.Backup;


public class ListBackupsEndpoint(IBackupService backupService) : EndpointWithoutRequest<IReadOnlyList<BackupFileResponse>>
{
	private readonly IBackupService _backupService = backupService;


	public override void Configure()
	{
		this.Get("/backups");
		this.Permissions(Permission.BackupManage);
	}

	public override async Task<IReadOnlyList<BackupFileResponse>> ExecuteAsync(CancellationToken cancellationToken)
	{
		var files = await _backupService.ListBackupsAsync(cancellationToken);
		return files.Select(BackupFileResponse.From).ToList();
	}
}
