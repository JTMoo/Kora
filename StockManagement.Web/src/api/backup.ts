import { authHeaders, send, sendForm } from "./client";

export type BackupFile = { fileName: string; sizeBytes: number; createdAtUtc: string };

export const backupApi = {
	listBackups: (signal?: AbortSignal) => send<BackupFile[]>("/backups", { signal }),
	createBackup: () => send<BackupFile>("/backups", { method: "POST" }),
	downloadBackup: (fileName: string) => fetch(`/api/backups/${encodeURIComponent(fileName)}/download`, { headers: authHeaders() }),
	restoreBackup: (file: File) => sendForm<void>("/backups/restore", file, { Confirm: "true" })
};
