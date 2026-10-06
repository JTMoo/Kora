import { useState, type FormEvent } from "react";
import type { LicenseInfo } from "../../api";
import { api } from "../../api";
import { isTextKey, useI18n } from "../../i18n";
import { useToast } from "../../Toast";

export function ActivateLicenseForm({ onActivated }: { onActivated: (license: LicenseInfo) => void })
{
	const { t } = useI18n();
	const { show } = useToast();
	const [licenseKey, setLicenseKey] = useState("");
	const [error, setError] = useState<string>();
	const [busy, setBusy] = useState(false);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.activateLicense(licenseKey);
		setBusy(false);

		if (!result.ok)
		{
			const reason = result.failure.kind === "invalidState" ? result.failure.reason : result.failure.kind === "invalid" ? "licenseKeyRequired" : "licenseKeyInvalid";
			setError(t(isTextKey(reason) ? reason : "licenseKeyInvalid"));
			return;
		}

		setError(undefined);
		setLicenseKey("");
		show(t("licenseActivated"));
		onActivated(result.value);
	}

	return (
		<form onSubmit={onSubmit} className="form-grid">
			<label>
				{t("licenseKey")}
				<input value={licenseKey} required onChange={event => setLicenseKey(event.target.value)} />
			</label>
			{error && <p role="alert" className="failure">{error}</p>}
			<div className="form-actions">
				<button type="submit" className="primary" disabled={busy}>{t("activate")}</button>
			</div>
		</form>
	);
}
