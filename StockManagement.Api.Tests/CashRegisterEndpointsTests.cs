using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.CashRegister;
using StockManagement.Api.Features.Invoices;
using StockManagement.Api.Features.Payments;
using StockManagement.Api.Features.Sales;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class CashRegisterEndpointsTests
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
	public async Task OpenSession_NoneOpen_Returns201WithExpectedEqualToFloat()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/cash-register/sessions", new OpenCashRegisterSessionRequest(10000), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
		var session = await response.Content.ReadAsAsync<CashRegisterSessionResponse>();
		Assert.AreEqual(10000, session.OpeningFloat);
		Assert.AreEqual(10000, session.ExpectedAmount);
		Assert.AreEqual(CashRegisterSessionStatus.Open, session.Status);
	}

	[TestMethod]
	public async Task OpenSession_AlreadyOpen_Returns409()
	{
		// Arrange
		await this.OpenSessionAsync(10000);

		// Act
		var response = await _client.PostAsJsonAsync("/api/cash-register/sessions", new OpenCashRegisterSessionRequest(5000), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
		var conflict = await response.Content.ReadAsAsync<CashRegisterConflictResponse>();
		Assert.AreEqual("cashRegisterSessionAlreadyOpen", conflict.Reason);
	}

	[TestMethod]
	public async Task OpenSession_NegativeFloat_Returns400()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/cash-register/sessions", new OpenCashRegisterSessionRequest(-1), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[TestMethod]
	public async Task GetCurrentSession_NoneOpen_Returns404()
	{
		// Act
		var response = await _client.GetAsync("/api/cash-register/sessions/current");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateMovement_NoOpenSession_Returns404()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/cash-register/sessions/anything/movements", new CreateCashMovementRequest("anything", CashMovementType.In, 100, "float"), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateMovement_ValidCashIn_IncreasesExpectedAmount()
	{
		// Arrange
		var session = await this.OpenSessionAsync(10000);

		// Act
		var response = await _client.PostAsJsonAsync($"/api/cash-register/sessions/{session.Id}/movements", new CreateCashMovementRequest(session.Id, CashMovementType.In, 2000, "petty cash refill"), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
		var current = await _client.GetFromJsonAsync<CashRegisterSessionResponse>("/api/cash-register/sessions/current", ApiFactory.JsonOptions);
		Assert.AreEqual(12000, current.ExpectedAmount);
		Assert.AreEqual(1, current.Movements.Count);
	}

	[TestMethod]
	public async Task CreateMovement_CashOut_DecreasesExpectedAmount()
	{
		// Arrange
		var session = await this.OpenSessionAsync(10000);

		// Act
		await _client.PostAsJsonAsync($"/api/cash-register/sessions/{session.Id}/movements", new CreateCashMovementRequest(session.Id, CashMovementType.Out, 3000, "bank deposit"), ApiFactory.JsonOptions);

		// Assert
		var current = await _client.GetFromJsonAsync<CashRegisterSessionResponse>("/api/cash-register/sessions/current", ApiFactory.JsonOptions);
		Assert.AreEqual(7000, current.ExpectedAmount);
	}

	[TestMethod]
	public async Task CreateMovement_NonPositiveAmount_Returns400()
	{
		// Arrange
		var session = await this.OpenSessionAsync(10000);

		// Act
		var response = await _client.PostAsJsonAsync($"/api/cash-register/sessions/{session.Id}/movements", new CreateCashMovementRequest(session.Id, CashMovementType.In, 0, "reason"), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[TestMethod]
	public async Task CashSalePayment_DuringOpenSession_CountsTowardExpectedAmount()
	{
		// Arrange
		var session = await this.OpenSessionAsync(10000);
		var saleResponse = await _client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(_customer.CustomerId, SaleCondition.Credit, [new("A1", 1)]), ApiFactory.JsonOptions);
		var invoice = await saleResponse.Content.ReadAsAsync<InvoiceResponse>();

		// Act
		await _client.PostAsJsonAsync($"/api/invoices/{invoice.Number}/payments", new CreatePaymentRequest(invoice.Number, 5000, PaymentMethod.Cash, null), ApiFactory.JsonOptions);

		// Assert
		var current = await _client.GetFromJsonAsync<CashRegisterSessionResponse>("/api/cash-register/sessions/current", ApiFactory.JsonOptions);
		Assert.AreEqual(15000, current.ExpectedAmount);
	}

	[TestMethod]
	public async Task CloseSession_ComputesDifferenceAndClosesSession()
	{
		// Arrange
		var session = await this.OpenSessionAsync(10000);
		await _client.PostAsJsonAsync($"/api/cash-register/sessions/{session.Id}/movements", new CreateCashMovementRequest(session.Id, CashMovementType.In, 1000, "top-up"), ApiFactory.JsonOptions);

		// Act
		var response = await _client.PostAsJsonAsync($"/api/cash-register/sessions/{session.Id}/close", new CloseCashRegisterSessionRequest(session.Id, 10500, "short by 500"), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var report = await response.Content.ReadAsAsync<CashRegisterCloseReportResponse>();
		Assert.AreEqual(11000, report.ExpectedAmount);
		Assert.AreEqual(10500, report.CountedAmount);
		Assert.AreEqual(-500, report.Difference);
		Assert.AreEqual(CashRegisterSessionStatus.Closed, report.Session.Status);

		var currentResponse = await _client.GetAsync("/api/cash-register/sessions/current");
		Assert.AreEqual(HttpStatusCode.NotFound, currentResponse.StatusCode);
	}

	[TestMethod]
	public async Task CloseSession_NoOpenSession_Returns404()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/cash-register/sessions/none/close", new CloseCashRegisterSessionRequest("none", 100, null), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task ListSessions_AfterOpeningAndClosing_ReturnsSessionNewestFirst()
	{
		// Arrange
		var first = await this.OpenSessionAsync(1000);
		await _client.PostAsJsonAsync($"/api/cash-register/sessions/{first.Id}/close", new CloseCashRegisterSessionRequest(first.Id, 1000, null), ApiFactory.JsonOptions);
		var second = await this.OpenSessionAsync(2000);

		// Act
		var result = await _client.GetFromJsonAsync<CashRegisterSessionListResponse>("/api/cash-register/sessions", ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(2, result.Items.Count);
		Assert.AreEqual(second.Id, result.Items[0].Id);
		Assert.AreEqual(first.Id, result.Items[1].Id);
	}

	private async Task<CashRegisterSessionResponse> OpenSessionAsync(decimal openingFloat)
	{
		var response = await _client.PostAsJsonAsync("/api/cash-register/sessions", new OpenCashRegisterSessionRequest(openingFloat), ApiFactory.JsonOptions);
		return await response.Content.ReadAsAsync<CashRegisterSessionResponse>();
	}
}
