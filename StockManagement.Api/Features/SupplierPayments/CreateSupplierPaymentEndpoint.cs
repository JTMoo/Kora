using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Api.Features.Payments;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Api.Features.SupplierPayments;


public sealed record CreateSupplierPaymentRequest(string Number, decimal Amount, PaymentMethod Method, DateTime? Date);


public class CreateSupplierPaymentValidator : Validator<CreateSupplierPaymentRequest>
{
	public CreateSupplierPaymentValidator()
	{
		this.RuleFor(request => request.Amount).GreaterThan(0).WithMessage("amountNotPositive");
		this.RuleFor(request => request.Method).IsInEnum().WithMessage("paymentMethodInvalid").NotEqual(PaymentMethod.None).WithMessage("paymentMethodInvalid");
	}
}


/// <remarks>Rejects with 409 when the amount is not positive or exceeds the supplier invoice's amount due.</remarks>
public class CreateSupplierPaymentEndpoint(ISupplierPaymentService paymentService) : Endpoint<CreateSupplierPaymentRequest, Results<Created<PaymentResponse>, NotFound, Conflict<PaymentRejectedResponse>>>
{
	private readonly ISupplierPaymentService _paymentService = paymentService;


	public override void Configure()
	{
		this.Post("/supplier-invoices/{Number}/payments");
		this.Permissions(Permission.PayablesWrite);
	}

	public override async Task<Results<Created<PaymentResponse>, NotFound, Conflict<PaymentRejectedResponse>>> ExecuteAsync(CreateSupplierPaymentRequest request, CancellationToken cancellationToken)
	{
		var result = await _paymentService.RecordPaymentAsync(request.Number, request.Amount, request.Method, request.Date ?? DateTime.Now, cancellationToken);
		if (!result.Succeeded)
		{
			return result.Error switch
			{
				RecordSupplierPaymentError.SupplierInvoiceNotFound => TypedResults.NotFound(),
				RecordSupplierPaymentError.InvalidAmount => TypedResults.Conflict(new PaymentRejectedResponse("invalidPaymentAmount")),
				_ => TypedResults.Conflict(new PaymentRejectedResponse("paymentExceedsAmountDue"))
			};
		}

		return TypedResults.Created($"/api/supplier-invoices/{request.Number}/payments", PaymentResponse.From(result.Payment!));
	}
}
