import { useEffect, useState } from "react";
import { api, type ApiFailure, type SalesByPeriodRow } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";

function firstOfMonth(): string
{
	const now = new Date();
	return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
}

function today(): string
{
	return new Date().toISOString().slice(0, 10);
}

export function SalesByPeriodReport()
{
	const { t, formatNumber, formatDate } = useI18n();
	const [from, setFrom] = useState(firstOfMonth());
	const [to, setTo] = useState(today());
	const [rows, setRows] = useState<SalesByPeriodRow[]>([]);
	const [failure, setFailure] = useState<ApiFailure>();

	useEffect(() =>
	{
		const controller = new AbortController();
		api.getSalesByPeriod({ from, to }, controller.signal).then(result =>
		{
			if (controller.signal.aborted) return;
			setFailure(result.ok ? undefined : result.failure);
			setRows(result.ok ? result.value.items : []);
		});
		return () => controller.abort();
	}, [from, to]);

	return (
		<>
			<form className="panel" onSubmit={event => event.preventDefault()}>
				<div className="form-grid">
					<label>{t("from")}<input type="date" value={from} onChange={event => setFrom(event.target.value)} /></label>
					<label>{t("to")}<input type="date" value={to} onChange={event => setTo(event.target.value)} /></label>
				</div>
			</form>
			<FailureMessage failure={failure} />
			{rows.length === 0 && !failure ? <p className="small">{t("noSalesForPeriod")}</p> : (
				<table>
					<thead>
						<tr><th>{t("creationDate")}</th><th className="number">{t("invoiceCount")}</th><th className="number">{t("total")}</th><th className="number">{t("tax")}</th></tr>
					</thead>
					<tbody>
						{rows.map(row => (
							<tr key={row.date}>
								<td>{formatDate(row.date)}</td>
								<td className="number">{formatNumber(row.invoiceCount)}</td>
								<td className="number">{formatNumber(row.total)}</td>
								<td className="number">{formatNumber(row.tax)}</td>
							</tr>
						))}
					</tbody>
				</table>
			)}
		</>
	);
}
