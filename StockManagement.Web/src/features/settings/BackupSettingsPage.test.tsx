import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { mockApi, renderEnglish } from "../../test-utils";
import { BackupSettingsPage } from "./BackupSettingsPage";

const backup = { fileName: "kora-backup-20260108-120000.dump", sizeBytes: 2048, createdAtUtc: "2026-01-08T12:00:00Z" };

describe("BackupSettingsPage", () =>
{
	it("Load_NoBackups_ShowsEmptyState", async () =>
	{
		// Arrange
		mockApi({ "GET /api/backups": { body: [] } });
		renderEnglish(<BackupSettingsPage />);

		// Assert
		expect(await screen.findByText("No backups yet")).toBeInTheDocument();
	});

	it("Load_HasBackups_ListsFileName", async () =>
	{
		// Arrange
		mockApi({ "GET /api/backups": { body: [backup] } });
		renderEnglish(<BackupSettingsPage />);

		// Assert
		expect(await screen.findByText(backup.fileName)).toBeInTheDocument();
	});

	it("CreateBackup_Click_AddsItToTheList", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/backups": { body: [] },
			"POST /api/backups": { body: backup }
		});
		renderEnglish(<BackupSettingsPage />);
		await screen.findByText("No backups yet");

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Create backup" }));

		// Assert
		expect(await screen.findByText(backup.fileName)).toBeInTheDocument();
		expect(await screen.findByText("Backup created")).toBeInTheDocument();
	});

	it("Restore_NoFileChosen_ButtonDisabledEvenWhenConfirmed", async () =>
	{
		// Arrange
		mockApi({ "GET /api/backups": { body: [] } });
		renderEnglish(<BackupSettingsPage />);
		await screen.findByText("No backups yet");

		// Act
		await userEvent.click(screen.getByLabelText("I understand this will overwrite all current data"));

		// Assert
		expect(screen.getByRole("button", { name: "Restore" })).toBeDisabled();
	});

	it("Restore_FileChosenAndConfirmed_PostsTheFile", async () =>
	{
		// Arrange
		const fetchMock = mockApi({
			"GET /api/backups": { body: [] },
			"POST /api/backups/restore": { status: 204 }
		});
		renderEnglish(<BackupSettingsPage />);
		await screen.findByText("No backups yet");

		const file = new File(["dump-bytes"], "kora-backup-20260108-120000.dump");
		await userEvent.upload(screen.getByLabelText("Restore from file"), file);
		await userEvent.click(screen.getByLabelText("I understand this will overwrite all current data"));

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Restore" }));

		// Assert
		expect(await screen.findByText("Restore complete. Restart the app now.")).toBeInTheDocument();
		expect(fetchMock).toHaveBeenCalledWith("/api/backups/restore", expect.objectContaining({ method: "POST" }));
	});
});
