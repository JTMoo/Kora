import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { mockApi, renderEnglish } from "../../test-utils";
import { LicenseLockedScreen } from "./LicenseLockedScreen";

describe("LicenseLockedScreen", () =>
{
	it("ReportProblem_Click_OpensFeedbackDialogAndSubmitsWhileLocked", async () =>
	{
		// Arrange: a locked-out user has no other working endpoint, but /feedback must stay reachable (ADR-0042)
		const fetchMock = mockApi({ "POST /api/feedback": { body: { succeeded: true } } });
		renderEnglish(<LicenseLockedScreen onActivated={vi.fn()} />);

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Report a problem" }));
		await userEvent.type(screen.getByLabelText("Message"), "I'm locked out");
		await userEvent.click(screen.getByRole("button", { name: "Send" }));

		// Assert
		expect(fetchMock).toHaveBeenCalledWith("/api/feedback", expect.objectContaining({ method: "POST" }));
	});
});
