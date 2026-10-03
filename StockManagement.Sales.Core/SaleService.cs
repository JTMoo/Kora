using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Kernel.Util;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sales.Core;


internal class SaleService(IStockItemServiceProvider stockItemServiceProvider, IInvoiceServiceProvider invoiceServiceProvider, ISettingsService settingsService, IContingencyCdcIssuer contingencyCdcIssuer) : ISaleService
{
	private readonly IStockItemServiceProvider _stockItemServiceProvider = stockItemServiceProvider;
	private readonly IInvoiceServiceProvider _invoiceServiceProvider = invoiceServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;
	private readonly IContingencyCdcIssuer _contingencyCdcIssuer = contingencyCdcIssuer;


	public decimal CalculateTotal(IEnumerable<ShoppingCartItem> items, int currencyDecimalDigits)
	{
		return InvoiceCalculator.CalculateTotal((items ?? []).Select(ToSaleLine), currencyDecimalDigits);
	}

	public async Task<Invoice> CreateInvoiceAsync(Customer customer, IEnumerable<ShoppingCartItem> items, DateTime date, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		List<ShoppingCartItem> cartItems = [.. items ?? []];
		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
		var lines = cartItems.Select(ToSaleLine).ToList();
		var total = InvoiceCalculator.CalculateTotal(lines, companySettings.CurrencyDecimalDigits);

		return new Invoice()
		{
			Customer = customer,
			Date = date,
			ExpirationDate = InvoiceCalculator.CalculateExpirationDate(date, companySettings.PaymentTermInDays),
			Items = cartItems,
			Total = total,
			Tax = InvoiceCalculator.CalculateTax(lines, companySettings.CurrencyDecimalDigits),
			Cdc = ""
		};
	}

	/// <remarks>"Highest + 1" scoped to invoices already carrying the configured establishment/point-of-sale prefix; a counter document with $inc is the planned replacement.</remarks>
	public async Task<string> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var invoices = await _invoiceServiceProvider.GetInvoicesAsync(cancellationToken) ?? [];
		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
		var sequencesInUse = invoices
			.Select(invoice => InvoiceNumber.TryParseSequence(invoice.Number, companySettings.EstablishmentCode, companySettings.PointOfSaleCode))
			.Where(sequence => sequence.HasValue)
			.Select(sequence => sequence!.Value);
		var next = SequenceNumber.Next(sequencesInUse, companySettings.FirstInvoiceNumber);
		return InvoiceNumber.Format(companySettings.EstablishmentCode, companySettings.PointOfSaleCode, next);
	}

	/// <remarks>
	/// Stock is checked against the database, not against the possibly stale articles held by the cart.
	/// Stock and invoice are then written in one Mongo transaction: all or nothing, no oversell by parallel sales.
	/// </remarks>
	public async Task<SaleResult> CompleteSaleAsync(Invoice invoice, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(invoice);

		var items = invoice.Items ?? [];
		var currentStock = await this.LoadCurrentStockAsync(items.Select(item => item.StockItem.Code), cancellationToken);

		var requests = items.Select(item => new StockRequest(item.StockItem.Code, item.StockItem.Name, item.Amount, currentStock.GetValueOrDefault(item.StockItem.Code)?.Amount ?? 0));
		var shortages = StockAvailability.FindShortages(requests);
		if (shortages.Count > 0) return new SaleResult(shortages.Select(shortage => shortage.Name).ToList());

		// Contingency mode (#149): issue the CDC locally, from the pre-assigned DNIT range, only once the shortage
		// check above has passed and right before the write, so a failed sale never burns a reserved number (#167).
		invoice.Cdc = await _contingencyCdcIssuer.TryIssueAsync(cancellationToken) ?? "";

		var shortItems = await _invoiceServiceProvider.TryAddSaleAsync(invoice, cancellationToken);
		return shortItems.Count == 0 ? SaleResult.Success : new SaleResult(shortItems);
	}

	/// <remarks>Stock is checked before the cart is built, because <see cref="ShoppingCartItem.Amount"/> silently caps at the units in stock.</remarks>
	public async Task<SaleResult> SellAsync(Customer customer, IReadOnlyList<SaleItem> items, SaleCondition saleCondition, DateTime date, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(customer);
		ArgumentNullException.ThrowIfNull(items);
		if (items.Any(item => item.Amount <= 0)) throw new ArgumentOutOfRangeException(nameof(items), "Every amount must be greater than 0.");

		var currentStock = await this.LoadCurrentStockAsync(items.Select(item => item.Code), cancellationToken);

		var requests = items.Select(item => new StockRequest(item.Code, currentStock.GetValueOrDefault(item.Code)?.Name ?? item.Code, item.Amount, currentStock.GetValueOrDefault(item.Code)?.Amount ?? 0));
		var shortages = StockAvailability.FindShortages(requests);
		if (shortages.Count > 0) return new SaleResult(shortages.Select(shortage => shortage.Name).ToList());

		var cartItems = items.Select(item => new ShoppingCartItem(currentStock[item.Code]) { Amount = item.Amount });
		var invoice = await this.CreateInvoiceAsync(customer, cartItems, date, cancellationToken);
		invoice.SaleCondition = saleCondition;
		invoice.Number = await this.GetNextInvoiceNumberAsync(cancellationToken);

		var result = await this.CompleteSaleAsync(invoice, cancellationToken);
		return result.Succeeded ? result with { Invoice = invoice } : result;
	}

	private async Task<Dictionary<string, StockItem>> LoadCurrentStockAsync(IEnumerable<string> codes, CancellationToken cancellationToken)
	{
		Dictionary<string, StockItem> currentStock = [];
		foreach (var code in codes.Distinct())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (await _stockItemServiceProvider.GetStockItemAsync(code, cancellationToken) is not StockItem stockItem) continue;
			currentStock[code] = stockItem;
		}

		return currentStock;
	}

	private static SaleLine ToSaleLine(ShoppingCartItem item)
	{
		return new SaleLine(item.StockItem.Code, item.StockItem.Name, item.Amount, item.StockItem.Price, item.StockItem.VatRatePercent);
	}
}
