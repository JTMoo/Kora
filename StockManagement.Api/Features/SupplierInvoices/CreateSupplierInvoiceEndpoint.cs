using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.ExtensionMethods;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Api.Features.SupplierInvoices;


public sealed record CreateSupplierInvoiceRequest(string Number, string SupplierId, DateTime Date, DateTime ExpirationDate, decimal Total, IReadOnlyList<SupplierInvoiceItemRequest>? Items = null);


public sealed record SupplierInvoiceItemRequest(string Code, int Amount, decimal UnitPrice);


public sealed record DuplicateSupplierInvoiceNumberResponse(string Number);


public class CreateSupplierInvoiceValidator : Validator<CreateSupplierInvoiceRequest>
{
	public CreateSupplierInvoiceValidator()
	{
		this.RuleFor(request => request.Number).NotEmpty().WithMessage("nameRequired");
		this.RuleFor(request => request.SupplierId).NotEmpty().WithMessage("nameRequired");
		this.RuleFor(request => request.Total).GreaterThan(0).WithMessage("amountNotPositive").When(request => request.Items is null or []);
		this.RuleForEach(request => request.Items).ChildRules(item =>
		{
			item.RuleFor(line => line.Code).NotEmpty().WithMessage("codeRequired");
			item.RuleFor(line => line.Amount).GreaterThan(0).WithMessage("amountNotPositive");
			item.RuleFor(line => line.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("amountNotPositive");
		});
	}
}


/// <remarks>
/// With <see cref="CreateSupplierInvoiceRequest.Items"/>, checks in every line's stock atomically with the AP
/// record (#246) and recomputes <see cref="SupplierInvoice.Total"/> from the lines, ignoring the request's own
/// <c>Total</c>. No items -> unchanged flat-total behavior (a pure service bill, no stock effect).
/// </remarks>
public class CreateSupplierInvoiceEndpoint(ISupplierServiceProvider supplierServiceProvider, ISupplierInvoiceServiceProvider supplierInvoiceServiceProvider, IStockItemServiceProvider stockItemServiceProvider, ISupplierPaymentService paymentService, ISettingsService settingsService)
	: Endpoint<CreateSupplierInvoiceRequest, Results<Created<SupplierInvoiceResponse>, NotFound, Conflict<DuplicateSupplierInvoiceNumberResponse>>>
{
	public const string StockItemNotFound = "stockItemNotFound";

	private readonly ISupplierServiceProvider _supplierServiceProvider = supplierServiceProvider;
	private readonly ISupplierInvoiceServiceProvider _supplierInvoiceServiceProvider = supplierInvoiceServiceProvider;
	private readonly IStockItemServiceProvider _stockItemServiceProvider = stockItemServiceProvider;
	private readonly ISupplierPaymentService _paymentService = paymentService;
	private readonly ISettingsService _settingsService = settingsService;


	public override void Configure()
	{
		this.Post("/supplier-invoices");
		this.Permissions(Permission.PayablesWrite);
	}

	public override async Task<Results<Created<SupplierInvoiceResponse>, NotFound, Conflict<DuplicateSupplierInvoiceNumberResponse>>> ExecuteAsync(CreateSupplierInvoiceRequest request, CancellationToken cancellationToken)
	{
		if (await _supplierServiceProvider.GetSupplierByIdAsync(request.SupplierId, cancellationToken) is not Supplier supplier) return TypedResults.NotFound();

		List<SupplierInvoiceItem> items = [];
		foreach (var line in request.Items ?? [])
		{
			if (await _stockItemServiceProvider.GetStockItemAsync(line.Code, cancellationToken) is not StockItem stockItem) return TypedResults.NotFound();
			items.Add(new SupplierInvoiceItem(stockItem) { Amount = line.Amount, UnitPrice = line.UnitPrice });
		}

		var total = request.Total;
		if (items.Count > 0)
		{
			var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
			total = StockItemExtensions.CalculatePurchaseTotal(items, companySettings.CurrencyDecimalDigits);
		}

		var invoice = new SupplierInvoice
		{
			Number = request.Number,
			Supplier = supplier,
			Date = request.Date,
			ExpirationDate = request.ExpirationDate,
			Total = total,
			Items = items,
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
