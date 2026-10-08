import { expect, test, type Page } from "@playwright/test";

test.use({ locale: "en-US", viewport: { width: 1280, height: 720 } });

// Screenshots for PR review (CI artifact `web-screenshots`)
const shot = (page: Page, name: string) => page.screenshot({ path: `screenshots/${name}.png`, fullPage: true });

test("full sale: create customer, sell, view invoice, stock goes down", async ({ page }) =>
{
	await page.goto("/");
	await expect(page.getByRole("button", { name: "User login" })).toBeVisible();
	await shot(page, "0-login");

	await page.getByLabel("Username").fill("admin");
	await page.getByLabel("Password").fill("ChangeMe123!");
	await page.getByRole("button", { name: "User login" }).click();

	await expect(page.getByRole("cell", { name: "Screw" })).toBeVisible();
	await shot(page, "1-stock-items");

	await page.getByRole("button", { name: "Clients" }).click();
	await page.getByRole("button", { name: "+ Add New" }).click();
	await page.getByLabel("Name", { exact: true }).fill("Ana");
	await page.getByLabel("Lastname").fill("Gómez");
	await page.getByRole("button", { name: "Create customer" }).click();
	await expect(page.getByRole("cell", { name: "Gómez" })).toBeVisible();
	await shot(page, "2-customers");

	await page.getByRole("button", { name: "New sale" }).click();
	const customerId = await page.getByRole("option", { name: "Ana Gómez" }).getAttribute("value");
	await page.getByLabel("Customer").selectOption(customerId!);
	await page.getByRole("combobox", { name: /^Stock item/ }).selectOption("A1");
	await page.getByLabel("Quantity").fill("2");
	await page.getByRole("button", { name: "Add to shopping cart" }).click();
	await shot(page, "3-new-sale");
	await page.getByRole("button", { name: "Sell" }).click();

	await expect(page.getByRole("article")).toContainText("Screw");
	await expect(page.getByRole("article")).toContainText("Cash");
	await expect(page.getByTestId("invoice-total")).toHaveText("10,000");
	await shot(page, "4-invoice");

	await page.getByRole("button", { name: "Back", exact: true }).click();
	await expect(page.getByRole("cell", { name: "Ana Gómez" })).toBeVisible();
	await shot(page, "5-invoice-list");

	await page.getByRole("button", { name: "Stock items" }).click();
	await expect(page.getByRole("row", { name: /Screw A1/ })).toContainText("8");

	const screwRow = page.getByRole("row", { name: /Screw A1/ });
	await screwRow.getByRole("button", { name: "Check In/Out" }).click();
	const checkAmount = page.getByLabel("Amount").last();
	await checkAmount.fill("5");
	await page.getByLabel("Reason").fill("Restock");
	await shot(page, "5-check-in");
	await page.getByRole("button", { name: "Check In", exact: true }).click();
	await expect(screwRow).toContainText("13");

	await screwRow.getByRole("button", { name: "Check In/Out" }).click();
	await checkAmount.fill("2");
	await page.getByLabel("Reason").fill("Damaged");
	await page.getByRole("button", { name: "Check Out", exact: true }).click();
	await expect(screwRow).toContainText("11");

	await page.getByRole("button", { name: "Settings", exact: true }).click();
	await shot(page, "5-settings");
	await page.locator("main").getByLabel("Select language:").selectOption("es-PY");
	await expect(page.getByRole("heading", { name: "Configuracion" })).toBeVisible();

	await page.getByRole("button", { name: "Artículos" }).click();
	await page.getByRole("button", { name: "Importación de Excel" }).click();
	await expect(page.getByRole("button", { name: "Vista previa" })).toBeDisabled();
	await shot(page, "6-excel-import");
});
