using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Api.Features.Auth;


public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);


public class ChangePasswordValidator : Validator<ChangePasswordRequest>
{
	public ChangePasswordValidator()
	{
		this.RuleFor(request => request.CurrentPassword).NotEmpty().WithMessage("passwordRequired");
		this.RuleFor(request => request.NewPassword).NotEmpty().WithMessage("passwordRequired").MinimumLength(8).WithMessage("passwordTooShort");
	}
}


/// <remarks>
/// Self-service only: the target is the caller's own id from the JWT, never a request field. Clears
/// <see cref="User.MustChangePassword"/> (ADR-0046). A wrong current password is a 400 (<see cref="IncorrectCurrentPassword"/>),
/// not 401 - a 401 here would trip the web client's shared "session expired" handler and log the caller out.
/// </remarks>
public class ChangePasswordEndpoint(IUserServiceProvider userServiceProvider, IAuthService authService) : Endpoint<ChangePasswordRequest, Results<Ok, UnauthorizedHttpResult>>
{
	public const string IncorrectCurrentPassword = "incorrectCurrentPassword";

	private readonly IUserServiceProvider _userServiceProvider = userServiceProvider;
	private readonly IAuthService _authService = authService;


	public override void Configure()
	{
		this.Put("/auth/password");
	}

	public override async Task<Results<Ok, UnauthorizedHttpResult>> ExecuteAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
	{
		var userId = this.User.FindFirst("sub")?.Value ?? "";
		if (await _userServiceProvider.GetUserAsync(userId, cancellationToken) is not User user) return TypedResults.Unauthorized();
		if (await _authService.ValidateCredentialsAsync(user.Username, request.CurrentPassword, cancellationToken) is null) this.ThrowError(request => request.CurrentPassword, IncorrectCurrentPassword);

		user.PasswordHash = _authService.HashPassword(request.NewPassword);
		user.MustChangePassword = false;
		await _userServiceProvider.UpdateUserAsync(user, cancellationToken);

		return TypedResults.Ok();
	}
}
