import { send, type Result } from "./client";

export type StockValueLine = { code: string; name: string; manufacturer: string; amount: number; price: number; value: number };

export type StockValueReport = { totalValue: number; totalUnits: number; items: StockValueLine[] };

export type SalesByPeriodRow = { date: string; invoiceCount: number; total: number; tax: number };

export type SalesByPeriodReport = { items: SalesByPeriodRow[] };

export type SalesByCustomerRow = { customerId: number; customerName: string; invoiceCount: number; total: number };

export type SalesByCustomerResult = { items: SalesByCustomerRow[]; nextCursor: string | null };

export type PeriodFilter = { from: string; to: string };

export const reportsApi = {
	// Totals ride on every page's response; later pages' lines are appended to the first page's totals.
	getStockValue: (signal?: AbortSignal) => getAllStockValue(signal),
	getSalesByPeriod: (filter: PeriodFilter, signal?: AbortSignal) => send<SalesByPeriodReport>(`/reports/sales-by-period?${periodQuery(filter)}`, { signal }),
	getSalesByCustomer: (filter: PeriodFilter & { cursor?: string; pageSize: number }, signal?: AbortSignal) =>
		send<SalesByCustomerResult>(`/reports/sales-by-customer?${periodQuery(filter)}${filter.cursor ? `&cursor=${encodeURIComponent(filter.cursor)}` : ""}&pageSize=${filter.pageSize}`, { signal })
};

async function getAllStockValue(signal?: AbortSignal, pageSize = 100): Promise<Result<StockValueReport>>
{
	let cursor: string | undefined;
	let totalValue = 0;
	let totalUnits = 0;
	const items: StockValueLine[] = [];

	for (; ;)
	{
		const query = cursor ? `cursor=${encodeURIComponent(cursor)}&pageSize=${pageSize}` : `pageSize=${pageSize}`;
		const result = await send<StockValueReport & { nextCursor: string | null }>(`/reports/stock-value?${query}`, { signal });
		if (!result.ok) return result;

		totalValue = result.value.totalValue;
		totalUnits = result.value.totalUnits;
		items.push(...result.value.items);
		if (!result.value.nextCursor) return { ok: true, value: { totalValue, totalUnits, items } };
		cursor = result.value.nextCursor;
	}
}

function periodQuery(filter: PeriodFilter): string
{
	return `from=${encodeURIComponent(filter.from)}&to=${encodeURIComponent(filter.to)}`;
}
