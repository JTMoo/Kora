import { expect, test, type Page } from "@playwright/test";

test.use({ locale: "en-US", viewport: { width: 1280, height: 720 } });

// Screenshots for PR review (CI artifact `web-screenshots`)
const shot = (page: Page, name: string) => page.screenshot({ path: `screenshots/${name}.png`, fullPage: true });

test("cash register: open session, record cash movements, close with a shortfall", async ({ page }) =>
{
	await page.goto("/");
	await page.getByLabel("Username").fill("admin");
	await page.getByLabel("Password").fill("ChangeMe123!");
	await page.getByRole("button", { name: "User login" }).click();
	await expect(page.getByRole("cell", { name: "Screw" })).toBeVisible();

	await page.getByRole("button", { name: "Cash Register" }).click();
	await expect(page.getByRole("button", { name: "Open Session" })).toBeVisible();
	await shot(page, "cash-register-0-no-session");

	await page.getByLabel("Opening Float").fill("10000");
	await page.getByRole("button", { name: "Open Session" }).click();
	await expect(page.getByRole("button", { name: "Add Movement" })).toBeVisible();
	await shot(page, "cash-register-1-session-open");

	await page.getByLabel("Amount", { exact: true }).fill("2000");
	await page.getByLabel("Reason").fill("Petty cash top-up");
	await page.getByRole("button", { name: "Add Movement" }).click();
	await expect(page.getByRole("cell", { name: "Petty cash top-up" })).toBeVisible();
	await shot(page, "cash-register-2-movement-added");

	await page.getByLabel("Counted Amount").fill("11500");
	await page.getByRole("textbox", { name: "Note" }).fill("Short by 500");
	await page.getByRole("button", { name: "Close Session" }).click();

	await expect(page.getByText("Close-out report")).toBeVisible();
	await expect(page.getByText("-500").first()).toBeVisible();
	await shot(page, "cash-register-3-close-out-report");

	await page.getByRole("button", { name: "Close", exact: true }).click();
	await expect(page.getByRole("button", { name: "Open Session" })).toBeVisible();
	await expect(page.getByRole("cell", { name: "Closed", exact: true })).toBeVisible();
	await shot(page, "cash-register-4-history");
});
