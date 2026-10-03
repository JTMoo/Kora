// Barrel: combines the per-domain api slices below into the single `api` object most components use.
// Adding an endpoint touches only its own domain file; this file changes only when a whole new domain is added.

export * from "./client";
export * from "./stockItems";
export * from "./customers";
export * from "./suppliers";
export * from "./sales";
export * from "./invoices";
export * from "./settings";
export * from "./companySettings";
export * from "./users";
export * from "./auth";
export * from "./import";
export * from "./search";
export * from "./reports";

import { authApi } from "./auth";
import { companySettingsApi } from "./companySettings";
import { customersApi } from "./customers";
import { importApi } from "./import";
import { invoicesApi } from "./invoices";
import { reportsApi } from "./reports";
import { salesApi } from "./sales";
import { searchApi } from "./search";
import { settingsApi } from "./settings";
import { stockItemsApi } from "./stockItems";
import { suppliersApi } from "./suppliers";
import { usersApi } from "./users";

export const api = {
	...stockItemsApi,
	...customersApi,
	...suppliersApi,
	...salesApi,
	...invoicesApi,
	...settingsApi,
	...companySettingsApi,
	...usersApi,
	...authApi,
	...importApi,
	...searchApi,
	...reportsApi
};
