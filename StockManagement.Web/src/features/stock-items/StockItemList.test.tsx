import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { mockApi, nut, renderEnglish, screw, sentBody } from "../../test-utils";
import { StockItemList } from "./StockItemList";

describe("StockItemList", () =>
{
	it("Load_TwoItems_ShowsBoth", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [screw, nut], nextCursor: null } } });

		// Act
		renderEnglish(<StockItemList />);

		// Assert
		expect(await screen.findByRole("cell", { name: "Screw" })).toBeInTheDocument();
		expect(screen.getByRole("cell", { name: "Nut" })).toBeInTheDocument();
	});

	it("Search_ByCode_ShowsOnlyMatches", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [screw, nut], nextCursor: null } } });
		renderEnglish(<StockItemList />);
		await screen.findByRole("cell", { name: "Screw" });

		// Act
		await userEvent.type(screen.getByRole("searchbox"), "b2");

		// Assert
		expect(screen.queryByRole("cell", { name: "Screw" })).not.toBeInTheDocument();
		expect(screen.getByRole("cell", { name: "Nut" })).toBeInTheDocument();
	});

	it("Search_RegexCharacters_TreatedAsText", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [screw], nextCursor: null } } });
		renderEnglish(<StockItemList />);
		await screen.findByRole("cell", { name: "Screw" });

		// Act
		await userEvent.type(screen.getByRole("searchbox"), "(");

		// Assert
		expect(screen.queryByRole("cell", { name: "Screw" })).not.toBeInTheDocument();
	});

	it("Load_ApiDown_ShowsError", async () =>
	{
		// Arrange
		mockApi({});

		// Act
		renderEnglish(<StockItemList />);

		// Assert
		expect(await screen.findByRole("alert")).toHaveTextContent("An unexpected error occured");
	});

	it("Create_Valid_PostsAndAddsRow", async () =>
	{
		// Arrange
		const fetchMock = mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } }, "POST /api/stock-items": { status: 201, body: screw } });
		renderEnglish(<StockItemList />);

		// Act
		await userEvent.click(screen.getByRole("button", { name: "+ Add New" }));
		await userEvent.type(screen.getByLabelText("Code"), "A1");
		await userEvent.type(screen.getByLabelText("Name"), "Screw");
		await userEvent.click(screen.getByRole("button", { name: "Create Stock item" }));

		// Assert
		expect(await screen.findByRole("cell", { name: "Screw" })).toBeInTheDocument();
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		expect(sentBody(fetchMock, "POST /api/stock-items")).toEqual({ id: "", code: "A1", name: "Screw", description: "", location: "", amount: 0, price: 0, manufacturer: "", factor: 0, purchasePrice: 0, purchaseExchangeRate: 0, additionalPurchaseCost: 0, minimumStock: 0, barcode: "" });
	});

	it("Create_DuplicateCode_ShowsError", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } }, "POST /api/stock-items": { status: 409, body: { code: "A1" } } });
		renderEnglish(<StockItemList />);

		// Act
		await userEvent.click(screen.getByRole("button", { name: "+ Add New" }));
		await userEvent.type(screen.getByLabelText("Code"), "A1");
		await userEvent.type(screen.getByLabelText("Name"), "Screw");
		await userEvent.click(screen.getByRole("button", { name: "Create Stock item" }));

		// Assert
		expect(await screen.findByRole("alert")).toHaveTextContent("A stock item with this code already exists: A1");
	});

	it("AddNew_Click_OpensAndClosesDialog", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } } });
		renderEnglish(<StockItemList />);

		// Act + Assert
		await userEvent.click(screen.getByRole("button", { name: "+ Add New" }));
		expect(screen.getByRole("dialog")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "Cancel" }));
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it("Edit_Valid_UpdatesRow", async () =>
	{
		// Arrange
		const fetchMock = mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [screw], nextCursor: null } }, "PUT /api/stock-items/1": { body: { ...screw, amount: 25 } } });
		renderEnglish(<StockItemList />);
		await screen.findByRole("cell", { name: "Screw" });

		// Act
		await userEvent.click(screen.getAllByRole("button", { name: "Edit" })[0]);
		await userEvent.clear(screen.getByLabelText("Amount"));
		await userEvent.type(screen.getByLabelText("Amount"), "25");
		await userEvent.click(screen.getByRole("button", { name: "Save" }));

		// Assert
		expect(await screen.findByRole("cell", { name: "25" })).toBeInTheDocument();
		expect(sentBody(fetchMock, "PUT /api/stock-items/1")).toMatchObject({ id: "1", code: "A1", amount: 25 });
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});

	it("Delete_Confirmed_RemovesRow", async () =>
	{
		// Arrange
		const fetchMock = mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [screw, nut], nextCursor: null } }, "DELETE /api/stock-items/1": { status: 204 } });
		renderEnglish(<StockItemList />);
		await screen.findByRole("cell", { name: "Screw" });

		// Act
		await userEvent.click(screen.getAllByRole("button", { name: "Delete Item" })[0]);
		await userEvent.click(screen.getByRole("button", { name: "Confirm" }));

		// Assert
		expect(screen.queryByRole("cell", { name: "Screw" })).not.toBeInTheDocument();
		expect(screen.getByRole("cell", { name: "Nut" })).toBeInTheDocument();
		expect(fetchMock).toHaveBeenCalledWith("/api/stock-items/1", expect.objectContaining({ method: "DELETE" }));
	});

	it("Delete_Cancelled_KeepsRow", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [screw], nextCursor: null } } });
		renderEnglish(<StockItemList />);
		await screen.findByRole("cell", { name: "Screw" });

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Delete Item" }));
		await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

		// Assert
		expect(screen.getByRole("cell", { name: "Screw" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Delete Item" })).toBeInTheDocument();
	});

	it("Load_OutOfStockAndBelowMinimum_ShowStatusBadges", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [nut, { ...screw, minimumStock: 20 }], nextCursor: null } } });

		// Act
		renderEnglish(<StockItemList />);
		await screen.findByRole("cell", { name: "Screw" });

		// Assert
		expect(screen.getByText("Out of stock")).toBeInTheDocument();
		expect(screen.getByText("Low stock")).toBeInTheDocument();
	});

	it("ToggleBelowMinimum_Click_FiltersToItemsBelowTheirMinimum", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [{ ...screw, minimumStock: 20 }, { ...nut, minimumStock: 0 }], nextCursor: null } } });
		renderEnglish(<StockItemList />);
		await screen.findByRole("cell", { name: "Screw" });

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Below minimum" }));

		// Assert
		expect(screen.getByRole("cell", { name: "Screw" })).toBeInTheDocument();
		expect(screen.queryByRole("cell", { name: "Nut" })).not.toBeInTheDocument();
	});

	it("ToggleExcelImport_Click_ShowsAndHidesImportPanel", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } } });
		renderEnglish(<StockItemList />);

		// Act + Assert
		await userEvent.click(screen.getByRole("button", { name: "Excel Import" }));
		expect(screen.getByLabelText("Choose file")).toBeInTheDocument();

		await userEvent.click(screen.getByRole("button", { name: "Excel Import" }));
		expect(screen.queryByLabelText("Choose file")).not.toBeInTheDocument();
	});

	it("Import_Successful_RefreshesList", async () =>
	{
		// Arrange
		const batch = {
			id: "batch-1",
			target: "StockItems",
			fileName: "stock.xlsx",
			sheetName: "Sheet1",
			status: "Previewed",
			readyCount: 1,
			duplicateCount: 0,
			errorCount: 0,
			rows: [{ row: 2, status: "Ready", fields: { Code: "A1" } }]
		};
		const fetchMock = mockApi({
			"GET /api/stock-items?pageSize=100": { body: { items: [], nextCursor: null } },
			"POST /api/import/batches/columns": { body: { sheetName: "Sheet1", columns: [], fields: [] } },
			"POST /api/import/batches": { body: batch },
			"POST /api/import/batches/batch-1/commit": { body: { ...batch, status: "Committed" } }
		});
		renderEnglish(<StockItemList />);
		await userEvent.click(screen.getByRole("button", { name: "Excel Import" }));

		const file = new File(["dummy"], "stock.xlsx");
		await userEvent.upload(screen.getByLabelText("Choose file"), file);
		await userEvent.click(screen.getByRole("button", { name: "Preview" }));
		await userEvent.click(await screen.findByRole("button", { name: "Preview" }));

		// Act
		await userEvent.click(await screen.findByRole("button", { name: "Commit" }));

		// Assert
		expect(await screen.findByRole("button", { name: "Undo" })).toBeInTheDocument();
		expect(fetchMock.mock.calls.filter(([url, init]) => `${init?.method ?? "GET"} ${url}` === "GET /api/stock-items?pageSize=100")).toHaveLength(2);
	});

	it("CheckIn_Valid_PostsAndUpdatesAmount", async () =>
	{
		// Arrange
		const fetchMock = mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [screw], nextCursor: null } }, "POST /api/stock-items/1/check-in": { body: { ...screw, amount: 15 } } });
		renderEnglish(<StockItemList />);
		await screen.findByRole("cell", { name: "Screw" });

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Check In/Out" }));
		const amountField = screen.getByLabelText("Amount");
		await userEvent.clear(amountField);
		await userEvent.type(amountField, "5");
		await userEvent.type(screen.getByLabelText("Reason"), "Delivery");
		await userEvent.click(screen.getByRole("button", { name: "Check In" }));

		// Assert
		expect(await screen.findByRole("cell", { name: "15" })).toBeInTheDocument();
		expect(sentBody(fetchMock, "POST /api/stock-items/1/check-in")).toEqual({ amount: 5, reason: "Delivery" });
	});

	it("CheckOut_InsufficientStock_ShowsError", async () =>
	{
		// Arrange
		mockApi({ "GET /api/stock-items?pageSize=100": { body: { items: [screw], nextCursor: null } }, "POST /api/stock-items/1/check-out": { status: 409, body: { inStock: 2 } } });
		renderEnglish(<StockItemList />);
		await screen.findByRole("cell", { name: "Screw" });

		// Act
		await userEvent.click(screen.getByRole("button", { name: "Check In/Out" }));
		await userEvent.type(screen.getByLabelText("Reason"), "Damaged");
		await userEvent.click(screen.getByRole("button", { name: "Check Out" }));

		// Assert
		expect(await screen.findByRole("alert")).toHaveTextContent("Not enough in stock, only available: 2");
	});
});
