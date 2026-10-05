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
public sealed class CustomerServiceProviderTests
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
			.AddScoped<ICustomerServiceProvider, CustomerServiceProvider>()
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
	public async Task AddCustomerAsync_DuplicateCustomerId_ThrowsCustomerIdAlreadyExists()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddCustomerAsync(new Customer { CustomerId = 1001, Name = "Ann" }));

		// Act + Assert
		await Assert.ThrowsExceptionAsync<CustomerIdAlreadyExistsException>(() => this.UseAsync(provider => provider.AddCustomerAsync(new Customer { CustomerId = 1001, Name = "Bea" })));
	}

	[TestMethod]
	public async Task GetCustomerAsync_Stored_ReturnsIt()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddCustomerAsync(new Customer { CustomerId = 1001, Name = "Ann" }));

		// Act
		var customer = await this.UseAsync(provider => provider.GetCustomerAsync(1001));

		// Assert
		Assert.AreEqual("Ann", customer.Name);
	}

	[TestMethod]
	public async Task UpdateCustomerAsync_Existing_PersistsChanges()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddCustomerAsync(new Customer { CustomerId = 1001, Name = "Ann" }));
		var stored = await this.UseAsync(provider => provider.GetCustomerAsync(1001));
		stored.Name = "Bea";

		// Act
		var result = await this.UseAsync(provider => provider.UpdateCustomerAsync(stored));

		// Assert
		Assert.AreEqual(1, result);
		Assert.AreEqual("Bea", (await this.UseAsync(provider => provider.GetCustomerAsync(1001))).Name);
	}

	[TestMethod]
	public async Task DeleteCustomerAsync_Existing_Removed()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddCustomerAsync(new Customer { CustomerId = 1001, Name = "Ann" }));
		var stored = await this.UseAsync(provider => provider.GetCustomerAsync(1001));

		// Act
		var result = await this.UseAsync(provider => provider.DeleteCustomerAsync(stored));

		// Assert
		Assert.AreEqual(1, result);
		Assert.IsNull(await this.UseAsync(provider => provider.GetCustomerAsync(1001)));
	}


	private async Task<T> UseAsync<T>(Func<ICustomerServiceProvider, Task<T>> action)
	{
		await using var scope = _services.CreateAsyncScope();
		return await action(scope.ServiceProvider.GetRequiredService<ICustomerServiceProvider>());
	}

	private Task UseAsync(Func<ICustomerServiceProvider, Task> action)
	{
		return this.UseAsync<object?>(async provider =>
		{
			await action(provider);
			return null;
		});
	}
}
