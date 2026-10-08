import { api, type StuckTransmission } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { transmissionStatusBadge } from "./transmissionStatusBadge";

export function StuckTransmissionsTab()
{
	const { t, formatNumber, formatDate } = useI18n();
	const { data, failure } = useLoad(api.listStuckTransmissions);
	const items = data?.items ?? [];

	return (
		<div className="panel">
			<FailureMessage failure={failure} />
			<table>
				<thead>
					<tr><th>{t("invoiceNumber")}</th><th>{t("documentDate")}</th><th>{t("customer")}</th><th className="number">{t("total")}</th><th>{t("status")}</th><th>{t("cdc")}</th></tr>
				</thead>
				<tbody>
					{items.map((item: StuckTransmission) => (
						<tr key={item.number}>
							<td>{item.number}</td>
							<td>{formatDate(item.date)}</td>
							<td>{item.customerName}</td>
							<td className="number">{formatNumber(item.total)}</td>
							<td>{transmissionStatusBadge(item.transmissionStatus, t)}</td>
							<td>{item.cdc}</td>
						</tr>
					))}
				</tbody>
			</table>
		</div>
	);
}
