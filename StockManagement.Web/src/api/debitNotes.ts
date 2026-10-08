import { send, type Result } from "./client";
import type { TransmissionStatus } from "./remissionNotes";

export type DebitNoteLine = { description: string; amount: number; vatRatePercent: number };

export type DebitNote = { number: string; date: string; reason: string; invoiceNumber: string; total: number; tax: number; cdc: string; transmissionStatus: TransmissionStatus; lines: DebitNoteLine[] };

export type NewDebitNoteItem = { description: string; amount: number; vatRatePercent: number };

export type NewDebitNote = { invoiceNumber: string; reason: string; items: NewDebitNoteItem[] };

export const debitNotesApi = {
	listDebitNotes: (signal?: AbortSignal) => unwrapItems(send<{ items: DebitNote[] }>("/debit-notes", { signal })),
	getDebitNote: (number: string, signal?: AbortSignal) => send<DebitNote>(`/debit-notes/${encodeURIComponent(number)}`, { signal }),
	createDebitNote: (debitNote: NewDebitNote) => send<DebitNote>("/debit-notes", { method: "POST", body: JSON.stringify(debitNote) })
};

async function unwrapItems<T>(result: Promise<Result<{ items: T[] }>>): Promise<Result<T[]>>
{
	const response = await result;
	return response.ok ? { ok: true, value: response.value.items } : response;
}
