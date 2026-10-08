using Moq;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Licensing.Core;
using StockManagement.Licensing.Core.Contracts;
using Microsoft.Extensions.Options;

namespace StockManagement.Tests.Licensing;


[TestClass]
public sealed class LicenseServiceTests
{
	private readonly Mock<ILicenseServiceProvider> _provider = new();
	private readonly Mock<ILicenseTokenVerifier> _tokenVerifier = new();
	private readonly MutableTimeProvider _time = new(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));


	[TestMethod]
	public async Task GetStatusAsync_NothingStored_StartsTrialAndPersists()
	{
		// Arrange
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((LicenseState?)null);

		// Act
		var snapshot = await this.CreateService().GetStatusAsync();

		// Assert
		Assert.AreEqual(LicenseStatus.Trial, snapshot.Status);
		Assert.IsFalse(string.IsNullOrEmpty(snapshot.MachineId));
		_provider.Verify(provider => provider.AddAsync(It.Is<LicenseState>(state => state.TrialStartedAtUtc == _time.GetUtcNow().UtcDateTime), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task GetStatusAsync_TrialAlreadyStored_DoesNotRestartIt()
	{
		// Arrange
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new LicenseState { TrialStartedAtUtc = _time.GetUtcNow().UtcDateTime.AddDays(-5) });

		// Act
		var snapshot = await this.CreateService().GetStatusAsync();

		// Assert
		Assert.AreEqual(LicenseStatus.Trial, snapshot.Status);
		Assert.AreEqual(5, snapshot.DaysRemaining);
		_provider.Verify(provider => provider.AddAsync(It.IsAny<LicenseState>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task GetStatusAsync_ClockRolledBack_StatusUnaffected()
	{
		// Arrange: trial started 9 days ago, 1 day left - then the system clock gets rolled back a year
		var state = new LicenseState { TrialStartedAtUtc = _time.GetUtcNow().UtcDateTime.AddDays(-9) };
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);
		var service = this.CreateService();
		await service.GetStatusAsync();
		_time.UtcNow = _time.UtcNow.AddYears(-1);

		// Act
		var snapshot = await service.GetStatusAsync();

		// Assert: clamped to the high-water mark, so the trial still reads as 1 day left, not reset
		Assert.AreEqual(LicenseStatus.Trial, snapshot.Status);
		Assert.AreEqual(1, snapshot.DaysRemaining);
	}

	[TestMethod]
	public async Task TryActivateAsync_InvalidKey_LeavesActivationUnchanged()
	{
		// Arrange
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new LicenseState { TrialStartedAtUtc = _time.GetUtcNow().UtcDateTime });
		_tokenVerifier.Setup(verifier => verifier.TryVerify("bad-key", out It.Ref<LicenseToken?>.IsAny)).Returns(false);

		// Act
		var (success, status) = await this.CreateService().TryActivateAsync("bad-key");

		// Assert
		Assert.IsFalse(success);
		Assert.AreEqual(LicenseStatus.Trial, status.Status);
		_provider.Verify(provider => provider.UpdateAsync(It.Is<LicenseState>(s => s.ActivatedLicenseKey != ""), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task TryActivateAsync_ValidKey_PersistsAndReturnsActive()
	{
		// Arrange
		var state = new LicenseState { TrialStartedAtUtc = _time.GetUtcNow().UtcDateTime };
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);
		var token = new LicenseToken("Acme", LicensePlan.Yearly, _time.GetUtcNow().UtcDateTime, _time.GetUtcNow().UtcDateTime.AddYears(1));
		_tokenVerifier.Setup(verifier => verifier.TryVerify("good-key", out token)).Returns(true);

		// Act
		var (success, status) = await this.CreateService().TryActivateAsync("good-key");

		// Assert
		Assert.IsTrue(success);
		Assert.AreEqual(LicenseStatus.Active, status.Status);
		Assert.AreEqual(LicensePlan.Yearly, status.Plan);
		_provider.Verify(provider => provider.UpdateAsync(It.Is<LicenseState>(s => s.ActivatedLicenseKey == "good-key"), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
	}

	[TestMethod]
	public async Task TryActivateAsync_AlreadyExpiredKey_Rejected()
	{
		// Arrange
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new LicenseState { TrialStartedAtUtc = _time.GetUtcNow().UtcDateTime });
		var expiredToken = new LicenseToken("Acme", LicensePlan.Monthly, _time.GetUtcNow().UtcDateTime.AddDays(-60), _time.GetUtcNow().UtcDateTime.AddDays(-1));
		_tokenVerifier.Setup(verifier => verifier.TryVerify("expired-key", out expiredToken)).Returns(true);

		// Act
		var (success, _) = await this.CreateService().TryActivateAsync("expired-key");

		// Assert
		Assert.IsFalse(success);
		_provider.Verify(provider => provider.UpdateAsync(It.Is<LicenseState>(s => s.ActivatedLicenseKey != ""), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task TryActivateAsync_KeyBoundToAnotherMachine_Rejected()
	{
		// Arrange
		var state = new LicenseState { TrialStartedAtUtc = _time.GetUtcNow().UtcDateTime, MachineId = "this-machine" };
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);
		var token = new LicenseToken("Acme", LicensePlan.Monthly, _time.GetUtcNow().UtcDateTime, _time.GetUtcNow().UtcDateTime.AddDays(30), MachineId: "other-machine");
		_tokenVerifier.Setup(verifier => verifier.TryVerify("stolen-key", out token)).Returns(true);

		// Act
		var (success, status) = await this.CreateService().TryActivateAsync("stolen-key");

		// Assert
		Assert.IsFalse(success);
		Assert.AreEqual(LicenseStatus.Trial, status.Status);
		_provider.Verify(provider => provider.UpdateAsync(It.Is<LicenseState>(s => s.ActivatedLicenseKey != ""), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task GetStatusAsync_ActivatedKeyNowBoundToAnotherMachine_TreatedAsUnlicensed()
	{
		// Arrange: a valid, unexpired key that was activated on a different machine than this one
		var state = new LicenseState { TrialStartedAtUtc = _time.GetUtcNow().UtcDateTime.AddDays(-20), ActivatedLicenseKey = "copied-key", MachineId = "this-machine" };
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);
		var token = new LicenseToken("Acme", LicensePlan.Monthly, _time.GetUtcNow().UtcDateTime, _time.GetUtcNow().UtcDateTime.AddDays(30), MachineId: "other-machine");
		_tokenVerifier.Setup(verifier => verifier.TryVerify("copied-key", out token)).Returns(true);

		// Act
		var snapshot = await this.CreateService().GetStatusAsync();

		// Assert: trial+grace already lapsed and the token doesn't count, so it reads as Locked
		Assert.AreEqual(LicenseStatus.Locked, snapshot.Status);
	}

	private LicenseService CreateService()
	{
		var options = Options.Create(new LicensingOptions { TrialDays = 10, GraceDays = 3, MonthlyPricePyg = 100_000 });
		return new LicenseService(_provider.Object, _tokenVerifier.Object, options, _time);
	}


	private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
	{
		public DateTime UtcNow { get; set; } = utcNow;

		public override DateTimeOffset GetUtcNow() => this.UtcNow;
	}
}
