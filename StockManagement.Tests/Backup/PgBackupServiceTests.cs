using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StockManagement.Backup.Core;
using StockManagement.Backup.Core.Contracts;

namespace StockManagement.Tests.Backup;


[TestClass]
public sealed class PgBackupServiceTests
{
	private readonly Mock<IProcessRunner> _processRunner = new();
	private readonly Mock<IBackendTerminator> _backendTerminator = new();
	private readonly Mock<IConfiguration> _configuration = new();
	private readonly Mock<IConfigurationSection> _connectionStringsSection = new();
	private readonly string _directory = Path.Combine(Path.GetTempPath(), $"kora-backup-tests-{Guid.NewGuid():N}");


	[TestInitialize]
	public void Initialize()
	{
		_connectionStringsSection.Setup(section => section["Postgres"])
			.Returns("Host=127.0.0.1;Port=5432;Database=testdb;Username=postgres;Password=postgres");
		_configuration.Setup(configuration => configuration.GetSection("ConnectionStrings"))
			.Returns(_connectionStringsSection.Object);
	}

	[TestCleanup]
	public void Cleanup()
	{
		if (Directory.Exists(_directory))
			Directory.Delete(_directory, recursive: true);
	}


	[TestMethod]
	public async Task CreateBackupAsync_PgDumpSucceeds_ReturnsCreatedFile()
	{
		// Arrange
		_processRunner.Setup(runner => runner.RunAsync("pg_dump", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
			.Returns((string _, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> _, CancellationToken _) =>
			{
				var path = arguments[arguments.ToList().IndexOf("-f") + 1];
				File.WriteAllText(path, "dump-contents");
				return Task.FromResult(new ProcessResult(0, ""));
			});

		// Act
		var result = await this.CreateService().CreateBackupAsync();

		// Assert
		Assert.IsTrue(result.FileName.StartsWith("kora-backup-") && result.FileName.EndsWith(".dump"));
		Assert.AreEqual("dump-contents".Length, result.SizeBytes);
	}

	[TestMethod]
	public async Task CreateBackupAsync_PgDumpFails_ThrowsAndDeletesPartialFile()
	{
		// Arrange
		_processRunner.Setup(runner => runner.RunAsync("pg_dump", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
			.Returns((string _, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> _, CancellationToken _) =>
			{
				var path = arguments[arguments.ToList().IndexOf("-f") + 1];
				File.WriteAllText(path, "partial");
				return Task.FromResult(new ProcessResult(1, "connection refused"));
			});

		// Act & Assert
		var exception = await Assert.ThrowsExceptionAsync<BackupFailedException>(() => this.CreateService().CreateBackupAsync());
		Assert.AreEqual("connection refused", exception.ProcessOutput);
		Assert.IsFalse(Directory.Exists(_directory) && Directory.GetFiles(_directory).Any(file => file.EndsWith(".dump")));
	}

	[TestMethod]
	public async Task ListBackupsAsync_DirectoryMissing_ReturnsEmpty()
	{
		// Act
		var result = await this.CreateService().ListBackupsAsync();

		// Assert
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task ListBackupsAsync_HasDumpFiles_ReturnsThemNewestFirst()
	{
		// Arrange
		Directory.CreateDirectory(_directory);
		var older = Path.Combine(_directory, "kora-backup-20260101-000000.dump");
		var newer = Path.Combine(_directory, "kora-backup-20260102-000000.dump");
		await File.WriteAllTextAsync(older, "a");
		File.SetCreationTimeUtc(older, DateTime.UtcNow.AddDays(-1));
		await File.WriteAllTextAsync(newer, "bb");
		File.SetCreationTimeUtc(newer, DateTime.UtcNow);

		// Act
		var result = await this.CreateService().ListBackupsAsync();

		// Assert
		Assert.AreEqual(2, result.Count);
		Assert.AreEqual("kora-backup-20260102-000000.dump", result[0].FileName);
	}

	[TestMethod]
	public async Task OpenBackupAsync_PathTraversalAttempt_Throws()
	{
		// Arrange
		Directory.CreateDirectory(_directory);

		// Act & Assert
		await Assert.ThrowsExceptionAsync<FileNotFoundException>(() => this.CreateService().OpenBackupAsync("../../etc/passwd"));
	}

	[TestMethod]
	public async Task OpenBackupAsync_UnknownFile_Throws()
	{
		// Arrange
		Directory.CreateDirectory(_directory);

		// Act & Assert
		await Assert.ThrowsExceptionAsync<FileNotFoundException>(() => this.CreateService().OpenBackupAsync("does-not-exist.dump"));
	}

	[TestMethod]
	public async Task RestoreAsync_PgRestoreFails_ThrowsAfterPreRestoreSnapshot()
	{
		// Arrange
		_processRunner.Setup(runner => runner.RunAsync("pg_dump", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
			.Returns((string _, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> _, CancellationToken _) =>
			{
				File.WriteAllText(arguments[arguments.ToList().IndexOf("-f") + 1], "snapshot");
				return Task.FromResult(new ProcessResult(0, ""));
			});
		_processRunner.Setup(runner => runner.RunAsync("pg_restore", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ProcessResult(1, "pg_restore: error: could not execute query"));

		await using var upload = new MemoryStream([1, 2, 3]);

		// Act & Assert
		await Assert.ThrowsExceptionAsync<BackupFailedException>(() => this.CreateService().RestoreAsync(upload));
		_processRunner.Verify(runner => runner.RunAsync("pg_dump", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
		_backendTerminator.Verify(terminator => terminator.TerminateOtherBackendsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	private PgBackupService CreateService()
	{
		var options = Options.Create(new BackupOptions { Directory = _directory });
		return new PgBackupService(options, _configuration.Object, _processRunner.Object, _backendTerminator.Object, NullLogger<PgBackupService>.Instance);
	}
}
