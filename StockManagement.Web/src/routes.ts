import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import type { Invoice, Permission } from "./api";

// Kernel: each feature's route.tsx depends on this; this depends on nothing feature-specific.
// Adding a view means adding one route.tsx (icon, permission, render) and wiring it into App.tsx's `routes` array.

export type View = "stockItems" | "clients" | "suppliers" | "newSale" | "invoices" | "remissionNotes" | "creditNotes" | "debitNotes" | "goodsImports" | "marangatuExport" | "payables" | "cashRegister" | "reports" | "companySettings" | "printerSettings" | "settings" | "users" | "licensing";

export type NavContext = { invoice?: Invoice; onSold: (invoice: Invoice) => void };

export type NavRoute = { name: View; icon: LucideIcon; permission?: Permission; render: (ctx: NavContext) => ReactNode };
