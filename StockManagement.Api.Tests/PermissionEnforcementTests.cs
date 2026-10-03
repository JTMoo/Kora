using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.Customers;
using StockManagement.Api.Features.GoodsImportDocuments;
using StockManagement.Api.Features.Users;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests;


/// <summary>
/// A Standard user only gets the permissions granted on their account (ADR-0017); #14's "simple users can't change client data"
/// </summary>
[TestClass]
public sealed class PermissionEnforcementTests
{
	private const string Password = "s3cret!23";

	private ApiFactory _factory;
	private HttpClient _adminClient;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_factory = new();
		_adminClient = await _factory.CreateAuthenticatedClientAsync();
	}

	[TestCleanup]
	public void Cleanup()
	{
		_adminClient.Dispose();
		_factory.Dispose();
	}


	[TestMethod]
	public async Task StandardUser_WithoutCustomersWrite_CannotCreateCustomer()
	{
		// Arrange
		var client = await this.CreateStandardUserClientAsync("plain", []);

		// Act
		var response = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest("Ana"));

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithCustomersWrite_CanCreateCustomer()
	{
		// Arrange
		var client = await this.CreateStandardUserClientAsync("clerk", [Permission.CustomersWrite]);

		// Act
		var response = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest("Ana"));

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithoutSuppliersWrite_CannotCreateSupplier()
	{
		// Arrange
		var client = await this.CreateStandardUserClientAsync("plain2", []);

		// Act
		var response = await client.PostAsJsonAsync("/api/suppliers", new { Name = "Acme" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithSuppliersWrite_CanCreateSupplier()
	{
		// Arrange
		var client = await this.CreateStandardUserClientAsync("buyer", [Permission.SuppliersWrite]);

		// Act
		var response = await client.PostAsJsonAsync("/api/suppliers", new { Name = "Acme" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithoutGoodsImportsWrite_CannotCreateGoodsImportDocument()
	{
		// Arrange
		var client = await this.CreateStandardUserClientAsync("plain3", []);

		// Act
		var response = await client.PostAsJsonAsync("/api/goods-import-documents", new { ProformaNumber = "PF-100" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithGoodsImportsWrite_CanCreateGoodsImportDocument()
	{
		// Arrange
		var supplier = new Supplier("Acme Imports");
		await _factory.ScopedServices.GetRequiredService<ISupplierServiceProvider>().AddSupplierAsync(supplier);
		await _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>().AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10, price: 5000));
		var client = await this.CreateStandardUserClientAsync("importer", [Permission.GoodsImportsWrite]);

		// Act
		var response = await client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-101", supplier.Id, Incoterm.Fob, "Despachante SA", "DUA-2026-200", DateTime.Now, [new("A1", 1)]));

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithoutUsersManage_CannotListUsers()
	{
		// Arrange
		var client = await this.CreateStandardUserClientAsync("plain", []);

		// Act
		var response = await client.GetAsync("/api/users");

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithoutStockItemsWrite_CannotCheckIn()
	{
		// Arrange
		var stockItems = _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10, price: 5000);
		await stockItems.AddStockItemAsync(stockItem);
		var client = await this.CreateStandardUserClientAsync("plain3", []);

		// Act
		var response = await client.PostAsJsonAsync($"/api/stock-items/{stockItem.Id}/check-in", new { Id = stockItem.Id, Amount = 5, Reason = "Delivery" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithStockItemsWrite_CanCheckIn()
	{
		// Arrange
		var stockItems = _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10, price: 5000);
		await stockItems.AddStockItemAsync(stockItem);
		var client = await this.CreateStandardUserClientAsync("stocker", [Permission.StockItemsWrite]);

		// Act
		var response = await client.PostAsJsonAsync($"/api/stock-items/{stockItem.Id}/check-in", new { Id = stockItem.Id, Amount = 5, Reason = "Delivery" });

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
	}

	[TestMethod]
	public async Task StandardUser_WithoutStockItemsWrite_CannotCheckOut()
	{
		// Arrange
		var stockItems = _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10, price: 5000);
		await stockItems.AddStockItemAsync(stockItem);
		var client = await this.CreateStandardUserClientAsync("plain4", []);

		// Act
		var response = await client.PostAsJsonAsync($"/api/stock-items/{stockItem.Id}/check-out", new { Id = stockItem.Id, Amount = 5, Reason = "Damaged" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
	}

	private async Task<HttpClient> CreateStandardUserClientAsync(string username, IReadOnlyList<string> permissions)
	{
		await _adminClient.PostAsJsonAsync("/api/users", new CreateUserRequest(username, Password, Permissions: permissions));
		return await _factory.CreateAuthenticatedClientAsync(username, Password);
	}
}
