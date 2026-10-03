import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ana, mockApi, remissionNote, renderEnglish, screw, sentBody } from "../../test-utils";
import { RemissionNoteList } from "./RemissionNoteList";

describe("RemissionNoteList", () =>
{
	it("Load_OneRemissionNote_ShowsIt", async () =>
	{
		// Arrange
		mockApi({ "GET /api/remission-notes": { body: { items: [remissionNote] } } });

		// Act
		renderEnglish(<RemissionNoteList onSelect={vi.fn()} />);

		// Assert
		expect(await screen.findByRole("cell", { name: "Ana Gómez" })).toBeInTheDocument();
	});

	it("Create_Valid_PostsAndAddsRow", async () =>
	{
		// Arrange
		const fetchMock = mockApi({
			"GET /api/remission-notes": { body: { items: [] } },
			"GET /api/customers?pageSize=100": { body: { items: [ana], nextCursor: null } },
			"GET /api/stock-items?pageSize=100": { body: { items: [screw], nextCursor: null } },
			"POST /api/remission-notes": { status: 201, body: remissionNote }
		});
		renderEnglish(<RemissionNoteList onSelect={vi.fn()} />);

		// Act
		await userEvent.click(screen.getByRole("button", { name: "+ Add New" }));
		await userEvent.selectOptions(screen.getByLabelText("Customer"), "1001");
		await userEvent.type(screen.getByLabelText("Destination Address"), "Calle Falsa 123");
		await userEvent.selectOptions(screen.getByLabelText("Stock item"), "A1");
		await userEvent.click(screen.getByRole("button", { name: "Add to Shopping Cart" }));
		await userEvent.click(screen.getByRole("button", { name: "Create Remission Note" }));

		// Assert
		expect(await screen.findByRole("cell", { name: "Ana Gómez" })).toBeInTheDocument();
		expect(sentBody(fetchMock, "POST /api/remission-notes")).toEqual({ customerId: 1001, reason: "Venta", destinationAddress: "Calle Falsa 123", items: [{ code: "A1", amount: 1 }] });
	});
});
