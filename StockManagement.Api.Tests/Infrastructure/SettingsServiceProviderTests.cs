using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StockManagement.Infrastructure;
using StockManagement.Infrastructure.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Tests.Infrastructure;


[TestClass]
public sealed class SettingsServiceProviderTests
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
			.AddScoped<ISettingsServiceProvider, SettingsServiceProvider>()
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
	public async Task GetSettingsAsync_NothingStored_ReturnsNull()
	{
		// Act
		var result = await this.UseAsync(provider => provider.GetSettingsAsync());

		// Assert
		Assert.IsNull(result);
	}

	[TestMethod]
	public async Task AddSettingsAsync_Stored_GetSettingsAsyncReturnsIt()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddSettingsAsync(new AppSettings { Language = AvailableLanguages.Spanish }));

		// Act
		var result = await this.UseAsync(provider => provider.GetSettingsAsync());

		// Assert
		Assert.AreEqual(AvailableLanguages.Spanish, result!.Language);
	}

	[TestMethod]
	public async Task UpdateSettingsAsync_Existing_PersistsChange()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddSettingsAsync(new AppSettings { Language = AvailableLanguages.German }));
		var stored = await this.UseAsync(provider => provider.GetSettingsAsync());
		stored!.Language = AvailableLanguages.English;

		// Act
		var result = await this.UseAsync(provider => provider.UpdateSettingsAsync(stored));

		// Assert
		Assert.AreEqual(1, result);
		Assert.AreEqual(AvailableLanguages.English, (await this.UseAsync(provider => provider.GetSettingsAsync()))!.Language);
	}

	[TestMethod]
	public async Task AddSettingsAsync_CompanyFieldsStored_GetSettingsAsyncReturnsThem()
	{
		// Arrange
		await this.UseAsync(provider => provider.AddSettingsAsync(new AppSettings
		{
			CompanyName = "Acme",
			TaxId = "123456",
			Currency = "PYG",
			VatRatePercent = 5.5m,
			PaymentTermInDays = 14,
			FirstInvoiceNumber = 100,
			FirstCustomerId = 2000
		}));

		// Act
		var result = await this.UseAsync(provider => provider.GetSettingsAsync());

		// Assert
		Assert.AreEqual("Acme", result!.CompanyName);
		Assert.AreEqual("123456", result.TaxId);
		Assert.AreEqual("PYG", result.Currency);
		Assert.AreEqual(5.5m, result.VatRatePercent);
		Assert.AreEqual(14, result.PaymentTermInDays);
		Assert.AreEqual(100, result.FirstInvoiceNumber);
		Assert.AreEqual(2000, result.FirstCustomerId);
	}


	private async Task<T> UseAsync<T>(Func<ISettingsServiceProvider, Task<T>> action)
	{
		await using var scope = _services.CreateAsyncScope();
		return await action(scope.ServiceProvider.GetRequiredService<ISettingsServiceProvider>());
	}

	private Task UseAsync(Func<ISettingsServiceProvider, Task> action)
	{
		return this.UseAsync<object?>(async provider =>
		{
			await action(provider);
			return null;
		});
	}
}
