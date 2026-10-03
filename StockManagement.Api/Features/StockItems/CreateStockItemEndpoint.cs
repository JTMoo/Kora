using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using StockManagement.Auth.Core.Contracts;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Exceptions;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.ExtensionMethods;
using StockManagement.Settings.Core.Contracts;

namespace StockManagement.Api.Features.StockItems;


public sealed record CreateStockItemRequest(string Code, string Name, string Description = "", string Location = "", int Amount = 0, decimal Price = 0, string Manufacturer = "", decimal Factor = 0, decimal PurchasePrice = 0, decimal PurchaseExchangeRate = 0, decimal AdditionalPurchaseCost = 0, string? SupplierId = null, int MinimumStock = 0, decimal? VatRatePercent = null, string Barcode = "");


public class CreateStockItemValidator : Validator<CreateStockItemRequest>
{
	public CreateStockItemValidator()
	{
		this.RuleFor(request => request.Code).NotEmpty().WithMessage("codeRequired");
		this.RuleFor(request => request.Name).NotEmpty().WithMessage("nameRequired");
		this.RuleFor(request => request.Barcode).Matches(@"^(\d{8}|\d{12,13})$").When(request => request.Barcode != "").WithMessage("barcodeInvalid");
		this.RuleFor(request => request.Amount).GreaterThanOrEqualTo(0).WithMessage("amountNegative");
		this.RuleFor(request => request.Price).GreaterThanOrEqualTo(0).WithMessage("priceNegative");
		this.RuleFor(request => request.Factor).GreaterThanOrEqualTo(0).WithMessage("factorNegative");
		this.RuleFor(request => request.PurchasePrice).GreaterThanOrEqualTo(0).WithMessage("purchasePriceNegative");
		this.RuleFor(request => request.PurchaseExchangeRate).GreaterThanOrEqualTo(0).WithMessage("purchaseExchangeRateNegative");
		this.RuleFor(request => request.AdditionalPurchaseCost).GreaterThanOrEqualTo(0).WithMessage("additionalPurchaseCostNegative");
		this.RuleFor(request => request.MinimumStock).GreaterThanOrEqualTo(0).WithMessage("minimumStockNegative");
		this.RuleFor(request => request.VatRatePercent).GreaterThanOrEqualTo(0).When(request => request.VatRatePercent.HasValue).WithMessage("vatRateNegative");
	}
}


public sealed record DuplicateStockItemCodeResponse(string Code);


public sealed record DuplicateStockItemBarcodeResponse(string Barcode);


/// <remarks>When <c>Factor > 0</c>, <c>Price</c> is derived from the purchase fields (ADR-0020) instead of the request's <c>Price</c>.</remarks>
public class CreateStockItemEndpoint(IStockItemServiceProvider stockItemServiceProvider, ISettingsService settingsService) : Endpoint<CreateStockItemRequest, Results<Created<StockItemResponse>, Conflict<DuplicateStockItemCodeResponse>, Conflict<DuplicateStockItemBarcodeResponse>>>
{
	private readonly IStockItemServiceProvider _stockItemServiceProvider = stockItemServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;


	public override void Configure()
	{
		this.Post("/stock-items");
		this.Permissions(Permission.StockItemsWrite);
	}

	public override async Task<Results<Created<StockItemResponse>, Conflict<DuplicateStockItemCodeResponse>, Conflict<DuplicateStockItemBarcodeResponse>>> ExecuteAsync(CreateStockItemRequest request, CancellationToken cancellationToken)
	{
		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);

		var stockItem = new StockItem(request.Name, code: request.Code, description: request.Description, amount: request.Amount, manufacturer: request.Manufacturer)
		{
			Location = request.Location,
			Factor = request.Factor,
			PurchasePrice = request.PurchasePrice,
			PurchaseExchangeRate = request.PurchaseExchangeRate,
			AdditionalPurchaseCost = request.AdditionalPurchaseCost,
			SupplierId = request.SupplierId,
			MinimumStock = request.MinimumStock,
			VatRatePercent = request.VatRatePercent ?? companySettings.VatRatePercent,
			Barcode = request.Barcode
		};

		if (request.Factor > 0)
		{
			stockItem.Price = StockItemExtensions.CalculateSalePrice(request.PurchasePrice, request.PurchaseExchangeRate, request.AdditionalPurchaseCost, request.Factor, companySettings.CurrencyDecimalDigits);
		}
		else
		{
			stockItem.Price = request.Price;
		}

		try
		{
			await _stockItemServiceProvider.AddStockItemAsync(stockItem, cancellationToken);
		}
		catch (StockItemCodeAlreadyExistsException)
		{
			return TypedResults.Conflict(new DuplicateStockItemCodeResponse(request.Code));
		}
		catch (StockItemBarcodeAlreadyExistsException)
		{
			return TypedResults.Conflict(new DuplicateStockItemBarcodeResponse(request.Barcode));
		}

		// Reload to fill the Supplier navigation (AddStockItemAsync only tracked the FK)
		var created = await _stockItemServiceProvider.GetStockItemByIdAsync(stockItem.Id, cancellationToken);
		return TypedResults.Created($"/api/stock-items/{stockItem.Code}", StockItemResponse.From(created));
	}
}
