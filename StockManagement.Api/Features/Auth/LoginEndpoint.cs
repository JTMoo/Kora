using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Api.Features.Auth;


public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token, string Username, UserRole Role, IReadOnlyList<string> Permissions, bool MustChangePassword);


public class LoginValidator : Validator<LoginRequest>
{
	public LoginValidator()
	{
		this.RuleFor(request => request.Username).NotEmpty().WithMessage("usernameRequired");
		this.RuleFor(request => request.Password).NotEmpty().WithMessage("passwordRequired");
	}
}


/// <remarks>Stateless JWT: no server-side session, "logout" just discards the token client-side. See ADR-0010, ADR-0017.</remarks>
public class LoginEndpoint(IAuthService authService, IConfiguration configuration) : Endpoint<LoginRequest, Results<Ok<LoginResponse>, UnauthorizedHttpResult>>
{
	private readonly IAuthService _authService = authService;
	private readonly IConfiguration _configuration = configuration;


	public override void Configure()
	{
		this.Post("/auth/login");
		this.AllowAnonymous();
		this.Throttle(5, 60); // 5 attempts/minute per client IP (ADR-0046); unlimited password guessing otherwise (#238)
	}

	public override async Task<Results<Ok<LoginResponse>, UnauthorizedHttpResult>> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken)
	{
		var user = await _authService.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
		if (user is null) return TypedResults.Unauthorized();

		var signingKey = _configuration["Jwt:SigningKey"]!;
		var expiryHours = _configuration.GetValue("Jwt:ExpiryHours", 12);
		var permissions = Permission.Effective(user);

		var token = JwtBearer.CreateToken(options =>
		{
			options.SigningKey = signingKey;
			options.ExpireAt = DateTime.UtcNow.AddHours(expiryHours);
			options.User["sub"] = user.Id;
			options.User["username"] = user.Username;
			options.User.Roles.Add(user.Role.ToString());
			options.User.Permissions.AddRange(permissions);
		});

		return TypedResults.Ok(new LoginResponse(token, user.Username, user.Role, permissions, user.MustChangePassword));
	}
}
