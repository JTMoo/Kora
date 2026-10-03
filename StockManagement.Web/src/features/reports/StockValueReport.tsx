import { api } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";

export function StockValueReport()
{
	const { t, formatNumber } = useI18n();
	const { data, failure } = useLoad(api.getStockValue);

	return (
		<>
			<FailureMessage failure={failure} />
			{data && (
				<div className="panel form-grid">
					<label>{t("totalValue")}<output className="number">{formatNumber(data.totalValue)}</output></label>
					<label>{t("totalUnits")}<output className="number">{formatNumber(data.totalUnits)}</output></label>
				</div>
			)}
			<table>
				<thead>
					<tr><th>{t("code")}</th><th>{t("name")}</th><th>{t("manufacturer")}</th><th className="number">{t("amount")}</th><th className="number">{t("price")}</th><th className="number">{t("totalValue")}</th></tr>
				</thead>
				<tbody>
					{data?.items.map(item => (
						<tr key={item.code}>
							<td>{item.code}</td><td>{item.name}</td><td>{item.manufacturer}</td>
							<td className="number">{formatNumber(item.amount)}</td>
							<td className="number">{formatNumber(item.price)}</td>
							<td className="number">{formatNumber(item.value)}</td>
						</tr>
					))}
				</tbody>
			</table>
		</>
	);
}
