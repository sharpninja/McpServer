# Weighted Context Projection — Proposed Implementation

**Document:** proposed-implementation-weighted-context-projection-v0.1.md
**Version:** v0.1.11
**Status:** Proposed. Requires operator approval before any build work begins.
**Companion to:** `whitepaper-weighted-context-projection-v0.1.md` (v0.1.16) and `addendum-retrospective-linking-and-goal-metrics-v0.1.md` (v0.1.13)
**Code baseline:** `main` @ `e7c43a125e1bb4837b5b9b9d4021ae2b592f931f`
**Date:** 2026-09-20 (revised 2026-09-26, v0.1.11)

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

**Recommendation.** The memory bridge should bind to `memory_remember` for writes that carry
provenance and `memory_recall` for meaning-ranked reads, rather than to the compatibility
CRUD pair this document previously recommended.

**Correction to this retraction's own instruction.** The first version of this paragraph said
"no whitepaper change is required here, and WP §11 should be left alone." That overcorrected.
WP §11's alias *direction* was right, but its wording still presented a bare `remember` verb
as canonical with `memory_remember` as an "alias" of it, and Principle 6 did the same. An
implementer following those definitions literally would look for bare verbs that do not exist
on the tool surface. Both were reworded in whitepaper v0.1.8 to bind to the shipped `memory_*`
tools and to mark the short verb forms as prose shorthand.

The lesson generalizes past this section: retracting a wrong finding is not the same as
confirming the original text was well-phrased, and "leave it alone" is itself a claim that
needs checking.

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

#### 3.2.1 Correction: the ledger does not cover the bulk delete paths

The paragraph above overstated the audit guarantee, and external review caught it. The
claim "every mutation is mirrored" is true only of mutations that flow through the tracked
save path, because `AppendAuditRows` runs inside `SaveChanges`.

Session and turn deletion does not take that path. `DeleteTurnAsync` and
`DeleteSessionAsync` call `SoftDeleteRowsAsync`, which issues `ExecuteUpdateAsync` directly
against the query — a server-side bulk update that bypasses the change tracker and
therefore never reaches `AppendAuditRows`. `RestoreRowsAsync`, used by
`TryReviveTombstonedSessionAsync`, has the same shape.

What survives and what does not:

- **Content recoverability survives.** The bulk update only flips the shadow tombstone
  columns. Row content is untouched, and physical deletion is still impossible through the
  context, so the bytes of an omitted or tombstoned turn are still there to be read back.
  WP §6's recoverability invariant holds.
- **A complete deletion history does not.** There is no ledger row recording that a session
  or turn was tombstoned, who did it, or when it was revived. The tombstone columns carry
  `DeletedBy`/`DeleteReason` for the current state, but they are overwritten on revival and
  keep no history of repeated delete/restore cycles.

This is the **third** finding in this work stream to assert a property after reading only
part of the path. The prior two asserted absence; this one asserted completeness from
reading `AppendAuditRows` without checking whether every writer actually goes through
`SaveChanges`. Recorded here rather than silently patched.

**Added recommendation:** if the deletion history is to be auditable, the bulk paths must
append ledger rows explicitly, since EF will not do it for them. Until then, no document in
this set should claim a complete deletion/recovery trail.

#### 3.2.2 Correction: physical-delete blocking is a tracked-path guard, not a schema constraint

The correction above said content recoverability was "unaffected" because physical deletes are
"still blocked." That phrasing was too strong, and review was right to press on it. Where the
block actually lives:

- `ApplySoftDeletes` and `BlockPhysicalDeletes` run from `PrepareDbFkChanges`, invoked by the
  `SaveChanges` / `SaveChangesAsync` overrides.
- `BlockPhysicalDeletes` enumerates `ChangeTracker.Entries()` and throws if any persistent
  entity is in state `Deleted`.

Both facts confine the guard to the **tracked** save path. `ExecuteDelete` / `ExecuteDeleteAsync`
and raw `DELETE` SQL are translated straight to the server without materializing change-tracker
entries, so they bypass the guard entirely — no exception, no tombstone. No database-level
trigger or constraint backstops it.

The repository is aware of this: `TransactionGatedSessionLogServiceTests` contains
`TransactionGatedSessionLogServiceSource_DoesNotUseBulkPhysicalDeletes`, which reads the
service's own source file and asserts it contains neither `ExecuteDelete` nor `DELETE FROM`.
That is a **convention guard** — a source-text assertion over one file. It is real protection
and it is deliberate, but it constrains what today's code says, not what the database permits.
A new service, a new call site in another file, or a migration could delete rows outright
without failing any test.

**How to state the invariant.** Content survives deletion on every path the current services
use, upheld by the tracked save path plus that guard test, above the database rather than in
it. This design needs exactly that much and no more, since it only requires omitted turns to
remain readable. What must not be said — and what WP §6 said until v0.1.8 — is that physical
deletion is "rejected outright" as a structural property of the store.

**Recommendation:** if recoverability is to be load-bearing for anything beyond this
projection, move enforcement to the database (a delete-rejecting trigger, or revoking `DELETE`
on the durable tables) so it cannot be undone by code that never reads this document.
Otherwise, keep the guard test and treat it as the boundary it is. See open question 9.

**Pattern note.** This is the fourth correction in this document to generalize from a partial
read of a write path, and the second in a row to over-trust an application-layer guard. The
recurring shape: a check is found, and the property it enforces is then described as holding
unconditionally, without asking which call paths reach the check. Reading the guard is not the
same as reading everything that can avoid it.

#### 3.2.3 Correction: new tables do not inherit tombstoning — Phase 1 must register them

§3.2 concluded that this design needs no soft-delete work because the store already tombstones
rather than deletes. That conclusion holds for the **existing** tables and does not transfer to
the two new ones, which is a distinction the Phase 1 plan elided.

`SoftDeleteTurnRowsAsync` names its targets explicitly — `SessionLogActions`,
`SessionLogTurnTags`, `SessionLogTurnContexts`, `SessionLogProcessingDialogs`,
`SessionLogCommits`, `SessionLogTurnStringLists`, then `SessionLogTurns` — each through a
separate `SoftDeleteRowsAsync` call. The revival path carries the same fixed list. Neither
enumerates the model or discovers soft-deletable types.

So a sealed-turn row and a projection-state row for a turn deleted through
`sessionlog_delete_turn` or `sessionlog_delete_session` would keep `IsDeleted = false`. Naming
the types with an `Entity` suffix earns the shadow columns and the query filter, but nothing
ever flips the values. A projection query reading its own tables would then serve content from
a turn the operator believes is deleted — worse than the audit gap in §3.2.1, because this one
is visible to callers rather than only to auditors.

**Phase 1 must do both:**

1. **Register both new tables** in `SoftDeleteTurnRowsAsync` and in the revival path, in the
   same explicit style, so a deleted turn's seal and projection state are tombstoned with it.
2. **Join projection reads through the source turn**, so visibility derives from the turn
   rather than from a flag that a future table might again forget to set. Belt and braces: (1)
   keeps the data consistent, (2) keeps a missed registration from becoming a disclosure.

That these lists are hand-maintained is itself worth flagging to the operator: every future
child table inherits the same trap. Enumerating soft-deletable entity types from the model
would remove the class of bug, though it is out of scope here.

**Pattern note.** Fifth correction of the same shape, and the sharpest version of it: the
property was real, I checked that it held, and I then assumed it would extend to tables that do
not exist yet. An invariant maintained by a hardcoded list only covers what is on the list.

**Recommendation — now a much smaller one.** Nothing to build; two things to write down.

1. **Cite the mechanism in the whitepaper.** WP §6 asserts recoverability without naming what
   guarantees it. Naming `BlockPhysicalDeletes` and `DataAuditLogEntity` turns an
   assumption into a verifiable claim, and stops the next reviewer from making the mistake
   this section just made — scoped, per §3.2.1 and §3.2.2, to what the ledger actually covers
   and to the paths on which the physical-delete guard actually fires.
2. **Correct the tool descriptions, but keep the warning.** `sessionlog_delete_turn`
   (line 282) says it deletes "all of its child rows"; that is wrong against the storage
   layer, since the children are tombstoned rather than removed, and the wording should be
   fixed.

   `sessionlog_delete_session` (line 306) is a different case, and an earlier draft of this
   section got it backwards by recommending the "Irreversible" warning be dropped. From the
   MCP tool surface that warning is **effectively accurate**. There is no
   `sessionlog_restore` tool; revival happens only as a side effect of re-submitting the
   same session, which requires the caller to still hold enough of the payload to rebuild
   it. A deleted individual turn has no revival path on the tool surface at all. Rows
   surviving in storage is not the same as a caller being able to undo the call.

   So: keep the caution and make it precise — the data is recoverable by an operator with
   database access, not by the agent that made the call. Weakening it would invite agents to
   discard reachable session data expecting an undo that the tool surface does not offer.

One genuine follow-up: recovery requires `IgnoreQueryFilters`, and there is no
`sessionlog_restore` or equivalent tool. If WP §6 re-admit is ever to read a tombstoned turn
rather than only a demoted one, it needs an explicit `IgnoreQueryFilters(SoftDeleteQueryFilter)`
read path — the pattern already used in `RequirementsDatabaseDocumentService` and
`ToolRegistryService`. Storage-layer recoverability is settled *for the current service paths*
(§3.2.2); operator-facing reconstitution is a small unbuilt convenience, not a missing
invariant.

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

**Recommendation.** Add `Weight` as a new column.

**Correction — do not reuse `TokenCount` for `tokenEstimate`.** An earlier draft said
`TokenCount` (`int?`, already present) could back `tokenEstimate` directly. Review caught
that this is wrong, and the code confirms it: `QuadBrainOpenAiChatService` sets
`TokenCount = response.Usage.TotalTokens`, and that total is `promptTokens +
completionTokens` — provider usage for the whole call, including prompt and history. Other
producers populate the same field with their own provider-specific counts.

So `TokenCount` measures what a call cost, not how large the stored turn is when rendered
into a projection. Feeding it into `density = weight / max(tokenEstimate, 1)` would distort
the ordering: turns with long prompts look expensive regardless of their own payload size,
and a turn whose stored content exceeds the provider count would be under-charged against
the hard budget — exactly the overflow the budget invariant is supposed to prevent.

`tokenEstimate` must be computed from the **actual serialized payload** the projector will
send, under the tokenizer for the target model. Keep `TokenCount` as the provider-cost record
it already is.

**Follow-on correction: one stored estimate is not enough.** The first draft of this fix said
"stored as its own column," which review then flagged as still wrong — correctly. A single
frozen value cannot be right across the deployment options this document itself proposes:
Option B assembles an `IList<ChatMessage>`, Option C assembles a prompt blob for CLI stdin,
and the two differ in message wrappers, tool-schema serialization, and formatting before any
tokenizer disagreement is considered. WP §10's "prompt-blob vs messages[] fidelity" risk is
the same observation from the other end.

So the same sealed turn has **different** model-facing sizes depending on renderer and target
model, and a single stored number would let packing report `≤ B` while the assembled request
overflows it — defeating the invariant the estimate exists to enforce.

Split the quantity in two:

- **Immutable, in the seal:** a renderer-neutral measure of the sealed payload — serialized
  byte length, and optionally a token count under one declared reference tokenizer, labelled
  as reference-only. This is genuinely a property of the content and cannot change.
- **Derived, per projection pass:** the budget-facing `tokenEstimate`, computed over the
  finalized renderer-and-model-specific assembly. Cache it keyed by `(renderer, tokenizer)`
  in the mutable table if recomputation cost matters, but never treat a cached value from one
  renderer as valid for another.

The density denominator in WP §6 must use the derived estimate for the renderer actually in
use. §5.7's stability argument still holds — what stabilizes is the *payload*, which is what
made the denominator move between passes; it was never a claim that one number serves every
renderer.

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
`WorkspaceId` explicitly. Worth one line in WP §4.2 so the key is not `sessionId` alone.

**Correction on the key.** An earlier draft wrote that key as `(WorkspaceId, SessionLogId)`,
which is ambiguous in the damaging direction. The repository's actual uniqueness boundary is
`(WorkspaceId, SourceType, SessionId)` — declared as a unique index on `SessionLogEntity` —
because the external `SessionId` is provider-native and two different agents can legitimately
present the same one. An imported Cursor session and an imported Copilot session sharing a
provider ID would be conflated, and projection would assemble turns from the wrong agent.

Use `(WorkspaceId, SourceType, SessionId)` when keying by external identity, or the numeric
database primary key `SessionLogEntity.Id` when keying internally — and say which one is
meant. Do not write `SessionLogId` without stating whether it is the DTO's session id or the
row id.

The missing `WorkspaceId` filter is a code-style precaution. Keying by `SessionId` without
`SourceType` is not — it silently mixes two agents' transcripts, which is a correctness
defect. Under §5 both resolve once at seal time rather than on every projection read.

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

In-progress turns are read live from the mutable tables.

**Correction: the live turn is not free.** An earlier draft said reading it "costs nothing,
because the current turn is never a compaction candidate." That is wrong on both halves. A
long-running turn accumulates independently appended dialog and tool-trace items before it
ever reaches `complete_turn` — `AppendProcessingDialogAsync` is its own call, and
`SessionLogTurnEntity` documents that the model may append items independently. A tool-heavy
turn can therefore exhaust the model window, or trip a vendor CLI's own compaction, inside a
single oneshot.

Treating it as cost-free puts those bytes **outside** the hard-budget invariant of WP §6,
which is the specific failure the whole design exists to prevent — so the outer-orchestrator
deployment would still lose context on exactly the turns that matter most.

The live turn must be charged against `B` like any other content. WP §6.3 is the normative
rule: every orchestrator-issued model invocation, including a tool-loop continuation, is its
own generation snapshot (WP §6.1). The snapshot includes the tombstone set. The commit point
is `sendFence`: the final read and the transport handoff are one critical section. A tombstone,
reseal, evidence change, or superseding verdict cannot commit between that read and handoff.
Exactly one of handoff or mutating commit wins. A re-read followed by a committed mutation
and a handoff of the pre-mutation bytes is non-conforming. Spill unmarked unpinned content first. Shrink an unpinned active harm or
invalidation stub to the marker. Fail closed if the mandatory set cannot fit: prefix,
standing memories, pins with their markers, the live result, and every active harm,
invalidation, or dependency-gap marker (ADD §7.4). Do not omit those active markers, do not
keep a retired marker in the mandatory set, and do not expand an omitted turn only because
residual capacity grew (WP §6 step 5). What remains open is only which in-progress dialog
items count as the most recent tool results that must survive verbatim inside the turn, and
how a renderer estimates them before the seal (§10 question 7). The existence of spill,
active-marker retention, the send check, and fail-closed is not open.

**Two triggers, not one.** `sessionlog_submit` is an upsert of an entire
`UnifiedSessionLogDto`, so turns can arrive already carrying terminal status — the ingestion
path (`SessionLogIngestor`, `MarkdownSessionLogParser`) does exactly this. Sealing must
therefore fire on terminal-status turns arriving via `submit`, not only on the
`complete_turn` / `fail_turn` transitions.

**Correction: five terminal statuses, not two.** An earlier draft said the observed statuses
were `in_progress`, `completed`, `failed`. `SessionLogService.IsTerminalTurnStatus` in fact
recognizes `completed`, `failed`, `closed`, `cancelled`, and `canceled`, and `canceled` is
not hypothetical: `IsSupersededHookPersist` tests for exactly it, and superseded hook turns
are persisted with that status through `ReplaceTurnAsync` and the upsert path. A trigger set
limited to complete and fail would leave those turns permanently unsealed and therefore
absent from the sealed projection.

**Do not restate the status list in the sealing code.** Call
`IsTerminalTurnStatus` — promoted to whatever visibility the sealer needs — so the trigger
and the predicate cannot drift apart. A copied list is exactly how this defect appears.

**Correction: terminal status is not currently a mutability boundary.** §5.1 above assumed
that reaching a terminal status ends authorship. It does not. `ReplaceTurnAsync`,
`ReplaceTurnSectionAsync`, `DeleteTurnItemAsync`, `SetTurnTitleAsync`, and
`AppendProcessingDialogAsync` all accept a turn that is already `completed` and mutate it
**without changing its status** — none of them consults `IsTerminalTurnStatus`, whose only
current caller is the compliance validator. (`SessionLogWorkflow` has an
`EnsureTurnMutable` gate, but that guards the in-memory active turn in the REPL, not the
service write paths.)

A seal taken at terminal status therefore goes stale silently: projection would omit content
appended after completion, or keep content deleted after it. Two acceptable resolutions,
and the choice belongs to the operator:

- **Reject post-terminal mutation.** Have the five paths above fail closed when the target
  turn is terminal, forcing an explicit `begin_turn` re-open first. Cleanest invariant;
  changes existing tool behavior and may break current callers.
- **Reseal on every mutation path.** Leave the write paths permissive and re-seal whenever
  one of them targets a terminal turn. Preserves behavior; costs a new sealed version per
  post-hoc edit, which interacts with §5.5 growth and requires §5.4 to be settled first.

What is **not** acceptable is the draft's implicit third option — sealing on the terminal
transition and assuming nothing edits the turn afterward.

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
| renderer-neutral size of that payload (serialized byte length; reference token count optional) | `pin` |
| — | `tokenEstimate` per `(renderer, tokenizer)`, derived — see §3.4 |
| turn identity (`sessionLogId`, `requestId`) | `projectionState` |
| `workspaceId` | `projectionGeneration` |
| seal sequence (supersession is derived, not stored — see §5.4) | `reAdmitCount`, `lastReAdmitGeneration`, `summaryText` |

`weight` must **not** live in the immutable record. It is re-scored every turn under WP §6.1
hysteresis, so storing it in an append-only structure would force a new sealed version on
every rescore — turning §5.5's growth concern from O(turns) into O(turns × passes).

### 5.4 Re-seal on re-open — open question

`sessionlog_begin_turn` is documented as "Begin (**or re-open**) a session turn with status
`in_progress`." A sealed turn can therefore be un-sealed, and `sessionlog_replace_turn`
can rewrite a completed turn wholesale with PUT semantics.

So sealing is not one-shot, and the immutable record cannot be updated in place without
abandoning the property it exists to provide.

**Correction to the recommended handling.** An earlier draft said to append a new sealed
version and then "mark the prior version superseded," with readers taking the highest
non-superseded sequence. Review caught that this contradicts itself: the superseded marker
sits in the immutable column group per §5.3, so setting it is an in-place update of a record
that is supposed to be write-once. The versioning scheme would defeat the property it exists
to provide.

Supersession must be **derived, not written back**. Three workable forms, in order of
preference:

1. **Derive from the sequence.** The current seal for a turn is the row with the highest
   seal sequence. No marker column, nothing to update, and the read is a single ranked
   query. `FederationOutboxEntity`'s database-generated monotonic `Sequence` is the pattern
   to copy.
2. **Record the predecessor on the new row.** Each sealed version names the sequence it
   replaces. Still append-only, and makes the chain explicit if history is to be walked.
3. **Move supersession state to the mutable table.** Acceptable if an explicit marker is
   wanted, but it must then be understood as scoring-side state, not part of the sealed
   record.

The §5.3 table is corrected accordingly: the immutable group holds the seal sequence only,
and no superseded marker.

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
is not guaranteed to reflect the assembled turn, so the denominator can move between passes
for reasons unrelated to scoring — which reorders the pack and registers as flips.

Sealing fixes the *payload* at terminal status, and with it the denominator for any one
renderer and tokenizer — which is the variance that mattered. It does not make the estimate
renderer-independent (§3.4). Measured state-flip rate and active-context token
replacement then attribute to the scorer, which is what the Phase 4 bars in WP §9.1
(≤ 0.10 and ≤ 0.25) and §8 need in order to mean anything. Without a stable denominator
those bars measure the estimator as much as the scorer.

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
2. ContextProjector builds a prompt blob under budget. Every later model call in an orchestrator-owned tool loop is its own generation snapshot and a new assembly under the same budget (WP §6.1, WP §6.3): freeze tombstones with the other inputs, spill unmarked unpinned content first, keep active harm and invalidation markers, and fail closed if the mandatory set still does not fit. Final read and transport handoff share `sendFence` (WP §6.1). A mutation that wins the fence cancels handoff. A handoff that wins leaves the mutation for the next generation. Do not hand off bytes from before a tombstone that already committed. Do not expand a turn only because residual capacity grew. A CLI-owned inner loop whose calls this orchestrator cannot measure is not a conforming claim of that invariant.
3. AgentInvoker calls Claude / Grok / Codex / etc. as a **sessionless oneshot** using that CLI’s fresh-session / no-resume flags **(assumption: exact flag names are CLI-specific and must be verified in the Phase 2 spike; do not invent flags here)**.
4. Capture the full turn back into SessionLog—including tool traces when the orchestrator owns the tool loop, or a recorded note that inner traces were CLI-owned and unobservable when it does not (see the tool-ownership declaration below); reweight; repeat.

**Why sessionless:** Resuming a vendor CLI session reintroduces vendor-owned transcript and their compaction. Fresh oneshots preserve *our* projection as the model-facing context.

**Trade-offs (engineering-honest):**

- Control is **prompt-blob**, not a full structured `messages[]` API (unless the chosen CLI exposes one).
- Tool loops: either let the CLI own inner tools for that oneshot, or keep tools in the orchestrator and pass results in the next projection. Split-brain tooling is the failure mode to avoid. **The spike must declare which owner it uses before starting**, because the two branches have different observability ceilings and therefore different acceptance gates (§8.1). Only the orchestrator-owned branch can claim the WP §6.3 budget invariant. A CLI-owned inner loop that issues model calls the orchestrator cannot measure fails the §8.1 budget gate.
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
| **1** | SessionLog schema extension (weight/pin/projection) + offline projection simulator on recorded sessions | **Schema fields present:** `turnId`, `sessionId`, `payload`, `weight`, `pin`, `projectionGeneration`, `projectionState`, `summaryText`, `tokenEstimate`, `reAdmitCount`, `lastReAdmitGeneration`; projection object fields `budgetTokens`, `generation`, `segments[]`, `omittedTurnIds[]`, `standingMemoryIds[]`. **Simulator I/O:** inputs = recorded SessionLog turns + budget `B` + pin set; outputs = `contextProjection` JSON + budget-adherence report + omit/re-admit trace with WP §6.1 `turnCause`, `originCause`, `pressureSource`, and a `rendered-change` event whenever the sent segment changes, including an unchanged `projectionState` (token replacement counts `score`, `edge-score`, and `summary-rewrite`, including tokens a discretionary summary growth displaces; state-flip still counts only a `projectionState` change whose `turnCause` is `score`). Replay diffs vs full-context baseline; no production CLI dependency yet. Prefer extend `sessionlog_*` over a new store unless operator rejects. |
| **2** | Outer-orchestrator spike (extend QBAgent) with **one** CLI (Claude or Grok) sessionless oneshot | End-to-end: log → score → project → oneshot → append → reweight; one re-admit demo; continuation without operator re-paste; **no** estimator-savings claim |
| **3** | PreCompact fallback for hook-rich hosts (Claude / Grok / Copilot) | Inject projection / memory on compact gate; measure re-paste rate vs baseline |
| **4** | Scorer v1 rules → v2 LLM judge with hysteresis | Both WP §9.1 stability bars, operator-adjustable but fixed before the phase opens: state-flip rate ≤ **0.10** (metric 2, denominator = previous active set) and active-context token replacement ≤ **0.25** (metric 3). Quiet-constraint eval suite green |

### 8.1 Phase 2 spike acceptance checklist (pass/fail)

All bullets must be **pass** before calling the Phase 2 spike done. Fail any → not done.

- [ ] **PASS/FAIL — Fresh CLI flags:** Sessionless oneshot uses verified fresh-session / no-resume flags for the chosen CLI (Claude **or** Grok); flag names documented from that CLI’s real help/docs—not invented.
- [ ] **PASS/FAIL - Budget at every issued call:** Each orchestrator-issued model invocation, including calls after live tool results, is one generation snapshot (WP §6.1) and is ≤ configured `budgetTokens`, or it is spilled or refused under WP §6.3 (local estimator OK for this engineering gate only). The snapshot includes tombstones. Final read and transport handoff share `sendFence`. A tombstone that wins the fence cancels handoff. A handoff of pre-tombstone bytes after that tombstone committed is a fail. A spike that lets the CLI own inner model calls the orchestrator cannot measure MUST NOT mark this item pass. It records the gap. A silent over-budget call is a fail. An unpinned spill of unmarked content recorded as `budget-demotion` is a pass. Omitting an active introduced-harm, invalidation, or dependency-gap marker is a fail. Keeping a retired marker in the mandatory set and failing closed for that reason is a fail. Dropping a pin to fit is a fail. Expanding an omitted or summarized turn only because residual capacity grew is a fail.
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
the renderer-neutral size of that payload, identity, `workspaceId`, seal sequence — but
**not** a single frozen `tokenEstimate`, per §3.4) written
at every status recognized by `IsTerminalTurnStatus`, reached through `complete_turn` /
`fail_turn`, through terminal-status turns arriving via `submit`, or through the
post-terminal mutation paths per §5.1; plus a mutable projection-state table (`Weight`, `Pin`, `ProjectionGeneration`,
`ProjectionState`, `SummaryText`, `ReAdmitCount`, `LastReAdmitGeneration`, plus the derived
`tokenEstimate` cache keyed by renderer and tokenizer). Status and
repair operations per §5.6. **Both new tables must be registered in the bulk tombstone and
revival paths, and projection reads must join through the source turn** (§3.2.3) — an earlier
version of this plan said no soft-delete work was needed, which was wrong. Three
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
Rules-based only, measured against the Phase 1 simulator, gated on both WP §9.1 bars
now fixed in §8: state-flip rate ≤ 0.10 and active-context token replacement ≤ 0.25.

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

1. **Should the `sessionlog_*` delete descriptions be corrected** (§3.2)? Narrowed: the
   `delete_turn` "all of its child rows" wording is wrong and should be fixed, but
   `delete_session`'s irreversibility warning should **stay** — no restore exists on the tool
   surface, so it is accurate from the caller's point of view.
2. *(Withdrawn — this asked whether consolidation was an unbuilt assumption. `memory_consolidate`
   ships with a real schema and implementation; see the §3.1 retraction. No decision needed.)*
3. **Which CLIs must the flag profile cover at Phase 2 exit?** Only `cline` has verified
   flags today.
4. **Does the Option B/C decision still need making,** given WP §4.2? If the two-renderer
   approach is accepted, §9 may be deciding something it no longer needs to decide.
5. **Re-opened turns (§5.4):** does a superseded seal stay readable to projection as
   history, or is it excluded as retracted? This changes WP §6 re-admit semantics.
6. **Post-terminal mutation (§5.1):** reject it and require an explicit re-open, or allow it
   and re-seal on every mutation path? The first is a cleaner invariant but changes shipped
   tool behavior; the second preserves behavior at the cost of seal churn.
7. **Live-turn spill mechanics, narrowed by WP §6.3.** Every orchestrator-issued model
   invocation is its own generation snapshot (WP §6.1), including the tombstone set, and
   `sendFence` is the commit point: final read and transport handoff are one critical section. It charges live content, spills unmarked unpinned content
   first, keeps active harm and invalidation markers, and fails closed if the mandatory
   set cannot fit. Retired markers are not in that set (ADD §7.4). Pins, active markers,
   and the live result this call exists to deliver stay. Capacity that appears because
   something else shrank does not authorize an expansion (WP §6 step 5). What remains
   open is which in-progress dialog items count as the most recent tool results that
   must survive verbatim, and how a renderer estimates them before the turn is sealed.
8. **Should the bulk delete paths append audit rows (§3.2.1)?** Today tombstoning and
   revival bypass the ledger, so there is no history of deletion events — only the current
   tombstone state. Content stays recoverable either way; the audit trail does not.
9. **Should physical-delete blocking move into the database (§3.2.2)?** Today it is a
   tracked-path guard plus a source-text test, which set-based or raw-SQL deletes in new code
   would bypass without failing anything. A delete-rejecting trigger or a revoked `DELETE`
   grant would make recoverability structural. Not required by this projection, which only
   needs omitted turns readable — but required by anything that wants to call the durable
   store tamper-evident.
Content stays recoverable either way; the audit trail does not.

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
| v0.1.11 | 2026-09-26 | Consistency with WP v0.1.16 / ADD v0.1.13 (Astra re-review, round 7). Phase 1 trace records `pressureSource` on budget demotion. No new runtime design beyond that contract |
| v0.1.10 | 2026-09-25 | Consistency with WP v0.1.15 / ADD v0.1.12 (Astra re-review, round 6). Phase 1 trace records a `rendered-change` event for same-state summary rewrites. No new runtime design beyond that contract |
| v0.1.9 | 2026-09-25 | Consistency with WP v0.1.14 / ADD v0.1.11 (Astra re-review, round 5). Phase 1 omit/re-admit trace records `turnCause` and `originCause`. No new runtime design beyond that contract |
| v0.1.8 | 2026-09-25 | Consistency with WP v0.1.13 / ADD v0.1.10 (Astra re-review, round 4). `sendFence` joins the final read and transport handoff. No new runtime design beyond that contract |
| v0.1.7 | 2026-09-25 | Consistency with WP v0.1.12 / ADD v0.1.9 (Astra re-review, round 3). Send is the commit point and tombstones are snapshot inputs. The mandatory marker set is active warnings only. No new runtime design beyond those contracts |
| v0.1.6 | 2026-09-25 | Consistency with WP v0.1.11 / ADD v0.1.8 (Astra re-review, round 2). Each orchestrator-issued call is one generation snapshot. Spill keeps harm and invalidation markers in the mandatory set. A capacity-only expansion is a budget-gate fail. No new runtime design beyond those contracts |
| v0.1.5 | 2026-09-25 | Consistency with WP v0.1.10 / ADD v0.1.7. §5.1 and open question 7 follow the WP §6.3 tool-loop budget (spill, then fail closed) instead of leaving that mechanism unresolved. Option C and the §8.1 budget gate require the check on every orchestrator-issued call. Phase 4 gates both stability metrics (state-flip rate and active-context token replacement). Phase 1 trace records the WP §6.1 transition cause. Hysteresis reference retargeted from WP §10 to WP §6.1 |
| v0.1.4 | 2026-09-20 | Round-6 external review; one P1. New tables do not inherit tombstoning: `SoftDeleteTurnRowsAsync` and the revival path name seven tables explicitly and discover nothing, so a sealed-turn or projection-state row would keep `IsDeleted = false` after `sessionlog_delete_turn`/`delete_session` and could serve a turn the operator believes deleted. Phase 1 must register both tables in each path **and** join projection reads through the source turn (new §3.2.3); the Phase 1 line claiming no soft-delete work was needed is corrected. Also renumbered §3.2.1 to a consistent heading depth |
| v0.1.3 | 2026-09-20 | Round-5 follow-up (three findings from the automatic pass on `56996df`). Physical-delete blocking is a tracked-path guard plus a source-text test, not a schema constraint — new §3.2.2, WP §6 narrowed again, open question 9. The §3.2 delete-warning reversal was never propagated to the README review checklist, which still told the operator to correct the warning on the abandoned premise. Banner versions were stale against this document's own changelog (v0.1.0 vs v0.1.1) and against the companion (v0.1.5 vs v0.1.8) |
| v0.1.2 | 2026-09-20 | Round-5 external review; two findings accepted. `tokenEstimate` cannot be a single frozen column in the seal, because Option B's `IList<ChatMessage>` and Option C's prompt blob render the same turn to different sizes under different tokenizers — the seal now carries a renderer-neutral payload size and the budget-facing estimate is derived per `(renderer, tokenizer)` (§3.4, §5.3, §5.7, Phase 1). Also confirmed that the whitepaper's remaining inverted memory-alias definitions needed fixing rather than being left alone, correcting §3.1's instruction |
| v0.1.1 | 2026-09-20 | Round-4 external review; eight findings accepted, all verified against `main` before editing. Corrections: the audit ledger does not cover the bulk tombstone/revival paths (new §3.2.1); `TokenCount` cannot back `tokenEstimate` because it is provider usage including prompt and history (§3.4); sealing must fire on all five statuses recognized by `IsTerminalTurnStatus`, not two (§5.1); terminal status is not currently a mutability boundary, so seals go stale silently unless post-terminal mutation is rejected or triggers a reseal (§5.1); the live in-progress turn is not cost-free and must be charged against the budget (§5.1); supersession must be derived from the seal sequence rather than written back into the immutable row (§5.3, §5.4); the session key needs `SourceType` or must name the numeric row id (§3.5); and the `delete_session` irreversibility warning should be kept, reversing an earlier recommendation, because no restore exists on the tool surface (§3.2). Open questions 6–8 added |
| v0.1.0 | 2026-09-20 | Created by splitting proposed implementation out of the whitepaper (whitepaper v0.1.6) and folding in `implementation-recommendations-v0.1.md`, which this document replaces. Contents: deployment options (§6), recommendations (§7), roadmap and phase gates (§8), and immediate next actions (§9) moved from the whitepaper; code-grounded corrections (§3), existing capability (§4), the sealed-projection design (§5), phasing notes (§8.3), open questions (§10), and method (§11) carried over from the recommendations note. Two findings from that note are retracted in place — see §3.1 and §3.2 |

**Non-claims.** This document inherits the whitepaper's non-claims and adds no measured results of its own. It asserts no benchmark outcome, no provider-metered cost figure, and no completed validation. No build was run and no tests were executed while writing it; every code claim is a read of `main` at the stated baseline and may be falsified by re-reading it.
