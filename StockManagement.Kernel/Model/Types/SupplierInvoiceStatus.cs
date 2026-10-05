namespace StockManagement.Kernel.Model.Types;


/// <summary>
/// Payment status of a <see cref="SupplierInvoice"/>, mirrors <see cref="InvoiceStatus"/> (no cancellation: suppliers bills aren't voided through Kora)
/// </summary>
public enum SupplierInvoiceStatus
{
	Open,

	PartiallyPaid,

	Paid,

	Overdue
}
