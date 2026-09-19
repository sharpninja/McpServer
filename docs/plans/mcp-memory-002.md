# Plan: MCP-MEMORY-002 Competitive agent memory

**Superseded as an active planning draft.** Implementation through S7b/H7b is on `develop` (PR #49 merge `63f2e745`, PR #50 merge `09be2022`). Keep this file for AC/hostile-gate history. Current operator docs: `docs/context/memory.md`, `docs/benchmarks/README.md`, `docs/AGENT-PLUGIN-AVAILABILITY.md`. This refresh does not mark MCP-MEMORY-002 Done in the TODO store (MCP unavailable).

**TODO:** MCP-MEMORY-002 (high, Architecture, live Done state unknown without marker)
**Process:** Byrd Development Process v4 — `docs/Development-Process-draft-v4.md` + `skills/byrd-tdd-process/SKILL.md` + `docs/byrd-todo-execution-spec.md`
**Status:** Superseded planning draft. Shipped path: S1-S7a H-done, S7b/H7b on develop, H7a `agree:true`, `-Plugin all` unblocked. Tokens remain the primary bench metric.
**Baseline:** McpServer ~1.4.30 (`memory_*`, `/mcpserver/memory`, sessionlog, context hybrid, GraphRAG, `/mcp-transport`)
**Breaking change:** No for v1. Compat CRUD retained; FR-MCP-MEMORY-001..007 contracts remain in force.
**Hostile gates:** H-plan, H0, H1–H6 red/green, **H7a-red/green (Grok bench+integration value gate)**, **H7b-red/green (other plugins, after H7a)**, H-done. Default close after H7a. Tokens primary. AGREE+receipt required. See Hostile validation checkpoints.

---

## Byrd v4 + AC/TDD binding (non-negotiable)

1. **MCP trust first** before sessionlog/TODO/memory writes.
2. **V² Planning** before code; finite scope; proof = requirements + AC + tests.
3. **Dense AC on every FR/TR.** Each AC is a checkbox-ready, falsifiable edge (happy path, validation, auth, isolation, concurrency, unicode, bounds, compat, events). **Do not be stingy with AC** — more edges ⇒ better tests.
4. **Use cases cover 100% of FRs.** UC-MEMORY-001..009 Realizes FR-010..018; extensions call out edge classes that map to AC.
5. **TDD covers 100% of AC.** Matrix maps each AC id → ≥1 TEST id → ≥1 unit test method. Slice exit: every in-scope AC Red→(mocks)→Green; `./build.ps1 Test` Failed 0 Skipped 0. Skips do not count as coverage.
6. **Mocks-first** then real Green; Refactor; integration after units; ValidateConfig + ValidateTraceability.
7. **Hostile AGREE** at H-plan / H0 / H-red / H-green / H7a (and H7b if scoped) / H-done.

Skill: `skills/byrd-tdd-process` on every implementation slice.

---

## Problem / Value (V²)

**Problem:** Peer agent-memory products win with remember/recall/explore, hybrid retrieval, consolidate, graph, UI, and client kits. McpServer has guidance CRUD + adjacent surfaces, not a first-class consolidating memory substrate.

**Value:** Durable cross-session facts for agents; human governance; peer-competitive recall inside workspace/CQRS/traceability.

**Viable:** Extend existing memory domain; reuse ONNX/HNSW/FTS; CQRS-only; no Python product path.

---

## Locked decisions

1. Extend `/mcpserver/memory` and `memory_*` — do not fork.
2. Global + Workspace scopes; Products do not auto-share memories.
3. CQRS-only; no public `IMemoryService` facade for new verbs (handlers own API; existing service adapters thin).
4. Sessionlog stays transcript; promote is explicit only.
5. Separate memory index from context/repo index; optional pack source `memories`.
6. Local-first ONNX embeddings; cloud optional.
7. No Python product path.
8. Hebbian default OFF.
9. Hostile AGREE gates.
10. Planning (FR/TR/TEST/AC/use cases/matrix) before schema code.
11. Hostile validation: H-plan, H0, H1–H6 red/green, H7a (Grok value gate), optional H7b, H-done; AGREE required.
12. Existing FR-MCP-MEMORY-001..007 remain green; new work is additive FR-010..018.
13. Standardized with/without memory benchmarks (pack v1); **tokens used is primary metric**.
14. **Grok-first:** implement `mcpserver-grok-plugin` and run integration tests + token bench on Grok to validate performance and value **before** implementing other agent plugins.
15. Other plugins (claude-code, claude-cowork, cline, cline-v2, codex, copilot, opencode) are deferred until **H7a (Grok value gate) AGREE**. H7b is follow-on; H-done defaults to requiring H7a not H7b.

---

## Out of scope

Replace GraphRAG/sessionlog; federated memory (FR-008 remains separate); ZK vault; Kanban/GraphFlow; Magic NL codegen; silent auto-write of answers; SaaS billing.

---

## Use cases (must cover 100% of FRs)

Each use case Realizes the listed FRs. Detailed steps are authored into the MCP Use Case store during Planning (before H0).

### UC-MEMORY-001 Remember durable guidance
- **Actors:** Agent, Operator
- **Goal:** Persist a multi-layer memory and receive a `MEMORY-*` id.
- **Realizes:** FR-MCP-MEMORY-010
- **Main success:** `memory_remember` / compat add → stored → Effective list Global-then-Workspace.
- **Extensions / edges:** null/empty/whitespace content; invalid type/confidence; duplicate id; read-only key; cross-workspace invisibility; unicode/newlines; policy rejection; txn gating.

### UC-MEMORY-002 Recall by meaning and keyword
- **Actors:** Agent
- **Goal:** Retrieve ranked memories for a query (hybrid BM25+vector, optional rerank).
- **Realizes:** FR-MCP-MEMORY-011
- **Main success:** exact + paraphrase fixtures hit top-N; filters work.
- **Extensions / edges:** empty/null query; minScore/topN bounds; soft-delete/foreign exclusion; unindexed behavior; deterministic ties.

### UC-MEMORY-003 Explore related memories
- **Actors:** Agent, Operator
- **Goal:** Walk edges from a seed memory or query.
- **Realizes:** FR-MCP-MEMORY-012
- **Main success:** explicit edge returned; Hebbian off by default.
- **Extensions / edges:** depth bounds; self-loop; directed edges; foreign targets; soft-deleted neighbors.

### UC-MEMORY-004 Consolidate / sleep maintenance
- **Actors:** Operator, System (job), Agent
- **Goal:** Merge near-duplicates / decay without unauthorized hard-delete.
- **Realizes:** FR-MCP-MEMORY-013
- **Main success:** dry-run no writes; write mode one survivor + lineage.
- **Extensions / edges:** empty/single; three-way; lock contention; cancel consistency; same-scope only; SSE.

### UC-MEMORY-005 Version and revert
- **Actors:** Operator, Agent
- **Goal:** Inspect versions and revert safely.
- **Realizes:** FR-MCP-MEMORY-014
- **Main success:** update appends version; revert restores and appends.
- **Extensions / edges:** missing N; soft-deleted parent; foreign list; read-only cannot revert; contiguous versions.

### UC-MEMORY-006 Promote from session or context
- **Actors:** Agent, Operator
- **Goal:** Explicit promote with provenance; no silent sessionlog rewrite.
- **Realizes:** FR-MCP-MEMORY-015
- **Main success:** SourceKind/SourceRef set; source bytes unchanged.
- **Extensions / edges:** invalid ref; cross-workspace; no auto-promote; double promote policy.

### UC-MEMORY-007 Govern memories in UI
- **Actors:** Operator
- **Goal:** Search/open/edit/versions in `/memory/` for active workspace only.
- **Realizes:** FR-MCP-MEMORY-016
- **Main success:** CQRS parity; revert ≤3 actions; fail closed on foreign.
- **Extensions / edges:** empty state; no secrets in DOM; Escape closes panel; public REST only.

### UC-MEMORY-008 Agent onboarding with memory tools
- **Actors:** Agent author, Agent
- **Goal:** Discover and use memory tools via kits and eight plugins.
- **Realizes:** FR-MCP-MEMORY-017
- **Main success:** tools search + docs examples + **Grok plugin checklist first**; other plugins after H7a.
- **Extensions / edges:** agent-oriented descriptions; compat verbs retained; STDIO≡HTTP verb set; REPL typed methods.

### UC-MEMORY-009 Benchmark memory efficacy across plugin agents
- **Actors:** Operator, CI, Plugin agent under test
- **Goal:** Run a standardized prompt pack with and without memory on every supported plugin agent and compare outcomes.
- **Realizes:** FR-MCP-MEMORY-018
- **Main success:** **Grok** runs full pack under both conditions with tokens primary; Grok integration tests pass; H7a value AGREE. Other plugins after H7a. Correctness/safety gates; NEG/SAFE clean.
- **Extensions / edges:** stub vs live mode; injection vs tool-only hosts; disposable bench workspace isolation; configurable CI gate; deterministic stub re-run.

**Coverage rule:** FR-010..017 each appear in UC-001..008; **FR-018** appears in UC-009. Planning creates these UCs with Realizes links before H0.



---

## Functional & technical requirements with dense AC

**AC count:** 283. Grok-first bench/integration. Prefer adding AC over deleting.

### FR-MCP-MEMORY-010 Multi-layer MemoryItem
Memories store title/summary/content/type/tags/confidence/provenance while remaining Global/Workspace scoped.

- **AC-FR-MCP-MEMORY-010-01** memory_remember with valid payload returns 200/201 and a MEMORY-* id.
- **AC-FR-MCP-MEMORY-010-02** Persisted row includes Title, optional Summary, Content, Type in {fact,decision,preference,procedure,entity,other}, Tags, Confidence in [0,1], optional SourceKind/SourceRef.
- **AC-FR-MCP-MEMORY-010-03** Effective list order remains Global then Workspace for the calling workspace.
- **AC-FR-MCP-MEMORY-010-04** Compat memory_add|list|update|remove still works for pre-migration rows (legacy text mapped to Content).
- **AC-FR-MCP-MEMORY-010-05** Existing secrets/policy rejection behavior is unchanged (no weaker policy).
- **AC-FR-MCP-MEMORY-010-06** Soft-deleted memories are omitted from default list and recall.
- **AC-FR-MCP-MEMORY-010-07** Null request body on remember/add returns 400 with validation FailureKind.
- **AC-FR-MCP-MEMORY-010-08** Empty Content (and empty legacy text) returns 400 validation.
- **AC-FR-MCP-MEMORY-010-09** Whitespace-only Content returns 400 validation.
- **AC-FR-MCP-MEMORY-010-10** Content with internal newlines and punctuation round-trips exactly.
- **AC-FR-MCP-MEMORY-010-11** Unicode (BMP + supplementary plane emoji) in Title/Content round-trips exactly.
- **AC-FR-MCP-MEMORY-010-12** Invalid Type enum value returns 400.
- **AC-FR-MCP-MEMORY-010-13** Confidence < 0 returns 400.
- **AC-FR-MCP-MEMORY-010-14** Confidence > 1 returns 400.
- **AC-FR-MCP-MEMORY-010-15** Confidence omitted defaults to a documented value in [0,1].
- **AC-FR-MCP-MEMORY-010-16** Tags empty array is accepted; null Tags treated as empty.
- **AC-FR-MCP-MEMORY-010-17** Duplicate tag values are de-duplicated or rejected consistently (documented); behavior is stable across REST and MCP.
- **AC-FR-MCP-MEMORY-010-18** Tag longer than configured max length returns 400.
- **AC-FR-MCP-MEMORY-010-19** Title longer than configured max length returns 400.
- **AC-FR-MCP-MEMORY-010-20** Content longer than configured max length returns 400.
- **AC-FR-MCP-MEMORY-010-21** Explicit client-supplied MEMORY-* id that already exists as active returns 409 Conflict.
- **AC-FR-MCP-MEMORY-010-22** Invalid MEMORY-* id format when client-supplied returns 400.
- **AC-FR-MCP-MEMORY-010-23** Default scope when omitted is Workspace.
- **AC-FR-MCP-MEMORY-010-24** Scope=Global stores without owning workspace stamp; visible to all workspaces.
- **AC-FR-MCP-MEMORY-010-25** Workspace-scoped memory is invisible to a different workspace get/list/recall.
- **AC-FR-MCP-MEMORY-010-26** Read-only API key cannot remember/add/update/remove (403 or documented read-only failure).
- **AC-FR-MCP-MEMORY-010-27** Full-access key can mutate; get after add returns the same id and Content.
- **AC-FR-MCP-MEMORY-010-28** Get unknown id returns 404.
- **AC-FR-MCP-MEMORY-010-29** Get soft-deleted id returns 404 (not the tombstone payload) for normal callers.
- **AC-FR-MCP-MEMORY-010-30** Update unknown id returns 404.
- **AC-FR-MCP-MEMORY-010-31** Remove unknown id returns 404.
- **AC-FR-MCP-MEMORY-010-32** Remove is idempotent for already soft-deleted: second remove returns 404 or documented no-op success consistently.
- **AC-FR-MCP-MEMORY-010-33** Update can change Scope Global↔Workspace under validated rules; invalid transition returns 400.
- **AC-FR-MCP-MEMORY-010-34** Within each scope group, list sorts by Id ascending (ordinal).
- **AC-FR-MCP-MEMORY-010-35** CreatedAt/UpdatedAt are set on create; UpdatedAt advances on content update; CreatedAt is stable.
- **AC-FR-MCP-MEMORY-010-36** Author/updatedBy attribution is recorded on create and update.
- **AC-FR-MCP-MEMORY-010-37** Mutating remember/add/update/remove appends a sessionlog action when a turn is open (or documents no-turn behavior).
- **AC-FR-MCP-MEMORY-010-38** Transaction gating: mutation without required turn transaction fails closed per host policy.
- **AC-FR-MCP-MEMORY-010-39** Partial update: omitted optional Summary does not clear existing Summary unless explicit null/clear is defined.
- **AC-FR-MCP-MEMORY-010-40** Category (legacy) still accepted and round-trips; maps into Type or preserved field per docs.
- **AC-FR-MCP-MEMORY-010-41** Keyword list filter (compat) is case-insensitive substring over Content/Title.
- **AC-FR-MCP-MEMORY-010-42** Concurrent updates to same id: last-write-wins or version conflict is documented and stable (409 on stale base version if optimistic).
- **AC-FR-MCP-MEMORY-010-43** CancellationToken mid-write does not leave a half-written active row observable via get/list.
- **AC-FR-MCP-MEMORY-010-44** REQUIRED MEMORIES injection still uses Content (or legacy text) raw; Summary/confidence/tags never appear in the injected block.
- **AC-FR-MCP-MEMORY-010-45** Empty Effective set still renders REQUIRED MEMORIES / - None contract unchanged.
- **AC-FR-MCP-MEMORY-010-46** Product membership does not auto-share memories across member workspaces.
- **AC-FR-MCP-MEMORY-010-47** memory_list with invalid scope query returns 400.
- **AC-FR-MCP-MEMORY-010-48** Remember via MCP tool and REST produce equivalent stored fields for the same payload.
### FR-MCP-MEMORY-011 Recall hybrid search
`memory_recall` returns ranked memories via BM25 + vector fusion with optional filters.

- **AC-FR-MCP-MEMORY-011-01** Empty query returns 400.
- **AC-FR-MCP-MEMORY-011-02** Whitespace-only query returns 400.
- **AC-FR-MCP-MEMORY-011-03** Null query returns 400.
- **AC-FR-MCP-MEMORY-011-04** Seeded fixture: exact token query returns target in top-N with score ≥ minScore.
- **AC-FR-MCP-MEMORY-011-05** Seeded fixture: paraphrase/semantic query returns target in top-N with score ≥ minScore.
- **AC-FR-MCP-MEMORY-011-06** Tag filter excludes non-matching tags.
- **AC-FR-MCP-MEMORY-011-07** Type filter excludes non-matching types.
- **AC-FR-MCP-MEMORY-011-08** Scope filter honors Global vs Workspace visibility.
- **AC-FR-MCP-MEMORY-011-09** No matches returns 200 with empty items (not 404).
- **AC-FR-MCP-MEMORY-011-10** Soft-deleted memories never appear in recall results.
- **AC-FR-MCP-MEMORY-011-11** Other-workspace Workspace memories never appear in recall.
- **AC-FR-MCP-MEMORY-011-12** Global memories can appear in recall for any workspace.
- **AC-FR-MCP-MEMORY-011-13** minScore=0 includes all scored hits up to topN.
- **AC-FR-MCP-MEMORY-011-14** minScore=1 returns only perfect-score hits (or empty).
- **AC-FR-MCP-MEMORY-011-15** minScore < 0 or > 1 returns 400.
- **AC-FR-MCP-MEMORY-011-16** topN omitted uses documented default.
- **AC-FR-MCP-MEMORY-011-17** topN=0 or negative returns 400.
- **AC-FR-MCP-MEMORY-011-18** topN above configured max is clamped or 400 (documented); behavior stable.
- **AC-FR-MCP-MEMORY-011-19** Results are ordered by descending score; ties broken by Id ascending.
- **AC-FR-MCP-MEMORY-011-20** Each hit includes id, score, and enough fields to open the memory without a second round-trip (id minimum).
- **AC-FR-MCP-MEMORY-011-21** Query longer than configured max returns 400.
- **AC-FR-MCP-MEMORY-011-22** Combined tag+type+scope filters AND together.
- **AC-FR-MCP-MEMORY-011-23** Unindexed (EmbeddingStatus pending/failed) memories are excluded from vector path but may still BM25-match if FTS ready; documented.
- **AC-FR-MCP-MEMORY-011-24** After content update, recall eventually reflects new content (index refresh within documented bound or sync in test host).
- **AC-FR-MCP-MEMORY-011-25** Recall does not return memories from sessionlog; only Memory store.
- **AC-FR-MCP-MEMORY-011-26** Read-only API key may recall/list.
- **AC-FR-MCP-MEMORY-011-27** Unicode query tokens match unicode Content.
- **AC-FR-MCP-MEMORY-011-28** Identical scores: deterministic ordering across two consecutive recalls.
### FR-MCP-MEMORY-012 Explore links
`memory_explore` returns neighborhood over explicit and (if enabled) co-retrieved edges.

- **AC-FR-MCP-MEMORY-012-01** Given explicit edge A→B, explore(A) includes B with weight and EdgeType.
- **AC-FR-MCP-MEMORY-012-02** With Hebbian Enabled=false (default), explore never returns co-retrieved-only edges.
- **AC-FR-MCP-MEMORY-012-03** With Hebbian enabled, co-retrieval can create/strengthen co-retrieved edges without changing recall ranking path.
- **AC-FR-MCP-MEMORY-012-04** Unknown seed memory id returns 404.
- **AC-FR-MCP-MEMORY-012-05** Explore results confined to caller workspace Effective set (no cross-workspace leakage).
- **AC-FR-MCP-MEMORY-012-06** Soft-deleted seed returns 404.
- **AC-FR-MCP-MEMORY-012-07** Soft-deleted neighbor is omitted from explore results.
- **AC-FR-MCP-MEMORY-012-08** depth=1 returns only direct neighbors; depth>1 returns transitive up to depth (capped).
- **AC-FR-MCP-MEMORY-012-09** depth=0 or negative returns 400.
- **AC-FR-MCP-MEMORY-012-10** depth above max returns 400 or clamps (documented).
- **AC-FR-MCP-MEMORY-012-11** Self-loop edges are rejected on create (400) or never returned.
- **AC-FR-MCP-MEMORY-012-12** Duplicate explicit edge (same From,To,EdgeType) is idempotent upsert or 409 (documented).
- **AC-FR-MCP-MEMORY-012-13** Explore with query seed (no id) returns neighborhood around top recall hit or documented empty.
- **AC-FR-MCP-MEMORY-012-14** Edge weight outside allowed range rejected on create.
- **AC-FR-MCP-MEMORY-012-15** Invalid EdgeType rejected on create.
- **AC-FR-MCP-MEMORY-012-16** Hebbian off→on→off: while off, explore ignores previously created co-retrieved edges unless explicitly kept (documented).
- **AC-FR-MCP-MEMORY-012-17** Explore does not mutate memories' Content.
- **AC-FR-MCP-MEMORY-012-18** Bidirectional: A→B does not imply B→A unless a reverse edge exists.
- **AC-FR-MCP-MEMORY-012-19** maxNeighbors truncates result set; stable order by weight desc then id.
- **AC-FR-MCP-MEMORY-012-20** Creating edge to foreign-workspace memory id fails closed (400/403/404).
### FR-MCP-MEMORY-013 Consolidate
Sleep/consolidate merges near-duplicates, refreshes summaries, decays stale low-confidence items.

- **AC-FR-MCP-MEMORY-013-01** dry-run=true returns a merge plan and writes zero row mutations.
- **AC-FR-MCP-MEMORY-013-02** dry-run=false on two near-duplicate facts leaves exactly one surviving MEMORY-* with lineage via versions/edges.
- **AC-FR-MCP-MEMORY-013-03** Consolidate never hard-deletes unless AllowHardDelete (or equivalent) is true.
- **AC-FR-MCP-MEMORY-013-04** Per-workspace lock: concurrent consolidate returns busy/conflict without corrupting rows.
- **AC-FR-MCP-MEMORY-013-05** Successful write-mode consolidate emits SSE memory.consolidated (or documented event).
- **AC-FR-MCP-MEMORY-013-06** dry-run is the default when the flag is omitted.
- **AC-FR-MCP-MEMORY-013-07** Empty workspace (no memories) dry-run returns empty plan 200.
- **AC-FR-MCP-MEMORY-013-08** Single memory dry-run returns empty merge plan (nothing to merge).
- **AC-FR-MCP-MEMORY-013-09** Three near-duplicates collapse to one survivor in write mode.
- **AC-FR-MCP-MEMORY-013-10** Survivor Content/Summary is non-empty after merge.
- **AC-FR-MCP-MEMORY-013-11** Merged-away ids become soft-deleted (or redirected) and disappear from default list/recall.
- **AC-FR-MCP-MEMORY-013-12** Get on merged-away id returns 404 or redirect payload per docs (stable).
- **AC-FR-MCP-MEMORY-013-13** Consolidate never merges across workspaces.
- **AC-FR-MCP-MEMORY-013-14** Consolidate never merges Global into Workspace-owned without explicit policy (default: only same-scope pairs).
- **AC-FR-MCP-MEMORY-013-15** Decay: low-confidence stale items are flagged/decayed per policy without unauthorized hard-delete.
- **AC-FR-MCP-MEMORY-013-16** Read-only API key cannot run write-mode consolidate.
- **AC-FR-MCP-MEMORY-013-17** Plan items include candidate ids and similarity score.
- **AC-FR-MCP-MEMORY-013-18** Similarity threshold below 0 or above 1 returns 400.
- **AC-FR-MCP-MEMORY-013-19** Cancel during write-mode leaves store consistent (no half-merged graph).
- **AC-FR-MCP-MEMORY-013-20** After merge, explore edges are rewired to survivor or documented equivalent.
### FR-MCP-MEMORY-014 Version and revert
Every content mutation writes a version; revert restores prior content.

- **AC-FR-MCP-MEMORY-014-01** Updating Content increments VersionNumber and stores a snapshot.
- **AC-FR-MCP-MEMORY-014-02** GET versions returns versions ordered ascending by VersionNumber.
- **AC-FR-MCP-MEMORY-014-03** Revert to N restores Content (and declared mutable fields) to snapshot N.
- **AC-FR-MCP-MEMORY-014-04** Revert itself appends a new version (history preserved).
- **AC-FR-MCP-MEMORY-014-05** Revert to missing N returns 400/404.
- **AC-FR-MCP-MEMORY-014-06** Initial create yields VersionNumber=1 (or documented initial).
- **AC-FR-MCP-MEMORY-014-07** Update that does not change Content does not append a duplicate version (or appends no-op per docs).
- **AC-FR-MCP-MEMORY-014-08** Versions for soft-deleted memory are not listed to normal callers (404 on parent).
- **AC-FR-MCP-MEMORY-014-09** Foreign-workspace cannot list versions of a Workspace memory.
- **AC-FR-MCP-MEMORY-014-10** Revert to current N is idempotent content-wise and still appends or no-ops per docs.
- **AC-FR-MCP-MEMORY-014-11** Version snapshot includes Content and Title at minimum.
- **AC-FR-MCP-MEMORY-014-12** Version list pagination (if any) is stable; without pagination returns full history up to configured cap.
- **AC-FR-MCP-MEMORY-014-13** Read-only key can list versions but cannot revert.
- **AC-FR-MCP-MEMORY-014-14** After revert, recall/index refresh reflects reverted Content.
- **AC-FR-MCP-MEMORY-014-15** Version numbers are contiguous positive integers with no gaps after successful updates.
### FR-MCP-MEMORY-015 Promote
Explicit promote from sessionlog or context into memory with provenance.

- **AC-FR-MCP-MEMORY-015-01** Promote from sessionlog action/dialog creates memory with SourceKind=sessionlog and SourceRef set.
- **AC-FR-MCP-MEMORY-015-02** Promote from context chunk creates memory with SourceKind=context and SourceRef set.
- **AC-FR-MCP-MEMORY-015-03** After promote, referenced sessionlog rows are byte-identical (no silent rewrite).
- **AC-FR-MCP-MEMORY-015-04** Missing/invalid source ref returns 400.
- **AC-FR-MCP-MEMORY-015-05** Promoting a source from another workspace returns 403/404.
- **AC-FR-MCP-MEMORY-015-06** Null body returns 400.
- **AC-FR-MCP-MEMORY-015-07** Unsupported SourceKind returns 400.
- **AC-FR-MCP-MEMORY-015-08** Promote does not auto-run on sessionlog complete; only explicit promote API/tool.
- **AC-FR-MCP-MEMORY-015-09** Promoted Content is taken from source text without summarization unless caller supplies Summary.
- **AC-FR-MCP-MEMORY-015-10** Double promote of same SourceRef creates two memories or idempotent same id (documented); never corrupts source.
- **AC-FR-MCP-MEMORY-015-11** Read-only key cannot promote.
- **AC-FR-MCP-MEMORY-015-12** Promote result includes new MEMORY-* id and provenance fields.
- **AC-FR-MCP-MEMORY-015-13** Promote of deleted/missing session turn returns 404.
- **AC-FR-MCP-MEMORY-015-14** Context source promote does not copy sibling product-requirement chunks from other workspaces.
### FR-MCP-MEMORY-016 Memory UI
`/memory/` supports search, open, edit, version view for the active workspace.

- **AC-FR-MCP-MEMORY-016-01** Search UI lists only active-workspace Effective memories.
- **AC-FR-MCP-MEMORY-016-02** Edit persists via the same CQRS update path as REST.
- **AC-FR-MCP-MEMORY-016-03** Version view lists ordered versions; revert completes in ≤3 UI actions.
- **AC-FR-MCP-MEMORY-016-04** Opening another workspace's memory id fails closed.
- **AC-FR-MCP-MEMORY-016-05** Empty state shows a clear no-memories message (not a blank crash).
- **AC-FR-MCP-MEMORY-016-06** Search box with no hits shows empty results, not an error toast claiming server failure.
- **AC-FR-MCP-MEMORY-016-07** Soft-deleted memories are not listed in the default UI view.
- **AC-FR-MCP-MEMORY-016-08** Creating via UI requires non-empty Content; client-side or server 400 surfaced.
- **AC-FR-MCP-MEMORY-016-09** UI does not display raw API keys or workspace secrets.
- **AC-FR-MCP-MEMORY-016-10** Recall/search from UI respects tag chips/filters when present.
- **AC-FR-MCP-MEMORY-016-11** Keyboard: Escape closes detail/version panel without navigation errors.
- **AC-FR-MCP-MEMORY-016-12** UI calls only /mcpserver/memory* REST (no direct DB, no undocumented admin routes).
### FR-MCP-MEMORY-017 Agent onboarding
Documented Streamable HTTP + STDIO kits and plugins expose memory tools with agent-oriented descriptions. **Implement Grok plugin first**; other seven plugins only after Grok H7a value gate AGREE.

- **AC-FR-MCP-MEMORY-017-01** New memory verbs appear in /mcpserver/tools search.
- **AC-FR-MCP-MEMORY-017-02** Committed .mcp.json / docs examples for /mcp-transport and STDIO include the new verbs.
- **AC-FR-MCP-MEMORY-017-03** Plugin sync order is **grok first**, then the remaining seven (claude-code, claude-cowork, cline, cline-v2, codex, copilot, opencode) only after Grok H7 value gate AGREE. Each plugin eventually exposes skill/descriptor/shim for new verbs; Grok is the pilot lane.
- **AC-FR-MCP-MEMORY-017-04** One-prompt give-yourself-memory sample exists in docs with real tool names.
- **AC-FR-MCP-MEMORY-017-05** Tool descriptions are agent-oriented (say when to remember vs recall vs promote) not only schema echoes.
- **AC-FR-MCP-MEMORY-017-06** Compat verbs memory_add|list|update|remove remain listed alongside new verbs.
- **AC-FR-MCP-MEMORY-017-07** Marker template MCP Memories section documents new verbs without removing old ones.
- **AC-FR-MCP-MEMORY-017-08** Plugin validation fails CI if a synced skill omits a required new verb (checklist/guard).
- **AC-FR-MCP-MEMORY-017-09** STDIO and Streamable HTTP both advertise the same verb set for memory.
- **AC-FR-MCP-MEMORY-017-10** REPL exposes typed methods for remember/recall/explore/consolidate/promote/revert.
- **AC-FR-MCP-MEMORY-017-11** Invalid REPL memory method returns method-not-found envelope without crashing.
- **AC-FR-MCP-MEMORY-017-12** S5 checklist receipt has an explicit pass/fail row for **grok** before other plugins are required; full eight-row checklist is required only after Grok value gate (H7a) AGREE.
- **AC-FR-MCP-MEMORY-017-13** Implement **mcpserver-grok-plugin** memory surfaces (skill/descriptor/shim for new verbs + injection/fallback) in S5 before any other plugin sync work starts.
- **AC-FR-MCP-MEMORY-017-14** Non-Grok plugin sync PRs/checklists are not merged for MCP-MEMORY-002 until Grok H7a value gate AGREE (documentation of deferral allowed).


### TR-MCP-MEMORY-MODEL-002 Model/migrations
EF entities/migrations for extended Memory, Version, Edge on SQLite/PostgreSQL/SQL Server; backfill Content from legacy text.

- **AC-TR-MCP-MEMORY-MODEL-002-01** Migrations apply cleanly on SQLite, PostgreSQL, and SQL Server test hosts.
- **AC-TR-MCP-MEMORY-MODEL-002-02** Backfill sets Content from legacy text/guidance for all pre-existing rows.
- **AC-TR-MCP-MEMORY-MODEL-002-03** Unique (FromMemoryId, ToMemoryId, EdgeType) enforced at DB layer.
- **AC-TR-MCP-MEMORY-MODEL-002-04** MemoryVersion table FK to Memory cascades or restricts per documented policy; orphan versions impossible.
- **AC-TR-MCP-MEMORY-MODEL-002-05** Soft-delete column/filter remains enforced by default queries.
- **AC-TR-MCP-MEMORY-MODEL-002-06** Down migration (if supported) or forward-only policy is documented; test host can recreate schema.
- **AC-TR-MCP-MEMORY-MODEL-002-07** Indexes exist for (WorkspaceId, Scope), EmbeddingStatus, and FTS/HNSW side tables as applicable.
- **AC-TR-MCP-MEMORY-MODEL-002-08** Column lengths match validated max lengths in API (no silent truncate).
- **AC-TR-MCP-MEMORY-MODEL-002-09** Provider-specific types (uuid/jsonb/nvarchar) do not break round-trip of Tags JSON/array.
- **AC-TR-MCP-MEMORY-MODEL-002-10** Audit/append-only expectations for versions hold under concurrent inserts.
### TR-MCP-MEMORY-SEARCH-002 Index/search
Memory indexer hooks into embedding/HNSW + FTS; fusion weights configurable; optional ONNX rerank behind flag.

- **AC-TR-MCP-MEMORY-SEARCH-002-01** Indexing a memory flips EmbeddingStatus to ready (or failed with reason).
- **AC-TR-MCP-MEMORY-SEARCH-002-02** Startup/consolidate reconcile repairs stale EmbeddingStatus vs row hash.
- **AC-TR-MCP-MEMORY-SEARCH-002-03** Fusion weights from config change fixture ordering in a documented way.
- **AC-TR-MCP-MEMORY-SEARCH-002-04** Rerank remains off unless Mcp:Memory:Rerank:Enabled=true.
- **AC-TR-MCP-MEMORY-SEARCH-002-05** Failed embedding does not crash the host; memory remains listable via CRUD.
- **AC-TR-MCP-MEMORY-SEARCH-002-06** Re-index after Content change updates vector/FTS; old vector not returned for obsolete text queries exclusively.
- **AC-TR-MCP-MEMORY-SEARCH-002-07** HNSW/FTS side tables are workspace-safe (no cross-workspace ANN leakage).
- **AC-TR-MCP-MEMORY-SEARCH-002-08** Indexer respects CancellationToken and leaves status failed/pending not half-ready.
- **AC-TR-MCP-MEMORY-SEARCH-002-09** Local ONNX path works without cloud credentials in test host.
- **AC-TR-MCP-MEMORY-SEARCH-002-10** Cloud embedding path is opt-in and fails closed when disabled.
- **AC-TR-MCP-MEMORY-SEARCH-002-11** Empty Content cannot reach ready status (validation precedes index).
- **AC-TR-MCP-MEMORY-SEARCH-002-12** Batch index of N fixtures completes with all ready or explicit failures counted.
### TR-MCP-MEMORY-JOBS-002 Consolidate job
Consolidate hosted job with per-workspace lock; SSE event.

- **AC-TR-MCP-MEMORY-JOBS-002-01** Job respects per-workspace lock (GraphRAG-like).
- **AC-TR-MCP-MEMORY-JOBS-002-02** dry-run default is true unless explicitly overridden.
- **AC-TR-MCP-MEMORY-JOBS-002-03** Event payload includes workspace id and consolidate run id.
- **AC-TR-MCP-MEMORY-JOBS-002-04** Job can be disabled via config without affecting CRUD.
- **AC-TR-MCP-MEMORY-JOBS-002-05** Overlapping scheduled ticks for same workspace do not double-apply merges.
- **AC-TR-MCP-MEMORY-JOBS-002-06** Job logs structured start/end/error without writing secrets.
- **AC-TR-MCP-MEMORY-JOBS-002-07** Lock TTL/expiry recovers from crashed holder (documented test).
- **AC-TR-MCP-MEMORY-JOBS-002-08** Stats endpoint reflects last run time and last dry-run vs write mode.
### TR-MCP-MEMORY-API-002 API surfaces
CQRS + REST + MCP + client + REPL + plugin descriptors; error envelope unchanged.

- **AC-TR-MCP-MEMORY-API-002-01** Controllers/MCP/REPL/plugins/MemoryClient only dispatch CQRS handlers (no parallel domain service API).
- **AC-TR-MCP-MEMORY-API-002-02** Error responses use existing envelope (type/title/status/detail pattern) for new endpoints.
- **AC-TR-MCP-MEMORY-API-002-03** Compat memory_add|list|update|remove remain registered.
- **AC-TR-MCP-MEMORY-API-002-04** OpenAPI/swagger lists new routes under /mcpserver/memory.
- **AC-TR-MCP-MEMORY-API-002-05** MemoryClient methods mirror REST 1:1 for remember/recall/explore/consolidate/promote/revert/versions.
- **AC-TR-MCP-MEMORY-API-002-06** MCP tool input schemas reject unknown required-breaking fields per schema (additionalProperties policy documented).
- **AC-TR-MCP-MEMORY-API-002-07** Optional context pack source memories is off unless requested; when on, only Effective memories of caller.
- **AC-TR-MCP-MEMORY-API-002-08** SSE subscription for memory.* does not leak events across workspaces.
- **AC-TR-MCP-MEMORY-API-002-09** All mutating new endpoints honor transaction gating consistently with CRUD.
- **AC-TR-MCP-MEMORY-API-002-10** Idempotency-Key header if supported is documented; if not supported, duplicate posts create duplicates predictably.
### TR-MCP-MEMORY-UI-002 UI packaging
Static UI packaged like Use Case Manager; Nuke UpdateService deploy path.

- **AC-TR-MCP-MEMORY-UI-002-01** UI is served at /memory/ from packaged static assets.
- **AC-TR-MCP-MEMORY-UI-002-02** Deploy docs name Nuke UpdateService as the ship path.
- **AC-TR-MCP-MEMORY-UI-002-03** UI calls only public REST memory endpoints (no direct DB).
- **AC-TR-MCP-MEMORY-UI-002-04** Static assets are included in publish output / Linux service package.
- **AC-TR-MCP-MEMORY-UI-002-05** Deep link /memory/{id} opens detail or fails closed for unknown id.
- **AC-TR-MCP-MEMORY-UI-002-06** UI works behind the same API-key auth as other /mcpserver pages (or documented cookie bridge).
- **AC-TR-MCP-MEMORY-UI-002-07** Content-Security / no inline-eval policy matches sibling static UIs (Use Case Manager pattern).
- **AC-TR-MCP-MEMORY-UI-002-08** 404 for /memory/unknown-asset does not serve index with 200 incorrectly for missing hashed bundles.

---

### FR-MCP-MEMORY-018 Cross-plugin with/without memory benchmarks
Versioned prompt pack under paired without_memory / with_memory conditions. **Primary metric: tokens used.** **Pilot host: Grok** for integration tests and required CI bench. Other plugins after H7a AGREE. Pass/fail is correctness/safety gate only.

- **AC-FR-MCP-MEMORY-018-01** Canonical prompt fixture pack lives at docs/benchmarks/memory-prompt-pack-v1.yaml (or .json) and is versioned in git.
- **AC-FR-MCP-MEMORY-018-02** Pack defines ≥8 prompts spanning: preference, decision, fact-paraphrase, procedure, multi-fact, negative-absent, conflict-stale-vs-new, refuse-invented-secret.
- **AC-FR-MCP-MEMORY-018-03** Each prompt entry has: id, class, user_text, gold_answer_rubric, seeded_memories[], forbidden_claims[], scoring (exact|contains|rubric).
- **AC-FR-MCP-MEMORY-018-04** Each prompt declares paired conditions: without_memory and with_memory (same user_text).
- **AC-FR-MCP-MEMORY-018-05** without_memory condition: Effective memory set empty OR injection suppressed AND memory_recall tools unavailable/stubbed per harness mode — documented and enforced.
- **AC-FR-MCP-MEMORY-018-06** with_memory condition: seeded_memories loaded into workspace; REQUIRED MEMORIES injection enabled on hosts that support it; memory_recall/remember tools available.
- **AC-FR-MCP-MEMORY-018-07** Harness supports all eight plugins, but **default/required CI path runs Grok first**. Other plugins are opt-in until Grok with/without token bench + integration tests prove performance and value (H7a AGREE).
- **AC-FR-MCP-MEMORY-018-08** Per plugin × prompt × condition, harness records: transcript, tool calls, injected block presence, **tokens_in, tokens_out, tokens_total** (primary metric), latency_ms (secondary), score, pass/fail (correctness gate), notes.
- **AC-FR-MCP-MEMORY-018-09** Results artifact docs/benchmarks/results/memory-bench-<utc>.json includes per-cell token fields and a summary markdown table of tokens by plugin × condition.
- **AC-FR-MCP-MEMORY-018-10** Scoring is deterministic for exact/contains rubrics; subjective rubric prompts use a fixed checklist (≥2 independent raters or locked auto-rubric) documented in the pack.
- **AC-FR-MCP-MEMORY-018-11** Primary efficacy metric is tokens: for each plugin, report mean and median tokens_total (and tokens_in/tokens_out) across pack prompts under with_memory vs without_memory; correctness pass/fail is a gate, not the primary comparison.
- **AC-FR-MCP-MEMORY-018-12** On negative-absent prompts: with_memory must not invent seeded-looking facts; without_memory must not claim remembered preferences — both conditions forbid hallucination per forbidden_claims.
- **AC-FR-MCP-MEMORY-018-13** On conflict-stale-vs-new: with_memory prefers the newer/higher-confidence seeded memory per pack gold; without_memory does not assert the seeded newer fact.
- **AC-FR-MCP-MEMORY-018-14** when injection is supported, with_memory transcripts include the REQUIRED MEMORIES header contract; without_memory transcripts do not include seeded memory text.
- **AC-FR-MCP-MEMORY-018-15** When tools are the recall path (no injection host): with_memory runs show memory_recall (or list) tool use before answering gold; without_memory shows no successful recall of seeded ids.
- **AC-FR-MCP-MEMORY-018-16** Plugin `grok` **must** complete the full pack under both conditions without harness crash; cell results including tokens_* present for every prompt id. Required before other plugins.
- **AC-FR-MCP-MEMORY-018-17** Plugin `claude-code` completes the full pack under both conditions after Grok H7a value gate AGREE; until then this AC is tracked but not gating H-done (gated by H7b instead).
- **AC-FR-MCP-MEMORY-018-18** Plugin `claude-cowork` completes the full pack under both conditions after Grok H7a value gate AGREE; until then this AC is tracked but not gating H-done (gated by H7b instead).
- **AC-FR-MCP-MEMORY-018-19** Plugin `cline` completes the full pack under both conditions after Grok H7a value gate AGREE; until then this AC is tracked but not gating H-done (gated by H7b instead).
- **AC-FR-MCP-MEMORY-018-20** Plugin `cline-v2` completes the full pack under both conditions after Grok H7a value gate AGREE; until then this AC is tracked but not gating H-done (gated by H7b instead).
- **AC-FR-MCP-MEMORY-018-21** Plugin `codex` completes the full pack under both conditions after Grok H7a value gate AGREE; until then this AC is tracked but not gating H-done (gated by H7b instead).
- **AC-FR-MCP-MEMORY-018-22** Plugin `copilot` completes the full pack under both conditions after Grok H7a value gate AGREE; until then this AC is tracked but not gating H-done (gated by H7b instead).
- **AC-FR-MCP-MEMORY-018-23** Plugin `opencode` completes the full pack under both conditions after Grok H7a value gate AGREE; until then this AC is tracked but not gating H-done (gated by H7b instead).
- **AC-FR-MCP-MEMORY-018-24** Cross-plugin summary compares with_memory vs without_memory pass-rate per plugin and overall macro average.
- **AC-FR-MCP-MEMORY-018-25** Benchmark does not require network cloud models when a recorded/stub agent mode is selected; live mode is opt-in and documented.
- **AC-FR-MCP-MEMORY-018-26** Live mode (optional) uses the same pack and scoring; results tagged mode=live|stub|recorded.
- **AC-FR-MCP-MEMORY-018-27** Seeded memories for the pack use only Workspace scope in a disposable bench workspace; Global seeds are explicit and cleaned up.
- **AC-FR-MCP-MEMORY-018-28** Bench workspace isolation: pack run cannot read or mutate the operator's primary workspace memories.
- **AC-FR-MCP-MEMORY-018-29** Failing a lift threshold in CI is configurable: default report-only on first land; gate mode fails S7/H7 when Memory:Bench:Gate=true.
- **AC-FR-MCP-MEMORY-018-30** Pack prompts are free of real secrets; fixtures use synthetic tokens like BENCH-PREF-EDITOR=neovim.
- **AC-FR-MCP-MEMORY-018-31** Re-run of stub/recorded mode on same commit produces identical pass/fail vector (byte-stable scores for exact/contains).
- **AC-FR-MCP-MEMORY-018-32** docs/benchmarks/README.md documents how to run pack for one plugin and for all eight, with and without memory.
- **AC-FR-MCP-MEMORY-018-33** USER-GUIDE or MCP-SERVER links to the benchmark pack as the standard memory efficacy check for plugins.
- **AC-FR-MCP-MEMORY-018-34** Token accounting uses the plugin host's reported usage when available; otherwise a documented tokenizer estimate (named model/encoding) with estimator_id recorded on the cell.
- **AC-FR-MCP-MEMORY-018-35** tokens_in counts prompt/system/injection/tool-result tokens attributable to the turn; tokens_out counts completion tokens; tokens_total = tokens_in + tokens_out.
- **AC-FR-MCP-MEMORY-018-36** REQUIRED MEMORIES injection (with_memory) is included in tokens_in when present; without_memory cells must not charge injection tokens for suppressed memory.
- **AC-FR-MCP-MEMORY-018-37** Tool-call payloads and tool results in the turn are included in tokens_in per host accounting rules (documented); both conditions use the same rules.
- **AC-FR-MCP-MEMORY-018-38** Summary table columns include at least: plugin, condition, prompts_n, tokens_total_sum, tokens_total_mean, tokens_total_median, tokens_in_mean, tokens_out_mean, pass_rate.
- **AC-FR-MCP-MEMORY-018-39** Per-prompt delta row: tokens_total(with_memory) - tokens_total(without_memory) for the same plugin and prompt id (can be positive when injection adds context).
- **AC-FR-MCP-MEMORY-018-40** Macro metric: mean tokens_total across all plugins for with_memory vs without_memory; reported as primary headline numbers in the summary markdown.
- **AC-FR-MCP-MEMORY-018-41** Correctness gate: preference/decision/fact with_memory cells must pass gold rubric; failures invalidate using that cell in optional efficiency ratios but tokens are still recorded.
- **AC-FR-MCP-MEMORY-018-42** Optional efficiency ratio tokens_total / max(pass,ε) may be reported but must be labeled secondary; never replace raw tokens as the primary metric.
- **AC-FR-MCP-MEMORY-018-43** Stub/recorded mode still emits token fields (from recording or estimator); missing tokens_* fails the cell schema.
- **AC-FR-MCP-MEMORY-018-44** NEG/SAFE prompts still record tokens under both conditions; safety pass/fail remains mandatory alongside token capture.
- **AC-FR-MCP-MEMORY-018-45** Benchmark README states that tokens used is the primary metric; pass/fail is correctness/safety.
- **AC-FR-MCP-MEMORY-018-46** Integration tests for memory (*.IntegrationTests and plugin-host integration) use the **Grok** agent/plugin lane as the pilot host before other agents.
- **AC-FR-MCP-MEMORY-018-47** Grok value gate (H7a): after Grok with/without token bench + Grok integration tests, operator/hostile AGREE that token metrics and correctness demonstrate performance and value sufficient to proceed to other plugins.
- **AC-FR-MCP-MEMORY-018-48** Until H7a AGREE, CI must not require non-Grok plugin bench cells to be green; running them is optional/experimental.
- **AC-FR-MCP-MEMORY-018-49** Live or stub Grok bench mode records tokens as primary metric; Grok integration tests assert end-to-end remember→recall (or injection) path on the Grok plugin.
- **AC-FR-MCP-MEMORY-018-50** H7b (remaining seven plugins) is blocked until H7a AGREE; H-done may proceed after H7a if remaining plugins are explicitly deferred in doneSummary — OR H-done waits for H7b when operator demands full eight. Default: **H-done requires H7a; H7b is a follow-on TODO/slice unless operator expands scope.**


### TR-MCP-MEMORY-BENCH-002 Benchmark harness
pwsh/dotnet/plugin runners; result schema requires token fields; **default plugin = grok**; `-Plugin all` only post-H7a. CI lead metrics = token means.

- **AC-TR-MCP-MEMORY-BENCH-002-01** Harness is pwsh and/or dotnet test + plugin bats/node runners only (no Python product path).
- **AC-TR-MCP-MEMORY-BENCH-002-02** Harness invokes each plugin through its supported validation entrypoint (bats/ts/REPL) with a documented adapter interface.
- **AC-TR-MCP-MEMORY-BENCH-002-03** Result schema is JSON-schema validated before write.
- **AC-TR-MCP-MEMORY-BENCH-002-04** Pack schema is JSON-schema/YAML-schema validated in CI.
- **AC-TR-MCP-MEMORY-BENCH-002-05** Bench can suppress injection and tool registration independently to form without_memory matrix cells.
- **AC-TR-MCP-MEMORY-BENCH-002-06** Timing metrics captured but not used as sole pass/fail unless pack marks latency_budget_ms.
- **AC-TR-MCP-MEMORY-BENCH-002-07** Nuke/CI target `BenchMemory` (or equivalent) runs stub mode for **Grok by default**; all plugins only with explicit `-Plugin all` after H7a.
- **AC-TR-MCP-MEMORY-BENCH-002-08** Artifacts are gitignored under docs/benchmarks/results/* except committed baselines/golden stub vectors if any.
- **AC-TR-MCP-MEMORY-BENCH-002-09** Result JSON schema requires integers tokens_in, tokens_out, tokens_total ≥ 0 and string token_source ∈ {host,estimator,recorded}.
- **AC-TR-MCP-MEMORY-BENCH-002-10** Harness refuses to finalize a run if any cell lacks token fields (hard fail, not warn).
- **AC-TR-MCP-MEMORY-BENCH-002-11** Estimator implementations are pure and versioned (estimator_id + estimator_version) for deterministic stub re-runs.
- **AC-TR-MCP-MEMORY-BENCH-002-12** CI BenchMemory summary prints token means by plugin × condition as the first metrics block.
- **AC-TR-MCP-MEMORY-BENCH-002-13** BenchMemory default `-Plugin grok` (or equivalent); `-Plugin all` is explicit and post-H7a.
- **AC-TR-MCP-MEMORY-BENCH-002-14** Grok integration test project/host wiring is documented in docs/benchmarks/README.md as the pilot validation path.


## Testing requirements

- **TEST-MCP-MEMORY-010** Model/migration/backfill/isolation/list
- **TEST-MCP-MEMORY-011** Recall + indexer
- **TEST-MCP-MEMORY-012** Edge + explore + Hebbian
- **TEST-MCP-MEMORY-013** Consolidate + job
- **TEST-MCP-MEMORY-014** Version/revert
- **TEST-MCP-MEMORY-015** Promote + sessionlog immutability
- **TEST-MCP-MEMORY-016** API/MCP/client/REPL/auth/injection/onboarding contracts
- **TEST-MCP-MEMORY-017** UI smoke / hosting / CQRS path parity
- **TEST-MCP-MEMORY-018** Cross-plugin with/without memory benchmark pack + harness

### AC → TEST → slice → unit test method (100% coverage contract)

| AC id | TEST id | Slice | Planned unit test |
|---|---|---|---|
| AC-FR-MCP-MEMORY-010-01 | TEST-MCP-MEMORY-016 | S1 | `MemoryRememberTests.Remember_ValidPayload_ReturnsMemoryId` |
| AC-FR-MCP-MEMORY-010-02 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.PersistsMultiLayerFields` |
| AC-FR-MCP-MEMORY-010-03 | TEST-MCP-MEMORY-010 | S1 | `MemoryListEffectiveTests.OrdersGlobalThenWorkspace` |
| AC-FR-MCP-MEMORY-010-04 | TEST-MCP-MEMORY-016 | S1 | `MemoryCompatCrudTests.LegacyRows_ListUpdateRemove_StillWork` |
| AC-FR-MCP-MEMORY-010-05 | TEST-MCP-MEMORY-010 | S1 | `MemoryPolicyTests.SecretsPolicy_Unchanged` |
| AC-FR-MCP-MEMORY-010-06 | TEST-MCP-MEMORY-010 | S1 | `MemoryListEffectiveTests.OmitsSoftDeleted` |
| AC-FR-MCP-MEMORY-010-07 | TEST-MCP-MEMORY-016 | S1 | `MemoryRememberTests.NullBody_Returns400` |
| AC-FR-MCP-MEMORY-010-08 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.EmptyContent_Returns400` |
| AC-FR-MCP-MEMORY-010-09 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.WhitespaceOnlyContent_Returns400` |
| AC-FR-MCP-MEMORY-010-10 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.NewlinesPunctuation_RoundTripExact` |
| AC-FR-MCP-MEMORY-010-11 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.Unicode_RoundTripExact` |
| AC-FR-MCP-MEMORY-010-12 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.InvalidType_Returns400` |
| AC-FR-MCP-MEMORY-010-13 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.ConfidenceBelowZero_Returns400` |
| AC-FR-MCP-MEMORY-010-14 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.ConfidenceAboveOne_Returns400` |
| AC-FR-MCP-MEMORY-010-15 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.ConfidenceOmitted_UsesDefault` |
| AC-FR-MCP-MEMORY-010-16 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.NullOrEmptyTags_Accepted` |
| AC-FR-MCP-MEMORY-010-17 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.DuplicateTags_StableBehavior` |
| AC-FR-MCP-MEMORY-010-18 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.TagTooLong_Returns400` |
| AC-FR-MCP-MEMORY-010-19 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.TitleTooLong_Returns400` |
| AC-FR-MCP-MEMORY-010-20 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.ContentTooLong_Returns400` |
| AC-FR-MCP-MEMORY-010-21 | TEST-MCP-MEMORY-016 | S1 | `MemoryRememberTests.DuplicateActiveId_Returns409` |
| AC-FR-MCP-MEMORY-010-22 | TEST-MCP-MEMORY-016 | S1 | `MemoryRememberTests.InvalidIdFormat_Returns400` |
| AC-FR-MCP-MEMORY-010-23 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.DefaultScope_IsWorkspace` |
| AC-FR-MCP-MEMORY-010-24 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.GlobalScope_VisibleEverywhere` |
| AC-FR-MCP-MEMORY-010-25 | TEST-MCP-MEMORY-010 | S1 | `MemoryIsolationTests.WorkspaceMemory_HiddenFromOtherWorkspace` |
| AC-FR-MCP-MEMORY-010-26 | TEST-MCP-MEMORY-016 | S1 | `MemoryAuthTests.ReadOnlyKey_CannotMutate` |
| AC-FR-MCP-MEMORY-010-27 | TEST-MCP-MEMORY-016 | S1 | `MemoryAuthTests.FullKey_MutateThenGet` |
| AC-FR-MCP-MEMORY-010-28 | TEST-MCP-MEMORY-016 | S1 | `MemoryGetTests.UnknownId_Returns404` |
| AC-FR-MCP-MEMORY-010-29 | TEST-MCP-MEMORY-016 | S1 | `MemoryGetTests.SoftDeleted_Returns404` |
| AC-FR-MCP-MEMORY-010-30 | TEST-MCP-MEMORY-016 | S1 | `MemoryUpdateTests.UnknownId_Returns404` |
| AC-FR-MCP-MEMORY-010-31 | TEST-MCP-MEMORY-016 | S1 | `MemoryRemoveTests.UnknownId_Returns404` |
| AC-FR-MCP-MEMORY-010-32 | TEST-MCP-MEMORY-016 | S1 | `MemoryRemoveTests.SecondRemove_StableBehavior` |
| AC-FR-MCP-MEMORY-010-33 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.ScopeTransition_Validated` |
| AC-FR-MCP-MEMORY-010-34 | TEST-MCP-MEMORY-010 | S1 | `MemoryListEffectiveTests.WithinScope_SortsByIdOrdinal` |
| AC-FR-MCP-MEMORY-010-35 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.Timestamps_CreateAndUpdate` |
| AC-FR-MCP-MEMORY-010-36 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.Attribution_Recorded` |
| AC-FR-MCP-MEMORY-010-37 | TEST-MCP-MEMORY-015 | S1 | `MemorySessionLogTests.Mutations_AppendActionWhenTurnOpen` |
| AC-FR-MCP-MEMORY-010-38 | TEST-MCP-MEMORY-016 | S1 | `MemoryTxnTests.MissingTurn_FailsClosed` |
| AC-FR-MCP-MEMORY-010-39 | TEST-MCP-MEMORY-010 | S1 | `MemoryModelTests.PartialUpdate_DoesNotClearSummary` |
| AC-FR-MCP-MEMORY-010-40 | TEST-MCP-MEMORY-010 | S1 | `MemoryCompatCrudTests.LegacyCategory_RoundTrips` |
| AC-FR-MCP-MEMORY-010-41 | TEST-MCP-MEMORY-010 | S1 | `MemoryListEffectiveTests.KeywordFilter_CaseInsensitive` |
| AC-FR-MCP-MEMORY-010-42 | TEST-MCP-MEMORY-010 | S1 | `MemoryConcurrencyTests.ConcurrentUpdate_DocumentedSemantics` |
| AC-FR-MCP-MEMORY-010-43 | TEST-MCP-MEMORY-010 | S1 | `MemoryConcurrencyTests.CancelMidWrite_NoPartialVisible` |
| AC-FR-MCP-MEMORY-010-44 | TEST-MCP-MEMORY-016 | S1 | `MemoryInjectionTests.RequiredBlock_RawContentOnly` |
| AC-FR-MCP-MEMORY-010-45 | TEST-MCP-MEMORY-016 | S1 | `MemoryInjectionTests.EmptySet_RendersNone` |
| AC-FR-MCP-MEMORY-010-46 | TEST-MCP-MEMORY-010 | S1 | `MemoryIsolationTests.ProductMembership_DoesNotShareMemories` |
| AC-FR-MCP-MEMORY-010-47 | TEST-MCP-MEMORY-016 | S1 | `MemoryListTests.InvalidScopeQuery_Returns400` |
| AC-FR-MCP-MEMORY-010-48 | TEST-MCP-MEMORY-016 | S1 | `MemorySurfaceParityTests.McpAndRest_EquivalentStore` |
| AC-FR-MCP-MEMORY-011-01 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.EmptyQuery_Returns400` |
| AC-FR-MCP-MEMORY-011-02 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.WhitespaceQuery_Returns400` |
| AC-FR-MCP-MEMORY-011-03 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.NullQuery_Returns400` |
| AC-FR-MCP-MEMORY-011-04 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.ExactToken_ReturnsTargetInTopN` |
| AC-FR-MCP-MEMORY-011-05 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.Paraphrase_ReturnsTargetInTopN` |
| AC-FR-MCP-MEMORY-011-06 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.TagFilter_ExcludesOthers` |
| AC-FR-MCP-MEMORY-011-07 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.TypeFilter_ExcludesOthers` |
| AC-FR-MCP-MEMORY-011-08 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.ScopeFilter_HonorsVisibility` |
| AC-FR-MCP-MEMORY-011-09 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.NoMatches_ReturnsEmpty200` |
| AC-FR-MCP-MEMORY-011-10 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.SoftDeleted_NeverReturned` |
| AC-FR-MCP-MEMORY-011-11 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.ForeignWorkspace_NeverReturned` |
| AC-FR-MCP-MEMORY-011-12 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.Global_VisibleToAllWorkspaces` |
| AC-FR-MCP-MEMORY-011-13 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.MinScoreZero_IncludesAllHits` |
| AC-FR-MCP-MEMORY-011-14 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.MinScoreOne_OnlyPerfect` |
| AC-FR-MCP-MEMORY-011-15 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.MinScoreOutOfRange_Returns400` |
| AC-FR-MCP-MEMORY-011-16 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.TopNOmitted_UsesDefault` |
| AC-FR-MCP-MEMORY-011-17 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.TopNNonPositive_Returns400` |
| AC-FR-MCP-MEMORY-011-18 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.TopNAboveMax_StableBehavior` |
| AC-FR-MCP-MEMORY-011-19 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.OrderedByScoreThenId` |
| AC-FR-MCP-MEMORY-011-20 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.HitIncludesIdAndScore` |
| AC-FR-MCP-MEMORY-011-21 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.QueryTooLong_Returns400` |
| AC-FR-MCP-MEMORY-011-22 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.CombinedFilters_AndSemantics` |
| AC-FR-MCP-MEMORY-011-23 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.Unindexed_DocumentedBehavior` |
| AC-FR-MCP-MEMORY-011-24 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.AfterUpdate_IndexReflectsNewContent` |
| AC-FR-MCP-MEMORY-011-25 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.DoesNotSearchSessionLog` |
| AC-FR-MCP-MEMORY-011-26 | TEST-MCP-MEMORY-016 | S2 | `MemoryAuthTests.ReadOnlyKey_CanRecall` |
| AC-FR-MCP-MEMORY-011-27 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.UnicodeQuery_MatchesContent` |
| AC-FR-MCP-MEMORY-011-28 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.TieBreak_Deterministic` |
| AC-FR-MCP-MEMORY-012-01 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.ExplicitEdge_ReturnedWithWeight` |
| AC-FR-MCP-MEMORY-012-02 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.HebbianOff_NoCoRetrievedOnlyEdges` |
| AC-FR-MCP-MEMORY-012-03 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.HebbianOn_StrengthensCoRetrieved` |
| AC-FR-MCP-MEMORY-012-04 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.UnknownSeed_Returns404` |
| AC-FR-MCP-MEMORY-012-05 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.NoCrossWorkspaceLeakage` |
| AC-FR-MCP-MEMORY-012-06 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.SoftDeletedSeed_Returns404` |
| AC-FR-MCP-MEMORY-012-07 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.SoftDeletedNeighbor_Omitted` |
| AC-FR-MCP-MEMORY-012-08 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.Depth_ControlsHops` |
| AC-FR-MCP-MEMORY-012-09 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.DepthNonPositive_Returns400` |
| AC-FR-MCP-MEMORY-012-10 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.DepthAboveMax_StableBehavior` |
| AC-FR-MCP-MEMORY-012-11 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.SelfLoop_RejectedOrAbsent` |
| AC-FR-MCP-MEMORY-012-12 | TEST-MCP-MEMORY-012 | S3 | `MemoryEdgeTests.DuplicateExplicit_StableBehavior` |
| AC-FR-MCP-MEMORY-012-13 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.QuerySeed_UsesTopRecallOrEmpty` |
| AC-FR-MCP-MEMORY-012-14 | TEST-MCP-MEMORY-012 | S3 | `MemoryEdgeTests.WeightOutOfRange_Returns400` |
| AC-FR-MCP-MEMORY-012-15 | TEST-MCP-MEMORY-012 | S3 | `MemoryEdgeTests.InvalidEdgeType_Returns400` |
| AC-FR-MCP-MEMORY-012-16 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.HebbianToggle_DocumentedVisibility` |
| AC-FR-MCP-MEMORY-012-17 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.DoesNotMutateContent` |
| AC-FR-MCP-MEMORY-012-18 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.Directed_NoImpliedReverse` |
| AC-FR-MCP-MEMORY-012-19 | TEST-MCP-MEMORY-012 | S3 | `MemoryExploreTests.MaxNeighbors_TruncatesStable` |
| AC-FR-MCP-MEMORY-012-20 | TEST-MCP-MEMORY-012 | S3 | `MemoryEdgeTests.ForeignTarget_FailsClosed` |
| AC-FR-MCP-MEMORY-013-01 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.DryRun_NoMutations` |
| AC-FR-MCP-MEMORY-013-02 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.WriteMode_MergesNearDuplicates` |
| AC-FR-MCP-MEMORY-013-03 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.HardDelete_OnlyWhenConfigured` |
| AC-FR-MCP-MEMORY-013-04 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.LockContention_BusyError` |
| AC-FR-MCP-MEMORY-013-05 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.EmitsConsolidatedEvent` |
| AC-FR-MCP-MEMORY-013-06 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.DryRunDefaultTrue` |
| AC-FR-MCP-MEMORY-013-07 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.EmptyWorkspace_EmptyPlan` |
| AC-FR-MCP-MEMORY-013-08 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.SingleMemory_NoMerge` |
| AC-FR-MCP-MEMORY-013-09 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.ThreeWay_CollapsesToOne` |
| AC-FR-MCP-MEMORY-013-10 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.Survivor_NonEmptyContent` |
| AC-FR-MCP-MEMORY-013-11 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.MergedAway_HiddenFromListRecall` |
| AC-FR-MCP-MEMORY-013-12 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.GetMergedAway_StableBehavior` |
| AC-FR-MCP-MEMORY-013-13 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.NeverMergesAcrossWorkspaces` |
| AC-FR-MCP-MEMORY-013-14 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.SameScopeOnly_ByDefault` |
| AC-FR-MCP-MEMORY-013-15 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.Decay_FlagsWithoutHardDelete` |
| AC-FR-MCP-MEMORY-013-16 | TEST-MCP-MEMORY-016 | S4 | `MemoryAuthTests.ReadOnlyKey_CannotConsolidateWrite` |
| AC-FR-MCP-MEMORY-013-17 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.PlanItems_IncludeIdsAndScore` |
| AC-FR-MCP-MEMORY-013-18 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.ThresholdOutOfRange_Returns400` |
| AC-FR-MCP-MEMORY-013-19 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.CancelMidRun_ConsistentStore` |
| AC-FR-MCP-MEMORY-013-20 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateTests.Edges_RewiredToSurvivor` |
| AC-FR-MCP-MEMORY-014-01 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.Update_AppendsVersion` |
| AC-FR-MCP-MEMORY-014-02 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.List_OrderedAscending` |
| AC-FR-MCP-MEMORY-014-03 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.Revert_RestoresSnapshot` |
| AC-FR-MCP-MEMORY-014-04 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.Revert_AppendsNewVersion` |
| AC-FR-MCP-MEMORY-014-05 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.RevertMissing_Returns400Or404` |
| AC-FR-MCP-MEMORY-014-06 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.Create_InitialVersion` |
| AC-FR-MCP-MEMORY-014-07 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.NoOpUpdate_VersionPolicy` |
| AC-FR-MCP-MEMORY-014-08 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.SoftDeletedParent_VersionsHidden` |
| AC-FR-MCP-MEMORY-014-09 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.ForeignWorkspace_CannotListVersions` |
| AC-FR-MCP-MEMORY-014-10 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.RevertToCurrent_Stable` |
| AC-FR-MCP-MEMORY-014-11 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.Snapshot_IncludesContentAndTitle` |
| AC-FR-MCP-MEMORY-014-12 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.List_PaginationOrFullCap` |
| AC-FR-MCP-MEMORY-014-13 | TEST-MCP-MEMORY-016 | S1 | `MemoryAuthTests.ReadOnlyKey_CanListVersionsNotRevert` |
| AC-FR-MCP-MEMORY-014-14 | TEST-MCP-MEMORY-011 | S1 | `MemoryRecallTests.AfterRevert_IndexReflectsContent` |
| AC-FR-MCP-MEMORY-014-15 | TEST-MCP-MEMORY-014 | S1 | `MemoryVersionTests.VersionNumbers_Contiguous` |
| AC-FR-MCP-MEMORY-015-01 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.FromSessionLog_SetsProvenance` |
| AC-FR-MCP-MEMORY-015-02 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.FromContext_SetsProvenance` |
| AC-FR-MCP-MEMORY-015-03 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.SessionLogRows_Unchanged` |
| AC-FR-MCP-MEMORY-015-04 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.InvalidRef_Returns400` |
| AC-FR-MCP-MEMORY-015-05 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.CrossWorkspace_Forbidden` |
| AC-FR-MCP-MEMORY-015-06 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.NullBody_Returns400` |
| AC-FR-MCP-MEMORY-015-07 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.UnsupportedSourceKind_Returns400` |
| AC-FR-MCP-MEMORY-015-08 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.NoAutoPromote_OnTurnComplete` |
| AC-FR-MCP-MEMORY-015-09 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.Content_FromSourceRaw` |
| AC-FR-MCP-MEMORY-015-10 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.DoublePromote_Stable` |
| AC-FR-MCP-MEMORY-015-11 | TEST-MCP-MEMORY-016 | S4 | `MemoryAuthTests.ReadOnlyKey_CannotPromote` |
| AC-FR-MCP-MEMORY-015-12 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.Result_IncludesIdAndProvenance` |
| AC-FR-MCP-MEMORY-015-13 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.MissingTurn_Returns404` |
| AC-FR-MCP-MEMORY-015-14 | TEST-MCP-MEMORY-015 | S4 | `MemoryPromoteTests.ContextPromote_NoSiblingLeak` |
| AC-FR-MCP-MEMORY-016-01 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.Search_OnlyActiveWorkspace` |
| AC-FR-MCP-MEMORY-016-02 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.Edit_UsesSameCqrsPath` |
| AC-FR-MCP-MEMORY-016-03 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.Revert_WithinThreeActions` |
| AC-FR-MCP-MEMORY-016-04 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.ForeignMemory_FailClosed` |
| AC-FR-MCP-MEMORY-016-05 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.EmptyState_Message` |
| AC-FR-MCP-MEMORY-016-06 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.NoHits_EmptyNotError` |
| AC-FR-MCP-MEMORY-016-07 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.SoftDeleted_NotListed` |
| AC-FR-MCP-MEMORY-016-08 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.CreateEmpty_Blocked` |
| AC-FR-MCP-MEMORY-016-09 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.NoSecretsInDom` |
| AC-FR-MCP-MEMORY-016-10 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.Filters_Respected` |
| AC-FR-MCP-MEMORY-016-11 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.Escape_ClosesPanel` |
| AC-FR-MCP-MEMORY-016-12 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.CallsOnlyPublicRest` |
| AC-FR-MCP-MEMORY-017-01 | TEST-MCP-MEMORY-016 | S5 | `MemoryToolsDiscoveryTests.NewVerbs_AppearInToolsSearch` |
| AC-FR-MCP-MEMORY-017-02 | TEST-MCP-MEMORY-016 | S5 | `MemoryOnboardingDocsTests.Examples_ListNewVerbs` |
| AC-FR-MCP-MEMORY-017-03 | TEST-MCP-MEMORY-016 | S5 | `MemoryPluginSyncTests.GrokFirst_ThenOthersAfterValueGate` |
| AC-FR-MCP-MEMORY-017-04 | TEST-MCP-MEMORY-016 | S5 | `MemoryOnboardingDocsTests.GiveYourselfMemory_SampleExists` |
| AC-FR-MCP-MEMORY-017-05 | TEST-MCP-MEMORY-016 | S5 | `MemoryToolsDiscoveryTests.Descriptions_AgentOriented` |
| AC-FR-MCP-MEMORY-017-06 | TEST-MCP-MEMORY-016 | S5 | `MemoryToolsDiscoveryTests.CompatVerbs_StillListed` |
| AC-FR-MCP-MEMORY-017-07 | TEST-MCP-MEMORY-016 | S5 | `MemoryOnboardingDocsTests.MarkerTemplate_DocumentsNewAndOld` |
| AC-FR-MCP-MEMORY-017-08 | TEST-MCP-MEMORY-016 | S5 | `MemoryPluginSyncTests.MissingVerb_FailsValidation` |
| AC-FR-MCP-MEMORY-017-09 | TEST-MCP-MEMORY-016 | S5 | `MemoryToolsDiscoveryTests.StdioAndHttp_SameVerbSet` |
| AC-FR-MCP-MEMORY-017-10 | TEST-MCP-MEMORY-016 | S5 | `MemoryReplTests.TypedMethods_Present` |
| AC-FR-MCP-MEMORY-017-11 | TEST-MCP-MEMORY-016 | S5 | `MemoryReplTests.InvalidMethod_NoCrash` |
| AC-FR-MCP-MEMORY-017-12 | TEST-MCP-MEMORY-016 | S5 | `MemoryPluginSyncTests.ChecklistReceipt_GrokBeforeOthers` |
| AC-TR-MCP-MEMORY-MODEL-002-01 | TEST-MCP-MEMORY-010 | S1 | `MemoryMigrationTests.Applies_OnAllProviders` |
| AC-TR-MCP-MEMORY-MODEL-002-02 | TEST-MCP-MEMORY-010 | S1 | `MemoryMigrationTests.Backfill_ContentFromLegacyText` |
| AC-TR-MCP-MEMORY-MODEL-002-03 | TEST-MCP-MEMORY-010 | S1 | `MemoryEdgeTests.UniqueFromToType_Enforced` |
| AC-TR-MCP-MEMORY-MODEL-002-04 | TEST-MCP-MEMORY-010 | S1 | `MemoryMigrationTests.VersionFk_NoOrphans` |
| AC-TR-MCP-MEMORY-MODEL-002-05 | TEST-MCP-MEMORY-010 | S1 | `MemoryMigrationTests.SoftDeleteFilter_Default` |
| AC-TR-MCP-MEMORY-MODEL-002-06 | TEST-MCP-MEMORY-010 | S1 | `MemoryMigrationTests.SchemaRecreate_Works` |
| AC-TR-MCP-MEMORY-MODEL-002-07 | TEST-MCP-MEMORY-010 | S1 | `MemoryMigrationTests.Indexes_Present` |
| AC-TR-MCP-MEMORY-MODEL-002-08 | TEST-MCP-MEMORY-010 | S1 | `MemoryMigrationTests.ColumnLengths_MatchValidation` |
| AC-TR-MCP-MEMORY-MODEL-002-09 | TEST-MCP-MEMORY-010 | S1 | `MemoryMigrationTests.TagsRoundTrip_AllProviders` |
| AC-TR-MCP-MEMORY-MODEL-002-10 | TEST-MCP-MEMORY-010 | S1 | `MemoryMigrationTests.VersionInsert_ConcurrentSafe` |
| AC-TR-MCP-MEMORY-SEARCH-002-01 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.ReadyOrFailedStatus` |
| AC-TR-MCP-MEMORY-SEARCH-002-02 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.Reconcile_RepairsStale` |
| AC-TR-MCP-MEMORY-SEARCH-002-03 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.FusionWeights_AffectOrdering` |
| AC-TR-MCP-MEMORY-SEARCH-002-04 | TEST-MCP-MEMORY-011 | S2 | `MemoryRecallTests.Rerank_DefaultOff` |
| AC-TR-MCP-MEMORY-SEARCH-002-05 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.FailedEmbed_StillListable` |
| AC-TR-MCP-MEMORY-SEARCH-002-06 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.Reindex_AfterContentChange` |
| AC-TR-MCP-MEMORY-SEARCH-002-07 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.Ann_NoCrossWorkspaceLeak` |
| AC-TR-MCP-MEMORY-SEARCH-002-08 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.Cancel_LeavesSafeStatus` |
| AC-TR-MCP-MEMORY-SEARCH-002-09 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.OnnxLocal_NoCloudRequired` |
| AC-TR-MCP-MEMORY-SEARCH-002-10 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.CloudOptIn_FailsClosedWhenOff` |
| AC-TR-MCP-MEMORY-SEARCH-002-11 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.EmptyContent_NeverReady` |
| AC-TR-MCP-MEMORY-SEARCH-002-12 | TEST-MCP-MEMORY-011 | S2 | `MemoryIndexerTests.BatchIndex_AllTerminal` |
| AC-TR-MCP-MEMORY-JOBS-002-01 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateJobTests.PerWorkspaceLock` |
| AC-TR-MCP-MEMORY-JOBS-002-02 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateJobTests.DryRunDefaultTrue` |
| AC-TR-MCP-MEMORY-JOBS-002-03 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateJobTests.EventIncludesWorkspaceAndRunId` |
| AC-TR-MCP-MEMORY-JOBS-002-04 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateJobTests.Disabled_NoEffectOnCrud` |
| AC-TR-MCP-MEMORY-JOBS-002-05 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateJobTests.OverlappingTicks_NoDoubleApply` |
| AC-TR-MCP-MEMORY-JOBS-002-06 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateJobTests.Logs_NoSecrets` |
| AC-TR-MCP-MEMORY-JOBS-002-07 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateJobTests.LockExpiry_Recovers` |
| AC-TR-MCP-MEMORY-JOBS-002-08 | TEST-MCP-MEMORY-013 | S4 | `MemoryConsolidateJobTests.Stats_ReflectLastRun` |
| AC-TR-MCP-MEMORY-API-002-01 | TEST-MCP-MEMORY-016 | S5 | `MemoryCqrsSurfaceTests.Adapters_DispatchHandlersOnly` |
| AC-TR-MCP-MEMORY-API-002-02 | TEST-MCP-MEMORY-016 | S5 | `MemoryErrorEnvelopeTests.UsesStandardEnvelope` |
| AC-TR-MCP-MEMORY-API-002-03 | TEST-MCP-MEMORY-016 | S5 | `MemoryCompatCrudTests.ToolsStillRegistered` |
| AC-TR-MCP-MEMORY-API-002-04 | TEST-MCP-MEMORY-016 | S5 | `MemoryOpenApiTests.NewRoutes_Listed` |
| AC-TR-MCP-MEMORY-API-002-05 | TEST-MCP-MEMORY-016 | S5 | `MemoryClientTests.Methods_MirrorRest` |
| AC-TR-MCP-MEMORY-API-002-06 | TEST-MCP-MEMORY-016 | S5 | `MemoryMcpSchemaTests.UnknownFields_Policy` |
| AC-TR-MCP-MEMORY-API-002-07 | TEST-MCP-MEMORY-016 | S5 | `MemoryContextSourceTests.MemoriesSource_OptInAndScoped` |
| AC-TR-MCP-MEMORY-API-002-08 | TEST-MCP-MEMORY-016 | S5 | `MemorySseTests.NoCrossWorkspaceEvents` |
| AC-TR-MCP-MEMORY-API-002-09 | TEST-MCP-MEMORY-016 | S5 | `MemoryTxnTests.NewMutations_HonorGating` |
| AC-TR-MCP-MEMORY-API-002-10 | TEST-MCP-MEMORY-016 | S5 | `MemoryApiIdempotencyTests.DocumentedBehavior` |
| AC-TR-MCP-MEMORY-UI-002-01 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiHostingTests.ServedAtMemoryPath` |
| AC-TR-MCP-MEMORY-UI-002-02 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiDeployDocsTests.NamesUpdateService` |
| AC-TR-MCP-MEMORY-UI-002-03 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiTests.CallsOnlyPublicRest` |
| AC-TR-MCP-MEMORY-UI-002-04 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiHostingTests.Assets_InPublishOutput` |
| AC-TR-MCP-MEMORY-UI-002-05 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiHostingTests.DeepLink_DetailOrFailClosed` |
| AC-TR-MCP-MEMORY-UI-002-06 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiHostingTests.Auth_SameAsMcpserver` |
| AC-TR-MCP-MEMORY-UI-002-07 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiHostingTests.Csp_MatchesSiblingUis` |
| AC-TR-MCP-MEMORY-UI-002-08 | TEST-MCP-MEMORY-017 | S6 | `MemoryUiHostingTests.MissingAsset_NotFalse200` |
| AC-FR-MCP-MEMORY-018-01 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchPackTests.PackFile_ExistsAndVersioned` |
| AC-FR-MCP-MEMORY-018-02 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchPackTests.CoversEightPromptClasses` |
| AC-FR-MCP-MEMORY-018-03 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchPackTests.EntrySchema_Complete` |
| AC-FR-MCP-MEMORY-018-04 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchPackTests.PairedConditions` |
| AC-FR-MCP-MEMORY-018-05 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchHarnessTests.WithoutMemory_NoEffectiveInjectionOrTools` |
| AC-FR-MCP-MEMORY-018-06 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchHarnessTests.WithMemory_SeededAndToolsAvailable` |
| AC-FR-MCP-MEMORY-018-07 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchHarnessTests.GrokFirst_OthersOptInUntilValueGate` |
| AC-FR-MCP-MEMORY-018-08 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchHarnessTests.RecordsPerCellMetrics_IncludingTokens` |
| AC-FR-MCP-MEMORY-018-09 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchHarnessTests.WritesResultArtifacts_WithTokenSummary` |
| AC-FR-MCP-MEMORY-018-10 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchScoringTests.DeterministicOrDocumentedRubric` |
| AC-FR-MCP-MEMORY-018-11 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.ReportsMeanMedianTokens_ByPluginAndCondition` |
| AC-FR-MCP-MEMORY-018-12 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchSafetyTests.NegativePrompts_NoHallucinatedMemory` |
| AC-FR-MCP-MEMORY-018-13 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchConflictTests.PrefersNewerSeeded` |
| AC-FR-MCP-MEMORY-018-14 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchInjectionTests.InjectionPresentOnlyWithMemory` |
| AC-FR-MCP-MEMORY-018-15 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchToolUseTests.RecallUsedWhenNoInjection` |
| AC-FR-MCP-MEMORY-018-16 | TEST-MCP-MEMORY-018 | S7a | `MemoryBenchPluginTests.Grok_CompletesFullPack_Required` |
| AC-FR-MCP-MEMORY-018-17 | TEST-MCP-MEMORY-018 | S7b | `MemoryBenchPluginTests.ClaudeCode_CompletesFullPack_AfterGrokValueGate` |
| AC-FR-MCP-MEMORY-018-18 | TEST-MCP-MEMORY-018 | S7b | `MemoryBenchPluginTests.ClaudeCowork_CompletesFullPack_AfterGrokValueGate` |
| AC-FR-MCP-MEMORY-018-19 | TEST-MCP-MEMORY-018 | S7b | `MemoryBenchPluginTests.Cline_CompletesFullPack_AfterGrokValueGate` |
| AC-FR-MCP-MEMORY-018-20 | TEST-MCP-MEMORY-018 | S7b | `MemoryBenchPluginTests.ClineV2_CompletesFullPack_AfterGrokValueGate` |
| AC-FR-MCP-MEMORY-018-21 | TEST-MCP-MEMORY-018 | S7b | `MemoryBenchPluginTests.Codex_CompletesFullPack_AfterGrokValueGate` |
| AC-FR-MCP-MEMORY-018-22 | TEST-MCP-MEMORY-018 | S7b | `MemoryBenchPluginTests.Copilot_CompletesFullPack_AfterGrokValueGate` |
| AC-FR-MCP-MEMORY-018-23 | TEST-MCP-MEMORY-018 | S7b | `MemoryBenchPluginTests.Opencode_CompletesFullPack_AfterGrokValueGate` |
| AC-FR-MCP-MEMORY-018-24 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchReportTests.SummaryComparesConditions` |
| AC-FR-MCP-MEMORY-018-25 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchHarnessTests.StubMode_NoCloudRequired` |
| AC-FR-MCP-MEMORY-018-26 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchHarnessTests.LiveMode_Tagged` |
| AC-FR-MCP-MEMORY-018-27 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchHarnessTests.Seeds_WorkspaceScopedCleanup` |
| AC-FR-MCP-MEMORY-018-28 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchIsolationTests.NoPrimaryWorkspaceBleed` |
| AC-FR-MCP-MEMORY-018-29 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchCiTests.GateMode_Configurable` |
| AC-FR-MCP-MEMORY-018-30 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchPackTests.NoRealSecretsInFixtures` |
| AC-FR-MCP-MEMORY-018-31 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchHarnessTests.Rerun_DeterministicStub` |
| AC-FR-MCP-MEMORY-018-32 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchDocsTests.Readme_RunInstructions` |
| AC-FR-MCP-MEMORY-018-33 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchDocsTests.UserGuide_LinksPack` |
| AC-TR-MCP-MEMORY-BENCH-002-01 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.NoPythonProductPath` |
| AC-TR-MCP-MEMORY-BENCH-002-02 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.PluginAdapterInterface` |
| AC-TR-MCP-MEMORY-BENCH-002-03 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.ResultSchema_Validated` |
| AC-TR-MCP-MEMORY-BENCH-002-04 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.PackSchema_Validated` |
| AC-TR-MCP-MEMORY-BENCH-002-05 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.IndependentSuppressionFlags` |
| AC-TR-MCP-MEMORY-BENCH-002-06 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.Latency_OptionalBudget` |
| AC-TR-MCP-MEMORY-BENCH-002-07 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.CiTarget_BenchMemory` |
| AC-TR-MCP-MEMORY-BENCH-002-08 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.Results_GitignorePolicy` |
| AC-FR-MCP-MEMORY-018-34 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.Source_HostReportedOrDocumentedEstimator` |
| AC-FR-MCP-MEMORY-018-35 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.Defines_InOutTotal` |
| AC-FR-MCP-MEMORY-018-36 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.Injection_CountedOnlyWithMemory` |
| AC-FR-MCP-MEMORY-018-37 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.ToolPayloads_CountedConsistently` |
| AC-FR-MCP-MEMORY-018-38 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchReportTests.SummaryTable_TokenColumns` |
| AC-FR-MCP-MEMORY-018-39 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.PerPrompt_TokenDelta` |
| AC-FR-MCP-MEMORY-018-40 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchReportTests.Headline_MacroMeanTokens` |
| AC-FR-MCP-MEMORY-018-41 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.CorrectnessGate_TokensStillRecorded` |
| AC-FR-MCP-MEMORY-018-42 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.EfficiencyRatio_SecondaryOnly` |
| AC-FR-MCP-MEMORY-018-43 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.StubMode_RequiresTokenFields` |
| AC-FR-MCP-MEMORY-018-44 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTokenTests.SafetyPrompts_TokensAndPassFail` |
| AC-FR-MCP-MEMORY-018-45 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchDocsTests.Readme_TokensArePrimaryMetric` |
| AC-TR-MCP-MEMORY-BENCH-002-09 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.ResultSchema_RequiresTokenFields` |
| AC-TR-MCP-MEMORY-BENCH-002-10 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.MissingTokens_HardFail` |
| AC-TR-MCP-MEMORY-BENCH-002-11 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.Estimator_VersionedDeterministic` |
| AC-TR-MCP-MEMORY-BENCH-002-12 | TEST-MCP-MEMORY-018 | S7 | `MemoryBenchTechTests.CiSummary_TokensFirst` |
| AC-FR-MCP-MEMORY-018-46 | TEST-MCP-MEMORY-018 | S7a | `MemoryIntegrationTests.UseGrokLane_AsPilot` |
| AC-FR-MCP-MEMORY-018-47 | TEST-MCP-MEMORY-018 | S7a | `MemoryBenchValueGateTests.GrokH7a_RequiresAgreeBeforeOtherPlugins` |
| AC-FR-MCP-MEMORY-018-48 | TEST-MCP-MEMORY-018 | S7a | `MemoryBenchCiTests.NonGrok_OptionalUntilH7a` |
| AC-FR-MCP-MEMORY-018-49 | TEST-MCP-MEMORY-018 | S7a | `MemoryIntegrationTests.Grok_RememberRecall_EndToEnd` |
| AC-FR-MCP-MEMORY-018-50 | TEST-MCP-MEMORY-018 | S7a | `MemoryBenchValueGateTests.HDone_DefaultRequiresH7a_H7bFollowOn` |
| AC-FR-MCP-MEMORY-017-13 | TEST-MCP-MEMORY-016 | S5 | `MemoryPluginSyncTests.ImplementGrokPlugin_BeforeOthers` |
| AC-FR-MCP-MEMORY-017-14 | TEST-MCP-MEMORY-016 | S5 | `MemoryPluginSyncTests.NonGrok_BlockedUntilH7a` |
| AC-TR-MCP-MEMORY-BENCH-002-13 | TEST-MCP-MEMORY-018 | S7a | `MemoryBenchTechTests.DefaultPlugin_IsGrok` |
| AC-TR-MCP-MEMORY-BENCH-002-14 | TEST-MCP-MEMORY-018 | S7a | `MemoryBenchDocsTests.Readme_GrokIsPilot` |

**Count check:** matrix rows MUST equal 283. H0 fails if any AC lacks TEST or planned method.




---

## Public interfaces (before implementation)

REST additive: remember, recall, explore, consolidate, versions, revert, promote, stats.  
MCP additive verbs; compat CRUD retained.  
Client/REPL/plugins thin over CQRS.

---

## Iterative slices (Byrd micro-cycle each)

Each slice owns the AC rows tagged with its slice id. **H{n}-green blocked until 100% of that slice's AC are Green.**

**P0 Planning:** Author all FR/TR/TEST + full AC text into MCP store; create UC-MEMORY-001..009; freeze matrix (283/283); expand `docs/context/memory.md`; ValidateTraceability; **Hostile H0** (after H-plan).

**S1 Model/versions/edges:** AC slice S1 (FR-010, FR-014, TR-MODEL).  
**S2 Index/recall:** S2 (FR-011, TR-SEARCH).  
**S3 Explore/Hebbian:** S3 (FR-012).  
**S4 Consolidate/promote:** S4 (FR-013, FR-015, TR-JOBS).  
**S5 Surfaces + Grok plugin (pilot):** S5 (FR-017, TR-API) — wire MCP/client/REPL, then **implement grok plugin only**; other plugins deferred to post-H7a (S5b/H7b).  
**S6 UI + ship:** S6 (FR-016, TR-UI); integration after units.

**S7a Grok benchmarks + integration (value gate):** FR-018/TR-BENCH on **Grok only**; integration tests use Grok lane; token primary metric; **H7a-red/H7a-green**. Validate performance and value before any other agent plugin work.

**S7b Other plugins (after H7a AGREE only):** remaining seven plugins × pack; **H7b-red/H7b-green**. Follow-on; not default H-done blocker.

Then **H-done** (defaults after H7a); Nuke UpdateService only on operator ask.

---

---

## Hostile validation checkpoints

Use the workspace `hostile-validator` skill. **Work class:** Class 1 (project implementation) for S1–S6 and H-done; Class 1 planning/docs for **H-plan** and **H0**. Surface C applies. Parent must run **add-profile** in the hostile brief. **Default posture: FAIL** until independently re-verified.

**Receipts:** `docs/receipts/hostile-validator-<utc>.md` plus matching `.json`. Cite the receipt path in the TODO `doneSummary` / phase notes. **Never** rewrite a prior DISAGREE receipt; open a new receipt on resume.

**Hard rules:**
- OverallVerdict **AGREE** required to leave a checkpoint. **DISAGREE** blocks the next phase and blocks `done: true`.
- Do not start schema/product code before **H0 AGREE**.
- Do not start slice S{n+1} before **H{n}-green AGREE**.
- Do not mark MCP-MEMORY-002 `Done: true` before **H-done AGREE**.
- Skipped tests are failures for hostile purposes. Gate command: `./build.ps1 Test` with Failed 0 and Skipped 0 for in-scope + prior units, then `ValidateConfig` and `ValidateTraceability` where the checkpoint says so.
- Hostile attacks the **claim**, not the author's confidence. Evidence must be re-gathered by the validator (git, tests, MCP store reads, file hashes), not pasted from the implementer without re-check.

### H-plan — Plan validation (before / as opening of Planning authorship)

**When:** After this plan doc exists on `memory` branch; before or concurrent with writing FR/TR/TEST into the MCP store. **Blocks:** treating the plan as frozen for H0.

**Claim under attack:** "docs/plans/mcp-memory-002.md is a complete Byrd v4 plan: dense AC, use cases covering all FRs, 100% AC↔TEST matrix, locked decisions, slices, and hostile gates."

**Attack surface (validator must re-verify):**
1. Byrd v4 binding present (Planning before code; Red→mocks-first→Green→Refactor; Failed 0 Skipped 0; Validate*; hostile AGREE; no TODO.yaml hand-edits).
2. FR-010..018 and TR-MODEL/SEARCH/JOBS/API/UI/BENCH-002 each have **numbered AC** in the plan (not summary-only).
3. AC catalog count == matrix unique rows == `283` (or current catalog if amended upward). Prefer more AC; never fewer without plan amendment.
4. Every matrix row names TEST id + planned unit test method + slice.
5. UC-MEMORY-001..009 cover FR-010..018; no FR orphaned; Grok-first + tokens primary present.
6. Locked decisions include: extend-not-fork, CQRS-only, no Product auto-share, no silent sessionlog write-back, Hebbian default off, no Python product path, FR-001..007 remain green.
7. Out of scope excludes federation rewrite (FR-008 separate), GraphRAG replacement, silent auto-write.
8. Slices S1–S6, S7a (Grok), optional S7b map to AC slice tags; red/green hostile pairs listed.
9. Plan does **not** authorize skipping H0 or collapsing red/green into one gate.
10. `docs/plans/mcp-memory-002-ac-catalog.json` matches plan AC ids (hash/count check).

**Exit:** H-plan OverallVerdict AGREE receipt. On DISAGREE, amend plan + catalog, new receipt — do not proceed to store authorship claiming completeness.

### H0 — Planning artifacts in MCP store (after Planning, before any S1 schema/code)

**When:** FR/TR/TEST + AC text + mappings + UC-MEMORY-001..008 exist in the MCP requirements/use-case store; `ValidateTraceability` run; no Memory Version/Edge/indexer product code landed for this TODO.

**Claim under attack:** "Planning is complete and traceable; implementation may begin."

**Attack surface:**
1. Every FR-MCP-MEMORY-010..018 exists in MCP store with structured acceptance criteria matching plan AC-FR-* ids (spot-check ≥5 AC per FR including edge ACs, not only happy path).
2. Every TR-MCP-MEMORY-*-002 exists with AC-TR-* text.
3. TEST-MCP-MEMORY-010..018 exist; FR→TR→TEST mappings complete for in-scope ids.
4. UC-MEMORY-001..009 exist with Realizes links covering FR-010..018 (usecase_list / coverage tool).
5. Matrix still 283/283 (or amended count) with no AC lacking TEST/method.
6. `./build.ps1 ValidateTraceability` green (or documented equivalent export path).
7. Git: no S1 product schema/migrations for MemoryVersion/MemoryEdge/indexer beyond plan/docs for this TODO (diff review).
8. Existing FR-MCP-MEMORY-001..007 remain present; plan does not mark them obsolete.
9. CQRS-only and Global/Workspace isolation language appear in TR/FR text, not only in the plan prose.
10. TODO MCP-MEMORY-002 still `Done: false`; implementationTasks show Planning items completable but S1 code tasks not falsely done.

**Exit:** H0 AGREE. **Blocks all S1 worktrees/schema.**

### Per-slice red/green pairs (S1–S6)

For each slice Sn, run **two** hostile checkpoints. Never combine into one.

#### Shared red-gate attack pattern (H{n}-red)

**When:** Acceptance unit tests for every Sn-tagged AC exist and are red for the right reason (missing behavior / failing asserts), not red from compile errors or missing project references that hide AC intent.

**Attack surface:**
1. Every Sn-tagged AC id from the matrix has a named test method (grep/reflection or test list).
2. Tests fail on current `memory` branch HEAD for behavioral reasons tied to the AC statement.
3. Mocks/stubs only at intended CQRS/port seams — not vacuously returning success that would fake Green later.
4. No production implementation for Sn feature yet (or incomplete enough that AC tests still fail).
5. XML docs / TEST-FR-TR citations present on new test types where Byrd requires.
6. Failed count > 0 for new tests; Skipped == 0 for those new tests.

**Exit:** H{n}-red AGREE before Green implementation for that slice.

#### Shared green-gate attack pattern (H{n}-green)

**When:** Sn implementation complete; mocks-first proven earlier; real code green; full prior+current unit suite Failed 0 Skipped 0; ValidateConfig + ValidateTraceability as applicable.

**Attack surface:**
1. Every Sn-tagged AC now has a passing named test (re-run, not trust prior paste).
2. `./build.ps1 Test` Failed 0 Skipped 0 for the gate set (current + prior slices).
3. No public parallel domain service facade for new verbs — adapters dispatch CQRS only (architecture grep).
4. Slice-specific invariants below.
5. No weakening of AC text or deletion of tests to achieve green.
6. Sessionlog/TODO updates via MCP tools, not hand-edited TODO.yaml.
7. Receipt cites commands, commit/HEAD, and AC coverage counts for Sn.

**Exit:** H{n}-green AGREE before S{n+1} (or Validation/H-done as listed).

### Slice-specific hostile focus

#### H1-red / H1-green — S1 Model, versions, edges (FR-010, FR-014, TR-MODEL)

- **Red focus:** multi-layer field persistence; empty/unicode/confidence bounds; soft-delete omit; scope ordering; version append/revert; migration/backfill tests exist and fail correctly; isolation tests named.
- **Green focus:** migrations on SQLite/PostgreSQL/SQL Server; Content backfill; Unique(From,To,EdgeType); compat Effective + REQUIRED MEMORIES raw contract unchanged; read-only key cannot mutate; foreign workspace hidden; Version revert appends history; `283` in-scope matrix; S1-tagged rows all Green.

#### H2-red / H2-green — S2 Index and recall (FR-011, TR-SEARCH)

- **Red focus:** empty/null query 400; exact + paraphrase fixtures; minScore/topN bounds; filter AND semantics; soft-delete/foreign exclusion; EmbeddingStatus transitions.
- **Green focus:** indexer ready/failed; reconcile repairs stale; fusion weights affect order; rerank default off; ANN/FTS no cross-workspace leakage; ONNX local without cloud; after-update index refresh; S2 AC 100% Green.

#### H3-red / H3-green — S3 Explore + Hebbian (FR-012)

- **Red focus:** explicit edge return; Hebbian off default; depth bounds; self-loop reject; directed no implied reverse; foreign target fail-closed.
- **Green focus:** Hebbian default config false; co-retrieved edges invisible while off; explore does not mutate Content; maxNeighbors stable order; S3 AC 100% Green.

#### H4-red / H4-green — S4 Consolidate + promote (FR-013, FR-015, TR-JOBS)

- **Red focus:** dry-run default/no mutations; write merge survivor+lineage; lock busy; no cross-workspace merge; promote provenance; sessionlog byte-identical; no auto-promote.
- **Green focus:** per-workspace lock + TTL recovery; SSE event payload workspace+run id; AllowHardDelete default false; promote SourceKind/SourceRef; read-only cannot write-consolidate/promote; S4 AC 100% Green.

#### H5-red / H5-green — S5 Surfaces + plugins (FR-017, TR-API)

- **Red focus:** tools search lists new verbs; OpenAPI routes; MemoryClient mirror; compat verbs still registered; plugin checklist tests fail until synced.
- **Green focus:** CQRS-only adapters; error envelope unchanged; STDIO≡HTTP verb set; **Grok plugin checklist pass** (other seven deferred post-H7a); marker template documents new+old verbs; optional `memories` context source opt-in + scoped; SSE no cross-workspace; S5 AC 100% Green.

#### Validation gate (after units, before or overlapping S6 as needed)

Not a substitute for H-red/H-green. After units green for implemented slices: add/run `*.IntegrationTests` for memory paths. If tests expose paradoxical requirements, **refine requirements / add AC** — never weaken tests. Hostile may be asked to spot-check integration evidence inside H6-green / H-done.

#### H6-red / H6-green — S6 UI + packaging (FR-016, TR-UI)

- **Red focus:** UI-only-active-workspace; CQRS path parity; revert ≤3 actions; foreign fail-closed; empty state; no secrets in DOM; public REST only.
- **Green focus:** `/memory/` static assets in publish output; Nuke UpdateService named in deploy docs; CSP matches sibling UIs; deep link fail-closed; auth same as /mcpserver; S6 AC 100% Green.


#### H7a-red / H7a-green — S7a Grok bench + integration (value gate)

- **When (red):** Pack/harness/token-schema tests and **Grok** integration tests exist and fail for the right reason; Grok plugin adapter tests named.
- **When (green):** Grok completes full pack × both conditions with tokens_*; Grok integration tests green (remember→recall/injection); token summary published; correctness/safety gates green for Grok cells.
- **Red focus:** default `-Plugin grok`; token fields required; Grok CompletesFullPack; Grok E2E integration; no requirement yet that other plugins pass.
- **Green focus:** README states tokens primary + **Grok is pilot**; CI BenchMemory defaults to Grok; token headline metrics; NEG/SAFE clean on Grok; isolation; **value claim**: hostile/operator AGREE that Grok token + correctness results justify expanding to other agents.
- **Blocks:** starting non-Grok plugin implementation (S5b/S7b); default **H-done** (H7a required).

#### H7b-red / H7b-green — S7b Remaining seven plugins (only after H7a AGREE)

- **When:** After H7a AGREE only.
- **Focus:** claude-code, claude-cowork, cline, cline-v2, codex, copilot, opencode each complete pack × both conditions with tokens_*; checklists eight/eight.
- **Blocks:** nothing in the default DoD; blocks only if operator expands MCP-MEMORY-002 scope to require full eight before `Done: true`.


### H-done — Close claim (after H7a-green by default, full suite, Validate*)

**When:** S1–S6 H-green AGREE + **H7a-green AGREE**; full `./build.ps1 Test` Failed 0 Skipped 0; ValidateConfig + ValidateTraceability green; Grok integration + token bench evidence attached; TODO still `Done: false`. H7b not required by default.

**Claim under attack:** "MCP-MEMORY-002 is done: competitive memory shipped under Byrd v4 with 283/283 AC Green (default-path scope), use cases FR-010..018, Grok token bench + integration (H7a) proving value, CQRS-only, no cross-workspace leakage, compat CRUD + REQUIRED MEMORIES preserved, Grok plugin synced, other plugins deferred, UI live at /memory/."

**Attack surface:**
1. Recount AC: catalog/matrix/store agree; every AC has a passing test in the final gate (sample + aggregate counts).
2. All H-plan, H0, H1-red..H6-green receipts exist with OverallVerdict AGREE (list paths).
3. No DISAGREE left unresolved without a superseding AGREE receipt.
4. CQRS-only: no new public IMemory* facade for additive verbs.
5. Isolation: product membership does not share memories; foreign workspace tests green.
6. Compat: memory_add|list|update|remove + REQUIRED MEMORIES contract tests green.
7. Hebbian default off in shipped config.
8. Grok plugin checklist pass; other plugins deferred unless H7b in scope.
9. Deploy not silently performed; Nuke UpdateService only if operator asked (or explicitly out of done claim).
10. **H7a-green** AGREE exists; Grok stub/live bench summary shows both conditions with **token totals/means** as headline metrics; NEG/SAFE clean on Grok.
11. `todo_get MCP-MEMORY-002` is still Done:false until this AGREE; validator does not flip done unless the brief explicitly assigns that after AGREE.

**Exit:** H-done AGREE receipt path recorded; only then `Done: true` with `doneSummary` citing the receipt.

---

## Definition of Done (DoD)

- All 283 AC have named automated tests that passed in the final gate (Failed 0, Skipped 0)
- UC-MEMORY-001..009 Realizes FR-010..018 in the MCP store
- Grok stub/live bench + Grok integration evidence attached via **H7a** receipt, including tokens_in/out/total summary; other plugins deferred until H7a AGREE
- ValidateConfig + ValidateTraceability green
- Hostile OverallVerdict AGREE on **H-done** (and all prior gates on the default path through **H7a**; H7b not required unless scoped in)
- `todo_get MCP-MEMORY-002` remains `Done: false` until that H-done AGREE exists
- Receipt: `docs/receipts/hostile-validator-<utc>.md` (+ `.json`) cited in doneSummary

---

## Hostile gate sequence (checklist)

| Order | Gate | Blocks |
|---|---|---|
| 1 | **H-plan** | Claiming plan complete / freezing AC count without AGREE |
| 2 | **H0** | Any S1 schema/product code |
| 3 | **H1-red** | S1 Green implementation |
| 4 | **H1-green** | S2 start |
| 5 | **H2-red** | S2 Green implementation |
| 6 | **H2-green** | S3 start |
| 7 | **H3-red** | S3 Green implementation |
| 8 | **H3-green** | S4 start |
| 9 | **H4-red** | S4 Green implementation |
| 10 | **H4-green** | S5 start |
| 11 | **H5-red** | S5 Green implementation |
| 12 | **H5-green** | S6 start (Validation integration may run after units) |
| 13 | **H6-red** | S6 Green implementation |
| 14 | **H6-green** | H7a start |
| 15 | **H7a-red** | Grok harness/integration Green |
| 16 | **H7a-green** | H-done attempt (default) or H7b if scope expanded |
| 17 | **H7b-red** | Other-plugin Green (after H7a only) |
| 18 | **H7b-green** | Optional full-eight close |
| 19 | **H-done** | `Done: true` (default after H7a) |

**Total named hostile gates: 19** (H-plan + H0 + 6×(red+green) + H7a-red/green + H7b-red/green + H-done). Default path uses 17 gates (skip H7b).

## Success metrics

- Hostile gates AGREE through H7a (Grok value gate) on the default path; H7b after value proven.
- 283/283 AC have Red→Green automated proof (no skips).
- Grok-first: with/without **token** results (primary) + Grok integration tests prove performance/value (H7a) before other agents; pass/fail safety gates; NEG/SAFE clean.
- Use cases cover FR-010..018 with no orphaned FR.
- Receipts under `docs/receipts/`.
- Remember session A / paraphrase-recall session B without reading sessionlog.
- Zero cross-workspace leakage tests green.
- Compat Effective list + REQUIRED MEMORIES contract unchanged for legacy rows.

## References

- `docs/Development-Process-draft-v4.md`
- `skills/byrd-tdd-process/SKILL.md`
- `docs/byrd-todo-execution-spec.md`
- `docs/context/memory.md`
- Existing FR-MCP-MEMORY-001..007 (must remain green)
- `docs/benchmarks/memory-prompt-pack-v1.yaml` + `docs/benchmarks/README.md`
- `docs/plans/mcp-memory-002-ac-catalog.json`
