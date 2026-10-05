# ADR-0012: Stock check-in / check-out with a reason

- Status: Proposed
- Date: 2026-09-25

## Context

- #53 (parity: create/edit stock items, check-in / check-out); this ADR covers the check-in/check-out half
- #8: every check-in/check-out should carry a reason, for traceability
- #29: transaction data already has stock item, time, kind, amount (`Transaction`, added with the EF Core cutover); no reason, no requirement yet that amount changes go through it
- Today `UpdateStockItemAsync` records a `Transaction.Kind.Amount` row automatically whenever `Amount` differs, with no reason: a side effect of editing the item, not an explicit stock movement
- Sale checkout (`InvoiceServiceProvider.TryAddSaleAsync`) already decrements stock with a conditional `UPDATE ... WHERE Amount >= @amount` per line to avoid oversell under concurrent writes

## Options

- Reuse the plain edit form's Amount field for check-in/check-out: no place for a reason, and silently lets stock go negative
- New `StockMovements` concept, separate from `Transaction`: duplicates what `Transaction` already tracks
- Add `Reason` to `Transaction`, add two explicit provider methods (`CheckInStockItemAsync`, `TryCheckOutStockItemAsync`) and two endpoints, keep the existing edit-triggered `Transaction` row as is (reason stays empty there — it is not an explicit movement)

## Decision

- `Transaction` gets a `Reason` string (default `""`); only check-in/check-out endpoints set it
- `IStockItemServiceProvider`: `CheckInStockItemAsync(StockItem, int amount, string reason)` (always succeeds once the item exists) and `TryCheckOutStockItemAsync(StockItem, int amount, string reason)` (`false` when fewer than `amount` units are in stock; nothing written)
- Check-out uses the same conditional `UPDATE ... WHERE Amount >= @amount` pattern as sale checkout, in its own DB transaction with the `Transaction` insert, so a concurrent check-out can't take stock negative
- `POST /stock-items/{Id}/check-in`, `POST /stock-items/{Id}/check-out`; 404 unknown id, 409 with `{ inStock }` when check-out has insufficient stock; both require a non-empty reason and a positive amount
- React: one check-in/check-out form per stock item row, reusing the existing Amount and (new) Reason fields

## Consequences

- The edit form still does not require or expose a reason: editing `Amount` directly is treated as a correction, not a checked movement (#8 is only about check-in/check-out)
- `Transaction.Reason` empty on every row written before this ADR and on plain edits going forward
