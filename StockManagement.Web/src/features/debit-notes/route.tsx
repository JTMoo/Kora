import { ReceiptText } from "lucide-react";
import type { NavRoute } from "../../routes";
import { DebitNoteBrowser } from "./DebitNoteBrowser";

export const debitNotesRoute: NavRoute = {
	name: "debitNotes",
	icon: ReceiptText,
	permission: "Sales.Read",
	render: () => <DebitNoteBrowser />
};
