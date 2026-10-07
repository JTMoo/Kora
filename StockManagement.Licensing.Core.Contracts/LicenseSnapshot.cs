namespace StockManagement.Licensing.Core.Contracts;


/// <param name="Status">Current enforcement state</param>
/// <param name="Plan">Activated plan, or <see cref="LicensePlan.None"/> during trial/locked</param>
/// <param name="TrialEndsAtUtc">When the free trial ends (set on first use)</param>
/// <param name="GraceEndsAtUtc">When the post-trial grace period ends; <see langword="null"/> once consumed or never entered</param>
/// <param name="SubscriptionExpiresAtUtc">Current activation's expiry, or <see langword="null"/> if never activated</param>
/// <param name="DaysRemaining">Days left in <see cref="Status"/> (trial, grace, or active subscription); 0 once <see cref="LicenseStatus.Locked"/></param>
public sealed record LicenseSnapshot(LicenseStatus Status, LicensePlan Plan, DateTime TrialEndsAtUtc, DateTime? GraceEndsAtUtc, DateTime? SubscriptionExpiresAtUtc, int DaysRemaining);
