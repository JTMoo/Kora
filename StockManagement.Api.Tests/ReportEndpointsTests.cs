using System.Net;
using System.Net.Http.Json;
using StockManagement.Api.Features.Reports;
using StockManagement.Api.Features.Users;
using StockManagement.Auth.Core.Contracts;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class ReportEndpointsTests
{
	private const string Password = "s3cret!23";

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
	public async Task GetStockValue_SumsAmountTimesPriceAcrossItems()
	{
		// Arrange
		await _client.PostAsJsonAsync("/api/stock-items", new { Code = "A1", Name = "Screw", Amount = 10, Price = 500m });
		await _client.PostAsJsonAsync("/api/stock-items", new { Code = "B2", Name = "Nut", Amount = 4, Price = 250m });

		// Act
		var response = await _client.GetAsync("/api/reports/stock-value");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadAsAsync<StockValueResponse>();
		Assert.AreEqual(14, body.TotalUnits);
		Assert.AreEqual(6000m, body.TotalValue);
		CollectionAssert.AreEquivalent(new[] { "A1", "B2" }, body.Items.Select(item => item.Code).ToList());
	}

	[TestMethod]
	public async Task GetSalesByPeriod_GroupsByDayAndExcludesCancelled()
	{
		// Arrange
		var (customerId, _) = await this.CreateCustomerAndSaleAsync("Ana", "Gomez", 2, 5000m);

		// Act
		var response = await _client.GetAsync(SalesByPeriodUrl(DateTime.Today, DateTime.Today.AddDays(1)));

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadAsAsync<SalesByPeriodResponse>();
		var row = body.Items.Single();
		Assert.AreEqual(1, row.InvoiceCount);
		Assert.AreEqual(10000m, row.Total);
		Assert.IsTrue(customerId > 0);
	}

	[TestMethod]
	public async Task GetSalesByPeriod_FromAfterTo_ReturnsBadRequest()
	{
		// Act
		var response = await _client.GetAsync(SalesByPeriodUrl(DateTime.Today, DateTime.Today.AddDays(-1)));

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[TestMethod]
	public async Task GetSalesByCustomer_OrdersByTotalDescending()
	{
		// Arrange
		await this.CreateCustomerAndSaleAsync("Ana", "Gomez", 1, 1000m);
		await this.CreateCustomerAndSaleAsync("Beto", "Diaz", 5, 1000m);

		// Act
		var response = await _client.GetAsync($"/api/reports/sales-by-customer?from={Uri.EscapeDataString(DateTime.Today.ToString("O"))}&to={Uri.EscapeDataString(DateTime.Today.AddDays(1).ToString("O"))}");

		// Assert
		var body = await response.Content.ReadAsAsync<SalesByCustomerResponse>();
		CollectionAssert.AreEqual(new[] { "Beto Diaz", "Ana Gomez" }, body.Items.Select(row => row.CustomerName).ToList());
	}

	[TestMethod]
	public async Task StandardUser_WithoutReportsRead_CannotViewStockValue()
	{
		// Arrange
		await _client.PostAsJsonAsync("/api/users", new CreateUserRequest("clerk", Password, Permissions: []));
		var client = await _factory.CreateAuthenticatedClientAsync("clerk", Password);

		// Act
		var response = await client.GetAsync("/api/reports/stock-value");

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithReportsRead_CanViewStockValue()
	{
		// Arrange
		await _client.PostAsJsonAsync("/api/users", new CreateUserRequest("auditor", Password, Permissions: [Permission.ReportsRead]));
		var client = await _factory.CreateAuthenticatedClientAsync("auditor", Password);

		// Act
		var response = await client.GetAsync("/api/reports/stock-value");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
	}

	private async Task<(int CustomerId, string InvoiceNumber)> CreateCustomerAndSaleAsync(string name, string lastname, int amount, decimal price)
	{
		var code = Guid.NewGuid().ToString("N")[..8];
		await _client.PostAsJsonAsync("/api/stock-items", new { Code = code, Name = code, Amount = amount, Price = price });
		var customerResponse = await _client.PostAsJsonAsync("/api/customers", new { Name = name, Lastname = lastname });
		var customer = await customerResponse.Content.ReadAsAsync<CustomerCreated>();

		var saleResponse = await _client.PostAsJsonAsync("/api/sales", new { CustomerId = customer.CustomerId, SaleCondition = "Cash", Items = new[] { new { Code = code, Amount = amount } } });
		var invoice = await saleResponse.Content.ReadAsAsync<InvoiceCreated>();
		return (customer.CustomerId, invoice.Number);
	}

	private static string SalesByPeriodUrl(DateTime from, DateTime to)
	{
		return $"/api/reports/sales-by-period?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
	}

	private sealed record CustomerCreated(int CustomerId);
	private sealed record InvoiceCreated(string Number);
}
