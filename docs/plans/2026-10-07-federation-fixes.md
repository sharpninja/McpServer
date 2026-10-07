# Federation fixes: cross-platform workspace identity, hub routing, workspace sync, proxy backfill

- Date: 2026-10-07
- Branch / worktree: `claude/federation-fixes-20261007` at `.worktrees/federation-fixes-20261007` (base `origin/develop` f56dcf70)
- Process: Byrd Development Process v4 (`docs/Development-Process-draft-v4.md`). Per phase: requirements, red tests derived from requirements, tests green against mocks, then real implementation, refactor, and the full suite (current + previous) green before the phase gate.
- MCP session: `ClaudeCode-20261007T135500Z-federation-fixes`
- Related: PLAN-PATHNORM-001 (hub), triage groups `triage-group-bfc69ceba9e9f1fb` (path mangling), `triage-group-d708a0e30a2b7195` (workspace field drift), `triage-group-18acf2d8a0adfecd` (no backfill). All three triage groups ended `failed` with no research or TODO, so this plan carries the research.

## 1. Problem statement (evidence)

Observed 2026-10-06/07 with LocalProxy PAYTON-LEGION2 (Windows) and hub LAB-OMARCHY (Linux, cwd `/opt/mcpserver/app`):

1. **Hub cannot address proxy workspaces.** Direct probe of the hub with the proxy's hub token (2026-10-07):
   - no `X-Workspace-Path` -> 200 with the hub's own McpServer TODOs (58).
   - `X-Workspace-Path: F:\GitHub\McpServer` -> 400 "Unknown workspace path".
   - `X-Workspace-Path: <GlobalWorkspaceId hash>` -> 400 "Unknown workspace path".
   - the exact headers a LocalProxy forward carries -> 400.
   Consequence: forwarded proxy traffic either fails or silently reads/writes the hub's primary workspace.
2. **Windows paths mangled on Linux.** `TriageService.ResolveSubmittingWorkspace` calls `Path.GetFullPath` (`src/QBrainAi.Services/Services/TriageService.cs:1417`), producing `/opt/mcpserver/app/C:\Users\kingd`. About 20 other request-path resolvers call `Path.GetFullPath` / `Path.Combine` on request-supplied paths with host-OS semantics (WorkspaceService.NormalizePath :629-635, WorkspaceResolutionMiddleware, HandoffWorkspacePaths, TodoServiceResolver, TodoServiceFactory, EfTodoService, RequirementsDatabaseDocumentService, WorkspaceTokenService, HostileReviewService, TodoExecutionService, FileGitHubWorkspaceTokenStore, AgentPoolService, AuditedAgentCliClient, RepoFileService, MarkerFileService, WikiDumpService, WorkspacePolicyDirectiveParser, AgentHelpPinnedPathResolver, HandoffSourceResolver, SessionLogWorkspaceAttributionValidator). `WorkspaceIdentityPath` already detects platform from path syntax and only calls `GetFullPath` for host-native paths; the bug is the bypasses.
3. **Hub canonical workspace rows drift.** `FederationTopologyService.EnsureFederatedWorkspaceRow` (:691-720) hard-codes `TodoPath = "docs/todo.yaml"` and syncs only Name/IsEnabled. Proxy inventory (`FederationWorkspaceMetadataPayload`) carries only IsPrimary/DataDirectory/TunnelProvider and the hub never reads it. Observed: 1 TodoPath and 8 CurrentRequirementLayerKey mismatches of 55.
4. **No proxy-to-hub backfill.** Enrollment registers inventory only. ~1.885M workspace-scoped proxy rows vs 1,650 on the hub (all post-enrollment audit rows). Hub-side adapter apply runs in a fresh DI scope with an empty `WorkspaceContext` (adapters never call `OverrideWorkspaceId` with the GlobalWorkspaceId, except memory), so even per-operation replay cannot land rows in the right workspace. Several domains have no adapter (use cases, triage, brain slots, handoff, actors, documents/chunks). `POST federation/operations` records but never applies; only `federation/envelopes` applies. Every recorded operation fans out to all other proxies.
5. **Operator directive (PLAN-PATHNORM-001):** persist paths with `/`. Windows storage keys currently use `\` and the GlobalWorkspaceId hashes `lower(proxyId:windows:UPPER(path))`, so a separator change alters every existing Windows id and needs a migration or legacy-key fallback.

## 2. Phase order and rationale

| Phase | Scope | Why this order |
|---|---|---|
| P1 | Syntax-based workspace path handling everywhere a request supplies a path, plus forward-slash persisted paths and id migration (D1) | Prerequisite for every hub lookup and id; removes the mangling class; ids change once, before backfill |
| P2 | Hub resolution of proxy workspaces + fail-closed forwarding | Without it, forwarded traffic and any backfilled data are unreachable |
| P3 | Canonical workspace field sync (proxy inventory -> hub row) | Hub must serve proxy workspaces with the proxy's configuration |
| P4 | Proxy-to-hub backfill with workspace-scoped apply and read gating | Depends on P1-P3 ids, routing and configuration being correct |
| (P5) | Retired: folded into P1 per D1 | |

Each phase ends at a gate: full `QBrainAi.Support.Mcp.Tests` + `QBrainAi.Client.Tests` + `Build.Tests`, receipts saved under `docs/receipts/federation-fixes-20261007/`, session-log actions recorded.

**Gate policy (operator decision 2026-10-07: baseline-relative).** The base commit `origin/develop` f56dcf70 already fails 26 tests (13 in `QBrainAi.Support.Mcp.Tests`, 13 in `Build.Tests`; listed in `docs/receipts/federation-fixes-20261007/02-p1.3.txt`, measured on a clean worktree). A phase gate passes when the failing set equals that baseline set exactly: no new failures, and any baseline failure that starts passing is noted. The baseline failures are triaged separately and are not fixed on this branch.

## 3. Requirements (proposed ids; to be created in the MCP requirements store before red tests)

### P1: Cross-platform workspace paths
- **FR-MCP-FED-PATH-001**: A server must interpret any request-supplied workspace path by its own syntax (drive letter, UNC, POSIX root), never by the host OS, so a Linux host accepts and stores Windows paths unchanged in meaning.
  - AC1: On a Linux host, `C:\Users\kingd`, `C:/Users/kingd` resolve to the same Windows identity; nothing is prefixed with the process cwd.
  - AC2: On a Windows host, `/home/x` stays a POSIX path.
  - AC3: Triage submit/status round-trips: `getReport` with the submitter's original path finds the report on a Linux hub.
  - AC4: `WorkspaceService.Init` never creates a directory from a foreign-platform path.
- **TR-MCP-FED-PATH-001**: Introduce a single request-path normalization entry point (`IWorkspacePathNormalizer` backed by `WorkspaceIdentityPath`) with an injectable host-platform abstraction; all resolvers listed in section 1.2 use it. No direct `Path.GetFullPath` on request-supplied workspace paths outside it (enforced by an architecture test).
- **TEST-MCP-FED-PATH-001**: Unit matrix {host: Windows, Linux (simulated)} x {input: `C:\`, `C:/`, `\\srv\sh`, `//srv/sh`, `/home/x`, relative, mixed separators} for the normalizer; triage submit/getReport/queryGroups on simulated Linux with Windows paths; architecture test scanning `src/` for `Path.GetFullPath` on request paths outside the allowlist.

### P2: Hub routing of proxy workspaces
- **FR-MCP-FED-ROUTE-001**: The hub must resolve a forwarded request to the proxy workspace's canonical row using (X-Mcp-Proxy-Id, proxy-local workspace path) or the GlobalWorkspaceId.
  - AC1: Forwarded `GET /todo` with `X-Workspace-Path: F:\GitHub\McpServer` from PAYTON-LEGION2 returns that workspace's data (200), never 400 and never the hub primary workspace.
  - AC2: `X-Workspace-Path: <GlobalWorkspaceId>` resolves to the same workspace.
  - AC3: A request carrying `X-Mcp-Federation-Hop` without a resolvable workspace fails closed (4xx with diagnostic), never falls back to the API key's default workspace.
  - AC4: The proxy forwards the hub GlobalWorkspaceId (not its local path) in `X-Mcp-Global-Workspace-Id` (fixes `FederationMiddleware.cs:109`).
- **TR-MCP-FED-ROUTE-001**: Workspace resolution middleware gains a federation lookup (FederationWorkspaces by proxyId + normalized path, or by GlobalWorkspaceId) before path lookup; the inter-server token is only valid for workspaces registered to that proxy.
- **TEST-MCP-FED-ROUTE-001**: Middleware/controller tests for AC1-AC4 with an in-memory hub; negative test that a hub-token request without workspace returns 4xx when a hop header is present.

### P3: Canonical workspace sync
- **FR-MCP-FED-WSSYNC-001**: Proxy-owned workspace configuration (TodoPath, RunAs, AgentPath, PromptTemplate, StatusPrompt, ImplementPrompt, PlanPrompt, IsPrimary, DataDirectory, TunnelProvider) must propagate to the hub canonical row on enroll and heartbeat. Hub-owned state (CurrentRequirementLayerKey) is documented as hub-owned and reported, not overwritten (decision D3).
  - AC1: After enroll, every proxy-owned field on all canonical rows equals the proxy row (55/55 in the lab repro).
  - AC2: Changing TodoPath on the proxy is reflected on the hub after the next heartbeat.
  - AC3: `GET federation/workspaces` exposes a drift indicator for hub-owned fields that differ.
- **TR-MCP-FED-WSSYNC-001**: Typed fields on `FederationWorkspaceRegistrationRequest` (versioned payload, backward compatible with older proxies); `EnsureFederatedWorkspaceRow` applies them; no hard-coded TodoPath when the proxy supplies one.
- **TEST-MCP-FED-WSSYNC-001**: Topology service tests for create/update/heartbeat propagation and old-proxy compatibility.

### P4: Proxy-to-hub backfill
- **FR-MCP-FED-BACKFILL-001**: After enrollment, a LocalProxy must copy all existing workspace-scoped data for each enrolled workspace to the hub, idempotently, preserving ids, versions, timestamps and history where the domain supports it.
  - AC1: Domains: todo (items, tasks, list items, requirement links, document metadata, audit history), session_log (full graph), requirements (incl. acceptance criteria, traceability, scope layers), memory (+versions), use cases, documents/chunks, triage, tools buckets/definitions, agents, brain slots, handoff runs, actors. Each domain is either backfilled or explicitly listed as local-only with a reason.
  - AC2: Re-running the backfill creates no duplicates and reports `already_applied` counts.
  - AC3: Backfill operations do not fan out to other proxies as new work.
  - AC4: `federation/status` reports backfill state per workspace/domain; `staleReadStatus` is non-clear until backfill completes; forwarded reads for an incomplete workspace either are served locally or carry `X-Mcp-Stale-Read` (decision D4).
  - AC5: Row-count and checksum parity report per domain per workspace after completion.
- **TR-MCP-FED-BACKFILL-001**: Adapter apply on the hub runs inside a workspace scope set from GlobalWorkspaceId (`OverrideWorkspaceId`); adapters gain bulk enumerate (`EnumerateAsync(workspace, cursor)`); backfill sends signed envelopes marked `applyMode: backfill` (no fanout); durable per-workspace/domain cursor on the proxy; new adapters for uncovered domains.
- **TEST-MCP-FED-BACKFILL-001**: Per-adapter enumerate/apply round-trip tests; idempotency; no-fanout; resume-from-cursor; parity report; status gating.

### P1 (continued, per D1): Forward-slash persisted paths
- **FR-MCP-PATHNORM-001** (PLAN-PATHNORM-001): Persist workspace paths with `/`; keep the `windows:` / `case-sensitive:` tag; detect Windows by drive letter or UNC; preserve UNC `//`; convert to `\` only at OS boundaries.
- **TR-MCP-PATHNORM-001**: Storage-key and GlobalWorkspaceId migration across SQLite, SQL Server, PostgreSQL; legacy-key lookup fallback during transition; re-enrollment plan for proxies.
- **TEST-MCP-PATHNORM-001**: Migration tests per provider; legacy id resolution; OS-boundary conversion.

## 4. Operator decisions (2026-10-07)

- **D1 (decided: fold P5 into P1)**: The forward-slash separator change lands in P1 together with syntax-based path handling, so workspace ids change once, before any backfill writes rows under them. PAYTON-LEGION2 (the only enrolled proxy) is re-enrolled once after P1/P2 deploy. P5 is retired as a separate phase; its requirements (FR/TR/TEST-MCP-PATHNORM-001) become P1 requirements.
- **D2 (decided: signed envelopes)**: Backfill uses the existing signed envelope path (`federation/envelopes`) with batching and an `applyMode: backfill` marker that suppresses fanout, so validation, conflict and idempotency rules stay in one path.
- **D3 (decided: hub-owned)**: CurrentRequirementLayerKey and other hub-maintained fields are hub-owned. Heartbeats never overwrite them; drift is reported.
- **D4 (decided: serve locally)**: While a workspace's backfill is incomplete, the LocalProxy serves reads for that workspace from its local store; writes still forward to the hub. Reads switch to the hub when that workspace's backfill completes.

## 5. Out of scope / noted

- FR id mismatch: code comments cite FR-MCP-082/083/085 for federation query/push, but those ids are different features in `Functional-Requirements.md`. Correct the comments in the phase that touches each file.
- `C:\Windows\System32\drivers\etc\hosts` on PAYTON-LEGION2 maps LAB-OMARCHY to unreachable 192.168.1.182 (hub reachable at 10.42.0.214). Operator action.

## 6. Receipts

- Baseline build: `dotnet build QBrainAi.sln -c Debug` exit 0, 0 errors, 00:01:56.51 (2026-10-07, `artifacts-local/baseline-build.log`).
- Hub probe output: section 1.1 (2026-10-07).
- Prior evidence: 2026-10-06 session (workspace equivalence 55/55 ids, 9 field diffs; per-table row inventory).
