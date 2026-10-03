import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { acme, importDoc, mockApi, renderEnglish, screw, sentBody } from "../../test-utils";
import { GoodsImportDocumentList } from "./GoodsImportDocumentList";

describe("GoodsImportDocumentList", () =>
{
	it("Load_OneDocument_ShowsIt", async () =>
	{
		// Arrange
		mockApi({ "GET /api/goods-import-documents?pageSize=20": { body: { items: [importDoc], nextCursor: null } } });

		// Act
		renderEnglish(<GoodsImportDocumentList onSelect={() => {}} />);

		// Assert
		expect(await screen.findByRole("cell", { name: "Despachante SA" })).toBeInTheDocument();
	});

	it("Create_Valid_PostsAndShowsToast", async () =>
	{
		// Arrange
		const fetchMock = mockApi({
			"GET /api/goods-import-documents?pageSize=20": { body: { items: [], nextCursor: null } },
			"GET /api/suppliers?pageSize=100": { body: { items: [acme], nextCursor: null } },
			"GET /api/stock-items?pageSize=100": { body: { items: [screw], nextCursor: null } },
			"POST /api/goods-import-documents": { status: 201, body: importDoc }
		});
		renderEnglish(<GoodsImportDocumentList onSelect={() => {}} />);

		// Act
		await userEvent.click(screen.getByRole("button", { name: "+ Add New" }));
		await userEvent.type(screen.getByLabelText("Proforma Number"), "PF-001");
		await userEvent.selectOptions(screen.getByLabelText("Supplier"), "1");
		await userEvent.type(screen.getByLabelText("Broker Name"), "Despachante SA");
		await userEvent.type(screen.getByLabelText("DUA Reference"), "DUA-001");
		await userEvent.selectOptions(screen.getByLabelText("Stock item"), "A1");
		await userEvent.click(screen.getByRole("button", { name: "Add to Shopping Cart" }));
		await userEvent.click(screen.getByRole("button", { name: "Create Goods Import Document" }));

		// Assert
		expect(await screen.findByText("Goods import document PF-001 saved.")).toBeInTheDocument();
		expect(sentBody(fetchMock, "POST /api/goods-import-documents")).toEqual({ proformaNumber: "PF-001", supplierId: "1", incoterm: "Fob", brokerName: "Despachante SA", duaReference: "DUA-001", items: [{ code: "A1", amount: 1 }] });
	});

	it("Create_DuplicateProformaNumber_ShowsError", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/goods-import-documents?pageSize=20": { body: { items: [], nextCursor: null } },
			"GET /api/suppliers?pageSize=100": { body: { items: [acme], nextCursor: null } },
			"GET /api/stock-items?pageSize=100": { body: { items: [screw], nextCursor: null } },
			"POST /api/goods-import-documents": { status: 409, body: { proformaNumber: "PF-001" } }
		});
		renderEnglish(<GoodsImportDocumentList onSelect={() => {}} />);

		// Act
		await userEvent.click(screen.getByRole("button", { name: "+ Add New" }));
		await userEvent.type(screen.getByLabelText("Proforma Number"), "PF-001");
		await userEvent.selectOptions(screen.getByLabelText("Supplier"), "1");
		await userEvent.type(screen.getByLabelText("Broker Name"), "Despachante SA");
		await userEvent.type(screen.getByLabelText("DUA Reference"), "DUA-001");
		await userEvent.selectOptions(screen.getByLabelText("Stock item"), "A1");
		await userEvent.click(screen.getByRole("button", { name: "Add to Shopping Cart" }));
		await userEvent.click(screen.getByRole("button", { name: "Create Goods Import Document" }));

		// Assert
		expect(await screen.findByRole("alert")).toHaveTextContent("A goods import document with this proforma number already exists: PF-001");
	});
});
