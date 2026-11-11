# Handoff: Dolt federation (TOP PRIORITY), 2026-11-11T12:09:03 local

Operator, 2026-11-11: "federation is my top priority." This is the current, authoritative handoff for the versioned (Dolt) federation effort. It supersedes `federation-fixes-handoff-20261016T121400.md` for the versioned-federation mode; the existing explicit DirectProxy forwarding remains a separate supported feature and is not replaced by it.

## 1. Priority and status in one line

Dolt versioned-relational federation is the operator's number-one priority. The plan has passed independent plan-readiness hostile validation at AGREE 98/98. It is NOT implementation-ready: every Ph0 feasibility output (B1-B12) is unproven, DEPLOY HOLD is in force and has not been lifted, and no package is installed and nothing is activated.

## 2. What the design is (operator decisions locked)

- LAB-OMARCHY is the hub; every node (hub included) executes and persists work locally and exchanges committed history bidirectionally through a guarded publication boundary. Eventual consistency, not a distributed transaction.
- Storage engine: Dolt (MySQL-wire, Git-style commit/branch/merge on relational data), selected by the operator on 2026-11-11 (U1) as the fourth supported provider alongside SQLite, PostgreSQL, SQL Server. One Dolt repo/database per logical project per host.
- Merge: Dolt cell-level relational merge with mandatory conflict review, accepted by the operator on 2026-11-11 (U2) as satisfying the "Git-style merging" requirement. Branches are IN the initial release; pull requests are OUT.
- Identity: two-character creator tags plus a durable non-reused sequence; permanent origin-scoped aliases; human-ID reservation at the hub.
- Authority: existing Duende IdentityServer, reference tokens, per-node clients; the HTTPS gateway is the auth boundary (native remotesapi is loopback-only). Audit flows to the hub append-only, independent of version commits.
- Source of truth for contracts: `docs/Project/federation-dolt/` (FR/TR/TEST, matrix, mapping). Workspace decisions recorded in Workspace memory `MEMORY-PREFERENCE-002` (v4).

## 3. Plan packet and receipts

- Architecture: `docs/plans/2026-11-10-federation-dolt-architecture.md` (authoritative).
- BDPv4 plan (Ph0-Ph8): `docs/plans/2026-11-10-federation-dolt-bdpv4.md`.
- Workflows U01-U15 + 16-FR inventory: `docs/plans/2026-11-10-federation-three-system-workflows.md`.
- Contracts (16 FR->TR->TEST, 99 Given/When/Then + 34 gap-check, stable error map): `docs/plans/2026-11-10-federation-current-contracts.md`.
- Scope coverage: `docs/plans/2026-11-10-federation-reconciliation.md`. Library options A-I: `docs/plans/2026-11-10-federation-library-options.md`.
- Correctness evaluation (valid vs correct; DRAFT decision-support): `docs/plans/2026-11-11-federation-correctness-evaluation.md`.
- HV receipts (request/response JSONL + verdict) under `docs/receipts/hv/`: R2 `20261111T090739-federation-r2-*` (DISAGREE 95/94), R3 `20261111T093826-*` (DISAGREE 97/97), R4 `20261111T100429-*` (AGREE 98/98, full packet), R5 `20261111T114239-*` (AGREE 98/98, scoped delta). Reviewer: Claude Opus (different family from the Codex author). Convergence 95/94 -> 97/97 -> 98/98 -> 98/98.
- The compiled `docs/plans/exports/Federation-2026-11-10-R2.*` PDF/HTML is a STALE pre-fix rendering and must be recompiled by the Codex export tooling before it is used as the packet artifact; the markdown sources are authoritative.

## 4. The decisive risk and the operator's decision on it

The load-bearing dependency is a working EF Core 10 provider over the MySQL wire for Dolt on net10.0. As of Nov 2026 there is NO GA provider: Pomelo has no EF10 release (latest 9.0.0 pins EF Core 9.x; EF10 port PR open/unmerged); the only confirmed EF10 build is the third-party Microting fork. Operator decision 2026-11-11: use the unfinished EF10 driver (the Pomelo port or the Microting fork), patched by this project and contributed upstream as needed; this is an accepted dependency for the Dolt engine context, recorded in architecture section 8, bdpv4 section 5, and `MEMORY-PREFERENCE-002`. Silent EF downgrade and a bespoke from-scratch provider remain forbidden without operator agreement.

## 5. Next steps (when the operator lifts the pause)

1. Ph0/B1 as a provider-enablement spike: make the EF10 port/fork drive a pinned Dolt image on both LAB (Linux) and LEGION (native Windows), patching as needed, and prove the real schema/query/transaction/migration/concurrency/remote tests. B1 remains a hard go/no-go.
2. Two residual go/no-go items, both Ph0 evidence: Dolt's own MySQL-compatibility gaps (Go-side, worked around or patched upstream, NOT fixable in the .NET driver), and crash-safe native Windows Dolt hosting on LEGION (B1/B9).
3. Reduced first increment recommended: one project, two real hosts, a single versioned domain end-to-end (commit -> publish -> merge/review -> recovery) before building the full fourth-provider + full-migration-tool surface.
4. Pending proposal (not yet approved): a requirement/AC test-tagging mechanism (a `Requirement`/`Ac` xUnit v3 trait driving tiered Nuke targets `TestForAc`/`TestForRequirement` and `TestAffectedProjects`, plus a ValidateTraceability trait check) to run only pertinent tests during development and let per-task HV scope tighten. Decide: general build infrastructure (TRs under FR-MCP-BUILD-001) vs federation-scoped first.

## 6. Standing duties and conventions in force

- Keep LAB-OMARCHY and PAYTON-LEGION2 Global MCP memories in sync by hand until federation ships (the drift federation removes). `MEMORY-BESTPRACTICE-001` was synced to both (v4, section 10, byte-identical) on 2026-11-11. Working sync path: scp a pwsh script to LEGION2 and run its local plugin (the OMARCHY-plugin-over-SMB-mount path times out).
- HV reviewer must differ from the author's agent family AND model family (operator 2026-11-11; see `docs/architecture/SessionTurn-Review-AgentContext-Architecture-Amendment-1-Durable-HV.md`). Claude reviewing the Codex-authored packet satisfies both.
- xUnit v3 is the test standard (native skip; no SkippableFact/v2) per `MEMORY-BESTPRACTICE-001` section 10.

## 7. Git state (today, branch develop, pushed to origin)

- f7655d8d docs(federation): HV plan-readiness AGREE 98/98 and correctness evaluation
- 30466238 build(test): finish xUnit v3 standardization, remove v2/SkippableFact footprint
- c945fc01 docs(federation): record EF10 prerelease-provider acceptance (B1) and xUnit v3 test standard
- 3c10bd89 build(service): publish the MCP server ReadyToRun in UpdateService
- f8014bc7 docs(hv): amend durable-HV architecture for model-family reviewer exclusion
- cdd73f2a docs(federation): retain R1 and R5 plan-readiness HV receipts

Deliberately NOT committed (flagged to the operator): `docs/Project/Functional-Requirements.md` + `docs/Project/wiki/azure/.mcp-requirements-manifest.json` resurrect retired `FR-MCP-USECASE-016/017` (`MEMORY-PROCESS-014`, never recreate) and remove a legitimate timestamp AC; `.claude/settings.local.json` carries one stale per-session local Bash permission. These remain dirty pending the operator's decision.
