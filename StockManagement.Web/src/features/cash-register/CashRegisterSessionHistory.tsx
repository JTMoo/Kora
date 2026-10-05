import { useEffect, useState } from "react";
import { api, type ApiFailure, type CashRegisterSession } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { cashRegisterSessionStatusBadge } from "./cashRegisterSessionStatus";

const pageSize = 20;

export function CashRegisterSessionHistory({ refreshToken }: { refreshToken: number })
{
	const { t, formatNumber, formatDate } = useI18n();
	// Cursor history (ADR-0029): cursorHistory[pageIndex] is the cursor used to fetch the current page; "previous" pops it
	const [cursorHistory, setCursorHistory] = useState<(string | undefined)[]>([undefined]);
	const [pageIndex, setPageIndex] = useState(0);
	const [items, setItems] = useState<CashRegisterSession[]>([]);
	const [nextCursor, setNextCursor] = useState<string | null>(null);
	const [failure, setFailure] = useState<ApiFailure>();

	useEffect(() =>
	{
		const controller = new AbortController();
		api.listCashRegisterSessions({ cursor: cursorHistory[pageIndex], pageSize }, controller.signal).then(result =>
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

	return (
		<>
			<h3>{t("cashRegisterSessions")}</h3>
			<FailureMessage failure={failure} />
			<table>
				<thead>
					<tr>
						<th>{t("openedAt")}</th><th>{t("closedAt")}</th>
						<th className="number">{t("openingFloat")}</th><th className="number">{t("expectedAmount")}</th>
						<th className="number">{t("countedAmount")}</th><th className="number">{t("difference")}</th>
						<th>{t("status")}</th>
					</tr>
				</thead>
				<tbody>
					{items.map(session => (
						<tr key={session.id}>
							<td>{formatDate(session.openedAt)}</td>
							<td>{session.closedAt ? formatDate(session.closedAt) : "-"}</td>
							<td className="number">{formatNumber(session.openingFloat)}</td>
							<td className="number">{formatNumber(session.expectedAmount)}</td>
							<td className="number">{session.countedAmount === null ? "-" : formatNumber(session.countedAmount)}</td>
							<td className="number">{session.countedAmount === null ? "-" : formatNumber(session.countedAmount - session.expectedAmount)}</td>
							<td>{cashRegisterSessionStatusBadge(session, t)}</td>
						</tr>
					))}
				</tbody>
			</table>
			<div className="form-actions">
				<button type="button" disabled={pageIndex <= 0} onClick={() => setPageIndex(pageIndex - 1)}>{t("previous")}</button>
				<button type="button" disabled={!nextCursor} onClick={goToNextPage}>{t("next")}</button>
			</div>
		</>
	);
}
