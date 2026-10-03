import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { mockApi, renderEnglish } from "../../test-utils";
import { MarangatuExportPage } from "./MarangatuExportPage";

describe("MarangatuExportPage", () =>
{
	it("Download_Clicked_RequestsCsvForSelectedPeriod", async () =>
	{
		// Arrange
		const fetchMock = mockApi({ "GET /api/invoices/iva-book?from=2026-09-01&to=2026-09-15": { body: "code,name\n" } });
		renderEnglish(<MarangatuExportPage />);
		await userEvent.clear(screen.getByLabelText("From"));
		await userEvent.type(screen.getByLabelText("From"), "2026-09-01");
		await userEvent.clear(screen.getByLabelText("To"));
		await userEvent.type(screen.getByLabelText("To"), "2026-09-15");

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Download CSV" }));

		// Assert
		expect(fetchMock).toHaveBeenCalledWith("/api/invoices/iva-book?from=2026-09-01&to=2026-09-15", expect.anything());
	});

	it("Render_FromAfterTo_DisablesDownload", async () =>
	{
		// Arrange
		renderEnglish(<MarangatuExportPage />);

		// Act
		await userEvent.clear(screen.getByLabelText("From"));
		await userEvent.type(screen.getByLabelText("From"), "2026-09-20");
		await userEvent.clear(screen.getByLabelText("To"));
		await userEvent.type(screen.getByLabelText("To"), "2026-09-01");

		// Assert
		expect(screen.getByRole("button", { name: "Download CSV" })).toBeDisabled();
	});
});
