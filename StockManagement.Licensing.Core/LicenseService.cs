using Microsoft.Extensions.Options;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Licensing.Core;


internal sealed class LicenseService(ILicenseServiceProvider provider, ILicenseTokenVerifier tokenVerifier, IOptions<LicensingOptions> options, TimeProvider timeProvider) : ILicenseService
{
	private readonly ILicenseServiceProvider _provider = provider;
	private readonly ILicenseTokenVerifier _tokenVerifier = tokenVerifier;
	private readonly LicensingOptions _options = options.Value;
	private readonly TimeProvider _timeProvider = timeProvider;


	public async Task<LicenseSnapshot> GetStatusAsync(CancellationToken cancellationToken = default)
	{
		var state = await this.GetOrStartTrialAsync(cancellationToken);
		return LicenseCalculator.Compute(state, this.VerifyActivated(state), _options, _timeProvider.GetUtcNow().UtcDateTime);
	}

	public async Task<(bool Success, LicenseSnapshot Status)> TryActivateAsync(string licenseKey, CancellationToken cancellationToken = default)
	{
		if (!_tokenVerifier.TryVerify(licenseKey, out var token) || token is null)
		{
			return (false, await this.GetStatusAsync(cancellationToken));
		}

		var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
		if (token.ExpiresAtUtc <= nowUtc)
		{
			return (false, await this.GetStatusAsync(cancellationToken));
		}

		var state = await this.GetOrStartTrialAsync(cancellationToken);
		state.ActivatedLicenseKey = licenseKey;
		state.ActivatedAtUtc = nowUtc;
		await _provider.UpdateAsync(state, cancellationToken);

		return (true, LicenseCalculator.Compute(state, token, _options, nowUtc));
	}

	private async Task<LicenseState> GetOrStartTrialAsync(CancellationToken cancellationToken)
	{
		var state = await _provider.GetAsync(cancellationToken);
		if (state is not null) return state;

		state = new LicenseState { TrialStartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime };
		await _provider.AddAsync(state, cancellationToken);
		return state;
	}

	private LicenseToken? VerifyActivated(LicenseState state)
	{
		if (string.IsNullOrEmpty(state.ActivatedLicenseKey)) return null;
		return _tokenVerifier.TryVerify(state.ActivatedLicenseKey, out var token) ? token : null;
	}
}
