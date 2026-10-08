using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.Import;
using StockManagement.Api.Features.Invoices;
using StockManagement.Api.Features.StockItems;
using StockManagement.Api.Features.Users;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Import.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class ImportBatchEndpointsTests
{
	private ApiFactory _factory;
	private HttpClient _client;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_factory = new();
		_client = await _factory.CreateAuthenticatedClientAsync();
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}


	[TestMethod]
	public async Task Preview_NewCustomers_ReturnsReadyRowsAndStoresNothingYet()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.Customers, CreateWorkbook("Customers",
			["Name", "Lastname"],
			["Ann", "Miller"],
			["Bo", "Nolan"]));

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var batch = await response.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(ImportBatchStatus.Previewed, batch.Status);
		Assert.AreEqual(2, batch.ReadyCount);
		Assert.AreEqual(0, batch.DuplicateCount);
		Assert.AreEqual(0, batch.ErrorCount);

		var customers = _factory.ScopedServices.GetRequiredService<ICustomerServiceProvider>();
		CollectionAssert.AreEqual(Array.Empty<Customer>(), (await customers.GetCustomersAsync()).ToList());
	}

	[TestMethod]
	public async Task Preview_IdentificationNumberAlreadyStored_ReportsAsDuplicate()
	{
		// Arrange
		var customers = _factory.ScopedServices.GetRequiredService<ICustomerServiceProvider>();
		await customers.AddCustomerAsync(new Customer { CustomerId = 1001, Name = "Ann", IdentificationNumber = "ID-1" });

		var content = ExcelFileContent(ImportTarget.Customers, CreateWorkbook("Customers", ["Name", "IdentificationNumber"], ["Ann again", "ID-1"]));

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		var batch = await response.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(0, batch.ReadyCount);
		Assert.AreEqual(1, batch.DuplicateCount);
	}

	[TestMethod]
	public async Task PreviewThenCommit_ReadyCustomers_WritesThemAndAssignsCustomerIds()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.Customers, CreateWorkbook("Customers", ["Name"], ["Ann"], ["Bo"]));
		var previewResponse = await _client.PostAsync("/api/import/batches", content);
		var previewed = await previewResponse.Content.ReadAsAsync<ImportBatchResponse>();

		// Act
		var commitResponse = await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, commitResponse.StatusCode);
		var committed = await commitResponse.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(ImportBatchStatus.Committed, committed.Status);

		var customers = _factory.ScopedServices.GetRequiredService<ICustomerServiceProvider>();
		var stored = (await customers.GetCustomersAsync()).OrderBy(customer => customer.CustomerId).ToList();
		CollectionAssert.AreEqual(new[] { "Ann", "Bo" }, stored.Select(customer => customer.Name).ToList());
		Assert.AreEqual(Customer.FirstCustomerId, stored[0].CustomerId);
		Assert.AreEqual(Customer.FirstCustomerId + 1, stored[1].CustomerId);
	}

	[TestMethod]
	public async Task Commit_AlreadyCommitted_Returns409()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.Customers, CreateWorkbook("Customers", ["Name"], ["Ann"]));
		var previewed = await (await _client.PostAsync("/api/import/batches", content)).Content.ReadAsAsync<ImportBatchResponse>();
		await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Act
		var response = await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
	}

	[TestMethod]
	public async Task Commit_UnknownBatch_Returns404()
	{
		// Act
		var response = await _client.PostAsync($"/api/import/batches/{Guid.NewGuid()}/commit", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task PreviewCommitThenUndo_RemovesTheImportedCustomers()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.Customers, CreateWorkbook("Customers", ["Name"], ["Ann"]));
		var previewed = await (await _client.PostAsync("/api/import/batches", content)).Content.ReadAsAsync<ImportBatchResponse>();
		await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Act
		var undoResponse = await _client.PostAsync($"/api/import/batches/{previewed.Id}/undo", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, undoResponse.StatusCode);
		var undone = await undoResponse.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(ImportBatchStatus.Undone, undone.Status);

		var customers = _factory.ScopedServices.GetRequiredService<ICustomerServiceProvider>();
		CollectionAssert.AreEqual(Array.Empty<Customer>(), (await customers.GetCustomersAsync()).ToList());
	}

	[TestMethod]
	public async Task Undo_NotCommittedYet_Returns409()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.Customers, CreateWorkbook("Customers", ["Name"], ["Ann"]));
		var previewed = await (await _client.PostAsync("/api/import/batches", content)).Content.ReadAsAsync<ImportBatchResponse>();

		// Act
		var response = await _client.PostAsync($"/api/import/batches/{previewed.Id}/undo", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
	}

	[TestMethod]
	public async Task Get_UnknownBatch_Returns404()
	{
		// Act
		var response = await _client.GetAsync($"/api/import/batches/{Guid.NewGuid()}");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task Preview_StockItemsTarget_UsesTheSameGenericPipeline()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.StockItems, CreateWorkbook("Stock", ["Code", "Name", "Amount"], ["A1", "Screw", "10"]));

		// Act
		var previewResponse = await _client.PostAsync("/api/import/batches", content);
		var previewed = await previewResponse.Content.ReadAsAsync<ImportBatchResponse>();
		var commitResponse = await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, commitResponse.StatusCode);
		var stockItems = _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>();
		CollectionAssert.AreEqual(new[] { "A1" }, (await stockItems.GetAllStockItemsAsync()).Select(item => item.Code).ToList());
	}

	[TestMethod]
	public async Task Preview_StockItemCodeAlreadyStored_ReportsAsDuplicate()
	{
		// Arrange
		var stockItems = _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>();
		await stockItems.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10));

		var content = ExcelFileContent(ImportTarget.StockItems, CreateWorkbook("Stock", ["Code", "Name"], ["A1", "Screw again"], ["B2", "Nut"]));

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		var batch = await response.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(1, batch.ReadyCount);
		Assert.AreEqual(1, batch.DuplicateCount);
	}

	[TestMethod]
	public async Task Preview_StockItemRowWithBadNumber_ReportsRowErrorAndSkipsRow()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.StockItems, CreateWorkbook("Stock",
			["Code", "Name", "Amount"],
			["A1", "Screw", "10"],
			["B2", "Nut", "not-a-number"]));

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		var batch = await response.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(1, batch.ReadyCount);
		Assert.AreEqual(1, batch.ErrorCount);
		Assert.AreEqual(3, batch.Rows.Single(row => row.Status == ImportRowStatus.Error).Row);
	}

	[TestMethod]
	public async Task PreviewThenCommit_OpeningStockMatchingExistingCode_ChecksInAndReportsNothingCreated()
	{
		// Arrange
		var stockItems = _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>();
		await stockItems.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 3));

		var content = ExcelFileContent(ImportTarget.OpeningStock, CreateWorkbook("Opening", ["Code", "Amount"], ["A1", "10"]));
		var previewResponse = await _client.PostAsync("/api/import/batches", content);
		var previewed = await previewResponse.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(1, previewed.ReadyCount);

		// Act
		var commitResponse = await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, commitResponse.StatusCode);
		var item = await (await _client.GetAsync("/api/stock-items/A1")).Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual(13, item.Amount);

		var allItems = await stockItems.GetAllStockItemsAsync();
		Assert.AreEqual(1, allItems.Count());
	}

	[TestMethod]
	public async Task Preview_OpeningStockCodeNotAnExistingStockItem_ReportsAsDuplicate()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.OpeningStock, CreateWorkbook("Opening", ["Code", "Amount"], ["Unknown", "10"]));

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		var batch = await response.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(0, batch.ReadyCount);
		Assert.AreEqual(1, batch.DuplicateCount);
	}

	[TestMethod]
	public async Task PreviewCommitThenUndo_OpeningStock_ChecksTheAmountBackOut()
	{
		// Arrange
		var stockItems = _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>();
		await stockItems.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 3));

		var content = ExcelFileContent(ImportTarget.OpeningStock, CreateWorkbook("Opening", ["Code", "Amount"], ["A1", "10"]));
		var previewed = await (await _client.PostAsync("/api/import/batches", content)).Content.ReadAsAsync<ImportBatchResponse>();
		await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Act
		var undoResponse = await _client.PostAsync($"/api/import/batches/{previewed.Id}/undo", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, undoResponse.StatusCode);
		var item = await (await _client.GetAsync("/api/stock-items/A1")).Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual(3, item.Amount);
	}

	[TestMethod]
	public async Task PreviewThenCommit_OpenInvoiceWithAmountPaid_CreatesInvoiceWithSeededPayment()
	{
		// Arrange
		var customers = _factory.ScopedServices.GetRequiredService<ICustomerServiceProvider>();
		await customers.AddCustomerAsync(new Customer { CustomerId = 1001, Name = "Ann", IdentificationNumber = "ID-1" });

		var content = ExcelFileContent(ImportTarget.OpenInvoices, CreateWorkbook("OpenInvoices",
			["CustomerIdentificationNumber", "Number", "Date", "ExpirationDate", "Total", "Tax", "AmountPaid"],
			["ID-1", "500", "2026-01-01", "2026-02-01", "1000", "100", "400"]));
		var previewResponse = await _client.PostAsync("/api/import/batches", content);
		var previewed = await previewResponse.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(1, previewed.ReadyCount);

		// Act
		var commitResponse = await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, commitResponse.StatusCode);
		var invoice = await (await _client.GetAsync("/api/invoices/500")).Content.ReadAsAsync<InvoiceResponse>();
		Assert.AreEqual(1000, invoice.Total);
		Assert.AreEqual(400, invoice.AmountPaid);
		Assert.AreEqual(600, invoice.AmountDue);
		Assert.AreEqual(SaleCondition.Credit, invoice.SaleCondition);
	}

	[TestMethod]
	public async Task Preview_OpenInvoiceCustomerNotFound_ReportsAsDuplicate()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.OpenInvoices, CreateWorkbook("OpenInvoices",
			["CustomerIdentificationNumber", "Number", "Date", "ExpirationDate", "Total", "Tax", "AmountPaid"],
			["Unknown", "500", "2026-01-01", "2026-02-01", "1000", "100", "0"]));

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		var batch = await response.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(0, batch.ReadyCount);
		Assert.AreEqual(1, batch.DuplicateCount);
	}

	[TestMethod]
	public async Task PreviewCommitThenUndo_OpenInvoice_DeletesTheInvoice()
	{
		// Arrange
		var customers = _factory.ScopedServices.GetRequiredService<ICustomerServiceProvider>();
		await customers.AddCustomerAsync(new Customer { CustomerId = 1001, Name = "Ann", IdentificationNumber = "ID-1" });

		var content = ExcelFileContent(ImportTarget.OpenInvoices, CreateWorkbook("OpenInvoices",
			["CustomerIdentificationNumber", "Number", "Date", "ExpirationDate", "Total", "Tax", "AmountPaid"],
			["ID-1", "500", "2026-01-01", "2026-02-01", "1000", "100", "0"]));
		var previewed = await (await _client.PostAsync("/api/import/batches", content)).Content.ReadAsAsync<ImportBatchResponse>();
		await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Act
		var undoResponse = await _client.PostAsync($"/api/import/batches/{previewed.Id}/undo", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, undoResponse.StatusCode);
		Assert.AreEqual(HttpStatusCode.NotFound, (await _client.GetAsync("/api/invoices/500")).StatusCode);
	}

	[TestMethod]
	public async Task GetFields_Customers_ReturnsNamePropertyField()
	{
		// Act
		var response = await _client.GetAsync("/api/import/fields?Target=Customers");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var fields = await response.Content.ReadAsAsync<List<ImportField>>();
		Assert.IsTrue(fields.Any(field => field.Name == nameof(Customer.Name)));
	}

	[TestMethod]
	public async Task DetectColumns_UnrecognizedHeader_ReportsColumnWithNoMatchedField()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.Customers, CreateWorkbook("Customers", ["Name", "Unrecognized"], ["Ann", "?"]));

		// Act
		var response = await _client.PostAsync("/api/import/batches/columns", content);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var detected = await response.Content.ReadAsAsync<DetectedColumnsResponse>();
		Assert.AreEqual(nameof(Customer.Name), detected.Columns.Single(column => column.Column == 1).MatchedFieldName);
		Assert.IsNull(detected.Columns.Single(column => column.Column == 2).MatchedFieldName);
		Assert.IsTrue(detected.Fields.Any(field => field.Name == nameof(Customer.Name)));
	}

	[TestMethod]
	public async Task Preview_WithColumnMapping_UsesItInsteadOfHeaderNames()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.Customers, CreateWorkbook("Customers", ["Vorname"], ["Ann"]));
		content.Add(new StringContent(JsonSerializer.Serialize(new Dictionary<int, string> { [1] = nameof(Customer.Name) })), "Mapping");

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		var batch = await response.Content.ReadAsAsync<ImportBatchResponse>();
		Assert.AreEqual(1, batch.ReadyCount);
	}

	[TestMethod]
	public async Task GetReport_BatchWithDuplicateAndErrorRows_ReturnsCsvOfNonReadyRows()
	{
		// Arrange
		var stockItems = _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>();
		await stockItems.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10));

		var content = ExcelFileContent(ImportTarget.StockItems, CreateWorkbook("Stock",
			["Code", "Name", "Amount"],
			["A1", "Screw again", "5"],
			["B2", "Nut", "not-a-number"]));
		var previewed = await (await _client.PostAsync("/api/import/batches", content)).Content.ReadAsAsync<ImportBatchResponse>();

		// Act
		var response = await _client.GetAsync($"/api/import/batches/{previewed.Id}/report");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		Assert.AreEqual("text/csv", response.Content.Headers.ContentType!.MediaType);
		var csv = await response.Content.ReadAsStringAsync();
		var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
		Assert.AreEqual("Row,Status,Message,Data", lines[0]);
		Assert.AreEqual(2, lines.Length - 1);
		StringAssert.Contains(lines[1], "Duplicate");
		StringAssert.Contains(lines[2], "Error");
	}

	[TestMethod]
	public async Task GetReport_UnknownBatch_Returns404()
	{
		// Act
		var response = await _client.GetAsync($"/api/import/batches/{Guid.NewGuid()}/report");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task Preview_NoFile_Returns400()
	{
		// Arrange
		using var content = new MultipartFormDataContent { { new StringContent(nameof(ImportTarget.Customers)), "Target" } };

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "fileRequired");
	}

	[TestMethod]
	public async Task Preview_FileTooLarge_Returns400()
	{
		// Arrange
		using var content = new MultipartFormDataContent { { new StringContent(nameof(ImportTarget.Customers)), "Target" } };
		var oversized = new ByteArrayContent(new byte[ImportFileValidation.MaxFileSizeBytes + 1]);
		oversized.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
		content.Add(oversized, "File", "legacy.xlsx");

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "fileTooLarge");
	}

	[TestMethod]
	public async Task Preview_DisallowedFileType_Returns400()
	{
		// Arrange
		using var content = new MultipartFormDataContent { { new StringContent(nameof(ImportTarget.Customers)), "Target" } };
		var disguised = new ByteArrayContent([0x4D, 0x5A]);
		disguised.Headers.ContentType = new MediaTypeHeaderValue("application/x-msdownload");
		content.Add(disguised, "File", "legacy.exe");

		// Act
		var response = await _client.PostAsync("/api/import/batches", content);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "fileTypeNotAllowed");
	}

	[TestMethod]
	public async Task Preview_NoToken_ReturnsUnauthorized()
	{
		// Arrange
		using var anonymousClient = _factory.CreateClient();
		var content = ExcelFileContent(ImportTarget.Customers, CreateWorkbook("Customers", ["Name"], ["Ann"]));

		// Act
		var response = await anonymousClient.PostAsync("/api/import/batches", content);

		// Assert
		Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[TestMethod]
	public async Task Preview_CustomersWriteOnly_CannotPreviewStockItemsTarget()
	{
		// Arrange
		var client = await this.CreateStandardUserClientAsync("customers-only", [Permission.CustomersWrite]);
		var content = ExcelFileContent(ImportTarget.StockItems, CreateWorkbook("Stock", ["Code", "Name", "Amount"], ["A1", "Screw", "10"]));

		// Act
		var response = await client.PostAsync("/api/import/batches", content);

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[TestMethod]
	public async Task Commit_CustomersWriteOnly_CannotCommitStockItemsBatch()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.StockItems, CreateWorkbook("Stock", ["Code", "Name", "Amount"], ["A1", "Screw", "10"]));
		var previewed = await (await _client.PostAsync("/api/import/batches", content)).Content.ReadAsAsync<ImportBatchResponse>();
		var client = await this.CreateStandardUserClientAsync("customers-only2", [Permission.CustomersWrite]);

		// Act
		var response = await client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[TestMethod]
	public async Task Undo_CustomersWriteOnly_CannotUndoStockItemsBatch()
	{
		// Arrange
		var content = ExcelFileContent(ImportTarget.StockItems, CreateWorkbook("Stock", ["Code", "Name", "Amount"], ["A1", "Screw", "10"]));
		var previewed = await (await _client.PostAsync("/api/import/batches", content)).Content.ReadAsAsync<ImportBatchResponse>();
		await _client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);
		var client = await this.CreateStandardUserClientAsync("customers-only3", [Permission.CustomersWrite]);

		// Act
		var response = await client.PostAsync($"/api/import/batches/{previewed.Id}/undo", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[TestMethod]
	public async Task PreviewThenCommit_StockItemsWriteOnly_CanImportStockItems()
	{
		// Arrange
		var client = await this.CreateStandardUserClientAsync("stockitems-only", [Permission.StockItemsWrite]);
		var content = ExcelFileContent(ImportTarget.StockItems, CreateWorkbook("Stock", ["Code", "Name", "Amount"], ["A1", "Screw", "10"]));

		// Act
		var previewResponse = await client.PostAsync("/api/import/batches", content);
		var previewed = await previewResponse.Content.ReadAsAsync<ImportBatchResponse>();
		var commitResponse = await client.PostAsync($"/api/import/batches/{previewed.Id}/commit", null);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, previewResponse.StatusCode);
		Assert.AreEqual(HttpStatusCode.OK, commitResponse.StatusCode);
	}


	private async Task<HttpClient> CreateStandardUserClientAsync(string username, IReadOnlyList<string> permissions)
	{
		const string password = "s3cret!23";
		await _client.PostAsJsonAsync("/api/users", new CreateUserRequest(username, password, Permissions: permissions));
		return await _factory.CreateAuthenticatedClientAsync(username, password);
	}

	private static MultipartFormDataContent ExcelFileContent(ImportTarget target, XLWorkbook workbook)
	{
		using var stream = new MemoryStream();
		workbook.SaveAs(stream);

		var content = new MultipartFormDataContent
		{
			{ new StringContent(target.ToString()), "Target" }
		};
		var fileContent = new ByteArrayContent(stream.ToArray());
		fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
		content.Add(fileContent, "File", "legacy.xlsx");
		return content;
	}

	private static XLWorkbook CreateWorkbook(string sheetName, string[] headers, params string[][] rows)
	{
		var workbook = new XLWorkbook();
		var worksheet = workbook.AddWorksheet(sheetName);

		for (var column = 0; column < headers.Length; column++)
		{
			worksheet.Cell(1, column + 1).Value = headers[column];
		}

		for (var row = 0; row < rows.Length; row++)
		{
			for (var column = 0; column < rows[row].Length; column++)
			{
				worksheet.Cell(row + 2, column + 1).Value = rows[row][column];
			}
		}

		return workbook;
	}
}
