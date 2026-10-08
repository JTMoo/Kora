using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Api.Features.Licensing;


/// <summary>
/// Blocks API calls once the trial and any grace period have lapsed with no active subscription (ADR-0041).
/// Auth, the license endpoints, feedback (ADR-0042 - must stay reachable so a locked-out user can report it),
/// and non-API requests (the React build, health check) stay reachable.
/// </summary>
public sealed class LicenseEnforcementMiddleware(RequestDelegate next)
{
	private readonly RequestDelegate _next = next;


	public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
	{
		if (!this.RequiresLicense(context.Request.Path))
		{
			await _next(context);
			return;
		}

		var status = await licenseService.GetStatusAsync(context.RequestAborted);
		if (status.Status == LicenseStatus.Locked)
		{
			context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
			await context.Response.WriteAsJsonAsync(new { code = "license_expired" });
			return;
		}

		await _next(context);
	}

	private bool RequiresLicense(PathString path)
	{
		return path.StartsWithSegments("/api")
			&& !path.StartsWithSegments("/api/auth")
			&& !path.StartsWithSegments("/api/license")
			&& !path.StartsWithSegments("/api/feedback");
	}
}
