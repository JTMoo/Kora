import { send } from "./client";

export type KudeFormat = "Ticket" | "A4";

export type PrinterSettings = { defaultPrinterName: string; receiptPaperWidthMm: number; kudeFormat: KudeFormat; printOnSaleComplete: boolean };

export const printerSettingsApi = {
	getPrinterSettings: (signal?: AbortSignal) => send<PrinterSettings>("/printer-settings", { signal }),
	updatePrinterSettings: (settings: PrinterSettings) => send<PrinterSettings>("/printer-settings", { method: "PUT", body: JSON.stringify(settings) })
};
