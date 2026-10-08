import { send } from "./client";

export type PaymentMethod = "Cash" | "BankTransfer" | "Check" | "BancardQr" | "Other";

export type SupplierInvoiceStatus = "Open" | "PartiallyPaid" | "Paid" | "Overdue";

export type SupplierInvoiceLine = { code: string; name: string; amount: number; unitPrice: number };

export type SupplierInvoice = { number: string; date: string; expirationDate: string; total: number; supplierId: string; supplierName: string; amountPaid: number; amountDue: number; status: SupplierInvoiceStatus; items: SupplierInvoiceLine[] };

export type NewSupplierInvoice = { number: string; supplierId: string; date: string; expirationDate: string; total: number; items?: { code: string; amount: number; unitPrice: number }[] };

export type SupplierInvoiceFilter = { supplierId?: string; cursor?: string; pageSize: number };

export type SupplierPayment = { date: string; amount: number; method: PaymentMethod };

export type SupplierInvoicePaymentsResult = { items: SupplierPayment[]; amountPaid: number; amountDue: number; status: SupplierInvoiceStatus };

export type NewSupplierPayment = { amount: number; method: PaymentMethod; date?: string };

export const payablesApi = {
	getSupplierInvoice: (number: string, signal?: AbortSignal) => send<SupplierInvoice>(`/supplier-invoices/${encodeURIComponent(number)}`, { signal }),
	listSupplierInvoices: (filter: SupplierInvoiceFilter, signal?: AbortSignal) => send<{ items: SupplierInvoice[]; nextCursor: string | null }>(`/supplier-invoices?${supplierInvoiceFilterQuery(filter)}`, { signal }),
	createSupplierInvoice: (invoice: NewSupplierInvoice) => send<SupplierInvoice>("/supplier-invoices", { method: "POST", body: JSON.stringify(invoice) }),
	listSupplierPayments: (number: string, signal?: AbortSignal) => send<SupplierInvoicePaymentsResult>(`/supplier-invoices/${encodeURIComponent(number)}/payments`, { signal }),
	createSupplierPayment: (number: string, payment: NewSupplierPayment) => send<SupplierPayment>(`/supplier-invoices/${encodeURIComponent(number)}/payments`, { method: "POST", body: JSON.stringify(payment) })
};

function supplierInvoiceFilterQuery(filter: SupplierInvoiceFilter): string
{
	const params = new URLSearchParams({ pageSize: String(filter.pageSize) });
	if (filter.supplierId) params.set("supplierId", filter.supplierId);
	if (filter.cursor) params.set("cursor", filter.cursor);
	return params.toString();
}
