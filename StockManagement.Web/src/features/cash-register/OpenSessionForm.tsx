import { useState, type FormEvent } from "react";
import { api, type ApiFailure } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";

export function OpenSessionForm({ onOpened }: { onOpened: () => void })
{
	const { t } = useI18n();
	const [openingFloat, setOpeningFloat] = useState("0");
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.openCashRegisterSession(Number(openingFloat));
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		onOpened();
	}

	return (
		<form onSubmit={onSubmit} className="panel">
			<div className="form-grid">
				<label>
					{t("openingFloat")}
					<input type="number" min={0} step="1" required value={openingFloat} onChange={event => setOpeningFloat(event.target.value)} />
				</label>
			</div>
			<FailureMessage failure={failure} />
			<div className="form-actions">
				<button type="submit" disabled={busy}>{t("openSession")}</button>
			</div>
		</form>
	);
}
