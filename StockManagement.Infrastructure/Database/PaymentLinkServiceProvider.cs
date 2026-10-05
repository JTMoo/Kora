using Microsoft.EntityFrameworkCore;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;

namespace StockManagement.Infrastructure.Database;


/// <summary>
/// <see cref="IPaymentLinkServiceProvider"/> on <see cref="AppDbContext"/>
/// </summary>
public class PaymentLinkServiceProvider(AppDbContext db) : IPaymentLinkServiceProvider
{
	private readonly AppDbContext _db = db;


	public async Task<PaymentLink?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
	{
		return await _db.PaymentLinks.FirstOrDefaultAsync(link => link.ExternalId == externalId, cancellationToken);
	}

	public async Task<PaymentLink?> GetLatestForInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default)
	{
		return await _db.PaymentLinks
			.Where(link => link.Invoice.Number == invoiceNumber)
			.OrderByDescending(link => link.CreatedAt)
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task AddAsync(PaymentLink link, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(link);

		_db.PaymentLinks.Add(link);
		await _db.SaveChangesAsync(cancellationToken);
	}

	public async Task<int> UpdateAsync(PaymentLink link, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(link);

		_db.PaymentLinks.Update(link);
		return await _db.SaveChangesAsync(cancellationToken);
	}
}
