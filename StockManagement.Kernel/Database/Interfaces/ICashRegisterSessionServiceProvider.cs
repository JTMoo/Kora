using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Kernel.Database.Interfaces;


public interface ICashRegisterSessionServiceProvider
{
	public Task<CashRegisterSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

	/// <returns>The session with <see cref="CashRegisterSessionStatus.Open"/>, if any; a single till has at most one</returns>
	public Task<CashRegisterSession?> GetOpenSessionAsync(CancellationToken cancellationToken = default);

	/// <summary>Sessions newest-opened first, one page at a time</summary>
	public Task<CursorPage<CashRegisterSession>> GetSessionsAsync(string? cursor, int pageSize, CancellationToken cancellationToken = default);

	public Task AddSessionAsync(CashRegisterSession session, CancellationToken cancellationToken = default);

	/// <returns>Rows affected; 1 on success</returns>
	public Task<int> UpdateSessionAsync(CashRegisterSession session, CancellationToken cancellationToken = default);

	/// <summary>Sum of sale <see cref="Payment.Amount"/> tagged with <paramref name="sessionId"/> (cash received at the till)</summary>
	public Task<decimal> GetCashPaymentsTotalAsync(string sessionId, CancellationToken cancellationToken = default);
}
