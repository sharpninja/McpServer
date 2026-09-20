# Weighted Context Projection — Implementation Recommendations

**Status:** advisory, non-normative
**Companion to:** `whitepaper-weighted-context-projection-v0.1.md` (v0.1.4)
**Code baseline:** `main` @ `e7c43a125e1bb4837b5b9b9d4021ae2b592f931f`
**Whitepaper baseline:** `cursor/whitepaper-context-projection-v012` @ `eb3655a4160164f70c802fbcebd45a3be4232626`
**Date:** 2026-09-20

## 1. Purpose and standing

This document does not amend the whitepaper. The whitepaper describes a design; this
describes what building that design costs against the code that exists today, and
where the two disagree.

Everything below is grounded in a read of `main` at the baseline commit. Every claim
names the file it came from so it can be re-checked or falsified. Where the whitepaper
and the code disagree, the code wins as a statement of fact about the present, and the
disagreement is recorded as a correction to be made — not as a defect in the design.

Nothing here should be read as approval to build. §12 of the whitepaper still owns the
Phase 1 decision and the Option B/C decision.

## 2. Headline

Three things changed shape once the design was read against the code.

**The design is cheaper than it looks in two places.** The Option C agent invoker is
substantially already built, and the Agent Framework dependency that Option B needs is
already referenced and already in use. Neither option is greenfield.

**The recoverability invariant is already backed, and more strongly than the design
claims.** `McpDbContext` blocks physical deletes outright and mirrors every mutation into
an append-only audit ledger with before-and-after snapshots, so §6 can cite enforcement
rather than assume it. An earlier draft of this document asserted the opposite; §3.2 carries
the retraction and why the mechanism is easy to miss.

**Three schema and naming assumptions still need correction before code is written** —
§3.1, §3.3, §3.4 — plus a narrower scoping note in §3.5. None invalidate the design; all
would waste Phase 1 effort if found during implementation instead of before it.

## 3. Corrections to make before building

These are places where the whitepaper describes something that does not match the code.
Each one would surface as rework during Phase 1.

### 3.1 The memory verbs are not tool names

The whitepaper refers throughout to `remember`, `recall`, `explore`, `consolidate`, and
`promote` as the shipped MCP-MEMORY-002 surface, and §15 describes `memory_remember` as
a bridge alias.

The shipped surface is five tools: `memory_add`, `memory_get`, `memory_list`,
`memory_remove`, `memory_update` — one descriptor each under `mcps/mcpserver/tools/`,
backed by `McpServer.Services/Services/MemoryService.cs` and `McpServer.Client/MemoryClient.cs`.

The five verbs appear in `docs/plans/mcp-memory-002-ac-catalog.json` and in test names.
They are the plan's vocabulary, not the tool surface. `memory_remember` does not exist
anywhere in the tree, so the alias relationship in §15 is inverted: there is no verb for
it to alias.

**Recommendation.** Rewrite every verb reference to the actual tool name, and bind the
memory bridge to `memory_add` (write) and `memory_list` (read, which already supports
scope, category, and keyword filters). Note that there is no `consolidate` or `promote`
tool — if the projection design depends on consolidation as an existing capability, that
dependency is unmet and needs its own line item rather than a call site.

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
rows themselves are never physically gone. Design Principle #1 holds, and §6's
recoverability invariant rests on infrastructure that already exists and already
fail-closes. The eviction-versus-demotion distinction is sound.

**Recommendation — now a much smaller one.** Nothing to build; two things to write down.

1. **Cite the mechanism in the whitepaper.** §6 asserts recoverability without naming what
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
`sessionlog_restore` or equivalent tool. If §6 re-admit is ever to read a tombstoned turn
rather than only a demoted one, it needs an explicit `IgnoreQueryFilters(SoftDeleteQueryFilter)`
read path — the pattern already used in `RequirementsDatabaseDocumentService` and
`ToolRegistryService`. Storage-layer recoverability is settled; operator-facing
reconstitution is a small unbuilt convenience, not a missing invariant.

### 3.3 There is no `payload` field

§4.2 models a turn as carrying a `payload`. In `SessionLogTurnEntity.cs` turn content is
spread across five scalar columns — `QueryText`, `Response`, `Interpretation`,
`RawContextJson`, `OriginalEntryJson` — plus six child collections: `Actions`, `Tags`,
`ContextItems`, `ProcessingDialog`, `Commits`, `StringListItems`.

**Recommendation.** Define `payload` explicitly as a projection-time assembly over those
eleven sources, and specify the assembly order, because `tokenEstimate` is meaningless
until it is fixed. This also means the §6 load step is a multi-table include, not a
column read — relevant to both the projection latency budget and the Phase 1 simulator.

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
`WorkspaceId` explicitly. Worth one line in §4.2 so the key is `(WorkspaceId, SessionLogId)`
rather than `sessionId` alone. This is a code-style precaution, not a defect class.

## 4. What already exists

Two scoping assumptions in §7 are more favorable than the whitepaper assumes.

### 4.1 The Option C invoker is mostly built

`McpServer.Common.AgentCli` already provides what §7 Option C describes as new work:

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

§14.1's gate requires verified fresh-session / no-resume flags for the chosen CLI. That
gate cannot be satisfied by configuration today — it needs a flag-profile abstraction
first. This is small but it is a prerequisite, not a nice-to-have, and it belongs in
Phase 2 scope explicitly. It also bears on the whitepaper's no-invented-flags standard:
the flag set for any CLI other than `cline` is currently an assumption in code, and
should be verified per CLI rather than inherited.

### 4.2 Option B is closer than the whitepaper implies

§7 frames Option B as a weaker fit given a preference for CLI subscriptions. The code
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
configuration choice rather than an architectural commitment, and the §12 Option decision
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

## 5. Suggested phasing

Ordered against the whitepaper's own phases, with the above folded in.

**Phase 1 — schema and offline simulation.**
Add `Weight`, `Pin`, `ProjectionGeneration`, `ProjectionState`, `SummaryText`,
`ReAdmitCount`, `LastReAdmitGeneration` to `SessionLogTurnEntity`; reuse `TokenCount`.
No tombstone or soft-delete work is needed (§3.2). Three provider migrations plus snapshot. Extend `SessionLogSchemaGuard`. Build the simulator as a test
project replaying recorded sessions, so scorer changes are measurable before anything
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
turn per pass threshold now fixed in §10.1 and §14.

## 6. Open questions for the operator

1. **Should the `sessionlog_*` delete descriptions be corrected** (§3.2)? They tell agents
   an operation is irreversible when the storage layer guarantees it is not.
2. **Is consolidation assumed?** Per §3.1 there is no `consolidate` tool. If the design
   relies on it, it is unbuilt work rather than an integration point.
3. **Which CLIs must the flag profile cover at Phase 2 exit?** Only `cline` has verified
   flags today.
4. **Does the Option B/C decision still need making,** given §4.2? If the two-renderer
   approach is accepted, §12 may be deciding something it no longer needs to decide.

## 7. Method and limits

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
