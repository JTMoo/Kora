using Microsoft.Extensions.Options;
using Moq;
using StockManagement.Feedback.Core;
using StockManagement.Feedback.Core.Contracts;

namespace StockManagement.Tests.Feedback;


[TestClass]
public sealed class FeedbackServiceTests
{
	private readonly Mock<IReportSink> _reportSink = new();
	private readonly FeedbackOptions _options = new() { MaxMessageLength = 20, MaxLogExcerptLength = 30 };


	[TestMethod]
	public async Task SubmitAsync_ValidReport_PassesCategoryAndCorrelationIdToSink()
	{
		// Arrange
		_reportSink.Setup(sink => sink.SubmitAsync(It.IsAny<UserReport>(), It.IsAny<CancellationToken>())).ReturnsAsync(ReportSubmissionResult.Success);

		// Act
		var result = await this.CreateService().SubmitAsync(ReportCategory.Bug, "It crashed", "stack trace", "corr-1");

		// Assert
		Assert.IsTrue(result.Succeeded);
		_reportSink.Verify(sink => sink.SubmitAsync(It.Is<UserReport>(report =>
			report.Category == ReportCategory.Bug && report.Message == "It crashed" && report.LogExcerpt == "stack trace" && report.CorrelationId == "corr-1"),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task SubmitAsync_MessageLongerThanMax_TruncatesBeforeSending()
	{
		// Arrange
		_reportSink.Setup(sink => sink.SubmitAsync(It.IsAny<UserReport>(), It.IsAny<CancellationToken>())).ReturnsAsync(ReportSubmissionResult.Success);
		var longMessage = new string('x', 100);

		// Act
		await this.CreateService().SubmitAsync(ReportCategory.Feedback, longMessage, null, "corr-2");

		// Assert
		_reportSink.Verify(sink => sink.SubmitAsync(It.Is<UserReport>(report => report.Message.Length == 20), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task SubmitAsync_NoLogExcerpt_PassesNullThrough()
	{
		// Arrange
		_reportSink.Setup(sink => sink.SubmitAsync(It.IsAny<UserReport>(), It.IsAny<CancellationToken>())).ReturnsAsync(ReportSubmissionResult.Success);

		// Act
		await this.CreateService().SubmitAsync(ReportCategory.Feedback, "Love the app", null, "corr-3");

		// Assert
		_reportSink.Verify(sink => sink.SubmitAsync(It.Is<UserReport>(report => report.LogExcerpt == null), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task SubmitAsync_SinkFails_ReturnsFailure()
	{
		// Arrange
		_reportSink.Setup(sink => sink.SubmitAsync(It.IsAny<UserReport>(), It.IsAny<CancellationToken>())).ReturnsAsync(ReportSubmissionResult.Failure);

		// Act
		var result = await this.CreateService().SubmitAsync(ReportCategory.Bug, "It crashed", null, "corr-4");

		// Assert
		Assert.IsFalse(result.Succeeded);
	}

	[TestMethod]
	public async Task SubmitAsync_BlankMessage_Throws()
	{
		// Act & Assert
		await Assert.ThrowsExceptionAsync<ArgumentException>(() => this.CreateService().SubmitAsync(ReportCategory.Feedback, "   ", null, "corr-5"));
	}

	private FeedbackService CreateService()
	{
		return new FeedbackService(_reportSink.Object, Options.Create(_options));
	}
}
