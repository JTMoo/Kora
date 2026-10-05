# ADR-0036: Nota de Débito Electrónica

- Status: Proposed
- Date: 2026-10-03

## Context

- #184: PM research delta — counterpart to credit notes (#56), increases to a previously issued invoice (interest, surcharges, price corrections upward)
- Same DTE-type gap class as #162 (remission note): `DteXmlBuilder`/`ISifenGateway`/outbox have no debit-note type
- `CreditNote` has no SIFEN integration today (no `Cdc`/`TransmissionStatus`) — it predates ADR-0031. A debit note needs one from the start since the acceptance criteria require the same signing/outbox/transmission path as the invoice

## Options

- Reuse `CreditNote`'s shape (flat `Total`/`Tax`, no items, no SIFEN fields) — doesn't carry what `DteXmlBuilder` needs (itemized `gCamItem`/`gCamIVA`) and gives no CDC to transmit
- New `DebitNote` entity following the full `RemissionNote` pattern (own outbox, own `Cdc`/`TransmissionStatus`, DNIT composite `Number`) plus itemized charge lines
- Model debit note lines as full `StockItem` lines like `Invoice` — wrong shape: debit notes add charges (interest, corrections), not stock movements

## Decision

- `DebitNote` (`StockManagement.Kernel/Model`): `Number` (DNIT composite, own sequence scoped to establishment/PV, same as `RemissionNote.Number`), `Date`, `Reason` (free text), `Invoice` (required nav — the original invoice being debited), `Items` (`List<DebitNoteItem>`), `Total`/`Tax` (computed at creation from items, stored), `Cdc`, `TransmissionStatus`
- `DebitNoteItem`: `Description` (free text charge, e.g. "Intereses por mora"), `Amount` (decimal, VAT-inclusive line total), `VatRatePercent` — no `StockItem` reference, a debit note charges money, not stock
- `SifenDocumentType.NotaDeDebitoElectronica = 6` (DNIT iTiDE code; unverified, same flag as every other SIFEN constant in this project — no network access to dnit.gov.py)
- `DteXmlBuilder.BuildDebitNote`: same groups as `BuildInvoice` (`gTimb`/`gDatGralOpe`/`gDtipDE`/`gCamIVA`/`gTotSub`) plus `gCamDEAsoc` (`iTipDocAso=1`, `dCdCDERef=<original invoice's Cdc>`) linking back to the invoice — the "iTiDE + reference" the issue asks for
- `IDebitNoteService.CreateAsync` requires `Invoice.Cdc` non-empty (the invoice must already be transmitted to SIFEN) before issuing a debit note against it — can't reference a DE that doesn't exist yet on DNIT's side; throws `InvalidOperationException`, same style as `DirectDnitSifenGateway`'s other precondition checks
- `PendingDebitNoteTransmission` outbox, `IPendingDebitNoteTransmissionServiceProvider`, `SifenTransmissionWorker`/`SifenTransmissionProcessor` gain a third polling loop — same shape as the remission-note addition in #162, not merged into the existing two to keep each outbox pointing at one entity type
- `POST /debit-notes` (`Sales.Write`, 404 unknown invoice, 422 invoice not yet transmitted), `GET /debit-notes/{Number}`, `GET /debit-notes` (`Sales.Read`)

## Consequences

- `CreditNote` stays without SIFEN fields — giving it a `Cdc` is a separate change if the owner wants credit notes transmitted too, not bundled here
- Debit notes can only be issued against invoices DNIT has already accepted or that carry a contingency CDC; an invoice stuck in `Error`/`Rejected` can't be debited until resolved
