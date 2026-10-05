using Moq;
using StockManagement.Kernel.Database.Interfaces;
using StockManagement.Kernel.Model;
using StockManagement.Kernel.Model.Types;
using StockManagement.Sifen.Core;
using StockManagement.Sifen.Core.Contracts;

namespace StockManagement.Tests.Sifen;


[TestClass]
public sealed class SifenTransmissionProcessorTests
{
	private readonly Mock<IPendingTransmissionServiceProvider> _pendingTransmissions = new();

	[TestMethod]
	public async Task ApplyAsync_Accepted_MarksInvoiceAcceptedWithCdc()
	{
		// Arrange
		var transmission = new PendingTransmission(new Invoice(), DateTime.Now);
		var result = SifenTransmissionResult.Accepted("01" + new string('0', 42));

		// Act
		await SifenTransmissionProcessor.ApplyAsync(_pendingTransmissions.Object, transmission, result, DateTime.Now);

		// Assert
		_pendingTransmissions.Verify(provider => provider.MarkTerminalAsync(transmission, TransmissionStatus.Accepted, result.Cdc!, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task ApplyAsync_Rejected_MarksInvoiceRejectedAndDoesNotReschedule()
	{
		// Arrange
		var transmission = new PendingTransmission(new Invoice(), DateTime.Now);
		var result = SifenTransmissionResult.Rejected("cdc", "invalid RUC");

		// Act
		await SifenTransmissionProcessor.ApplyAsync(_pendingTransmissions.Object, transmission, result, DateTime.Now);

		// Assert
		_pendingTransmissions.Verify(provider => provider.MarkTerminalAsync(transmission, TransmissionStatus.Rejected, "cdc", It.IsAny<CancellationToken>()), Times.Once);
		_pendingTransmissions.Verify(provider => provider.MarkErrorAsync(It.IsAny<PendingTransmission>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task ApplyAsync_Error_SchedulesNextAttemptViaRetryPolicy()
	{
		// Arrange
		var now = new DateTime(2026, 10, 2, 12, 0, 0);
		var invoice = new Invoice { Date = now };
		var transmission = new PendingTransmission(invoice, now) { Attempts = 0 };
		var result = SifenTransmissionResult.Error("timeout");

		// Act
		await SifenTransmissionProcessor.ApplyAsync(_pendingTransmissions.Object, transmission, result, now);

		// Assert
		_pendingTransmissions.Verify(provider => provider.MarkErrorAsync(transmission, "timeout", now.AddMinutes(1), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task ApplyAsync_ErrorPastDeadline_SchedulesNoFurtherAttempt()
	{
		// Arrange
		var invoiceDate = new DateTime(2026, 10, 2, 12, 0, 0);
		var now = invoiceDate.AddHours(73);
		var invoice = new Invoice { Date = invoiceDate };
		var transmission = new PendingTransmission(invoice, now) { Attempts = 5 };
		var result = SifenTransmissionResult.Error("timeout");

		// Act
		await SifenTransmissionProcessor.ApplyAsync(_pendingTransmissions.Object, transmission, result, now);

		// Assert
		_pendingTransmissions.Verify(provider => provider.MarkErrorAsync(transmission, "timeout", null, It.IsAny<CancellationToken>()), Times.Once);
	}
}


/// <summary>
/// Same decisions as <see cref="SifenTransmissionProcessorTests"/>, for <see cref="SifenTransmissionProcessor.ApplyRemisionAsync"/> (#162).
/// </summary>
[TestClass]
public sealed class SifenTransmissionProcessorRemisionTests
{
	private readonly Mock<IPendingRemisionTransmissionServiceProvider> _pendingTransmissions = new();

	[TestMethod]
	public async Task ApplyRemisionAsync_Accepted_MarksRemissionNoteAcceptedWithCdc()
	{
		// Arrange
		var transmission = new PendingRemisionTransmission(new RemissionNote(), DateTime.Now);
		var result = SifenTransmissionResult.Accepted("07" + new string('0', 42));

		// Act
		await SifenTransmissionProcessor.ApplyRemisionAsync(_pendingTransmissions.Object, transmission, result, DateTime.Now);

		// Assert
		_pendingTransmissions.Verify(provider => provider.MarkTerminalAsync(transmission, TransmissionStatus.Accepted, result.Cdc!, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task ApplyRemisionAsync_Error_SchedulesNextAttemptViaRetryPolicy()
	{
		// Arrange
		var now = new DateTime(2026, 10, 3, 12, 0, 0);
		var remissionNote = new RemissionNote { Date = now };
		var transmission = new PendingRemisionTransmission(remissionNote, now) { Attempts = 0 };
		var result = SifenTransmissionResult.Error("timeout");

		// Act
		await SifenTransmissionProcessor.ApplyRemisionAsync(_pendingTransmissions.Object, transmission, result, now);

		// Assert
		_pendingTransmissions.Verify(provider => provider.MarkErrorAsync(transmission, "timeout", now.AddMinutes(1), It.IsAny<CancellationToken>()), Times.Once);
	}
}


/// <summary>
/// Same decisions as <see cref="SifenTransmissionProcessorTests"/>, for <see cref="SifenTransmissionProcessor.ApplyCancellationAsync"/> (#206).
/// </summary>
[TestClass]
public sealed class SifenTransmissionProcessorCancellationTests
{
	private readonly Mock<ICancellationRequestServiceProvider> _cancellationRequests = new();

	[TestMethod]
	public async Task ApplyCancellationAsync_Accepted_MarksRequestAccepted()
	{
		// Arrange
		var request = new CancellationRequest(new Invoice(), "reason", DateTime.Now);
		var result = SifenTransmissionResult.Accepted("");

		// Act
		await SifenTransmissionProcessor.ApplyCancellationAsync(_cancellationRequests.Object, request, result, DateTime.Now);

		// Assert
		_cancellationRequests.Verify(provider => provider.MarkTerminalAsync(request, TransmissionStatus.Accepted, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task ApplyCancellationAsync_Rejected_MarksRequestRejectedAndDoesNotReschedule()
	{
		// Arrange
		var request = new CancellationRequest(new Invoice(), "reason", DateTime.Now);
		var result = SifenTransmissionResult.Rejected("", "invalid reference");

		// Act
		await SifenTransmissionProcessor.ApplyCancellationAsync(_cancellationRequests.Object, request, result, DateTime.Now);

		// Assert
		_cancellationRequests.Verify(provider => provider.MarkTerminalAsync(request, TransmissionStatus.Rejected, It.IsAny<CancellationToken>()), Times.Once);
		_cancellationRequests.Verify(provider => provider.MarkErrorAsync(It.IsAny<CancellationRequest>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	public async Task ApplyCancellationAsync_Error_SchedulesNextAttemptWithin48hOfAcceptedAt()
	{
		// Arrange: deadline anchors on the invoice's AcceptedAt, not its Date (#206) - 72h "transmission" window ignored here
		var now = new DateTime(2026, 10, 5, 12, 0, 0);
		var invoice = new Invoice { Date = now.AddDays(-5), AcceptedAt = now };
		var request = new CancellationRequest(invoice, "reason", now) { Attempts = 0 };
		var result = SifenTransmissionResult.Error("timeout");

		// Act
		await SifenTransmissionProcessor.ApplyCancellationAsync(_cancellationRequests.Object, request, result, now);

		// Assert
		_cancellationRequests.Verify(provider => provider.MarkErrorAsync(request, "timeout", now.AddMinutes(1), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task ApplyCancellationAsync_ErrorPast48hWindow_SchedulesNoFurtherAttempt()
	{
		// Arrange
		var acceptedAt = new DateTime(2026, 10, 1, 12, 0, 0);
		var now = acceptedAt.AddHours(49);
		var invoice = new Invoice { Date = acceptedAt, AcceptedAt = acceptedAt };
		var request = new CancellationRequest(invoice, "reason", acceptedAt) { Attempts = 5 };
		var result = SifenTransmissionResult.Error("timeout");

		// Act
		await SifenTransmissionProcessor.ApplyCancellationAsync(_cancellationRequests.Object, request, result, now);

		// Assert
		_cancellationRequests.Verify(provider => provider.MarkErrorAsync(request, "timeout", null, It.IsAny<CancellationToken>()), Times.Once);
	}
}


/// <summary>
/// Same decisions as <see cref="SifenTransmissionProcessorTests"/>, for <see cref="SifenTransmissionProcessor.ApplyInutilizacionAsync"/> (#206).
/// </summary>
[TestClass]
public sealed class SifenTransmissionProcessorInutilizacionTests
{
	private readonly Mock<IInvoiceNumberVoidServiceProvider> _numberVoids = new();

	[TestMethod]
	public async Task ApplyInutilizacionAsync_Accepted_MarksVoidAccepted()
	{
		// Arrange
		var numberVoid = new InvoiceNumberVoid(1, 10, "reason", DateTime.Now);
		var result = SifenTransmissionResult.Accepted("");

		// Act
		await SifenTransmissionProcessor.ApplyInutilizacionAsync(_numberVoids.Object, numberVoid, result, DateTime.Now);

		// Assert
		_numberVoids.Verify(provider => provider.MarkTerminalAsync(numberVoid, TransmissionStatus.Accepted, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task ApplyInutilizacionAsync_Error_SchedulesNextAttemptViaRetryPolicy()
	{
		// Arrange
		var now = new DateTime(2026, 10, 5, 12, 0, 0);
		var numberVoid = new InvoiceNumberVoid(1, 10, "reason", now) { Attempts = 0 };
		var result = SifenTransmissionResult.Error("timeout");

		// Act
		await SifenTransmissionProcessor.ApplyInutilizacionAsync(_numberVoids.Object, numberVoid, result, now);

		// Assert
		_numberVoids.Verify(provider => provider.MarkErrorAsync(numberVoid, "timeout", now.AddMinutes(1), It.IsAny<CancellationToken>()), Times.Once);
	}
}
