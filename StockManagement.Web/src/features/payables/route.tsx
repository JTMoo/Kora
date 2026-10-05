import { Receipt } from "lucide-react";
import type { NavRoute } from "../../routes";
import { SupplierInvoiceList } from "./SupplierInvoiceList";

export const payablesRoute: NavRoute = {
	name: "payables",
	icon: Receipt,
	permission: "Payables.Read",
	render: () => <SupplierInvoiceList />
};
