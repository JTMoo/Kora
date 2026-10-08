import { useState } from "react";
import { authHeaders, type ApiFailure, type RemissionNote } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { reasonLabel, transmissionStatusBadge } from "./remissionNoteStatus";

export function RemissionNoteView({ remissionNote, onBack }: { remissionNote: RemissionNote; onBack: () => void })
{
	const { t, formatDate } = useI18n();
	const [kudeFailure, setKudeFailure] = useState<ApiFailure>();

	async function onViewKude()
	{
		setKudeFailure(undefined);
		const response = await fetch(`/api/remission-notes/${encodeURIComponent(remissionNote.number)}/kude`, { headers: authHeaders() });
		if (!response.ok)
		{
			setKudeFailure(response.status === 404 ? { kind: "notFound" } : { kind: "invalidState", reason: t("kudeUnavailable") });
			return;
		}

		const url = URL.createObjectURL(await response.blob());
		window.open(url, "_blank");
	}

	return (
		<Page title={t("remissionNotes")} toolbar={
			<button type="button" className="quiet" onClick={onBack}>{t("back")}</button>
		}>
			<article className="paper" aria-label={`${t("remissionNote")} ${remissionNote.number}`}>
				<header>
					<h3>{t("remissionNote")} {remissionNote.number}</h3>
					{transmissionStatusBadge(remissionNote, t)}
				</header>
				<dl>
					<dt>{t("customerName")}</dt><dd>{remissionNote.customerName} ({remissionNote.customerId})</dd>
					<dt>{t("remissionNoteDate")}</dt><dd>{formatDate(remissionNote.date)}</dd>
					<dt>{t("reason")}</dt><dd>{reasonLabel(remissionNote.reason, t)}</dd>
					<dt>{t("destinationAddress")}</dt><dd>{remissionNote.destinationAddress}</dd>
					<dt>{t("cdc")}</dt><dd>{remissionNote.cdc || "-"}</dd>
				</dl>
				<table>
					<thead>
						<tr><th>{t("code")}</th><th>{t("name")}</th><th className="number">{t("quantity")}</th></tr>
					</thead>
					<tbody>
						{remissionNote.lines.map(line => (
							<tr key={line.code}>
								<td>{line.code}</td><td>{line.name}</td><td className="number">{line.amount}</td>
							</tr>
						))}
					</tbody>
				</table>
				<div className="form-actions">
					<button type="button" onClick={onViewKude}>{t("viewKude")}</button>
				</div>
				<FailureMessage failure={kudeFailure} />
			</article>
		</Page>
	);
}
