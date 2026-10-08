using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.Invoices;
using StockManagement.Api.Features.Payments;
using StockManagement.Api.Features.Sales;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests;


/// <remarks>
/// No Bancard sandbox is reachable from this test suite (<c>Bancard:BaseUrl</c> is unset here, same as production
/// appsettings until an operator configures it), so the happy path (gateway returns a real QR) is covered at the
/// <see cref="StockManagement.Tests.Sales.PaymentLinkServiceTests"/> unit level instead; these cover the
/// misconfigured-gateway and not-found/already-paid paths the endpoint itself is responsible for.
/// </remarks>
[TestClass]
public sealed class PaymentLinkEndpointsTests
{
	private ApiFactory _factory;
	private HttpClient _client;
	private Customer _customer;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_factory = new();
		_client = await _factory.CreateAuthenticatedClientAsync();

		await _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>().AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10, price: 5000));
		_customer = new Customer() { CustomerId = 1001, Name = "Ana" };
		await _factory.ScopedServices.GetRequiredService<ICustomerServiceProvider>().AddCustomerAsync(_customer);
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}

	[TestMethod]
	public async Task CreatePaymentLink_UnknownInvoice_Returns404()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/invoices/999/payment-link", new CreatePaymentLinkRequest("999"), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CreatePaymentLink_InvoiceFullyPaid_Returns409()
	{
		// Arrange
		var invoiceNumber = await this.CreateCreditSaleAsync();
		await _client.PostAsJsonAsync($"/api/invoices/{invoiceNumber}/payments", new CreatePaymentRequest(invoiceNumber, 15000, PaymentMethod.Cash, null), ApiFactory.JsonOptions);

		// Act
		var response = await _client.PostAsJsonAsync($"/api/invoices/{invoiceNumber}/payment-link", new CreatePaymentLinkRequest(invoiceNumber), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
		var conflict = await response.Content.ReadAsAsync<PaymentLinkRejectedResponse>();
		Assert.AreEqual("invoiceAlreadyPaid", conflict.Reason);
	}

	[TestMethod]
	public async Task CreatePaymentLink_GatewayNotConfigured_Returns409AndWritesNoLink()
	{
		// Arrange
		var invoiceNumber = await this.CreateCreditSaleAsync();

		// Act
		var response = await _client.PostAsJsonAsync($"/api/invoices/{invoiceNumber}/payment-link", new CreatePaymentLinkRequest(invoiceNumber), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
		var conflict = await response.Content.ReadAsAsync<PaymentLinkRejectedResponse>();
		Assert.AreEqual("paymentLinkGatewayError", conflict.Reason);

		var getResponse = await _client.GetAsync($"/api/invoices/{invoiceNumber}/payment-link");
		Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
	}

	[TestMethod]
	public async Task GetPaymentLink_NoneCreated_Returns404()
	{
		// Arrange
		var invoiceNumber = await this.CreateCreditSaleAsync();

		// Act
		var response = await _client.GetAsync($"/api/invoices/{invoiceNumber}/payment-link");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task BancardWebhook_UnknownShopProcessId_Returns200()
	{
		// Act - always 200 regardless of match (#255): a distinguishable not-found would let an unauthenticated
		// caller enumerate valid shop_process_id values.
		var response = await _client.PostAsJsonAsync("/api/webhooks/bancard", new BancardWebhookRequest("does-not-exist"), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
	}

	/// <returns>Number of a stored 15000-total credit invoice for <see cref="_customer"/></returns>
	private async Task<string> CreateCreditSaleAsync()
	{
		var response = await _client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(_customer.CustomerId, SaleCondition.Credit, [new("A1", 3)]), ApiFactory.JsonOptions);
		var invoice = await response.Content.ReadAsAsync<InvoiceResponse>();
		return invoice.Number;
	}
}
