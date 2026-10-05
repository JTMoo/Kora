import { send } from "./client";

export type CashRegisterSessionStatus = "Open" | "Closed";

export type CashMovementType = "In" | "Out";

export type CashMovement = { type: CashMovementType; amount: number; reason: string; date: string; createdByUserId: string };

export type CashRegisterSession = {
	id: string;
	openedAt: string;
	openedByUserId: string;
	openingFloat: number;
	closedAt: string | null;
	closedByUserId: string | null;
	countedAmount: number | null;
	note: string;
	status: CashRegisterSessionStatus;
	expectedAmount: number;
	movements: CashMovement[];
};

export type NewCashMovement = { type: CashMovementType; amount: number; reason: string };

export type CashRegisterCloseReport = { session: CashRegisterSession; expectedAmount: number; countedAmount: number; difference: number };

export type CashRegisterSessionFilter = { cursor?: string; pageSize: number };

export const cashRegisterApi = {
	getCurrentCashRegisterSession: (signal?: AbortSignal) => send<CashRegisterSession>("/cash-register/sessions/current", { signal }),
	getCashRegisterSession: (id: string, signal?: AbortSignal) => send<CashRegisterSession>(`/cash-register/sessions/${encodeURIComponent(id)}`, { signal }),
	listCashRegisterSessions: (filter: CashRegisterSessionFilter, signal?: AbortSignal) => send<{ items: CashRegisterSession[]; nextCursor: string | null }>(`/cash-register/sessions?${cashRegisterSessionFilterQuery(filter)}`, { signal }),
	openCashRegisterSession: (openingFloat: number) => send<CashRegisterSession>("/cash-register/sessions", { method: "POST", body: JSON.stringify({ openingFloat }) }),
	addCashMovement: (sessionId: string, movement: NewCashMovement) => send<CashMovement>(`/cash-register/sessions/${encodeURIComponent(sessionId)}/movements`, { method: "POST", body: JSON.stringify(movement) }),
	closeCashRegisterSession: (sessionId: string, countedAmount: number, note: string) => send<CashRegisterCloseReport>(`/cash-register/sessions/${encodeURIComponent(sessionId)}/close`, { method: "POST", body: JSON.stringify({ countedAmount, note }) })
};

function cashRegisterSessionFilterQuery(filter: CashRegisterSessionFilter): string
{
	const params = new URLSearchParams({ pageSize: String(filter.pageSize) });
	if (filter.cursor) params.set("cursor", filter.cursor);
	return params.toString();
}
