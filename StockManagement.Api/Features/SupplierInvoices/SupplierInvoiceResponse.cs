using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.SupplierInvoices;


public sealed record SupplierInvoiceResponse(string Number, DateTime Date, DateTime ExpirationDate, decimal Total, string SupplierId, string SupplierName, decimal AmountPaid, decimal AmountDue, SupplierInvoiceStatus Status)
{
	public static SupplierInvoiceResponse From(SupplierInvoice invoice, ISupplierPaymentService paymentService)
	{
		return new(
			invoice.Number,
			invoice.Date,
			invoice.ExpirationDate,
			invoice.Total,
			invoice.Supplier.Id,
			invoice.Supplier.Name,
			paymentService.GetAmountPaid(invoice),
			paymentService.GetAmountDue(invoice),
			paymentService.GetStatus(invoice, DateTime.Now));
	}
}
