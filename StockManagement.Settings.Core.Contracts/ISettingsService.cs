using StockManagement.Kernel.Model.Types;

namespace StockManagement.Settings.Core.Contracts;


public interface ISettingsService
{
	/// <summary>
	/// The application's UI language
	/// </summary>
	public Task<AvailableLanguages> GetLanguageAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Stores the application's UI language
	/// </summary>
	public Task SetLanguageAsync(AvailableLanguages language, CancellationToken cancellationToken = default);

	/// <summary>
	/// The company's settings, or defaults if none were stored yet
	/// </summary>
	public Task<CompanySettings> GetCompanySettingsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Stores the company's settings
	/// </summary>
	public Task SetCompanySettingsAsync(CompanySettings settings, CancellationToken cancellationToken = default);

	/// <summary>
	/// The printer settings used for receipts, or defaults if none were stored yet
	/// </summary>
	public Task<PrinterSettings> GetPrinterSettingsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Stores the printer settings used for receipts
	/// </summary>
	public Task SetPrinterSettingsAsync(PrinterSettings settings, CancellationToken cancellationToken = default);
}
