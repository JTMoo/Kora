# ADR-0008: EF Core on PostgreSQL, one context, change handlers

- Status: Proposed
- Date: 2026-09-24
- Issue: [#65](https://github.com/JTMoo/StockManagement/issues/65)

## Context

- Hand-written Mongo layer per entity (`*ServiceProvider`, `DatabaseManager`)
- ERP data is relational: invoices → customers → stock items, ledger, receivables
- Multi-step writes not atomic (#41); Mongo transactions need a replica set
- Owner wants one context with all sets and loosely coupled reactions to entity changes

## Options

| Option | For | Against |
|---|---|---|
| EF Core + MongoDB provider | no data move | no joins/`Include`, no migrations, no `ExecuteUpdate`, replica set for transactions |
| EF Core + PostgreSQL (Npgsql) | FKs, joins, migrations, transactions, `numeric` money | data move from Mongo, Postgres on customer machine |
| Keep Mongo driver | nothing to do | no change tracking, no unit of work |

## Decision

- EF Core + PostgreSQL, new `StockManagement.Infrastructure` project
- One `AppDbContext`, one `DbSet` per entity, mapping via `IEntityTypeConfiguration<T>`
- Reactions: `IEntityChangedHandler<T>` (Kernel), called in `SaveChangesAsync` before the commit
  - Rounds until handlers make no new changes (max 10), then one save = one transaction
  - Handler throws → nothing saved
  - Each entity handled once per change type per save
- Connection: `ConnectionStrings:Postgres`

## Consequences

- Migration per entity: Mongo `*ServiceProvider` → `AppDbContext`, then drop MongoDB.Driver
- Existing installs: one-off Mongo → Postgres import
- `$inc` counters and conditional stock `$inc` (#41) → `ExecuteUpdateAsync` or concurrency tokens
- Duplicate key → `DbUpdateException` (Postgres `23505`) mapped to domain error in Infrastructure
- Schema via EF migrations once the first entity is live (slice 1 uses `EnsureCreated` in tests)
- API tests: Testcontainers PostgreSQL
- Installer ships or requires PostgreSQL
- Sync `SaveChanges` throws: handlers are async

## Slice 2 (StockItem)

- `StockItemServiceProvider` (Infrastructure) implements Kernel's `IStockItemServiceProvider`; `BaseDocument.Id` made `public` (was `internal`, per ADR-0004's own follow-up) so Infrastructure can update by Id
- `IStockItemServiceProvider` no longer returns Mongo's `ReplaceOneResult`/`DeleteResult`; both writes now return `int` (1 on success, throws otherwise)
- Migrations: `dotnet ef migrations add`, `StockManagement.Infrastructure/Database/Migrations`, design-time factory since `AppDbContext` takes `IServiceProvider`
- `Transaction.Time` mapped `timestamp without time zone`: `DateTime.Now` is local, Npgsql only accepts UTC for `timestamptz`
- `Transaction.Invoice` not mapped yet (Invoice isn't in Postgres); ignored in `TransactionConfiguration`
- Gotcha: adding a dependent entity before updating/removing its principal marks the principal `Added` via graph fixup; `StockItemServiceProvider` updates/removes the `StockItem` first, then adds the `Transaction`
- **Not done**: `StockItemServiceProvider` isn't wired into the API or GUI. `InvoiceServiceProvider.TryAddSaleAsync` decrements stock and writes the invoice in one Mongo session transaction (ADR-0007); moving `StockItem` alone would split that atomic write across two databases. Wiring StockItem into the running app needs that crossing solved first — customers/invoices next, or an outbox/saga for the sale — asked the owner rather than picking silently

## Slice 3 (full API cutover: Customer, Invoice, atomic sale)

- Owner decision: move all of it to Postgres, no production data to migrate ("not in production yet") — supersedes ADR-0007's Mongo session transaction
- `CustomerServiceProvider`, `InvoiceServiceProvider` added alongside `StockItemServiceProvider`; API host (`Program.cs`) wired fully onto `AppDbContext`, Mongo removed from the API and its tests
- `Invoice.Items` (`List<ShoppingCartItem>`, not a `BaseDocument`) mapped `OwnsMany`, own `InvoiceItems` table, shadow `Guid Id`, required FK to `StockItem` (a non-owned entity — an owned type can still reference a regular one); `AutoInclude()` on `Invoice.Items`, `Invoice.Customer`, and the owned item's `StockItem` navigation, so a read returns the full graph
- Sale atomicity (replaces ADR-0007's Mongo session transaction): explicit `Database.BeginTransactionAsync`, one conditional `ExecuteUpdateAsync` per line (`WHERE Code == code AND Amount >= amount`, oversell-safe under concurrency — verified with a real concurrent `Task.WhenAll` test), then `SaveChangesAsync` for the `Transaction`/`Invoice` inserts, then `CommitAsync`; any line short → `RollbackAsync`, nothing written
- Duplicate invoice number → `DbUpdateException` (Postgres `23505`) → `InvoiceNumberAlreadyExistsException`, same pattern as `StockItemCodeAlreadyExistsException`/`CustomerIdAlreadyExistsException`
- `ISaleService`/`ICustomerService` stay `AddSingleton` in the shared `Sales.Core`/`Customers.Core` extensions (used by the WPF GUI too, which validates DI scopes at build); the API overrides them to `Scoped` in `Program.cs` only, via a reflection-based `ServiceDescriptor` swap (`MakeScoped<T>`) — needed because the EF providers they now depend on are Scoped (`AppDbContext` isn't thread-safe) and a Singleton can't consume a Scoped service
- **Not done**: the WPF GUI (still Mongo-backed for everything, including `User`/`Settings`) — this session can't build or verify WPF in a Linux container; flagged to the owner rather than rewired blind
- Migrations regenerated (`InitialCreate`) to include `Customers`, `Invoices`, `InvoiceItems` alongside `StockItems`, `Transactions`
- `PostgresContainer.cs` (Api.Tests): one shared assembly-level Testcontainers Postgres, mirroring the old `MongoContainer.cs`; `MongoContainer.cs` and the `Testcontainers.MongoDb` package reference deleted
