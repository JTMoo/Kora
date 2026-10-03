using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sales.Core.Contracts;

namespace StockManagement.Sales.Core;


internal class PaymentLinkService(
	IInvoiceServiceProvider invoiceServiceProvider,
	IPaymentLinkServiceProvider paymentLinkServiceProvider,
	IPaymentLinkGateway gateway,
	IPaymentService paymentService) : IPaymentLinkService
{
	private static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(1);

	private readonly IInvoiceServiceProvider _invoiceServiceProvider = invoiceServiceProvider;
	private readonly IPaymentLinkServiceProvider _paymentLinkServiceProvider = paymentLinkServiceProvider;
	private readonly IPaymentLinkGateway _gateway = gateway;
	private readonly IPaymentService _paymentService = paymentService;


	public async Task<CreatePaymentLinkResult> CreateForInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (await _invoiceServiceProvider.GetInvoiceAync(invoiceNumber, cancellationToken) is not Invoice invoice)
			return CreatePaymentLinkResult.Failure(CreatePaymentLinkError.InvoiceNotFound);

		var amountDue = _paymentService.GetAmountDue(invoice);
		if (amountDue <= 0) return CreatePaymentLinkResult.Failure(CreatePaymentLinkError.AlreadyPaid);

		var now = DateTime.Now;
		if (await _paymentLinkServiceProvider.GetLatestForInvoiceAsync(invoiceNumber, cancellationToken) is PaymentLink existing
			&& existing.Status == PaymentLinkStatus.Pending)
		{
			if (existing.ExpiresAt > now) return CreatePaymentLinkResult.Success(existing);

			existing.Status = PaymentLinkStatus.Expired;
			await _paymentLinkServiceProvider.UpdateAsync(existing, cancellationToken);
		}

		var gatewayResult = await _gateway.CreateAsync(invoiceNumber, amountDue, cancellationToken);
		if (!gatewayResult.Succeeded) return CreatePaymentLinkResult.Failure(CreatePaymentLinkError.GatewayError);

		var link = new PaymentLink
		{
			Invoice = invoice,
			ExternalId = gatewayResult.ExternalId!,
			QrUrl = gatewayResult.QrUrl!,
			Amount = amountDue,
			Status = PaymentLinkStatus.Pending,
			CreatedAt = now,
			ExpiresAt = now + LinkLifetime
		};
		await _paymentLinkServiceProvider.AddAsync(link, cancellationToken);

		return CreatePaymentLinkResult.Success(link);
	}

	public Task<PaymentLink?> GetLatestForInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default)
	{
		return _paymentLinkServiceProvider.GetLatestForInvoiceAsync(invoiceNumber, cancellationToken);
	}

	public async Task<bool> ConfirmAsync(string externalId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (await _paymentLinkServiceProvider.GetByExternalIdAsync(externalId, cancellationToken) is not PaymentLink link) return false;
		if (link.Status != PaymentLinkStatus.Pending) return true;

		if (link.ExpiresAt <= DateTime.Now)
		{
			link.Status = PaymentLinkStatus.Expired;
			await _paymentLinkServiceProvider.UpdateAsync(link, cancellationToken);
			return true;
		}

		var status = await _gateway.GetStatusAsync(externalId, cancellationToken);
		if (status == PaymentLinkStatus.Paid)
		{
			// Best-effort reconciliation: records the payment first, then marks the link paid. A webhook retry that
			// lands between the two sees the link still Pending and tries again - RecordPaymentAsync then rejects it
			// with ExceedsAmountDue (or succeeds a no-op if the invoice was paid some other way since), never double-pays.
			var paymentResult = await _paymentService.RecordPaymentAsync(link.Invoice.Number, link.Amount, PaymentMethod.BancardQr, DateTime.Now, cancellationToken);
			if (!paymentResult.Succeeded) return false;

			link.Status = PaymentLinkStatus.Paid;
			link.PaidAt = DateTime.Now;
		}
		else
		{
			link.Status = status;
		}

		await _paymentLinkServiceProvider.UpdateAsync(link, cancellationToken);
		return true;
	}
}
