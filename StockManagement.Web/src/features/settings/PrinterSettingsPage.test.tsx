import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { mockApi, renderEnglish, sentBody } from "../../test-utils";
import { PrinterSettingsPage } from "./PrinterSettingsPage";

const printerSettings = { defaultPrinterName: "Front counter", receiptPaperWidthMm: 80, kudeFormat: "Ticket" as const, printOnSaleComplete: false };

describe("PrinterSettingsPage", () =>
{
	it("Load_Stored_ShowsFields", async () =>
	{
		// Arrange
		mockApi({ "GET /api/printer-settings": { body: printerSettings } });
		renderEnglish(<PrinterSettingsPage />);

		// Assert
		expect(await screen.findByDisplayValue("Front counter")).toBeInTheDocument();
		expect(screen.getByDisplayValue("80")).toBeInTheDocument();
	});

	it("Submit_ChangedPaperWidth_Puts", async () =>
	{
		// Arrange
		const fetchMock = mockApi({
			"GET /api/printer-settings": { body: printerSettings },
			"PUT /api/printer-settings": { body: { ...printerSettings, receiptPaperWidthMm: 58 } }
		});
		renderEnglish(<PrinterSettingsPage />);
		await screen.findByDisplayValue("Front counter");

		// Act
		await userEvent.clear(screen.getByLabelText("Receipt paper width (mm)"));
		await userEvent.type(screen.getByLabelText("Receipt paper width (mm)"), "58");
		await userEvent.click(screen.getByRole("button", { name: "Save" }));

		// Assert
		expect(sentBody(fetchMock, "PUT /api/printer-settings")).toMatchObject({ receiptPaperWidthMm: 58 });
	});

	it("PrintTestPage_Click_OpensWindowAndPrints", async () =>
	{
		// Arrange
		mockApi({ "GET /api/printer-settings": { body: printerSettings } });
		const printMock = vi.fn();
		const testWindow = { document: { write: vi.fn(), close: vi.fn() }, focus: vi.fn(), print: printMock };
		vi.spyOn(window, "open").mockReturnValue(testWindow as unknown as Window);
		renderEnglish(<PrinterSettingsPage />);
		await screen.findByDisplayValue("Front counter");

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Print test page" }));

		// Assert
		expect(printMock).toHaveBeenCalledOnce();
	});
});
