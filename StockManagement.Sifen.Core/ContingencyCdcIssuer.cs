using System.Security.Cryptography;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Util;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


/// <summary>
/// <see cref="IContingencyCdcIssuer"/> on <see cref="IContingencyCdcRangeServiceProvider"/>
/// </summary>
public sealed class ContingencyCdcIssuer(
	IContingencyCdcRangeServiceProvider rangeServiceProvider,
	ICdcGenerator cdcGenerator,
	ISettingsService settingsService) : IContingencyCdcIssuer
{
	private readonly IContingencyCdcRangeServiceProvider _rangeServiceProvider = rangeServiceProvider;
	private readonly ICdcGenerator _cdcGenerator = cdcGenerator;
	private readonly ISettingsService _settingsService = settingsService;


	public async Task<ContingencyStatus> GetStatusAsync(CancellationToken cancellationToken = default)
	{
		var range = await _rangeServiceProvider.GetAsync(cancellationToken);
		return range is null
			? new ContingencyStatus(RangeConfigured: false, IsActive: false, 0, 0, 0)
			: new ContingencyStatus(RangeConfigured: true, range.IsActive, range.RangeStart, range.RangeEnd, range.NextNumber);
	}

	public Task ConfigureRangeAsync(long rangeStart, long rangeEnd, CancellationToken cancellationToken = default)
	{
		if (rangeEnd <= rangeStart) throw new ArgumentOutOfRangeException(nameof(rangeEnd), "rangeEnd must be after rangeStart.");
		return _rangeServiceProvider.SetRangeAsync(rangeStart, rangeEnd, cancellationToken);
	}

	public Task SetActiveAsync(bool active, CancellationToken cancellationToken = default)
	{
		return _rangeServiceProvider.SetActiveAsync(active, cancellationToken);
	}

	public async Task<string?> TryIssueAsync(SifenDocumentType documentType, CancellationToken cancellationToken = default)
	{
		if (await _rangeServiceProvider.TryReserveNextAsync(cancellationToken) is not long documentNumber) return null;

		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
		if (!RucValidator.TryNormalize(companySettings.Ruc, out var normalizedRuc))
			throw new InvalidOperationException("Company settings RUC is missing or invalid; set it before issuing a contingency CDC.");

		var rucParts = normalizedRuc.Split('-');

		return _cdcGenerator.Generate(new CdcInput(
			documentType,
			RucBase: rucParts[0],
			RucCheckDigit: int.Parse(rucParts[1]),
			companySettings.EstablishmentCode,
			companySettings.PointOfSaleCode,
			DocumentNumber: documentNumber,
			TaxpayerType.Juridica,
			IssueDate: DateOnly.FromDateTime(DateTime.Now),
			EmissionType.Contingencia,
			SecurityCode: RandomNumberGenerator.GetInt32(1_000_000_000).ToString("D9")));
	}
}
