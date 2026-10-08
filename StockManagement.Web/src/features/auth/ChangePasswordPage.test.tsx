import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { mockApi, renderEnglish } from "../../test-utils";
import { ChangePasswordPage } from "./ChangePasswordPage";

describe("ChangePasswordPage", () =>
{
	it("Submit_CorrectCurrentPassword_ClearsForcedFlag", async () =>
	{
		// Arrange
		mockApi({ "PUT /api/auth/password": { status: 200 } });
		localStorage.setItem("auth.mustChangePassword", "true");
		renderEnglish(<ChangePasswordPage />);

		// Act
		await userEvent.type(screen.getByLabelText("Current password"), "ChangeMe123!");
		await userEvent.type(screen.getByLabelText("New password"), "newPassword1!");
		await userEvent.click(screen.getByRole("button", { name: "Change password" }));

		// Assert
		await waitFor(() => expect(localStorage.getItem("auth.mustChangePassword")).toBe("false"));
	});

	it("Submit_WrongCurrentPassword_ShowsFailureWithoutLoggingOut", async () =>
	{
		// Arrange: 400 with the validation-style error, not 401 - a 401 would trip the shared "session expired" logout
		mockApi({ "PUT /api/auth/password": { status: 400, body: { errors: [{ reason: "incorrectCurrentPassword" }] } } });
		localStorage.setItem("auth.mustChangePassword", "true");
		renderEnglish(<ChangePasswordPage />);

		// Act
		await userEvent.type(screen.getByLabelText("Current password"), "wrong");
		await userEvent.type(screen.getByLabelText("New password"), "newPassword1!");
		await userEvent.click(screen.getByRole("button", { name: "Change password" }));

		// Assert
		expect(await screen.findByRole("alert")).toHaveTextContent("Current password is incorrect.");
		expect(localStorage.getItem("auth.mustChangePassword")).toBe("true");
	});
});
