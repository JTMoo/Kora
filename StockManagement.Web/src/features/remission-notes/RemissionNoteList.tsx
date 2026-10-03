import { useState } from "react";
import { api, type RemissionNote } from "../../api";
import { Dialog } from "../../Dialog";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { useToast } from "../../Toast";
import { RemissionNoteForm } from "./RemissionNoteForm";
import { reasonLabel, transmissionStatusBadge } from "./remissionNoteStatus";

export function RemissionNoteList({ onSelect }: { onSelect: (remissionNote: RemissionNote) => void })
{
	const { t, formatDate } = useI18n();
	const { show: showToast } = useToast();
	const { data: remissionNotes = [], setData, failure } = useLoad(api.listRemissionNotes);
	const [formOpen, setFormOpen] = useState(false);

	function onSaved(remissionNote: RemissionNote)
	{
		setData([remissionNote, ...remissionNotes]);
		setFormOpen(false);
		showToast(t("remissionNoteSavedToast").replace("{0}", remissionNote.number));
	}

	return (
		<Page title={t("remissionNotes")} toolbar={
			<button type="button" className="primary" onClick={() => setFormOpen(true)}>+ {t("addNew")}</button>
		}>
			<Dialog open={formOpen} onClose={() => setFormOpen(false)} title={t("createRemissionNote")}>
				<RemissionNoteForm onSaved={onSaved} onCancel={() => setFormOpen(false)} />
			</Dialog>
			<FailureMessage failure={failure} />
			<table>
				<thead>
					<tr><th>{t("remissionNoteNumber")}</th><th>{t("remissionNoteDate")}</th><th>{t("customerName")}</th><th>{t("reason")}</th><th>{t("transmissionStatus")}</th></tr>
				</thead>
				<tbody>
					{remissionNotes.map(remissionNote => (
						<tr key={remissionNote.number}>
							<td><button type="button" className="quiet" onClick={() => onSelect(remissionNote)}>{remissionNote.number}</button></td>
							<td>{formatDate(remissionNote.date)}</td><td>{remissionNote.customerName}</td>
							<td>{reasonLabel(remissionNote.reason, t)}</td>
							<td>{transmissionStatusBadge(remissionNote, t)}</td>
						</tr>
					))}
				</tbody>
			</table>
		</Page>
	);
}
