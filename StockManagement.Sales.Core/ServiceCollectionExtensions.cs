using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Sales.Core;


public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers the sales services, including the Bancard <see cref="IPaymentLinkGateway"/> (#150). Expects the
	/// Kernel service providers to be registered.
	/// </summary>
	public static IServiceCollection AddSalesCore(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddScoped<ISaleService, SaleService>();
		services.AddScoped<ICreditNoteService, CreditNoteService>();
		services.AddScoped<IRemissionNoteService, RemissionNoteService>();
		services.AddScoped<IDebitNoteService, DebitNoteService>();
		services.AddScoped<ISifenEventService, SifenEventService>();
		services.AddScoped<IPaymentService, PaymentService>();
		services.AddScoped<IIvaBookExportService, IvaBookExportService>();
		services.AddScoped<IPurchaseIvaBookExportService, PurchaseIvaBookExportService>();

		services.Configure<BancardGatewayOptions>(configuration.GetSection(BancardGatewayOptions.SectionName));
		services.AddHttpClient(nameof(BancardPaymentLinkGateway));
		services.AddScoped<IPaymentLinkGateway, BancardPaymentLinkGateway>();
		services.AddScoped<IPaymentLinkService, PaymentLinkService>();
		return services;
	}
}
