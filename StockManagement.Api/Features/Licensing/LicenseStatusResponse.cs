using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Api.Features.Licensing;


public sealed record LicenseStatusResponse(LicenseStatus Status, LicensePlan Plan, DateTime TrialEndsAtUtc, DateTime? GraceEndsAtUtc, DateTime? SubscriptionExpiresAtUtc, int DaysRemaining, int MonthlyPricePyg, int YearlyPricePyg, string MachineId, int? DiscountPercent, int? EffectivePricePyg)
{
	public static LicenseStatusResponse From(LicenseSnapshot snapshot, LicensingOptions options)
	{
		return new LicenseStatusResponse(snapshot.Status, snapshot.Plan, snapshot.TrialEndsAtUtc, snapshot.GraceEndsAtUtc, snapshot.SubscriptionExpiresAtUtc, snapshot.DaysRemaining, options.MonthlyPricePyg, options.YearlyPricePyg, snapshot.MachineId, snapshot.DiscountPercent, snapshot.EffectivePricePyg);
	}
}
