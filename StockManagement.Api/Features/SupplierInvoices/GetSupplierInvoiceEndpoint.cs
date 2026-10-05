using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.SupplierInvoices;


public sealed record GetSupplierInvoiceRequest(string Number);


public class GetSupplierInvoiceEndpoint(ISupplierInvoiceServiceProvider supplierInvoiceServiceProvider, ISupplierPaymentService paymentService) : Endpoint<GetSupplierInvoiceRequest, Results<Ok<SupplierInvoiceResponse>, NotFound>>
{
	private readonly ISupplierInvoiceServiceProvider _supplierInvoiceServiceProvider = supplierInvoiceServiceProvider;
	private readonly ISupplierPaymentService _paymentService = paymentService;


	public override void Configure()
	{
		this.Get("/supplier-invoices/{Number}");
		this.Permissions(Permission.PayablesRead);
	}

	public override async Task<Results<Ok<SupplierInvoiceResponse>, NotFound>> ExecuteAsync(GetSupplierInvoiceRequest request, CancellationToken cancellationToken)
	{
		if (await _supplierInvoiceServiceProvider.GetSupplierInvoiceAsync(request.Number, cancellationToken) is not { } invoice) return TypedResults.NotFound();

		return TypedResults.Ok(SupplierInvoiceResponse.From(invoice, _paymentService));
	}
}
