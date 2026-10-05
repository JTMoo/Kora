using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StockManagement.Infrastructure;
using StockManagement.Infrastructure.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;

namespace StockManagement.Api.Tests.Infrastructure;


[TestClass]
public sealed class StockItemServiceProviderTests
{
	private ServiceProvider _services;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		var connectionString = new NpgsqlConnectionStringBuilder(PostgresContainer.ConnectionString) { Database = $"test_{Guid.NewGuid():N}", Pooling = false }.ConnectionString;
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection([new("ConnectionStrings:Postgres", connectionString)])
			.Build();

		_services = new ServiceCollection()
			.AddInfrastructure(configuration)
			.AddScoped<IStockItemServiceProvider, StockItemServiceProvider>()
			.BuildServiceProvider();

		await using var scope = _services.CreateAsyncScope();
		await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
	}

	[TestCleanup]
	public ValueTask CleanupAsync()
	{
		return _services.DisposeAsync();
	}


	[TestMethod]
	public async Task AddStockItemAsync_NewCode_StoredWithAmountTransaction()
	{
		// Act
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10)));

		// Assert
		var stored = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));
		Assert.AreEqual(10, stored.Amount);
		var transaction = await this.UseDbAsync(db => db.Transactions.SingleAsync());
		Assert.AreEqual(Transaction.Kind.Amount, transaction.SelectedKind);
		Assert.AreEqual(10, transaction.Amount);
	}

	[TestMethod]
	public async Task AddStockItemAsync_DuplicateCode_ThrowsStockItemCodeAlreadyExists()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Screw", code: "A1")));

		// Act + Assert
		await Assert.ThrowsExceptionAsync<StockItemCodeAlreadyExistsException>(() => this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Other", code: "A1"))));
	}

	[TestMethod]
	public async Task GetAllStockItemsAsync_TwoStored_ReturnsBoth()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Screw", code: "A1")));
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Nut", code: "B2")));

		// Act
		var stockItems = await this.UseAsync(provider => provider.GetAllStockItemsAsync());

		// Assert
		CollectionAssert.AreEquivalent(new[] { "A1", "B2" }, stockItems.Select(item => item.Code).ToList());
	}

	[TestMethod]
	public async Task GetStockItemByIdAsync_KnownId_ReturnsItem()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Screw", code: "A1")));
		var stored = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));

		// Act
		var stockItem = await this.UseAsync(provider => provider.GetStockItemByIdAsync(stored.Id));

		// Assert
		Assert.AreEqual("A1", stockItem.Code);
	}

	[TestMethod]
	public async Task UpdateStockItemAsync_AmountChanged_RecordsDeltaTransaction()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10)));
		var stored = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));

		// Act
		stored.Amount = 7;
		var result = await this.UseAsync(provider => provider.UpdateStockItemAsync(stored));

		// Assert
		Assert.AreEqual(1, result);
		var last = await this.UseDbAsync(db => db.Transactions.OrderBy(t => t.Time).LastAsync());
		Assert.AreEqual(-3, last.Amount);
	}

	[TestMethod]
	public async Task DeleteStockItemAsync_Existing_RemovedAndRecordsDeletionTransaction()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Screw", code: "A1")));
		var stored = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));

		// Act
		var result = await this.UseAsync(provider => provider.DeleteStockItemAsync(stored));

		// Assert
		Assert.AreEqual(1, result);
		Assert.IsNull(await this.UseAsync(provider => provider.GetStockItemAsync("A1")));
	}

	[TestMethod]
	public async Task CheckInStockItemAsync_Valid_IncreasesAmountAndRecordsReasonedTransaction()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10)));
		var stored = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));

		// Act
		await this.UseAsync(provider => provider.CheckInStockItemAsync(stored, 5, "Delivery"));

		// Assert
		var updated = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));
		Assert.AreEqual(15, updated.Amount);
		var last = await this.UseDbAsync(db => db.Transactions.OrderBy(t => t.Time).LastAsync());
		Assert.AreEqual(5, last.Amount);
		Assert.AreEqual("Delivery", last.Reason);
	}

	[TestMethod]
	public async Task TryCheckOutStockItemAsync_EnoughStock_DecreasesAmountRecordsTransactionAndReturnsTrue()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 10)));
		var stored = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));

		// Act
		var result = await this.UseAsync(provider => provider.TryCheckOutStockItemAsync(stored, 4, "Damaged"));

		// Assert
		Assert.IsTrue(result);
		var updated = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));
		Assert.AreEqual(6, updated.Amount);
		var last = await this.UseDbAsync(db => db.Transactions.OrderBy(t => t.Time).LastAsync());
		Assert.AreEqual(-4, last.Amount);
		Assert.AreEqual("Damaged", last.Reason);
	}

	[TestMethod]
	public async Task TryCheckOutStockItemAsync_InsufficientStock_ReturnsFalseAndWritesNothing()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddStockItemAsync(new StockItem("Screw", code: "A1", amount: 3)));
		var stored = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));

		// Act
		var result = await this.UseAsync(provider => provider.TryCheckOutStockItemAsync(stored, 4, "Damaged"));

		// Assert
		Assert.IsFalse(result);
		var updated = await this.UseAsync(provider => provider.GetStockItemAsync("A1"));
		Assert.AreEqual(3, updated.Amount);
		var transactionCount = await this.UseDbAsync(db => db.Transactions.CountAsync());
		Assert.AreEqual(1, transactionCount); // only the AddStockItemAsync transaction
	}


	private async Task<T> UseAsync<T>(Func<IStockItemServiceProvider, Task<T>> action)
	{
		await using var scope = _services.CreateAsyncScope();
		return await action(scope.ServiceProvider.GetRequiredService<IStockItemServiceProvider>());
	}

	private Task UseAsync(Func<IStockItemServiceProvider, Task> action)
	{
		return this.UseAsync<object?>(async provider =>
		{
			await action(provider);
			return null;
		});
	}

	private async Task<T> UseDbAsync<T>(Func<AppDbContext, Task<T>> action)
	{
		await using var scope = _services.CreateAsyncScope();
		return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
	}
}
