using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Backup.Core.Contracts;

namespace StockManagement.Api.Features.Backup;


public sealed record DownloadBackupRequest(string FileName);


public class DownloadBackupEndpoint(IBackupService backupService) : Endpoint<DownloadBackupRequest, Results<FileStreamHttpResult, NotFound>>
{
	private readonly IBackupService _backupService = backupService;


	public override void Configure()
	{
		this.Get("/backups/{FileName}/download");
		this.Permissions(Permission.BackupManage);
	}

	public override async Task<Results<FileStreamHttpResult, NotFound>> ExecuteAsync(DownloadBackupRequest request, CancellationToken cancellationToken)
	{
		try
		{
			var stream = await _backupService.OpenBackupAsync(request.FileName, cancellationToken);
			return TypedResults.Stream(stream, "application/octet-stream", request.FileName);
		}
		catch (FileNotFoundException)
		{
			return TypedResults.NotFound();
		}
	}
}
