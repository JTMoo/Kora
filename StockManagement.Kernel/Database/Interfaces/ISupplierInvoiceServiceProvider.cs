using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model;

namespace StockManagement.Kernel.Database.Interfaces;


public interface ISupplierInvoiceServiceProvider
{
	public Task<SupplierInvoice> GetSupplierInvoiceAsync(string number, CancellationToken cancellationToken = default);

	public Task<IEnumerable<SupplierInvoice>> GetSupplierInvoicesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Supplier invoices matching every given filter, newest first then <see cref="BaseDocument.Id"/>, one page at a time
	/// </summary>
	public Task<CursorPage<SupplierInvoice>> GetSupplierInvoicesAsync(string? supplierId, string? cursor, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>
	/// Adds the invoice and checks in every <see cref="SupplierInvoiceItem.Amount"/> as stock, atomically
	/// </summary>
	/// <exception cref="Exceptions.SupplierInvoiceNumberAlreadyExistsException">Number already in use; nothing written</exception>
	public Task AddSupplierInvoiceAsync(SupplierInvoice invoice, CancellationToken cancellationToken = default);

	/// <returns>Rows affected; 1 on success</returns>
	public Task<int> UpdateSupplierInvoiceAsync(SupplierInvoice invoice, CancellationToken cancellationToken = default);
}
