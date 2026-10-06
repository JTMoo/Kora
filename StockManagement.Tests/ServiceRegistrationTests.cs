using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StockManagement.Auth.Core;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Customers.Core;
using StockManagement.Customers.Core.Contracts;
using StockManagement.Feedback.Core;
using StockManagement.Feedback.Core.Contracts;
using StockManagement.Import.Core;
using StockManagement.Import.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Sales.Core;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests;


[TestClass]
public sealed class ServiceRegistrationTests
{
	/// <summary>
	/// Providers are Scoped in production (EF's <c>AppDbContext</c> isn't thread-safe); mocking them Scoped here
	/// catches a Core service wrongly registered Singleton (issue #97), which <c>ValidateScopes</c> only flags on resolve
	/// </summary>
	[TestMethod]
	public void AddCores_ResolveAgainstScopedProviderInterfaces_ResolvesEveryContractWithinScope()
	{
		// Arrange
		var services = new ServiceCollection()
			.AddScoped(_ => new Mock<IStockItemServiceProvider>().Object)
			.AddScoped(_ => new Mock<ICustomerServiceProvider>().Object)
			.AddScoped(_ => new Mock<IInvoiceServiceProvider>().Object)
			.AddScoped(_ => new Mock<ICreditNoteServiceProvider>().Object)
			.AddScoped(_ => new Mock<IUserServiceProvider>().Object)
			.AddScoped(_ => new Mock<ISettingsServiceProvider>().Object)
			.AddScoped(_ => new Mock<IImportBatchServiceProvider>().Object)
			.AddScoped(_ => new Mock<IPaymentLinkServiceProvider>().Object)
			.AddScoped(_ => new Mock<IContingencyCdcIssuer>().Object)
			.AddScoped(_ => new Mock<IRemissionNoteServiceProvider>().Object)
			.AddScoped(_ => new Mock<IGoodsImportDocumentServiceProvider>().Object)
			.AddScoped(_ => new Mock<IDebitNoteServiceProvider>().Object)
			.AddScoped(_ => new Mock<ISupplierInvoiceServiceProvider>().Object)
			.AddScoped(_ => new Mock<ICancellationRequestServiceProvider>().Object)
			.AddScoped(_ => new Mock<IInvoiceNumberVoidServiceProvider>().Object)
			.AddScoped(_ => new Mock<ICashRegisterSessionServiceProvider>().Object)
			.AddSalesCore(new ConfigurationBuilder().Build())
			.AddCustomersCore()
			.AddImportCore()
			.AddAuthCore()
			.AddSettingsCore()
			.AddFeedbackCore(new ConfigurationBuilder().Build());

		// Act
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true, ValidateScopes = true });
		using var scope = provider.CreateScope();

		// Assert
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<ISaleService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IPaymentService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<ISupplierPaymentService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IPaymentLinkService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<ICreditNoteService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IRemissionNoteService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IPurchaseIvaBookExportService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IDebitNoteService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<ISifenEventService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<ICashRegisterService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<ICustomerService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IStockItemImportService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IImportBatchService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IAuthService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<ISettingsService>());
		Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IFeedbackService>());
	}
}
