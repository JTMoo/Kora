import { Banknote, DatabaseBackup, Printer, Settings } from "lucide-react";
import type { NavRoute } from "../../routes";
import { BackupSettingsPage } from "./BackupSettingsPage";
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

export const backupsRoute: NavRoute = {
	name: "backups",
	icon: DatabaseBackup,
	permission: "Backup.Manage",
	render: () => <BackupSettingsPage />
};

export const settingsRoute: NavRoute = {
	name: "settings",
	icon: Settings,
	render: () => <SettingsPage />
};
