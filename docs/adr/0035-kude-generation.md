# ADR-0035: KuDE generation for SIFEN documents

- Status: Accepted
- Date: 2026-10-03

## Context

- #183, cycle-4 PM finding. `Sifen.Core` builds/transmits XML DTEs (invoice, remission note) but produces nothing a human can read
- DNIT requires a KuDE (graphic representation, PDF/HTML + QR to DNIT's verification page) for any recipient not itself SIFEN-obligated — the common retail/cash-customer case. Without it a legally-transmitted invoice has nothing to hand the customer
- ADR-0031 already decided: "KuDE ... generated on demand from the signed XML + CDC, never persisted as a stored file"
- `Invoice.Cdc`/`RemissionNote.Cdc` are empty until a CDC exists: contingency mode assigns one at sale time ([[sifen-contingency-mode]]); otherwise it's assigned lazily by `DirectDnitSifenGateway` when the outbox worker transmits. A KuDE needs a real CDC, so it can't be produced before that
- No SIFEN DTE support exists yet for credit notes, so no KuDE for them either (#56 predates ADR-0031)
- `CompanySettings.EstablishmentAddress` doesn't exist — both `DirectDnitSifenGateway` call sites hardcode `DteEmisor.EstablishmentAddress` to `""`, a known gap. A KuDE needs a real issuer address

## Options

- Representation format: PDF (binary, needs a PDF-generation library) / HTML (text, browser prints/saves to PDF)
- QR payload: full DNIT `cHashQR` scheme (needs a DNIT-issued CSC secret we don't model yet) / a best-effort verification URL carrying only fields we can honestly populate
- Scope: invoice + remission note (what's shipped) / also credit notes (no SIFEN DTE support yet — out of scope regardless)

## Decision

### Representation: HTML, not PDF

- `StockManagement.Web` is already browser-viewable (ADR-0003); the browser's own print dialog turns the KuDE into a PDF or paper copy without Kora owning PDF layout/pagination
- Avoids a new PDF-rendering dependency (and its licensing/footprint) for a feature that's legally satisfied by a printable page — KIS
- Generated on demand from `DteInvoiceData`/`DteRemisionData` — the same contracts `IDteXmlBuilder` already consumes — never persisted, consistent with ADR-0031

### QR: QRCoder, best-effort payload

- `QRCoder` (MIT, pure managed, no native deps) renders the QR as a PNG, embedded as a `data:` URI in the HTML — no file on disk, same "never persisted" rule
- DNIT's published KuDE QR encodes a verification URL with a `cHashQR` computed from a contributor-specific CSC (Código de Seguridad del Contribuyente) secret Kora doesn't hold or model yet (separate from `CdcInput.SecurityCode`, which is just a CDC field). Fabricating a hash would make the QR point somewhere actively wrong
- Ships with the fields honestly known today (`Id`=CDC, `dFeEmiDE`, `dRucRec`, `dTotGralOpe`, `dTotIVA`): a reader can see the CDC and amounts; it does **not** yet verify against DNIT's online checker. Flagged, not faked — same pattern as ADR-0031's other unverified SIFEN constants
- **Blocked on owner**: obtaining a CSC and completing the hash once DNIT access is available

### Scope

- `IKudeHtmlBuilder` (new, `Sifen.Core.Contracts`): `BuildInvoice(DteInvoiceData, string qrDataUri)`, `BuildRemision(DteRemisionData, string qrDataUri)` — pure string builders, same shape/layering as `IDteXmlBuilder`
- `KudeDataMapper` (new, `Sifen.Core`): entity (`Invoice`/`RemissionNote`) + `CompanySettings` → `DteInvoiceData`/`DteRemisionData`, requiring `Cdc` already set (throws otherwise — no new CDC is minted for a KuDE, it would disagree with whatever was actually transmitted). Kept separate from `DirectDnitSifenGateway`'s own mapping (which *does* mint a CDC when missing) rather than merged, to avoid touching already-shipped transmission code for this
- API: `GET /invoices/{Number}/kude`, `GET /remission-notes/{Number}/kude`, `text/html`, `Sales.Read` — 404 unknown document, 409 if `Cdc` is still empty ("not transmitted/issued yet")
- `CompanySettings.EstablishmentAddress` added (closes the `""` gap) and wired into both `DirectDnitSifenGateway` call sites — needed for the KuDE issuer block and for the DTE XML itself (`dDirEmi` was always meant to carry it)

## Consequences

- No credit-note KuDE until credit notes get SIFEN DTE support (separate, unfiled gap)
- QR is not yet DNIT-verifiable; a customer or auditor scanning it gets amounts/CDC but not a live DNIT confirmation page until the CSC is wired in
- Printing/saving as PDF is the browser's job, not Kora's — no page-size/margin control beyond standard CSS print rules
