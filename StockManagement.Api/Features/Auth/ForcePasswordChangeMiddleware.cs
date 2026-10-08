using StockManagement.Kernel.Database.Interfaces;

namespace StockManagement.Api.Features.Auth;


/// <summary>
/// Blocks every other endpoint for a user with <c>User.MustChangePassword</c> set (the seeded admin, #244), until
/// they change it via <see cref="ChangePasswordEndpoint"/>. Auth and feedback stay reachable, same exemptions as
/// <c>LicenseEnforcementMiddleware</c> (ADR-0046).
/// </summary>
public sealed class ForcePasswordChangeMiddleware(RequestDelegate next)
{
	private readonly RequestDelegate _next = next;


	public async Task InvokeAsync(HttpContext context, IUserServiceProvider userServiceProvider)
	{
		if (context.User.Identity?.IsAuthenticated != true || !this.RequiresCheck(context.Request.Path))
		{
			await _next(context);
			return;
		}

		var userId = context.User.FindFirst("sub")?.Value;
		var user = userId is null ? null : await userServiceProvider.GetUserAsync(userId, context.RequestAborted);
		if (user?.MustChangePassword == true)
		{
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
			await context.Response.WriteAsJsonAsync(new { code = "password_change_required" });
			return;
		}

		await _next(context);
	}

	private bool RequiresCheck(PathString path)
	{
		return path.StartsWithSegments("/api")
			&& !path.StartsWithSegments("/api/auth")
			&& !path.StartsWithSegments("/api/feedback");
	}
}
