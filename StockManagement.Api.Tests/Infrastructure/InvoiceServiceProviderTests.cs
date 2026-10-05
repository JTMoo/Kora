using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StockManagement.Infrastructure;
using StockManagement.Infrastructure.Database;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests.Infrastructure;


[TestClass]
public sealed class InvoiceServiceProviderTests
{
	private ServiceProvider _services;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		var connectionString = new NpgsqlConnectionStringBuilder(PostgresContainer.ConnectionString) { Database = $"test_{Guid.NewGuid():N}", Pooling = false }.ConnectionString;
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection([new("ConnectionStrings:Postgres", connectionString)])
			.Build();

		_services = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();

		await using var scope = _services.CreateAsyncScope();
		await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
	}

	[TestCleanup]
	public ValueTask CleanupAsync()
	{
		return _services.DisposeAsync();
	}


	[TestMethod]
	public async Task TryAddSaleAsync_EnoughStock_DecrementsAndStoresInvoice()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10);
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		db.StockItems.Add(stockItem);
		db.Customers.Add(customer);
		await db.SaveChangesAsync();

		var invoice = new Invoice { Number = "1", Customer = customer, SaleCondition = SaleCondition.Cash, Items = [new ShoppingCartItem(stockItem) { Amount = 4 }] };
		var provider = new InvoiceServiceProvider(db);

		// Act
		var shortages = await provider.TryAddSaleAsync(invoice);

		// Assert
		Assert.AreEqual(0, shortages.Count);
		var storedItem = await db.StockItems.AsNoTracking().SingleAsync(item => item.Code == "A1");
		Assert.AreEqual(6, storedItem.Amount);
		var storedInvoice = await db.Invoices.AsNoTracking().SingleAsync();
		Assert.AreEqual("1", storedInvoice.Number);
		var transaction = await db.Transactions.AsNoTracking().SingleAsync();
		Assert.AreEqual(-4, transaction.Amount);
	}

	[TestMethod]
	public async Task TryAddSaleAsync_EnoughStock_WritesPendingTransmissionOutboxRow()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10);
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		db.StockItems.Add(stockItem);
		db.Customers.Add(customer);
		await db.SaveChangesAsync();

		var invoice = new Invoice { Number = "1", Customer = customer, SaleCondition = SaleCondition.Cash, Items = [new ShoppingCartItem(stockItem) { Amount = 4 }] };
		var provider = new InvoiceServiceProvider(db);

		// Act
		await provider.TryAddSaleAsync(invoice);

		// Assert
		var transmission = await db.PendingTransmissions.AsNoTracking().SingleAsync();
		Assert.AreEqual(invoice.Id, transmission.Invoice.Id);
		Assert.AreEqual(0, transmission.Attempts);
		Assert.AreEqual(TransmissionStatus.Pending, (await db.Invoices.AsNoTracking().SingleAsync()).TransmissionStatus);
	}

	[TestMethod]
	public async Task TryAddSaleAsync_NotEnoughStock_ReturnsShortageAndWritesNothing()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10);
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		// ShoppingCartItem.Amount clamps to StockItem.Amount at set time, so ask for the line before dropping stock below it
		var line = new ShoppingCartItem(stockItem) { Amount = 4 };
		stockItem.Amount = 2;
		db.StockItems.Add(stockItem);
		db.Customers.Add(customer);
		await db.SaveChangesAsync();

		var invoice = new Invoice { Number = "1", Customer = customer, SaleCondition = SaleCondition.Cash, Items = [line] };
		var provider = new InvoiceServiceProvider(db);

		// Act
		var shortages = await provider.TryAddSaleAsync(invoice);

		// Assert
		CollectionAssert.AreEqual(new[] { "Screw" }, shortages.ToList());
		Assert.AreEqual(2, (await db.StockItems.AsNoTracking().SingleAsync(item => item.Code == "A1")).Amount);
		Assert.AreEqual(0, await db.Invoices.CountAsync());
	}

	[TestMethod]
	public async Task TryAddSaleAsync_ConcurrentSalesOversellingStock_OnlyOneSucceeds()
	{
		// Arrange: two sales of 6 units each against 10 in stock - only one can succeed
		await using var setup = _services.CreateAsyncScope();
		var setupDb = setup.ServiceProvider.GetRequiredService<AppDbContext>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10);
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		setupDb.StockItems.Add(stockItem);
		setupDb.Customers.Add(customer);
		await setupDb.SaveChangesAsync();

		async Task<IReadOnlyList<string>> SellAsync(int number)
		{
			await using var scope = _services.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var localStockItem = await db.StockItems.AsNoTracking().SingleAsync(item => item.Code == "A1");
			var localCustomer = await db.Customers.AsNoTracking().SingleAsync();
			var invoice = new Invoice { Number = number.ToString(), Customer = localCustomer, SaleCondition = SaleCondition.Cash, Items = [new ShoppingCartItem(localStockItem) { Amount = 6 }] };
			return await new InvoiceServiceProvider(db).TryAddSaleAsync(invoice);
		}

		// Act
		var results = await Task.WhenAll(SellAsync(1), SellAsync(2));

		// Assert
		Assert.AreEqual(1, results.Count(result => result.Count == 0), "exactly one sale should have succeeded");
		Assert.AreEqual(1, results.Count(result => result.Count == 1), "exactly one sale should have been short");
		await using var verify = _services.CreateAsyncScope();
		var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
		Assert.AreEqual(4, (await verifyDb.StockItems.AsNoTracking().SingleAsync()).Amount);
	}

	[TestMethod]
	public async Task GetInvoicesAsync_FilteredByCustomerId_ReturnsOnlyThatCustomersInvoices()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10);
		var ann = new Customer { CustomerId = 1001, Name = "Ann" };
		var bob = new Customer { CustomerId = 1002, Name = "Bob" };
		db.StockItems.Add(stockItem);
		db.Customers.AddRange(ann, bob);
		db.Invoices.AddRange(
			new Invoice { Number = "1", Customer = ann, Date = new DateTime(2026, 1, 1), Items = [] },
			new Invoice { Number = "2", Customer = bob, Date = new DateTime(2026, 1, 2), Items = [] });
		await db.SaveChangesAsync();
		var provider = new InvoiceServiceProvider(db);

		// Act
		var result = await provider.GetInvoicesAsync(customerId: 1001, from: null, to: null, cursor: null, pageSize: 20);

		// Assert
		Assert.AreEqual(1, result.Items.Count);
		Assert.AreEqual("1", result.Items.Single().Number);
	}

	[TestMethod]
	public async Task GetInvoicesAsync_FilteredByDateRange_ExcludesInvoicesOutsideRange()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		db.Customers.Add(customer);
		db.Invoices.AddRange(
			new Invoice { Number = "1", Customer = customer, Date = new DateTime(2026, 1, 1), Items = [] },
			new Invoice { Number = "2", Customer = customer, Date = new DateTime(2026, 6, 1), Items = [] },
			new Invoice { Number = "3", Customer = customer, Date = new DateTime(2026, 12, 1), Items = [] });
		await db.SaveChangesAsync();
		var provider = new InvoiceServiceProvider(db);

		// Act
		var result = await provider.GetInvoicesAsync(customerId: null, from: new DateTime(2026, 3, 1), to: new DateTime(2026, 9, 1), cursor: null, pageSize: 20);

		// Assert
		Assert.AreEqual("2", result.Items.Single().Number);
	}

	[TestMethod]
	public async Task GetInvoicesAsync_SecondPage_ReturnsRemainingInvoicesNewestFirst()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		db.Customers.Add(customer);
		db.Invoices.AddRange(
			new Invoice { Number = "1", Customer = customer, Date = new DateTime(2026, 1, 1), Items = [] },
			new Invoice { Number = "2", Customer = customer, Date = new DateTime(2026, 1, 2), Items = [] },
			new Invoice { Number = "3", Customer = customer, Date = new DateTime(2026, 1, 3), Items = [] });
		await db.SaveChangesAsync();
		var provider = new InvoiceServiceProvider(db);

		// Act
		var firstPage = await provider.GetInvoicesAsync(customerId: null, from: null, to: null, cursor: null, pageSize: 2);
		var result = await provider.GetInvoicesAsync(customerId: null, from: null, to: null, cursor: firstPage.NextCursor, pageSize: 2);

		// Assert
		Assert.IsNull(result.NextCursor);
		Assert.AreEqual("1", result.Items.Single().Number);
	}

	[TestMethod]
	public async Task TryAddSaleAsync_DuplicateNumber_ThrowsInvoiceNumberAlreadyExists()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10);
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		db.StockItems.Add(stockItem);
		db.Customers.Add(customer);
		await db.SaveChangesAsync();
		await new InvoiceServiceProvider(db).TryAddSaleAsync(new Invoice { Number = "1", Customer = customer, SaleCondition = SaleCondition.Cash, Items = [new ShoppingCartItem(stockItem) { Amount = 1 }] });

		// Act + Assert
		await using var otherScope = _services.CreateAsyncScope();
		var otherDb = otherScope.ServiceProvider.GetRequiredService<AppDbContext>();
		var otherStockItem = await otherDb.StockItems.AsNoTracking().SingleAsync();
		var otherCustomer = await otherDb.Customers.AsNoTracking().SingleAsync();
		var duplicate = new Invoice { Number = "1", Customer = otherCustomer, SaleCondition = SaleCondition.Cash, Items = [new ShoppingCartItem(otherStockItem) { Amount = 1 }] };
		await Assert.ThrowsExceptionAsync<InvoiceNumberAlreadyExistsException>(() => new InvoiceServiceProvider(otherDb).TryAddSaleAsync(duplicate));
		// Rolled back with the rest of the transaction, so only the first sale's decrement (10 - 1) sticks
		Assert.AreEqual(9, (await otherDb.StockItems.AsNoTracking().SingleAsync()).Amount);
	}
}
