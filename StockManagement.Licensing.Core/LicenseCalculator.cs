using StockManagement.Kernel.Model;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Licensing.Core;


/// <summary>
/// Pure status computation, kept separate from <see cref="LicenseService"/> so it is unit-testable without a database
/// </summary>
internal static class LicenseCalculator
{
	public static LicenseSnapshot Compute(LicenseState state, LicenseToken? activeToken, LicensingOptions options, DateTime nowUtc)
	{
		var trialEndsAtUtc = state.TrialStartedAtUtc.AddDays(options.TrialDays);

		if (activeToken is not null && activeToken.ExpiresAtUtc > nowUtc)
		{
			return new LicenseSnapshot(LicenseStatus.Active, activeToken.Plan, trialEndsAtUtc, null, activeToken.ExpiresAtUtc, DaysUntil(activeToken.ExpiresAtUtc, nowUtc), state.MachineId, activeToken.DiscountPercent, activeToken.EffectivePricePyg);
		}

		if (nowUtc < trialEndsAtUtc)
		{
			return new LicenseSnapshot(LicenseStatus.Trial, LicensePlan.None, trialEndsAtUtc, null, activeToken?.ExpiresAtUtc, DaysUntil(trialEndsAtUtc, nowUtc), state.MachineId);
		}

		// Grace period runs from whichever lapsed more recently: the trial, or an expired subscription
		var graceStartsAtUtc = activeToken is not null && activeToken.ExpiresAtUtc > trialEndsAtUtc ? activeToken.ExpiresAtUtc : trialEndsAtUtc;
		var graceEndsAtUtc = graceStartsAtUtc.AddDays(options.GraceDays);

		if (nowUtc < graceEndsAtUtc)
		{
			return new LicenseSnapshot(LicenseStatus.GracePeriod, activeToken?.Plan ?? LicensePlan.None, trialEndsAtUtc, graceEndsAtUtc, activeToken?.ExpiresAtUtc, DaysUntil(graceEndsAtUtc, nowUtc), state.MachineId);
		}

		return new LicenseSnapshot(LicenseStatus.Locked, activeToken?.Plan ?? LicensePlan.None, trialEndsAtUtc, graceEndsAtUtc, activeToken?.ExpiresAtUtc, 0, state.MachineId);
	}

	private static int DaysUntil(DateTime deadlineUtc, DateTime nowUtc)
	{
		return Math.Max(0, (int)Math.Ceiling((deadlineUtc - nowUtc).TotalDays));
	}
}
