using StockManagement.Kernel.Model;

namespace StockManagement.Sales.Core.Contracts;


/// <summary>
/// Creates SIFEN Nota de Débito Electrónica documents (#184) - increases a previously issued invoice (interest,
/// surcharges, price corrections upward), the counterpart to <see cref="ICreditNoteService"/>.
/// </summary>
public interface IDebitNoteService
{
	/// <summary>
	/// DNIT composite number for the next new debit note, scoped to the configured establishment/point of sale -
	/// own sequence, independent of <see cref="ISaleService.GetNextInvoiceNumberAsync"/>.
	/// </summary>
	public Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Builds and stores a numbered debit note against <paramref name="invoice"/>, queuing it for SIFEN transmission.
	/// </summary>
	/// <exception cref="ArgumentException"><paramref name="items"/> is empty</exception>
	/// <exception cref="ArgumentOutOfRangeException">An item amount is not greater than 0</exception>
	/// <exception cref="InvalidOperationException"><paramref name="invoice"/> has no Cdc yet (not transmitted to SIFEN)</exception>
	public Task<DebitNote> CreateAsync(Invoice invoice, string reason, IReadOnlyList<(string Description, decimal Amount, int VatRatePercent)> items, DateTime date, CancellationToken cancellationToken = default);
}
