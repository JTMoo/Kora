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


public sealed record UpdateStockItemRequest(string Id, string Code, string Name, string Description = "", string Location = "", int Amount = 0, decimal Price = 0, string Manufacturer = "", decimal Factor = 0, decimal PurchasePrice = 0, decimal PurchaseExchangeRate = 0, decimal AdditionalPurchaseCost = 0, string? SupplierId = null, int MinimumStock = 0, decimal VatRatePercent = 0, string Barcode = "");


public class UpdateStockItemValidator : Validator<UpdateStockItemRequest>
{
	public UpdateStockItemValidator()
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
		this.RuleFor(request => request.VatRatePercent).GreaterThanOrEqualTo(0).WithMessage("vatRateNegative");
	}
}


/// <remarks>Matches the stored item by <c>Id</c> (docs/decisions.md: update by Id, never by business key), so Code can be renamed. When <c>Factor > 0</c>, <c>Price</c> is derived from the purchase fields (ADR-0020) instead of the request's <c>Price</c>.</remarks>
public class UpdateStockItemEndpoint(IStockItemServiceProvider stockItemServiceProvider, ISettingsService settingsService) : Endpoint<UpdateStockItemRequest, Results<Ok<StockItemResponse>, NotFound, Conflict<DuplicateStockItemCodeResponse>, Conflict<DuplicateStockItemBarcodeResponse>>>
{
	private readonly IStockItemServiceProvider _stockItemServiceProvider = stockItemServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;


	public override void Configure()
	{
		this.Put("/stock-items/{Id}");
		this.Permissions(Permission.StockItemsWrite);
	}

	public override async Task<Results<Ok<StockItemResponse>, NotFound, Conflict<DuplicateStockItemCodeResponse>, Conflict<DuplicateStockItemBarcodeResponse>>> ExecuteAsync(UpdateStockItemRequest request, CancellationToken cancellationToken)
	{
		if (await _stockItemServiceProvider.GetStockItemByIdAsync(request.Id, cancellationToken) is not StockItem stockItem) return TypedResults.NotFound();

		stockItem.Code = request.Code;
		stockItem.Name = request.Name;
		stockItem.Description = request.Description;
		stockItem.Location = request.Location;
		stockItem.Amount = request.Amount;
		stockItem.Manufacturer = request.Manufacturer;
		stockItem.Factor = request.Factor;
		stockItem.PurchasePrice = request.PurchasePrice;
		stockItem.PurchaseExchangeRate = request.PurchaseExchangeRate;
		stockItem.AdditionalPurchaseCost = request.AdditionalPurchaseCost;
		stockItem.SupplierId = request.SupplierId;
		stockItem.MinimumStock = request.MinimumStock;
		stockItem.VatRatePercent = request.VatRatePercent;
		stockItem.Barcode = request.Barcode;

		if (request.Factor > 0)
		{
			var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
			stockItem.Price = StockItemExtensions.CalculateSalePrice(request.PurchasePrice, request.PurchaseExchangeRate, request.AdditionalPurchaseCost, request.Factor, companySettings.CurrencyDecimalDigits);
		}
		else
		{
			stockItem.Price = request.Price;
		}

		try
		{
			await _stockItemServiceProvider.UpdateStockItemAsync(stockItem, cancellationToken);
		}
		catch (StockItemCodeAlreadyExistsException)
		{
			return TypedResults.Conflict(new DuplicateStockItemCodeResponse(request.Code));
		}
		catch (StockItemBarcodeAlreadyExistsException)
		{
			return TypedResults.Conflict(new DuplicateStockItemBarcodeResponse(request.Barcode));
		}

		// Reload: stockItem.Supplier may still reference the old row after a SupplierId change
		var updated = await _stockItemServiceProvider.GetStockItemByIdAsync(stockItem.Id, cancellationToken);
		return TypedResults.Ok(StockItemResponse.From(updated));
	}
}
