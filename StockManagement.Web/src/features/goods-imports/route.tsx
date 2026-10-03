import { Ship } from "lucide-react";
import type { NavRoute } from "../../routes";
import { GoodsImportDocumentBrowser } from "./GoodsImportDocumentBrowser";

export const goodsImportsRoute: NavRoute = {
	name: "goodsImports",
	icon: Ship,
	permission: "GoodsImports.Read",
	render: () => <GoodsImportDocumentBrowser />
};
