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
		var effectiveNowUtc = await this.ClampToHighWaterMarkAsync(state, cancellationToken);
		return LicenseCalculator.Compute(state, this.VerifyActivated(state), _options, effectiveNowUtc);
	}

	public async Task<(bool Success, LicenseSnapshot Status)> TryActivateAsync(string licenseKey, CancellationToken cancellationToken = default)
	{
		if (!_tokenVerifier.TryVerify(licenseKey, out var token) || token is null)
		{
			return (false, await this.GetStatusAsync(cancellationToken));
		}

		var state = await this.GetOrStartTrialAsync(cancellationToken);
		var nowUtc = await this.ClampToHighWaterMarkAsync(state, cancellationToken);

		if (token.ExpiresAtUtc <= nowUtc || !IsForThisMachine(token, state))
		{
			return (false, await this.GetStatusAsync(cancellationToken));
		}

		state.ActivatedLicenseKey = licenseKey;
		state.ActivatedAtUtc = nowUtc;
		await _provider.UpdateAsync(state, cancellationToken);

		return (true, LicenseCalculator.Compute(state, token, _options, nowUtc));
	}

	private async Task<LicenseState> GetOrStartTrialAsync(CancellationToken cancellationToken)
	{
		var state = await _provider.GetAsync(cancellationToken);
		if (state is null)
		{
			state = new LicenseState { TrialStartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime, MachineId = Guid.NewGuid().ToString() };
			await _provider.AddAsync(state, cancellationToken);
			return state;
		}

		// An existing DB upgraded onto ADR-0044 has no MachineId yet - backfill it once, same lazy-init as the trial start
		if (string.IsNullOrEmpty(state.MachineId))
		{
			state.MachineId = Guid.NewGuid().ToString();
			await _provider.UpdateAsync(state, cancellationToken);
		}

		return state;
	}

	/// <summary>
	/// Clamps the wall clock to the latest UTC instant this install has ever observed, and persists any advance.
	/// Rolling the system clock backward then reads as "no time passed" instead of rewinding the trial/subscription.
	/// </summary>
	private async Task<DateTime> ClampToHighWaterMarkAsync(LicenseState state, CancellationToken cancellationToken)
	{
		var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
		if (nowUtc <= state.HighWaterMarkUtc) return state.HighWaterMarkUtc;

		state.HighWaterMarkUtc = nowUtc;
		await _provider.UpdateAsync(state, cancellationToken);
		return nowUtc;
	}

	private LicenseToken? VerifyActivated(LicenseState state)
	{
		if (string.IsNullOrEmpty(state.ActivatedLicenseKey)) return null;
		if (!_tokenVerifier.TryVerify(state.ActivatedLicenseKey, out var token) || token is null) return null;
		return IsForThisMachine(token, state) ? token : null;
	}

	/// <summary>
	/// A key with no <see cref="LicenseToken.MachineId"/> is unbound (legacy/manual issuance); one with a MachineId
	/// must match this install's, so an activated key cannot simply be copied onto another machine (ADR-0044)
	/// </summary>
	private static bool IsForThisMachine(LicenseToken token, LicenseState state)
	{
		return token.MachineId is null || token.MachineId == state.MachineId;
	}
}
