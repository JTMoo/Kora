using FastEndpoints;
using FluentValidation;

namespace StockManagement.Api.Features.Feedback;


public class SubmitFeedbackValidator : Validator<SubmitFeedbackRequest>
{
	public SubmitFeedbackValidator()
	{
		this.RuleFor(request => request.Message).NotEmpty().WithMessage("feedbackMessageRequired");
		this.RuleFor(request => request.CorrelationId).NotEmpty().WithMessage("invalidInput");
	}
}
