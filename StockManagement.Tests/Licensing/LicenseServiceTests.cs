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
	private readonly FixedTimeProvider _time = new(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));


	[TestMethod]
	public async Task GetStatusAsync_NothingStored_StartsTrialAndPersists()
	{
		// Arrange
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((LicenseState?)null);

		// Act
		var snapshot = await this.CreateService().GetStatusAsync();

		// Assert
		Assert.AreEqual(LicenseStatus.Trial, snapshot.Status);
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
	public async Task TryActivateAsync_InvalidKey_LeavesStateUnchanged()
	{
		// Arrange
		_provider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new LicenseState { TrialStartedAtUtc = _time.GetUtcNow().UtcDateTime });
		_tokenVerifier.Setup(verifier => verifier.TryVerify("bad-key", out It.Ref<LicenseToken?>.IsAny)).Returns(false);

		// Act
		var (success, status) = await this.CreateService().TryActivateAsync("bad-key");

		// Assert
		Assert.IsFalse(success);
		Assert.AreEqual(LicenseStatus.Trial, status.Status);
		_provider.Verify(provider => provider.UpdateAsync(It.IsAny<LicenseState>(), It.IsAny<CancellationToken>()), Times.Never);
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
		_provider.Verify(provider => provider.UpdateAsync(It.Is<LicenseState>(s => s.ActivatedLicenseKey == "good-key"), It.IsAny<CancellationToken>()), Times.Once);
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
		_provider.Verify(provider => provider.UpdateAsync(It.IsAny<LicenseState>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	private LicenseService CreateService()
	{
		var options = Options.Create(new LicensingOptions { TrialDays = 10, GraceDays = 3, MonthlyPricePyg = 100_000 });
		return new LicenseService(_provider.Object, _tokenVerifier.Object, options, _time);
	}


	private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
	{
		private readonly DateTimeOffset _utcNow = utcNow;

		public override DateTimeOffset GetUtcNow() => _utcNow;
	}
}
