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
public sealed class SupplierServiceProviderTests
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
			.AddScoped<ISupplierServiceProvider, SupplierServiceProvider>()
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
	public async Task AddSupplierAsync_DuplicateName_ThrowsSupplierNameAlreadyExists()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddSupplierAsync(new Supplier("Acme")));

		// Act + Assert
		await Assert.ThrowsExceptionAsync<SupplierNameAlreadyExistsException>(() => this.UseAsync(provider => provider.AddSupplierAsync(new Supplier("Acme"))));
	}

	[TestMethod]
	public async Task UpdateSupplierAsync_Existing_PersistsChanges()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddSupplierAsync(new Supplier("Acme")));
		var stored = (await this.UseAsync(provider => provider.GetAllSuppliersAsync())).Single();
		stored.LeadTimeDays = 12;

		// Act
		var result = await this.UseAsync(provider => provider.UpdateSupplierAsync(stored));

		// Assert
		Assert.AreEqual(1, result);
		Assert.AreEqual(12, (await this.UseAsync(provider => provider.GetSupplierByIdAsync(stored.Id))).LeadTimeDays);
	}

	[TestMethod]
	public async Task DeleteSupplierAsync_NotAssigned_Removed()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddSupplierAsync(new Supplier("Acme")));
		var stored = (await this.UseAsync(provider => provider.GetAllSuppliersAsync())).Single();

		// Act
		var result = await this.UseAsync(provider => provider.DeleteSupplierAsync(stored));

		// Assert
		Assert.AreEqual(1, result);
		Assert.IsNull(await this.UseAsync(provider => provider.GetSupplierByIdAsync(stored.Id)));
	}

	[TestMethod]
	public async Task DeleteSupplierAsync_AssignedToStockItem_ThrowsSupplierInUse()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddSupplierAsync(new Supplier("Acme")));
		var supplier = (await this.UseAsync(provider => provider.GetAllSuppliersAsync())).Single();
		await this.UseStockItemsAsync(stockItems => stockItems.AddStockItemAsync(new StockItem("Screw", code: "A1") { SupplierId = supplier.Id }));

		// Act + Assert
		await Assert.ThrowsExceptionAsync<SupplierInUseException>(() => this.UseAsync(provider => provider.DeleteSupplierAsync(supplier)));
	}


	private async Task<T> UseAsync<T>(Func<ISupplierServiceProvider, Task<T>> action)
	{
		await using var scope = _services.CreateAsyncScope();
		return await action(scope.ServiceProvider.GetRequiredService<ISupplierServiceProvider>());
	}

	private Task UseAsync(Func<ISupplierServiceProvider, Task> action)
	{
		return this.UseAsync<object?>(async provider =>
		{
			await action(provider);
			return null;
		});
	}

	private async Task UseStockItemsAsync(Func<IStockItemServiceProvider, Task> action)
	{
		await using var scope = _services.CreateAsyncScope();
		await action(scope.ServiceProvider.GetRequiredService<IStockItemServiceProvider>());
	}
}
