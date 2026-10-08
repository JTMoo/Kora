import { Receipt } from "lucide-react";
import type { NavRoute } from "../../routes";
import { CreditNoteView } from "./CreditNoteView";

export const creditNotesRoute: NavRoute = {
	name: "creditNotes",
	icon: Receipt,
	permission: "Sales.Read",
	render: () => <CreditNoteView />
};
