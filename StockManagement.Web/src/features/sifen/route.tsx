import { Radio } from "lucide-react";
import type { NavRoute } from "../../routes";
import { SifenOperationsPage } from "./SifenOperationsPage";

export const sifenOperationsRoute: NavRoute = {
	name: "sifenOperations",
	icon: Radio,
	permission: "Sales.Read",
	render: () => <SifenOperationsPage />
};
