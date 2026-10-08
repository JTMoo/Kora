using System.Net;
using System.Net.Http.Json;
using StockManagement.Api.Features.Backup;
using StockManagement.Api.Features.Customers;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class BackupEndpointsTests
{
	private ApiFactory _factory;
	private HttpClient _client;
	private string _backupDirectory;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_backupDirectory = Path.Combine(Path.GetTempPath(), $"kora-backup-tests-{Guid.NewGuid():N}");
		_factory = new(new Dictionary<string, string?> { ["Backup:Directory"] = _backupDirectory });
		_client = await _factory.CreateAuthenticatedClientAsync();
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
		if (Directory.Exists(_backupDirectory))
			Directory.Delete(_backupDirectory, recursive: true);
	}


	[TestMethod]
	public async Task CreateThenList_ReturnsTheCreatedBackup()
	{
		// Act
		var createResponse = await _client.PostAsync("/api/backups", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, createResponse.StatusCode);
		var created = await createResponse.Content.ReadAsAsync<BackupFileResponse>();
		Assert.IsTrue(created.SizeBytes > 0);

		var list = await (await _client.GetAsync("/api/backups")).Content.ReadAsAsync<List<BackupFileResponse>>();
		Assert.IsTrue(list.Any(file => file.FileName == created.FileName));
	}

	[TestMethod]
	public async Task Download_UnknownFile_Returns404()
	{
		// Act
		var response = await _client.GetAsync("/api/backups/does-not-exist.dump/download");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task Download_PathTraversalAttempt_Returns404()
	{
		// Act
		var response = await _client.GetAsync("/api/backups/..%2f..%2fetc%2fpasswd/download");

		// Assert
		Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode);
	}

	[TestMethod]
	public async Task Restore_WithoutConfirm_Returns400()
	{
		// Arrange
		using var content = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3]), "File", "backup.dump" } };

		// Act
		var response = await _client.PostAsync("/api/backups/restore", content);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "confirmRequired");
	}

	[TestMethod]
	public async Task Restore_NoFile_Returns400()
	{
		// Arrange
		using var content = new MultipartFormDataContent { { new StringContent("true"), "Confirm" } };

		// Act
		var response = await _client.PostAsync("/api/backups/restore", content);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "fileRequired");
	}

	[TestMethod]
	public async Task CreateBackupAddCustomerThenRestore_RevertsToTheBackedUpState()
	{
		// Arrange - one customer, then back it up
		await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest("Ana", Lastname: "Silva"));
		var backup = await (await _client.PostAsync("/api/backups", null)).Content.ReadAsAsync<BackupFileResponse>();

		// Arrange - a second customer added after the backup, which the restore should undo
		await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest("Bo", Lastname: "Nolan"));
		var beforeRestore = await (await _client.GetAsync("/api/customers")).Content.ReadAsAsync<CustomerListResponse>();
		Assert.AreEqual(2, beforeRestore.Items.Count);

		var dumpBytes = await (await _client.GetAsync($"/api/backups/{backup.FileName}/download")).Content.ReadAsByteArrayAsync();

		// Act
		using var restoreContent = new MultipartFormDataContent
		{
			{ new ByteArrayContent(dumpBytes), "File", backup.FileName },
			{ new StringContent("true"), "Confirm" }
		};
		var restoreResponse = await _client.PostAsync("/api/backups/restore", restoreContent);

		// Assert
		Assert.AreEqual(HttpStatusCode.NoContent, restoreResponse.StatusCode);
		var afterRestore = await (await _client.GetAsync("/api/customers")).Content.ReadAsAsync<CustomerListResponse>();
		CollectionAssert.AreEqual(new[] { "Ana" }, afterRestore.Items.Select(customer => customer.Name).ToList());

		// A pre-restore safety snapshot should also exist
		var files = await (await _client.GetAsync("/api/backups")).Content.ReadAsAsync<List<BackupFileResponse>>();
		Assert.IsTrue(files.Any(file => file.FileName.StartsWith("pre-restore-")));
	}

	[TestMethod]
	public async Task Create_NoToken_ReturnsUnauthorized()
	{
		// Arrange
		using var anonymousClient = _factory.CreateClient();

		// Act
		var response = await anonymousClient.PostAsync("/api/backups", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
	}
}
