using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Api.Features.Payments;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.SupplierPayments;


public sealed record ListSupplierPaymentsRequest(string Number);


public sealed record SupplierInvoicePaymentsResponse(IReadOnlyList<PaymentResponse> Items, decimal AmountPaid, decimal AmountDue, SupplierInvoiceStatus Status);


public class ListSupplierPaymentsEndpoint(ISupplierInvoiceServiceProvider supplierInvoiceServiceProvider, ISupplierPaymentService paymentService) : Endpoint<ListSupplierPaymentsRequest, Results<Ok<SupplierInvoicePaymentsResponse>, NotFound>>
{
	private readonly ISupplierInvoiceServiceProvider _supplierInvoiceServiceProvider = supplierInvoiceServiceProvider;
	private readonly ISupplierPaymentService _paymentService = paymentService;


	public override void Configure()
	{
		this.Get("/supplier-invoices/{Number}/payments");
		this.Permissions(Permission.PayablesRead);
	}

	public override async Task<Results<Ok<SupplierInvoicePaymentsResponse>, NotFound>> ExecuteAsync(ListSupplierPaymentsRequest request, CancellationToken cancellationToken)
	{
		if (await _supplierInvoiceServiceProvider.GetSupplierInvoiceAsync(request.Number, cancellationToken) is not SupplierInvoice invoice) return TypedResults.NotFound();

		var items = (invoice.Payments ?? []).OrderBy(payment => payment.Date).Select(PaymentResponse.From).ToList();
		return TypedResults.Ok(new SupplierInvoicePaymentsResponse(items, _paymentService.GetAmountPaid(invoice), _paymentService.GetAmountDue(invoice), _paymentService.GetStatus(invoice, DateTime.Now)));
	}
}
