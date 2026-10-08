# ADR-0043: Usage telemetry

- Status: Proposed
- Date: 2026-10-07

## Context

- Owner wants to know how customers actually use Kora (feature usage, errors, perf) to drive the roadmap.
- Customer installs run their own `StockManagement.Api` on-prem (Postgres local, Paraguay market) - same trust boundary as ADR-0042 feedback relay: nothing customer-identifying can leak, and the client must not look like spyware.
- A feedback relay already exists (ADR-0042) and a license server is pending (ADR-0041) - both hosting decisions still open with the owner.

## Options

- A: Self-built `POST /telemetry` event endpoint on the same relay host as ADR-0042. Reuses hosting/trust boundary, full control of schema/retention, no dashboard out of the box.
- B: PostHog self-hosted. Good dashboards, but another service to run and secure.
- C: PostHog Cloud. Fast to wire up, but customer usage data leaves our infra to a 3rd party - risk with on-prem customers.
- D: OpenTelemetry. Built for traces/metrics, not product analytics (funnels/retention) - wrong tool for this ask.

## Decision

- A. Extend the existing relay service to also accept `POST /telemetry` events (pseudonymous `installId`, no PII, see `analysis/usage-analytics.md`).
- Default opt-in off; one Settings toggle to enable.
- Client buffers events locally, batches, flushes best-effort; drops oldest past a cap when offline.
- Events: feature/route usage, errors (scrubbed), perf timings, app version/OS, license tier. Never business data (amounts, customer identity, stock data).

## Consequences

- Open: relay hosting (same open item as ADR-0041/0042) - blocks shipping this.
- Open: single relay service for feedback + telemetry, or split - pick when hosting lands.
- Deferred: dashboard - phase 4, after enough events exist to need one.
- If a dashboard product (PostHog Cloud) is needed later, swap the client's target URL - not the whole architecture.
