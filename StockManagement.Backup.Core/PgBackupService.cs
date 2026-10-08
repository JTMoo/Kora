using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using StockManagement.Backup.Core.Contracts;

namespace StockManagement.Backup.Core;


internal sealed class PgBackupService : IBackupService
{
	private readonly BackupOptions _options;
	private readonly string _connectionString;
	private readonly IProcessRunner _processRunner;
	private readonly IBackendTerminator _backendTerminator;
	private readonly ILogger<PgBackupService> _logger;


	public PgBackupService(IOptions<BackupOptions> options, IConfiguration configuration, IProcessRunner processRunner, IBackendTerminator backendTerminator, ILogger<PgBackupService> logger)
	{
		this._options = options.Value;
		this._connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("ConnectionStrings:Postgres is missing.");
		this._processRunner = processRunner;
		this._backendTerminator = backendTerminator;
		this._logger = logger;
	}


	public async Task<BackupFile> CreateBackupAsync(CancellationToken cancellationToken = default)
	{
		return await this.DumpAsync($"kora-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.dump", cancellationToken);
	}

	public Task<IReadOnlyList<BackupFile>> ListBackupsAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var directory = this.ResolveDirectory();
		if (!System.IO.Directory.Exists(directory))
			return Task.FromResult<IReadOnlyList<BackupFile>>([]);

		var files = new System.IO.DirectoryInfo(directory).GetFiles("*.dump")
			.OrderByDescending(file => file.CreationTimeUtc)
			.Select(file => new BackupFile(file.Name, file.Length, file.CreationTimeUtc))
			.ToList();
		return Task.FromResult<IReadOnlyList<BackupFile>>(files);
	}

	public Task<Stream> OpenBackupAsync(string fileName, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var path = this.ResolveExistingFilePath(fileName);
		return Task.FromResult<Stream>(System.IO.File.OpenRead(path));
	}

	public async Task RestoreAsync(Stream backupData, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(backupData);

		await this.DumpAsync($"pre-restore-{DateTime.UtcNow:yyyyMMdd-HHmmss}.dump", cancellationToken);

		var uploadPath = System.IO.Path.Combine(this.ResolveDirectory(), $"restore-upload-{Guid.NewGuid():N}.dump");
		await using (var fileStream = System.IO.File.Create(uploadPath))
			await backupData.CopyToAsync(fileStream, cancellationToken);

		try
		{
			await this._backendTerminator.TerminateOtherBackendsAsync(this._connectionString, cancellationToken);

			var builder = new NpgsqlConnectionStringBuilder(this._connectionString);
			var result = await this._processRunner.RunAsync(
				this._options.PgRestorePath,
				["-h", builder.Host ?? "127.0.0.1", "-p", builder.Port.ToString(), "-U", builder.Username ?? "postgres", "-d", builder.Database ?? "", "--clean", "--if-exists", "--no-owner", uploadPath],
				new Dictionary<string, string> { ["PGPASSWORD"] = builder.Password ?? "" },
				cancellationToken);

			if (result.ExitCode != 0)
			{
				this._logger.LogError("pg_restore exited {ExitCode}: {Output}", result.ExitCode, result.StandardError);
				throw new BackupFailedException("Restore failed.", result.StandardError);
			}
		}
		finally
		{
			System.IO.File.Delete(uploadPath);
		}
	}

	private async Task<BackupFile> DumpAsync(string fileName, CancellationToken cancellationToken)
	{
		var directory = this.ResolveDirectory();
		System.IO.Directory.CreateDirectory(directory);
		var path = System.IO.Path.Combine(directory, fileName);

		var builder = new NpgsqlConnectionStringBuilder(this._connectionString);
		var result = await this._processRunner.RunAsync(
			this._options.PgDumpPath,
			["-h", builder.Host ?? "127.0.0.1", "-p", builder.Port.ToString(), "-U", builder.Username ?? "postgres", "-Fc", "-f", path, builder.Database ?? ""],
			new Dictionary<string, string> { ["PGPASSWORD"] = builder.Password ?? "" },
			cancellationToken);

		if (result.ExitCode != 0)
		{
			this._logger.LogError("pg_dump exited {ExitCode}: {Output}", result.ExitCode, result.StandardError);
			System.IO.File.Delete(path);
			throw new BackupFailedException("Backup failed.", result.StandardError);
		}

		var info = new System.IO.FileInfo(path);
		return new BackupFile(info.Name, info.Length, info.CreationTimeUtc);
	}

	private string ResolveDirectory()
	{
		if (!string.IsNullOrWhiteSpace(this._options.Directory))
			return this._options.Directory;

		var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		return System.IO.Path.Combine(appData, "Kora", "Backups");
	}

	/// <exception cref="FileNotFoundException"><paramref name="fileName"/> is not a known backup file</exception>
	private string ResolveExistingFilePath(string fileName)
	{
		var directory = this.ResolveDirectory();
		var safeName = System.IO.Path.GetFileName(fileName);
		var path = System.IO.Path.Combine(directory, safeName);

		if (safeName != fileName || !System.IO.File.Exists(path))
			throw new FileNotFoundException("Backup file not found.", fileName);

		return path;
	}
}
