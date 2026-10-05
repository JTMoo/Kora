using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.Invoices;
using StockManagement.Api.Features.RemissionNotes;
using StockManagement.Api.Features.Sales;
using StockManagement.Api.Features.Settings;
using StockManagement.Api.Features.Sifen;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests;


/// <summary>
/// KuDE (printable representation) endpoints (#183, ADR-0035) - require a CDC to already exist, which contingency
/// mode (#149/#188) assigns synchronously, same setup <see cref="RemissionNoteEndpointsTests"/> uses.
/// </summary>
[TestClass]
public sealed class KudeEndpointsTests
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

		await _client.PutAsJsonAsync("/api/company-settings", new UpdateCompanySettingsRequest("Kora", "", "PYG", 10m, 30, 1, 1001, 0,
			Ruc: "1946520-3", TimbradoNumber: "12345678", TimbradoValidFrom: new DateTime(2026, 1, 1), EstablishmentAddress: "Avda. Mcal. López 1234"), ApiFactory.JsonOptions);
		await _client.PutAsJsonAsync("/api/sifen/contingency-range", new SetContingencyRangeRequest(1, 100), ApiFactory.JsonOptions);
		await _client.PostAsJsonAsync("/api/sifen/contingency-mode", new SetContingencyModeRequest(true), ApiFactory.JsonOptions);
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}


	[TestMethod]
	public async Task GetInvoiceKude_CdcAssigned_Returns200Html()
	{
		// Arrange
		var created = await _client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(1001, SaleCondition.Cash, [new("A1", 2)]), ApiFactory.JsonOptions);
		var invoice = await created.Content.ReadAsAsync<InvoiceResponse>();

		// Act
		var response = await _client.GetAsync($"/api/invoices/{invoice.Number}/kude");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
		var html = await response.Content.ReadAsStringAsync();
		StringAssert.Contains(html, "Ana");
		StringAssert.Contains(html, "Screw");
	}

	[TestMethod]
	public async Task GetRemissionNoteKude_CdcAssigned_Returns200Html()
	{
		// Arrange
		var created = await _client.PostAsJsonAsync("/api/remission-notes", new CreateRemissionNoteRequest(1001, RemissionReason.Venta, "Avda. España 500", [new("A1", 1)]), ApiFactory.JsonOptions);
		var remissionNote = await created.Content.ReadAsAsync<RemissionNoteResponse>();

		// Act
		var response = await _client.GetAsync($"/api/remission-notes/{remissionNote.Number}/kude");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var html = await response.Content.ReadAsStringAsync();
		StringAssert.Contains(html, "Ana");
		StringAssert.Contains(html, "Screw");
	}

	[TestMethod]
	public async Task GetInvoiceKude_NoCdcYet_Returns409()
	{
		// Arrange: contingency off, so the sale leaves Invoice.Cdc empty until the outbox worker transmits
		await _client.PostAsJsonAsync("/api/sifen/contingency-mode", new SetContingencyModeRequest(false), ApiFactory.JsonOptions);
		var created = await _client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(1001, SaleCondition.Cash, [new("A1", 1)]), ApiFactory.JsonOptions);
		var invoice = await created.Content.ReadAsAsync<InvoiceResponse>();

		// Act
		var response = await _client.GetAsync($"/api/invoices/{invoice.Number}/kude");

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
	}

	[TestMethod]
	public async Task GetInvoiceKude_UnknownNumber_Returns404()
	{
		// Act
		var response = await _client.GetAsync("/api/invoices/999-999-9999999/kude");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}
}
