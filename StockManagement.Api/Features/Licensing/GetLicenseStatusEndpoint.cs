using FastEndpoints;
using Microsoft.Extensions.Options;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Api.Features.Licensing;


/// <remarks>No permission gate (unlike other endpoints): every authenticated user needs this to render the trial/lockout banner, not just Settings.Read holders.</remarks>
public class GetLicenseStatusEndpoint(ILicenseService licenseService, IOptions<LicensingOptions> options) : EndpointWithoutRequest<LicenseStatusResponse>
{
	private readonly ILicenseService _licenseService = licenseService;
	private readonly LicensingOptions _options = options.Value;


	public override void Configure()
	{
		this.Get("/license");
	}

	public override async Task<LicenseStatusResponse> ExecuteAsync(CancellationToken cancellationToken)
	{
		return LicenseStatusResponse.From(await _licenseService.GetStatusAsync(cancellationToken), _options);
	}
}
