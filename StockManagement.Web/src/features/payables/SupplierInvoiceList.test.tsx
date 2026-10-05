import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { acme, mockApi, renderEnglish, sentBody } from "../../test-utils";
import { SupplierInvoiceList } from "./SupplierInvoiceList";

const invoice = { number: "SI-1", date: "2026-01-01T00:00:00", expirationDate: "2026-01-31T00:00:00", total: 1000, supplierId: acme.id, supplierName: acme.name, amountPaid: 0, amountDue: 1000, status: "Open" as const };

describe("SupplierInvoiceList", () =>
{
	it("Load_OneInvoice_ShowsItWithAmountDue", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/suppliers?pageSize=100": { body: { items: [acme], nextCursor: null } },
			"GET /api/supplier-invoices?pageSize=20": { body: { items: [invoice], nextCursor: null } }
		});

		// Act
		renderEnglish(<SupplierInvoiceList />);

		// Assert
		expect(await screen.findByRole("cell", { name: "SI-1" })).toBeInTheDocument();
		expect(screen.getAllByRole("cell", { name: "1,000" })).toHaveLength(2);
	});

	it("Create_Valid_PostsAndRefreshesList", async () =>
	{
		// Arrange
		const fetchMock = mockApi({
			"GET /api/suppliers?pageSize=100": { body: { items: [acme], nextCursor: null } },
			"GET /api/supplier-invoices?pageSize=20": { body: { items: [], nextCursor: null } },
			"POST /api/supplier-invoices": { status: 201, body: invoice }
		});
		renderEnglish(<SupplierInvoiceList />);
		await screen.findByRole("combobox", { name: "Supplier" });

		// Act
		await userEvent.type(screen.getByLabelText("Invoice number"), "SI-1");
		await userEvent.selectOptions(screen.getByLabelText("Supplier"), acme.id);
		await userEvent.type(screen.getByLabelText("Total"), "1000");
		await userEvent.click(screen.getByRole("button", { name: "Create supplier invoice" }));

		// Assert
		expect(sentBody(fetchMock, "POST /api/supplier-invoices")).toMatchObject({ number: "SI-1", supplierId: acme.id, total: 1000 });
	});

	it("RecordPayment_Valid_PostsAgainstInvoice", async () =>
	{
		// Arrange
		const fetchMock = mockApi({
			"GET /api/suppliers?pageSize=100": { body: { items: [acme], nextCursor: null } },
			"GET /api/supplier-invoices?pageSize=20": { body: { items: [invoice], nextCursor: null } },
			"POST /api/supplier-invoices/SI-1/payments": { status: 201, body: { date: "2026-01-02T00:00:00", amount: 400, method: "Cash" } }
		});
		renderEnglish(<SupplierInvoiceList />);
		await screen.findByRole("cell", { name: "SI-1" });

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Record payment" }));
		await userEvent.type(screen.getByLabelText("Amount"), "400");
		const recordButtons = screen.getAllByRole("button", { name: "Record payment" });
		await userEvent.click(recordButtons[recordButtons.length - 1]);

		// Assert
		expect(sentBody(fetchMock, "POST /api/supplier-invoices/SI-1/payments")).toMatchObject({ amount: 400, method: "Cash" });
	});
});
