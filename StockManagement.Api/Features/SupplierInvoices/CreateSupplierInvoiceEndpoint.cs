using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.SupplierInvoices;


public sealed record CreateSupplierInvoiceRequest(string Number, string SupplierId, DateTime Date, DateTime ExpirationDate, decimal Total);


public sealed record DuplicateSupplierInvoiceNumberResponse(string Number);


public class CreateSupplierInvoiceValidator : Validator<CreateSupplierInvoiceRequest>
{
	public CreateSupplierInvoiceValidator()
	{
		this.RuleFor(request => request.Number).NotEmpty().WithMessage("nameRequired");
		this.RuleFor(request => request.SupplierId).NotEmpty().WithMessage("nameRequired");
		this.RuleFor(request => request.Total).GreaterThan(0).WithMessage("amountNotPositive");
	}
}


public class CreateSupplierInvoiceEndpoint(ISupplierServiceProvider supplierServiceProvider, ISupplierInvoiceServiceProvider supplierInvoiceServiceProvider, ISupplierPaymentService paymentService)
	: Endpoint<CreateSupplierInvoiceRequest, Results<Created<SupplierInvoiceResponse>, NotFound, Conflict<DuplicateSupplierInvoiceNumberResponse>>>
{
	private readonly ISupplierServiceProvider _supplierServiceProvider = supplierServiceProvider;
	private readonly ISupplierInvoiceServiceProvider _supplierInvoiceServiceProvider = supplierInvoiceServiceProvider;
	private readonly ISupplierPaymentService _paymentService = paymentService;


	public override void Configure()
	{
		this.Post("/supplier-invoices");
		this.Permissions(Permission.PayablesWrite);
	}

	public override async Task<Results<Created<SupplierInvoiceResponse>, NotFound, Conflict<DuplicateSupplierInvoiceNumberResponse>>> ExecuteAsync(CreateSupplierInvoiceRequest request, CancellationToken cancellationToken)
	{
		if (await _supplierServiceProvider.GetSupplierByIdAsync(request.SupplierId, cancellationToken) is not Supplier supplier) return TypedResults.NotFound();

		var invoice = new SupplierInvoice
		{
			Number = request.Number,
			Supplier = supplier,
			Date = request.Date,
			ExpirationDate = request.ExpirationDate,
			Total = request.Total
		};

		try
		{
			await _supplierInvoiceServiceProvider.AddSupplierInvoiceAsync(invoice, cancellationToken);
		}
		catch (SupplierInvoiceNumberAlreadyExistsException)
		{
			return TypedResults.Conflict(new DuplicateSupplierInvoiceNumberResponse(request.Number));
		}

		return TypedResults.Created($"/api/supplier-invoices/{invoice.Number}", SupplierInvoiceResponse.From(invoice, _paymentService));
	}
}
