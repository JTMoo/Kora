using StockManagement.Kernel.Model;

namespace StockManagement.Kernel.Database.Interfaces;


/// <summary>
/// Storage for <see cref="DebitNote"/> (#184) - mirrors <see cref="IRemissionNoteServiceProvider"/>'s shape.
/// </summary>
public interface IDebitNoteServiceProvider
{
	public Task<IReadOnlyList<DebitNote>> GetDebitNotesAsync(CancellationToken cancellationToken = default);

	public Task<DebitNote?> GetDebitNoteAsync(string number, CancellationToken cancellationToken = default);

	/// <summary>
	/// Stores the debit note and its outbox row (<see cref="PendingDebitNoteTransmission"/>) in one transaction.
	/// </summary>
	/// <exception cref="Exceptions.DebitNoteNumberAlreadyExistsException">Number already exists; nothing written</exception>
	public Task AddAsync(DebitNote debitNote, CancellationToken cancellationToken = default);
}
