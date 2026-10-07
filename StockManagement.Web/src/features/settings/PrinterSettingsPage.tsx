import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type KudeFormat, type PrinterSettings } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";

function openTestPage(settings: PrinterSettings)
{
	const testWindow = window.open("", "_blank");
	if (!testWindow) return;

	const isTicket = settings.kudeFormat === "Ticket";
	testWindow.document.write(`
		<!doctype html>
		<html>
		<head>
			<meta charset="utf-8" />
			<title>Test page</title>
			<style>
				body { font-family: Arial, sans-serif; font-size: ${isTicket ? "10px" : "12px"}; margin: ${isTicket ? "4px" : "24px"}; width: ${isTicket ? `${settings.receiptPaperWidthMm}mm` : "auto"}; }
			</style>
		</head>
		<body>
			<p>${settings.defaultPrinterName || "(no printer name set)"}</p>
			<p>${settings.kudeFormat} - ${settings.receiptPaperWidthMm}mm</p>
			<p>0123456789</p>
		</body>
		</html>
	`);
	testWindow.document.close();
	testWindow.focus();
	testWindow.print();
}

export function PrinterSettingsPage()
{
	const { t } = useI18n();
	const { data: settings, setData, failure: loadFailure } = useLoad(api.getPrinterSettings);
	const [draft, setDraft] = useState<PrinterSettings>();
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	const current = draft ?? settings;

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		if (!current) return;

		setBusy(true);
		const result = await api.updatePrinterSettings(current);
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		setData(result.value);
		setDraft(undefined);
	}

	if (!current) return <Page title={t("printerSettings")}><FailureMessage failure={loadFailure} /></Page>;

	return (
		<Page title={t("printerSettings")}>
			<form onSubmit={onSubmit} className="panel">
				<div className="form-grid">
					<label>
						{t("defaultPrinterName")}
						<input value={current.defaultPrinterName} onChange={event => setDraft({ ...current, defaultPrinterName: event.target.value })} />
					</label>
					<label>
						{t("receiptPaperWidthMm")}
						<input type="number" min={20} max={300} step="1" value={current.receiptPaperWidthMm} onChange={event => setDraft({ ...current, receiptPaperWidthMm: Number(event.target.value) })} />
					</label>
					<label>
						{t("kudeFormat")}
						<select value={current.kudeFormat} onChange={event => setDraft({ ...current, kudeFormat: event.target.value as KudeFormat })}>
							<option value="Ticket">{t("ticket")}</option>
							<option value="A4">{t("a4")}</option>
						</select>
					</label>
					<label>
						<input type="checkbox" checked={current.printOnSaleComplete} onChange={event => setDraft({ ...current, printOnSaleComplete: event.target.checked })} />
						{t("printOnSaleComplete")}
					</label>
				</div>
				<FailureMessage failure={failure} />
				<div className="form-actions">
					<button type="submit" disabled={busy}>{t("save")}</button>
					<button type="button" onClick={() => openTestPage(current)}>{t("printTestPage")}</button>
				</div>
			</form>
		</Page>
	);
}
