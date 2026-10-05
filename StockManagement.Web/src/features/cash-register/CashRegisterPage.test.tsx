import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { mockApi, renderEnglish, sentBody } from "../../test-utils";
import { CashRegisterPage } from "./CashRegisterPage";

const openSession = {
	id: "s1", openedAt: "2026-10-05T08:00:00", openedByUserId: "u1", openingFloat: 10000,
	closedAt: null, closedByUserId: null, countedAmount: null, note: "", status: "Open" as const,
	expectedAmount: 10000, movements: []
};

const emptyHistory = { items: [], nextCursor: null };

describe("CashRegisterPage", () =>
{
	it("NoOpenSession_ShowsOpenSessionForm", async () =>
	{
		// Arrange
		mockApi({ "GET /api/cash-register/sessions/current": { status: 404 }, "GET /api/cash-register/sessions?pageSize=20": { body: emptyHistory } });

		// Act
		renderEnglish(<CashRegisterPage />);

		// Assert
		expect(await screen.findByRole("button", { name: "Open Session" })).toBeInTheDocument();
	});

	it("OpenSession_Valid_PostsOpeningFloatAndShowsSummary", async () =>
	{
		// Arrange
		const fetchMock = mockApi({
			"GET /api/cash-register/sessions/current": { status: 404 },
			"GET /api/cash-register/sessions?pageSize=20": { body: emptyHistory },
			"POST /api/cash-register/sessions": { status: 201, body: openSession }
		});
		renderEnglish(<CashRegisterPage />);
		await screen.findByRole("button", { name: "Open Session" });

		// Act
		await userEvent.clear(screen.getByLabelText("Opening Float"));
		await userEvent.type(screen.getByLabelText("Opening Float"), "10000");
		await userEvent.click(screen.getByRole("button", { name: "Open Session" }));

		// Assert
		expect(sentBody(fetchMock, "POST /api/cash-register/sessions")).toEqual({ openingFloat: 10000 });
	});

	it("OpenSessionWithExistingOpen_ShowsSummaryAndMovementForm", async () =>
	{
		// Arrange
		mockApi({ "GET /api/cash-register/sessions/current": { body: openSession }, "GET /api/cash-register/sessions?pageSize=20": { body: emptyHistory } });

		// Act
		renderEnglish(<CashRegisterPage />);

		// Assert
		expect(await screen.findByRole("button", { name: "Add Movement" })).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "Close Session" })).toBeInTheDocument();
	});

	it("AddMovement_CashIn_PostsMovement", async () =>
	{
		// Arrange
		const fetchMock = mockApi({
			"GET /api/cash-register/sessions/current": { body: openSession },
			"GET /api/cash-register/sessions?pageSize=20": { body: emptyHistory },
			"POST /api/cash-register/sessions/s1/movements": { status: 201, body: { type: "In", amount: 500, reason: "float", date: "2026-10-05T09:00:00", createdByUserId: "u1" } }
		});
		renderEnglish(<CashRegisterPage />);
		await screen.findByRole("button", { name: "Add Movement" });

		// Act
		await userEvent.type(screen.getByLabelText("Amount"), "500");
		await userEvent.type(screen.getByLabelText("Reason"), "float");
		await userEvent.click(screen.getByRole("button", { name: "Add Movement" }));

		// Assert
		expect(sentBody(fetchMock, "POST /api/cash-register/sessions/s1/movements")).toEqual({ type: "In", amount: 500, reason: "float" });
	});

	it("CloseSession_Valid_ShowsCloseOutReport", async () =>
	{
		// Arrange
		mockApi({
			"GET /api/cash-register/sessions/current": { body: openSession },
			"GET /api/cash-register/sessions?pageSize=20": { body: emptyHistory },
			"POST /api/cash-register/sessions/s1/close": { body: { session: { ...openSession, status: "Closed", countedAmount: 9500 }, expectedAmount: 10000, countedAmount: 9500, difference: -500 } }
		});
		renderEnglish(<CashRegisterPage />);
		await screen.findByRole("button", { name: "Close Session" });

		// Act
		await userEvent.type(screen.getByLabelText("Counted Amount"), "9500");
		await userEvent.click(screen.getByRole("button", { name: "Close Session" }));

		// Assert
		expect(await screen.findByText("Close-out report")).toBeInTheDocument();
		expect(screen.getByText("-500")).toBeInTheDocument();
	});
});
