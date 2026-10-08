import { send } from "./client";
import type { InvoiceStatus } from "./invoices";
import type { PaymentMethod } from "./payables";

export type PaymentLinkStatus = "Pending" | "Paid" | "Expired" | "Cancelled" | "Failed";

export type PaymentLink = { externalId: string; qrUrl: string; amount: number; status: PaymentLinkStatus; createdAt: string; expiresAt: string };

export type Payment = { date: string; amount: number; method: PaymentMethod };

export type InvoicePaymentsResult = { items: Payment[]; amountPaid: number; amountDue: number; status: InvoiceStatus };

export type NewPayment = { amount: number; method: PaymentMethod; date?: string };

export const paymentsApi = {
	getPaymentLink: (number: string, signal?: AbortSignal) => send<PaymentLink>(`/invoices/${encodeURIComponent(number)}/payment-link`, { signal }),
	createPaymentLink: (number: string) => send<PaymentLink>(`/invoices/${encodeURIComponent(number)}/payment-link`, { method: "POST" }),
	listPayments: (number: string, signal?: AbortSignal) => send<InvoicePaymentsResult>(`/invoices/${encodeURIComponent(number)}/payments`, { signal }),
	createPayment: (number: string, payment: NewPayment) => send<Payment>(`/invoices/${encodeURIComponent(number)}/payments`, { method: "POST", body: JSON.stringify(payment) })
};
