import { FileSpreadsheet } from "lucide-react";
import type { NavRoute } from "../../routes";
import { MarangatuExportPage } from "./MarangatuExportPage";

export const marangatuExportRoute: NavRoute = {
	name: "marangatuExport",
	icon: FileSpreadsheet,
	permission: "Sales.Read",
	render: () => <MarangatuExportPage />
};
