import { useEffect, useState } from "react";
import { api, type ApiFailure, type SupplierInvoice } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { RecordPaymentForm } from "./RecordPaymentForm";
import { SupplierInvoiceForm } from "./SupplierInvoiceForm";
import { supplierInvoiceStatusBadge } from "./supplierInvoiceStatus";

const pageSize = 20;

export function SupplierInvoiceList()
{
	const { t, formatNumber, formatDate } = useI18n();
	const { data: suppliers = [] } = useLoad(api.listSuppliers);
	// Cursor history (ADR-0029): cursorHistory[pageIndex] is the cursor used to fetch the current page; "previous" pops it
	const [cursorHistory, setCursorHistory] = useState<(string | undefined)[]>([undefined]);
	const [pageIndex, setPageIndex] = useState(0);
	const [items, setItems] = useState<SupplierInvoice[]>([]);
	const [nextCursor, setNextCursor] = useState<string | null>(null);
	const [failure, setFailure] = useState<ApiFailure>();
	const [payingNumber, setPayingNumber] = useState<string>();
	const [refreshToken, setRefreshToken] = useState(0);

	useEffect(() =>
	{
		const controller = new AbortController();
		api.listSupplierInvoices({ cursor: cursorHistory[pageIndex], pageSize }, controller.signal).then(result =>
		{
			if (controller.signal.aborted) return;
			setFailure(result.ok ? undefined : result.failure);
			setItems(result.ok ? result.value.items : []);
			setNextCursor(result.ok ? result.value.nextCursor : null);
		});
		return () => controller.abort();
	}, [cursorHistory, pageIndex, refreshToken]);

	function resetToFirstPage()
	{
		setCursorHistory([undefined]);
		setPageIndex(0);
		setRefreshToken(token => token + 1);
	}

	function goToNextPage()
	{
		if (!nextCursor) return;
		setCursorHistory([...cursorHistory.slice(0, pageIndex + 1), nextCursor]);
		setPageIndex(pageIndex + 1);
	}

	function onPaid()
	{
		setPayingNumber(undefined);
		setRefreshToken(token => token + 1);
	}

	return (
		<Page title={t("payables")}>
			<SupplierInvoiceForm suppliers={suppliers} onCreated={resetToFirstPage} />
			<FailureMessage failure={failure} />
			<table>
				<thead>
					<tr>
						<th>{t("supplierInvoiceNumber")}</th><th>{t("creationDate")}</th><th>{t("supplier")}</th>
						<th className="number">{t("total")}</th><th className="number">{t("amountDue")}</th><th>{t("status")}</th><th></th>
					</tr>
				</thead>
				<tbody>
					{items.map(item => (
						<tr key={item.number}>
							<td>{item.number}</td>
							<td>{formatDate(item.date)}</td>
							<td>{item.supplierName}</td>
							<td className="number">{formatNumber(item.total)}</td>
							<td className="number">{formatNumber(item.amountDue)}</td>
							<td>{supplierInvoiceStatusBadge(item, t)}</td>
							<td className="row-actions">
								{item.status !== "Paid" && payingNumber !== item.number && (
									<button type="button" className="quiet" onClick={() => setPayingNumber(item.number)}>{t("recordPayment")}</button>
								)}
							</td>
						</tr>
					))}
				</tbody>
			</table>
			{payingNumber && (
				<RecordPaymentForm invoiceNumber={payingNumber} onPaid={onPaid} onCancel={() => setPayingNumber(undefined)} />
			)}
			<div className="form-actions">
				<button type="button" disabled={pageIndex <= 0} onClick={() => setPageIndex(pageIndex - 1)}>{t("previous")}</button>
				<button type="button" disabled={!nextCursor} onClick={goToNextPage}>{t("next")}</button>
			</div>
		</Page>
	);
}
