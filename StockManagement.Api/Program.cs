using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Auth.Core;
using StockManagement.Customers.Core;
using StockManagement.Api.Features.Auth;
using StockManagement.Api.Features.Licensing;
using StockManagement.Api.Security;
using StockManagement.Feedback.Core;
using StockManagement.Import.Core;
using StockManagement.Infrastructure;
using StockManagement.Infrastructure.Database;
using StockManagement.Licensing.Core;
using StockManagement.Sales.Core;
using StockManagement.Settings.Core;
using StockManagement.Sifen.Core;

var builder = WebApplication.CreateBuilder(args);

var jwtSigningKey = JwtSigningKeyProvider.Resolve(builder.Configuration["Jwt:SigningKey"], builder.Environment.IsDevelopment());
builder.Configuration["Jwt:SigningKey"] = jwtSigningKey; // keeps LoginEndpoint (reads IConfiguration) in sync with the resolved, possibly-generated key

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
	.AddFeedbackCore(builder.Configuration)
	.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHealthChecks();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
	await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

// Unhandled exceptions get a correlation id in the response (ADR-0042): the web client offers to send a report
// for it via POST /feedback, same endpoint the manual "report a problem" action uses.
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
	var correlationId = Guid.NewGuid().ToString();
	var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

	context.RequestServices.GetRequiredService<ILogger<Program>>()
		.LogError(exception, "Unhandled exception {CorrelationId} on {Path}.", correlationId, context.Request.Path);

	context.Response.StatusCode = StatusCodes.Status500InternalServerError;
	await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
	{
		Status = StatusCodes.Status500InternalServerError,
		Title = "An unexpected error occurred.",
		Extensions = { ["correlationId"] = correlationId }
	});
}));

// Polled by StockManagement.Desktop to know when the bundled API is ready (ADR-0016)
app.MapHealthChecks("/health");

// React build (StockManagement.Web) lands in wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ForcePasswordChangeMiddleware>();
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
