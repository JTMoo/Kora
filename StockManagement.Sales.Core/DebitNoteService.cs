using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Util;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sales.Core;


internal class DebitNoteService(IDebitNoteServiceProvider debitNoteServiceProvider, ISettingsService settingsService, IContingencyCdcIssuer contingencyCdcIssuer) : IDebitNoteService
{
	private readonly IDebitNoteServiceProvider _debitNoteServiceProvider = debitNoteServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;
	private readonly IContingencyCdcIssuer _contingencyCdcIssuer = contingencyCdcIssuer;


	/// <remarks>"Highest + 1" scoped to debit notes already carrying the configured establishment/point-of-sale prefix, same approach as <see cref="SaleService.GetNextInvoiceNumberAsync"/> - own sequence, not shared with invoices.</remarks>
	public async Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var debitNotes = await _debitNoteServiceProvider.GetDebitNotesAsync(cancellationToken) ?? [];
		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
		var sequencesInUse = debitNotes
			.Select(debitNote => InvoiceNumber.TryParseSequence(debitNote.Number, companySettings.EstablishmentCode, companySettings.PointOfSaleCode))
			.Where(sequence => sequence.HasValue)
			.Select(sequence => sequence!.Value);
		var next = SequenceNumber.Next(sequencesInUse, companySettings.FirstInvoiceNumber);
		return InvoiceNumber.Format(companySettings.EstablishmentCode, companySettings.PointOfSaleCode, next);
	}

	public async Task<DebitNote> CreateAsync(Invoice invoice, string reason, IReadOnlyList<(string Description, decimal Amount, int VatRatePercent)> items, DateTime date, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(invoice);
		ArgumentNullException.ThrowIfNull(items);
		if (items.Count == 0) throw new ArgumentException("A debit note needs at least one item.", nameof(items));
		if (items.Any(item => item.Amount <= 0)) throw new ArgumentOutOfRangeException(nameof(items), "Every amount must be greater than 0.");
		if (string.IsNullOrEmpty(invoice.Cdc)) throw new InvalidOperationException($"Invoice '{invoice.Number}' has no Cdc yet; it must be transmitted to SIFEN before a debit note can reference it.");

		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
		var debitNoteItems = items.Select(item => new DebitNoteItem { Description = item.Description, Amount = item.Amount, VatRatePercent = item.VatRatePercent }).ToList();
		var total = debitNoteItems.Sum(item => item.Amount);
		var tax = debitNoteItems.Sum(item => Math.Round(item.Amount * item.VatRatePercent / (100 + item.VatRatePercent), companySettings.CurrencyDecimalDigits, MidpointRounding.AwayFromZero));

		var debitNote = new DebitNote
		{
			Invoice = invoice,
			Date = date,
			Reason = reason,
			Items = debitNoteItems,
			Total = total,
			Tax = tax,
			Number = await this.GetNextNumberAsync(cancellationToken),
		};

		// Contingency mode: issue the CDC locally, from the pre-assigned DNIT range, only once every argument check
		// above has passed and right before the write - same reservation point as SaleService.CompleteSaleAsync and
		// RemissionNoteService.CreateAsync, so a rejected debit note never burns a reserved number (#167).
		debitNote.Cdc = await _contingencyCdcIssuer.TryIssueAsync(SifenDocumentType.NotaDeDebitoElectronica, cancellationToken) ?? "";

		await _debitNoteServiceProvider.AddAsync(debitNote, cancellationToken);
		return debitNote;
	}
}
