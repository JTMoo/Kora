import { render } from "@testing-library/react";
import type { ReactElement } from "react";
import { vi } from "vitest";
import type { Permission, UserRole } from "./api";
import { AuthProvider } from "./auth";
import { I18nProvider } from "./i18n";
import { ToastProvider } from "./Toast";

export const allPermissions: Permission[] = ["Users.Manage", "Customers.Read", "Customers.Write", "StockItems.Read", "StockItems.Write", "Sales.Read", "Sales.Write", "Settings.Read", "Settings.Write", "Suppliers.Read", "Suppliers.Write", "Reports.Read"];

export type Route = { status?: number; body?: unknown };

/** Stubs fetch: key is "METHOD /api/path"; returns the fetch mock to inspect calls. */
export function mockApi(routes: Record<string, Route>)
{
	const fetchMock = vi.fn(async (url: string, init?: RequestInit) =>
	{
		const route = routes[`${init?.method ?? "GET"} ${url}`];
		if (!route) throw new TypeError("Failed to fetch");
		const status = route.status ?? 200;
		// 204 (and other null-body statuses) must not carry a body
		return status === 204 ? new Response(null, { status }) : new Response(JSON.stringify(route.body ?? null), { status });
	});
	vi.stubGlobal("fetch", fetchMock);
	return fetchMock;
}

export function sentBody(fetchMock: ReturnType<typeof mockApi>, key: string): unknown
{
	const call = fetchMock.mock.calls.find(([url, init]) => `${init?.method ?? "GET"} ${url}` === key);
	return call ? JSON.parse(call[1]!.body as string) : undefined;
}

/** Renders as a logged-in user (seeds `localStorage` before mount); pass `authenticated: false` for the login screen. */
export function renderEnglish(ui: ReactElement, { authenticated = true, role = "Admin", permissions = allPermissions }: { authenticated?: boolean; role?: UserRole; permissions?: Permission[] } = {})
{
	if (authenticated)
	{
		localStorage.setItem("auth.username", "admin");
		localStorage.setItem("auth.role", role);
		localStorage.setItem("auth.permissions", JSON.stringify(permissions));
	}

	return render(<I18nProvider culture="en-US"><AuthProvider><ToastProvider>{ui}</ToastProvider></AuthProvider></I18nProvider>);
}

export const screw = { id: "1", code: "A1", name: "Screw", description: "M6", location: "A-1", amount: 10, price: 5000, manufacturer: "None", factor: 0, purchasePrice: 0, purchaseExchangeRate: 0, additionalPurchaseCost: 0, minimumStock: 0, barcode: "" };
export const nut = { id: "2", code: "B2", name: "Nut", description: "M6", location: "B-2", amount: 0, price: 1000, manufacturer: "None", factor: 0, purchasePrice: 0, purchaseExchangeRate: 0, additionalPurchaseCost: 0, minimumStock: 0, barcode: "" };
export const ana = { customerId: 1001, name: "Ana", lastname: "Gómez", address: "", phoneNumber: "", identificationNumber: "", postboxNumber: "", email: "", miscellaneous: "" };
export const acme = { id: "1", name: "Acme", contactName: "Joe", country: "PY", currency: "PYG", leadTimeDays: 5, miscellaneous: "" };
export const invoice = { number: "001-001-0000007", date: "2026-09-24T10:00:00", expirationDate: "2026-10-24T10:00:00", total: 10000, tax: 909, amountPaid: 10000, amountDue: 0, status: "Paid", saleCondition: "Cash", customerId: 1001, customerName: "Ana Gómez", isCancelled: false, lines: [{ code: "A1", name: "Screw", amount: 2, unitPrice: 5000 }] };
