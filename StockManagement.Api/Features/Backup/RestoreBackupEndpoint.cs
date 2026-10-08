using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Backup.Core.Contracts;

namespace StockManagement.Api.Features.Backup;


public sealed class RestoreBackupRequest
{
	public IFormFile File { get; set; } = default!;

	/// <summary>Must be explicitly true - the web UI gates this behind a typed confirmation, not just a click (ADR-0045)</summary>
	public bool Confirm { get; set; }
}


public class RestoreBackupValidator : Validator<RestoreBackupRequest>
{
	public RestoreBackupValidator()
	{
		this.RuleFor(request => request.File).Must(file => file is { Length: > 0 }).WithMessage("fileRequired");
		this.RuleFor(request => request.Confirm).Equal(true).WithMessage("confirmRequired");
	}
}


/// <remarks>Destructive: pre-restore snapshot, then <c>pg_restore --clean</c> over the live DB (ADR-0045). Restart the app afterwards.</remarks>
public class RestoreBackupEndpoint(IBackupService backupService, ILogger<RestoreBackupEndpoint> logger) : Endpoint<RestoreBackupRequest>
{
	private readonly IBackupService _backupService = backupService;
	private readonly ILogger<RestoreBackupEndpoint> _logger = logger;


	public override void Configure()
	{
		this.Post("/backups/restore");
		this.AllowFileUploads();
		this.Permissions(Permission.BackupManage);
	}

	public override async Task HandleAsync(RestoreBackupRequest request, CancellationToken cancellationToken)
	{
		await using var stream = request.File.OpenReadStream();

		try
		{
			await _backupService.RestoreAsync(stream, cancellationToken);
		}
		catch (BackupFailedException ex)
		{
			_logger.LogError(ex, "Restore failed: {Output}", ex.ProcessOutput);
			throw;
		}
	}
}
