using StockManagement.Kernel.Model.Types;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Api.Features.Settings;


public sealed record PrinterSettingsResponse(string DefaultPrinterName, int ReceiptPaperWidthMm, KudeFormat KudeFormat, bool PrintOnSaleComplete)
{
	public static PrinterSettingsResponse From(PrinterSettings settings)
	{
		return new PrinterSettingsResponse(settings.DefaultPrinterName, settings.ReceiptPaperWidthMm, settings.KudeFormat, settings.PrintOnSaleComplete);
	}
}
