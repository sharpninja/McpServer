# Weighted Context Projection — Proposed Implementation

**Document:** proposed-implementation-weighted-context-projection-v0.1.md
**Version:** v0.1.0
**Status:** Proposed. Requires operator approval before any build work begins.
**Companion to:** `whitepaper-weighted-context-projection-v0.1.md` (v0.1.5)
**Code baseline:** `main` @ `e7c43a125e1bb4837b5b9b9d4021ae2b592f931f`
**Date:** 2026-09-20

> **Cross-reference convention.** `WP §N` refers to a section of the whitepaper. A bare
> `§N` refers to a section of *this* document. The two numbering schemes overlap, so the
> prefix is load-bearing.

## 1. Purpose and standing

The whitepaper describes a design. This document describes **how that design would be
built against the code that exists today, and what it would cost** — deployment options,
corrections the code forces on the design, recommendations, phases, and exit gates. The
two were previously one file; they are separated because a design argument and a build
proposal are read by different people for different reasons, and because implementation
detail kept accumulating inside a document whose job was to argue a design.

Nothing here is approved. Every claim about the codebase names the file it came from so it
can be re-checked or falsified. Where the whitepaper and the code disagree, the code wins
as a statement of fact about the present, and the disagreement is recorded as a correction
to be made — not as a defect in the design. No build was run and no tests were executed
for this document.

This document does not amend the whitepaper, and it does not decide anything. §9 still
owns the Phase 1 decision and the Option B/C decision; §10 lists what remains genuinely
undecided.

## 2. Headline

Three things changed shape once the design was read against the code.

**The design is cheaper than it looks in two places.** The Option C agent invoker is
substantially already built, and the Agent Framework dependency that Option B needs is
already referenced and already in use. Neither option is greenfield.

**The recoverability invariant is already backed, and more strongly than the design
claims.** `McpDbContext` blocks physical deletes outright and mirrors every mutation into
an append-only audit ledger with before-and-after snapshots, so WP §6 can cite enforcement
rather than assume it. An earlier draft of this document asserted the opposite; §3.2 carries
the retraction and why the mechanism is easy to miss.

**Two schema assumptions still need correction before code is written** — §3.3 and §3.4 —
plus a narrower scoping note in §3.5. Neither invalidates the design; both would waste
Phase 1 effort if found during implementation instead of before it.

**Two of this document's own findings have been retracted.** §3.1 (memory verbs) and §3.2
(append-only) were both wrong, both in the same direction — asserting that something did
not exist on the strength of a narrow read. They are kept in place as retractions rather
than deleted, so the error and its cause stay auditable. Weigh the surviving findings
accordingly: the ones that claim *presence* of code were checked by reading that code, and
the ones that claimed *absence* are the ones that failed.

## 3. Corrections to make before building

These are places where the whitepaper describes something that does not match the code.
Each one would surface as rework during Phase 1.

### 3.1 RETRACTED — the memory verbs *are* backed by shipped tools

**This finding was wrong and is withdrawn in full.** An earlier revision of this document
claimed the shipped memory surface was five CRUD tools (`memory_add`, `memory_get`,
`memory_list`, `memory_remove`, `memory_update`); that the whitepaper's `remember` /
`recall` / `explore` / `consolidate` / `promote` vocabulary existed only in
`docs/plans/mcp-memory-002-ac-catalog.json` and test names; that `memory_remember` did not
exist; and that WP §11's alias relationship was therefore inverted. None of that holds.

`mcps/mcpserver/tools/` contains **eleven** memory descriptors, not five: `memory_add`,
`memory_consolidate`, `memory_explore`, `memory_get`, `memory_list`, `memory_promote`,
`memory_recall`, `memory_remember`, `memory_remove`, `memory_revert`, `memory_update`.
Each carries a full input schema, and the richer verbs have implementations in
`src/McpServer.Services/Memory/` (`MemoryPromoteOperations.cs`, `MemoryExploreOperations.cs`,
among others).

The alias direction runs the other way from what this document claimed.
`memory_remember`'s own descriptor reads: "Use this instead of `memory_add` when title,
type, tags, confidence, or provenance matter." So `memory_remember` is the typed,
provenance-carrying write path and `memory_add` is the thinner compatibility surface —
which is what WP §11 already said. `memory_consolidate` likewise exists, with
`dryRun`, `similarityThreshold`, and `allowHardDelete` parameters.

**Recommendation: none. No whitepaper change is required here, and WP §11 should be left
alone.** The memory bridge should bind to `memory_remember` for writes that carry
provenance and `memory_recall` for meaning-ranked reads, rather than to the compatibility
CRUD pair this document previously recommended.

**Why it was wrong.** The claim came from reading `MemoryService.cs` and a partial listing
rather than enumerating the descriptor directory. It is the same failure mode as the §3.2
retraction below: asserting absence from a narrow read instead of confirming absence across
the tree. Absence is the expensive claim to make and the one this document twice got wrong.

### 3.2 SessionLog *is* append-only — the recoverability invariant is already backed

**An earlier draft of this document claimed the opposite. That claim was wrong and is
retracted.** It was based on reading `SessionLogTurnEntity.cs` plus the `sessionlog_*`
tool descriptions, neither of which shows the mechanism: the tombstones are EF **shadow
properties** declared by convention in `McpDbContext`, so they appear on no entity class.

The actual enforcement, all in `McpServer.Storage/McpDbContext.cs`:

- `ApplySoftDeleteMetadata` adds `IsDeleted`, `DeletedAtUtc`, `DeletedBy`, and
  `DeleteReason` to every type in `DurableEntityTypes` — every mapped type whose name
  ends in `Entity` except `DataAuditLogEntity`. `SessionLogTurnEntity` and all six of
  its child types qualify.
- `ApplySoftDeleteQueryFilters` adds a global `IsDeleted == false` filter to each, so the
  `_db.Remove` calls cited in the earlier draft produce invisibility, not destruction.
- `ApplySoftDeletes` rewrites every `EntityState.Deleted` entry to `Modified` and stamps
  the tombstone.
- `BlockPhysicalDeletes` then **throws** `InvalidOperationException` — "Physical deletes
  are blocked for persistent MCP data" — if any `Deleted` entry survives. A physical
  delete of a turn is not discouraged; it is impossible through the context.
- `AppendAuditRows` writes `DataAuditLogEntity` rows for added, modified, and deleted
  entries, carrying `PreviousSnapshotJson`, `CurrentSnapshotJson`, and `DiffJson`.
  `DataAuditLogEntity` is documented as the "append-only generic audit ledger" and is
  itself excluded from soft-delete, so the ledger cannot be tombstoned either.

So original writes and additive data are reconstitutable from the audit ledger, and the
rows themselves are never physically gone. Design Principle #1 holds, and WP §6's
recoverability invariant rests on infrastructure that already exists and already
fail-closes. The eviction-versus-demotion distinction is sound.

**Recommendation — now a much smaller one.** Nothing to build; two things to write down.

1. **Cite the mechanism in the whitepaper.** WP §6 asserts recoverability without naming what
   guarantees it. Naming `BlockPhysicalDeletes` and `DataAuditLogEntity` turns an
   assumption into a verifiable claim, and stops the next reviewer from making the mistake
   this section just made.
2. **Fix the tool descriptions.** `sessionlog_delete_session` (line 306) advertises itself
   as "Irreversible" and `sessionlog_delete_turn` (line 282) as deleting "all of its child
   rows." Both are inaccurate against the storage layer. These strings are what an agent
   reads when deciding whether a call is safe, so the inaccuracy is load-bearing in the
   wrong direction — it makes agents more reluctant than the infrastructure requires, and
   it misled this review.

One genuine follow-up: recovery requires `IgnoreQueryFilters`, and there is no
`sessionlog_restore` or equivalent tool. If WP §6 re-admit is ever to read a tombstoned turn
rather than only a demoted one, it needs an explicit `IgnoreQueryFilters(SoftDeleteQueryFilter)`
read path — the pattern already used in `RequirementsDatabaseDocumentService` and
`ToolRegistryService`. Storage-layer recoverability is settled; operator-facing
reconstitution is a small unbuilt convenience, not a missing invariant.

### 3.3 There is no `payload` field

WP §4.2 models a turn as carrying a `payload`. In `SessionLogTurnEntity.cs` turn content is
spread across five scalar columns — `QueryText`, `Response`, `Interpretation`,
`RawContextJson`, `OriginalEntryJson` — plus six child collections: `Actions`, `Tags`,
`ContextItems`, `ProcessingDialog`, `Commits`, `StringListItems`.

**Recommendation.** Superseded by §5 — the sealed-projection design resolves this by
reusing the existing `UnifiedRequestEntryDto` contract as the payload definition. If §5 is
not adopted, `payload` must instead be defined as an explicit projection-time assembly over
those eleven sources with a specified order, and the WP §6 load step documented as a
multi-table include rather than a column read.

### 3.4 `Score` is already taken

`SessionLogTurnEntity.Score` exists as `double?`, documented as "Success score for this
turn." It is not a projection weight. Reusing it would silently conflate turn quality
with projection priority.

**Recommendation.** Add `Weight` as a new column. `TokenCount` (`int?`, already present)
can back `tokenEstimate` directly and should be reused rather than duplicated.

### 3.5 Workspace scoping is implicit, and turn tables are the gap

**Partly retracted.** An earlier draft called an unscoped projection query a cross-tenant
read. That overstated it: `McpDbContext` declares 33 `HasQueryFilter("Workspace", …)`
filters and `SessionLogEntity` is one of them (line 993), so reads that start at or
navigate from the session are tenant-scoped without caller effort. `StampWorkspaceId`
also fills the discriminator on insert automatically.

What remains is narrower and still worth knowing: **`SessionLogTurnEntity` and its child
types have no `Workspace` query filter of their own.** They carry `WorkspaceId` (per
`20260226113035_AddWorkspaceIdMultiTenant`), so they are filterable — but a projection
query that starts at `SessionLogTurns` directly instead of navigating from `SessionLogs`
inherits no scoping.

**Recommendation.** Have the projector navigate from `SessionLogEntity`, or filter
`WorkspaceId` explicitly. Worth one line in WP §4.2 so the key is `(WorkspaceId, SessionLogId)`
rather than `sessionId` alone. This is a code-style precaution, not a defect class. Under
§5 it resolves once at seal time rather than on every projection read.

## 4. What already exists

Two scoping assumptions in §6 are more favorable than the whitepaper assumes.

### 4.1 The Option C invoker is mostly built

`McpServer.Common.AgentCli` already provides what §6 Option C describes as new work:

- `IAgentCliClient.InvokeAsync(prompt, options, ct)` — a one-shot that takes a prompt blob
  and returns a structured `AgentCliResult`. This is the Option C call shape.
- `IProcessSpawner` / `DefaultProcessSpawner` — fresh process per invocation, already
  abstracted for testing.
- `InvokeStreamingAsync` for line-by-line stdout.
- `CreateInteractiveSession` — the `-i` resume path, which is precisely what Option C must
  *not* use. It already exists and is already distinguished from the one-shot path.

So Phase 2 does not need an invoker. It needs a projector that produces the prompt blob
`InvokeAsync` already accepts. That is a meaningful reduction against the whitepaper's
Phase 2 estimate.

**One real gap.** The argument vocabulary is hardcoded. `AgentCliClient.cs` (~lines 282–314)
branches on `NormalizeAgentName` into exactly two cases: a `cline` branch (`-p`, `-c`,
`--thinking`) and an everything-else branch (`-p`/`-i`, `--model`, `--silent`, `--stream on`,
`--yolo`). There is no per-CLI flag profile and no extra-arguments escape hatch.
`AgentCliClientOptions.AgentPath` defaults to `"cline"`.

§8.1's gate requires verified fresh-session / no-resume flags for the chosen CLI. That
gate cannot be satisfied by configuration today — it needs a flag-profile abstraction
first. This is small but it is a prerequisite, not a nice-to-have, and it belongs in
Phase 2 scope explicitly. It also bears on the whitepaper's no-invented-flags standard:
the flag set for any CLI other than `cline` is currently an assumption in code, and
should be verified per CLI rather than inherited.

### 4.2 Option B is closer than the whitepaper implies

§6 frames Option B as a weaker fit given a preference for CLI subscriptions. The code
complicates that framing:

- `Microsoft.Agents.AI`, `Microsoft.Agents.AI.OpenAI`, and `Microsoft.Agents.AI.Workflows`
  are already in `Directory.Packages.props`.
- `QBAgentChatClientFactory` already builds a `Microsoft.Extensions.AI.IChatClient` against
  the QuadBrain OpenAI-compatible endpoint, described in its own doc comment as making
  QuadBrain "a drop-in OpenAI model behind the Agent Framework loop."

Full `messages[]` control therefore already exists on the QuadBrain path. Option B's
central technical prerequisite is met.

**Recommendation.** Stop treating B and C as exclusive. Have the projector emit a
neutral assembly and render it two ways — `IList<ChatMessage>` for the `IChatClient` path,
a single prompt blob for the `IAgentCliClient` path. The selection then becomes a host
configuration choice rather than an architectural commitment, and the §9 Option decision
narrows to "which path do we validate first," which is a much cheaper decision to get wrong.

### 4.3 Schema changes cost three migrations, and there is a guard to extend

Schema work multiplies across `McpServer.Storage.SqliteMigrations`,
`McpServer.Storage.SqlServerMigrations`, and `McpServer.Storage.PostgreSqlMigrations`,
plus `McpDbContextModelSnapshot`.

`SessionLogSchemaGuard` already exists as a fail-closed probe: it throws
`SessionLogSchemaPendingMigrationException` when expected columns are absent, naming the
specific pending migration per provider.

**Recommendation.** Extend that guard with the new projection columns. It is the
difference between a partial deploy failing loudly and a partial deploy projecting
silently without weights — which would look like a context-quality problem and be
diagnosed as one. This is the cheapest high-value item in this document.

## 5. Sealed projection (ImmutableSessionLog)

Operator-proposed and, on the sealing trigger, operator-decided. This section records the
design and the questions it leaves open. It supersedes §3.3 and §3.5 as the recommended
resolution for both.

The shape: project an immutable per-turn record out of SessionLog, and make that record —
not the live tables — the input to context projection.

### 5.1 Trigger: seal on turn completion — **decided**

Seal when a turn reaches a terminal status, not on every write. A turn is authored across
`sessionlog_begin_turn` → n appends (`sessionlog_dialog` appends processing dialog items,
and `SessionLogTurnEntity` documents that the model may append them independently) →
`sessionlog_complete_turn` or `sessionlog_fail_turn`. Only at that terminal point is the
turn genuinely immutable.

Projecting per write would produce many superseded versions per turn, which matters here
for a specific reason: see §5.5.

In-progress turns are read live from the mutable tables. This costs nothing, because the
current turn is never a compaction candidate.

**Two triggers, not one.** `sessionlog_submit` is an upsert of an entire
`UnifiedSessionLogDto`, so turns can arrive already carrying `completed` or `failed`
status — the ingestion path (`SessionLogIngestor`, `MarkdownSessionLogParser`) does exactly
this. Sealing must therefore fire on terminal-status turns arriving via `submit`, not only
on the `complete_turn` / `fail_turn` transitions. Observed statuses in `SessionLogService`
are `in_progress`, `completed`, `failed`.

### 5.2 Payload schema: reuse `UnifiedRequestEntryDto`

No new shape is needed. `UnifiedRequestEntryDto`
(`McpServer.Client/Models/SessionLogModels.cs`) is already the assembled turn — `requestId`,
`timestamp`, `queryText`, `queryTitle`, `response`, `interpretation`, `status`, `planFile`,
`todoId`, `model`, and nested collections including `actions`. It is already the wire
contract for `sessionlog_complete_turn`, `sessionlog_replace_turn`, and
`sessionlog_replace_section`, and already what the ingestor and the Markdown parser emit.

Sealing serializes that DTO once and stores it. §3.3's problem — `payload` having no
definition and therefore `tokenEstimate` having no stable basis — is resolved by
construction, and the assembly order question is answered by an existing contract rather
than a new convention.

### 5.3 Split immutable content from mutable scoring

The WP §4.2 field list divides cleanly, and should be stored as two tables:

| Immutable — sealed once at terminal status | Mutable — rewritten per projection generation |
|---|---|
| serialized `UnifiedRequestEntryDto` payload | `weight` |
| `tokenEstimate` over that payload | `pin` |
| turn identity (`sessionLogId`, `requestId`) | `projectionState` |
| `workspaceId` | `projectionGeneration` |
| seal sequence, superseded marker | `reAdmitCount`, `lastReAdmitGeneration`, `summaryText` |

`weight` must **not** live in the immutable record. It is re-scored every turn under WP §10
hysteresis, so storing it in an append-only structure would force a new sealed version on
every rescore — turning §5.5's growth concern from O(turns) into O(turns × passes).

### 5.4 Re-seal on re-open — open question

`sessionlog_begin_turn` is documented as "Begin (**or re-open**) a session turn with status
`in_progress`." A sealed turn can therefore be un-sealed, and `sessionlog_replace_turn`
can rewrite a completed turn wholesale with PUT semantics.

So sealing is not one-shot, and the immutable record cannot be updated in place without
abandoning the property it exists to provide. The recommended handling: append a new
sealed version under a monotonic sequence and mark the prior version superseded; readers
take the highest non-superseded sequence per turn. `FederationOutboxEntity` already uses a
database-generated monotonic `Sequence` for a comparable purpose and is a reasonable
pattern to copy.

What needs deciding: whether a re-opened turn's prior seal stays readable to projection
(treat it as history) or is excluded immediately (treat it as retracted). This affects WP §6
re-admit semantics and should not be settled implicitly by whoever writes the query.

### 5.5 Growth, and why sealing per turn matters

An entity named `ImmutableSessionLogEntity` ends in `Entity`, so `DurableEntityTypes` picks
it up: it inherits the soft-delete shadow properties, and `BlockPhysicalDeletes` will throw
on any attempt to physically remove it (§3.2). That is the correct behavior for a durable
record, and it means **sealed versions can never be pruned through the context.**

Sealing at terminal status makes version count O(turns). Sealing per write would make it
O(writes), permanently, with no delete path. This is the concrete reason §5.1 is decided the
way it is.

### 5.6 Status and repair are not optional

This repo has built a materialized projection before and recorded what it cost.
`ITodoWorkflow` exposes `GetProjectionStatusAsync` and `RepairProjectionAsync`, and its own
documentation states that projections "can become stale or corrupted; use
`RepairProjectionAsync` to rebuild them from source TODO data."

Ship the equivalents with ImmutableSessionLog from the start: a status/health check, and a
rebuild-from-source path. The source tables remain authoritative; the sealed log is a
derived read model and must be treated as disposable and reconstructible. A projection that
cannot be rebuilt is a second source of truth by accident.

Precedent for building derived rows inside the write path already exists in
`AppendAuditRows`, which adds `DataAuditLogEntity` rows during `SaveChanges`.

### 5.7 Second-order benefit: a stable density denominator

WP §6 packs by `density = weight / max(tokenEstimate, 1)`. `TokenCount` is nullable today and
is not guaranteed to reflect the assembled turn, so `tokenEstimate` can move between passes
for reasons unrelated to scoring — which reorders the pack and registers as flips.

Sealing fixes the denominator at terminal status. Measured flip rate then attributes to the
scorer alone, which is what the Phase 4 ≤ 0.10 flips/turn/pass gate in WP §9.1 and §8 needs
in order to mean anything. Without a stable denominator that threshold measures the
estimator as much as the scorer.

### 5.8 Three copies — decide deliberately

This puts turn content in three places: the source tables, `DataAuditLogEntity` snapshots,
and the sealed log. That is defensible when the roles are distinct — the audit ledger exists
for reconstitution and compliance, the sealed log for read performance and a stable token
denominator — but it should be written down as a decision with those roles stated. Someone
will eventually ask why the database is several times larger than the conversation, and the
answer should be on record before they do.

## 6. Runtime deployment options

Honest comparison of where this can live.

### Option A — Stay inside IDE plugins

**What works:** Hook injection of REQUIRED MEMORIES; PreCompact / PostCompact reactions; prompt prefixes; limited transcript glimpses depending on host APIs.

**What fails:** Plugins do not own the full host transcript. The host can still auto-compact. Projection can *mitigate* loss but cannot guarantee that model-facing context equals SessionLog-derived projection.

**Verdict:** Necessary interim hardening; not sufficient for the end state.

### Option B — Microsoft Agent Framework with client-managed history

MAF supports client-managed chat history patterns: an `AgentSession` holding local conversation state, plus a pluggable `ChatHistoryProvider` that controls where history lives and how it is retrieved (`InMemoryChatHistoryProvider` ships as the default; a `DatabaseChatHistoryProvider` is the application-implemented durable option). On invoke, the framework can obtain an exact history list from the provider (modulo system messages, tools, and context-provider contributions). That is materially stronger control than opaque host threads.

**Caveat:** Foundry service-managed conversations (or any service-owned history store) weaken control: the service may retain or reshape history outside the projector. Prefer client-managed providers when the goal is weighted projection.

**Naming note (carried over from whitepaper v0.1.3):** earlier revisions cited `ChatMessageStore` as the client-managed abstraction. That name is not used by the cited Agent Framework guidance—it is earlier-preview / Semantic Kernel terminology—and has been corrected to `AgentSession` / `ChatHistoryProvider` here and in WP §12.

**Verdict:** Strong fit for API-shaped agents where message arrays are first-class. Weaker fit when the operational preference is frontier **CLI subscriptions** rather than per-token API burn.

### Option C — Outer orchestrator (extend QBAgent) + sessionless frontier CLI oneshots

**Shape:**

1. SessionLog is source of truth.
2. ContextProjector builds a prompt blob under budget.
3. AgentInvoker calls Claude / Grok / Codex / etc. as a **sessionless oneshot** using that CLI’s fresh-session / no-resume flags **(assumption: exact flag names are CLI-specific and must be verified in the Phase 2 spike; do not invent flags here)**.
4. Capture the full turn back into SessionLog—including tool traces when the orchestrator owns the tool loop, or a recorded note that inner traces were CLI-owned and unobservable when it does not (see the tool-ownership declaration below); reweight; repeat.

**Why sessionless:** Resuming a vendor CLI session reintroduces vendor-owned transcript and their compaction. Fresh oneshots preserve *our* projection as the model-facing context.

**Trade-offs (engineering-honest):**

- Control is **prompt-blob**, not a full structured `messages[]` API (unless the chosen CLI exposes one).
- Tool loops: either let the CLI own inner tools for that oneshot, or keep tools in the orchestrator and pass results in the next projection. Both are viable; split-brain tooling is the failure mode to avoid. **The spike must declare which owner it uses before starting**, because the two branches have different observability ceilings and therefore different acceptance gates (§8.1).
- Fair-use / rate limits of CLI **subscriptions** still apply even for sessionless oneshots. This is not infinite capacity and is not a claim of uncapped API throughput.
- Prompt-cache friendliness: keep a **stable prefix** (system + standing memories) and a **variable tail** (projection segments). Do not reshuffle the prefix every turn.
- Host UI may still compact its *display* transcript; that is UX, not model-facing truth, if invocation bypasses the host model path.

**Verdict:** Best match for escaping host auto-compaction on the *model-facing* path while remaining compatible with CLI subscription usage (fair-use still applies). **Primary recommendation for Phase 2 spike** (pending operator approve/reject in §9).

---

## 7. Recommendations

Mapped **1:1 to the roadmap phases** in §8. Cross-cutting constraints listed after.

| Rec | Maps to | Action |
| --- | --- | --- |
| **R0** | **Phase 0** | Treat compaction / context loss as the primary problem; complete operator review of the whitepaper and of this proposal; run Hostile Validation when unblocked (Perplexity HV still pending API key). Keep the durable memory layer as the cross-session companion, not the turn ledger. |
| **R1** | **Phase 1** | Extend MCP `sessionlog_*` with weight / pin / projection metadata (prefer extend over new store); ship offline projection simulator with the schema fields and I/O in §8 Phase 1 exit criteria. |
| **R2** | **Phase 2** | Spike Option C (outer orchestrator + one Claude or Grok sessionless CLI oneshot) early—before over-investing in in-host projection theater. Meet the Phase 2 spike acceptance checklist in §8.1. |
| **R3** | **Phase 3** | Keep Option A PreCompact fallback for hook-rich hosts (Claude / Grok / Copilot) during transition; measure re-paste rate vs baseline. |
| **R4** | **Phase 4** | Scorer honesty: ship v1 rules with hysteresis before any learned / LLM judge (v2). |

**Cross-cutting (all phases):**

- Do **not** rehabilitate `memory-bench-whitespace` (whitespace/estimator multiturn bench) as provider-metered savings proof.
- Success = task continuity without operator re-paste after reproject / compact fallback—not estimator deltas.
- Prefer MAF client-managed history (**Option B**) when the runtime is API-native rather than CLI-subscription-native; Option B is a parallel path, not a substitute for the Phase 2 CLI spike unless the operator redirects.

---

## 8. Roadmap and phase gates

| Phase | Deliverable | Exit criteria |
| --- | --- | --- |
| **0** | This whitepaper + Hostile Validation (HV) alignment | Operator review of open questions; HV still pending where blocked on API keys |
| **1** | SessionLog schema extension (weight/pin/projection) + offline projection simulator on recorded sessions | **Schema fields present:** `turnId`, `sessionId`, `payload`, `weight`, `pin`, `projectionGeneration`, `projectionState`, `summaryText`, `tokenEstimate`, `reAdmitCount`, `lastReAdmitGeneration`; projection object fields `budgetTokens`, `generation`, `segments[]`, `omittedTurnIds[]`, `standingMemoryIds[]`. **Simulator I/O:** inputs = recorded SessionLog turns + budget `B` + pin set; outputs = `contextProjection` JSON + budget-adherence report + omit/re-admit trace. Replay diffs vs full-context baseline; no production CLI dependency yet. Prefer extend `sessionlog_*` over a new store unless operator rejects. |
| **2** | Outer-orchestrator spike (extend QBAgent) with **one** CLI (Claude or Grok) sessionless oneshot | End-to-end: log → score → project → oneshot → append → reweight; one re-admit demo; continuation without operator re-paste; **no** estimator-savings claim |
| **3** | PreCompact fallback for hook-rich hosts (Claude / Grok / Copilot) | Inject projection / memory on compact gate; measure re-paste rate vs baseline |
| **4** | Scorer v1 rules → v2 LLM judge with hysteresis | `projectionState` flip rate ≤ **0.10** per turn per pass (WP §9.1 metric 2, operator-adjustable but fixed before the phase opens); quiet-constraint eval suite green |

### 8.1 Phase 2 spike acceptance checklist (pass/fail)

All bullets must be **pass** before calling the Phase 2 spike done. Fail any → not done.

- [ ] **PASS/FAIL — Fresh CLI flags:** Sessionless oneshot uses verified fresh-session / no-resume flags for the chosen CLI (Claude **or** Grok); flag names documented from that CLI’s real help/docs—not invented.
- [ ] **PASS/FAIL — Projection under budget:** Assembled prompt / projection is ≤ configured `budgetTokens` (local estimator OK for this engineering gate only).
- [ ] **PASS/FAIL — Tool-ownership declaration:** The spike states in writing, before running, whether the orchestrator or the CLI owns the inner tool loop (§6 Option C, WP §10.4).
- [ ] **PASS/FAIL — SessionLog append:** Full oneshot turn appends to SessionLog via extended `sessionlog_*` (or agreed interim path)—no silent drop. Scope depends on the declaration above: **orchestrator-owned tools** → request + response + full tool traces must all append; **CLI-owned tools** → request + response + whatever traces the CLI surfaces must append, *and* the turn must record that inner traces were CLI-owned and unobservable. An unobservable trace that is explicitly marked is a pass; an unobservable trace that is silently absent is a fail.
- [ ] **PASS/FAIL — Reweight:** At least one scorer/projector pass updates `weight` / `projectionState` after append.
- [ ] **PASS/FAIL — Re-admit demo:** One previously omitted turn is re-admitted into a later projection and used by a subsequent oneshot (receipt: turnIds + generations).
- [ ] **PASS/FAIL — No estimator savings claim:** Spike write-up does **not** claim provider-metered token savings or cite `memory-bench-whitespace` deltas as cost proof.
- [ ] **PASS/FAIL — Continuation:** Operator (or rubric) confirms task continues without re-pasting standing constraints after reproject.

### 8.2 Out of scope for v0.1.x

Explicitly **not** attempted in this proposal or the Phase 0–2 decision window:

- Learned / trained scorer weights or production v2 judge prompts
- Full eight-CLI matrix (Codex / Cline / OpenCode / Copilot / …) oneshot certification
- Legal opinion on vendor CLI Terms of Service for high-frequency sessionless oneshots
- Provider-metered A/B cost studies or published “token savings %”
- New SessionLog implementation code or production migrations (Phase 1 may prototype schema offline only)
- Inventing additional literature beyond the frozen citation list in WP §12
- Claiming Perplexity Hostile Validation complete while API key remains missing post-reseed
- Publishing operator `add-profile` / standing-rules profile files publicly

---

### 8.3 Where the code read changes the phasing

Ordered against the whitepaper's own phases, with the above folded in.

**Phase 1 — sealed projection, schema, and offline simulation.**
Per §5: an immutable sealed-turn table (serialized `UnifiedRequestEntryDto`,
`tokenEstimate`, identity, `workspaceId`, seal sequence, superseded marker) written at
terminal status from both `complete_turn`/`fail_turn` and terminal-status turns arriving via
`submit`; plus a mutable projection-state table (`Weight`, `Pin`, `ProjectionGeneration`,
`ProjectionState`, `SummaryText`, `ReAdmitCount`, `LastReAdmitGeneration`). Status and
repair operations per §5.6. No tombstone or soft-delete work is needed (§3.2). Three
provider migrations plus snapshot. Extend `SessionLogSchemaGuard`. Build the simulator as a
test project replaying recorded sessions, so scorer changes are measurable before anything
reaches a live loop.

**Phase 2 — projector and one host.**
Flag-profile abstraction in `McpServer.Common.AgentCli`. `ContextProjector` producing the
neutral assembly of §4.2. Payload assembly per §3.3. Wire into `QBAgentRunLoop`. Validate
one render path; leave the other behind configuration.

**Phase 3 — fallback.**
PreCompact-equivalent handling. `plugins/core/lib-node` and `plugins/core/lib-sh` are the
existing plugin seams.

**Phase 4 — scorer v1.**
Rules-based only, measured against the Phase 1 simulator, gated on the ≤ 0.10 flips per
turn per pass threshold now fixed in WP §9.1 and §8.

## 9. Immediate next actions (operator review)

Concrete checklist for tomorrow—no need to re-derive the design:

- [ ] **Approve or reject Option C** as the primary Phase 2 spike target (Claude or Grok CLI sessionless oneshot; prompt-blob control, not full `messages[]` unless that CLI exposes it).
- [ ] **Approve Phase 1 SessionLog path:** extend existing MCP `sessionlog_*` with weight / pin / projection metadata **vs** stand up a new store (default recommendation: extend).
- [ ] **Confirm success metric:** zero operator re-paste of constraints / paths / acceptance criteria after reproject (or compact fallback)—not estimator token deltas; not provider-metered savings claims from `memory-bench-whitespace`.
- [ ] **Note:** Perplexity HV still **pending API key** after box reseed—do not treat HV as done.
- [ ] **Note:** Full 19-file `add-profile` restore still needed when PAYTON-LEGION2 reconnects; standing rules currently restored from durable memory only. Never publish profile files publicly.
- [ ] **Skim related-work table** for fairness (especially PACE proximity + re-admit gap); literature list is frozen for v0.1.x—no invented papers.

---

## 10. Open questions for the operator

1. **Should the `sessionlog_*` delete descriptions be corrected** (§3.2)? They tell agents
   an operation is irreversible when the storage layer guarantees it is not.
2. *(Withdrawn — this asked whether consolidation was an unbuilt assumption. `memory_consolidate`
   ships with a real schema and implementation; see the §3.1 retraction. No decision needed.)*
3. **Which CLIs must the flag profile cover at Phase 2 exit?** Only `cline` has verified
   flags today.
4. **Does the Option B/C decision still need making,** given WP §4.2? If the two-renderer
   approach is accepted, §9 may be deciding something it no longer needs to decide.
5. **Re-opened turns (§5.4):** does a superseded seal stay readable to projection as
   history, or is it excluded as retracted? This changes WP §6 re-admit semantics.

## 11. Method and limits

Findings come from a direct read of `main` at `e7c43a12`: entity definitions under
`src/McpServer.Storage/Entities/`, the MCP tool surface in
`src/McpServer.Support.Mcp/McpStdio/`, `src/McpServer.Services/Services/`,
`src/McpServer.Common.AgentCli/`, `src/McpServer.QBAgent/`, `Directory.Packages.props`,
the tool descriptors under `mcps/mcpserver/tools/`, and — after operator correction —
`McpServer.Storage/McpDbContext.cs`.

**How this document was wrong.** The first draft's most emphatic finding, §3.2, was false.
It came from reading entity classes and tool descriptions without reading the `DbContext`
conventions that govern them. EF shadow properties and `SaveChanges` interceptors are
invisible to exactly that reading strategy. Any future review of this codebase should read
`McpDbContext` before asserting anything about persistence semantics.

**What this review did not do.** It did not build or run the solution, did not execute the
test suite, and did not read the memory branch beyond its tool descriptors and file layout.
No effort estimate is offered in engineer-days, and no claim is made about token savings —
the whitepaper's prohibition on estimator-based savings claims applies here too. Branches
other than `main` and the whitepaper branch were not surveyed, so a capability may exist
in flight that this document reports as missing.

Line numbers are accurate as of the baseline commits and will drift.

---

## Document control

| Version | Date (CT) | Notes |
| --- | --- | --- |
| v0.1.0 | 2026-09-20 | Created by splitting proposed implementation out of the whitepaper (whitepaper v0.1.6) and folding in `implementation-recommendations-v0.1.md`, which this document replaces. Contents: deployment options (§6), recommendations (§7), roadmap and phase gates (§8), and immediate next actions (§9) moved from the whitepaper; code-grounded corrections (§3), existing capability (§4), the sealed-projection design (§5), phasing notes (§8.3), open questions (§10), and method (§11) carried over from the recommendations note. Two findings from that note are retracted in place — see §3.1 and §3.2 |

**Non-claims.** This document inherits the whitepaper's non-claims and adds no measured results of its own. It asserts no benchmark outcome, no provider-metered cost figure, and no completed validation. No build was run and no tests were executed while writing it; every code claim is a read of `main` at the stated baseline and may be falsified by re-reading it.
