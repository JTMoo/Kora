import { BarChart3 } from "lucide-react";
import type { NavRoute } from "../../routes";
import { ReportsPage } from "./ReportsPage";

export const reportsRoute: NavRoute = {
	name: "reports",
	icon: BarChart3,
	permission: "Reports.Read",
	render: () => <ReportsPage />
};
