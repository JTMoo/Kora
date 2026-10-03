using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.StockItems;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class StockItemEndpointsTests
{
	private ApiFactory _factory;
	private HttpClient _client;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_factory = new();
		_client = await _factory.CreateAuthenticatedClientAsync();

		var stockItems = _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>();
		await stockItems.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10, price: 5000) { Barcode = "4006381333931" });
		await stockItems.AddStockItemAsync(new StockItem("Nut", code: "B2", amount: 3, price: 1000));
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}


	[TestMethod]
	public async Task ListStockItems_TwoStored_ReturnsBoth()
	{
		// Act
		var response = await _client.GetAsync("/api/stock-items");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var stockItems = await response.Content.ReadAsAsync<StockItemListResponse>();
		CollectionAssert.AreEquivalent(new[] { "A1", "B2" }, stockItems.Items.Select(item => item.Code).ToList());
	}

	[TestMethod]
	public async Task GetStockItem_KnownCode_ReturnsItem()
	{
		// Act
		var response = await _client.GetAsync("/api/stock-items/A1");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual("Screw", stockItem.Name);
		Assert.AreEqual(10, stockItem.Amount);
		Assert.AreEqual(5000m, stockItem.Price);
	}

	[TestMethod]
	public async Task GetStockItem_UnknownCode_Returns404()
	{
		// Act
		var response = await _client.GetAsync("/api/stock-items/X9");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateStockItem_NewCode_Returns201AndStores()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items", new { Code = "C3", Name = "Bolt", Amount = 4, Price = 250m });

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual("Bolt", stockItem.Name);
		Assert.AreEqual(4, stockItem.Amount);
		Assert.IsNotNull((await _client.GetAsync("/api/stock-items/C3")).Content);
	}

	[TestMethod]
	public async Task CreateStockItem_FactorGreaterThanZero_DerivesPriceFromPurchaseData()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items", new { Code = "C3", Name = "Bolt", PurchasePrice = 10m, PurchaseExchangeRate = 7300m, AdditionalPurchaseCost = 5000m, Factor = 1.25m });

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual(97500m, stockItem.Price);
	}

	[TestMethod]
	public async Task CreateStockItem_FactorZero_KeepsManualPrice()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items", new { Code = "C3", Name = "Bolt", Price = 250m });

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual(250m, stockItem.Price);
	}

	[TestMethod]
	public async Task CreateStockItem_DuplicateCode_Returns409()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items", new { Code = "A1", Name = "Other" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateStockItem_ValidBarcode_StoresIt()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items", new { Code = "C3", Name = "Bolt", Barcode = "40123455" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual("40123455", stockItem.Barcode);
	}

	[TestMethod]
	public async Task CreateStockItem_InvalidBarcode_Returns400()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items", new { Code = "C3", Name = "Bolt", Barcode = "not-a-barcode" });

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "barcodeInvalid");
	}

	[TestMethod]
	public async Task CreateStockItem_DuplicateBarcode_Returns409()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items", new { Code = "C3", Name = "Bolt", Barcode = "4006381333931" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateStockItem_EmptyName_Returns400()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items", new { Code = "C3", Name = "" });

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "nameRequired");
	}

	[TestMethod]
	public async Task UpdateStockItem_KnownId_Returns200AndUpdates()
	{
		// Arrange
		var id = (await (await _client.GetAsync("/api/stock-items/A1")).Content.ReadAsAsync<StockItemResponse>()).Id;

		// Act
		var response = await _client.PutAsJsonAsync($"/api/stock-items/{id}", new { Id = id, Code = "A1", Name = "Screw XL", Amount = 20, Price = 6000m });

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual("Screw XL", stockItem.Name);
		Assert.AreEqual(20, stockItem.Amount);
	}

	[TestMethod]
	public async Task UpdateStockItem_FactorGreaterThanZero_DerivesPriceFromPurchaseData()
	{
		// Arrange
		var id = (await (await _client.GetAsync("/api/stock-items/A1")).Content.ReadAsAsync<StockItemResponse>()).Id;

		// Act
		var response = await _client.PutAsJsonAsync($"/api/stock-items/{id}", new { Id = id, Code = "A1", Name = "Screw", PurchasePrice = 10m, PurchaseExchangeRate = 7300m, AdditionalPurchaseCost = 5000m, Factor = 1.25m, Price = 1m });

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual(97500m, stockItem.Price);
	}

	[TestMethod]
	public async Task UpdateStockItem_CodeChangedToDuplicate_Returns409()
	{
		// Arrange
		var id = (await (await _client.GetAsync("/api/stock-items/A1")).Content.ReadAsAsync<StockItemResponse>()).Id;

		// Act
		var response = await _client.PutAsJsonAsync($"/api/stock-items/{id}", new { Id = id, Code = "B2", Name = "Screw" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
	}

	[TestMethod]
	public async Task UpdateStockItem_BarcodeChangedToDuplicate_Returns409()
	{
		// Arrange
		var id = (await (await _client.GetAsync("/api/stock-items/B2")).Content.ReadAsAsync<StockItemResponse>()).Id;

		// Act
		var response = await _client.PutAsJsonAsync($"/api/stock-items/{id}", new { Id = id, Code = "B2", Name = "Nut", Barcode = "4006381333931" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
	}

	[TestMethod]
	public async Task UpdateStockItem_UnknownId_Returns404()
	{
		// Act
		var response = await _client.PutAsJsonAsync("/api/stock-items/unknown", new { Id = "unknown", Code = "X9", Name = "Anything" });

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task DeleteStockItem_KnownId_Returns204AndRemoves()
	{
		// Arrange
		var id = (await (await _client.GetAsync("/api/stock-items/A1")).Content.ReadAsAsync<StockItemResponse>()).Id;

		// Act
		var response = await _client.DeleteAsync($"/api/stock-items/{id}");

		// Assert
		Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
		Assert.AreEqual(HttpStatusCode.NotFound, (await _client.GetAsync("/api/stock-items/A1")).StatusCode);
	}

	[TestMethod]
	public async Task DeleteStockItem_UnknownId_Returns404()
	{
		// Act
		var response = await _client.DeleteAsync("/api/stock-items/unknown");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task GetStockItemByBarcode_KnownBarcode_ReturnsItem()
	{
		// Act
		var response = await _client.GetAsync("/api/stock-items/by-barcode/4006381333931");

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual("A1", stockItem.Code);
	}

	[TestMethod]
	public async Task GetStockItemByBarcode_UnknownBarcode_Returns404()
	{
		// Act
		var response = await _client.GetAsync("/api/stock-items/by-barcode/0000000000000");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CheckInStockItem_Valid_Returns200AndIncreasesAmount()
	{
		// Arrange
		var id = (await (await _client.GetAsync("/api/stock-items/A1")).Content.ReadAsAsync<StockItemResponse>()).Id;

		// Act
		var response = await _client.PostAsJsonAsync($"/api/stock-items/{id}/check-in", new { Id = id, Amount = 5, Reason = "Delivery" });

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual(15, stockItem.Amount);
	}

	[TestMethod]
	public async Task CheckInStockItem_EmptyReason_Returns400()
	{
		// Arrange
		var id = (await (await _client.GetAsync("/api/stock-items/A1")).Content.ReadAsAsync<StockItemResponse>()).Id;

		// Act
		var response = await _client.PostAsJsonAsync($"/api/stock-items/{id}/check-in", new { Id = id, Amount = 5, Reason = "" });

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "reasonRequired");
	}

	[TestMethod]
	public async Task CheckInStockItem_UnknownId_Returns404()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items/unknown/check-in", new { Id = "unknown", Amount = 5, Reason = "Delivery" });

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CheckOutStockItem_EnoughStock_Returns200AndDecreasesAmount()
	{
		// Arrange
		var id = (await (await _client.GetAsync("/api/stock-items/A1")).Content.ReadAsAsync<StockItemResponse>()).Id;

		// Act
		var response = await _client.PostAsJsonAsync($"/api/stock-items/{id}/check-out", new { Id = id, Amount = 4, Reason = "Damaged" });

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var stockItem = await response.Content.ReadAsAsync<StockItemResponse>();
		Assert.AreEqual(6, stockItem.Amount);
	}

	[TestMethod]
	public async Task CheckOutStockItem_InsufficientStock_Returns409()
	{
		// Arrange
		var id = (await (await _client.GetAsync("/api/stock-items/B2")).Content.ReadAsAsync<StockItemResponse>()).Id;

		// Act
		var response = await _client.PostAsJsonAsync($"/api/stock-items/{id}/check-out", new { Id = id, Amount = 4, Reason = "Damaged" });

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
		var body = await response.Content.ReadAsAsync<InsufficientStockResponse>();
		Assert.AreEqual(3, body.InStock);
	}

	[TestMethod]
	public async Task CheckOutStockItem_UnknownId_Returns404()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/stock-items/unknown/check-out", new { Id = "unknown", Amount = 1, Reason = "Damaged" });

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}
}
