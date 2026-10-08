import { send } from "./client";
import type { CreditNote } from "./invoices";

export const creditNotesApi = {
	getCreditNote: (number: number, signal?: AbortSignal) => send<CreditNote>(`/credit-notes/${number}`, { signal })
};
