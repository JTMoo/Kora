using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.Payments;
using StockManagement.Api.Features.Reports;
using StockManagement.Api.Features.SupplierInvoices;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class SupplierInvoiceEndpointsTests
{
	private ApiFactory _factory;
	private HttpClient _client;
	private Supplier _supplier;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_factory = new();
		_client = await _factory.CreateAuthenticatedClientAsync();

		_supplier = new Supplier("Acme Imports");
		await _factory.ScopedServices.GetRequiredService<ISupplierServiceProvider>().AddSupplierAsync(_supplier);
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}


	[TestMethod]
	public async Task CreateSupplierInvoice_UnknownSupplier_Returns404()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/supplier-invoices", new CreateSupplierInvoiceRequest("SI-1", "unknown", DateTime.Now, DateTime.Now.AddDays(30), 1000));

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateSupplierInvoice_DuplicateNumber_Returns409()
	{
		// Arrange
		await this.CreateSupplierInvoiceAsync("SI-1", 1000);

		// Act
		var response = await _client.PostAsJsonAsync("/api/supplier-invoices", new CreateSupplierInvoiceRequest("SI-1", _supplier.Id, DateTime.Now, DateTime.Now.AddDays(30), 500));

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
		var conflict = await response.Content.ReadAsAsync<DuplicateSupplierInvoiceNumberResponse>();
		Assert.AreEqual("SI-1", conflict.Number);
	}

	[TestMethod]
	public async Task GetSupplierInvoice_JustCreated_HasFullAmountDue()
	{
		// Arrange
		await this.CreateSupplierInvoiceAsync("SI-1", 1000);

		// Act
		var invoice = await (await _client.GetAsync("/api/supplier-invoices/SI-1")).Content.ReadAsAsync<SupplierInvoiceResponse>();

		// Assert
		Assert.AreEqual(1000, invoice.Total);
		Assert.AreEqual(0, invoice.AmountPaid);
		Assert.AreEqual(1000, invoice.AmountDue);
		Assert.AreEqual(SupplierInvoiceStatus.Open, invoice.Status);
		Assert.AreEqual(_supplier.Id, invoice.SupplierId);
	}

	[TestMethod]
	public async Task GetSupplierInvoice_Unknown_Returns404()
	{
		// Act
		var response = await _client.GetAsync("/api/supplier-invoices/unknown");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task ListSupplierInvoices_FiltersBySupplier()
	{
		// Arrange
		var otherSupplier = new Supplier("Other Co");
		await _factory.ScopedServices.GetRequiredService<ISupplierServiceProvider>().AddSupplierAsync(otherSupplier);
		await this.CreateSupplierInvoiceAsync("SI-1", 1000);
		await _client.PostAsJsonAsync("/api/supplier-invoices", new CreateSupplierInvoiceRequest("SI-2", otherSupplier.Id, DateTime.Now, DateTime.Now.AddDays(30), 500));

		// Act
		var result = await (await _client.GetAsync($"/api/supplier-invoices?supplierId={_supplier.Id}")).Content.ReadAsAsync<SupplierInvoiceListResponse>();

		// Assert
		Assert.AreEqual(1, result.Items.Count);
		Assert.AreEqual("SI-1", result.Items.Single().Number);
	}

	[TestMethod]
	public async Task CreatePayment_PartialAmount_Returns201AndInvoiceIsPartiallyPaid()
	{
		// Arrange
		await this.CreateSupplierInvoiceAsync("SI-1", 1000);

		// Act
		var response = await _client.PostAsJsonAsync("/api/supplier-invoices/SI-1/payments", new { Amount = 400m, Method = PaymentMethod.Cash });

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
		var invoice = await (await _client.GetAsync("/api/supplier-invoices/SI-1")).Content.ReadAsAsync<SupplierInvoiceResponse>();
		Assert.AreEqual(400, invoice.AmountPaid);
		Assert.AreEqual(600, invoice.AmountDue);
		Assert.AreEqual(SupplierInvoiceStatus.PartiallyPaid, invoice.Status);
	}

	[TestMethod]
	public async Task CreatePayment_ExceedsAmountDue_Returns409AndWritesNothing()
	{
		// Arrange
		await this.CreateSupplierInvoiceAsync("SI-1", 1000);

		// Act
		var response = await _client.PostAsJsonAsync("/api/supplier-invoices/SI-1/payments", new { Amount = 2000m, Method = PaymentMethod.Cash });

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
		var conflict = await response.Content.ReadAsAsync<PaymentRejectedResponse>();
		Assert.AreEqual("paymentExceedsAmountDue", conflict.Reason);
	}

	[TestMethod]
	public async Task GetAccountsPayableAging_NotYetDue_BucketedAsCurrent()
	{
		// Arrange
		await this.CreateSupplierInvoiceAsync("SI-1", 1000);

		// Act
		var response = await _client.GetAsync("/api/reports/accounts-payable-aging");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadAsAsync<AccountsPayableAgingResponse>();
		Assert.AreEqual(1000m, body.Totals.Current);
		Assert.AreEqual(1000m, body.Totals.Total);
		var row = body.Items.Single();
		Assert.AreEqual(_supplier.Name, row.SupplierName);
	}

	[TestMethod]
	public async Task GetAccountsPayableAging_FullyPaidInvoice_ExcludedFromTotals()
	{
		// Arrange
		await this.CreateSupplierInvoiceAsync("SI-1", 1000);
		await _client.PostAsJsonAsync("/api/supplier-invoices/SI-1/payments", new { Amount = 1000m, Method = PaymentMethod.Cash });

		// Act
		var response = await _client.GetAsync("/api/reports/accounts-payable-aging");

		// Assert
		var body = await response.Content.ReadAsAsync<AccountsPayableAgingResponse>();
		Assert.AreEqual(0m, body.Totals.Total);
		Assert.IsFalse(body.Items.Any());
	}

	private async Task CreateSupplierInvoiceAsync(string number, decimal total)
	{
		var response = await _client.PostAsJsonAsync("/api/supplier-invoices", new CreateSupplierInvoiceRequest(number, _supplier.Id, DateTime.Now, DateTime.Now.AddDays(30), total));
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
	}
}
