using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Features.GoodsImportDocuments;
using StockManagement.Api.Features.StockItems;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests;


/// <summary>
/// Goods-import document chain endpoints (#164) - posting a document checks in stock (unlike
/// <see cref="RemissionNoteEndpointsTests"/>'s remission note, which never touches <see cref="StockItem.Amount"/>)
/// so it can feed the existing landed-cost allocation (#120/#143).
/// </summary>
[TestClass]
public sealed class GoodsImportDocumentEndpointsTests
{
	private ApiFactory _factory;
	private HttpClient _client;
	private string _supplierId;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_factory = new();
		_client = await _factory.CreateAuthenticatedClientAsync();

		await _factory.ScopedServices.GetRequiredService<IStockItemServiceProvider>().AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10, price: 5000));

		var supplier = new Supplier("Acme Imports", country: "CN");
		await _factory.ScopedServices.GetRequiredService<ISupplierServiceProvider>().AddSupplierAsync(supplier);
		_supplierId = supplier.Id;
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}


	[TestMethod]
	public async Task CreateGoodsImportDocument_ValidRequest_Returns201AndChecksInStock()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-001", _supplierId, Incoterm.Fob, "Despachante SA", "DUA-2026-123", DateTime.Now, [new("A1", 50)]), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
		var document = await response.Content.ReadAsAsync<GoodsImportDocumentResponse>();
		Assert.AreEqual("PF-001", document.ProformaNumber);
		Assert.AreEqual(Incoterm.Fob, document.Incoterm);
		Assert.AreEqual("Despachante SA", document.BrokerName);
		Assert.AreEqual("DUA-2026-123", document.DuaReference);
		Assert.AreEqual(_supplierId, document.SupplierId);
		Assert.AreEqual(50, document.Items.Single().Amount);

		var stockItem = await _client.GetFromJsonAsync<StockItemResponse>("/api/stock-items/A1", ApiFactory.JsonOptions);
		Assert.AreEqual(60, stockItem.Amount);
	}

	[TestMethod]
	public async Task CreateGoodsImportDocument_Stored_CanBeReadBack()
	{
		// Arrange
		var created = await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-002", _supplierId, Incoterm.Cif, "Despachante SA", "DUA-2026-124", DateTime.Now, [new("A1", 1)]), ApiFactory.JsonOptions);

		// Act
		var response = await _client.GetAsync(created.Headers.Location);

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		var document = await response.Content.ReadAsAsync<GoodsImportDocumentResponse>();
		Assert.AreEqual("PF-002", document.ProformaNumber);
		Assert.AreEqual(Incoterm.Cif, document.Incoterm);
	}

	[TestMethod]
	public async Task ListGoodsImportDocuments_OneStored_IncludesIt()
	{
		// Arrange
		await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-003", _supplierId, Incoterm.Exw, "Despachante SA", "DUA-2026-125", DateTime.Now, [new("A1", 1)]), ApiFactory.JsonOptions);

		// Act
		var response = await _client.GetFromJsonAsync<GoodsImportDocumentListResponse>("/api/goods-import-documents", ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(1, response.Items.Count);
	}

	[TestMethod]
	public async Task CreateGoodsImportDocument_DuplicateProformaNumber_Returns409()
	{
		// Arrange
		await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-004", _supplierId, Incoterm.Fob, "Despachante SA", "DUA-2026-126", DateTime.Now, [new("A1", 1)]), ApiFactory.JsonOptions);

		// Act
		var response = await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-004", _supplierId, Incoterm.Fob, "Despachante SA", "DUA-2026-127", DateTime.Now, [new("A1", 1)]), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateGoodsImportDocument_UnknownSupplier_Returns404()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-005", "unknown-supplier", Incoterm.Fob, "Despachante SA", "DUA-2026-128", DateTime.Now, [new("A1", 1)]), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateGoodsImportDocument_UnknownStockItemCode_Returns404()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-006", _supplierId, Incoterm.Fob, "Despachante SA", "DUA-2026-129", DateTime.Now, [new("B2", 1)]), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[TestMethod]
	public async Task CreateGoodsImportDocument_NoItems_Returns400()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-007", _supplierId, Incoterm.Fob, "Despachante SA", "DUA-2026-130", DateTime.Now, []), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "itemsRequired");
	}

	[TestMethod]
	public async Task CreateGoodsImportDocument_EmptyBrokerName_Returns400()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-008", _supplierId, Incoterm.Fob, "", "DUA-2026-131", DateTime.Now, [new("A1", 1)]), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "brokerNameRequired");
	}

	[TestMethod]
	public async Task CreateGoodsImportDocument_EmptyDuaReference_Returns400()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/goods-import-documents", new CreateGoodsImportDocumentRequest("PF-009", _supplierId, Incoterm.Fob, "Despachante SA", "", DateTime.Now, [new("A1", 1)]), ApiFactory.JsonOptions);

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "duaReferenceRequired");
	}

	[TestMethod]
	public async Task GetGoodsImportDocument_UnknownId_Returns404()
	{
		// Act
		var response = await _client.GetAsync("/api/goods-import-documents/unknown-id");

		// Assert
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}
}
