# ADR-0044: Licensing security hardening and owner discounts

- Status: Proposed
- Date: 2026-10-07

## Context

- ADR-0041 shipped the offline-verifiable client contract; server + payment integration explicitly deferred. Hosting (one service vs two) still open with the owner.
- Owner ask: licensing "as safe as possible", plus the ability to grant per-customer discounts.
- Threats against the merged client (ADR-0041):
  1. System clock rolled back → resurrects a lapsed trial/subscription (`LicenseCalculator` compares against `DateTime.UtcNow` only).
  2. `LicenseState` row edited/deleted directly, or app reinstalled → trial restarts for free. No fix without server-side activation tracking; **accepted risk**, unchanged from ADR-0041.
  3. `Licensing:PublicKey`/config swapped for an attacker-controlled keypair → self-signed keys accepted. Config is deployment-controlled (Electron build), not user-editable; true fix is distribution, not code - noted below, not solved here.
  4. A valid activated key copied onto other machines (no binding).
  5. No key rotation path (single static key) - a compromised key can't be retired without breaking every issued license.
  6. No revocation (chargebacks, piracy) - needs a server round-trip, out of scope.

## Options (client hardening, this PR)

- Clock rollback: ignore (status quo) / persist a high-water-mark UTC and clamp `now` to it.
- Machine binding: none / optional `MachineId` claim, enforced only when the issuer sets it.
- Key rotation: single key (status quo) / `kid`-prefixed token + `PublicKeys` dictionary.
- Discounts: recomputed client-side from a promo table / baked into the signed token at issuance, client only displays.

## Decision

- **Clock-rollback guard:** `LicenseState.HighWaterMarkUtc`, the latest UTC instant this install has ever observed. Every status read clamps `now` to `max(now, HighWaterMarkUtc)` and persists on advance (`LicenseService.ClampToHighWaterMarkAsync`). Rolling the clock back reads as "no time passed," not a rewind. Does not stop a full reinstall (new DB row) - that stays the ADR-0041 accepted risk.
- **Key rotation:** token format becomes `kid.base64url(payload).base64url(signature)`. `LicensingOptions.PublicKeys: Dictionary<kid, base64 SPKI>` replaces the single `PublicKey`. Unknown/missing `kid` fails closed. Lets the server retire a key without invalidating every issued license at once. No tokens exist in the wild yet (`PublicKey` has always shipped empty), so this is a breaking format change with nothing to migrate.
- **Machine binding:** `LicenseState.MachineId`, a GUID generated once per install (lazily, same pattern as `TrialStartedAtUtc`; backfilled on upgrade for rows from before this ADR) and shown on the licensing page so the owner can quote it when issuing a key. `LicenseToken.MachineId` is optional: `null` = unbound (manual/legacy issuance, no enforcement); set = must equal the local `MachineId` or the key is rejected, both at activation and on every later status read. Stops a leaked/shared key from running on a second machine without the server having to be involved.
- **Discount claims:** `LicenseToken.DiscountPercent` (0-100) and `EffectivePricePyg` baked into the signed payload at issuance. Client only displays them (`LicensingPage`); never recomputes a price. Out-of-range `DiscountPercent` fails verification.
- **Public key distribution:** stays a deployment concern, not solved in code. The key(s) must ship inside the Electron build's `appsettings.json` (not a user-editable override file) so swapping it requires rebuilding/repackaging the app, not editing a text file. No code change here; flagged for the Electron packaging follow-up.

## Server design (follow-up, not built - needs the hosting decision first)

- **Signing-key custody:** the private key must never be reachable by the same code path as the feedback relay (ADR-0042) or any other secret. Recommendation for the pending hosting card: **one service, two secrets** - license-signing key and feedback-relay token live in separate secret-store entries/env scopes, loaded by separate modules, so a vulnerability in one relay/endpoint doesn't expose the other. Two separate services is safer in isolation but doubles hosting/ops cost for a single-operator project; not justified unless the license server later needs independent scaling or a stricter network boundary (e.g. an HSM-backed signer). Revisit if that need appears.
- **Key rotation workflow:** server keeps the current signing key plus recently-retired ones (by `kid`) so already-issued tokens keep verifying until they naturally expire; new issuance always uses the current `kid`.
- **Activation + periodic re-validation:** first activation is online (submits `MachineId`, receives a token); client re-validates signature+expiry locally on every read (already true today) and should phone home periodically (e.g. weekly) when reachable to pick up revocations early - still works fully offline between checks (existing grace period absorbs it).
- **Revocation:** server maintains a revoked-key/kid list; the periodic re-validation call checks it. No client-side revocation without a server round-trip - documented limitation.
- **Discount issuance:** owner-admin creates promo codes and per-customer overrides (percent or fixed PYG, duration, expiry, usage cap, plan scope - Monthly/Yearly/both). Applied at key issuance only: the server bakes `DiscountPercent`/`EffectivePricePyg` into the token it signs. The client never applies a promo code itself, so there's nothing for a customer to tamper with. Yearly stays `MonthlyPricePyg * 10` as the undiscounted base (ADR-0041); a discount is applied on top, not by editing that formula.
- **Payment webhook (once a provider is chosen):** verify the provider's signature before trusting any payment event; rate-limit the activation and feedback endpoints; log activations/revocations with licensee + kid + machine id for support/audit, no payment data.

## Consequences

- Breaking config change: `Licensing:PublicKey` → `Licensing:PublicKeys:<kid>`. No real keys deployed yet, so no migration needed.
- `LicenseState` gains two columns (`MachineId`, `HighWaterMarkUtc`); migration backfills existing rows lazily on next read, same as the original `TrialStartedAtUtc` bootstrap.
- DB-row deletion/reinstall trial reset remains unfixed (ADR-0041 accepted risk) - would need server-tracked activation per machine, which needs the hosting decision.
- Public-key-swap risk is only as mitigated as the Electron packaging makes it; purely server-side fixes (signing, rotation, revocation) stay blocked on the hosting decision.
