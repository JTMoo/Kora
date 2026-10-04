using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers the Sifen services: CDC generation, DTE XML building, XAdES signing, XSD validation, the direct-DNIT
	/// <see cref="ISifenGateway"/> (ADR-0031, #134), and the outbox retry worker.
	/// </summary>
	public static IServiceCollection AddSifenCore(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddScoped<ICdcGenerator, CdcGenerator>();
		services.AddScoped<IDteXmlBuilder, DteXmlBuilder>();
		services.AddScoped<IXadesSigner, XadesSigner>();
		services.AddScoped<IDteXsdValidator, DteXsdValidator>();
		services.AddScoped<IContingencyCdcIssuer, ContingencyCdcIssuer>();
		services.AddScoped<IKudeHtmlBuilder, KudeHtmlBuilder>();
		services.AddScoped<IKudeQrCodeGenerator, KudeQrCodeGenerator>();
		services.AddScoped<IKudeVerificationUrlBuilder, KudeVerificationUrlBuilder>();

		services.Configure<SifenGatewayOptions>(configuration.GetSection(SifenGatewayOptions.SectionName));
		services.AddHttpClient(nameof(DirectDnitSifenGateway));
		services.AddScoped<ISifenGateway, DirectDnitSifenGateway>();
		services.AddHostedService<SifenTransmissionWorker>();
		return services;
	}
}
