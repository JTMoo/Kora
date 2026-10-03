import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { invoice, mockApi, renderEnglish, sentBody } from "../../test-utils";
import { InvoiceView } from "./InvoiceView";
import type { Invoice } from "../../api";

describe("InvoiceView", () =>
{
	it("Render_WithInvoice_ShowsLinesAndTotal", () =>
	{
		// Arrange / Act
		renderEnglish(<InvoiceView invoice={invoice as Invoice} />);

		// Assert
		expect(screen.getByRole("cell", { name: "Screw" })).toBeInTheDocument();
		expect(screen.getByTestId("invoice-total")).toHaveTextContent("10,000");
		expect(screen.getByRole("heading", { name: "Invoice 001-001-0000007" })).toBeInTheDocument();
		expect(screen.getByText("Cash")).toBeInTheDocument();
		expect(screen.getByText("Paid")).toBeInTheDocument();
	});

	it("Search_UnknownNumber_ShowsInvoiceNotFound", async () =>
	{
		// Arrange
		mockApi({ "GET /api/invoices/001-001-0000099": { status: 404 } });
		renderEnglish(<InvoiceView />);

		// Act
		await userEvent.type(screen.getByLabelText("Invoice ID"), "001-001-0000099");
		await userEvent.click(screen.getByRole("button", { name: "Show" }));

		// Assert
		expect(await screen.findByRole("alert")).toHaveTextContent("Invoice not found.");
	});

	it("Search_KnownNumber_ShowsInvoice", async () =>
	{
		// Arrange
		mockApi({ "GET /api/invoices/001-001-0000007": { body: invoice } });
		renderEnglish(<InvoiceView />);

		// Act
		await userEvent.type(screen.getByLabelText("Invoice ID"), "001-001-0000007");
		await userEvent.click(screen.getByRole("button", { name: "Show" }));

		// Assert
		expect(await screen.findByRole("article", { name: "Invoice 001-001-0000007" })).toBeInTheDocument();
	});

	it("CancelInvoice_ReasonGiven_ShowsCancelledAndHidesForm", async () =>
	{
		// Arrange
		const fetchMock = mockApi({ "POST /api/invoices/001-001-0000007/cancel": { body: { number: 1, date: "2026-09-27T10:00:00", reason: "Customer returned the goods", total: 10000, tax: 909, invoiceNumber: "001-001-0000007" } } });
		renderEnglish(<InvoiceView invoice={invoice as Invoice} />);
		await userEvent.click(screen.getByRole("button", { name: "Cancel invoice" }));
		await userEvent.type(screen.getByLabelText("Reason"), "Customer returned the goods");

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Cancel invoice" }));

		// Assert
		expect(await screen.findByTestId("invoice-cancelled")).toHaveTextContent("Cancelled");
		expect(screen.queryByLabelText("Reason")).not.toBeInTheDocument();
		expect(sentBody(fetchMock, "POST /api/invoices/001-001-0000007/cancel")).toEqual({ number: "001-001-0000007", reason: "Customer returned the goods" });
	});

	it("Render_AlreadyCancelledInvoice_HidesCancelButton", () =>
	{
		// Arrange / Act
		renderEnglish(<InvoiceView invoice={{ ...invoice, isCancelled: true } as Invoice} />);

		// Assert
		expect(screen.getByTestId("invoice-cancelled")).toHaveTextContent("Cancelled");
		expect(screen.queryByRole("button", { name: "Cancel invoice" })).not.toBeInTheDocument();
	});

	it("Render_AlreadyCancelledInvoice_HidesPaymentLinkSection", () =>
	{
		// Arrange / Act
		renderEnglish(<InvoiceView invoice={{ ...invoice, isCancelled: true } as Invoice} />);

		// Assert
		expect(screen.queryByRole("button", { name: "Generate payment link" })).not.toBeInTheDocument();
	});

	it("Load_NoPaymentLink_ShowsGenerateButton", async () =>
	{
		// Arrange
		mockApi({ "GET /api/invoices/001-001-0000007/payment-link": { status: 404 } });

		// Act
		renderEnglish(<InvoiceView invoice={invoice as Invoice} />);

		// Assert
		expect(await screen.findByRole("button", { name: "Generate payment link" })).toBeInTheDocument();
	});

	it("GeneratePaymentLink_Success_ShowsStatusAndLink", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/invoices/001-001-0000007/payment-link": { status: 404 },
			"POST /api/invoices/001-001-0000007/payment-link": { status: 201, body: { externalId: "ext-1", qrUrl: "https://bancard.example/pay/ext-1", amount: 10000, status: "Pending", createdAt: "2026-09-27T10:00:00", expiresAt: "2026-09-28T10:00:00" } }
		});
		renderEnglish(<InvoiceView invoice={invoice as Invoice} />);
		await screen.findByRole("button", { name: "Generate payment link" });

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Generate payment link" }));

		// Assert
		expect(await screen.findByRole("link", { name: "Open payment page" })).toHaveAttribute("href", "https://bancard.example/pay/ext-1");
		expect(screen.getByText("Pending")).toBeInTheDocument();
	});

	it("GeneratePaymentLink_AlreadyPaid_ShowsLocalizedError", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/invoices/001-001-0000007/payment-link": { status: 404 },
			"POST /api/invoices/001-001-0000007/payment-link": { status: 409, body: { reason: "invoiceAlreadyPaid" } }
		});
		renderEnglish(<InvoiceView invoice={invoice as Invoice} />);
		await screen.findByRole("button", { name: "Generate payment link" });

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Generate payment link" }));

		// Assert
		expect(await screen.findByRole("alert")).toHaveTextContent("Invoice is already paid.");
	});
});
