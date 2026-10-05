import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type CashRegisterCloseReport } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";

export function CloseSessionForm({ sessionId, onClosed }: { sessionId: string; onClosed: (report: CashRegisterCloseReport) => void })
{
	const { t } = useI18n();
	const [countedAmount, setCountedAmount] = useState("");
	const [note, setNote] = useState("");
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.closeCashRegisterSession(sessionId, Number(countedAmount), note);
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		onClosed(result.value);
	}

	return (
		<form onSubmit={onSubmit} className="panel">
			<div className="form-grid">
				<label>
					{t("countedAmount")}
					<input type="number" min={0} step="1" required value={countedAmount} onChange={event => setCountedAmount(event.target.value)} />
				</label>
				<label>
					{t("note")}
					<input type="text" value={note} onChange={event => setNote(event.target.value)} />
				</label>
			</div>
			<FailureMessage failure={failure} />
			<div className="form-actions">
				<button type="submit" disabled={busy}>{t("closeSession")}</button>
			</div>
		</form>
	);
}
