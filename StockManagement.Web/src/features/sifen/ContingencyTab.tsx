import { useState, type FormEvent } from "react";
import { api, type ApiFailure } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";

export function ContingencyTab()
{
	const { t } = useI18n();
	const { data: status, setData, failure: loadFailure } = useLoad(api.getContingencyStatus);
	const [rangeStart, setRangeStart] = useState("");
	const [rangeEnd, setRangeEnd] = useState("");
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onToggleActive()
	{
		if (!status) return;
		setBusy(true);
		const result = await api.setContingencyMode(!status.isActive);
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);
		setFailure(undefined);
		setData(result.value);
	}

	async function onSaveRange(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.setContingencyRange(Number(rangeStart), Number(rangeEnd));
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);
		setFailure(undefined);
		setData(result.value);
		setRangeStart("");
		setRangeEnd("");
	}

	if (!status) return <FailureMessage failure={loadFailure} />;

	return (
		<div className="panel">
			<div className="form-grid">
				<label>{t("contingencyRangeConfigured")}<span>{status.rangeConfigured ? t("yes") : t("no")}</span></label>
				<label>{t("rangeStart")}<span>{status.rangeStart}</span></label>
				<label>{t("rangeEnd")}<span>{status.rangeEnd}</span></label>
				<label>{t("contingencyNextNumber")}<span>{status.nextNumber}</span></label>
				<label>{t("contingencyRemaining")}<span>{status.remaining}</span></label>
			</div>
			<FailureMessage failure={failure} />
			<div className="form-actions">
				<button type="button" disabled={busy || !status.rangeConfigured} onClick={onToggleActive}>
					{t(status.isActive ? "disableContingencyMode" : "enableContingencyMode")}
				</button>
			</div>
			<form onSubmit={onSaveRange} className="form-grid">
				<label>{t("rangeStart")}<input type="number" min={1} value={rangeStart} onChange={event => setRangeStart(event.target.value)} required /></label>
				<label>{t("rangeEnd")}<input type="number" min={1} value={rangeEnd} onChange={event => setRangeEnd(event.target.value)} required /></label>
				<div className="form-actions">
					<button type="submit" disabled={busy}>{t("saveRange")}</button>
				</div>
			</form>
		</div>
	);
}
