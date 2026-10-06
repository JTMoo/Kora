using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Licensing.Core;


public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers trial/subscription status, offline key verification (ADR-0041). Expects the Kernel service providers to be registered.
	/// </summary>
	public static IServiceCollection AddLicensingCore(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<LicensingOptions>(configuration.GetSection(LicensingOptions.SectionName));
		services.AddSingleton(TimeProvider.System);
		services.AddSingleton<ILicenseTokenVerifier, EcdsaLicenseTokenVerifier>();
		services.AddScoped<ILicenseService, LicenseService>();
		return services;
	}
}
