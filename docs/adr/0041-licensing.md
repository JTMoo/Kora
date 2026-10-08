# ADR-0041: Licensing (trial + subscription)

- Status: Proposed
- Date: 2026-10-06

## Context

- No licensing today: the app runs forever, free. Owner wants a 10-day trial, then Monthly or Yearly subscription required (Yearly = 10x Monthly).
- Single-tenant desktop install (ADR-0013/0016), one Postgres DB per customer — same model as `AppSettings` (ADR-0015): one row of state, no multi-tenant concerns.
- Activation/renewal needs a license server + payment provider. Out of scope here — build the offline-verifiable contract now, server + payment integration as a follow-up project/issues.

## Options

- Key crypto: Ed25519 (not in .NET BCL, needs a 3rd-party lib) / ECDsa P-256 (BCL, every platform Kora ships on) / HMAC shared secret (symmetric, secret would ship inside the app — reversible)
- Enforcement point: ASP.NET middleware (path-based, framework-agnostic) / FastEndpoints global pre-processor (ties to FastEndpoints internals)
- Trial state: new `LicenseState` singleton row (own table) / fields bolted onto `AppSettings`

## Decision

- **Crypto:** ECDsa P-256, built into `System.Security.Cryptography` — no new dependency. Key format: `base64url(payloadJson).base64url(signature)`. Payload: `{ licensee, plan, issuedAtUtc, expiresAtUtc }`. App ships only the server's public key (`Licensing:PublicKey` config); the private key lives on the (future) license server, never in this repo.
- **State:** `LicenseState` (own table, `StockManagement.Kernel.Model`), same singleton-row pattern as `AppSettings`: `TrialStartedAtUtc`, `ActivatedLicenseKey`, `ActivatedAtUtc`. Set lazily on first read — covers fresh installs and existing DBs upgrading onto this ADR identically.
- **Status computation** (`LicenseCalculator`, pure, unit-tested): `Trial` while `now < TrialStartedAtUtc + TrialDays`; `Active` while a verified activation's `ExpiresAtUtc > now`; `GracePeriod` for `GraceDays` after trial or subscription lapses (absorbs a late renewal); `Locked` after that — every activation is re-verified against the public key on every read, never trusted at rest.
- **Enforcement:** `LicenseEnforcementMiddleware`, plain ASP.NET middleware after auth, before FastEndpoints. Blocks every `/api/*` call with `402` + `{ code: "license_expired" }` once `Locked`, except `/api/auth/*` and `/api/license/*`. Health check and the React static files are outside `/api` already.
- **Pricing:** `Licensing` config section (`TrialDays`=10, `GraceDays`=3, `MonthlyPricePyg`). `YearlyPricePyg` is always `MonthlyPricePyg * 10` — computed, not stored, so it can't drift.
- **Endpoints:** `GET /license` (no permission gate — every authenticated user needs it for the banner), `POST /license/activate` (`Settings.Write`).
- **New project:** `StockManagement.Licensing.Core` + `.Core.Contracts`, same layering as every other domain (ADR-0002).

## Consequences

- No license server exists yet — `Licensing:PublicKey` ships empty, so activation always fails closed until the owner configures a real key; trial/grace still work standalone.
- A system-clock rollback defeats the trial; acceptable for v1 (same trust level as every other `DateTime.UtcNow` check in this codebase).
- Owner decisions still open: where the license server runs, which payment provider issues keys (Bancard link already exists, #150, for payment collection — not key issuance).
