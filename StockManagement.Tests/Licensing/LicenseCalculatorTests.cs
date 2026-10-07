using StockManagement.Kernel.Model;
using StockManagement.Licensing.Core;
using StockManagement.Licensing.Core.Contracts;

namespace StockManagement.Tests.Licensing;


[TestClass]
public sealed class LicenseCalculatorTests
{
	private static readonly LicensingOptions Options = new() { TrialDays = 10, GraceDays = 3, MonthlyPricePyg = 100_000 };
	private static readonly DateTime TrialStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);


	[TestMethod]
	public void Compute_WithinTrialWindow_ReturnsTrialWithDaysRemaining()
	{
		// Arrange
		var state = new LicenseState { TrialStartedAtUtc = TrialStart };

		// Act
		var snapshot = LicenseCalculator.Compute(state, null, Options, TrialStart.AddDays(3));

		// Assert
		Assert.AreEqual(LicenseStatus.Trial, snapshot.Status);
		Assert.AreEqual(7, snapshot.DaysRemaining);
	}

	[TestMethod]
	public void Compute_TrialJustExpired_EntersGracePeriod()
	{
		// Arrange
		var state = new LicenseState { TrialStartedAtUtc = TrialStart };

		// Act
		var snapshot = LicenseCalculator.Compute(state, null, Options, TrialStart.AddDays(10).AddHours(1));

		// Assert
		Assert.AreEqual(LicenseStatus.GracePeriod, snapshot.Status);
		Assert.AreEqual(3, snapshot.DaysRemaining);
	}

	[TestMethod]
	public void Compute_TrialAndGraceExpired_ReturnsLocked()
	{
		// Arrange
		var state = new LicenseState { TrialStartedAtUtc = TrialStart };

		// Act
		var snapshot = LicenseCalculator.Compute(state, null, Options, TrialStart.AddDays(14));

		// Assert
		Assert.AreEqual(LicenseStatus.Locked, snapshot.Status);
		Assert.AreEqual(0, snapshot.DaysRemaining);
	}

	[TestMethod]
	public void Compute_ActiveToken_ReturnsActiveRegardlessOfTrial()
	{
		// Arrange
		var state = new LicenseState { TrialStartedAtUtc = TrialStart };
		var token = new LicenseToken("Acme", LicensePlan.Monthly, TrialStart, TrialStart.AddDays(30));

		// Act
		var snapshot = LicenseCalculator.Compute(state, token, Options, TrialStart.AddDays(20));

		// Assert
		Assert.AreEqual(LicenseStatus.Active, snapshot.Status);
		Assert.AreEqual(LicensePlan.Monthly, snapshot.Plan);
		Assert.AreEqual(10, snapshot.DaysRemaining);
	}

	[TestMethod]
	public void Compute_TokenExpired_FallsBackToGraceFromItsOwnExpiry()
	{
		// Arrange
		var state = new LicenseState { TrialStartedAtUtc = TrialStart };
		var token = new LicenseToken("Acme", LicensePlan.Yearly, TrialStart, TrialStart.AddDays(30));

		// Act: subscription lapsed 1 day ago, well after the 10-day trial window
		var snapshot = LicenseCalculator.Compute(state, token, Options, TrialStart.AddDays(31));

		// Assert
		Assert.AreEqual(LicenseStatus.GracePeriod, snapshot.Status);
		Assert.AreEqual(2, snapshot.DaysRemaining);
	}

	[TestMethod]
	public void Compute_TokenAndGraceExpired_ReturnsLocked()
	{
		// Arrange
		var state = new LicenseState { TrialStartedAtUtc = TrialStart };
		var token = new LicenseToken("Acme", LicensePlan.Yearly, TrialStart, TrialStart.AddDays(30));

		// Act
		var snapshot = LicenseCalculator.Compute(state, token, Options, TrialStart.AddDays(40));

		// Assert
		Assert.AreEqual(LicenseStatus.Locked, snapshot.Status);
	}
}
