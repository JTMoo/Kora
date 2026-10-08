import { FileSpreadsheet } from "lucide-react";
import type { NavRoute } from "../../routes";
import { PurchaseIvaBookExportPage } from "./PurchaseIvaBookExportPage";

export const purchaseIvaBookExportRoute: NavRoute = {
	name: "purchaseIvaBookExport",
	icon: FileSpreadsheet,
	permission: "GoodsImports.Read",
	render: () => <PurchaseIvaBookExportPage />
};
