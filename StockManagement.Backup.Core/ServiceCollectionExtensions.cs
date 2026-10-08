using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Backup.Core.Contracts;

namespace StockManagement.Backup.Core;


public static class ServiceCollectionExtensions
{
	/// <summary>Registers backup/restore via <c>pg_dump</c>/<c>pg_restore</c> (ADR-0045)</summary>
	public static IServiceCollection AddBackupCore(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<BackupOptions>(configuration.GetSection(BackupOptions.SectionName));
		services.AddSingleton<IProcessRunner, ProcessRunner>();
		services.AddSingleton<IBackendTerminator, NpgsqlBackendTerminator>();
		services.AddScoped<IBackupService, PgBackupService>();
		return services;
	}
}
