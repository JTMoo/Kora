import { useState, type FormEvent } from "react";
import { api, type ApiFailure, type CreditNote } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";

export function CreditNoteView()
{
	const { t, formatNumber, formatDate } = useI18n();
	const [number, setNumber] = useState("");
	const [creditNote, setCreditNote] = useState<CreditNote>();
	const [failure, setFailure] = useState<ApiFailure>();

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		const result = await api.getCreditNote(Number(number));
		setCreditNote(result.ok ? result.value : undefined);
		setFailure(result.ok ? undefined : result.failure);
	}

	return (
		<Page title={t("creditNotes")} toolbar={
			<form onSubmit={onSubmit} className="toolbar-form">
				<input type="number" min={1} required aria-label={t("creditNoteId")} placeholder={t("creditNoteId")} value={number} onChange={event => setNumber(event.target.value)} />
				<button type="submit">{t("show")}</button>
			</form>
		}>
			<FailureMessage failure={failure} />
			{creditNote && (
				<article className="paper" aria-label={`${t("creditNote")} ${creditNote.number}`}>
					<header>
						<h3>{t("creditNote")} {creditNote.number}</h3>
					</header>
					<dl>
						<dt>{t("invoiceId")}</dt><dd>{creditNote.invoiceNumber}</dd>
						<dt>{t("creationDate")}</dt><dd>{formatDate(creditNote.date)}</dd>
						<dt>{t("reason")}</dt><dd>{creditNote.reason}</dd>
					</dl>
					<dl>
						<dt>{t("tax")}</dt><dd>{formatNumber(creditNote.tax)}</dd>
						<dt>{t("total")}</dt><dd data-testid="credit-note-total">{formatNumber(creditNote.total)}</dd>
					</dl>
				</article>
			)}
		</Page>
	);
}
