import type { LicenseInfo } from "../../api";
import { useI18n } from "../../i18n";

export function LicenseBanner({ license, onManage }: { license: LicenseInfo; onManage: () => void })
{
	const { t } = useI18n();
	if (license.status !== "Trial" && license.status !== "GracePeriod") return null;

	const tone = license.status === "GracePeriod" ? "danger" : "warning";
	const message = license.status === "GracePeriod" ? "graceBannerMessage" : "trialBannerMessage";

	return (
		<div className={`license-banner ${tone}`}>
			<span>{license.daysRemaining} {t(message)}</span>
			<button type="button" className="quiet" onClick={onManage}>{t("licensing")}</button>
		</div>
	);
}
