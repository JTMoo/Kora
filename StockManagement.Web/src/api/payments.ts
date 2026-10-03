import { send } from "./client";

export type PaymentLinkStatus = "Pending" | "Paid" | "Expired" | "Cancelled" | "Failed";

export type PaymentLink = { externalId: string; qrUrl: string; amount: number; status: PaymentLinkStatus; createdAt: string; expiresAt: string };

export const paymentsApi = {
	getPaymentLink: (number: string, signal?: AbortSignal) => send<PaymentLink>(`/invoices/${encodeURIComponent(number)}/payment-link`, { signal }),
	createPaymentLink: (number: string) => send<PaymentLink>(`/invoices/${encodeURIComponent(number)}/payment-link`, { method: "POST" })
};
