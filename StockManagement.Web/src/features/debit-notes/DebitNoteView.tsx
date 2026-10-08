import type { DebitNote } from "../../api";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { transmissionStatusBadge } from "./debitNoteStatus";

export function DebitNoteView({ debitNote, onBack }: { debitNote: DebitNote; onBack: () => void })
{
	const { t, formatNumber, formatDate } = useI18n();

	return (
		<Page title={t("debitNotes")} toolbar={
			<button type="button" className="quiet" onClick={onBack}>{t("back")}</button>
		}>
			<article className="paper" aria-label={`${t("debitNote")} ${debitNote.number}`}>
				<header>
					<h3>{t("debitNote")} {debitNote.number}</h3>
					{transmissionStatusBadge(debitNote, t)}
				</header>
				<dl>
					<dt>{t("invoiceId")}</dt><dd>{debitNote.invoiceNumber}</dd>
					<dt>{t("creationDate")}</dt><dd>{formatDate(debitNote.date)}</dd>
					<dt>{t("reason")}</dt><dd>{debitNote.reason}</dd>
					<dt>{t("cdc")}</dt><dd>{debitNote.cdc || "-"}</dd>
				</dl>
				<table>
					<thead>
						<tr><th>{t("description")}</th><th className="number">{t("amount")}</th><th className="number">{t("vatRatePercent")}</th></tr>
					</thead>
					<tbody>
						{debitNote.lines.map((line, index) => (
							<tr key={index}>
								<td>{line.description}</td><td className="number">{formatNumber(line.amount)}</td><td className="number">{line.vatRatePercent}</td>
							</tr>
						))}
					</tbody>
					<tfoot>
						<tr><th colSpan={2}>{t("tax")}</th><td className="number">{formatNumber(debitNote.tax)}</td></tr>
						<tr className="total"><th colSpan={2}>{t("total")}</th><td className="number">{formatNumber(debitNote.total)}</td></tr>
					</tfoot>
				</table>
			</article>
		</Page>
	);
}
