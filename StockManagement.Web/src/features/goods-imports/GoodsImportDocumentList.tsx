import { useEffect, useState } from "react";
import { api, type ApiFailure, type GoodsImportDocument } from "../../api";
import { Dialog } from "../../Dialog";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { useToast } from "../../Toast";
import { GoodsImportDocumentForm } from "./GoodsImportDocumentForm";

const pageSize = 20;

export function GoodsImportDocumentList({ onSelect }: { onSelect: (document: GoodsImportDocument) => void })
{
	const { t, formatDate } = useI18n();
	const { show: showToast } = useToast();
	// Cursor history (ADR-0029): cursorHistory[pageIndex] is the cursor used to fetch the current page; "previous" pops it
	const [cursorHistory, setCursorHistory] = useState<(string | undefined)[]>([undefined]);
	const [pageIndex, setPageIndex] = useState(0);
	const [items, setItems] = useState<GoodsImportDocument[]>([]);
	const [nextCursor, setNextCursor] = useState<string | null>(null);
	const [failure, setFailure] = useState<ApiFailure>();
	const [formOpen, setFormOpen] = useState(false);
	const [refreshToken, setRefreshToken] = useState(0);

	useEffect(() =>
	{
		const controller = new AbortController();
		api.listGoodsImportDocuments({ cursor: cursorHistory[pageIndex], pageSize }, controller.signal).then(result =>
		{
			if (controller.signal.aborted) return;
			setFailure(result.ok ? undefined : result.failure);
			setItems(result.ok ? result.value.items : []);
			setNextCursor(result.ok ? result.value.nextCursor : null);
		});
		return () => controller.abort();
	}, [cursorHistory, pageIndex, refreshToken]);

	function goToNextPage()
	{
		if (!nextCursor) return;
		setCursorHistory([...cursorHistory.slice(0, pageIndex + 1), nextCursor]);
		setPageIndex(pageIndex + 1);
	}

	function onSaved(document: GoodsImportDocument)
	{
		setFormOpen(false);
		setCursorHistory([undefined]);
		setPageIndex(0);
		setRefreshToken(token => token + 1);
		showToast(t("goodsImportDocumentSavedToast").replace("{0}", document.proformaNumber));
	}

	return (
		<Page title={t("goodsImports")} toolbar={
			<button type="button" className="primary" onClick={() => setFormOpen(true)}>+ {t("addNew")}</button>
		}>
			<Dialog open={formOpen} onClose={() => setFormOpen(false)} title={t("createGoodsImportDocument")}>
				<GoodsImportDocumentForm onSaved={onSaved} onCancel={() => setFormOpen(false)} />
			</Dialog>
			<FailureMessage failure={failure} />
			<table>
				<thead>
					<tr><th>{t("proformaNumber")}</th><th>{t("documentDate")}</th><th>{t("supplier")}</th><th>{t("incoterm")}</th><th>{t("brokerName")}</th></tr>
				</thead>
				<tbody>
					{items.map(document => (
						<tr key={document.id}>
							<td><button type="button" className="quiet" onClick={() => onSelect(document)}>{document.proformaNumber}</button></td>
							<td>{formatDate(document.date)}</td><td>{document.supplierName}</td>
							<td>{document.incoterm.toUpperCase()}</td><td>{document.brokerName}</td>
						</tr>
					))}
				</tbody>
			</table>
			<div className="form-actions">
				<button type="button" disabled={pageIndex <= 0} onClick={() => setPageIndex(pageIndex - 1)}>{t("previous")}</button>
				<button type="button" disabled={!nextCursor} onClick={goToNextPage}>{t("next")}</button>
			</div>
		</Page>
	);
}
