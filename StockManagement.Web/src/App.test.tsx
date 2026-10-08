import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { App } from "./App";
import { mockApi, renderEnglish } from "./test-utils";

describe("App", () =>
{
	it("SelectLanguage_German_ShowsGermanTexts", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } },
			"GET /api/settings": { body: { language: "English" } },
			"PUT /api/settings": { body: { language: "German" } }
		});
		renderEnglish(<App />);
		await userEvent.click(screen.getByRole("button", { name: "Settings" }));

		// Act
		await userEvent.selectOptions(await screen.findByRole("combobox"), "de-DE");

		// Assert
		expect(screen.getByRole("button", { name: "Neuer Verkauf" })).toBeInTheDocument();
	});

	it("ToggleMenu_Collapsed_KeepsMenuUsable", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } }, "GET /api/customers?pageSize=100": { body: { items: [], nextCursor: null } } });
		renderEnglish(<App />);

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Menu" }));
		await userEvent.click(screen.getByRole("button", { name: "Clients" }));

		// Assert
		expect(screen.queryByText("Clients", { selector: "span" })).not.toBeInTheDocument();
		expect(screen.getByRole("heading", { name: "Clients" })).toBeInTheDocument();
	});

	it("NotLoggedIn_ShowsLoginScreenInsteadOfShell", () =>
	{
		// Arrange + Act
		renderEnglish(<App />, { authenticated: false });

		// Assert
		expect(screen.getByRole("heading", { name: "User login" })).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "Settings" })).not.toBeInTheDocument();
	});

	it("MustChangePassword_ShowsChangePasswordScreenInsteadOfShell", () =>
	{
		// Arrange + Act
		localStorage.setItem("auth.mustChangePassword", "true");
		renderEnglish(<App />);

		// Assert
		expect(screen.getByRole("heading", { name: "Change password" })).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "Settings" })).not.toBeInTheDocument();
	});

	it("WithoutUsersManage_HidesUsersNavItem", () =>
	{
		// Arrange + Act
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } } });
		renderEnglish(<App />, { permissions: ["StockItems.Read"] });

		// Assert
		expect(screen.queryByRole("button", { name: "Users" })).not.toBeInTheDocument();
	});

	it("LicenseTrialing_ShowsBannerWithDaysRemaining", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } },
			"GET /api/license": { body: { status: "Trial", plan: "None", trialEndsAtUtc: "2026-01-20T00:00:00Z", graceEndsAtUtc: null, subscriptionExpiresAtUtc: null, daysRemaining: 3, monthlyPricePyg: 150000, yearlyPricePyg: 1500000 } }
		});
		renderEnglish(<App />);

		// Assert
		expect(await screen.findByText("3 days left in your free trial.")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Settings" })).toBeInTheDocument();
	});

	it("LicenseLocked_ShowsLockScreenInsteadOfShell", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } },
			"GET /api/license": { body: { status: "Locked", plan: "None", trialEndsAtUtc: "2026-01-10T00:00:00Z", graceEndsAtUtc: "2026-01-13T00:00:00Z", subscriptionExpiresAtUtc: null, daysRemaining: 0, monthlyPricePyg: 150000, yearlyPricePyg: 1500000 } }
		});
		renderEnglish(<App />);

		// Assert
		expect(await screen.findByText("Your trial has ended")).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "Settings" })).not.toBeInTheDocument();
	});
});
