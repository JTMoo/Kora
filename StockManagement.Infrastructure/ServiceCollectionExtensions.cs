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
		services.AddScoped<IStockItemServiceProvider, EfStockItemServiceProvider>();
		services.AddScoped<ICustomerServiceProvider, EfCustomerServiceProvider>();
		services.AddScoped<IInvoiceServiceProvider, EfInvoiceServiceProvider>();
		services.AddScoped<ICreditNoteServiceProvider, EfCreditNoteServiceProvider>();
		services.AddScoped<ISettingsServiceProvider, EfSettingsServiceProvider>();
		services.AddScoped<IUserServiceProvider, EfUserServiceProvider>();
		services.AddScoped<IImportBatchServiceProvider, EfImportBatchServiceProvider>();
		services.AddScoped<ISupplierServiceProvider, EfSupplierServiceProvider>();
		services.AddScoped<ISupplierInvoiceServiceProvider, EfSupplierInvoiceServiceProvider>();
		services.AddScoped<ISearchServiceProvider, EfSearchServiceProvider>();
		services.AddScoped<IPendingTransmissionServiceProvider, EfPendingTransmissionServiceProvider>();
		services.AddScoped<IPaymentLinkServiceProvider, EfPaymentLinkServiceProvider>();
		services.AddScoped<IContingencyCdcRangeServiceProvider, EfContingencyCdcRangeServiceProvider>();
		services.AddScoped<IRemissionNoteServiceProvider, EfRemissionNoteServiceProvider>();
		services.AddScoped<IPendingRemisionTransmissionServiceProvider, EfPendingRemisionTransmissionServiceProvider>();
		services.AddScoped<IDebitNoteServiceProvider, EfDebitNoteServiceProvider>();
		services.AddScoped<IPendingDebitNoteTransmissionServiceProvider, EfPendingDebitNoteTransmissionServiceProvider>();
		services.AddScoped<IGoodsImportDocumentServiceProvider, EfGoodsImportDocumentServiceProvider>();
		services.AddScoped<IReportServiceProvider, EfReportServiceProvider>();
		return services;
	}
}
