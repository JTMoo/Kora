import { useEffect, useState } from "react";
import { api, type ApiFailure, type SalesByCustomerRow } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";

const pageSize = 20;

function firstOfMonth(): string
{
	const now = new Date();
	return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
}

function today(): string
{
	return new Date().toISOString().slice(0, 10);
}

export function SalesByCustomerReport()
{
	const { t, formatNumber } = useI18n();
	const [from, setFrom] = useState(firstOfMonth());
	const [to, setTo] = useState(today());
	// Cursor history (ADR-0029): cursorHistory[pageIndex] is the cursor used to fetch the current page; "previous" pops it
	const [cursorHistory, setCursorHistory] = useState<(string | undefined)[]>([undefined]);
	const [pageIndex, setPageIndex] = useState(0);
	const [rows, setRows] = useState<SalesByCustomerRow[]>([]);
	const [nextCursor, setNextCursor] = useState<string | null>(null);
	const [failure, setFailure] = useState<ApiFailure>();

	useEffect(() =>
	{
		const controller = new AbortController();
		api.getSalesByCustomer({ from, to, cursor: cursorHistory[pageIndex], pageSize }, controller.signal).then(result =>
		{
			if (controller.signal.aborted) return;
			setFailure(result.ok ? undefined : result.failure);
			setRows(result.ok ? result.value.items : []);
			setNextCursor(result.ok ? result.value.nextCursor : null);
		});
		return () => controller.abort();
	}, [from, to, cursorHistory, pageIndex]);

	function resetToFirstPage()
	{
		setCursorHistory([undefined]);
		setPageIndex(0);
	}

	function goToNextPage()
	{
		if (!nextCursor) return;
		setCursorHistory([...cursorHistory.slice(0, pageIndex + 1), nextCursor]);
		setPageIndex(pageIndex + 1);
	}

	return (
		<>
			<form className="panel" onSubmit={event => event.preventDefault()}>
				<div className="form-grid">
					<label>{t("from")}<input type="date" value={from} onChange={event => { setFrom(event.target.value); resetToFirstPage(); }} /></label>
					<label>{t("to")}<input type="date" value={to} onChange={event => { setTo(event.target.value); resetToFirstPage(); }} /></label>
				</div>
			</form>
			<FailureMessage failure={failure} />
			{rows.length === 0 && !failure ? <p className="small">{t("noSalesForPeriod")}</p> : (
				<>
					<table>
						<thead>
							<tr><th>{t("customerName")}</th><th className="number">{t("invoiceCount")}</th><th className="number">{t("total")}</th></tr>
						</thead>
						<tbody>
							{rows.map(row => (
								<tr key={row.customerId}>
									<td>{row.customerName}</td>
									<td className="number">{formatNumber(row.invoiceCount)}</td>
									<td className="number">{formatNumber(row.total)}</td>
								</tr>
							))}
						</tbody>
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
