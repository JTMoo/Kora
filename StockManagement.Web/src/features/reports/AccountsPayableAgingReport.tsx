import { useEffect, useState } from "react";
import { api, type AgingTotals, type ApiFailure, type PayableAgingRow } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";

const pageSize = 20;
const emptyTotals: AgingTotals = { current: 0, days1To30: 0, days31To60: 0, days61To90: 0, days90Plus: 0, total: 0 };

export function AccountsPayableAgingReport()
{
	const { t, formatNumber } = useI18n();
	// Cursor history (ADR-0029): cursorHistory[pageIndex] is the cursor used to fetch the current page; "previous" pops it
	const [cursorHistory, setCursorHistory] = useState<(string | undefined)[]>([undefined]);
	const [pageIndex, setPageIndex] = useState(0);
	const [totals, setTotals] = useState<AgingTotals>(emptyTotals);
	const [rows, setRows] = useState<PayableAgingRow[]>([]);
	const [nextCursor, setNextCursor] = useState<string | null>(null);
	const [failure, setFailure] = useState<ApiFailure>();

	useEffect(() =>
	{
		const controller = new AbortController();
		api.getAccountsPayableAging({ cursor: cursorHistory[pageIndex], pageSize }, controller.signal).then(result =>
		{
			if (controller.signal.aborted) return;
			setFailure(result.ok ? undefined : result.failure);
			setTotals(result.ok ? result.value.totals : emptyTotals);
			setRows(result.ok ? result.value.items : []);
			setNextCursor(result.ok ? result.value.nextCursor : null);
		});
		return () => controller.abort();
	}, [cursorHistory, pageIndex]);

	function goToNextPage()
	{
		if (!nextCursor) return;
		setCursorHistory([...cursorHistory.slice(0, pageIndex + 1), nextCursor]);
		setPageIndex(pageIndex + 1);
	}

	return (
		<>
			<FailureMessage failure={failure} />
			{rows.length === 0 && !failure ? <p className="small">{t("noOpenSupplierInvoices")}</p> : (
				<>
					<table>
						<thead>
							<tr>
								<th>{t("supplier")}</th>
								<th className="number">{t("current")}</th>
								<th className="number">{t("days1To30")}</th>
								<th className="number">{t("days31To60")}</th>
								<th className="number">{t("days61To90")}</th>
								<th className="number">{t("days90Plus")}</th>
								<th className="number">{t("total")}</th>
							</tr>
						</thead>
						<tbody>
							{rows.map(row => (
								<tr key={row.supplierId}>
									<td>{row.supplierName}</td>
									<td className="number">{formatNumber(row.current)}</td>
									<td className="number">{formatNumber(row.days1To30)}</td>
									<td className="number">{formatNumber(row.days31To60)}</td>
									<td className="number">{formatNumber(row.days61To90)}</td>
									<td className="number">{formatNumber(row.days90Plus)}</td>
									<td className="number">{formatNumber(row.total)}</td>
								</tr>
							))}
						</tbody>
						<tfoot>
							<tr>
								<td>{t("total")}</td>
								<td className="number">{formatNumber(totals.current)}</td>
								<td className="number">{formatNumber(totals.days1To30)}</td>
								<td className="number">{formatNumber(totals.days31To60)}</td>
								<td className="number">{formatNumber(totals.days61To90)}</td>
								<td className="number">{formatNumber(totals.days90Plus)}</td>
								<td className="number">{formatNumber(totals.total)}</td>
							</tr>
						</tfoot>
					</table>
					<div className="form-actions">
						<button type="button" disabled={pageIndex <= 0} onClick={() => setPageIndex(pageIndex - 1)}>{t("previous")}</button>
						<button type="button" disabled={!nextCursor} onClick={goToNextPage}>{t("next")}</button>
					</div>
				</>
			)}
		</>
	);
}
