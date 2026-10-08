using FastEndpoints;
using Microsoft.Extensions.Logging;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Backup.Core.Contracts;

namespace StockManagement.Api.Features.Backup;


public class CreateBackupEndpoint(IBackupService backupService, ILogger<CreateBackupEndpoint> logger) : EndpointWithoutRequest<BackupFileResponse>
{
	private readonly IBackupService _backupService = backupService;
	private readonly ILogger<CreateBackupEndpoint> _logger = logger;


	public override void Configure()
	{
		this.Post("/backups");
		this.Permissions(Permission.BackupManage);
	}

	public override async Task<BackupFileResponse> ExecuteAsync(CancellationToken cancellationToken)
	{
		try
		{
			var file = await _backupService.CreateBackupAsync(cancellationToken);
			return BackupFileResponse.From(file);
		}
		catch (BackupFailedException ex)
		{
			_logger.LogError(ex, "Backup failed: {Output}", ex.ProcessOutput);
			throw;
		}
	}
}
