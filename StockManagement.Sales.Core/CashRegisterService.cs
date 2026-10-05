using StockManagement.Kernel.Database;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Sales.Core;


internal class CashRegisterService(ICashRegisterSessionServiceProvider sessionServiceProvider) : ICashRegisterService
{
	private readonly ICashRegisterSessionServiceProvider _sessionServiceProvider = sessionServiceProvider;


	public async Task<OpenCashRegisterSessionResult> OpenSessionAsync(decimal openingFloat, string openedByUserId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (openingFloat < 0) return OpenCashRegisterSessionResult.Failure(OpenCashRegisterSessionError.InvalidOpeningFloat);
		if (await _sessionServiceProvider.GetOpenSessionAsync(cancellationToken) is not null) return OpenCashRegisterSessionResult.Failure(OpenCashRegisterSessionError.AlreadyOpen);

		var session = new CashRegisterSession
		{
			OpenedAt = DateTime.Now,
			OpenedByUserId = openedByUserId,
			OpeningFloat = openingFloat,
			Status = CashRegisterSessionStatus.Open
		};
		await _sessionServiceProvider.AddSessionAsync(session, cancellationToken);

		return OpenCashRegisterSessionResult.Success(session);
	}

	public Task<CashRegisterSession?> GetOpenSessionAsync(CancellationToken cancellationToken = default)
	{
		return _sessionServiceProvider.GetOpenSessionAsync(cancellationToken);
	}

	public Task<CashRegisterSession?> GetSessionAsync(string id, CancellationToken cancellationToken = default)
	{
		return _sessionServiceProvider.GetByIdAsync(id, cancellationToken);
	}

	public Task<CursorPage<CashRegisterSession>> GetSessionsAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default)
	{
		return _sessionServiceProvider.GetSessionsAsync(cursor, pageSize, cancellationToken);
	}

	public async Task<AddCashMovementResult> AddMovementAsync(CashMovementType type, decimal amount, string reason, string createdByUserId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (amount <= 0) return AddCashMovementResult.Failure(AddCashMovementError.InvalidAmount);
		if (await _sessionServiceProvider.GetOpenSessionAsync(cancellationToken) is not CashRegisterSession session) return AddCashMovementResult.Failure(AddCashMovementError.NoOpenSession);

		var movement = new CashMovement { Type = type, Amount = amount, Reason = reason, Date = DateTime.Now, CreatedByUserId = createdByUserId };
		session.Movements.Add(movement);
		await _sessionServiceProvider.UpdateSessionAsync(session, cancellationToken);

		return AddCashMovementResult.Success(movement);
	}

	public async Task<CloseCashRegisterSessionResult> CloseSessionAsync(decimal countedAmount, string note, string closedByUserId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (countedAmount < 0) return CloseCashRegisterSessionResult.Failure(CloseCashRegisterSessionError.NegativeCountedAmount);
		if (await _sessionServiceProvider.GetOpenSessionAsync(cancellationToken) is not CashRegisterSession session) return CloseCashRegisterSessionResult.Failure(CloseCashRegisterSessionError.NoOpenSession);

		var expected = await this.GetExpectedAmountAsync(session, cancellationToken);

		session.ClosedAt = DateTime.Now;
		session.ClosedByUserId = closedByUserId;
		session.CountedAmount = countedAmount;
		session.Note = note;
		session.Status = CashRegisterSessionStatus.Closed;
		await _sessionServiceProvider.UpdateSessionAsync(session, cancellationToken);

		return CloseCashRegisterSessionResult.Success(new CashRegisterCloseReport(session, expected, countedAmount, countedAmount - expected));
	}

	public async Task<decimal> GetExpectedAmountAsync(CashRegisterSession session, CancellationToken cancellationToken = default)
	{
		var cashPayments = await _sessionServiceProvider.GetCashPaymentsTotalAsync(session.Id, cancellationToken);
		return CashRegisterCalculator.ExpectedAmount(session, cashPayments);
	}
}
