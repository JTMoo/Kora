# Code quality + dead code audit

Cycle 1, 2026-10-02. Repo: JTMoo/Kora @ main (6ea47fb). Recurring — rerun after each merge batch, append a new dated section below instead of rewriting.

## Scope

- C#: Kernel, `*.Core` + `*.Core.Contracts`, Infrastructure, Api, Api.Tests, Tests, Language
- React: StockManagement.Web
- Electron/scripts/CI: StockManagement.Desktop, scripts/, .github/workflows/, top-level config

Tooling check: no .NET analyzers configured beyond SDK defaults; `tsconfig.json` has `noUnusedLocals`/`noUnusedParameters: true` (catches basic TS dead code in CI already — not re-reported here). No ESLint config.

## Dedup against existing issues

Already filed, not re-reported: #112 (Kernel dead code), #97 (`ISaleService` Singleton/Scoped), #76 (no Ef-prefix).

## Dead code (all grep-verified repo-wide)

| # | File | What | Size | Issue |
|---|------|------|------|-------|
| 1 | `StockManagement.Kernel/Model/ExtensionMethods/ObservableCollectionExtensions.cs` | `EqualizeTo<T>` on `ObservableCollection<T>` — WPF-only type, zero refs | S | #112 |
| 2 | `StockManagement.Kernel/Model/ExtensionMethods/EnumExtensions.cs` | `GetEnumDescription` — zero refs | S | #112 |
| 3 | `StockManagement.Kernel/Model/ExtensionMethods/ListExtensions.cs` | `EqualizeTo<T>`, `RemoveUnavailableItems` — zero refs | S | #112 |
| 4 | `StockManagement.Kernel/Model/ExtensionMethods/IEnumerableExtensions.cs` + `StockManagement.Tests/Kernel/IEnumerableExtensionsTests.cs` | `ConvertToShoppingCartList`, `Where(List<Func<T,bool>>)` — zero production callers, only self-test | S | #112 |
| 5 | `StockManagement.Kernel/Model/ExtensionMethods/StringExtensions.cs` + `StockManagement.Tests/Kernel/StringExtensionsTests.cs` | `ReplaceLineBreakWithWhitespace`, `MatchesSearch` — zero production callers, only self-test | S | #112 |
| 6 | `StockManagement.Kernel/StockManagement.Kernel.csproj` | Unused `PackageReference`: `ClosedXML`, `Microsoft.Extensions.Configuration.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions` | S | #112 |
| 7 | `StockManagement.Web/src/api/stockItems.ts:9` | `listStockItemsBelowMinimum` exported, zero callers — `StockItemList.tsx` reimplements the filter client-side instead | S | new |
| 8 | `StockManagement.Web/src/index.css` | `button.danger`/`.danger` selector — zero className usage | S | new (ties into quality #1 below) |

Not dead, contradicts/refines original #112 framing: `BaseDocument`/`ManufacturerSerializer`/`MongoDB.Driver` are correctly kept, but it's not just `Model/StockItem` reusing the Mongo POCO as EF entity — **every** `Kernel.Model.*` class (`Invoice`, `User`, `Customer`, `CreditNote`, `AppSettings`, `Transaction`, `Supplier`, `ImportBatch`) is a live `DbSet<T>` in `AppDbContext.cs:24-40`. `NotificationBase` (WPF `INotifyPropertyChanged`) still backs `ShoppingCartItem`/`Payment` but nothing in the API consumes `PropertyChanged` — not zero-ref so not listed as dead, worth a call in #112 on dropping it to plain auto-properties.

Electron/scripts/CI: **no dead code found.** Every script/workflow verified referenced; `Delivery.yml` confirmed gone (replaced by `Release.yml`), nothing left pointing at it except a stale CLAUDE.md line (see below).

## Quality issues (ranked, highest impact first)

1. **[HIGH, M] DI lifetime mismatch — same bug class as #97, much wider.** Every `*.Core` module except `CreditNoteService` registers `AddSingleton` while depending on a `Kernel` `I*ServiceProvider` that Infrastructure registers `AddScoped`:
   - `Auth.Core`: `AuthService` → `IUserServiceProvider`
   - `Customers.Core`: `CustomerService` → `ICustomerServiceProvider`
   - `Import.Core`: `StockItemImportService`, all 3 `IImportTargetHandler`s, `ImportBatchService` → scoped providers
   - `Sales.Core`: `SaleService` (#97), `PaymentService` → `IInvoiceServiceProvider`
   - `Settings.Core`: `SettingsService` → `ISettingsServiceProvider`

   Hidden because `StockManagement.Tests/ServiceRegistrationTests.cs:26-32` mocks every provider `AddSingleton` instead of `AddScoped`, so `ValidateOnBuild` never trips in tests. Fix: switch all to `AddScoped`; fix the test to register mocks `AddScoped` inside a scope so it actually catches this class of bug going forward. One PR, resolves alongside #97.

2. **[HIGH, L] Missing `CancellationToken` on async Kernel/Infrastructure methods.** CLAUDE.md requires it; 6 of 8 provider interfaces have zero `CancellationToken` params across ~35 methods (`ICustomerServiceProvider`, `IImportBatchServiceProvider`, `ISettingsServiceProvider`, `IStockItemServiceProvider`, `ISupplierServiceProvider`, `IUserServiceProvider`) and their `Ef*ServiceProvider` implementations don't thread one into EF async calls. FastEndpoints already has a token at the endpoint; it's dropped at the provider boundary. Mechanical but touches interfaces + implementations + call sites + test mocks.

3. **[M] Delete actions bypass the Kora design system's `danger` Button + confirm step.** `StockItemList.tsx:48`, `SupplierList.tsx:24` use `window.confirm()` + `.quiet` button; `UserList.tsx:52` has no confirm step at all before delete. Design doc reserves `danger` fill + an in-page confirm step for exactly this case (see `InvoiceView.tsx:76-90`'s cancel-invoice pattern for a model to reuse).

4. **[M] Stock status shown via border-only CSS, not `StatusBadge`.** `StockItemList.tsx:60-62` marks below-minimum/sold-out rows with a border class only — design doc explicitly says "pair with `StatusBadge`, never the border alone." Same gap on invoice status (`InvoiceView.tsx:52-53`, plain `<span>`; `InvoiceList.tsx` shows no status at all) — one `StatusBadge` component, reused across stock items + invoices + import rows, fixes both.

5. **[L] Create/edit forms rendered permanently inline instead of a `Dialog`.** `StockItemList.tsx:64`, `CustomerList.tsx:33-35` (and by the same shape `SupplierList`, `UserList`) match verbatim the problem the design system's `Dialog` doc was written to replace. Blocked on `Dialog` existing — track as a follow-up once it's built, not urgent alone.

6. **[S]** 3 contract records missing `<summary>` XML doc (CLAUDE.md rule): `Import.Core.Contracts/DetectedColumn.cs:4-6`, `ImportRowError.cs:4-5`, `ImportField.cs:4-5`.

7. **[M]** `Electron main.js` `waitForApi()` only checks for network error, not HTTP status — a 500/404 response is treated as "API ready." Fix: check `response.statusCode` in 200-299.

8. **[S]** `Electron main.js` has no user-facing failure path when the bundled API never starts within the timeout — only `console.error`'d, window still opens on a dead URL. Add `dialog.showErrorBox`.

9. **[S]** `scripts/quickstart.ps1:34-40` — `dotnet restore`/`npm ci` failures aren't checked (`$ErrorActionPreference = Stop` doesn't catch non-zero exit from native `.exe`s); script prints bogus "Setup done." Add `$LASTEXITCODE` checks like the existing `psql` check.

10. **[S]** Stale docs: `CLAUDE.md:31` still lists `Delivery.yml (MSI on tag)` and "Windows tests" — both gone since ADR-0011/PR #79 (now `Release.yml`, Electron installers, all-Ubuntu CI). `CLAUDE.md:51,57,59` still say "one `IMongoClient`", "Mongo duplicate key → domain error", "no `.msi` in git" — stale since the Postgres/EF Core (ADR-0008) and Electron (ADR-0013) moves. `.github/workflows/Integration.yml:1,3` has a trailing-space typo in the workflow name and a stale "master branch" comment (trigger itself correctly says `main`).

11. **[S]** `.github/workflows/sync-main-into-prs.yml:29` — `gh pr list` has no `--limit`, silently caps at 30 open PRs. Add `--limit 100` defensively.

## Low-value tests / coverage gaps

- `StockManagement.Tests/Kernel/IEnumerableExtensionsTests.cs`, `StringExtensionsTests.cs` — test only the dead code above; delete alongside it.
- `ServiceRegistrationTests.cs:26-32` — actively masks quality issue #1 (mocks Scoped as Singleton); fix alongside #1, not just a coverage note.
- No unit test for `TypeExtensions.cs` despite real production use in `Import.Core/ExcelEntityParser.cs` — gap, not sized.

## Files verified clean (checked, nothing found)

- Kernel: `BaseDocument`, `ManufacturerSerializer`, `PagedResult`, `EntityChangeType`, `IEntityChangedHandler`, all 8 `*ServiceProvider` interfaces, all 11 exception types, all `Model/*` entities + enums, `StockItemExtensions`, `TypeExtensions`, `ConversionHelper`, `RucValidator`, `SequenceNumber`
- Infrastructure: all `Ef*ServiceProvider`, `*Configuration`, `AppDbContext`, design-time factory
- All `*.Core`/`*.Core.Contracts` modules, `Api/Features/**`, `Language/**`
- No `async void`, empty `catch`, fire-and-forget, or `double`-for-money found anywhere in scope
- Web: `src/api/*.ts`, `App.tsx`, `routes.ts`, i18n pipeline, `Page.tsx`/`FailureMessage.tsx`/`useLoad.ts`/`test-utils.tsx`/`main.tsx`/`auth.tsx`, `permissionOptions.ts`/`PermissionCheckboxes.tsx`, all test files, e2e suite, CSS (except `.danger`)
- Electron/scripts/CI: `Desktop/package.json` + lockfile, `.gitignore`, `quickstart.sh`, `generate-adr-index.sh`, `Release.yml`, `sync-main-into-prs.yml` (aside from #11), `.editorconfig`, `README.md`; PR #111's claude-review.yml fix verified coherent, no infinite-loop risk

## Actions taken this cycle

- Dead code items #1-#8 above: PR opened (zero behavior change, see PR link in issue tracker)
- Quality issues #1-#11: filed as GitHub issues (see issue numbers in tracker)

## Cycle 2, 2026-10-02

Repo @ main (42b3afa). Delta since cycle 1 - merged: cursor pagination (ADR-0029), #52 validation codes, #113 CancellationToken, per-item VAT, invoice composite numbering, Sifen.Core + gateway/outbox worker, landed cost (#120), open-invoice import, name-matching, design-system rollout (Dialog/CommandPalette/DataTable/StatusBadge/Toast), `/api/search`, `sync-main-into-prs.yml` fix.

### Findings (new)

| # | File | What | Issue |
|---|------|------|-------|
| 1 | `StockItems/CheckInStockItemEndpoint.cs`, `CheckOutStockItemEndpoint.cs` | No `Permissions()` call - any authenticated user can mutate stock | #151 |
| 2 | `IInvoiceServiceProvider`, `ICreditNoteServiceProvider` | Missing `CancellationToken`, same bug class as #113 but excluded from its "6 of 8"; includes the new ADR-0029 cursor-pagination method, so `InvoiceServiceProvider`'s `.ToListAsync()` for it also drops the token | #152 |
| 3 | `StockItemList.tsx`, `SupplierList.tsx`, `UserList.tsx` | `window.confirm()` / no confirm at all on delete, no `danger` Button exists - cycle-1 finding, #105 closed without fixing it | #153 |
| 4 | `Invoices.resx` (`cash`, `credit`, `exceptionShoppingCartItemOutOfRange`, `invoice`), `StockItems.resx` (`checkin`, `checkout`) | More of #137's pattern: `Designer.cs` getter, no matching `<data>` entry in any locale | comment on #137 |

### Verified clean / no new dead code

- `SifenTransmissionWorker`: correct `IServiceScopeFactory` use, no fire-and-forget, exceptions caught + logged
- DI lifetimes (#97/#1 fix): all `*.Core` + `Sifen.Core` services now `AddScoped`, matches their Kernel/EF provider dependencies
- Cursor pagination (#107/ADR-0029): `Customer`/`StockItem`/`Supplier`/`User` providers all correctly threaded with `CancellationToken`; only `Invoice` missed (finding #2 above)
- `SearchEndpoint`: no endpoint-level `Permissions()` by design (filters per-domain internally, documented in its `<summary>`) - not a gap
- `/api/search`, `sync-main-into-prs.yml`, `AllocateLandedCostEndpoint`, open-invoice import target, name-matching: no dead code, no missing CancellationToken, Permission checks present
- No new `async void`, empty `catch`, fire-and-forget, or `double`-for-money

### Actions taken this cycle

- No dead code to remove (nothing newly zero-ref found)
- Findings above filed as #151-#153 + comment on #137

## Cycle 3, 2026-10-03

Repo @ main (6a9abb5). Delta since cycle 2 (bf99814) - merged: #137/#152/#153/#151 fixes, cursor pagination follow-ups (#107), frontend test infra (#109), Marangatu IVA book CSV export (#123), SIFEN contingency mode (#149), Bancard QR payment links (#150).

### Findings (new)

| # | File | What | Issue |
|---|------|------|-------|
| 1 | `SaleService.CreateInvoiceAsync`, `ContingencyCdcIssuer.TryIssueAsync` | CDC reserved (committed) before stock-shortage check; failed sale permanently burns a finite DNIT-granted number, no rollback, no test | #167 |
| 2 | `BancardPaymentLinkGateway.GetStatusAsync` | Bare catch incl. `OperationCanceledException`, defaults to `Pending`, no logging | #168 |
| 3 | `PaymentLinkService`/`PaymentLinkStatus.Expired` | `ExpiresAt` computed, never checked; `Expired` never assigned; no de-dupe of live links per invoice | #169 |
| 4 | `ExportIvaBookEndpoint.BuildCsv`/`CsvField` | No test coverage on CSV escaping/formatting that accountants import into Marangatu | #170 |
| 5 | `IvaBreakdown` vs `InvoiceCalculator.CalculateTax` | Duplicated VAT-grouping/rounding logic, acknowledged but not deduped | #171 |

### Verified clean / no new dead code

- Dead code: zero orphans across all new types (contingency CDC, payment links, IVA book export)
- Permissions: all 6 new endpoints have `Permissions()`; `BancardWebhookEndpoint` correctly `AllowAnonymous()` + re-verifies against gateway
- `CancellationToken`: fully threaded through all new interfaces/impls; #152 fix holds
- DI lifetimes: all new services `AddScoped`; `ServiceRegistrationTests.cs` extended to catch a regression
- resx/Designer.cs: no new resx surface in this delta, nothing to drift
- Atomicity: `TryReserveNextAsync` reservation itself is race-safe (bug in #167 is *when* it's called, not the reservation); `PaymentLinkService.ConfirmAsync` record-then-mark ordering safe on webhook retry
- Money/decimal: `decimal` throughout, consistent rounding
- No `async void`, fire-and-forget; no secrets in new `Bancard` config section
- No frontend shipped yet for Marangatu export/contingency/payment links in this delta - design-system check deferred to cycle 4

### Actions taken this cycle

- No dead code to remove
- Findings above filed as #167-#171

## Cycle 4, 2026-10-03

Repo @ main (28a831b). Delta since cycle 3 (6ea47fb) - merged: #167-#171 fixes (#173-#176, #178), Nota de Remisión (#162, PR #177), basic reports (#163, PR #179), barcode scan-to-sell (#165, PR #180), goods-import documents (#164, PR #181), sync-main-into-prs checkout-token fix (PR #182).

### Findings (new)

| # | File | What | Issue |
|---|------|------|-------|
| 1 | `DirectDnitSifenGateway.SendRemisionAsync` | Remisión always `EmissionType.Normal`, no contingency/offline path unlike invoices (#149/#167); self-flagged in code | #188 |
| 2 | `permissionOptions.ts` | `GoodsImports.Read`/`.Write` (#181) missing from the user permission picker - a Standard user can never be granted goods-import access via the UI | #189 |

### Design-system / UI-gap check (ADR-0025)

- Reports (#163) and barcode scan-to-sell (#165) got UI this cycle, built on `Page`/`Segmented` - consistent with the design system
- Still API-only, no React UI: Remisión (#177), goods-import documents (#181), Bancard payment links (#150), Marangatu IVA-book export (#123) - filed as #190

### Verified clean / no new dead code

- Dead code: none found; `#178`'s `VatRateGroup` extraction correctly deduped `IvaBreakdown`/`InvoiceCalculator` with no leftover duplicate
- Permissions: every new endpoint (Remisión x3, Reports x3, barcode, goods-import x3) has `Permissions()`; `GoodsImportsRead`/`Write`/`ReportsRead` all exercised by `PermissionEnforcementTests`
- `CancellationToken`: threaded through all new providers/endpoints, no regressions
- Atomicity: `GoodsImportDocumentServiceProvider.AddGoodsImportDocumentAsync` - document insert + stock check-in + transaction log in one transaction, rolls back together
- resx/Designer.cs: new `Reports.resx` set, `Users.resx` additions - `ResxDesignerSyncTests` green, no drift
- e2e `global-setup.ts` TRUNCATE list extended for all 4 new tables (`RemissionNotes`, `RemissionNoteItems`, `PendingRemisionTransmissions`, `GoodsImportDocuments`, `GoodsImportDocumentItems`)
- Test coverage: all 4 new feature areas (Remisión, reports, barcode, goods-import) have endpoint + unit tests
- Money/decimal: `decimal` throughout; no `double`
- No `async void`, fire-and-forget, empty `catch`, or secrets in new config

### Actions taken this cycle

- No dead code to remove
- Findings above filed as #188-#190

## Cycle 5, 2026-10-05

Repo @ main (91f9614). Delta since cycle 4 (28a831b) - merged: #188/#189/#190 fixes (contingency CDC for Remisión, permission-picker fix, Remisión/goods-import/payment-link/Marangatu web pages), Libro de Compras export (#186), Nota de Débito (#184, ADR-0036), SIFEN KuDE generation (#183, ADR-0035), accounts-receivable aging report (#185).

### Findings (new)

| # | File | What | Issue |
|---|------|------|-------|
| 1 | `ReportServiceProvider.LoadOpenInvoiceAgingAsync` | AR aging report loads *all* non-cancelled invoices+payments into memory, buckets/sorts/pages in C# - doc comment claims "keyset (ADR-0029)" but nothing is DB-pushed, unlike `GetSalesByCustomerAsync` in the same file | #209 |
| 2 | `DteXmlBuilder.cs:217,248,309,335`, `KudeHtmlBuilder.cs:24`, `KudeVerificationUrlBuilder.cs:22` | VAT-split formula (`amount*rate/(100+rate)`) re-duplicated 6x in Sifen.Core - #171's fix (`VatRateGroup`) is `internal` to Sales.Core, unreachable from Sifen.Core | #210 |

### Verified clean / no new dead code

- Dead code: none found
- Permissions: all 7 new endpoints (DebitNotes x3, KuDE x2, AR aging, purchase IVA book) have `Permissions()`
- `CancellationToken`: threaded through all new providers/endpoints
- DI lifetimes: all new registrations `AddScoped` (`IDebitNoteService`, `IPurchaseIvaBookExportService`, `IKudeHtmlBuilder`/`IKudeQrCodeGenerator`/`IKudeVerificationUrlBuilder`, `IDebitNoteServiceProvider`, `IReportServiceProvider`)
- resx/Designer.cs: `GoodsImport`, `Reports`, `Invoices` new keys - all 4 locales + Designer.cs in sync (only pre-existing resgen template placeholders differ, unrelated)
- SIFEN hardcoded-field regression check (#203/#204 class): `KudeDataMapper.ToEmisor`/`ToReceptor`, `DirectDnitSifenGateway`'s debit-note mapping all pull from real `CompanySettings`/`Customer`/`Invoice` data - no new hardcoded/empty DTE fields
- Atomicity: `DebitNoteServiceProvider.AddAsync` wraps DebitNote + PendingDebitNoteTransmission insert in one transaction, rolls back on duplicate-key (same shape as invoices)
- Money/decimal: `decimal` throughout; no `double`
- No `async void`, fire-and-forget, empty `catch` (new broad catch in debit-note transmission path correctly excludes `OperationCanceledException`, logs via `ILogger`)
- Test coverage: DebitNote (service + endpoint + XML builder tests), KuDE (data mapper + HTML builder + QR + verification-URL + endpoint tests), AR aging (3 integration tests covering current/90+/paid-exclusion buckets) all present
- `PurchaseIvaBookRow.SupplierRuc` always `""` (no RUC field on `Supplier` yet) - honestly flagged in `<remarks>` as unverified/placeholder, not a silent hardcode (not a new instance of the #203/#204 bug class)

### Actions taken this cycle

- No dead code to remove
- Findings above filed as #209-#210

## Cycle 6, 2026-10-05

Repo @ main (5509f3e). Delta since cycle 5 (91f9614) - merged: #212 (VatSplit dedup, closes #210), #213 (AR aging DB-pushdown, closes #209), #214 (SIFEN Cancelación/Inutilización, ADR per #206), #215 (accounts payable: SupplierInvoice/SupplierPayment, AP aging report).

### Findings (new)

| # | File | What | Issue |
|---|------|------|-------|
| 1 | `ReportServiceProvider.GetAccountsPayableAgingTotalsAsync`/`LoadOpenSupplierInvoiceAgingAsync` | AP aging (#215) loads *all* `SupplierInvoices` into memory, buckets/sorts/pages in C# - doc comment claims "same shape as `GetAccountsReceivableAgingByCustomerAsync`" but that method was DB-pushed by #213 earlier this same cycle; #215 copied the pre-fix pattern | #216 |
| 2 | `StockManagement.Web/e2e/global-setup.ts` | TRUNCATE list not updated for #215's new tables (`SupplierInvoices`, `SupplierPayments`) - every prior table-adding PR updated this list, #215 missed it | #217 |

### Verified clean / no new dead code

- Dead code: none found
- #212: `VatSplit.VatShare` correctly extracted to `Kernel.Util`, all 6 duplicate sites (`VatRateGroup`, `DteXmlBuilder` x4, `KudeHtmlBuilder`, `KudeVerificationUrlBuilder`) switched over - #210 fully closed
- #213: `OpenInvoiceAgingQuery` now pushes bucket computation, cursor filter and `OrderBy`/`Take` into SQL; plain class (not record) used for the grouped `Select` projection, with a comment explaining why (EF can't translate `Sum` through a record ctor) - #209 fully closed, 2 new tests cover the DB-pushed bucketing
- #214: permissions on all 4 new endpoints (`RequestCancellation`/`RequestNumberVoid` write, `ListCancellations`/`ListNumberVoids` read); `SifenTransmissionWorker` wired for both new outboxes; e2e TRUNCATE list *does* include `CancellationRequests`/`InvoiceNumberVoids` (this PR got it right, #215 didn't); `CancellationToken` threaded throughout; `SifenEventService` validates 48h Cancelación deadline, 1000-range/150-char Inutilización limits, and both overlap checks (in-use numbers, existing voids) before queuing; DNIT response parsing (`dCodRes` 0260) honestly flagged `<remarks>` as unverified, same as existing DTE gateway code
- #215: `Payables.Read`/`Payables.Write` permissions on all 5 endpoints; present in `permissionOptions.ts` (cycle-4's #189 gap class not repeated); `SupplierInvoiceServiceProvider.SaveChangesAsync` catches Postgres 23505 → `SupplierInvoiceNumberAlreadyExistsException`, same shape as `Invoice`; all new registrations `AddScoped`
- CancellationToken: threaded through all new providers/endpoints in both PRs
- Money/decimal: `decimal` throughout; no `double`
- No `async void`, fire-and-forget, empty `catch`

### Actions taken this cycle

- No dead code to remove
- Findings above filed as #216-#217

## Cycle 7, 2026-10-05

Repo @ main (13833ea). Delta since cycle 6 (5509f3e): #221 (.NET 10 upgrade), #222 (Ef-prefix rename, closes #76), #223 (cash register #166).

### Findings (new)

| # | File | What | Issue |
|---|------|------|-------|
| 1 | `CashRegisterService.OpenSessionAsync`/`AddMovementAsync`/`CloseSessionAsync` | #223 stored opening float/movement amount/counted amount unrounded - every other money-accepting service (`PaymentService`, `SupplierPaymentService`) rounds to `CompanySettings.CurrencyDecimalDigits` first | #224 (fixed this cycle) |
| 2 | `ListCashRegisterSessionsEndpoint.ExecuteAsync` | N+1: looped the page calling `GetExpectedAmountAsync` per session, each running its own cash-payments query | #225 (fixed this cycle) |
| 3 | `DirectDnitSifenGateway.LoadCertificate`, `XadesSignerTests.CreateSelfSignedCertificate` | #221's net10.0 retarget surfaced `SYSLIB0057` (obsolete `X509Certificate2` ctors), not flagged under net8.0 | #226 (fixed this cycle) |

### Verified clean / no new dead code

- Dead code: none found
- #221: no leftover `net8.0`/`net8` references anywhere (csproj, CI, scripts, Dockerfiles, docs) - full rebuild with a freshly `apt`-installed .NET 10 SDK succeeds, 0 errors
- #222: no stale `Ef`-prefixed type names left in code, docs, ADRs or CLAUDE.md - #76 fully closed
- #223: `CashRegister.Read`/`.Write` permissions on all 6 endpoints and present in `permissionOptions.ts` (cycle-4's #189 gap class not repeated); `CancellationToken` threaded throughout; e2e `global-setup.ts` TRUNCATE list already includes `CashRegisterSessions`/`CashMovements` (#217's gap class not repeated); resx/Designer.cs key counts match (27/27); atomic single-aggregate writes via EF owned collection (`Movements` `AutoInclude`d)
- Pre-existing, out of scope for this delta: `NU1902`/`NU1903` transitive advisories on `SharpCompress`/`Snappier` (via Testcontainers, present since the package was first added, not introduced by #221-#223)

### Actions taken this cycle

- #224-#226 fixed directly (small, same PR): `CashRegisterService` now rounds via `ISettingsService`; `ListCashRegisterSessionsEndpoint` batches expected-amount lookup through a new `GetExpectedAmountsAsync`/`GetCashPaymentsTotalsAsync` (one query instead of N); `X509Certificate2` ctors replaced with `X509CertificateLoader`
- No dead code to remove
