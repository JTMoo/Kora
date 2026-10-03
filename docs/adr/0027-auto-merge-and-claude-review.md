# ADR-0027: Unattended PR merging: required checks + Claude review, no owner click

- Status: Proposed
- Date: 2026-09-28

## Context

- Owner no longer wants to approve every PR by hand; wants to be pulled in only for important technical decisions or when the next step is unclear (owner, 2026-09-28)
- `JTMoo/StockManagement` is a personal-account repo: GitHub merge queues need an org
- Parallel threads on separate branches cause merge conflicts that pile up before anyone merges
- Same decision covers 3 sibling PRs, each scoped to its own conflict-free change: #100 (generated ADR index), #101 (`api.ts`/`App.tsx` split per domain), #102 (SessionStart hook + permission allowlist)

## Options

- Merge gate: owner click / required status checks only / required checks + a Claude review check
- Conflict handling: wait until merge time / merge `main` into open PR branches after every push to `main`

## Decision

- Branch protection on `main`: required status checks (`api-tests`, `web-tests`, `unit-tests`, `adr-index` from #100, `claude-review`), require branches up to date, no owner review required. Repo setting, not API-settable — owner does this once (see PR body)
- `.github/workflows/claude-review.yml`: `anthropics/claude-code-action` runs `/code-review` on every PR; a blocking finding fails the check, same as a failing test. Auth: `CLAUDE_CODE_OAUTH_TOKEN` (owner has no API key — subscription-based, `claude setup-token`), not `anthropic_api_key`. Secrets can't be used in a job-level `if:` (GitHub invalidates the whole workflow file — hit this in #103); a step checks the secret into an output instead, and the checkout/review steps are conditioned on that, so the job succeeds (not skipped, not failed) until the secret exists
- Threads enable GitHub's native per-PR auto-merge on their own PR once pushed (`enable_pr_auto_merge`); it merges once every required check is green
- `.github/workflows/sync-main-into-prs.yml`: after each push to `main`, merge `main` into every open PR branch; a real conflict gets a PR comment instead of a silent merge

## Consequences

- Owner is no longer on the critical path for a green PR; still asked before anything the review/CI gate can't judge (architecture, scope)
- A red Claude review or CI check blocks auto-merge the same way; no separate "soft" gate
- Needs `CLAUDE_CODE_OAUTH_TOKEN` as a repo secret — owner adds it, see PR body. Until then `claude-review` is skipped, not blocking (the other required checks still gate the merge)
- `sync-main-into-prs` pushes directly to other threads' branches; a thread mid-push could race it (accepted; rare, and a conflict comment is visible either way)
- `sync-main-into-prs`'s push authenticates as `github-actions[bot]` (default `GITHUB_TOKEN`); GitHub holds the resulting `pull_request` checks as `action_required` (0 jobs, needs manual approval) instead of running them, even on a same-repo branch - stuck until someone clicks "Approve and run". Fix: push with `SYNC_PR_TOKEN`, a PAT the owner adds as a repo secret, so the commit is attributed to a real collaborator (same as any human/App push, which run checks normally); falls back to `GITHUB_TOKEN` and reproduces the symptom until that secret exists. Also skips the merge entirely when the branch already contains `main`, and a `concurrency` group serializes syncs so a push mid-run doesn't pile up overlapping merge attempts.
- First attempt at the `SYNC_PR_TOKEN` fix (2026-10-02) only pointed the `gh` CLI's `GH_TOKEN` env at it, not `actions/checkout`'s `token:` input. `actions/checkout` wires up the git credential (`http.extraheader`) from its own `token:` (defaulting to `GITHUB_TOKEN`), separately from `GH_TOKEN` - a plain `git push` uses that credential, not `GH_TOKEN`, so every push was still attributed to `github-actions[bot]` and the stall continued (hit again on #173-#178, 2026-10-03). Fixed by also passing `token: ${{ secrets.SYNC_PR_TOKEN || secrets.GITHUB_TOKEN }}` to the checkout step.
