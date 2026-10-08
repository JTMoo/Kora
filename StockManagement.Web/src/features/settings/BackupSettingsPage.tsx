import { useRef, useState } from "react";
import { api, type ApiFailure, type BackupFile } from "../../api";
import { FailureMessage } from "../../FailureMessage";
import { Page } from "../../Page";
import { useI18n } from "../../i18n";
import { useLoad } from "../../useLoad";

function formatSize(bytes: number)
{
	if (bytes < 1024) return `${bytes} B`;
	if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
	return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function BackupSettingsPage()
{
	const { t, formatDate } = useI18n();
	const { data: backups, setData: setBackups, failure: loadFailure } = useLoad(api.listBackups);
	const [failure, setFailure] = useState<ApiFailure>();
	const [busy, setBusy] = useState(false);
	const [confirmed, setConfirmed] = useState(false);
	const [selectedFile, setSelectedFile] = useState<File>();
	const [notice, setNotice] = useState<string>();
	const fileInput = useRef<HTMLInputElement>(null);

	async function onCreateBackup()
	{
		setBusy(true);
		const result = await api.createBackup();
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		setNotice(t("backupCreatedToast"));
		setBackups([result.value, ...(backups ?? [])]);
	}

	async function onDownload(fileName: string)
	{
		const response = await api.downloadBackup(fileName);
		if (!response.ok) return setFailure({ kind: "unexpected" });

		const url = URL.createObjectURL(await response.blob());
		const link = document.createElement("a");
		link.href = url;
		link.download = fileName;
		link.click();
		URL.revokeObjectURL(url);
	}

	async function onRestore()
	{
		if (!selectedFile || !confirmed) return;

		setBusy(true);
		const result = await api.restoreBackup(selectedFile);
		setBusy(false);
		if (!result.ok) return setFailure(result.failure);

		setFailure(undefined);
		setNotice(t("restoreSucceededMessage"));
		setConfirmed(false);
		setSelectedFile(undefined);
		if (fileInput.current) fileInput.current.value = "";
	}

	return (
		<Page title={t("backups")} toolbar={<button type="button" onClick={onCreateBackup} disabled={busy}>{t("createBackup")}</button>}>
			<FailureMessage failure={loadFailure ?? failure} />
			{notice && <p role="status">{notice}</p>}

			<div className="panel">
				{!backups?.length
					? <p>{t("noBackupsYet")}</p>
					: (
						<table>
							<thead>
								<tr>
									<th>{t("name")}</th>
									<th>{t("backupDate")}</th>
									<th>{t("backupSize")}</th>
									<th />
								</tr>
							</thead>
							<tbody>
								{backups.map((backup: BackupFile) => (
									<tr key={backup.fileName}>
										<td>{backup.fileName}</td>
										<td>{formatDate(backup.createdAtUtc)}</td>
										<td>{formatSize(backup.sizeBytes)}</td>
										<td><button type="button" onClick={() => onDownload(backup.fileName)}>{t("download")}</button></td>
									</tr>
								))}
							</tbody>
						</table>
					)}
			</div>

			<div className="panel form-grid">
				<h3>{t("restoreBackup")}</h3>
				<p>{t("restoreWarning")}</p>
				<input ref={fileInput} type="file" accept=".dump" aria-label={t("restoreBackup")} onChange={event => setSelectedFile(event.target.files?.[0])} />
				<label>
					<input type="checkbox" checked={confirmed} onChange={event => setConfirmed(event.target.checked)} />
					{t("restoreConfirmCheckbox")}
				</label>
				<div className="form-actions">
					<button type="button" onClick={onRestore} disabled={busy || !confirmed || !selectedFile}>{t("restore")}</button>
				</div>
			</div>
		</Page>
	);
}
