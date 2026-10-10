# Hostile validator receipt -- H2-red / MCP-MEMORY-002

- **TimestampUtc:** 20260919T055510Z
- **ValidatorIdentity:** GrokSubagentHostile
- **WorkClass:** Class 1 H2-red
- **DefaultPosture:** FAIL/UNKNOWN until independently re-verified with tools
- **TodoId:** MCP-MEMORY-002
- **PriorH1GreenAgree:** docs/receipts/hostile-validator-20260919T053914Z.md (not rewritten)
- **planFile:** docs/plans/mcp-memory-002.md (§ H2-red)
- **acCatalog:** docs/plans/mcp-memory-002-ac-catalog.json (S2 rows)
- **PR:** https://github.com/sharpninja/McpServer/pull/47 (draft, head `memory-s2-red` → `cursor/memory-s1-green-4b97`)
- **GitHeadVerified:** `998ba295b31051bcb873175c95ad01f253d19f36` (S2 Red suite commit; detached worktree `/tmp/s2-red-wt`)
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
- Live TODO: MCP connector `todo_get` **timed out**; re-verified with **curl + X-Api-Key + X-Workspace-Path** `GET /mcpserver/todo/MCP-MEMORY-002` -> `done:false`; remaining still cites H1-green AGREE and "Next: S2 Red … then H2-red" (stale relative to draft PR #47; disclosed). Validator did **not** mark Done.
- Evidence source: `git fetch origin memory-s2-red` + detached worktree `/tmp/s2-red-wt` @ `998ba295` (not dirty local `memory` clone).
- Prerequisite: H1-green AGREE twin receipts present on this branch tip (`hostile-validator-20260919T053914Z.*`).

## Explicit FAIL list

(none)

## UNKNOWN list

(none)

## Independent test re-run (validator)

Command (worktree @ S2 Red HEAD):
```
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter FullyQualifiedName~McpServer.Support.Mcp.Tests.Memory
```

- **Memory namespace:** Failed **37**, Passed **88**, Skipped **0**, Total **125**.
- TRX: `/tmp/s2-red-test-results/memory-s2-red.trx` — outcomes Passed=88, Failed=37, Skipped=0.
- **S2 catalog matrix:** **40/40** methods present; **Failed 37, Passed 3, Skipped 0, Missing 0**.
- **S2 production passes (allowed pre-Green):** `MemoryRecallTests.NoMatches_ReturnsEmpty200`, `MemoryRecallTests.DoesNotSearchSessionLog`, `MemoryIndexerTests.Cancel_LeavesSafeStatus`.
- **S2 mocks-first:** **Passed 9, Failed 0, Skipped 0** (`MemoryS2PortMockTests.PortMock_*`).
- **S1 Green catalog in same filter:** **Failed 0, Passed 73, Skipped 0, Missing 0**.
- **S1 mocks-first:** Passed 3 (`MemoryS1PortMockTests.PortMock_*`).
- Compat (FR-MCP-MEMORY-001..007 + contract/TxnGated/McpTool/Federation filter): Failed **0**, Passed **37**, Skipped **0**.

### Focus Red surfaces (independent FAIL for missing hybrid/indexer)

- empty/null/whitespace query 400: Expected **400** Actual **200** (`EmptyQuery_Returns400`, `WhitespaceQuery_Returns400`, `NullQuery_Returns400`).
- exact + paraphrase fixtures: `ExactToken_ReturnsTargetInTopN` fails (hits lack Score); `Paraphrase_ReturnsTargetInTopN` fails (empty match collection — keyword reflector only).
- minScore/topN bounds: `MinScoreOutOfRange_Returns400` / `TopNNonPositive_Returns400` Expected 400 Actual 200; `MinScoreZero`/`MinScoreOne` fail (Score null); `TopNOmitted_UsesDefault` Expected 10 Actual 13; `TopNAboveMax_StableBehavior` Assert.True false.
- filter AND semantics: `CombinedFilters_AndSemantics` fails (Hybrid recall hits must include a score).
- soft-delete/foreign exclusion: named tests present; `DoesNotContain` for doomed/foreign runs before score assert — overall tests still **Failed** on missing Score for the allowed hit (see Accuracy residual).
- EmbeddingStatus transitions: `ReadyOrFailedStatus` / `Reconcile_RepairsStale` / `BatchIndex_AllTerminal` / `Reindex_AfterContentChange` — no Index/Reconcile/Batch handlers registered; `EmptyContent_NeverReady` Expected 400 Actual **501**.

### Accuracy residual (not claim FAIL)

- `SoftDeleted_NeverReturned` / `ForeignWorkspace_NeverReturned` fail on Score requirement after `DoesNotContain` for excluded ids — exclusion may already hold via S1 isolation/keyword path; Red matrix still correctly Failed. Disclosed; Accuracy −1.
- Hostile-validator skill + profile dirs absent; MCP `todo_get` timed out (curl used); live TODO remaining text not yet advanced to H2-red. Disclosed under honesty claim.

## H2-red claim verdicts

### H2R-1 -- PASS
**Claim:** S2 Red suite exists with catalog method names for all S2 ACs (40 matrix).

**Evidence:** Catalog `slice==S2` = **40** unique methods. Independent TRX on Red HEAD: **40/40** present (0 Missing). Methods span `MemoryRecallTests`, `MemoryIndexerTests`, and `MemoryAuthTests.ReadOnlyKey_CanRecall`.

### H2R-2 -- PASS
**Claim:** New production-path Red tests fail for missing hybrid/indexer behavior (not compile errors / not skips).

**Evidence:** Independent run Failed **37** / Skipped **0**. Failures: empty/null query still 200; missing Score on keyword hits; paraphrase empty; unbound minScore/topN; missing Index/Reconcile/Batch handlers (DI exception / 501). Suite compiled and executed.

### H2R-3 -- PASS
**Claim:** Mocks-first port tests may pass; production S2 matrix stays Red.

**Evidence:** S2 mocks-first **9/9 Passed**. S2 production **37 Failed / 3 Passed / 0 Skipped** — overall S2 acceptance remains Red until Green indexer/hybrid. Matches PR #47 narrative (125 total / 88 pass / 37 fail / 0 skip).

### H2R-4 -- PASS
**Claim:** No S2 Green indexer/hybrid claimed complete on the Red branch alone.

**Evidence:** `git grep` on suite HEAD: **no** Index/Reconcile/Batch command handlers. `RecallMemoryQueryHandler` still documents S1 keyword reflection and calls `operations.RecallAsync(keyword,…)`. Diff vs S1 Green base adds CQRS port commands + recall contracts + tests; production remains keyword reflector. PR #47: S2 Red only / does not complete S2 Green.

### H2R-5 -- PASS
**Claim:** S1 Green remains Failed 0 in the same Memory filter (no S1 regression).

**Evidence:** Independent TRX: S1 catalog **73/73 Passed**, Failed **0**, Skipped **0**.

### H2R-6 -- PASS
**Claim:** FR-001..007 compat not broken by Red PR.

**Evidence:** Re-ran compat/artifact filter on same worktree: Failed **0**, Passed **37**, Skipped **0**.

### H2R-7 -- PASS
**Claim:** No Python product path.

**Evidence:** `git ls-tree -r --name-only` suite HEAD | `rg '\.py$'` -> empty.

### H2R-8 -- PASS
**Claim:** TODO MCP-MEMORY-002 still Done=false; validator did not mark done / merge / implement S2 Green.

**Evidence:** curl GET todo -> `done:false`. Validator did **not** call todo update/done, did **not** merge PR #47, did **not** add indexer handlers.

### H2R-9 -- PASS
**Claim:** Honesty: disclose gaps (missing hostile-validator skill, MCP timeout, SoftDeleted/Foreign score residual, stale TODO remaining text).

**Evidence:** Skill missing; ProfileFileCount=0; MCP timeout→curl; dirty `memory` clone avoided via `/tmp/s2-red-wt`; SoftDeleted/Foreign Accuracy residual; TODO remaining still says "Next: S2 Red then H2-red" despite draft PR #47.

### H2R-10 -- PASS
**Claim:** Prerequisite H1-green AGREE exists and was not rewritten.

**Evidence:** Prior twin receipts `docs/receipts/hostile-validator-20260919T053914Z.md` + `.json` on branch; OverallVerdict AGREE; this run writes **new** H2-red twin pair only.

## H2-red focus surface (plan §)

### H2R-F1 empty/null query 400 -- PASS
Named tests `EmptyQuery_Returns400`, `WhitespaceQuery_Returns400`, `NullQuery_Returns400` exist; all Failed Expected 400 Actual 200. Mocks-first PortMock_Empty/Null Passed.

### H2R-F2 exact + paraphrase fixtures -- PASS
`ExactToken_ReturnsTargetInTopN` Failed (no Score); `Paraphrase_ReturnsTargetInTopN` Failed (empty matches). PortMock_ExactAndParaphrase Passed.

### H2R-F3 minScore/topN bounds -- PASS
Named bound tests present and Failed (400 not enforced; Score null; default topN not applied). PortMock_MinScoreAndTopNBounds Passed.

### H2R-F4 filter AND semantics -- PASS
`CombinedFilters_AndSemantics` present; Failed on missing Score. PortMock_CombinedFilters Passed.

### H2R-F5 soft-delete/foreign exclusion -- PASS
`SoftDeleted_NeverReturned` + `ForeignWorkspace_NeverReturned` named; both Failed overall (Score). PortMock_SoftDeletedAndForeign_Excluded Passed.

### H2R-F6 EmbeddingStatus transitions -- PASS
Indexer tests for ReadyOrFailed / Reconcile / Batch / Reindex / EmptyContent_NeverReady present; fail for unregistered handlers / 501. PortMock_Index_ReadyOrFailedStatus + PortMock_Reconcile + PortMock_BatchIndex Passed.

## B1–B6 workspace rules

### B1 Honesty -- PASS
Disclosed skill/profile gaps, MCP timeout, SoftDeleted/Foreign residual, stale TODO remaining, remote worktree preference.

### B2 Receipts -- PASS
New twin receipts only; prior H1-green AGREE not rewritten; TODO not marked done; no merge; no S2 Green implementation by validator.

### B3 MCP-only storage -- PASS
TODO verified via HTTP API; no TODO.yaml hand-edit by validator.

### B4 No Python product/lab -- PASS
No `.py` on Red branch product tree.

### B5 Look-before-delete -- PASS
Read-only validation; no deletes of project docs/requirements.

### B6 Byrd phase-order at H2-red -- PASS
WorkClass=Class 1 H2-red. Validated Red suite only; did not mark TODO done; did not merge; did not implement S2 Green.

## Notes (hostile)

- Counts from independent `dotnet test` on detached worktree at Red HEAD `998ba295`, not authorship narrative alone.
- PR #47 body counts (Total 125 / Passed 88 / Failed 37 / Skipped 0; S2 37/3/0; mocks 9; S1 73 Failed 0) match independent re-run.
- Log: `/tmp/s2-red-dotnet-test.log`; TRX: `/tmp/s2-red-test-results/memory-s2-red.trx`.
- Scored claim set for PASS/FAIL tally: **H2R-1..H2R-10 + H2R-F1..H2R-F6 = 16** (all PASS). B1–B6 also PASS (workspace rules; Completeness).
