using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Sifen.Core;


/// <summary>
/// Polls the <see cref="Kernel.Model.PendingTransmission"/> outbox and calls <see cref="ISifenGateway"/> for each
/// due row (ADR-0031). No message broker - single-process API, same reasoning as every other "no fire-and-forget" rule.
/// </summary>
public sealed class SifenTransmissionWorker(IServiceScopeFactory scopeFactory, ILogger<SifenTransmissionWorker> logger) : BackgroundService
{
	private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
	private const int BatchSize = 20;

	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
	private readonly ILogger<SifenTransmissionWorker> _logger = logger;


	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(PollInterval);
		do
		{
			await this.ProcessDueAsync(stoppingToken);
		}
		while (await timer.WaitForNextTickAsync(stoppingToken));
	}

	private async Task ProcessDueAsync(CancellationToken cancellationToken)
	{
		await using var scope = _scopeFactory.CreateAsyncScope();
		var gateway = scope.ServiceProvider.GetRequiredService<ISifenGateway>();

		await this.ProcessDueInvoicesAsync(scope.ServiceProvider.GetRequiredService<IPendingTransmissionServiceProvider>(), gateway, cancellationToken);
		await this.ProcessDueRemisionesAsync(scope.ServiceProvider.GetRequiredService<IPendingRemisionTransmissionServiceProvider>(), gateway, cancellationToken);
		await this.ProcessDueDebitNotesAsync(scope.ServiceProvider.GetRequiredService<IPendingDebitNoteTransmissionServiceProvider>(), gateway, cancellationToken);
		await this.ProcessDueCancellationsAsync(scope.ServiceProvider.GetRequiredService<ICancellationRequestServiceProvider>(), gateway, cancellationToken);
		await this.ProcessDueNumberVoidsAsync(scope.ServiceProvider.GetRequiredService<IInvoiceNumberVoidServiceProvider>(), gateway, cancellationToken);
	}

	/// <summary>
	/// Same polling loop for the <see cref="Kernel.Model.CancellationRequest"/> outbox (#206).
	/// </summary>
	private async Task ProcessDueCancellationsAsync(ICancellationRequestServiceProvider cancellationRequests, ISifenGateway gateway, CancellationToken cancellationToken)
	{
		var due = await cancellationRequests.GetDueAsync(DateTime.Now, BatchSize, cancellationToken);
		foreach (var request in due)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				var result = await gateway.SendCancellationEventAsync(request, cancellationToken);
				await SifenTransmissionProcessor.ApplyCancellationAsync(cancellationRequests, request, result, DateTime.Now, cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "SIFEN Cancelación failed for invoice {InvoiceNumber}", request.Invoice.Number);
			}
		}
	}

	/// <summary>
	/// Same polling loop for the <see cref="Kernel.Model.InvoiceNumberVoid"/> outbox (#206).
	/// </summary>
	private async Task ProcessDueNumberVoidsAsync(IInvoiceNumberVoidServiceProvider numberVoids, ISifenGateway gateway, CancellationToken cancellationToken)
	{
		var due = await numberVoids.GetDueAsync(DateTime.Now, BatchSize, cancellationToken);
		foreach (var numberVoid in due)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				var result = await gateway.SendInutilizacionEventAsync(numberVoid, cancellationToken);
				await SifenTransmissionProcessor.ApplyInutilizacionAsync(numberVoids, numberVoid, result, DateTime.Now, cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "SIFEN Inutilización failed for range {RangeStart}-{RangeEnd}", numberVoid.RangeStart, numberVoid.RangeEnd);
			}
		}
	}

	private async Task ProcessDueInvoicesAsync(IPendingTransmissionServiceProvider pendingTransmissions, ISifenGateway gateway, CancellationToken cancellationToken)
	{
		var due = await pendingTransmissions.GetDueAsync(DateTime.Now, BatchSize, cancellationToken);
		foreach (var transmission in due)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				var result = await gateway.SendAsync(transmission.Invoice, cancellationToken);
				await SifenTransmissionProcessor.ApplyAsync(pendingTransmissions, transmission, result, DateTime.Now, cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "SIFEN transmission failed for invoice {InvoiceNumber}", transmission.Invoice.Number);
			}
		}
	}

	/// <summary>
	/// Same polling loop for the <see cref="Kernel.Model.RemissionNote"/> outbox (#162).
	/// </summary>
	private async Task ProcessDueRemisionesAsync(IPendingRemisionTransmissionServiceProvider pendingTransmissions, ISifenGateway gateway, CancellationToken cancellationToken)
	{
		var due = await pendingTransmissions.GetDueAsync(DateTime.Now, BatchSize, cancellationToken);
		foreach (var transmission in due)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				var result = await gateway.SendRemisionAsync(transmission.RemissionNote, cancellationToken);
				await SifenTransmissionProcessor.ApplyRemisionAsync(pendingTransmissions, transmission, result, DateTime.Now, cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "SIFEN transmission failed for remission note {RemissionNoteNumber}", transmission.RemissionNote.Number);
			}
		}
	}

	/// <summary>
	/// Same polling loop for the <see cref="Kernel.Model.DebitNote"/> outbox (#184).
	/// </summary>
	private async Task ProcessDueDebitNotesAsync(IPendingDebitNoteTransmissionServiceProvider pendingTransmissions, ISifenGateway gateway, CancellationToken cancellationToken)
	{
		var due = await pendingTransmissions.GetDueAsync(DateTime.Now, BatchSize, cancellationToken);
		foreach (var transmission in due)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				var result = await gateway.SendDebitNoteAsync(transmission.DebitNote, cancellationToken);
				await SifenTransmissionProcessor.ApplyDebitNoteAsync(pendingTransmissions, transmission, result, DateTime.Now, cancellationToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "SIFEN transmission failed for debit note {DebitNoteNumber}", transmission.DebitNote.Number);
			}
		}
	}
}
