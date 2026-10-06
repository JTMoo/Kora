import { Banknote, Printer, Settings } from "lucide-react";
import type { NavRoute } from "../../routes";
import { CompanySettingsPage } from "./CompanySettingsPage";
import { PrinterSettingsPage } from "./PrinterSettingsPage";
import { SettingsPage } from "./SettingsPage";

export const companySettingsRoute: NavRoute = {
	name: "companySettings",
	icon: Banknote,
	render: () => <CompanySettingsPage />
};

export const printerSettingsRoute: NavRoute = {
	name: "printerSettings",
	icon: Printer,
	render: () => <PrinterSettingsPage />
};

export const settingsRoute: NavRoute = {
	name: "settings",
	icon: Settings,
	render: () => <SettingsPage />
};
