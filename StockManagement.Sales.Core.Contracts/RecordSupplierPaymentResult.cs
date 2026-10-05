using StockManagement.Kernel.Model;

namespace StockManagement.Sales.Core.Contracts;


public enum RecordSupplierPaymentError
{
	SupplierInvoiceNotFound,

	InvalidAmount,

	ExceedsAmountDue
}


/// <summary>
/// Outcome of <see cref="ISupplierPaymentService.RecordPaymentAsync"/>.
/// </summary>
public sealed record RecordSupplierPaymentResult(bool Succeeded, Payment? Payment = null, RecordSupplierPaymentError? Error = null)
{
	public static RecordSupplierPaymentResult Success(Payment payment)
	{
		return new(true, payment);
	}

	public static RecordSupplierPaymentResult Failure(RecordSupplierPaymentError error)
	{
		return new(false, Error: error);
	}
}
