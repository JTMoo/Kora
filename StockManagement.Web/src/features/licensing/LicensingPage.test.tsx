import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { mockApi, renderEnglish, sentBody } from "../../test-utils";
import { LicensingPage } from "./LicensingPage";

const trial = { status: "Trial", plan: "None", trialEndsAtUtc: "2026-01-20T00:00:00Z", graceEndsAtUtc: null, subscriptionExpiresAtUtc: null, daysRemaining: 7, monthlyPricePyg: 150000, yearlyPricePyg: 1500000 };
const active = { status: "Active", plan: "Yearly", trialEndsAtUtc: "2026-01-10T00:00:00Z", graceEndsAtUtc: null, subscriptionExpiresAtUtc: "2027-01-01T00:00:00Z", daysRemaining: 300, monthlyPricePyg: 150000, yearlyPricePyg: 1500000 };

describe("LicensingPage", () =>
{
	it("Load_Trial_ShowsStatusAndDaysRemaining", async () =>
	{
		// Arrange
		mockApi({ "GET /api/license": { body: trial } });
		renderEnglish(<LicensingPage />);

		// Assert
		expect(await screen.findByText("Trial")).toBeInTheDocument();
		expect(screen.getByText("7")).toBeInTheDocument();
	});

	it("Activate_ValidKey_ShowsActivePlan", async () =>
	{
		// Arrange
		mockApi({ "GET /api/license": { body: trial }, "POST /api/license/activate": { body: active } });
		renderEnglish(<LicensingPage />);
		await screen.findByText("Trial");

		// Act
		await userEvent.type(screen.getByLabelText("License Key"), "some-key");
		await userEvent.click(screen.getByRole("button", { name: "Activate" }));

		// Assert
		expect(await screen.findByText("Active")).toBeInTheDocument();
		expect(screen.getByText("Yearly")).toBeInTheDocument();
	});

	it("Activate_InvalidKey_ShowsFailure", async () =>
	{
		// Arrange
		const fetchMock = mockApi({ "GET /api/license": { body: trial }, "POST /api/license/activate": { status: 422, body: { reason: "licenseKeyInvalid" } } });
		renderEnglish(<LicensingPage />);
		await screen.findByText("Trial");

		// Act
		await userEvent.type(screen.getByLabelText("License Key"), "bad-key");
		await userEvent.click(screen.getByRole("button", { name: "Activate" }));

		// Assert
		expect(await screen.findByRole("alert")).toHaveTextContent("This license key is not valid.");
		expect(sentBody(fetchMock, "POST /api/license/activate")).toMatchObject({ licenseKey: "bad-key" });
	});

	it("StandardUserWithoutSettingsWrite_HidesActivationForm", async () =>
	{
		// Arrange
		mockApi({ "GET /api/license": { body: trial } });
		renderEnglish(<LicensingPage />, { role: "Standard", permissions: [] });

		// Assert
		await screen.findByText("Trial");
		expect(screen.queryByLabelText("License Key")).not.toBeInTheDocument();
	});
});
