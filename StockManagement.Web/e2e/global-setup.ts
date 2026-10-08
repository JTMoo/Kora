import { randomUUID } from "node:crypto";
import { Client } from "pg";
import { postgres } from "../playwright.config";

// No create-stock-item endpoint yet, so seed the table directly. Runs after webServer,
// so the API has already applied migrations (Database.MigrateAsync in Program.cs).
export default async function globalSetup()
{
	const client = new Client(postgres);
	await client.connect();
	await client.query('TRUNCATE "StockItems", "Customers", "Invoices", "InvoiceItems", "Payments", "Transactions", "CreditNotes", "PendingTransmissions", "PaymentLinks", "RemissionNotes", "RemissionNoteItems", "PendingRemisionTransmissions", "GoodsImportDocuments", "GoodsImportDocumentItems", "DebitNotes", "DebitNoteItems", "PendingDebitNoteTransmissions", "CancellationRequests", "InvoiceNumberVoids", "CashRegisterSessions", "CashMovements"');

	// The AddUsers migration seeds the admin with MustChangePassword = true (#244); these specs log in and
	// go straight to their own feature, not the forced change-password screen, so clear it here same as the
	// API test harness's ApiFactory.CreateAuthenticatedClientAsync does.
	await client.query('UPDATE "Users" SET "MustChangePassword" = false WHERE "Username" = \'admin\'');
	await client.query(
		'INSERT INTO "StockItems" ("Id", "Name", "Code", "Amount", "Description", "Location", "Price", "Factor", "Manufacturer", "Miscellaneous") VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10), ($11, $12, $13, $14, $15, $16, $17, $18, $19, $20)',
		[
			randomUUID(), "Screw", "A1", 10, "M6", "A-1", 5000.0, 0.0, "", "",
			randomUUID(), "Nut", "B2", 1, "M6", "B-2", 1000.0, 0.0, "", ""
		]
	);
	await client.end();
}
