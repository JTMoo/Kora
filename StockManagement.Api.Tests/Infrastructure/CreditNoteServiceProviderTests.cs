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
public sealed class CreditNoteServiceProviderTests
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
	public async Task TryAddCreditNoteAsync_InvoiceNotCancelled_RestocksMarksInvoiceCancelledAndStoresCreditNote()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10);
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		db.StockItems.Add(stockItem);
		db.Customers.Add(customer);
		await db.SaveChangesAsync();
		await new InvoiceServiceProvider(db).TryAddSaleAsync(new Invoice { Number = "1", Customer = customer, SaleCondition = SaleCondition.Cash, Items = [new ShoppingCartItem(stockItem) { Amount = 4 }] });
		var invoice = await db.Invoices.SingleAsync();

		var creditNote = new CreditNote { Number = 1, Reason = "Customer returned the goods", Invoice = invoice, Total = invoice.Total, Tax = invoice.Tax };
		var provider = new CreditNoteServiceProvider(db);

		// Act
		var succeeded = await provider.TryAddCreditNoteAsync(creditNote);

		// Assert
		Assert.IsTrue(succeeded);
		Assert.AreEqual(10, (await db.StockItems.AsNoTracking().SingleAsync()).Amount);
		Assert.IsTrue((await db.Invoices.AsNoTracking().SingleAsync()).IsCancelled);
		var storedCreditNote = await db.CreditNotes.AsNoTracking().SingleAsync();
		Assert.AreEqual(1, storedCreditNote.Number);
		var stockTransaction = await db.Transactions.AsNoTracking().SingleAsync(transaction => transaction.Amount > 0);
		Assert.AreEqual(4, stockTransaction.Amount);
		Assert.AreEqual("Customer returned the goods", stockTransaction.Reason);
	}

	[TestMethod]
	public async Task TryAddCreditNoteAsync_InvoiceAlreadyCancelled_ReturnsFalseAndWritesNothing()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10);
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		db.StockItems.Add(stockItem);
		db.Customers.Add(customer);
		await db.SaveChangesAsync();
		await new InvoiceServiceProvider(db).TryAddSaleAsync(new Invoice { Number = "1", Customer = customer, SaleCondition = SaleCondition.Cash, Items = [new ShoppingCartItem(stockItem) { Amount = 4 }] });
		var invoice = await db.Invoices.SingleAsync();
		var provider = new CreditNoteServiceProvider(db);
		await provider.TryAddCreditNoteAsync(new CreditNote { Number = 1, Reason = "First cancellation", Invoice = invoice, Total = invoice.Total, Tax = invoice.Tax });

		// Act
		var secondInvoice = await db.Invoices.SingleAsync();
		var succeeded = await provider.TryAddCreditNoteAsync(new CreditNote { Number = 2, Reason = "Second cancellation", Invoice = secondInvoice, Total = secondInvoice.Total, Tax = secondInvoice.Tax });

		// Assert
		Assert.IsFalse(succeeded);
		Assert.AreEqual(1, await db.CreditNotes.CountAsync());
		Assert.AreEqual(10, (await db.StockItems.AsNoTracking().SingleAsync()).Amount);
	}

	[TestMethod]
	public async Task TryAddCreditNoteAsync_DuplicateNumber_ThrowsCreditNoteNumberAlreadyExists()
	{
		// Arrange
		await using var scope = _services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var stockItem = new StockItem("Screw", code: "A1", amount: 10);
		var customer = new Customer { CustomerId = 1001, Name = "Ann" };
		db.StockItems.Add(stockItem);
		db.Customers.Add(customer);
		await db.SaveChangesAsync();
		var invoiceProvider = new InvoiceServiceProvider(db);
		await invoiceProvider.TryAddSaleAsync(new Invoice { Number = "1", Customer = customer, SaleCondition = SaleCondition.Cash, Items = [new ShoppingCartItem(stockItem) { Amount = 1 }] });
		await invoiceProvider.TryAddSaleAsync(new Invoice { Number = "2", Customer = await db.Customers.SingleAsync(), SaleCondition = SaleCondition.Cash, Items = [new ShoppingCartItem(await db.StockItems.SingleAsync()) { Amount = 1 }] });
		var provider = new CreditNoteServiceProvider(db);
		var firstInvoice = await db.Invoices.SingleAsync(invoice => invoice.Number == "1");
		await provider.TryAddCreditNoteAsync(new CreditNote { Number = 1, Reason = "First", Invoice = firstInvoice, Total = firstInvoice.Total, Tax = firstInvoice.Tax });

		// Act + Assert
		var secondInvoice = await db.Invoices.SingleAsync(invoice => invoice.Number == "2");
		await Assert.ThrowsExceptionAsync<CreditNoteNumberAlreadyExistsException>(() =>
			provider.TryAddCreditNoteAsync(new CreditNote { Number = 1, Reason = "Duplicate", Invoice = secondInvoice, Total = secondInvoice.Total, Tax = secondInvoice.Tax }));
		// Rolled back with the rest of the transaction: the second invoice's IsCancelled claim and its stock return don't stick
		Assert.AreEqual(1, await db.CreditNotes.CountAsync());
		Assert.IsFalse((await db.Invoices.AsNoTracking().SingleAsync(invoice => invoice.Number == "2")).IsCancelled);
	}
}
