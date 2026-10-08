import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type CancellationRequestItem } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { transmissionStatusBadge } from "./transmissionStatusBadge";

export function CancellationsTab()
{
	const { t, formatDate } = useI18n();
	const { data, setData, failure: loadFailure } = useLoad(api.listCancellations);
	const items = data?.items ?? [];
	const [invoiceNumber, setInvoiceNumber] = useState("");
	const [reason, setReason] = useState("");
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.requestCancellation(invoiceNumber, reason);
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		setData({ items: [result.value, ...items] });
		setInvoiceNumber("");
		setReason("");
	}

	return (
		<div className="panel">
			<form onSubmit={onSubmit} className="form-grid">
				<label>{t("invoiceNumber")}<input value={invoiceNumber} onChange={event => setInvoiceNumber(event.target.value)} required /></label>
				<label>{t("cancelReason")}<input value={reason} onChange={event => setReason(event.target.value)} required /></label>
				<div className="form-actions">
					<button type="submit" disabled={busy}>{t("requestCancellation")}</button>
				</div>
			</form>
			<FailureMessage failure={failure} />
			<FailureMessage failure={loadFailure} />
			<table>
				<thead>
					<tr><th>{t("invoiceNumber")}</th><th>{t("cancelReason")}</th><th>{t("requestedAt")}</th><th>{t("status")}</th></tr>
				</thead>
				<tbody>
					{items.map((item: CancellationRequestItem) => (
						<tr key={item.invoiceNumber}>
							<td>{item.invoiceNumber}</td>
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
