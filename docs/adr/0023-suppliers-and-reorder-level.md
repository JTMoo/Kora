# ADR-0023: Suppliers and reorder level

- Status: Proposed
- Date: 2026-09-27

## Context

- #57: purchases (#25, not yet built) need a supplier (name, contact, country, currency, lead time); stock items need a preferred supplier and a minimum stock to flag reorder candidates
- Every existing top-level entity (`Customer`, `Invoice`, `StockItem`) lives as a `Kernel.Model` class + `I*ServiceProvider` interface + EF `Ef*ServiceProvider`; only entities with actual business logic beyond CRUD (e.g. `Customer`'s sequential id) get a `*.Core`/`*.Core.Contracts` project
- `StockItem` itself has no `Core` project — its endpoints call `IStockItemServiceProvider` directly

## Options

- Add a `Suppliers.Core`/`Suppliers.Core.Contracts` pair even though there's no logic beyond CRUD (matches `Customers`, adds an empty pass-through layer) / skip the `Core` layer and call `ISupplierServiceProvider` straight from the endpoints (matches `StockItems`, KIS rule 3)
- `StockItem.SupplierId`: a real FK (`HasOne(...).WithMany()`, `ON DELETE` enforced by Postgres) / a loose string with no DB-level relation
- Deleting a supplier assigned to stock items: block it (409) / null out the reference on affected items

## Decision

- No `Suppliers.Core`: `ISupplierServiceProvider` (Kernel) is called directly from `StockManagement.Api/Features/Suppliers/*`, same shape as `StockItems`
- `StockItem` gets `SupplierId` (nullable FK) + `Supplier` navigation + `MinimumStock` (int); `Supplier` is a normal `BaseDocument` with a unique `Name` index (same conflict-on-duplicate pattern as `StockItem.Code`)
- Deleting an in-use supplier is blocked: Postgres FK violation (`23503`) is caught in `SupplierServiceProvider` and mapped to `SupplierInUseException` → 409, same pattern as the existing `23505` → `*AlreadyExistsException` mapping
- New permissions `Suppliers.Read`/`Suppliers.Write` (ADR-0017)
- `IStockItemServiceProvider.GetStockItemsBelowMinimumAsync()` + `GET /stock-items/below-minimum`: items with `MinimumStock > 0` and `Amount < MinimumStock`, for the web "below minimum" list; also usable to prefill a purchase once #25 exists
- Purchases (#25) and purchase price/exchange rate on stock items (#28) are out of scope here

## Consequences

- Consistent with `StockItems`: an entity with no business rules beyond CRUD gets no `Core` project, only `Kernel` + `Infrastructure` + `Api`
- The FK means a supplier can't be deleted while referenced; the UI surfaces this as a plain conflict message rather than silently orphaning stock items
- `#25` will need to read `StockItem.SupplierId`/`MinimumStock` and the below-minimum endpoint when it lands
