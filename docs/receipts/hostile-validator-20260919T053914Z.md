# Hostile validator receipt -- H1-green / MCP-MEMORY-002

- **TimestampUtc:** 20260919T053914Z
- **ValidatorIdentity:** GrokSubagentHostile
- **WorkClass:** Class 1 H1-green
- **DefaultPosture:** FAIL/UNKNOWN until independently re-verified with tools
- **TodoId:** MCP-MEMORY-002
- **PriorH1RedAgree:** docs/receipts/hostile-validator-20260919T052748Z.md (not rewritten)
- **planFile:** docs/plans/mcp-memory-002.md (§ H1-green)
- **acCatalog:** docs/plans/mcp-memory-002-ac-catalog.json (S1 rows)
- **PR:** https://github.com/sharpninja/McpServer/pull/44 (draft, head `cursor/memory-s1-green-4b97` into `memory-s1-red`)
- **GitHeadVerified:** `fd807466ff4a12e92f11954b516291fea534a4a0` (S1 Green HEAD; preferred over dirty local `memory` clone)
- **OverallVerdict:** **AGREE**
- **PASS / FAIL / UNKNOWN / N/A:** 16 / 0 / 0 / 0
- **Accuracy:** 99/100
- **Completeness:** 99/100
- **thresholdNote:** Operator AGREE floor for THIS run: every scored claim PASS AND Accuracy>=98 AND Completeness>=98. Observed Accuracy=99, Completeness=99, FAIL=0, UNKNOWN=0 -> AGREE.

## add-profile

- Hostile skill `~/.grok/skills/hostile-validator/SKILL.md`: **MISSING** on this box (honesty note; claims independently re-verified).
- Profile dirs checked: `/home/box/.claude/profile` (absent), `/home/box/.grok/profile` (absent).
- **ProfileFileCount:** 0

## Trust bootstrap

- Health `http://127.0.0.1:7147/health`: Healthy; storage reachable; federation disabled.
- API key: `/home/box/.config/mcpserver/mcpserver-workspace.apikey` (present).
- Live TODO: MCP connector `todo_get` **timed out**; re-verified with **curl + X-Api-Key + X-Workspace-Path** `GET /mcpserver/todo/MCP-MEMORY-002` -> `done:false`; remaining cites H1-green hostile. Validator did **not** mark Done.
- Evidence source: `git fetch` + detached worktree `/tmp/s1-green-wt` at Green HEAD above (not dirty local `memory` clone).

## Explicit FAIL list

(none)

## UNKNOWN list

(none)

## Independent test re-run (validator)

Command (worktree @ Green HEAD):
```
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter FullyQualifiedName~McpServer.Support.Mcp.Tests.Memory
```

- **Memory namespace:** Failed **0**, Passed **76**, Skipped **0**, Total **76** (73 catalog matrix + 3 mocks-first port).
- TRX: `/tmp/s1-green-test-results/memory-green.trx` — outcomes Passed=76.
- **S1 catalog matrix:** 73/73 methods matched in TRX and **Passed** (0 Missing, 0 Failed, 0 Skipped).
- **Mocks-first:** 3/3 Passed (`MemoryS1PortMockTests.PortMock_*`).
- **Compat (FR-MCP-MEMORY-001..007 + contract artifacts):** filter `MemoryServiceTests|MemoryControllerTests|MemoryEntityScopeTests|MemoryContractArtifactTests` = Failed **0**, Passed **28**, Skipped **0**.

### Focus Green surfaces (independent PASS)

- `MemoryMigrationTests.Applies_OnAllProviders` — Passed
- `MemoryMigrationTests.Backfill_ContentFromLegacyText` — Passed
- `MemoryEdgeTests.UniqueFromToType_Enforced` — Passed
- `MemoryVersionTests.Revert_RestoresSnapshot` — Passed
- `MemoryVersionTests.Revert_AppendsNewVersion` — Passed
- `MemoryAuthTests.ReadOnlyKey_CannotMutate` — Passed
- `MemoryIsolationTests.WorkspaceMemory_HiddenFromOtherWorkspace` — Passed
- `MemoryModelTests.PersistsMultiLayerFields` — Passed
- `MemoryListEffectiveTests.OmitsSoftDeleted` / `OrdersGlobalThenWorkspace` — Passed
- `MemoryInjectionTests.RequiredBlock_RawContentOnly` / `EmptySet_RendersNone` — Passed

### Accuracy residual (not claim FAIL)

- Hostile-validator skill + profile dirs absent on this box; MCP `todo_get` timed out (curl used). Disclosed; Accuracy −1.

## H1-green claim verdicts

### H1G-1 -- PASS
**Claim:** Every S1-tagged AC now has a passing named test (re-run, not trust prior paste).

**Evidence:** Catalog `slice==S1` = **73** unique methods. Independent TRX on Green HEAD: **73/73 Passed**, 0 missing, 0 failed, 0 skipped.

### H1G-2 -- PASS
**Claim:** Memory gate set Failed 0 Skipped 0 on Green HEAD.

**Evidence:** Independent `dotnet test` Memory namespace: Failed **0**, Passed **76**, Skipped **0**.

### H1G-3 -- PASS
**Claim:** Forward-only migrations named `*MemoryVersion*` on SQLite / PostgreSQL / SQL Server.

**Evidence:** Present on all three providers at `20260918223000_AddMemoryVersionAndEdgeStorage.cs` (class `AddMemoryVersionAndEdgeStorage`). `Down` throws `NotSupportedException` (forward-only). `MemoryMigrationTests.Applies_OnAllProviders` Passed.

### H1G-4 -- PASS
**Claim:** Content backfill from legacy Text.

**Evidence:** Each provider migration runs `UPDATE ... SET Content = Text WHERE Content IS NULL OR Content = ''` (SQLite/PostgreSQL quoted; SQL Server bracketed). `MemoryMigrationTests.Backfill_ContentFromLegacyText` Passed.

### H1G-5 -- PASS
**Claim:** Unique(From,To,EdgeType) enforced.

**Evidence:** Migrations create unique index `IX_MemoryEdges_FromMemoryId_ToMemoryId_EdgeType`. EF `McpDbContext` maps `MemoryEdgeEntity` with `HasIndex(From,To,EdgeType).IsUnique()`. `MemoryEdgeTests.UniqueFromToType_Enforced` Passed.

### H1G-6 -- PASS
**Claim:** Remember / ListVersions / Revert CQRS handlers exist and are registered.

**Evidence:** Handlers: `RememberMemoryCommandHandler`, `ListMemoryVersionsQueryHandler`, `RevertMemoryCommandHandler` (+ S1 `RecallMemoryQueryHandler`). Registration via `AddCqrsHandlers(typeof(RememberMemoryCommand).Assembly)` in `Program.cs` and `McpStdioHost.cs`; harness uses `AddCqrs(...)` with DI singleton `DbContextOptions`. Suite no longer returns 501 for remember/revert.

### H1G-7 -- PASS
**Claim:** Multi-layer persistence (Title/Summary/Content/Type/Tags/Confidence/provenance/EmbeddingStatus).

**Evidence:** `MemoryEntity` columns + migrations add columns; `McpDbContext` maps `MemoryEntity`/`MemoryVersionEntity`/`MemoryEdgeEntity` DbSets. `MemoryModelTests.PersistsMultiLayerFields` Passed. HEAD fix `fd807466` maps Tags property expected by S1 tests.

### H1G-8 -- PASS
**Claim:** Version revert restores snapshot and appends history.

**Evidence:** `MemoryCompetitiveOperations.RevertAsync` restores Title/Content from snapshot then `MemoryVersions.Add` with `NextVersionNumberAsync` (append-only). Tests `Revert_RestoresSnapshot` + `Revert_AppendsNewVersion` Passed.

### H1G-9 -- PASS
**Claim:** Compat Effective + REQUIRED MEMORIES raw contract unchanged / green.

**Evidence:** Compat filter 28/0/0. `MemoryInjectionTests.RequiredBlock_RawContentOnly` + `EmptySet_RendersNone` Passed. `MemoryRequiredMemoriesRenderer` renders Content-only block. Effective soft-delete omit + Global-then-Workspace ordering Passed.

### H1G-10 -- PASS
**Claim:** Read-only key cannot mutate.

**Evidence:** `MemoryAuthTests.ReadOnlyKey_CannotMutate` Passed; `ReadOnlyKey_CanListVersionsNotRevert` Passed. Operations return 403 when `readOnlyCaller`.

### H1G-11 -- PASS
**Claim:** Foreign workspace hidden; product membership does not share memories.

**Evidence:** `MemoryIsolationTests.WorkspaceMemory_HiddenFromOtherWorkspace` + `ProductMembership_DoesNotShareMemories` Passed.

### H1G-12 -- PASS
**Claim:** No public parallel domain service facade for new remember/revert verbs — CQRS-owned.

**Evidence:** `RememberMemoryCommand` XML docs + `MemoryCompetitiveOperations` state not a public `IMemoryService` remember facade. New verbs land as `ICommandHandler`/`IQueryHandler` types scanned by `AddCqrsHandlers`.

### H1G-13 -- PASS
**Claim:** No weakening of AC text or deletion of Red tests to achieve green.

**Evidence:** `git diff --name-status origin/memory-s1-red...HEAD` on tests: only `M` `MemoryS1Harness.cs` (DI: pass `_options` into `AddCqrs` dispatcher). No test file deletions; catalog method set still 73/73.

### H1G-14 -- PASS
**Claim:** No Python product path.

**Evidence:** `git ls-tree -r --name-only HEAD | rg '\\.py$'` -> empty.

### H1G-15 -- PASS
**Claim:** TODO MCP-MEMORY-002 still Done=false; validator did not mark done / merge / start S2.

**Evidence:** curl GET todo -> `done:false`. Remaining still points at H1-green. No merge of PR #44. No S2 work by this validator.

### H1G-16 -- PASS
**Claim:** Honesty: disclose gaps (missing hostile-validator skill, MCP timeout, dirty local clone avoided).

**Evidence:** Skill missing; ProfileFileCount=0; MCP timeout→curl; dirty `memory` clone → remote Green worktree `/tmp/s1-green-wt` @ `fd807466ff4a12e92f11954b516291fea534a4a0`.

## H1-green focus surface (plan §)

### H1G-F1 migrations SQLite/PostgreSQL/SQL Server -- PASS
Three `*MemoryVersion*` migrations present; Applies_OnAllProviders Passed.

### H1G-F2 Content backfill -- PASS
SQL backfill in all three; Backfill test Passed.

### H1G-F3 Unique(From,To,EdgeType) -- PASS
Unique index + UniqueFromToType_Enforced Passed.

### H1G-F4 compat Effective + REQUIRED MEMORIES raw -- PASS
Effective ordering/omit + injection raw Content tests Passed; compat 28/0/0.

### H1G-F5 read-only cannot mutate -- PASS
Auth tests Passed.

### H1G-F6 foreign workspace hidden -- PASS
Isolation tests Passed.

### H1G-F7 Version revert appends history -- PASS
RevertAsync appends next version row; Revert_AppendsNewVersion Passed.

### H1G-F8 S1-tagged matrix rows all Green -- PASS
73/73 S1 catalog methods Passed on independent re-run (full catalog 283 remains multi-slice; S1 gate scope only).

## B1–B6 workspace rules

### B1 Honesty -- PASS
Disclosed skill/profile gaps, MCP timeout, remote worktree preference.

### B2 Receipts -- PASS
New twin receipts only; prior H1-red AGREE not rewritten; TODO not marked done; no merge; no S2 start.

### B3 MCP-only storage -- PASS
TODO verified via HTTP API; no TODO.yaml hand-edit by validator.

### B4 No Python product/lab -- PASS
No `.py` on Green branch product tree.

### B5 Look-before-delete -- PASS
Read-only validation; no deletes of project docs/requirements.

### B6 Byrd phase-order at H1-green -- PASS
WorkClass=Class 1 H1-green. Validated Green implementation only; did not mark TODO done; did not merge; did not start S2.

## Notes (hostile)

- Counts from independent `dotnet test` on detached worktree at Green HEAD `fd807466`, not authorship narrative alone.
- Red→Green delta is implementation + harness DI wiring; Red suite methods retained.
- Hand-written migrations lack Designer companions; accepted because provider apply/backfill tests Passed independently.
- Did **not** set TODO done:true. Did **not** merge PRs. Did **not** start S2.

## Outcome

**AGREE** — H1-green gate may proceed to S2 subject to operator process. Accuracy and Completeness meet the >=98 strict threshold with zero FAIL/UNKNOWN on scored claims.
