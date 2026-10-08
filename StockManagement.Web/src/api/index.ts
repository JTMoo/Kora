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
export * from "./printerSettings";
export * from "./users";
export * from "./auth";
export * from "./import";
export * from "./search";
export * from "./reports";
export * from "./goodsImports";
export * from "./remissionNotes";
export * from "./payments";
export * from "./payables";
export * from "./creditNotes";
export * from "./debitNotes";
export * from "./cashRegister";
export * from "./license";
export * from "./feedback";

import { authApi } from "./auth";
import { companySettingsApi } from "./companySettings";
import { printerSettingsApi } from "./printerSettings";
import { creditNotesApi } from "./creditNotes";
import { customersApi } from "./customers";
import { debitNotesApi } from "./debitNotes";
import { feedbackApi } from "./feedback";
import { goodsImportApi } from "./goodsImports";
import { importApi } from "./import";
import { invoicesApi } from "./invoices";
import { licenseApi } from "./license";
import { paymentsApi } from "./payments";
import { payablesApi } from "./payables";
import { cashRegisterApi } from "./cashRegister";
import { remissionNotesApi } from "./remissionNotes";
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
	...printerSettingsApi,
	...usersApi,
	...authApi,
	...importApi,
	...searchApi,
	...reportsApi,
	...goodsImportApi,
	...remissionNotesApi,
	...paymentsApi,
	...payablesApi,
	...creditNotesApi,
	...debitNotesApi,
	...cashRegisterApi,
	...licenseApi,
	...feedbackApi
};
