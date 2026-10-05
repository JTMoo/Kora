using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;

namespace StockManagement.Sales.Core;


/// <summary>
/// Payment status of a supplier invoice, derived from its <see cref="SupplierInvoice.Payments"/>; never stored.
/// </summary>
internal static class SupplierInvoiceStatusCalculator
{
	public static decimal AmountPaid(SupplierInvoice invoice)
	{
		return (invoice.Payments ?? []).Sum(payment => payment.Amount);
	}

	public static decimal AmountDue(SupplierInvoice invoice)
	{
		return invoice.Total - AmountPaid(invoice);
	}

	/// <remarks><see cref="SupplierInvoiceStatus.Overdue"/> takes priority over <see cref="SupplierInvoiceStatus.PartiallyPaid"/>.</remarks>
	public static SupplierInvoiceStatus GetStatus(SupplierInvoice invoice, DateTime asOf)
	{
		if (AmountDue(invoice) <= 0) return SupplierInvoiceStatus.Paid;
		if (asOf > invoice.ExpirationDate) return SupplierInvoiceStatus.Overdue;

		return AmountPaid(invoice) > 0 ? SupplierInvoiceStatus.PartiallyPaid : SupplierInvoiceStatus.Open;
	}
}
