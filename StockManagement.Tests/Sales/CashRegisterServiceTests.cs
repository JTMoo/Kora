using Moq;
using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Tests.Sales;


[TestClass]
public sealed class CashRegisterServiceTests
{
	private readonly Mock<ICashRegisterSessionServiceProvider> _sessions = new();


	[TestMethod]
	public async Task OpenSessionAsync_NoneOpen_CreatesOpenSession()
	{
		// Act
		var result = await this.CreateService().OpenSessionAsync(500, "user-1");

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual(500, result.Session!.OpeningFloat);
		Assert.AreEqual("user-1", result.Session.OpenedByUserId);
		Assert.AreEqual(CashRegisterSessionStatus.Open, result.Session.Status);
		_sessions.Verify(provider => provider.AddSessionAsync(result.Session, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task OpenSessionAsync_NegativeFloat_ReturnsFailure()
	{
		// Act
		var result = await this.CreateService().OpenSessionAsync(-1, "user-1");

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(OpenCashRegisterSessionError.InvalidOpeningFloat, result.Error);
		_sessions.Verify(provider => provider.AddSessionAsync(It.IsAny<CashRegisterSession>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task OpenSessionAsync_AlreadyOpen_ReturnsFailure()
	{
		// Arrange
		_sessions.Setup(provider => provider.GetOpenSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CashRegisterSession());

		// Act
		var result = await this.CreateService().OpenSessionAsync(100, "user-1");

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(OpenCashRegisterSessionError.AlreadyOpen, result.Error);
	}

	[TestMethod]
	public async Task AddMovementAsync_NoOpenSession_ReturnsFailure()
	{
		// Act
		var result = await this.CreateService().AddMovementAsync(CashMovementType.In, 50, "float top-up", "user-1");

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(AddCashMovementError.NoOpenSession, result.Error);
	}

	[TestMethod]
	public async Task AddMovementAsync_NonPositiveAmount_ReturnsFailure()
	{
		// Arrange
		_sessions.Setup(provider => provider.GetOpenSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CashRegisterSession());

		// Act
		var result = await this.CreateService().AddMovementAsync(CashMovementType.Out, 0, "reason", "user-1");

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(AddCashMovementError.InvalidAmount, result.Error);
	}

	[TestMethod]
	public async Task AddMovementAsync_ValidAmount_AddsMovementToOpenSession()
	{
		// Arrange
		var session = new CashRegisterSession();
		_sessions.Setup(provider => provider.GetOpenSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(session);

		// Act
		var result = await this.CreateService().AddMovementAsync(CashMovementType.Out, 30, "bank deposit", "user-1");

		// Assert
		Assert.IsTrue(result.Succeeded);
		CollectionAssert.Contains(session.Movements, result.Movement);
		Assert.AreEqual("user-1", result.Movement!.CreatedByUserId);
		_sessions.Verify(provider => provider.UpdateSessionAsync(session, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task CloseSessionAsync_NoOpenSession_ReturnsFailure()
	{
		// Act
		var result = await this.CreateService().CloseSessionAsync(100, "", "user-1");

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(CloseCashRegisterSessionError.NoOpenSession, result.Error);
	}

	[TestMethod]
	public async Task CloseSessionAsync_NegativeCountedAmount_ReturnsFailure()
	{
		// Act
		var result = await this.CreateService().CloseSessionAsync(-1, "", "user-1");

		// Assert
		Assert.IsFalse(result.Succeeded);
		Assert.AreEqual(CloseCashRegisterSessionError.NegativeCountedAmount, result.Error);
	}

	[TestMethod]
	public async Task CloseSessionAsync_ComputesExpectedAndDifference()
	{
		// Arrange: 100 opening + 50 cash in - 20 cash out + 200 cash sales = 330 expected; counted 325 -> -5 difference
		var session = new CashRegisterSession
		{
			Id = "s1",
			OpeningFloat = 100,
			Movements = [new CashMovement { Type = CashMovementType.In, Amount = 50 }, new CashMovement { Type = CashMovementType.Out, Amount = 20 }]
		};
		_sessions.Setup(provider => provider.GetOpenSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(session);
		_sessions.Setup(provider => provider.GetCashPaymentsTotalAsync("s1", It.IsAny<CancellationToken>())).ReturnsAsync(200);

		// Act
		var result = await this.CreateService().CloseSessionAsync(325, "short by 5", "user-2");

		// Assert
		Assert.IsTrue(result.Succeeded);
		Assert.AreEqual(330, result.Report!.ExpectedAmount);
		Assert.AreEqual(325, result.Report.CountedAmount);
		Assert.AreEqual(-5, result.Report.Difference);
		Assert.AreEqual(CashRegisterSessionStatus.Closed, session.Status);
		Assert.AreEqual("user-2", session.ClosedByUserId);
		Assert.AreEqual("short by 5", session.Note);
		_sessions.Verify(provider => provider.UpdateSessionAsync(session, It.IsAny<CancellationToken>()), Times.Once);
	}

	private CashRegisterService CreateService()
	{
		return new CashRegisterService(_sessions.Object);
	}
}
