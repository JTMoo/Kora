import { send } from "./client";

export type SifenTransmissionStatus = "Pending" | "Sent" | "Accepted" | "Rejected" | "Error" | "Cancelled";

export type ContingencyStatus = { rangeConfigured: boolean; isActive: boolean; rangeStart: number; rangeEnd: number; nextNumber: number; remaining: number };

export type CancellationRequestItem = { invoiceNumber: string; reason: string; requestedAt: string; transmissionStatus: SifenTransmissionStatus };

export type NumberVoidItem = { rangeStart: number; rangeEnd: number; reason: string; requestedAt: string; transmissionStatus: SifenTransmissionStatus };

export type StuckTransmission = { number: string; date: string; customerName: string; total: number; transmissionStatus: SifenTransmissionStatus; cdc: string };

export const sifenApi = {
	getContingencyStatus: (signal?: AbortSignal) => send<ContingencyStatus>("/sifen/contingency-mode", { signal }),
	setContingencyMode: (active: boolean) => send<ContingencyStatus>("/sifen/contingency-mode", { method: "POST", body: JSON.stringify({ active }) }),
	setContingencyRange: (rangeStart: number, rangeEnd: number) => send<ContingencyStatus>("/sifen/contingency-range", { method: "PUT", body: JSON.stringify({ rangeStart, rangeEnd }) }),
	listCancellations: (signal?: AbortSignal) => send<{ items: CancellationRequestItem[] }>("/sifen/cancellations", { signal }),
	requestCancellation: (invoiceNumber: string, reason: string) => send<CancellationRequestItem>("/sifen/cancellations", { method: "POST", body: JSON.stringify({ invoiceNumber, reason }) }),
	listNumberVoids: (signal?: AbortSignal) => send<{ items: NumberVoidItem[] }>("/sifen/number-voids", { signal }),
	requestNumberVoid: (rangeStart: number, rangeEnd: number, reason: string) => send<NumberVoidItem>("/sifen/number-voids", { method: "POST", body: JSON.stringify({ rangeStart, rangeEnd, reason }) }),
	listStuckTransmissions: (signal?: AbortSignal) => send<{ items: StuckTransmission[] }>("/sifen/stuck-transmissions", { signal })
};
