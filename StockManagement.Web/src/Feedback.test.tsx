import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { App } from "./App";
import { ErrorBoundary, FeedbackProvider } from "./Feedback";
import { AuthProvider } from "./auth";
import { I18nProvider } from "./i18n";
import { ToastProvider } from "./Toast";
import { mockApi, renderEnglish, sentBody } from "./test-utils";

function Throws(): never
{
	throw new Error("boom");
}

describe("Feedback", () =>
{
	it("ManualReport_FillMessageAndSend_PostsFeedback", async () =>
	{
		// Arrange
		const fetchMock = mockApi({
			"GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } },
			"POST /api/feedback": { body: { succeeded: true } }
		});
		renderEnglish(<App />);

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Report a problem" }));
		await userEvent.type(screen.getByPlaceholderText("What happened?"), "Something is off");
		await userEvent.click(screen.getByRole("button", { name: "Send" }));

		// Assert
		const body = sentBody(fetchMock, "POST /api/feedback") as { category: string; message: string };
		expect(body.category).toBe("Feedback");
		expect(body.message).toBe("Something is off");
		expect(await screen.findByText("Thanks - sent.")).toBeInTheDocument();
	});

	it("RenderError_CaughtByBoundary_OffersSendReportPrefilledAsBug", async () =>
	{
		// Arrange
		const fetchMock = mockApi({ "POST /api/feedback": { body: { succeeded: true } } });
		render(
			<I18nProvider culture="en-US">
				<AuthProvider>
					<ToastProvider>
						<FeedbackProvider>
							<ErrorBoundary><Throws /></ErrorBoundary>
						</FeedbackProvider>
					</ToastProvider>
				</AuthProvider>
			</I18nProvider>
		);

		// Act
		await userEvent.click(await screen.findByRole("button", { name: "Send" }));

		// Assert
		const body = sentBody(fetchMock, "POST /api/feedback") as { category: string; logExcerpt?: string };
		expect(body.category).toBe("Bug");
		expect(body.logExcerpt).toContain("boom");
	});
});
