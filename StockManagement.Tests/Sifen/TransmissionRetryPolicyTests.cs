using StockManagement.Sifen.Core;

namespace StockManagement.Tests.Sifen;


[TestClass]
public sealed class TransmissionRetryPolicyTests
{
	private static readonly DateTime InvoiceDate = new(2026, 10, 1, 12, 0, 0);

	[TestMethod]
	public void NextAttempt_FirstAttempt_ReturnsAboutOneMinuteLater()
	{
		// Act
		var next = TransmissionRetryPolicy.NextAttempt(InvoiceDate, attempts: 0, now: InvoiceDate);

		// Assert
		Assert.IsNotNull(next);
		Assert.AreEqual(InvoiceDate.AddMinutes(1), next.Value);
	}

	[TestMethod]
	public void NextAttempt_LaterAttempts_BacksOffExponentially()
	{
		// Act
		var third = TransmissionRetryPolicy.NextAttempt(InvoiceDate, attempts: 2, now: InvoiceDate);

		// Assert: base 1 min * 2^2 = 4 min
		Assert.AreEqual(InvoiceDate.AddMinutes(4), third!.Value);
	}

	[TestMethod]
	public void NextAttempt_ManyAttempts_CapsAtOneHour()
	{
		// Act
		var next = TransmissionRetryPolicy.NextAttempt(InvoiceDate, attempts: 20, now: InvoiceDate);

		// Assert
		Assert.AreEqual(InvoiceDate.AddHours(1), next!.Value);
	}

	[TestMethod]
	public void NextAttempt_PastDeadline_ReturnsNull()
	{
		// Act
		var next = TransmissionRetryPolicy.NextAttempt(InvoiceDate, attempts: 0, now: InvoiceDate.AddHours(72));

		// Assert
		Assert.IsNull(next);
	}

	[TestMethod]
	public void NextAttempt_WouldOvershootDeadline_ClampsToDeadline()
	{
		// Act: 1 min before the 72h deadline, next attempt would be a full hour out - clamp instead
		var next = TransmissionRetryPolicy.NextAttempt(InvoiceDate, attempts: 20, now: InvoiceDate.AddHours(72).AddMinutes(-1));

		// Assert
		Assert.AreEqual(InvoiceDate.AddHours(72), next!.Value);
	}

	[TestMethod]
	public void NextAttemptBefore_UsesGivenDeadlineInsteadOfInvoiceDatePlus72h()
	{
		// Arrange: an explicit 48h-style deadline unrelated to the 72h invoice-date default
		var deadline = InvoiceDate.AddHours(48);

		// Act + Assert
		Assert.AreEqual(InvoiceDate.AddMinutes(1), TransmissionRetryPolicy.NextAttemptBefore(deadline, attempts: 0, now: InvoiceDate)!.Value);
		Assert.IsNull(TransmissionRetryPolicy.NextAttemptBefore(deadline, attempts: 0, now: deadline));
	}
}
