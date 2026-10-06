using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Auth.Core;
using StockManagement.Customers.Core;
using StockManagement.Api.Features.Licensing;
using StockManagement.Import.Core;
using StockManagement.Infrastructure;
using StockManagement.Infrastructure.Database;
using StockManagement.Licensing.Core;
using StockManagement.Sales.Core;
using StockManagement.Settings.Core;
using StockManagement.Sifen.Core;

var builder = WebApplication.CreateBuilder(args);

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is missing.");

builder.Services
	.AddAuthenticationJwtBearer(options => options.SigningKey = jwtSigningKey)
	.AddAuthorization()
	.AddFastEndpoints()
	.AddInfrastructure(builder.Configuration)
	.AddInfrastructureServiceProviders()
	.AddSalesCore(builder.Configuration)
	.AddCustomersCore()
	.AddSettingsCore()
	.AddImportCore()
	.AddAuthCore()
	.AddSifenCore(builder.Configuration)
	.AddLicensingCore(builder.Configuration)
	.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHealthChecks();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
	await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

// Polled by StockManagement.Desktop to know when the bundled API is ready (ADR-0016)
app.MapHealthChecks("/health");

// React build (StockManagement.Web) lands in wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<LicenseEnforcementMiddleware>();

app.UseFastEndpoints(config =>
{
	config.Endpoints.RoutePrefix = "api";
	config.Errors.UseProblemDetails();
	config.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
});

await app.RunAsync();


/// <summary>
/// Entry point, public for <c>WebApplicationFactory</c>
/// </summary>
public partial class Program
{
}
