# Handoff (branch: develop)

The single current handoff for this branch, replaced in place as state changes. It stays lean: where things stand, where to resume, and pointers to the plan and receipts that hold the detail. Per operator convention (2026-11-11): one handoff at a time in a branch; anything with more detail than this is a plan, not a handoff.

Updated: 2026-11-11.

## Current priority: Dolt versioned federation (operator's top priority)

- **State:** plan-readiness passed independent hostile validation at AGREE 98/98 (rounds R2-R5; reviewer Claude Opus, different family from the Codex author). NOT implementation-ready: DEPLOY HOLD is in force and not lifted, every Ph0 B1-B12 feasibility output is unproven, and nothing is installed or activated.
- **Resume at:** the Ph0/B1 provider-enablement spike (make the accepted EF Core 10 driver - the Pomelo EF10 port or the Microting fork, patched and contributed upstream as needed - drive a pinned Dolt image on LAB Linux and LEGION Windows, proven against the real schema/query/transaction/migration/remote contracts). Then a reduced first increment: one project, both real hosts, one versioned domain end-to-end. Two residual go/no-go, both Ph0 evidence: Dolt's own MySQL-compatibility gaps (Go-side) and crash-safe native Windows Dolt hosting on LEGION (B1/B9). Pending operator call: the test-tagging mechanism scope (general build infra under FR-MCP-BUILD-001 vs federation-first).
- **Plan (detail lives here):** `docs/plans/2026-11-10-federation-dolt-architecture.md` (authoritative), `-bdpv4.md`, `-three-system-workflows.md`, `-current-contracts.md`, `-reconciliation.md`, `-library-options.md`; correctness evaluation `docs/plans/2026-11-11-federation-correctness-evaluation.md`. Contracts of record: `docs/Project/federation-dolt/`.
- **Receipts:** `docs/receipts/hv/20261111T090739|093826|100429|114239-federation-r{2,3,4,5}-plan-readiness.*` (convergence 95/94 -> 97/97 -> 98/98 -> 98/98).
- **Locked decisions:** Dolt selected as the versioned-storage engine; cell-level relational merge with mandatory conflict review accepted as Git-style merging; EF Core 10 prerelease/fork provider accepted (all in Workspace memory `MEMORY-PREFERENCE-002`). xUnit v3 is the test standard (`MEMORY-BESTPRACTICE-001` section 10). The compiled `docs/plans/exports/Federation-2026-11-10-R2.*` is a stale pre-fix rendering and needs recompiling before use.

## Standing duties

- Keep LAB-OMARCHY and PAYTON-LEGION2 Global MCP memories in sync by hand until federation ships (the drift federation removes).
- HV reviewer must differ from the author's agent family and model family (`docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md`).

## Superseded

This replaces the dated per-effort handoff `federation-dolt-handoff-20261111T120903.md` (removed) and the 2026-10-16 forwarding-first `federation-fixes-handoff-20261016T121400.md` for the versioned-federation mode. Explicit DirectProxy forwarding remains a separate supported feature. Older dated handoffs under `docs/handoffs/` are history (see `CATALOG.md`); going forward there is one handoff per branch, here.
