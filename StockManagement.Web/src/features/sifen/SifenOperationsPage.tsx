import { useState } from "react";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { CancellationsTab } from "./CancellationsTab";
import { ContingencyTab } from "./ContingencyTab";
import { NumberVoidsTab } from "./NumberVoidsTab";
import { StuckTransmissionsTab } from "./StuckTransmissionsTab";

const tabs = ["contingency", "cancellations", "numberVoids", "stuckTransmissions"] as const;

type SifenTab = typeof tabs[number];

const labels: Record<SifenTab, "contingencyTab" | "cancellationTab" | "numberVoidTab" | "stuckTransmissionsTab"> = {
	contingency: "contingencyTab",
	cancellations: "cancellationTab",
	numberVoids: "numberVoidTab",
	stuckTransmissions: "stuckTransmissionsTab"
};

export function SifenOperationsPage()
{
	const { t } = useI18n();
	const [tab, setTab] = useState<SifenTab>("contingency");

	return (
		<Page title={t("sifenOperations")}>
			<fieldset className="segmented">
				<legend>{t("sifenOperations")}</legend>
				<div>
					{tabs.map(value => (
						<label key={value}>
							<input type="radio" name="sifenTab" value={value} checked={tab === value} onChange={() => setTab(value)} />
							{t(labels[value])}
						</label>
					))}
				</div>
			</fieldset>
			{tab === "contingency" && <ContingencyTab />}
			{tab === "cancellations" && <CancellationsTab />}
			{tab === "numberVoids" && <NumberVoidsTab />}
			{tab === "stuckTransmissions" && <StuckTransmissionsTab />}
		</Page>
	);
}
