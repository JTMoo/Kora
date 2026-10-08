# ADR-0049: Domestic supplier purchases update stock

- Status: Proposed
- Date: 2026-10-08

## Context

- #246: `SupplierInvoice` has no line items, never touches stock; `GoodsImportDocument` does post stock but is customs-only (`Incoterm`/`BrokerName`/`DuaReference` required)
- Need: buy locally, stock goes up, bill (AP) owed - mirrors `CreateSaleEndpoint` decrementing stock

## Options

- Reuse `GoodsImportDocument` for domestic purchases, make customs fields optional / add a dedicated `SupplierInvoiceItem` line type for domestic purchases
- Compute `SupplierInvoice.Total` from lines server-side / trust client-sent `Total`
- Check-in logic in `Sales.Core` (service layer) / directly in `SupplierInvoiceServiceProvider` (Infrastructure), same precedent as `GoodsImportDocumentServiceProvider`

## Decision

- New `SupplierInvoiceItem` (`StockItem`, `Amount`, `UnitPrice`) owned collection on `SupplierInvoice`, separate from `GoodsImportDocumentItem` - keeps customs-only fields out of the domestic path, no shared base needed (KIS)
- `CreateSupplierInvoiceRequest.Items` optional; empty -> old flat-`Total` behavior unchanged (service-only bills, no stock effect). Non-empty -> server recomputes `Total = Σ Amount*UnitPrice` (rounded to `CompanySettings.CurrencyDecimalDigits`); client-sent `Total` is ignored to avoid an AP/stock mismatch
- `SupplierInvoiceServiceProvider.AddSupplierInvoiceAsync` checks in every line's `Amount` as stock + writes a `Transaction`, atomically with the invoice insert - same transaction pattern as `GoodsImportDocumentServiceProvider.AddGoodsImportDocumentAsync`
- No VAT/Tax field added to `SupplierInvoice`: `UnitPrice` is the line's gross unit cost (VAT-inclusive, same convention as `StockItem.Price` on the sale side); an IVA-book-style VAT breakdown for domestic purchases is out of scope here
- `GoodsImportDocuments` and `SupplierInvoices` stay fully separate document types/tables/permissions - a given purchase is recorded on exactly one path, so there is no double-count risk between them

## Consequences

- Domestic purchases finally move stock; `SupplierInvoiceForm` gets a shopping-cart-style line editor (mirrors `SaleForm`)
- Existing flat-total supplier invoices (pure service bills, no stock items) keep working unchanged
- Landed-cost allocation (#120) and the purchase IVA book (#186) still only read `GoodsImportDocument`/`StockItem.PurchasePrice` - not extended to domestic lines here; follow-up if the owner wants it
