using StockManagement.Kernel.Database;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Sales.Core.Contracts;


public interface ISupplierPaymentService
{
	/// <summary>
	/// Sum of every payment recorded against the supplier invoice
	/// </summary>
	public decimal GetAmountPaid(SupplierInvoice invoice);

	/// <summary>
	/// <see cref="SupplierInvoice.Total"/> minus <see cref="GetAmountPaid"/>
	/// </summary>
	public decimal GetAmountDue(SupplierInvoice invoice);

	/// <summary>
	/// Open / partly paid / paid / overdue, derived from payments and <paramref name="asOf"/> vs. <see cref="SupplierInvoice.ExpirationDate"/>
	/// </summary>
	public SupplierInvoiceStatus GetStatus(SupplierInvoice invoice, DateTime asOf);

	/// <summary>
	/// Records a payment against the supplier invoice; rounds <paramref name="amount"/> to the company's configured currency digits first
	/// </summary>
	/// <remarks>Rejected when the invoice does not exist, the amount is not positive, or it exceeds the invoice's amount due.</remarks>
	public Task<RecordSupplierPaymentResult> RecordPaymentAsync(string invoiceNumber, decimal amount, PaymentMethod method, DateTime date, CancellationToken cancellationToken = default);

	/// <summary>
	/// Supplier invoices with an amount due greater than zero (open, partly paid or overdue), soonest due date then <see cref="Database.BaseDocument.Id"/> first
	/// </summary>
	public Task<CursorPage<SupplierInvoice>> GetOpenSupplierInvoicesAsync(string? supplierId, string? cursor, int pageSize, CancellationToken cancellationToken = default);
}
