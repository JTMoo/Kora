import { useState } from "react";
import { api, type DebitNote } from "../../api";
import { Dialog } from "../../Dialog";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { useToast } from "../../Toast";
import { DebitNoteForm } from "./DebitNoteForm";
import { transmissionStatusBadge } from "./debitNoteStatus";

export function DebitNoteList({ onSelect }: { onSelect: (debitNote: DebitNote) => void })
{
	const { t, formatDate, formatNumber } = useI18n();
	const { show: showToast } = useToast();
	const { data: debitNotes = [], setData, failure } = useLoad(api.listDebitNotes);
	const [formOpen, setFormOpen] = useState(false);

	function onSaved(debitNote: DebitNote)
	{
		setData([debitNote, ...debitNotes]);
		setFormOpen(false);
		showToast(t("debitNoteSavedToast").replace("{0}", debitNote.number));
	}

	return (
		<Page title={t("debitNotes")} toolbar={
			<button type="button" className="primary" onClick={() => setFormOpen(true)}>+ {t("addNew")}</button>
		}>
			<Dialog open={formOpen} onClose={() => setFormOpen(false)} title={t("newDebitNote")}>
				<DebitNoteForm onSaved={onSaved} onCancel={() => setFormOpen(false)} />
			</Dialog>
			<FailureMessage failure={failure} />
			<table>
				<thead>
					<tr><th>{t("debitNote")}</th><th>{t("creationDate")}</th><th>{t("invoiceId")}</th><th className="number">{t("total")}</th><th>{t("transmissionStatus")}</th></tr>
				</thead>
				<tbody>
					{debitNotes.map(debitNote => (
						<tr key={debitNote.number}>
							<td><button type="button" className="quiet" onClick={() => onSelect(debitNote)}>{debitNote.number}</button></td>
							<td>{formatDate(debitNote.date)}</td><td>{debitNote.invoiceNumber}</td>
							<td className="number">{formatNumber(debitNote.total)}</td>
							<td>{transmissionStatusBadge(debitNote, t)}</td>
						</tr>
					))}
				</tbody>
			</table>
		</Page>
	);
}
