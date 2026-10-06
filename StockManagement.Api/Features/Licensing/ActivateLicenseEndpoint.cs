using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Api.Features.Licensing;


public sealed record ActivateLicenseRequest(string LicenseKey);

public sealed record LicenseKeyInvalidResponse(string Reason);


public class ActivateLicenseValidator : Validator<ActivateLicenseRequest>
{
	public ActivateLicenseValidator()
	{
		this.RuleFor(request => request.LicenseKey).NotEmpty().WithMessage("licenseKeyRequired");
	}
}


public class ActivateLicenseEndpoint(ILicenseService licenseService, IOptions<LicensingOptions> options) : Endpoint<ActivateLicenseRequest, Results<Ok<LicenseStatusResponse>, UnprocessableEntity<LicenseKeyInvalidResponse>>>
{
	private readonly ILicenseService _licenseService = licenseService;
	private readonly LicensingOptions _options = options.Value;


	public override void Configure()
	{
		this.Post("/license/activate");
		this.Permissions(Permission.SettingsWrite);
	}

	public override async Task<Results<Ok<LicenseStatusResponse>, UnprocessableEntity<LicenseKeyInvalidResponse>>> ExecuteAsync(ActivateLicenseRequest request, CancellationToken cancellationToken)
	{
		var (success, status) = await _licenseService.TryActivateAsync(request.LicenseKey, cancellationToken);
		if (!success) return TypedResults.UnprocessableEntity(new LicenseKeyInvalidResponse("licenseKeyInvalid"));

		return TypedResults.Ok(LicenseStatusResponse.From(status, _options));
	}
}
