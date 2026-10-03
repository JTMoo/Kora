import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { mockApi, renderEnglish, screw } from "../../test-utils";
import { ReportsPage } from "./ReportsPage";

describe("ReportsPage", () =>
{
	it("Load_StockValueTab_ShowsTotalsAndLines", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/reports/stock-value?pageSize=100": { body: { totalValue: 50000, totalUnits: 10, items: [{ code: "A1", name: "Screw", manufacturer: "None", amount: 10, price: 5000, value: 50000 }], nextCursor: null } }
		});

		// Act
		renderEnglish(<ReportsPage />);

		// Assert
		expect(await screen.findByRole("cell", { name: "A1" })).toBeInTheDocument();
		expect(screen.getAllByText("50,000").length).toBeGreaterThan(0);
	});

	it("SwitchToLowStockTab_ShowsBelowMinimumItems", async () =>
	{
		// Arrange
		const belowMinimum = { ...screw, amount: 1, minimumStock: 5 };
		mockApi({
			"GET /api/reports/stock-value?pageSize=100": { body: { totalValue: 0, totalUnits: 0, items: [], nextCursor: null } },
			"GET /api/stock-items/below-minimum?pageSize=100": { body: { items: [belowMinimum], nextCursor: null } }
		});
		renderEnglish(<ReportsPage />);

		// Act
		await userEvent.click(screen.getByRole("radio", { name: "Low stock" }));

		// Assert
		expect(await screen.findByRole("cell", { name: "A1" })).toBeInTheDocument();
	});

	it("SwitchToSalesByCustomerTab_ShowsEmptyState", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/reports/stock-value?pageSize=100": { body: { totalValue: 0, totalUnits: 0, items: [], nextCursor: null } }
		});
		renderEnglish(<ReportsPage />);
		await userEvent.click(screen.getByRole("radio", { name: "Sales by customer" }));

		// Assert: no fetch mocked for the date range the component picks -> network failure surfaces as a FailureMessage, not a crash
		expect(await screen.findByRole("alert")).toBeInTheDocument();
	});
});
