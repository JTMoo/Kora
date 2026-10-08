using FastEndpoints;
using FluentValidation;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Model.Types;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Api.Features.Settings;


public sealed record UpdatePrinterSettingsRequest(string DefaultPrinterName, int ReceiptPaperWidthMm, KudeFormat KudeFormat, bool PrintOnSaleComplete);


public class UpdatePrinterSettingsValidator : Validator<UpdatePrinterSettingsRequest>
{
	public UpdatePrinterSettingsValidator()
	{
		this.RuleFor(request => request.ReceiptPaperWidthMm).InclusiveBetween(20, 300).WithMessage("receiptPaperWidthMmOutOfRange");
	}
}


public class UpdatePrinterSettingsEndpoint(ISettingsService settingsService) : Endpoint<UpdatePrinterSettingsRequest, PrinterSettingsResponse>
{
	private readonly ISettingsService _settingsService = settingsService;


	public override void Configure()
	{
		this.Put("/printer-settings");
		this.Permissions(Permission.SettingsWrite);
	}

	public override async Task<PrinterSettingsResponse> ExecuteAsync(UpdatePrinterSettingsRequest request, CancellationToken cancellationToken)
	{
		var settings = new PrinterSettings(request.DefaultPrinterName, request.ReceiptPaperWidthMm, request.KudeFormat, request.PrintOnSaleComplete);
		await _settingsService.SetPrinterSettingsAsync(settings, cancellationToken);
		return PrinterSettingsResponse.From(settings);
	}
}
