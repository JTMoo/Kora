import { authHeaders, send } from "./client";
import type { SaleCondition } from "./sales";

export type InvoiceLine = { code: string; name: string; amount: number; unitPrice: number };

export type InvoiceStatus = "Open" | "PartiallyPaid" | "Paid" | "Overdue" | "Cancelled";

export type Invoice = { number: string; date: string; expirationDate: string; total: number; tax: number; amountPaid: number; amountDue: number; status: InvoiceStatus; saleCondition: SaleCondition; customerId: number; customerName: string; isCancelled: boolean; lines: InvoiceLine[] };

export type CreditNote = { number: number; date: string; reason: string; total: number; tax: number; invoiceNumber: string };

export type InvoiceListResult = { items: Invoice[]; nextCursor: string | null };

export type InvoiceFilter = { customerId?: number; from?: string; to?: string; cursor?: string; pageSize: number };

export const invoicesApi = {
	getInvoice: (number: string, signal?: AbortSignal) => send<Invoice>(`/invoices/${number}`, { signal }),
	listInvoices: (filter: InvoiceFilter, signal?: AbortSignal) => send<InvoiceListResult>(`/invoices?${invoiceFilterQuery(filter)}`, { signal }),
	cancelInvoice: (number: string, reason: string) => send<CreditNote>(`/invoices/${number}/cancel`, { method: "POST", body: JSON.stringify({ number, reason }) }),
	downloadIvaBook: (from: string, to: string) => fetch(`/api/invoices/iva-book?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`, { headers: authHeaders() })
};

function invoiceFilterQuery(filter: InvoiceFilter): string
{
	const params = new URLSearchParams({ pageSize: String(filter.pageSize) });
	if (filter.customerId !== undefined) params.set("customerId", String(filter.customerId));
	if (filter.from) params.set("from", filter.from);
	if (filter.to) params.set("to", filter.to);
	if (filter.cursor) params.set("cursor", filter.cursor);
	return params.toString();
}
