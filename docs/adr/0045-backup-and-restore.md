# ADR-0045: Backup and restore

- Status: Proposed
- Date: 2026-10-08

## Context

- #251: no backup/restore at any layer. `Database.MigrateAsync()` runs on every boot (`Program.cs:42`) with no safety net.
- Single Postgres DB per install (ADR-0008), desktop-first but browser-viewable (ADR-0013/0016).
- `postgresql-client` (`pg_dump`/`pg_restore`) ships alongside every Postgres install customers already run; confirmed present on `ubuntu-latest` CI runners.

## Options

- A: `pg_dump`/`pg_restore` shelled out from the API. Matches issue's own suggestion, no new schema-dump code to maintain, battle-tested, handles FKs/sequences correctly.
- B: Hand-rolled EF Core table dump (JSON/CSV per table). Rejected - reinvents transactional consistency and FK ordering that `pg_dump` already solves; more code, more bugs.
- C: Filesystem-level Postgres `pg_basebackup`. Rejected - needs WAL archiving/replication config, far beyond a single-tenant desktop app.

## Decision

- A. New `StockManagement.Backup.Core` wraps `pg_dump -Fc` (custom format, compressed) / `pg_restore --clean --if-exists`, connection parsed from `ConnectionStrings:Postgres`.
- **Scope**: DB only. SIFEN certificate/config files are customer-managed external files, not DB rows - out of scope, noted as a follow-up below.
- **Scheduled vs manual**: manual only (owner-triggered button). No in-process scheduler added for one feature; revisit if the owner asks.
- **Destination**: local folder on the machine running the API, configurable (`Backup:Directory`, default `%LocalAppData%/Kora/Backups` or `~/.local/share/Kora/Backups`). Files also stream over HTTP so a browser client (not just the desktop Electron shell) can download them.
- **Restore safety**: `POST /backups/restore` takes the uploaded dump, first runs an automatic pre-restore `pg_dump` snapshot (`pre-restore-*.dump`) into the same directory, terminates other backends on the target DB (`pg_terminate_backend`), then `pg_restore --clean --if-exists`. Requires `confirm: true` in the request body - the web UI gates this behind a typed confirmation, not just a click.
- **Permission**: new `Backup.Manage` in `Permission.Catalog` (ADR-0017 pattern) - Admin role has it by default via `Permission.Effective`, same as every other admin-only capability; owner can still grant it to a specific user.
- **Encryption**: none built into the dump file for v1 - an encryption key stored next to the backup (same machine) protects nothing, and a key stored elsewhere is a second thing to lose and needs its own custody story. Documented as the customer's responsibility (encrypted destination folder/drive); revisit only if the owner wants a managed key story later.

## Consequences

- Restore drops the API's live DB connections; existing EF Core/Npgsql pooled connections reconnect automatically, but the UI tells the owner to restart the app after a restore as a precaution.
- No encryption/offsite/scheduled backup in v1 - all three are customer-side process today (copy `Backup:Directory` to a drive/cloud folder on whatever cadence they choose).
- Follow-up, not blocking: back up the SIFEN certificate/config alongside the DB once it's clear where those live per-install.
