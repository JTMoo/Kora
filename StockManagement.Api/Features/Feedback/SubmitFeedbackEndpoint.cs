using FastEndpoints;
using StockManagement.Feedback.Core.Contracts;

namespace StockManagement.Api.Features.Feedback;


public sealed record SubmitFeedbackRequest(ReportCategory Category, string Message, string? LogExcerpt, string CorrelationId);


public sealed record SubmitFeedbackResponse(bool Succeeded);


/// <summary>
/// Single entry point for manual feedback/bug reports and for the web client's "send report?" prompt on an
/// unhandled error - both call this with the same shape (ADR-0042). Any authenticated user may submit.
/// </summary>
public class SubmitFeedbackEndpoint(IFeedbackService feedbackService) : Endpoint<SubmitFeedbackRequest, SubmitFeedbackResponse>
{
	private readonly IFeedbackService _feedbackService = feedbackService;


	public override void Configure()
	{
		this.Post("/feedback");
	}

	public override async Task<SubmitFeedbackResponse> ExecuteAsync(SubmitFeedbackRequest request, CancellationToken cancellationToken)
	{
		var result = await _feedbackService.SubmitAsync(request.Category, request.Message, request.LogExcerpt, request.CorrelationId, cancellationToken);
		return new SubmitFeedbackResponse(result.Succeeded);
	}
}
