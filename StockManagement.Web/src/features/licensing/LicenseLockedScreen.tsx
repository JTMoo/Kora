import type { LicenseInfo } from "../../api";
import { useAuth } from "../../auth";
import { useI18n } from "../../i18n";
import { ActivateLicenseForm } from "./ActivateLicenseForm";

export function LicenseLockedScreen({ onActivated }: { onActivated: (license: LicenseInfo) => void })
{
	const { t } = useI18n();
	const { logout, hasPermission } = useAuth();

	return (
		<div className="login">
			<div className="panel form-grid">
				<h2>{t("licenseLockedTitle")}</h2>
				<p>{t("licenseLockedMessage")}</p>
				{hasPermission("Settings.Write")
					? <ActivateLicenseForm onActivated={onActivated} />
					: <p>{t("contactToSubscribe")}</p>}
				<div className="form-actions">
					<button type="button" className="quiet" onClick={logout}>{t("logout")}</button>
				</div>
			</div>
		</div>
	);
}
