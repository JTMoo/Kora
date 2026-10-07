using FastEndpoints;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Api.Features.Settings;


public class GetPrinterSettingsEndpoint(ISettingsService settingsService) : EndpointWithoutRequest<PrinterSettingsResponse>
{
	private readonly ISettingsService _settingsService = settingsService;


	public override void Configure()
	{
		this.Get("/printer-settings");
		this.Permissions(Permission.SettingsRead);
	}

	public override async Task<PrinterSettingsResponse> ExecuteAsync(CancellationToken cancellationToken)
	{
		return PrinterSettingsResponse.From(await _settingsService.GetPrinterSettingsAsync(cancellationToken));
	}
}
