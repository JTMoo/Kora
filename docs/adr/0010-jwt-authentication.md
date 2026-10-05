# ADR-0010: JWT bearer authentication

- Status: Proposed
- Date: 2026-09-24

## Context

- Every API endpoint is `AllowAnonymous` today (ADR-0004): "Auth before any non-local deployment"
- No working auth exists to port: WPF's `LoginViewModel` lists users but never checks the password (`Kernel.Model.User` has no password field at all)
- No `IUserService`/`IAuthService`; `Kernel.Model.User`/`IUserServiceProvider` are Mongo-only, used by the WPF GUI
- Desktop-first, but the same API must also serve the browser (ADR-0003)

## Options

- Token: JWT bearer (FastEndpoints has this built in) / cookie session / OAuth against an external IdP
- Password storage: PBKDF2 in `System.Security.Cryptography` (no extra package) / `Microsoft.AspNetCore.Identity.PasswordHasher<T>`
- First user: seed an admin row in the migration / open, unauthenticated "first run" registration endpoint
- Token lifetime: short-lived + refresh token / single longer-lived token, re-login on expiry

## Decision

- FastEndpoints' built-in JWT bearer (`AddAuthenticationJwtBearer`, `JWTBearer.CreateToken`), `Jwt:SigningKey`/`Jwt:ExpiryHours` config (local dev default in `appsettings.json`, override in `appsettings.local.json` per ADR-0004)
- Token holds `sub` (user id) and `username` claims only; no roles/permissions yet (#14, #30 ask for those separately)
- 12 hour expiry, no refresh token: the token is stateless, so "logout" is discarding it client-side, nothing server-side to revoke
- `Kernel.Model.User` gets `Username` (unique) and `PasswordHash`; `IUserServiceProvider` gets `GetUserByUsernameAsync`, and its writes move from Mongo result types to `Task<int>` (rows affected), matching the ADR-0008 cutover already done for the other entities
- New `StockManagement.Auth.Core`/`.Contracts`: `IAuthService.ValidateCredentialsAsync` checks username/password (PBKDF2-SHA256, 100k iterations, own `PasswordHasher`, no extra NuGet package)
- `UserServiceProvider` (Infrastructure) is the first implementation with a real password; the `AddUsers` migration creates the table and seeds one `admin` user (password `ChangeMe123!`) so a fresh install has something to log in with
- `POST /api/auth/login`, `AllowAnonymous`; every other endpoint requires the bearer token (removed `AllowAnonymous()` from all of them)
- React: token kept in memory + `localStorage` (survives a refresh; no cookie, so no CSRF surface), sent as `Authorization: Bearer`; a login screen gates the app shell, a 401 from any call clears the token and returns to it

## Consequences

- No user management yet (create/edit/delete/roles): out of scope here, tracked by #14/#30, not closed by this PR
- No password change/reset endpoint: the seeded admin password is a real credential in the migration source until one exists - flagged, not solved, here
- `localStorage` token: no XSS-proof storage without adding a BFF/cookie layer; acceptable for now given no sensitive PII/payment data yet, revisit if that changes
- Local dev/CI: API tests and `npm run e2e` must log in first; `ApiFactory.CreateAuthenticatedClientAsync()` (seeded admin) covers this for `StockManagement.Api.Tests`
- WPF GUI's login stays a non-functional stub (still Mongo, still no password check) until the GUI is retired (ADR-0006 direction); not rewired here
