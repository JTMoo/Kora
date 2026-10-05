using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.DebitNotes;
using StockManagement.Api.Features.Invoices;
using StockManagement.Api.Features.Sales;
using StockManagement.Api.Features.Settings;
using StockManagement.Api.Features.Sifen;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests;


/// <summary>
/// SIFEN Nota de Débito Electrónica endpoints (#184) - increases a previously issued invoice, never changes stock.
/// </summary>
[TestClass]
public sealed class DebitNoteEndpointsTests
{
	private ApiFactory _factory;
	private HttpClient _client;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_factory = new();
		_client = await _factory.CreateAuthenticatedClientAsync();

		await _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>().AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10, price: 5000) { VatRatePercent = 10m });
		await _factory.ScopedServices.GetRequiredService<ICustomerServiceProvider>().AddCustomerAsync(new Customer() { CustomerId = 1001, Name = "Ana" });
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}


	/// <summary>
	/// Contingency mode (same as <c>RemissionNoteEndpointsTests</c>) issues the invoice's Cdc immediately, so the
	/// debit note has something to reference without needing the background transmission worker to run.
	/// </summary>
	private async Task<string> CreateTransmittedInvoiceAsync()
	{
		await _client.PutAsJsonAsync("/api/company-settings", new UpdateCompanySettingsRequest("Kora", "", "PYG", 10m, 30, 1, 1001, 0, Ruc: "1946520-3"), ApiFactory.JsonOptions);
		await _client.PutAsJsonAsync("/api/sifen/contingency-range", new SetContingencyRangeRequest(1, 100), ApiFactory.JsonOptions);
		await _client.PostAsJsonAsync("/api/sifen/contingency-mode", new SetContingencyModeRequest(true), ApiFactory.JsonOptions);

		var response = await _client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(1001, SaleCondition.Cash, [new("A1", 1)]), ApiFactory.JsonOptions);
		var invoice = await response.Content.ReadAsAsync<InvoiceResponse>();
		return invoice.Number;
	}

	[TestMethod]
	public async Task CreateDebitNote_ValidRequest_Returns201AndDoesNotChangeStock()
	{
		// Arrange
		var invoiceNumber = await CreateTransmittedInvoiceAsync();

		// Act
		var response = await _client.PostAsJsonAsync("/api/debit-notes", new CreateDebitNoteRequest(invoiceNumber, "Intereses por mora", [new("Intereses por mora", 11000m, 10)]), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
		var debitNote = await response.Content.ReadAsAsync<DebitNoteResponse>();
		Assert.AreEqual(invoiceNumber, debitNote.InvoiceNumber);
		Assert.AreEqual("Intereses por mora", debitNote.Reason);
		Assert.AreEqual(11000m, debitNote.Total);
		Assert.AreEqual(1000m, debitNote.Tax);
		Assert.AreEqual(TransmissionStatus.Pending, debitNote.TransmissionStatus);

		var stockItem = await _client.GetFromJsonAsync<StockManagement.Api.Features.StockItems.StockItemResponse>("/api/stock-items/A1", ApiFactory.JsonOptions);
		Assert.AreEqual(9, stockItem.Amount);
	}

	[TestMethod]
	public async Task CreateDebitNote_Stored_CanBeReadBack()
	{
		// Arrange
		var invoiceNumber = await CreateTransmittedInvoiceAsync();
		var created = await _client.PostAsJsonAsync("/api/debit-notes", new CreateDebitNoteRequest(invoiceNumber, "Correction", [new("line", 500m, 5)]), ApiFactory.JsonOptions);

		// Act
		var response = await _client.GetAsync(created.Headers.Location);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var debitNote = await response.Content.ReadAsAsync<DebitNoteResponse>();
		Assert.AreEqual("Correction", debitNote.Reason);
		Assert.AreEqual("line", debitNote.Lines.Single().Description);
	}

	[TestMethod]
	public async Task ListDebitNotes_OneStored_IncludesIt()
	{
		// Arrange
		var invoiceNumber = await CreateTransmittedInvoiceAsync();
		await _client.PostAsJsonAsync("/api/debit-notes", new CreateDebitNoteRequest(invoiceNumber, "reason", [new("line", 500m, 5)]), ApiFactory.JsonOptions);

		// Act
		var response = await _client.GetFromJsonAsync<DebitNoteListResponse>("/api/debit-notes", ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(1, response.Items.Count);
	}

	[TestMethod]
	public async Task CreateDebitNote_UnknownInvoice_Returns404()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/debit-notes", new CreateDebitNoteRequest("999-999-9999999", "reason", [new("line", 500m, 5)]), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateDebitNote_InvoiceNotTransmitted_Returns422()
	{
		// Arrange: no contingency mode, no transmission worker running - invoice.Cdc stays empty
		var saleResponse = await _client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(1001, SaleCondition.Cash, [new("A1", 1)]), ApiFactory.JsonOptions);
		var invoice = await saleResponse.Content.ReadAsAsync<InvoiceResponse>();

		// Act
		var response = await _client.PostAsJsonAsync("/api/debit-notes", new CreateDebitNoteRequest(invoice.Number, "reason", [new("line", 500m, 5)]), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateDebitNote_NoItems_Returns400()
	{
		// Arrange
		var invoiceNumber = await CreateTransmittedInvoiceAsync();

		// Act
		var response = await _client.PostAsJsonAsync("/api/debit-notes", new CreateDebitNoteRequest(invoiceNumber, "reason", []), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "itemsRequired");
	}

	[TestMethod]
	public async Task GetDebitNote_UnknownNumber_Returns404()
	{
		// Act
		var response = await _client.GetAsync("/api/debit-notes/99");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}
}
