import { api } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";

/** Below-minimum stock items (#57's endpoint), framed as a report (#163) */
export function LowStockReport()
{
	const { t, formatNumber } = useI18n();
	const { data: items = [], failure } = useLoad(api.listStockItemsBelowMinimum);

	return (
		<>
			<FailureMessage failure={failure} />
			{items.length === 0 && !failure ? <p className="small">{t("noLowStockItems")}</p> : (
				<table>
					<thead>
						<tr><th>{t("code")}</th><th>{t("name")}</th><th className="number">{t("amount")}</th><th className="number">{t("minimumStock")}</th></tr>
					</thead>
					<tbody>
						{items.map(item => (
							<tr key={item.code} className="below-minimum">
								<td>{item.code}</td><td>{item.name}</td>
								<td className="number">{formatNumber(item.amount)}</td>
								<td className="number">{formatNumber(item.minimumStock)}</td>
							</tr>
						))}
					</tbody>
				</table>
			)}
		</>
	);
}
