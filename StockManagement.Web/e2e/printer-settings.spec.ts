import { expect, test, type Page } from "@playwright/test";

test.use({ locale: "en-US", viewport: { width: 1280, height: 720 } });

// Screenshots for PR review (CI artifact `web-screenshots`)
const shot = (page: Page, name: string) => page.screenshot({ path: `screenshots/${name}.png`, fullPage: true });

test("printer settings: set default printer, change format, print test page", async ({ page }) =>
{
	await page.goto("/");
	await page.getByLabel("Username").fill("admin");
	await page.getByLabel("Password").fill("ChangeMe123!");
	await page.getByRole("button", { name: "User login" }).click();
	await expect(page.getByRole("cell", { name: "Screw" })).toBeVisible();

	await page.getByRole("button", { name: "Printer settings" }).click();
	await expect(page.getByLabel("Default printer name")).toBeVisible();
	await shot(page, "printer-settings-0-defaults");

	await page.getByLabel("Default printer name").fill("Front counter");
	await page.getByLabel("Receipt paper width (mm)").fill("58");
	await page.getByLabel("KuDE format").selectOption("A4");
	await page.getByRole("button", { name: "Save" }).click();
	await expect(page.getByLabel("Default printer name")).toHaveValue("Front counter");
	await shot(page, "printer-settings-1-saved");

	const testPage = await Promise.all([page.context().waitForEvent("page"), page.getByRole("button", { name: "Print test page" }).click()]);
	await expect(testPage[0].getByText("Front counter")).toBeVisible();
});
