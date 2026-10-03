import { FileText } from "lucide-react";
import type { NavRoute } from "../../routes";
import { RemissionNoteBrowser } from "./RemissionNoteBrowser";

export const remissionNotesRoute: NavRoute = {
	name: "remissionNotes",
	icon: FileText,
	permission: "Sales.Read",
	render: () => <RemissionNoteBrowser />
};
