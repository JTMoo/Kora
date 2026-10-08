import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type DebitNote, type NewDebitNoteItem } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";

const emptyItem: NewDebitNoteItem = { description: "", amount: 0, vatRatePercent: 10 };

export function DebitNoteForm({ onSaved, onCancel }: { onSaved: (debitNote: DebitNote) => void; onCancel: () => void })
{
	const { t } = useI18n();
	const [invoiceNumber, setInvoiceNumber] = useState("");
	const [reason, setReason] = useState("");
	const [items, setItems] = useState<NewDebitNoteItem[]>([{ ...emptyItem }]);
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	function updateItem(index: number, item: NewDebitNoteItem)
	{
		setItems(items.map((existing, existingIndex) => existingIndex === index ? item : existing));
	}

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.createDebitNote({ invoiceNumber, reason, items });
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		onSaved(result.value);
	}

	return (
		<form onSubmit={onSubmit} className="form-grid" aria-label={t("newDebitNote")}>
			<label>
				{t("invoiceId")}
				<input type="text" required value={invoiceNumber} onChange={event => setInvoiceNumber(event.target.value)} />
			</label>
			<label>
				{t("reason")}
				<input type="text" required value={reason} onChange={event => setReason(event.target.value)} />
			</label>
			<table>
				<thead>
					<tr><th>{t("description")}</th><th className="number">{t("amount")}</th><th className="number">{t("vatRatePercent")}</th></tr>
				</thead>
				<tbody>
					{items.map((item, index) => (
						<tr key={index}>
							<td><input type="text" required value={item.description} onChange={event => updateItem(index, { ...item, description: event.target.value })} /></td>
							<td><input type="number" min={0.01} step="0.01" required className="number" value={item.amount} onChange={event => updateItem(index, { ...item, amount: Number(event.target.value) })} /></td>
							<td><input type="number" min={0} max={100} step="1" required className="number" value={item.vatRatePercent} onChange={event => updateItem(index, { ...item, vatRatePercent: Number(event.target.value) })} /></td>
						</tr>
					))}
				</tbody>
			</table>
			<div className="form-actions">
				<button type="button" onClick={() => setItems([...items, { ...emptyItem }])}>{t("addItem")}</button>
			</div>
			<FailureMessage failure={failure} />
			<div className="form-actions">
				<button type="submit" disabled={busy}>{t("newDebitNote")}</button>
				<button type="button" onClick={onCancel}>{t("cancel")}</button>
			</div>
		</form>
	);
}
