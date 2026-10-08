import { useState, type FormEvent } from "react";
import type { ApiFailure } from "../../api";
import { api } from "../../api";
import { useAuth } from "../../auth";
import { FailureMessage } from "../../FailureMessage";
import { useI18n } from "../../i18n";

/// <summary>Forced on the seeded admin (and anyone with `User.MustChangePassword`) before using the rest of the app (ADR-0046).</summary>
export function ChangePasswordPage()
{
	const { t } = useI18n();
	const { logout, passwordChanged } = useAuth();
	const [currentPassword, setCurrentPassword] = useState("");
	const [newPassword, setNewPassword] = useState("");
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);

	async function onSubmit(event: FormEvent)
	{
		event.preventDefault();
		setBusy(true);
		const result = await api.changePassword(currentPassword, newPassword);
		setBusy(false);
		if (!result.ok)
		{
			setFailure(result.failure);
			return;
		}

		passwordChanged();
	}

	return (
		<div className="login">
			<form onSubmit={onSubmit} className="panel form-grid">
				<h2>{t("changePassword")}</h2>
				<p>{t("mustChangePasswordMessage")}</p>
				<FailureMessage failure={failure} unauthorized="invalidCredentials" />
				<label>
					{t("currentPassword")}
					<input type="password" value={currentPassword} autoFocus required onChange={event => setCurrentPassword(event.target.value)} />
				</label>
				<label>
					{t("newPassword")}
					<input type="password" value={newPassword} required minLength={8} onChange={event => setNewPassword(event.target.value)} />
				</label>
				<div className="form-actions">
					<button type="button" className="quiet" onClick={logout}>{t("logout")}</button>
					<button type="submit" className="primary" disabled={busy}>{t("changePassword")}</button>
				</div>
			</form>
		</div>
	);
}
