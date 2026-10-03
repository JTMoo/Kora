// Turns per-domain StockManagement.Language resx files into src/i18n/*.json (neutral + culture overrides).
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";

const domains = ["Common", "Auth", "Settings", "Customers", "StockItems", "Invoices", "Import", "Users", "Suppliers", "Reports"];
const source = new URL("../../StockManagement.Language/", import.meta.url);
const target = new URL("../src/i18n/", import.meta.url);
const entities = { amp: "&", lt: "<", gt: ">", quot: '"', apos: "'" };

function read(file) {
	const xml = readFileSync(new URL(file, source), "utf8").replace(/<!--[\s\S]*?-->/g, "");
	const texts = {};
	for (const [, key, value] of xml.matchAll(/<data name="(\w+)" xml:space="preserve">\s*<value>([\s\S]*?)<\/value>/g))
		texts[key] = value.replace(/&(\w+);/g, (match, name) => entities[name] ?? match);
	return texts;
}

function readDomain(domain, culture) {
	return read(`${domain}/${domain}${culture ? `.${culture}` : ""}.resx`);
}

const neutral = Object.assign({}, ...domains.map((domain) => readDomain(domain)));
mkdirSync(target, { recursive: true });
for (const culture of ["de-DE", "en-US", "es-PY"])
	writeFileSync(
		new URL(`${culture}.json`, target),
		JSON.stringify({ ...neutral, ...Object.assign({}, ...domains.map((domain) => readDomain(domain, culture))) }, null, "\t"),
	);
