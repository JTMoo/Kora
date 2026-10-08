# ADR-0046: Auth hardening - JWT key, forced password rotation, login throttling

- Status: Accepted
- Date: 2026-10-08

## Context

- #243: `appsettings.json` ships a real JWT HMAC key, committed to git, identical across every install - full authN bypass once an attacker reads the source.
- #244 / #239: seeded `admin`/`ChangeMe123!` has no way to rotate and nothing forces it to.
- #238: `POST /api/auth/login` has no rate limiting - unlimited password guessing.

## Decision

- **JWT key** (#243): `JwtSigningKeyProvider.Resolve` in `StockManagement.Api/Security` - outside `Development`, if `Jwt:SigningKey` is still the known placeholder, a per-install key is generated once and persisted under `%LocalAppData%/Kora/jwt-signing-key` (`GeneratedSigningKeyStore`, same pattern as `StockManagement.Feedback.Core.InstallId`). The resolved key is written back into `IConfiguration` so `LoginEndpoint` (which re-reads config to sign tokens) and the JWT bearer middleware always agree. Throws if the key is still the placeholder and no per-install key could be generated - never silently signs with a known key.
- **Forced password rotation** (#244, #239): new `User.MustChangePassword` column (migration sets it `true` on the seeded admin only). `ChangePasswordEndpoint` (`PUT /api/auth/password`) is self-service only - target is the caller's own id from the JWT `sub` claim, never a request field - and clears the flag on success. `ForcePasswordChangeMiddleware` blocks every other `/api` endpoint for a user with the flag set (same exemption list as `LicenseEnforcementMiddleware`: `/api/auth`, `/api/feedback`). `LoginResponse` carries `mustChangePassword` so the React shell routes straight to `ChangePasswordPage` without any other call landing on the 403 first.
- A wrong current password on `ChangePasswordEndpoint` is a 400 (`incorrectCurrentPassword`), not 401 - a 401 would trip the web client's shared "session expired" handler and log the caller out entirely instead of just failing the one field.
- **Login throttling** (#238): `LoginEndpoint.Configure()` adds `this.Throttle(5, 60)` (FastEndpoints built-in, per-IP via `X-Forwarded-For`/`RemoteIpAddress`). No separate lockout-after-N-failures state; the existing PBKDF2 cost plus this cap is enough for now.

## Consequences

- Reinstalling onto the same machine/profile keeps the generated JWT key (same file); wiping `%LocalAppData%/Kora` invalidates every outstanding token, forcing re-login everywhere - acceptable, same tradeoff already accepted for `InstallId`.
- `ForcePasswordChangeMiddleware` checks `MustChangePassword` on every request for an authenticated user via a DB round trip (same cost as `LicenseEnforcementMiddleware`'s license check already on the same pipeline).
- Throttle state is per-process memory, not shared across multiple API instances - fine for the single-instance desktop/small-business deployment model this app targets.
