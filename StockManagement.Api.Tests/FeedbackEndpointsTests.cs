using System.Net;
using System.Net.Http.Json;
using StockManagement.Api.Features.Feedback;
using StockManagement.Feedback.Core.Contracts;

namespace StockManagement.Api.Tests;


[TestClass]
public sealed class FeedbackEndpointsTests
{
	private ApiFactory _factory;
	private HttpClient _client;


	[TestInitialize]
	public async Task InitializeAsync()
	{
		_factory = new();
		_client = await _factory.CreateAuthenticatedClientAsync();
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
		_factory.Dispose();
	}


	[TestMethod]
	public async Task SubmitFeedback_ValidRequest_Succeeds()
	{
		// Act - no Feedback:RelayUrl configured in tests, so this hits NullReportSink rather than a real network call
		var response = await _client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackRequest(ReportCategory.Feedback, "Love the app", null, "corr-1"));

		// Assert
		Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		Assert.IsTrue((await response.Content.ReadAsAsync<SubmitFeedbackResponse>()).Succeeded);
	}

	[TestMethod]
	public async Task SubmitFeedback_EmptyMessage_ReturnsBadRequest()
	{
		// Act
		var response = await _client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackRequest(ReportCategory.Bug, "", null, "corr-2"));

		// Assert
		Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
		StringAssert.Contains(await response.Content.ReadAsStringAsync(), "feedbackMessageRequired");
	}

	[TestMethod]
	public async Task SubmitFeedback_NoAuth_ReturnsUnauthorized()
	{
		// Arrange
		var anonymous = _factory.CreateClient();

		// Act
		var response = await anonymous.PostAsJsonAsync("/api/feedback", new SubmitFeedbackRequest(ReportCategory.Bug, "It crashed", null, "corr-3"));

		// Assert
		Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
	}
}
