import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type CashMovementType } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n, type TextKey } from "../../i18n";

const types: { value: CashMovementType; label: TextKey }[] = [
	{ value: "In", label: "cashIn" },
	{ value: "Out", label: "cashOut" }
];

export function CashMovementForm({ sessionId, onAdded }: { sessionId: string; onAdded: () => void })
{
	const { t } = useI18n();
	const [type, setType] = useState<CashMovementType>("In");
	const [amount, setAmount] = useState("");
	const [reason, setReason] = useState("");
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.addCashMovement(sessionId, { type, amount: Number(amount), reason });
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		setAmount("");
		setReason("");
		onAdded();
	}

	return (
		<form onSubmit={onSubmit} className="panel">
			<div className="form-grid">
				<label>
					{t("movementType")}
					<select value={type} onChange={event => setType(event.target.value as CashMovementType)}>
						{types.map(option => <option key={option.value} value={option.value}>{t(option.label)}</option>)}
					</select>
				</label>
				<label>
					{t("amount")}
					<input type="number" min={0.01} step="0.01" required value={amount} onChange={event => setAmount(event.target.value)} />
				</label>
				<label>
					{t("reason")}
					<input type="text" required value={reason} onChange={event => setReason(event.target.value)} />
				</label>
			</div>
			<FailureMessage failure={failure} />
			<div className="form-actions">
				<button type="submit" disabled={busy}>{t("addMovement")}</button>
			</div>
		</form>
	);
}
