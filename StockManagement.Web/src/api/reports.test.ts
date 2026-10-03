import { describe, expect, it } from "vitest";
import { mockApi } from "../test-utils";
import { reportsApi } from "./reports";

const line = { code: "A1", name: "Screw", manufacturer: "None", amount: 10, price: 5000, value: 50000 };

describe("reportsApi", () =>
{
	it("getStockValue_OnePage_ReturnsTotalsAndItems", async () =>
	{
		// Arrange
		mockApi({ "GET /api/reports/stock-value?pageSize=100": { body: { totalValue: 50000, totalUnits: 10, items: [line], nextCursor: null } } });

		// Act
		const result = await reportsApi.getStockValue();

		// Assert
		expect(result).toEqual({ ok: true, value: { totalValue: 50000, totalUnits: 10, items: [line] } });
	});

	it("getStockValue_TwoPages_ConcatenatesItemsAndKeepsFirstPageTotals", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/reports/stock-value?pageSize=100": { body: { totalValue: 50000, totalUnits: 10, items: [line], nextCursor: "c1" } },
			"GET /api/reports/stock-value?cursor=c1&pageSize=100": { body: { totalValue: 50000, totalUnits: 10, items: [{ ...line, code: "B2" }], nextCursor: null } }
		});

		// Act
		const result = await reportsApi.getStockValue();

		// Assert
		expect(result.ok && result.value.items.map(item => item.code)).toEqual(["A1", "B2"]);
	});

	it("getStockValue_NetworkDown_ReturnsUnexpected", async () =>
	{
		// Arrange
		mockApi({});

		// Act
		const result = await reportsApi.getStockValue();

		// Assert
		expect(result).toEqual({ ok: false, failure: { kind: "unexpected" } });
	});

	it("getSalesByPeriod_Ok_ReturnsRows", async () =>
	{
		// Arrange
		mockApi({ "GET /api/reports/sales-by-period?from=2026-09-01&to=2026-09-30": { body: { items: [{ date: "2026-09-15", invoiceCount: 2, total: 10000, tax: 909 }] } } });

		// Act
		const result = await reportsApi.getSalesByPeriod({ from: "2026-09-01", to: "2026-09-30" });

		// Assert
		expect(result).toEqual({ ok: true, value: { items: [{ date: "2026-09-15", invoiceCount: 2, total: 10000, tax: 909 }] } });
	});

	it("getSalesByPeriod_FromAfterTo_ReturnsInvalid", async () =>
	{
		// Arrange
		mockApi({ "GET /api/reports/sales-by-period?from=2026-09-30&to=2026-09-01": { status: 400, body: { errors: [] } } });

		// Act
		const result = await reportsApi.getSalesByPeriod({ from: "2026-09-30", to: "2026-09-01" });

		// Assert
		expect(result).toEqual({ ok: false, failure: { kind: "invalid", codes: [] } });
	});

	it("getSalesByCustomer_WithCursor_IncludesCursorAndPageSize", async () =>
	{
		// Arrange
		mockApi({ "GET /api/reports/sales-by-customer?from=2026-09-01&to=2026-09-30&cursor=c1&pageSize=20": { body: { items: [], nextCursor: null } } });

		// Act
		const result = await reportsApi.getSalesByCustomer({ from: "2026-09-01", to: "2026-09-30", cursor: "c1", pageSize: 20 });

		// Assert
		expect(result).toEqual({ ok: true, value: { items: [], nextCursor: null } });
	});
});
