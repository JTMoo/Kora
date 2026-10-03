using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Kernel.Util;
using StockManagement.Sales.Core.Contracts;
using StockManagement.Settings.Core.Contracts;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sales.Core;


internal class RemissionNoteService(IRemissionNoteServiceProvider remissionNoteServiceProvider, ISettingsService settingsService, IContingencyCdcIssuer contingencyCdcIssuer) : IRemissionNoteService
{
	private readonly IRemissionNoteServiceProvider _remissionNoteServiceProvider = remissionNoteServiceProvider;
	private readonly ISettingsService _settingsService = settingsService;
	private readonly IContingencyCdcIssuer _contingencyCdcIssuer = contingencyCdcIssuer;


	/// <remarks>"Highest + 1" scoped to remission notes already carrying the configured establishment/point-of-sale prefix, same approach as <see cref="SaleService.GetNextInvoiceNumberAsync"/> - own sequence, not shared with invoices.</remarks>
	public async Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var remissionNotes = await _remissionNoteServiceProvider.GetRemissionNotesAsync(cancellationToken) ?? [];
		var companySettings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
		var sequencesInUse = remissionNotes
			.Select(remissionNote => InvoiceNumber.TryParseSequence(remissionNote.Number, companySettings.EstablishmentCode, companySettings.PointOfSaleCode))
			.Where(sequence => sequence.HasValue)
			.Select(sequence => sequence!.Value);
		var next = SequenceNumber.Next(sequencesInUse, companySettings.FirstInvoiceNumber);
		return InvoiceNumber.Format(companySettings.EstablishmentCode, companySettings.PointOfSaleCode, next);
	}

	public async Task<RemissionNote> CreateAsync(Customer customer, IReadOnlyList<(StockItem StockItem, int Amount)> items, RemissionReason reason, string destinationAddress, DateTime date, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(customer);
		ArgumentNullException.ThrowIfNull(items);
		if (items.Count == 0) throw new ArgumentException("A remission note needs at least one item.", nameof(items));
		if (items.Any(item => item.Amount <= 0)) throw new ArgumentOutOfRangeException(nameof(items), "Every amount must be greater than 0.");

		var remissionNote = new RemissionNote
		{
			Customer = customer,
			Date = date,
			Reason = reason,
			DestinationAddress = destinationAddress,
			Number = await this.GetNextNumberAsync(cancellationToken),
			Items = items.Select(item => new RemissionNoteItem(item.StockItem) { Amount = item.Amount }).ToList(),
		};

		// Contingency mode (#149/#188): issue the CDC locally, from the pre-assigned DNIT range, only once every
		// argument check above has passed and right before the write - same reservation point as SaleService's
		// CompleteSaleAsync, so a rejected remission note never burns a reserved number (#167).
		remissionNote.Cdc = await _contingencyCdcIssuer.TryIssueAsync(SifenDocumentType.NotaDeRemisionElectronica, cancellationToken) ?? "";

		await _remissionNoteServiceProvider.AddAsync(remissionNote, cancellationToken);
		return remissionNote;
	}
}
