using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Infrastructure.Database;
using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Infrastructure;


public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers <see cref="AppDbContext"/> on <c>ConnectionStrings:Postgres</c>
	/// </summary>
	/// <exception cref="InvalidOperationException">The connection string is missing</exception>
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("ConnectionStrings:Postgres is missing.");
		return services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
	}

	/// <summary>
	/// Registers the Kernel service provider interfaces against their <see cref="AppDbContext"/> implementations
	/// </summary>
	public static IServiceCollection AddInfrastructureServiceProviders(this IServiceCollection services)
	{
		services.AddScoped<IStockItemServiceProvider, StockItemServiceProvider>();
		services.AddScoped<ICustomerServiceProvider, CustomerServiceProvider>();
		services.AddScoped<IInvoiceServiceProvider, InvoiceServiceProvider>();
		services.AddScoped<ICreditNoteServiceProvider, CreditNoteServiceProvider>();
		services.AddScoped<ISettingsServiceProvider, SettingsServiceProvider>();
		services.AddScoped<IUserServiceProvider, UserServiceProvider>();
		services.AddScoped<IImportBatchServiceProvider, ImportBatchServiceProvider>();
		services.AddScoped<ISupplierServiceProvider, SupplierServiceProvider>();
		services.AddScoped<ISupplierInvoiceServiceProvider, SupplierInvoiceServiceProvider>();
		services.AddScoped<ISearchServiceProvider, SearchServiceProvider>();
		services.AddScoped<IPendingTransmissionServiceProvider, PendingTransmissionServiceProvider>();
		services.AddScoped<IPaymentLinkServiceProvider, PaymentLinkServiceProvider>();
		services.AddScoped<IContingencyCdcRangeServiceProvider, ContingencyCdcRangeServiceProvider>();
		services.AddScoped<IRemissionNoteServiceProvider, RemissionNoteServiceProvider>();
		services.AddScoped<IPendingRemisionTransmissionServiceProvider, PendingRemisionTransmissionServiceProvider>();
		services.AddScoped<IDebitNoteServiceProvider, DebitNoteServiceProvider>();
		services.AddScoped<IPendingDebitNoteTransmissionServiceProvider, PendingDebitNoteTransmissionServiceProvider>();
		services.AddScoped<IGoodsImportDocumentServiceProvider, GoodsImportDocumentServiceProvider>();
		services.AddScoped<IReportServiceProvider, ReportServiceProvider>();
		services.AddScoped<ICancellationRequestServiceProvider, CancellationRequestServiceProvider>();
		services.AddScoped<IInvoiceNumberVoidServiceProvider, InvoiceNumberVoidServiceProvider>();
		services.AddScoped<ICashRegisterSessionServiceProvider, CashRegisterSessionServiceProvider>();
		services.AddScoped<ILicenseServiceProvider, LicenseServiceProvider>();
		return services;
	}
}
