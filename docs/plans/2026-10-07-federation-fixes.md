# Federation fixes: cross-platform workspace identity, hub routing, workspace sync, proxy backfill

- Date: 2026-10-07
- Branch / worktree: `claude/federation-fixes-20261007` at `.worktrees/federation-fixes-20261007` (base `origin/develop` f56dcf70)
- Process: Byrd Development Process v4 (`docs/Development-Process-draft-v4.md`), applied per increment as defined in section 2a. Red tests are not acceptable (operator correction 2026-10-07, MCP memory `MEMORY-PROCEDURE-002`): tests are proven green against mocked data before any production code, then the real implementation must keep the same tests green.
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

## 2a. Per-increment procedure (BDP v4, operator correction 2026-10-07)

Every increment (for example P1.4a) follows these steps in order. Receipts record each step's command output.

1. **Contract and tests from requirements.** Define or confirm the public contract (interface or method signature). Write the tests from the TEST requirement ids against that contract only.
2. **Green against mocked data.** Each test class is a contract-test base with an abstract fixture hook. A `MockData` fixture supplies a mock or scripted fake of the contract (NSubstitute double or test-only fake driven by a data table of expected values). Run the `MockData` fixtures: all green. This proves the tests and their data are correct before production code exists. No `NotImplementedException` stubs and no deliberately failing first run.
3. **Real implementation.** Write the production code. Add the `Real` fixture for the same contract-test base. Run: `MockData` and `Real` fixtures both green.
4. **Refactor** with both fixtures green.
5. **Gate** (phase end, or checkpoint commit): full `QBrainAi.Support.Mcp.Tests` + `QBrainAi.Client.Tests` + `Build.Tests`, baseline-relative as below.

Naming: `<Subject>ContractTests` (abstract), `<Subject>MockDataTests`, `<Subject>RealTests`. Test filters: `FullyQualifiedName~MockDataTests` then `FullyQualifiedName~RealTests`.

**Gate policy (operator decision 2026-10-07: baseline-relative).** The base commit `origin/develop` f56dcf70 already fails 26 tests (13 in `QBrainAi.Support.Mcp.Tests`, 13 in `Build.Tests`; listed in `docs/receipts/federation-fixes-20261007/02-p1.3.txt`, measured on a clean worktree). A phase gate passes when the failing set equals that baseline set exactly: no new failures, and any baseline failure that starts passing is noted. The baseline failures are triaged separately and are not fixed on this branch.

## 3. Requirements (registered in the MCP requirements store 2026-10-07, workspace F:\GitHub\McpServer)

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
- **TR-MCP-PATHNORM-001** (updated 2026-10-07 per D5-D7): forward-slash lexical form; separator-agnostic predicates; `ToNativePath` at OS boundaries; `--workspace-path-rekey` maintenance command (backup, dry-run, report, one transaction with deferred or suspended FKs re-validated, C# recomputation of hashed values, token-store key rewrite); startup guard instead of a legacy dual-key lookup; re-enrollment of proxies.
- **TEST-MCP-PATHNORM-001** (updated 2026-10-07): storage-key equivalence, remote detection for `//`, native conversion, mapping function, planner, per-provider executors, startup guard, rehearsal on a restored copy; MockData fixtures first.

## 4. Operator decisions (2026-10-07)

- **D1 (decided: fold P5 into P1)**: The forward-slash separator change lands in P1 together with syntax-based path handling, so workspace ids change once, before any backfill writes rows under them. PAYTON-LEGION2 (the only enrolled proxy) is re-enrolled once after P1/P2 deploy. P5 is retired as a separate phase; its requirements (FR/TR/TEST-MCP-PATHNORM-001) become P1 requirements.
- **D2 (decided: signed envelopes)**: Backfill uses the existing signed envelope path (`federation/envelopes`) with batching and an `applyMode: backfill` marker that suppresses fanout, so validation, conflict and idempotency rules stay in one path.
- **D3 (decided: hub-owned)**: CurrentRequirementLayerKey and other hub-maintained fields are hub-owned. Heartbeats never overwrite them; drift is reported.
- **D4 (decided: serve locally)**: While a workspace's backfill is incomplete, the LocalProxy serves reads for that workspace from its local store; writes still forward to the hub. Reads switch to the hub when that workspace's backfill completes.

## 4a. P1 status: delivered increments (commit 8ca38d5d) and process deviation

P1 was split into increments during implementation; they are recorded here after the fact.

| Increment | Delivered | Tests (current classes) |
|---|---|---|
| P1.1 | `IWorkspacePathNormalizer`, `IWorkspaceHostEnvironment`, `WorkspacePathNormalizer` (Normalize, DetectPlatform), `SystemWorkspaceHostEnvironment`, `WorkspacePathNormalizer.Process` | `WorkspacePathNormalizerTests` |
| P1.2 | `TriageService` submit, routing, four query filters and research leaf name through the normalizer; `GetLeafName` | `TriageServiceTests` (5 new cases), `WorkspacePathNormalizerTests` |
| P1.3a | DI registration; `WorkspaceService` identity, DataDirectory, derived names, removal of post-normalization `Path.IsPathRooted` guards, `Init` refuses foreign-platform paths; `HandoffWorkspacePaths` (all MCP tool overrides); `WorkspaceTokenService`; `IsHostNative` | `WorkspaceServiceCrossPlatformPathTests`, `HandoffWorkspacePathsTests` (2 new), `WorkspaceTokenServiceTests` (1 new, 1 corrected) |
| P1.3b | `TodoServiceResolver`, `TodoServiceFactory`, `EfTodoService`, `TodoExecutionService`, `RequirementsDatabaseDocumentService`, `HostileReviewService`, `AgentPoolService`, `FileGitHubWorkspaceTokenStore`, `SessionLogWorkspaceAttributionValidator`; `Combine`; `Path.GetFullPath` ratchet | `WorkspacePathGetFullPathRatchetTests`, `SessionLogWorkspaceAttributionValidatorCrossPlatformTests` |
| P1.3c | `WorkspaceContext.SetDerivedPaths` used by middleware and MCP tools; `RepoFileService` refuses foreign-platform workspaces | `WorkspaceContextTests` (2 new), `WorkspaceResolutionMiddlewareTests` (1 new), `RepoFileServiceTests` (1 new) |

**Scope added beyond the original TR text** (all traceable to FR-MCP-FED-PATH-001 acceptance criteria): `GetLeafName`, `IsHostNative`, `Combine`, `WorkspaceContext.SetDerivedPaths`, foreign-workspace guards on `WorkspaceService.Init` and `RepoFileService`, ratchet-with-allowlist design for the architecture test.

**Process deviation (recorded, not repeated).** P1.1-P1.3 were executed red-first: tests were first run against `NotImplementedException` stubs or the unfixed code and observed failing, then the implementation was written. BDP v4 requires the tests to be proven green against mocked data first (section 2a). The P1.3b domain sites (`TodoServiceResolver`, `TodoServiceFactory`, `EfTodoService`, `TodoExecutionService`, `RequirementsDatabaseDocumentService`, `HostileReviewService`, `AgentPoolService`, `FileGitHubWorkspaceTokenStore`) also have no dedicated behaviour tests; they are covered only by the ratchet and existing suites. Remediation is P1.R.

## 4b. P1.R: retrofit mock-data fixtures for P1.1-P1.3 (operator request 2026-10-07)

Goal: every test written for P1.1-P1.3 runs as a contract-test base with a `MockData` fixture and a `Real` fixture, and receipts show the `MockData` run green followed by the `Real` run green. The production code already exists, so the retrofit cannot recreate the original order; it establishes that each test is valid against mocked data and keeps that proof in the suite for future changes.

| Retrofit | Subject | MockData fixture | Real fixture | Production change needed |
|---|---|---|---|---|
| R1 | `IWorkspacePathNormalizer` | `ScriptedWorkspacePathNormalizer`: test-only fake driven by a data table of (host platform, input) to (platform, normalized, leaf, host-native, host-call expected) and Combine rows; calls the mocked host only where the table marks the input host-native | `WorkspacePathNormalizer` over the NSubstitute simulated Linux/Windows host (current tests) | none |
| R2 | `IWorkspaceHostEnvironment` (process host) | NSubstitute host scripted with the expected platform, cwd and native normalization | `SystemWorkspaceHostEnvironment.Instance` | none |
| R3 | Consumers: `TriageService`, `WorkspaceService`, `HandoffWorkspacePaths`, `WorkspaceTokenService`, `SessionLogWorkspaceAttributionValidator`, `WorkspaceContext.SetDerivedPaths`, `WorkspaceResolutionMiddleware`, `RepoFileService` | NSubstitute `IWorkspacePathNormalizer` returning a scripted data table (canonical path, platform, host-native, leaf, combine results) | real `WorkspacePathNormalizer` over the simulated host | additive optional normalizer parameters where the consumer currently uses `WorkspacePathNormalizer.Process` directly: `SessionLogWorkspaceAttributionValidator.ValidatePaths/ValidateTurn/ValidateCommits`, `WorkspaceContext.SetDerivedPaths`, `RepoFileService` constructor, `WorkspaceResolutionMiddleware.InvokeAsync` (DI parameter). Default stays the process normalizer, so runtime behaviour is unchanged |
| R4 | P1.3b domain sites with no behaviour tests: `TodoServiceResolver`, `TodoServiceFactory`, `TodoExecutionService`, `RequirementsDatabaseDocumentService` scope override, `HostileReviewService` workspace match, `AgentPoolService` pooled device id, `FileGitHubWorkspaceTokenStore` key | NSubstitute normalizer with scripted data | real normalizer over the simulated host | additive optional normalizer parameter on each (constructor, or overload for statics); default process normalizer |
| R5 | `Path.GetFullPath` ratchet | in-memory source map (scripted file contents: identity file with 0 calls, allowlisted file at its count, allowlisted file above its count, unlisted new file) proving pass and each failure message | scan of the repository `src/` tree | extract counting and evaluation into a test-support helper that takes a source provider |

R1-R5 exit: `MockDataTests` filter green, then `RealTests` filter green, then the baseline-relative gate; receipts in `docs/receipts/federation-fixes-20261007/04-p1r-r1-r3.txt` and `05-p1r-r4-r5.txt`.

**P1.R status (2026-10-07).** R1-R5 delivered. Implementation note: the R3 and R4 MockData fixtures use the shared test-only `ScriptedWorkspacePathNormalizer` (one literal data table) rather than per-test NSubstitute normalizers, so all mocked path data is auditable in one place; hosts remain NSubstitute doubles (`SimulatedWorkspaceHosts`). Results: all `MockDataTests` 69/69 green, then all `RealTests` 67/67 green (7 contract subjects). Test files that had received P1 cases were restored byte-identical to base f56dcf70 and the cases moved into contract classes.

## 4c. P1.4 / P1.5: forward-slash persisted paths

Status: **approved 2026-10-07** with decisions D5-D7 as recommended (section 4c.6). FR-MCP-PATHNORM-001 (AC3), TR-MCP-PATHNORM-001 and TEST-MCP-PATHNORM-001 were updated in the MCP requirements store to match (legacy dual-key lookup removed in favour of the rekey command and startup guard).

### 4c.1 Inventory (2026-10-07)

Local `McpServer_PAYTON_LEGION2` (read-only queries): about 1.86M rows across 50 workspace columns hold backslash values (`DataAuditLogs.WorkspaceId` 1,248,032; `SessionLogTurnStringLists` 233,306; `Workspaces` 103 of 104). About 68 foreign keys reference workspace ids, all `NO ACTION`; 11 are composite (for example `("WorkspaceId","TodoId")`, `("WorkspaceId","RequirementKind","RequirementId")`), so a parent key cannot be updated in place. Latest migration in all three provider projects: `20261005170000_RenameTriageIsMcpServerRelatedColumn`.

Persisted values by kind (citations from the impact research; `McpDbContext.cs`, `FederationTopologyService.cs`, `TriageService.cs`, `HandoffReplayKeys.cs`):

| Kind | Columns | Change needed |
|---|---|---|
| A. Raw normalized path | `Workspaces.WorkspaceId` (PK), `WorkspacePath` (unique), `DataDirectory`; the `WorkspaceId` column of 57 workspace-scoped tables; `TriageReports.OriginalWorkspacePath`, `EffectiveWorkspacePath`; `TriageGroups.EffectiveWorkspacePath`; `FederationWorkspaces.WorkspacePath` (unique with ProxyId); `FederationOperations.GlobalWorkspaceId` (holds the proxy's raw path) | rewrite the separator |
| A2. Host `GetFullPath` output (not identity-normalized) | `ToolDefinitions.WorkspacePath`/`WorkspaceId` (`ToolRegistryService.cs:232-241`), `AgentWorkspaces.WorkspacePath` (`AgentService.cs:603-604`) | route through the normalizer in code, then rewrite |
| C. Hash of a path or storage key | `FederationWorkspaces.GlobalWorkspaceId`/`CanonicalWorkspaceId` and the hub's hash-keyed `Workspaces` rows (`SHA256(lower(proxyId:storageKey))`); `TriageGroups.GroupKey` and `GroupId` (FK from `TriageReports`, `TriageResearchRuns`; recomputable from stored `Fingerprint`); `HandoffIngestionRuns.ReplayIdentity` | recompute in C# (SQLite has no SHA-256) |
| Not persisted | `AgentPoolService.BuildPooledDeviceId` (in-memory), `WorkspaceTokenService` (in-memory, rotates) | none |
| Outside the database | `FileGitHubWorkspaceTokenStore` file keyed by exact normalized path | rewrite keys in the same maintenance run |

No column stores a `windows:` storage key directly.

### 4c.2 P1.4 code changes (increments, each per section 2a)

- **P1.4a Lexical form.** `WorkspaceIdentityPath.NormalizeWindowsLexicalPath`, `GetWindowsRoot`, `GetUncRoot` emit `/`: drive `C:/Users/kingd`, UNC `//srv/share/x` (leading `//` kept), storage key `windows:C:/USERS/KINGD`. Device forms: `\\?\C:\x` becomes `C:/x` (strip the device prefix, as `GetWindowsFinalPath` already does at `:735-742`); `\\?\UNC\srv\share` becomes `//srv/share`. Drive-letter case preserved. Opaque identifiers unchanged.
- **P1.4b Separator-agnostic predicates.** `IsRemoteWindowsPath` (`:982-991`) must recognize `//` as remote, otherwise `//srv/share` reaches `CreateFile` and breaks the finite-cancellation contract (`:488-493`). `IsNormalizedPathWithinRoot` (`:912`) uses `/` for normalized Windows paths. `NormalizeLocalAdministrativeShareAlias` (`:1121-1153`) accepts `//localhost/C$` (also fixes an existing gap).
- **P1.4c Native boundary.** Add `WorkspaceIdentityPath.ToNativePath(path)`: on a Windows host converts a Windows-platform path to backslashes; used only where `/` is not accepted or changes meaning: `CreateFile`/`GetFinalPathNameByHandle` in the physical resolver, `\\?\` long-path prefixes, and `cmd.exe` arguments (`/` reads as a switch). .NET file APIs, `Process.WorkingDirectory` and `git -C` accept `/` and are left as is.
- **P1.4d Mis-comparisons.** `WorkspaceService.cs:543-546` (primary check compares normalizer output with `GetFullPath(repoRoot)`; would insert a duplicate primary), `ToolRegistryService.cs:235`, `AgentService.cs:604` route through the normalizer. Ratchet counts for these files go down.
- **P1.4e Test expectations.** About 40-60 assertions in about 12 files assert backslash output (main ones: the normalizer contract literals and scripted data, `WorkspaceContextTests:56-61`, `FederationTopologyServiceTests:15,64`, `BugTriage139SixteenthIdentityConsumerTests:370-377`, UNC cases in `WorkspaceIdentityFourteenthNativeTests:109` and `BugTriage139SixteenthIdentityWindowsTests:205`). Per section 2a the MockData literals change first and must be green before production code changes.

### 4c.3 P1.5 data rewrite

- **Value mapping `f(x)`.** Applies only to Windows-shaped values: `^[A-Za-z]:\\` (drive) and `^\\\\` (UNC, device). Replace every `\` with `/`; UNC keeps `//`. POSIX paths, 64-hex hash ids, opaque ids and the empty global id are untouched. Idempotent (already-forward-slash values map to themselves). A blanket `REPLACE` is not used.
- **Mechanism (decision D5).** Recommended: an explicit maintenance command `--workspace-path-rekey` in `QBrainAi.Support.Mcp` modelled on `McpDatabaseEncryptionTransitionCommand` (`DatabaseMaintenance/`), not an automatic startup EF migration, because (a) hash columns need C# SHA-256, (b) the local store is 1.86M rows and the operator should control when it runs, (c) it needs a backup, dry-run and report. Options: `--dry-run`, `--backup-path` (required unless `--dry-run`), `--report-path`, `--instance`. Plus a **startup guard**: the server refuses to start with a clear error if any `Workspaces.WorkspaceId` is a Windows-shaped value containing `\`, so a P1.4 build never runs against an un-rekeyed store.
- **Algorithm (one transaction per database).**
  1. Build the mapping table `old -> new` from `Workspaces` (Windows-shaped ids only) and from every distinct Windows-shaped value in the kind-A columns.
  2. Suspend foreign-key enforcement for the transaction (decision D6): SQLite `PRAGMA defer_foreign_keys = ON`; SQL Server `ALTER TABLE ... NOCHECK CONSTRAINT` on the affected tables, then `WITH CHECK CHECK CONSTRAINT` (re-validates every row); PostgreSQL `ALTER CONSTRAINT ... DEFERRABLE` plus `SET CONSTRAINTS ALL DEFERRED`, restored to `NOT DEFERRABLE` after.
  3. One set-based `UPDATE ... FROM mapping` per kind-A column (about 60 statements, not per row).
  4. Recompute kind-C values in C#: triage `GroupKey`/`GroupId` from the new effective path and stored `Fingerprint` (and repoint `TriageReports.GroupId`, `TriageResearchRuns.GroupId`); `HandoffIngestionRuns.ReplayIdentity`; federation `GlobalWorkspaceId`/`CanonicalWorkspaceId` from `ProxyId` and the new storage key, and on a hub the matching hash-keyed `Workspaces` row and its children.
  5. Rewrite `FileGitHubWorkspaceTokenStore` keys (file, after the database commit, with a backup copy of the file).
  6. Validate: zero Windows-shaped values containing `\` in kind-A columns; per-table row counts unchanged; per-workspace row counts unchanged under the mapping; full FK validation; unique indexes intact. Write the report (mapping, counts before and after).
- **Rollback.** Restore the backup taken in step 0 (the command refuses to run without one). The report's mapping allows an audit of every changed id.
- **Legacy fallback (decision D7).** TR-MCP-PATHNORM-001 currently says "legacy-key lookup fallback during transition". Recommended replacement: no fallback in lookups; the startup guard plus the mandatory rekey guarantee no legacy ids remain, and incoming backslash request paths are normalized to `/` by `IWorkspacePathNormalizer` anyway. A fallback would keep two identities alive and make the hub/proxy mapping ambiguous. TR-MCP-PATHNORM-001 is updated if the operator approves.

### 4c.4 P1.5 increments and tests (each per section 2a)

- **P1.5a** `f(x)` mapping and hash recomputation as pure functions: contract tests with scripted input/output tables (MockData), then the real functions.
- **P1.5b** Rekey planner: given an inventory (mocked store snapshot), produces the mapping and the statement plan; MockData fixture uses a scripted snapshot; Real fixture uses a seeded in-memory SQLite database.
- **P1.5c** Provider executors: SQLite real fixture in-memory; SQL Server and PostgreSQL fixtures follow the existing provider migration test pattern (`SqlServerDecompose4nfBackfillMigrationTests`, `PostgresDecompose4nfBackfillMigrationTests`; note those are currently in the baseline failure set, so their fixture health is checked first).
- **P1.5d** Command, startup guard, report and token-store rewrite.
- **P1.5e Rehearsal on a copy.** Restore a backup of `McpServer_PAYTON_LEGION2` to a scratch database on local `MSSQLSERVER`, run `--dry-run` then the real rekey, and record the validation report as receipts before touching the live database.

### 4c.5 Deployment order (after P1 and P2 pass their gates)

1. Back up the local database (`McpServer_PAYTON_LEGION2`) and the hub database (`McpServer_Omarchy`).
2. Stop the local service; run `--workspace-path-rekey` locally; deploy the P1/P2 build; start; verify.
3. Same on the hub (LAB-OMARCHY).
4. Re-enroll PAYTON-LEGION2 (decision D1) and verify the hub's federation rows carry the new ids.

### 4c.6 Decisions (operator, 2026-10-07)

- **D5 (decided: maintenance command):** explicit `--workspace-path-rekey` maintenance command plus startup guard; no automatic startup EF migration for the rewrite.
- **D6 (decided: defer or suspend constraints):** foreign keys are deferred or suspended inside one transaction per database and fully re-validated before commit; no copying of composite parents.
- **D7 (decided: no fallback):** the startup guard replaces the dual-key lookup; the server refuses to start against an unconverted store.

## 5. Out of scope / noted

- FR id mismatch: code comments cite FR-MCP-082/083/085 for federation query/push, but those ids are different features in `Functional-Requirements.md`. Correct the comments in the phase that touches each file.
- `C:\Windows\System32\drivers\etc\hosts` on PAYTON-LEGION2 maps LAB-OMARCHY to unreachable 192.168.1.182 (hub reachable at 10.42.0.214). Operator action.

## 6. Receipts

- Baseline build: `dotnet build QBrainAi.sln -c Debug` exit 0, 0 errors, 00:01:56.51 (2026-10-07, `artifacts-local/baseline-build.log`).
- Hub probe output: section 1.1 (2026-10-07).
- Prior evidence: 2026-10-06 session (workspace equivalence 55/55 ids, 9 field diffs; per-table row inventory).
