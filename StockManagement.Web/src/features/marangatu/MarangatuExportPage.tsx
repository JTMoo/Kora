import { useState } from "react";
import { api, type ApiFailure } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";

function firstOfMonth(): string
{
	const now = new Date();
	return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
}

function today(): string
{
	return new Date().toISOString().slice(0, 10);
}

export function MarangatuExportPage()
{
	const { t } = useI18n();
	const [from, setFrom] = useState(firstOfMonth());
	const [to, setTo] = useState(today());
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onDownload()
	{
		setBusy(true);
		const response = await api.downloadIvaBook(from, to);
		setBusy(false);
		if (!response.ok) return setFailure({ kind: "unexpected" });

		const url = URL.createObjectURL(await response.blob());
		const link = document.createElement("a");
		link.href = url;
		link.download = `iva-book-${from}-${to}.csv`;
		link.click();
		URL.revokeObjectURL(url);
	}

	return (
		<Page title={t("marangatuExport")}>
			<form className="panel" onSubmit={event => event.preventDefault()}>
				<div className="form-grid">
					<label>{t("from")}<input type="date" value={from} onChange={event => setFrom(event.target.value)} /></label>
					<label>{t("to")}<input type="date" value={to} onChange={event => setTo(event.target.value)} /></label>
				</div>
				<FailureMessage failure={failure} />
				<div className="form-actions">
					<button type="button" disabled={busy || from > to} onClick={onDownload}>{t("downloadIvaBook")}</button>
				</div>
			</form>
		</Page>
	);
}
