import { send } from "./client";

export type ReportCategory = "Feedback" | "Bug";

export type SubmitFeedbackResult = { succeeded: boolean };

export const feedbackApi = {
	submitFeedback: (category: ReportCategory, message: string, logExcerpt: string | undefined, correlationId: string) =>
		send<SubmitFeedbackResult>("/feedback", { method: "POST", body: JSON.stringify({ category, message, logExcerpt, correlationId }) })
};
