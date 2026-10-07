# ADR-0042: Feedback and error reporting

- Status: Proposed
- Date: 2026-10-06

## Context

- Owner wants: manual "report a problem / feedback" + global exception handler that offers to send a report. Both must use the same code path.
- Each report becomes a GitHub issue in `JTMoo/Kora`. The client (self-hosted per customer, Electron + browser) must never know or show that it's GitHub.
- Customer installs run their own `StockManagement.Api` locally - a GitHub PAT baked into that install would leak to every customer.

## Options

- A: Client calls GitHub API directly with an embedded token. Rejected - token ships to every customer.
- B: Customer's API calls GitHub directly, token in that install's config. Rejected - same leak, every install needs the real token.
- C: Customer's API posts to a small relay service (owned/hosted by us) that holds the token and creates the issue. Client and customer's API never see GitHub.

## Decision

- C. New `IReportSink` abstraction (`StockManagement.Feedback.Core.Contracts`), default impl `HttpReportSink` posts to a configured relay URL (`Feedback:RelayUrl`).
- New small ASP.NET project `StockManagement.Feedback.Relay`, deployed separately from customer installs, holds `GitHub:Token`, creates the issue (label `user-report`).
- `StockManagement.Api` exposes one endpoint, `POST /feedback`, used both by the manual "Report a problem" entry and by the client's global error handling (same request shape, `category: Feedback | Bug`).
- Consent lives client-side: the global exception handler shows "Send report?" before calling `POST /feedback`; it never auto-sends.
- Scrubbing: message/log excerpt truncated server-side (`FeedbackOptions.MaxMessageLength` / `MaxLogExcerptLength`); no auth headers or tokens ever included in the report body.
- No offline retry queue for now (deferred - see Consequences).

## Consequences

- Open: where does `StockManagement.Feedback.Relay` get hosted, and who owns `GitHub:Token`/`Feedback:RelayUrl` for customer installs? Owner decision.
- Deferred: offline queue/retry if the relay is unreachable - reports are fire-and-forget today, failure is surfaced to the user to retry manually.
- Relay has no auth beyond a shared secret header + per-IP rate limit - revisit if abused.
