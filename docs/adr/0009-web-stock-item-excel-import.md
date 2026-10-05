# ADR-0009: Web stock item Excel import

- Status: Superseded by [0018](0018-generic-import-pipeline.md)
- Date: 2026-09-24

## Context

- WPF's `TableMappingViewModel` opens the file locally, then the user manually maps each Excel column to a `StockItem` property, previews the duplicate count, and confirms
- A browser can't open a local `OpenFileDialog` into server-side processing the same way; the file has to be uploaded
- `IStockItemImportService` (`StockManagement.Import.Core`) already holds the duplicate-split and insert logic ([#37](https://github.com/JTMoo/StockManagement/pull/37)) and is DI-registered against `IStockItemServiceProvider`, which the API already has bound to `StockItemServiceProvider` — reusable as-is
- Standing decision: import keeps the first row for a repeated code, reports the rest as duplicates

## Options

- Column mapping: keep the WPF two-step wizard (list sheets/headers, let the user map columns, then commit) / auto-match headers to `StockItem` properties by name and drop manual mapping
- Commit step: upload → preview → separate confirm call / one upload call that parses, dedupes and imports in a single request
- Sheet selection: let the user pick a worksheet / always use the first one

## Decision

- One `POST /stock-items/import` endpoint: multipart file upload, parses the first worksheet with ClosedXML, matches headers to `StockItem` properties by name (English or the localized `Display` name, case-insensitive), splits duplicates and imports in the same call
- New `IExcelStockItemParser` in `StockManagement.Import.Core` returns parsed items plus one `StockItemImportRowError` per row that failed conversion (row number + message), reusing `Language.Resources.failedConversion` and `TypeExtensions.TryConvertFromString`
- Response reports imported count, duplicate count, and the per-row errors; no manual mapping UI, no separate preview/confirm round trip
- React: file input + upload button on a new "Import" screen, results shown as counts plus an error table

## Consequences

- A workbook whose headers don't match a property name (or its translation) silently skips that column instead of asking the user to map it; renaming a header to match is the workaround
- Only the first worksheet is read; multi-sheet workbooks need the data on sheet one
- No dry-run: a large bad file has to be fixed and re-uploaded rather than re-mapped in place
- `TableMappingViewModel`/`ExcelImportDialogViewModel` (WPF) are unaffected; they keep the manual mapping flow until the WPF GUI is retired
