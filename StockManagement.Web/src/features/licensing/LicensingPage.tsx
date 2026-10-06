import type { LicensePlan, LicenseStatus } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { StatusBadge, type StatusTone } from "../../StatusBadge";
import { useAuth } from "../../auth";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";
import { api } from "../../api";
import { ActivateLicenseForm } from "./ActivateLicenseForm";

const statusTone: Record<LicenseStatus, StatusTone> = { Trial: "info", Active: "success", GracePeriod: "warning", Locked: "danger" };
const statusLabel: Record<LicenseStatus, "licenseStatusTrial" | "licenseStatusActive" | "licenseStatusGracePeriod" | "licenseStatusLocked"> =
	{ Trial: "licenseStatusTrial", Active: "licenseStatusActive", GracePeriod: "licenseStatusGracePeriod", Locked: "licenseStatusLocked" };
const planLabel: Record<LicensePlan, "noPlan" | "monthlyPlan" | "yearlyPlan"> = { None: "noPlan", Monthly: "monthlyPlan", Yearly: "yearlyPlan" };

export function LicensingPage()
{
	const { t, formatDate, formatNumber } = useI18n();
	const { hasPermission } = useAuth();
	const { data: license, setData, failure } = useLoad(api.getLicense);

	if (!license) return <Page title={t("licensing")}><FailureMessage failure={failure} /></Page>;

	return (
		<Page title={t("licensing")}>
			<div className="panel form-grid">
				<label>
					{t("licenseStatus")}
					<StatusBadge tone={statusTone[license.status]}>{t(statusLabel[license.status])}</StatusBadge>
				</label>
				<label>
					{t("daysRemaining")}
					<span>{license.status === "Locked" ? "—" : formatNumber(license.daysRemaining)}</span>
				</label>
				<label>
					{t("currentPlan")}
					<span>{t(planLabel[license.plan])}</span>
				</label>
				{license.subscriptionExpiresAtUtc && (
					<label>
						{t("subscriptionExpiresOn")}
						<span>{formatDate(license.subscriptionExpiresAtUtc)}</span>
					</label>
				)}
				<label>
					{t("monthlyPrice")}
					<span>{formatNumber(license.monthlyPricePyg)}</span>
				</label>
				<label>
					{t("yearlyPrice")}
					<span>{formatNumber(license.yearlyPricePyg)}</span>
				</label>
			</div>

			{hasPermission("Settings.Write") && (
				<div className="panel">
					<h3 className="display">{t("activateLicense")}</h3>
					<ActivateLicenseForm onActivated={setData} />
				</div>
			)}
		</Page>
	);
}
