import { send, type Result } from "./client";

export type RemissionReason = "Venta" | "Consignacion" | "Reposicion" | "TrasladoEntreLocales" | "Otro";

export type TransmissionStatus = "Pending" | "Sent" | "Accepted" | "Rejected" | "Error";

export type RemissionNoteLine = { code: string; name: string; amount: number };

export type RemissionNote = { number: string; date: string; reason: RemissionReason; destinationAddress: string; customerId: number; customerName: string; cdc: string; transmissionStatus: TransmissionStatus; lines: RemissionNoteLine[] };

export type NewRemissionNote = { customerId: number; reason: RemissionReason; destinationAddress: string; items: { code: string; amount: number }[] };

export const remissionNotesApi = {
	listRemissionNotes: (signal?: AbortSignal) => unwrapItems(send<{ items: RemissionNote[] }>("/remission-notes", { signal })),
	getRemissionNote: (number: string, signal?: AbortSignal) => send<RemissionNote>(`/remission-notes/${encodeURIComponent(number)}`, { signal }),
	createRemissionNote: (remissionNote: NewRemissionNote) => send<RemissionNote>("/remission-notes", { method: "POST", body: JSON.stringify(remissionNote) })
};

async function unwrapItems<T>(result: Promise<Result<{ items: T[] }>>): Promise<Result<T[]>>
{
	const response = await result;
	return response.ok ? { ok: true, value: response.value.items } : response;
}
