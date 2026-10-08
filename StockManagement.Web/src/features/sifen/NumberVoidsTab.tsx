import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type NumberVoidItem } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { transmissionStatusBadge } from "./transmissionStatusBadge";

export function NumberVoidsTab()
{
	const { t, formatDate } = useI18n();
	const { data, setData, failure: loadFailure } = useLoad(api.listNumberVoids);
	const items = data?.items ?? [];
	const [rangeStart, setRangeStart] = useState("");
	const [rangeEnd, setRangeEnd] = useState("");
	const [reason, setReason] = useState("");
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.requestNumberVoid(Number(rangeStart), Number(rangeEnd), reason);
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		setData({ items: [result.value, ...items] });
		setRangeStart("");
		setRangeEnd("");
		setReason("");
	}

	return (
		<div className="panel">
			<form onSubmit={onSubmit} className="form-grid">
				<label>{t("rangeStart")}<input type="number" min={1} value={rangeStart} onChange={event => setRangeStart(event.target.value)} required /></label>
				<label>{t("rangeEnd")}<input type="number" min={1} value={rangeEnd} onChange={event => setRangeEnd(event.target.value)} required /></label>
				<label>{t("reason")}<input value={reason} maxLength={150} onChange={event => setReason(event.target.value)} required /></label>
				<div className="form-actions">
					<button type="submit" disabled={busy}>{t("requestNumberVoid")}</button>
				</div>
			</form>
			<FailureMessage failure={failure} />
			<FailureMessage failure={loadFailure} />
			<table>
				<thead>
					<tr><th className="number">{t("rangeStart")}</th><th className="number">{t("rangeEnd")}</th><th>{t("reason")}</th><th>{t("requestedAt")}</th><th>{t("status")}</th></tr>
				</thead>
				<tbody>
					{items.map((item: NumberVoidItem) => (
						<tr key={`${item.rangeStart}-${item.rangeEnd}`}>
							<td className="number">{item.rangeStart}</td>
							<td className="number">{item.rangeEnd}</td>
							<td>{item.reason}</td>
							<td>{formatDate(item.requestedAt)}</td>
							<td>{transmissionStatusBadge(item.transmissionStatus, t)}</td>
						</tr>
					))}
				</tbody>
			</table>
		</div>
	);
}
