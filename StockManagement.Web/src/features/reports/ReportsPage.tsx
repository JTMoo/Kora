import { useState } from "react";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { AccountsPayableAgingReport } from "./AccountsPayableAgingReport";
import { AccountsReceivableAgingReport } from "./AccountsReceivableAgingReport";
import { LowStockReport } from "./LowStockReport";
import { SalesByCustomerReport } from "./SalesByCustomerReport";
import { SalesByPeriodReport } from "./SalesByPeriodReport";
import { StockValueReport } from "./StockValueReport";

const tabs = ["stockValue", "salesByPeriod", "salesByCustomer", "arAging", "apAging", "lowStock"] as const;

type ReportTab = typeof tabs[number];

const labels: Record<ReportTab, "stockValueReport" | "salesByPeriod" | "salesByCustomer" | "accountsReceivableAging" | "accountsPayableAging" | "lowStockReport"> = {
	stockValue: "stockValueReport",
	salesByPeriod: "salesByPeriod",
	salesByCustomer: "salesByCustomer",
	arAging: "accountsReceivableAging",
	apAging: "accountsPayableAging",
	lowStock: "lowStockReport"
};

export function ReportsPage()
{
	const { t } = useI18n();
	const [tab, setTab] = useState<ReportTab>("stockValue");

	return (
		<Page title={t("reports")}>
			<fieldset className="segmented">
				<legend>{t("reports")}</legend>
				<div>
					{tabs.map(value => (
						<label key={value}>
							<input type="radio" name="reportTab" value={value} checked={tab === value} onChange={() => setTab(value)} />
							{t(labels[value])}
						</label>
					))}
				</div>
			</fieldset>
			{tab === "stockValue" && <StockValueReport />}
			{tab === "salesByPeriod" && <SalesByPeriodReport />}
			{tab === "salesByCustomer" && <SalesByCustomerReport />}
			{tab === "arAging" && <AccountsReceivableAgingReport />}
			{tab === "apAging" && <AccountsPayableAgingReport />}
			{tab === "lowStock" && <LowStockReport />}
		</Page>
	);
}
