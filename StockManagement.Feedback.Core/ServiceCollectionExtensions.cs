using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Feedback.Core.Contracts;

namespace StockManagement.Feedback.Core;


public static class ServiceCollectionExtensions
{
	/// <summary>Registers the feedback service and its <see cref="IReportSink"/> (ADR-0042).</summary>
	public static IServiceCollection AddFeedbackCore(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<FeedbackOptions>(configuration.GetSection(FeedbackOptions.SectionName));
		services.AddScoped<IFeedbackService, FeedbackService>();

		if (string.IsNullOrWhiteSpace(configuration[$"{FeedbackOptions.SectionName}:RelayUrl"]))
			services.AddScoped<IReportSink, NullReportSink>();
		else
		{
			services.AddHttpClient(nameof(HttpReportSink));
			services.AddScoped<IReportSink, HttpReportSink>();
		}

		return services;
	}
}
