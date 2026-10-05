using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Sales.Core.Contracts;


public interface ICashRegisterService
{
	/// <remarks>Rejected when a session is already open, or when <paramref name="openingFloat"/> is negative</remarks>
	public Task<OpenCashRegisterSessionResult> OpenSessionAsync(decimal openingFloat, string openedByUserId, CancellationToken cancellationToken = default);

	/// <summary>The currently open session, if any; a single till has at most one</summary>
	public Task<CashRegisterSession?> GetOpenSessionAsync(CancellationToken cancellationToken = default);

	public Task<CashRegisterSession?> GetSessionAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>Sessions newest-opened first, one page at a time</summary>
	public Task<CursorPage<CashRegisterSession>> GetSessionsAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default);

	/// <remarks>Rejected when no session is open, or when <paramref name="amount"/> is not positive</remarks>
	public Task<AddCashMovementResult> AddMovementAsync(CashMovementType type, decimal amount, string reason, string createdByUserId, CancellationToken cancellationToken = default);

	/// <summary>Closes the open session, recording the physical count and computing the arqueo (expected vs. counted)</summary>
	/// <remarks>Rejected when no session is open, or when <paramref name="countedAmount"/> is negative</remarks>
	public Task<CloseCashRegisterSessionResult> CloseSessionAsync(decimal countedAmount, string note, string closedByUserId, CancellationToken cancellationToken = default);

	/// <summary>Expected cash for an already-closed session, recomputed from its recorded movements and payments</summary>
	public Task<decimal> GetExpectedAmountAsync(CashRegisterSession session, CancellationToken cancellationToken = default);

	/// <summary>Expected cash per session, batched in one query instead of one per session</summary>
	public Task<Dictionary<string, decimal>> GetExpectedAmountsAsync(IReadOnlyCollection<CashRegisterSession> sessions, CancellationToken cancellationToken = default);
}
