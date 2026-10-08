# Decisions

Read before an important decision. Update in the same PR.

## ADRs

Generated from `docs/adr/*.md` by `scripts/generate-adr-index.sh`. Don't hand-edit the table; add/rename an ADR file and re-run the script. CI fails the PR if this is stale.

<!-- ADR-INDEX:START -->
| ADR | Decision | Status |
|---|---|---|
| [0001](adr/0001-record-architecture-decisions.md) | Decisions live in the repo as ADRs | Accepted |
| [0002](adr/0002-business-logic-in-domain-and-application.md) | Business logic in Domain and Application | Accepted |
| [0003](adr/0003-fastendpoints-api-and-react-frontend.md) | FastEndpoints API + React, desktop-first | Accepted (desktop shell open) |
| [0004](adr/0004-api-host-shape-and-tests.md) | API host shape and integration tests | Proposed |
| [0005](adr/0005-manufacturer-as-free-text.md) | Manufacturer as free text | Proposed |
| [0006](adr/0006-react-web-app-shape.md) | React web app shape | Proposed |
| [0007](adr/0007-mongo-replica-set-and-transactions.md) | MongoDB as single-node replica set, multi-step writes in transactions | Superseded by [ADR-0008](0008-ef-core-on-postgresql.md) |
| [0008](adr/0008-ef-core-on-postgresql.md) | EF Core on PostgreSQL, one context, change handlers | Proposed |
| [0009](adr/0009-web-stock-item-excel-import.md) | Web stock item Excel import | Superseded by [0018](0018-generic-import-pipeline.md) |
| [0010](adr/0010-jwt-authentication.md) | JWT bearer authentication | Proposed |
| [0011](adr/0011-retire-wpf-gui.md) | Retire the WPF GUI | Proposed |
| [0012](adr/0012-stock-check-in-check-out.md) | Stock check-in / check-out with a reason | Proposed |
| [0013](adr/0013-electron-desktop-shell.md) | Electron desktop shell | Proposed |
| [0014](adr/0014-decimal-money-type.md) | `decimal` for money, no separate Money type | Accepted |
| [0015](adr/0015-company-settings-and-vat-rate.md) | Company settings and configurable VAT rate | Proposed |
| [0016](adr/0016-electron-bundles-and-launches-api.md) | Electron shell bundles and launches the API | Proposed |
| [0017](adr/0017-roles-and-permissions.md) | Roles and permissions | Proposed |
| [0018](adr/0018-generic-import-pipeline.md) | Generic import pipeline (batches, preview → commit, undo) | Proposed |
| [0019](adr/0019-opening-stock-import.md) | Opening stock as an import target | Proposed |
| [0020](adr/0020-stock-item-purchase-price-and-exchange-rate.md) | Stock item purchase price, additional cost, and exchange rate | Proposed |
| [0021](adr/0021-ruc-validation.md) | RUC validation and name-based duplicate matching | Proposed |
| [0022](adr/0022-invoice-payments-and-status.md) | Invoice payments and status | Proposed |
| [0023](adr/0023-suppliers-and-reorder-level.md) | Suppliers and reorder level | Proposed |
| [0024](adr/0024-credit-notes-cancel-invoice.md) | Cancel an invoice with a credit note | Proposed |
| [0025](adr/0025-kora-design-system.md) | Kora design system, full replacement of the WPF look | Proposed |
| [0026](adr/0026-cross-domain-search.md) | Cross-domain search via Postgres full-text + trigram | Accepted |
| [0027](adr/0027-auto-merge-and-claude-review.md) | Unattended PR merging: required checks + Claude review, no owner click | Proposed |
| [0028](adr/0028-import-column-mapping.md) | Import column mapping | Proposed |
| [0029](adr/0029-cursor-pagination-for-list-endpoints.md) | Cursor pagination for list endpoints | Proposed |
| [0030](adr/0030-frontend-testing-strategy.md) | Frontend testing strategy | Accepted |
| [0031](adr/0031-sifen-electronic-invoicing-and-per-category-iva.md) | SIFEN electronic invoicing and per-category IVA | Proposed |
| [0034](adr/0034-open-invoice-import.md) | Open invoices as an import target | Proposed |
| [0035](adr/0035-kude-generation.md) | KuDE generation for SIFEN documents | Accepted |
| [0036](adr/0036-debit-notes.md) | Nota de Débito Electrónica | Proposed |
| [0041](adr/0041-licensing.md) | Licensing (trial + subscription) | Proposed |
| [0042](adr/0042-feedback-and-error-reporting.md) | Feedback and error reporting | Proposed |
| [0043](adr/0043-usage-telemetry.md) | Usage telemetry | Proposed |
| [0044](adr/0044-licensing-security-and-discounts.md) | Licensing security hardening and owner discounts | Proposed |
| [0046](adr/0046-auth-hardening.md) | Auth hardening - JWT key, forced password rotation, login throttling | Accepted |
| [0049](adr/0049-domestic-supplier-purchase-updates-stock.md) | Domestic supplier purchases update stock | Proposed |
<!-- ADR-INDEX:END -->

## Standing decisions

- Update by `Id`, never by business key ([#32](https://github.com/JTMoo/StockManagement/pull/32))
- Cultures: de-DE, en-US, es-PY; all UI text via resources
- Import: code repeated in one file → keep first, rest reported as duplicates (owner, 2026-09-24)
- Resources split per domain (`StockManagement.Language/{Common,Auth,Settings,Customers,StockItems,Invoices,Import,Users}`) to cut merge conflicts; each domain is its own resx set + hand-written Designer.cs class, same as `Numbers` (owner, 2026-09-26)

## Open

| Question | Options |
|---|---|
| Sequential numbers | `$inc` counter / `Max + 1` |
| `#region` + banners in new code | keep / drop |
| Nullable warnings as errors | yes / no |
| Format check in CI | `dotnet format --verify-no-changes` + `.editorconfig` naming rules / none |
