# Usage analytics / telemetry

Investigation only. Decision = ADR-level (spans client, API, hosting, legal). See proposed ADR-0043 below.

## What to collect

- Feature usage: screen/route opened, action invoked (create invoice, import run, etc.) - event name + minimal props
- Errors: exception type/message (scrubbed), stack hash, not full stack to relay unless opted into bug report (ADR-0042 path)
- Performance: route load time, import duration, API p95 (client-measured, batched)
- Environment: app version, OS, Electron vs browser, screen size, `CompanySettings.Id` (hashed, not customer name)
- License state: trial/paid, plan tier (needed for product decisions, not billing)
- Never: invoice amounts, customer names/RUC, stock quantities, any business/PII data

## Event model

- `{ eventName, ts, sessionId, installId, appVersion, props: {} }`
- `installId`: random GUID generated once per install, stored locally, not tied to `CompanySettings` identity - pseudonymous
- `sessionId`: random per app launch
- Events batched client-side, flushed every N seconds or M events, flushed on shutdown (best-effort)

## Options

| Option | Fit | Effort | Cost | Notes |
|---|---|---|---|---|
| Self-built: `POST /telemetry` on existing relay (`StockManagement.Feedback.Relay`), events → Postgres/ClickHouse at relay host | High | Medium | Low (reuses hosting) | Same trust boundary as ADR-0042; full control over schema/retention; no dashboard out of box |
| PostHog self-hosted | Medium | High (own infra, upgrades) | Medium (hosting) | Good dashboards/funnels, EU hosting option for GDPR, but another service to run/secure |
| PostHog Cloud (EU region) | Medium | Low | Usage-based, scales with events | Fast to wire up, but customer usage data leaves our infra to a 3rd party - on-prem/Paraguay customers may object |
| OpenTelemetry (traces/metrics) | Low fit | Medium | Low | Built for perf/observability, not product analytics (funnels, retention) - wrong tool for "how people use the app" |
| Build on existing error-reporting path only (no new system) | Low fit | Low | None | Covers errors, not feature usage - doesn't answer the ask |

## Offline / buffering

- On-prem installs may run offline or behind restrictive networks
- Client buffers events in local SQLite/file, flushes when relay reachable, drops oldest after a cap (e.g. 5k events / 7 days) to bound disk use
- No blocking on flush failure - fire-and-forget, same posture as ADR-0042 feedback

## Consent / opt-in

- Default **off** until explicit opt-in (first-run or Settings toggle) - avoids "spyware" perception on customer-owned infra
- One settings toggle: "Share anonymous usage data to help us improve Kora" - visible, not buried
- Opt-out must stop the batching job entirely, not just stop sending

## Anonymization / retention

- No customer PII in events (see "Never" above)
- `installId` is the only identifier - not reversible to a customer without the relay operator's own install registry (keep none, or keep separate from event store)
- Retain raw events 90 days, aggregate past that - revisit if product research needs longer cohort analysis

## Legal

- Paraguay: Ley N° 6534/2020 (data protection) - applies to personal data; anonymous/pseudonymous product telemetry with no PII is low risk, but opt-in avoids any argument either way
- GDPR: only relevant if EU customers exist - none currently targeted (Paraguay market, see `analysis/paraguay-product-research.md`); revisit if that changes
- Self-hosting (option 1) keeps data in infra we control, simplest story if a customer asks "where does this go"

## Recommendation

Self-built events endpoint on the existing relay (`StockManagement.Feedback.Relay` → rename/extend to `StockManagement.Telemetry.Relay` or add a sibling endpoint on the same service). Reuses the hosting decision already pending for ADR-0041/0042, avoids a third-party data-sharing conversation with on-prem customers, lowest incremental effort.

PostHog Cloud is the fallback if the self-built dashboard (just SQL against the events table) proves insufficient - swap the client SDK's target URL, not the whole architecture.

## Phased rollout

1. `POST /telemetry` endpoint + `installId` generation + consent toggle (default off) - no dashboard yet, just land events in Postgres
2. Client instrumentation: route views, key actions (create/import/check-in), app version/OS - wire into existing error handler for error events too
3. Offline buffer + batching
4. Basic dashboard (SQL views or a small Grafana board) once enough data exists to be useful

## Open for owner

- Hosting of relay (blocks this too - same open item as ADR-0041/0042)
- Rename `StockManagement.Feedback.Relay` to host both feedback and telemetry, or separate service?
