// Shared HTTP client for StockManagement.Api (see ADR-0004). Expected failures are results, not exceptions.
// Kernel: domain api files (stockItems.ts, customers.ts, ...) depend on this; this depends on nothing else here.

export type ApiFailure =
	| { kind: "notFound" }
	| { kind: "invalid"; codes: string[] }
	| { kind: "conflict"; unavailableItems: string[] }
	| { kind: "duplicate"; code: string }
	| { kind: "insufficientStock"; inStock: number }
	| { kind: "invalidState"; reason: string }
	| { kind: "cannotDeleteSelf" }
	| { kind: "unauthorized" }
	| { kind: "unexpected" };

export type Result<T> = { ok: true; value: T } | { ok: false; failure: ApiFailure };

type ProblemDetails = { errors?: { reason: string }[] };

// Set by AuthProvider; kept out of React so the api client has no framework dependency
let authToken: string | null = null;
let onUnauthorized: (() => void) | null = null;

export function setAuthToken(token: string | null)
{
	authToken = token;
}

export function setUnauthorizedHandler(handler: (() => void) | null)
{
	onUnauthorized = handler;
}

export function authHeaders(): Record<string, string>
{
	return authToken ? { "Authorization": `Bearer ${authToken}` } : {};
}

export async function send<T>(path: string, init?: RequestInit): Promise<Result<T>>
{
	// Content-Type only with a body: FastEndpoints otherwise tries to parse the (empty) GET body as JSON and rejects it
	const headers = { ...authHeaders(), ...(init?.body ? { "Content-Type": "application/json" } : {}) };
	return handleResponse(() => fetch(`/api${path}`, { ...init, headers }));
}

// Cursor list endpoints (ADR-0029): follows NextCursor until exhausted, for views that still render one full list
export async function sendAllPages<T>(path: string, signal?: AbortSignal, pageSize = 100): Promise<Result<T[]>>
{
	const items: T[] = [];
	let cursor: string | undefined;

	for (; ;)
	{
		const query = cursor ? `cursor=${encodeURIComponent(cursor)}&pageSize=${pageSize}` : `pageSize=${pageSize}`;
		const separator = path.includes("?") ? "&" : "?";
		const result = await send<{ items: T[]; nextCursor: string | null }>(`${path}${separator}${query}`, { signal });
		if (!result.ok) return result;

		items.push(...result.value.items);
		if (!result.value.nextCursor) return { ok: true, value: items };
		cursor = result.value.nextCursor;
	}
}

export async function sendForm<T>(path: string, file: File, fields?: Record<string, string>): Promise<Result<T>>
{
	const body = new FormData();
	for (const [key, value] of Object.entries(fields ?? {})) body.append(key, value);
	body.append("File", file);
	return handleResponse(() => fetch(`/api${path}`, { method: "POST", body, headers: authHeaders() }));
}

async function handleResponse<T>(fetchCall: () => Promise<Response>): Promise<Result<T>>
{
	let response: Response;
	try
	{
		response = await fetchCall();
	}
	catch
	{
		return { ok: false, failure: { kind: "unexpected" } };
	}

	if (response.ok) return { ok: true, value: (response.status === 204 ? undefined : await response.json()) as T };
	if (response.status === 404) return { ok: false, failure: { kind: "notFound" } };
	if (response.status === 401)
	{
		onUnauthorized?.();
		return { ok: false, failure: { kind: "unauthorized" } };
	}
	if (response.status === 409)
	{
		const body = (await response.json()) as { unavailableItems?: string[]; code?: string; name?: string; barcode?: string; inStock?: number; reason?: string };
		if (body.unavailableItems) return { ok: false, failure: { kind: "conflict", unavailableItems: body.unavailableItems } };
		if (body.code) return { ok: false, failure: { kind: "duplicate", code: body.code } };
		if (body.name) return { ok: false, failure: { kind: "duplicate", code: body.name } };
		if (body.barcode) return { ok: false, failure: { kind: "duplicate", code: body.barcode } };
		if (body.inStock !== undefined) return { ok: false, failure: { kind: "insufficientStock", inStock: body.inStock } };
		if (body.reason !== undefined) return { ok: false, failure: { kind: "invalidState", reason: body.reason } };
		return { ok: false, failure: { kind: "cannotDeleteSelf" } };
	}
	if (response.status === 400) return { ok: false, failure: { kind: "invalid", codes: ((await response.json()) as ProblemDetails).errors?.map(error => error.reason) ?? [] } };
	return { ok: false, failure: { kind: "unexpected" } };
}
