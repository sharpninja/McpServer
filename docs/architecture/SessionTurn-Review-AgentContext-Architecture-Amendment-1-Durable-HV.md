<!-- SPDX-License-Identifier: Apache-2.0 -->
# Architecture Amendment 1: Durable Multi-Agent Hostile Validation

Status: proposed amendment, documentation only. It is not approved, not hostile-validated, and not an authorization to implement. It amends the foundation architecture `docs/architecture/SessionTurn-Review-AgentContext-Architecture.md` (the "foundation"; at develop `36783b79` it is byte-identical to the copy in the `session-20261009` worktree, SHA-256 `6E5D4F418E78CF9A76AB366F64D4F24002C09E3C3F024502ED89D77C6E77295E`). The plan derived from this amendment is the TODO `PLAN-HVPIPELINE-001`; the problems found while writing it are the TODO `REMED-HVPIPELINE-001`, which that plan pulls in (operator, 2026-10-16, verbatim: "keep track of problems to fix as a single todo item that will be pulled into the plan.").

Operator directive for this document (verbatim): "Create an amendment to the architecture document with the same level of rigor for diagrams, interfaces, AC, UC, and descriptive documentation."

How to read this amendment:

- It follows the foundation's conventions (foundation section 3) unless a clause below amends one. Its diagrams use the foundation's notation (foundation, "How to read the diagrams"): activity diagrams are Mermaid flowcharts read top to bottom, a double-bordered node (`[["..."]]`) is a call to a shared component, policy, or process drawn once where it is defined, every error exit ends in a terminal that names its code, and sequence diagrams list every response a caller can receive in `alt` blocks.
- A unit of work is exactly one CQRS command or query with its handler (operator, 2026-10-11: "I want to be clear, unit of work maps to one CQRS handler"). A policy is a rule a handler applies. A process is orchestration that calls handlers (the worker host, the reviewer run, plugin verbs, Director commands).
- Section numbers of this amendment are its own (1 to 22, then Addendum B). "Foundation section X" always names the foundation document.
- Every statement is marked by its origin: a settled operator requirement is quoted verbatim with its source (section 3); a design resolution is labelled R-n and states its rationale and the alternatives it rejects (section 5); an amended foundation clause is labelled A-n (section 6). Anything else is derived from those three and cites them.
- Requirement, test, and use-case identifiers in this document are proposed. The 63 requirement records of section 19 (FR, TR, and TEST `-MCP-HVPIPE-001` to `-021`) and their 21 mappings were created in the canonical store on 2026-10-16 between 16:32:30 and 16:37:30 local, status `pending`, so the requirements are captured as they emerge; the use cases are created in H0 (section 19).

## 1. Scope, relationship to the foundation, and precedence

- Scope: the durable, multi-agent hostile validation (HV) pipeline the foundation deferred (foundation section 3.1, "Model grade": "We are going to add an HV pipeline in the future, but not in this effort."). The pipeline admits HV requests over session-log turns, schedules them across a pool of five reviewer agents, runs each review as a durable, leased, live-streamed execution, keeps complete compressed evidence, and finalizes per-turn and holistic results in one transaction.
- Relationship: the foundation stays the authority for turn logging (Phase A), turn reviews (Phase B), and stateless agent context (Phase C). This amendment adds one bounded context, HostileValidation, and amends the foundation clauses listed in section 6, which are exactly the clauses that assume a manual HV with one configured grader.
- Precedence: where section 6 names a foundation clause as amended, the amendment's text governs; every other foundation clause stands unchanged. Because the foundation's plan (PLAN-SESSIONTURNREVIEW-001) was approved on 2026-10-10, the amended clauses are a change to that plan, and the foundation's own rule applies to them: "a change to this document after the plan is approved is a plan change that needs a new hostile validation" (foundation, introduction).
- Implementation status of the foundation (verified 2026-10-16 by a search of `src` at `36783b79`): of the foundation's new types only `IDispatcher` exists (`src/QBrainAi.Cqrs/IDispatcher.cs:9`); `SerializableWrite`, `IdempotentCreate`, `ReadableKeyAllocator`, `TurnLocator`, `ScoreLexeme`, `ScoreVerdict`, `ClassifiedErrorException`, `WorkspaceResolutionBehavior`, `RecordSessionTurnReviewCommand`, `CreateAgentContextHvRequestCommand`, and `RecordModelGradeCommand` are architecture only. Every amendment of section 6 is therefore a document change; no migration of an existing table or route is involved.
- Authorization: the operator authorized documentation and plan persistence for this work, not implementation. The standing code freeze applies (no new code until the operator approves the plans).

## 2. Goals and non-goals

Goals:

1. A durable HV queue whose admission, scheduling, ownership, and finalization survive server restarts, worker crashes, and concurrent workers without losing or duplicating a review result.
2. A reviewer pool of five agents (Cline, Copilot, Claude, Codex, Grok), selected round-robin among the ready reviewers, never the submitter, the agent that wrote the reviewed turns, or an alias of either.
3. Complete evidence: the complete accepted request and the complete response log of every review, compressed at rest with GZIP, verified lossless, retained until explicitly removed, and streamed live while the review runs.
4. Per-turn assessments and a separate holistic evaluation for every request, committed atomically with the turns' hostile reviews; reviewed turns and final results are immutable.
5. Independence: an agent's self-review never affects HV admission, execution, or verdict, and an HV verdict never completes a TODO.

Non-goals (each settled by the operator, section 3): access roles; a force re-review flag; automatic TODO state changes; automatic retention expiry; execution time limits; a second HV verdict store beside this one for turn reviews.

## 3. Settled requirements (verbatim sources)

Every requirement below was stated by the operator. Sources: the Cline session `/home/sharpninja/.cline/data/sessions/1792165626722_56f98/1792165626722_56f98.messages.json` (message index in brackets, every operator message read in full on 2026-10-16), the operator's messages in the Claude Code session that wrote this amendment, and the operator profile's standing HV rules. The handoff `docs/handoffs/hv-architecture-amendment-handoff-20261016T182551Z.md` (main checkout, untracked) summarized 22 of them; this register carries all of them.

Operator decisions of the HV planning conversation (2026-10-16):

- S-01 [24] "We need to plan a Hostile Validation queue and execution mechanism."
- S-02 [51] "HV queue needs configuration for available HV agents (cline, copilot, claude, codex, grok) and either the agent's default (highest effort) model, or a model/effort specified in configuration.  The HV agent configuration must have endpoints and REPL coverage so Director and other tools can manage in real time."
- S-03 [68] "When a HV request is received, it must submit a range of session log turns to include in the review.  The reviewer must grade each turn individualy and apply their review score and update the turn record.  The result will include the score for each turn, and an overall score for the whole HV request.  HV request/results will be store in the database."
- S-04 [76] "worktree session-20261009 should have updates to the session log schema for storing both agent self-review and hv review outcome on the turn." (Satisfied by foundation Phase B: `agentReview` and `hostileReview` on the turn.)
- S-05 [96] "That document is the base we are building on in this plan." (The foundation.)
- S-06 [104] "Review of a turn should be considered final as any subsequent changes should be reflected in a later turn."
- S-07 [112] "Session log may not allow updates to a turn with an HV review."
- S-08 [114] "A turn must have an agent review before it can be included in a HV." Superseded by S-28.
- S-09 [118] "Turns submitted, but marked Unnecessary, should still be included in the overall HV grade for the request."
- S-10 [120] "No, overall HV is a holistic review of the whole set of turns submitted." (Answer to a proposed mean-plus-floor overall score.)
- S-11 [122] "If the request recieves a session id (not a session turn id) in the set of turns, then the totality of session turns in that session are included by default."
- S-12 [124] "plugin core should present all missing agent reviews as a list for the agent to perform review on." (Scoped by S-28; resolution R-21.)
- S-13 [128] "HV reviews must use a standard json payload for responses that must include the complete request, the complete jsonl response log, and delineated list of specific failures with adequest description to remediate."
- S-14 [130] "HV request must supply a free text field for agent to add specific context in markdown or yaml format.  The request should be parsed for MCP Server FR/TR/AC/UC in the workspace and provided to the reviewing agent in a delineated list."
- S-15 [132] "execution failures are to be triaged (prevent duplicates) and mark the HV agent as offline until resolved."
- S-16 [134] "If there are no HV agents available to perform a review, the endpoint must refuse the request and the plugin core add the necessary context."
- S-17 [136] "HV agent resolution should round-robin HV review agents, excluding the agent submitting the review."
- S-18 [138] "need endpoints for getting running hvs, their last reported output, and eta.  Need endpoints for cancelling any hv.  need endpoint to attach to sse strem of the output of any running hv"
- S-19 [140] "need an HV notification stream endpoint to receive notifications of hv completion in the plugins"
- S-20 [144] (Force re-review flag?) "NO"
- S-21 [146] "Per turn should be a list of the requested turns to review with their score."
- S-22 [148] "Output should be captured live and streamed." Confirmed by the operator on 2026-10-16T14:14 local after the finding that develop has no true live streaming (problem P8, section 21): "Explains a lot.  Gotta be fixed in the plan."
- S-23 [150] "Single trx at end."
- S-24 [152] (Auto-start runners on submit or a manual trigger?) "Auto-start"
- S-25 [154] (AGREE auto-moves the TODO, or verdict only?) "Verdict only"
- S-26 [156] (Incident store?) "File through endpoint."
- S-27 [158] (Offline to online?) "NONCE the agent." and [229] "This is just a check that the agent is available and can execute an HV."
- S-28 [214] "Self-review of a turn is just a grade on the turn.  It is not relevant to the HV."
- S-29 [239] "Must participate in the audit log of the database." and [467] "Retain until told to remove.  There are no access roles.  Audit-failure is a hard stop."
- S-30 [474] "GZIP HV result logs to save space."
- S-31 [0] "create worktree from develop" and [463] "I've already specified both." (The implementation worktree is cut from `develop`; the foundation is the base.)
- S-32 [469] "That's your job." (Verifying task and requirement links and persisting the plan are agent work.) and [478] "write plan to high todo"
- S-33 [162] "apply byrd dev proces v4"

Standing operator rules that govern every HV (operator profile, sections 7.1 to 7.4):

- P-01 "HV must not be executed by the same agent family as the requesting agent.  If not specified by operator, ask what agent and model to use for HV in a session." (2026-10-13)
- P-02 "Do not bound review time" (2026-10-12): no timeout, kill, deadline, or wait limit on a reviewer run.
- P-03 Every HV response log carries progress entries with a numeric completion percentage and an estimated time remaining, emitted during the run (MEMORY-PREFERENCE-003); the estimate is the reviewer's own report and never a bound on review time.
- P-04 Verify the reviewer's model and effort from its own runtime receipts.
- P-05 98/98: a review passes only when accuracy (correctness) and completeness are both at least 0.98; the bar is never lowered.
- P-06 Every reviewer creates a complete session-log turn for its review and stores its entire result there.
- P-07 "Validate independently through a hostile reviewer wherever the plan calls for HV and before any done state. Self-check alone is not validation." (locked 2026-08-12)

Statements read and not carried as requirements: S-08 (superseded by S-28, the later answer to the same question) and the drafting agent's own proposals in its plan versions (messages 103, 161, 165, 213, 462), which are inputs to the resolutions of section 5, never settled requirements. Where a proposal conflicts with an operator statement, the operator statement governs: draft 161 allowed a later HV of an already reviewed turn ("new grade rows supersede"), which S-06, S-07, and S-20 forbid.

## 4. System context and component ownership

![System context and component ownership, part 1 of 13](c4/rendered/AM1-04-01.svg)

View: AM1-04-01

![System context and component ownership, part 2 of 13](c4/rendered/AM1-04-01-part2.svg)

View: AM1-04-01-part2

![System context and component ownership, part 3 of 13](c4/rendered/AM1-04-01-part3.svg)

View: AM1-04-01-part3

![System context and component ownership, part 4 of 13](c4/rendered/AM1-04-01-part4.svg)

View: AM1-04-01-part4

![System context and component ownership, part 5 of 13](c4/rendered/AM1-04-01-part5.svg)

View: AM1-04-01-part5

![System context and component ownership, part 6 of 13](c4/rendered/AM1-04-01-part6.svg)

View: AM1-04-01-part6

![System context and component ownership, part 7 of 13](c4/rendered/AM1-04-01-part7.svg)

View: AM1-04-01-part7

![System context and component ownership, part 8 of 13](c4/rendered/AM1-04-01-part8.svg)

View: AM1-04-01-part8

![System context and component ownership, part 9 of 13](c4/rendered/AM1-04-01-part9.svg)

View: AM1-04-01-part9

![System context and component ownership, part 10 of 13](c4/rendered/AM1-04-01-part10.svg)

View: AM1-04-01-part10

![System context and component ownership, part 11 of 13](c4/rendered/AM1-04-01-part11.svg)

View: AM1-04-01-part11

![System context and component ownership, part 12 of 13](c4/rendered/AM1-04-01-part12.svg)

View: AM1-04-01-part12

![System context and component ownership, part 13 of 13](c4/rendered/AM1-04-01-part13.svg)

View: AM1-04-01-part13

Ownership: the HostileValidation bounded context owns every unit, policy, entity, and process of this amendment. It calls the foundation's SessionTurnLogging (TurnLocator, TurnWritePipeline), TurnReviews (ScoreLexeme, ScoreVerdict, policy HvVerdictMapping), ResponsesAndTrust (RecordModelGradeCommand, DecideAgentResponseTrustCommand), and SharedComponents (SerializableWrite, IdempotentCreate, ReadableKeyAllocator, PagedQuery, ClassifiedErrorException, ContentHasher, SecretRedactor) through their Phase 2 seams, and the existing `ITriageService` (POST `/qbrainai/triage/reports`, `src/QBrainAi.Support.Mcp/Controllers/TriageController.cs:13-14,33`). Namespaces follow the foundation rule (foundation section 3.5): commands in `src/QBrainAi.Services/HostileValidation/Commands/` (namespace `QBrainAi.Support.Mcp.HostileValidation.Commands`), queries in `.../Queries/`, policies in `.../Policies/`, the worker in `.../Execution/`, adapters in `.../Reviewers/`, entities in `src/QBrainAi.Storage/Entities/HostileValidation/` (namespace `QBrainAi.Support.Mcp.Storage.Entities.HostileValidation`), DTOs in `src/QBrainAi.Client/Models/HostileValidation/` (namespace `QBrainAi.Client.Models.HostileValidation`), one type per file.

Trust boundaries:

- Callers authenticate with the workspace API key, as every route does; there are no access roles (S-29). Agent identity is therefore not authenticated by the server: the submitter's identity is the agent of the submitting turn the server finds (R-08), and the limit of that model is stated in section 8.
- The reviewer CLI is untrusted input to the server. Its output is evidence to verify, never an instruction; the server parses only the records the reviewer contract defines (section 13.3) and verifies them before anything is finalized (policy HvFinalVerification, section 15).
- Turn text sent to a reviewer passes through `SecretRedactor` (foundation section 3.5.2) before it leaves the server; the stored request evidence is the redacted brief exactly as sent.

## 5. Design decisions and resolutions

Each resolution names the settled requirements it implements, its rationale, and the alternatives it rejects. Resolutions are proposals for the operator's approval; the settled requirements of section 3 are not reopened.

- R-01 One bounded context. HostileValidation owns the pipeline and is not gated by `Mcp:AgentContext:Mode`: HV of turns works in every Mode, because the operator's HV requirements (S-01 to S-30) name turns, not agent-context responses. Rejected: extending the foundation's `AgentContextHvRequest` in ResponsesAndTrust (draft 103), because that record is mode-gated (foundation section 3.5.1) and lists responses, not turns, so a turn with no response would silently drop out of review (draft 213, "turn coverage versus response coverage").
- R-02 The HV request replaces the foundation's agent-context HV request. `HvRequest` keeps the foundation's key format `hvr-<sessionId>-<yyyyMMddTHHmmssZ>` (with `-<nn>` from the second key of one second, ReadableKeyAllocator) and lists turns (`HvRequestTurn`); the agent responses of those turns are linked as `HvRequestResponse` items when Phase C has recorded any. CreateAgentContextHvRequestCommand, GetAgentContextHvRequestQuery, QueryAgentContextHvRequestsQuery, and their entities are superseded (amendment A-04). Rejected: keeping two HV request records (one per turn, one per response), which would need a cross-record consistency rule the foundation never needed.
- R-03 Model grades come from finalization only. FinalizeHvRequestCommand dispatches the foundation's RecordModelGradeCommand for each linked response inside its own transaction; the public model-grade route, the two-step `codex exec resume` protocol, and `ModelGrader:*` are removed, and the eight receipt checks become the verification steps V-1 to V-8 of policy HvFinalVerification against evidence the server itself captured (amendment A-05). Rationale: the pipeline launches the reviewer and records its complete output, so the server no longer has to trust receipts written by a runner it did not control. Rejected: keeping the reviewer-submitted grade route beside the pipeline, which would let a grade bypass reviewer exclusion and atomic finalization (S-17, S-23).
- R-04 Freeze at admission, finality at finalization (S-06, S-07). Admission sets each member turn's HV state to `Pending` in the same transaction that creates the request; finalization sets it to `Final`. Every write to a `Pending` or `Final` turn or its children, from every surface and origin, is refused with 409 `turn_immutable` and `details.reason` `hv_pending` or `hv_final` (policy HvTurnFreeze), except the one write of finalization itself (origin `HvFinalization`). A request that ends `Canceled` or `Failed` releases its turns back to `None`. Rationale: the reviewed content equals the finalized content without a content comparison race, and the operator's "may not allow updates" is one guard in the existing write path. Rejected: letting writes continue and refusing finalization on a changed snapshot hash, which would waste completed reviews and still leave a window between the last check and the commit.
- R-05 Only terminal turns are admitted. A member turn that is not terminal (`TurnStatus.IsTerminal`, foundation section 3.5.3) refuses the whole request with 409 `hv_turns_not_terminal`, naming each turn. The submitting turn is never a member: a session scope excludes it and reports the exclusion in the result, and a turn or range scope that names it is refused with 400 `hv_scope_includes_submitting_turn` (the foundation's rule that "the request's own turn is never a candidate, a dependency, or cited evidence", foundation section 3.1). A Code or Docs turn that needs HV before it can complete is completed first with `hostileReview` `Other` (for example "HV requested hvr-..."), which the foundation already allows and never counts as an AGREE (foundation C8); finalization then replaces that `Other` with the HV result. Rationale: no turn changes status after it is frozen, so there is no close-inside-finalization path. Rejected: admitting in-progress turns with a declared close intent (draft 213, section 3.B), which adds a mechanism and still freezes a turn the model is working in.
- R-06 Hostile reviews are written only by the pipeline. On agent surfaces a `hostileReview` write may carry only `status` `Other` (with its required `otherNotes`); a write naming `Pass`, `Fail`, or `Running`, or carrying `hvVerdict`, is refused with 403 `review_hostile_pipeline_only` (amendment A-06). Process ConvertHvReceiptToReview is retired. Rationale: a manual Pass or Fail would bypass reviewer exclusion, complete evidence, and atomic finalization, and no surface may bypass them (draft 213, H5, adopted). Rejected: keeping the manual route for emergencies; a reviewer can be enabled and checked instead (S-27).
- R-07 Assignment at admission, durable round-robin. The submit transaction picks the next eligible reviewer after the workspace's round-robin cursor and advances the cursor in the same SerializableWrite (policy RoundRobinAssignment), so assignment survives restarts and two concurrent submissions never pick from the same cursor value. When the assigned reviewer is no longer eligible at claim time (offline, disabled), the claim reassigns through the same policy with its capacity filter, so a reviewer at its `MaxConcurrentRuns` is skipped for a later free one, and records the reassignment; when no eligible reviewer has capacity, nothing is written and the request waits. An assignee that is still eligible but at its `MaxConcurrentRuns` keeps its request, which waits for it while later requests are claimed. Rejected: assignment at claim only, which hides the assignee from the submitter and lets claim order, not round-robin order, pick reviewers.
- R-08 One agent identity, server-attributed. `AgentIdentity` (namespace `QBrainAi.Client.Agents`, shared by the server, the REPL, and, through the golden contract, the PowerShell core) maps every known alias to one of the five reviewer ids: Claude (`claude`, `claudecode`, `claude-code`, `claudecowork`, `claude-cowork`, `cowork`), Codex (`codex`), Copilot (`copilot`), Grok (`grok`, `grokcode`, `grok-code`, `grokbot`, `grok-bot`), Cline (`cline`, `cline-v2`, `clinev2`), case-insensitively and ignoring separators. The submitter is the agent of the submitting turn, located by its full turn key (TurnLocator); a free-text name never overrides it. The author is the agent of the reviewed session (one request names turns of one MCP session, R-26), stored normalized as `AuthorReviewerId`. A reviewer whose id equals the submitter's or the author's is never eligible (S-17, S-28, P-01, P-07). Develop holds three alias maps with different canonical forms (`src/QBrainAi.Repl.Host/MarkerFileClientOptionsResolver.cs:64-81`, `src/QBrainAi.Repl.Core/TodoSelectionStore.cs:166-171`, the plugin's `Get-ReplCanonicalAgentName`); they are context under the DRY ruling and are not changed by this amendment. Exclusion is by agent family, not by model vendor (D-01, settled): S-17 names "the agent submitting the review", P-01 constrains the agent family and asks separately for the agent and the model, and the operator runs Cline on `gpt-6-astra` (provider `openai-codex`, VI-01 receipt) while keeping Cline and Codex as two reviewers (S-02); a model a reviewer shares with the submitter stays visible in the run's provenance, and the operator can disable that reviewer (UpdateHvReviewerCommand). Exclusion covers the author as well as the submitter (D-05, settled): a review by the agent that wrote the turns is a self-review, which S-28 rules out of HV and P-07 rules out of validation.
- R-09 Operator submissions. A caller with no session-log turn (Director, an operator at the REPL) submits with `submitter.kind` `Operator` and a name; no reviewer is excluded for the submitter, and the author of the reviewed session is still excluded (R-08). Agent surfaces (plugin verbs, MCP tools, the REPL workflow verb) always send the submitting turn's key, which the plugin binds automatically (foundation process BindVerbToTurn). Limit (section 8): every caller holds the same workspace key, so the server cannot prove that an `Operator` submission did not come from an agent.
- R-10 Readiness through NONCE (S-15, S-27). A reviewer is `Unverified`, `Online`, or `Offline`, and separately enabled or disabled. "NONCE the agent" is a probe run of the reviewer through its adapter with a fresh random nonce: it passes when the reviewer answers with the probe record that echoes the nonce and the adapter's runtime provenance shows the reviewer's resolved model and effort (P-04). Only a passing NONCE moves a reviewer to `Online`. An execution failure moves it to `Offline` until a later NONCE passes. The worker host NONCEs at startup every enabled `Unverified` reviewer whose current configuration has no settled probe, and a configuration change requests a NONCE of the new configuration; an `Offline` reviewer, and an `Unverified` one whose probe of its current configuration is already settled (for example by a server fault), is NONCEd only when a caller asks (S-15: "until resolved"). Every probe ends through one policy, HvProbeOutcome (section 15), whichever unit ends it, and that policy first checks that the probe ran the reviewer's current configuration, so a probe of an older configuration never changes the reviewer. Rejected: a liveness ping or the server health nonce, which S-27 rules out.
- R-11 No execution time limit (P-02). A run has no timeout and no deadline. Liveness is the worker's lease, renewed by heartbeats; a stalled run stays visible (`lastOutputUtc`, the last progress record) and anyone can cancel it (S-18). The only expiry is the lease of a dead worker (R-13). This replaces the 30-minute `ExecutionTimeout` of the separate HostileReview options (`src/QBrainAi.Services/Options/HostileReviewWorkerOptions.cs:45-46`, read by nothing today, P1) for HV runs and for the HostileReview worker of section 16.7, whose artifact reviews have no time limit either (P-02). The process exit code is never the outcome of a run: the reviewer's records decide it (V-8 for a review, policy ReadinessProbeVerdict for a probe), because a CLI can end a refused or empty run with exit code 0 (VI-03: a headless Grok run whose write call was refused ended `cancelled` with exit 0, and two Copilot permission variants exited 0 with no output).
- R-12 True live output (S-22, P8). Each reviewer adapter delivers every output line as the process writes it, through a line callback, never after the process ends; the worker appends lines in batches (at most `OutputFlushInterval`, default 250 ms, or `OutputFlushBytes`, default 64 KiB, whichever comes first) through AppendHvRunOutputCommand, which stores each batch as one GZIP chunk and wakes the streams. The output stream replays stored chunks and then follows new ones (shared component OrderedEventReplay). Develop's one-shot strategy yields its output only after completion (`src/QBrainAi.Services/Services/OneShotCliAgentExecutionStrategy.cs:57-75`), so the adapters need a real line path (section 21, P8). Acceptance criterion FR-MCP-HVPIPE-008-AC003 makes the difference testable: a line is observable on the stream while the reviewer process is still running. The reviewer's records are read from each agent message's complete text, assembled from the CLI's native events (Grok's `streaming-json` sends message text one token per event, and Copilot sends deltas and then the whole message, as the probe runs of 2026-10-16 show), by one extraction function that the worker runs as lines arrive and finalization runs over the stored log (section 16.3), so a record split across events is read exactly once and both read the same records (FR-MCP-HVPIPE-010-AC006).
- R-13 Leases and fences (handoff requirement 10). A run is owned through a lease (owner, expiry) and a fence token that increases on every claim and takeover, the pattern develop already uses for handoff ingestion (owner and expiry with a `StateVersion` takeover, `src/QBrainAi.Services/Services/HandoffIngestionService.cs:724-735`, and a fenced terminal write that matches owner and version, `:782-807`). Every worker write carries the fence; a write with a stale fence changes nothing and returns `stale_fence`, and the worker that receives it stops its process. Expired leases are swept (ExpireHvRunLeasesCommand): the attempt becomes `Abandoned` and the request is queued again until `MaxAttemptsPerRequest` (default 3) is spent, then `Failed`, or `Canceled` when a cancel was requested; policy HvRetryDecision writes that outcome, with its turn release and its notification, for the sweep exactly as for FailHvRunCommand. Rejected: the unused lease options of the HostileReview subsystem (`HostileReviewWorkerOptions.cs:30-46`), which no code reads (section 21, P1).
- R-14 Evidence in the relational store, GZIP at rest (S-13, S-29, S-30). The accepted request is one GZIP blob written at admission; the response log is the run's ordered GZIP chunks; the result document is one GZIP blob written at finalization. No uncompressed copy of an evidence item is stored in the evidence tables, and each evidence item is stored there once (the structured rows of section 12, such as the request's `Context` and the findings, are records the units derive, not copies of the evidence blobs). The audit ledger keeps its own history of every evidence write, as it does for every audited entity: develop's ledger snapshots every property of an audited entity, binary columns included (`src/QBrainAi.Storage/McpDbContext.cs:1540-1589`), compressed with GZIP (`src/QBrainAi.Storage/Entities/DataAuditLogEntity.cs:119-147`), and a removal never changes it (R-20, I-13). Retrieval returns `application/gzip` (download name `<hvRequestId>-<kind>.jsonl.gz` or `.json.gz`) or, when asked, the decompressed bytes. The pattern is develop's audit ledger, which stores GZIP payloads in the database (`src/QBrainAi.Storage/Entities/DataAuditLogEntity.cs:66-147`). Rationale: S-23 ("Single trx at end") and S-29 (the database audit log) both hold only when the evidence is in the same transactional store as the result, and no crash window between a file system and the database remains. Rejected: `.jsonl.gz` files beside the database, which would need a two-store protocol, keep evidence outside the audit ledger, and split backups.
- R-15 Atomic finalization (S-23). FinalizeHvRequestCommand verifies the evidence and the reviewer output before it opens a transaction, then commits in one SerializableWrite: the result document, the per-turn assessments, the holistic evaluation, the failures, each turn's `hostileReview` through policy HvVerdictMapping (origin `HvFinalization`), each turn's `Final` HV state, the linked responses' model grades and trust decisions (when Mode is not `Off`), the request's `Completed` state, its notification, and the audit rows. The transaction re-checks everything the earlier reads relied on (the run's fence, the chunk manifest, the request and turn states). Crash windows are in section 9.
- R-16 Transactional notification outbox (S-19). Every state change that callers observe writes an `HvNotification` row in its own transaction, numbered by a per-workspace sequence that the same transaction advances, so the sequence has no gaps and no row is visible before its change commits. The notification stream replays rows after the caller's `Last-Event-ID` and then follows new ones (OrderedEventReplay). Delivery is at least once; consumers deduplicate by sequence. `IChangeEventBus` (develop's in-memory bus, `src/QBrainAi.Services/Notifications/IChangeEventBus.cs:4`) is used only to wake waiting streams and the worker, never as the record.
- R-17 Incidents through the triage endpoint (S-15, S-26). An execution failure writes an `HvIncident` row in the failure's transaction; FileHvIncidentCommand then submits it through `ITriageService` with its `dedupeKey` (`hv-reviewer-<reviewerId>-<failureCode>` for a failure attributable to the reviewer, `hv-server-<failureCode>` otherwise, section 12), which triage groups as `dedupe:<key>` (`src/QBrainAi.Services/Services/TriageService.cs:1475-1476`), and records the returned report and group ids. A filing that fails stays `Pending` and is retried by the worker host's sweep. Rejected: new incident tables with their own deduplication (S-26: "File through endpoint").
- R-18 Honest ETA (S-18, P-03). The ETA a caller sees is the reviewer's own latest progress record (`percentComplete`, `etaSeconds`), with `etaStatus` `Reported` and the record's time; with no progress record it is `etaStatus` `Unknown` and no number. The server never estimates.
- R-19 Audit participation and hard stop (S-29). Every HV entity carries `IsDeleted`, so `McpDbContext` writes its ledger rows for every HV mutation in the same `SaveChanges` (develop audits only entities with that property, `src/QBrainAi.Storage/McpDbContext.cs:1620-1627`). The ledger is append-only for HV: no HV unit or process updates or deletes a ledger row (I-13). Because develop skips the ledger when the audit table is unavailable (`McpDbContext.cs:1507-1538`), every HV unit first calls the shared component AuditLedgerGuard, which refuses with 503 `audit_unavailable` and writes nothing when the ledger cannot be written. Every evidence read, by a public surface or by the server itself, passes one boundary, AuditLedgerGuard `RecordRead`, which writes an explicit `read` ledger row and commits it on its own before any byte is returned or consumed, never inside the reader's write transaction, so no later rollback of the reader's own writes can erase the record of a read that happened; a read whose row cannot be written is refused (the "protected-log access" of draft 213 participates in the audit log, S-29). The public readers are GetHvEvidenceQuery, GetHvResultQuery, the output stream, and GetHvRequestQuery when it returns output lines, directly or for a same-key replay of SubmitHvRequestCommand; the internal readers are RunReviewerProcess (the accepted request it puts in the brief), FinalizeHvRequestCommand (the run's chunks it verifies), and CloseHvRunOutputCommand (a probe's chunks for policy ReadinessProbeVerdict); each internal reader reads before it opens its write transaction and re-checks in that transaction that the stored state is the one it read.
- R-20 Retention until removal (S-29). No unit deletes, expires, or compacts HV evidence except RemoveHvEvidenceCommand, an explicit call that removes the evidence bytes of one request, keeps its result rows, hashes, and sizes, records who removed it and why, and is audited. It clears the evidence tables only and never changes or deletes an audit ledger row: the ledger keeps the history of the evidence's writes and gains the record of the removal, because an audit log is never purged (I-13). There are no roles: any caller with the workspace key can call it, as S-29 requires.
- R-21 Self-review independence (S-28). No HV unit, policy, or process reads `agentReview`. The turn snapshot sent to a reviewer omits both review records (shared component TurnSnapshotBuilder), so a reviewer never sees a self-review grade. S-12 (a plugin list of missing agent reviews) is met by the foundation's Stop-gate remedy, which already lists each open turn's missing `agentReview` (foundation section 3.4, Stop gate, and `Get-CloseRemedy`, section 3.5.6), and is not an HV input. Reconciliation with the foundation's completion gate: HV admits only terminal turns (R-05), and the foundation's gate needs an `agentReview` status of `Pass`, `Fail`, or `Other` before a turn can close `completed` (foundation section 5.3, CompletionPolicy); that is a rule of closing a turn, not of HV, it is met by `Other` with a note as well as by a grade, and a turn closed `failed` or `canceled` needs no agent review at all; HV admission, execution, and verdict read none of it, and FR-MCP-HVPIPE-013 proves that every terminal turn is admitted and judged identically whatever its self-review.
- R-22 Verdict only (S-25). No HV unit writes TODO state. A result names the reviewed turns' `todoId` values for information only.
- R-23 Requirement references (S-14). The submitted context is stored as written (Markdown, YAML, or text, declared by `contextFormat`) and parsed for requirement references: FR, TR, and TEST ids of the form `<KIND>-<AREA>-<NNN>` with optional further dash-separated area segments, acceptance criteria written as `<FR id>-AC<NNN>` or `<FR id> ac-<n>`, and use cases by their store id or exact title. Each reference is resolved against the workspace's effective requirements and use cases and handed to the reviewer as a delineated list with its resolution status; an unresolved reference is listed as unresolved, never refused (shared component RequirementReferenceExtractor).
- R-24 The reviewer's own turn (P-06). The brief tells the reviewer to log its review as its own session-log turn, classified `Chore` so that its turn never needs an HV of its own (foundation completion policy: Chore needs no hostile review), and to store its entire result there. Finalization requires that turn to exist, to be persisted (not a failsafe or queued record), to have been created at or after the run's claim, to be closed `completed` as a `Chore`, and to contain every reviewer record of the run byte for byte in its stored normal form (V-6). A reviewer whose host cannot log turns fails its NONCE with `reviewer_turn_logging_unsupported`. The reviewer's plugin runs as a host session of its own: a review or probe run never reads or changes the plugin state or the session-log session of a live session of the same agent in the workspace (FR-MCP-HVPIPE-008-AC008), which matters because a reviewer starts in the workspace (section 16.2) and Grok's headless hooks write their host state under the working directory's `.mcpServer/grok/` (VI-04).
- R-25 The HostileReview subsystem stays separate. FR-MCP-HOSTILEREVIEW-001 to -006 (its queue, artifact links, findings, and query surface) are not changed by this amendment, as the foundation already states (foundation section 5.4); the problems found in it (section 21, P1 to P3) are tracked in REMED-HVPIPELINE-001. It stays separate (D-02, settled): it reviews artifacts (TODO, FR/TR/TEST, plans, files, commits; FR-MCP-HOSTILEREVIEW-002) while this pipeline reviews session turns (S-03), the approved foundation keeps it separate, and consolidating it is scope the operator has not named. Its missing worker (P1) has its own contract (section 16.7) and shares only the execution primitives that fit it (the reviewer adapters' launch builder, live-line path, containment, and provenance reader, and FencedLease), so reviewer execution exists once (the DRY ruling); nothing of this pipeline's evidence model applies to it, because FR-MCP-HOSTILEREVIEW-002 forbids storing a raw artifact or an assembled prompt.
- R-26 Scope limits. One request names turns of one MCP session. `MaxTurnsPerRequest` (default 200, the foundation's `HvRequest:MaxTurns` default) bounds every scope; a session scope above it is refused with 400 `hv_scope_too_large` naming the count, never truncated.
- R-27 One open review per turn. A turn is `Pending` in at most one open request (a unique filtered index on the turn key where the item is `Pending`); admission over a pending turn is refused with 409 `hv_turns_pending` naming the turns and their requests, and over a final turn with 409 `hv_turns_already_reviewed` (S-06, S-20).
- R-28 Cancellation by anyone (S-18). `Queued` becomes `Canceled` at once; `Running` becomes `CancelRequested`, and the worker stops the process at its next heartbeat and closes the run `Canceled`; a repeat on a `CancelRequested` or `Canceled` request returns 200 unchanged; `Finalizing`, `Completed`, and `Failed` refuse with 409 `hv_request_not_cancelable`. A cancel, a close, and a finalization of the same request are serialized on the request row, so exactly one of them commits first and the others see its state: a close that finds `CancelRequested` ends the run `Canceled` instead of making the request `Finalizing`, so an accepted cancellation is never lost (CloseHvRunOutputCommand).
- R-29 Worker units are internal. ClaimHvRunCommand, RecordHvRunStartCommand, RecordHvRunHeartbeatCommand, AppendHvRunOutputCommand, CloseHvRunOutputCommand, FailHvRunCommand, ExpireHvRunLeasesCommand, FinalizeHvRequestCommand, and FileHvIncidentCommand have no REST route, MCP tool, or client method; only the worker host dispatches them, in-process, through `IDispatcher`. The public mutations are submit, cancel, reviewer update, NONCE, and evidence removal.
- R-30 Plugins consume notifications through the REPL. Plugins never call REST (foundation section 3); the REPL gains a streaming verb that follows the notification stream, and the PowerShell core runs it in the background per host session (process WatchHvNotifications), keeping the last sequence and the inbox in one state file of the host session, so one atomic replace records both, and handing completions of requests that host session's MCP session submitted to the next prompt as context. A terminal notification carries the submitter's turn key (section 12), so the watcher matches it by that key and needs no local record of the submission, and a completion that arrives before the submit verb has returned is still delivered. Delivery to the model is at least once: a crash between adding a completion to a prompt and recording it delivered can repeat it, and each entry carries its sequence so a repeat is recognizable (the outbox rule of R-16).
- R-31 The 98/98 bar for the overall verdict (P-05), one predicate stated once here. The overall verdict is `AGREE` exactly when all of these hold: the holistic correctness and completeness both meet `max(configured hostile threshold, 0.98)` (ScoreVerdict, foundation section 3.5.4) and the holistic verdict is `AGREE`; every member turn's verdict is `AGREE`; every response grade of the accepted request is `AGREE` under the foundation's `verdict_match` rule; and no finding of any scope (turn, holistic, or response) is FAIL or UNKNOWN. Otherwise it is `DISAGREE`. The predicate reads only the reviewed inputs, which do not change: the response grades are those of every response admitted with the request (section 13.3), including one whose response has since left the review (its item `Closed`), because the reviewer graded what was admitted and emitted its verdict over it; a later state change of a response decides only whether its grade is applied (section 14.4), never the overall verdict. Per-turn hostile reviews keep the foundation's configured hostile thresholds through HvVerdictMapping. V-4 checks the reviewer's overall verdict against this predicate, and finalization stores the same value.

## 6. Amended foundation clauses

Each amendment names the foundation clause it replaces and states the replacement; nothing else in the named section changes.

- A-01 Foundation section 3.1, "Trust" and "Model grade", and section 6.1, fourth bullet: "model grades come only from the configured grader (default Codex, `gpt-6-astra`, `max`, the plan's HV reviewer)" and "this effort builds no grading worker or HV pipeline" are replaced by: model grades come only from FinalizeHvRequestCommand, for the reviewer the pipeline assigned to the request (R-03); the HV pipeline is this amendment.
- A-02 Foundation section 3 ("Process") and section 8.1: "a Codex gpt-6-astra max hostile validation" for every gate is replaced by: a hostile validation by the reviewer the operator names for the session, never of the requesting agent's family (P-01). The HV of a plan's own work does not use the pipeline that plan is building until that pipeline has passed its own end-to-end verification (section 20.3).
- A-03 Foundation section 6.2: `ModelGrader:AgentName`, `ModelGrader:RequiredModel`, `ModelGrader:RequiredEffort`, `ModelGrader:ReceiptRoot`, and `HvRequest:MaxTurns` are removed; the reviewer pool and `MaxTurnsPerRequest` of `HostileValidationOptions` (section 10) replace them. The startup validation of an empty grader identity and receipt root is removed with them.
- A-04 Foundation sections 6.3 (`AgentContextHvRequestEntity`, `AgentContextHvRequestItemEntity`), 6.4 (`CreateAgentContextHvRequestRequest`, `AgentContextHvRequestResult`, `AgentContextHvRequestQuery`), 6.5 (CreateAgentContextHvRequestCommand, GetAgentContextHvRequestQuery, QueryAgentContextHvRequestsQuery with their nested activities), 6.7 (their sequences), and Addendum A.9 are superseded by `HvRequest`, `HvRequestTurn`, `HvRequestResponse` (section 12), SubmitHvRequestCommand, GetHvRequestQuery, QueryHvRequestsQuery (section 14), and Addendum B. Every foundation reference to an HV request's `HvRequestId` (the grade's root id and foreign key, the finding's root id) now names `HvRequest`. ResponseStatusTransition's side effect ("the response's `Pending` items in `Open` HV requests become `Closed` with that reason, and each request left with no `Pending` item becomes `Completed`") becomes: the response's `Pending` `HvRequestResponse` items become `Closed` with that reason; the HV request's own state is decided by its turns and is never completed by a response transition.
- A-05 Foundation section 6.5, RecordModelGradeCommand: its whole contract (input, guards, checks, side effects, and diagrams, including the nested activities ReadModelGradeInputs, CheckGradeTarget, and SaveModelGrade) is replaced by the internal unit of section 14.5; its caller becomes FinalizeHvRequestCommand only (internal dispatch inside the finalization transaction, origin `HvFinalization`); the REST route `POST /qbrainai/agent-context/responses/{responseId}/grades/model`, the MCP tool `agent_context_record_model_grade`, the client and REPL methods, and `New-McpAgentContextRecordModelGradeParams` are removed; foundation section 6.7's "Sequence: submit RecordModelGradeCommand from an HV review" (the two-step `codex exec resume` runner) with its nested sequences SubmitOneModelGrade and ReadModelGradeInputs is replaced by the nested sequence FinalizeOneRequest of section 17.7, so no foundation sequence of a public grade submission, receipt paths, an `Open` HV request, or a grade that completes the request remains in force; 403 `agent_context_grader_not_allowed` is removed with the public route, and inside finalization a grader other than the run's reviewer is the 409 `conflict` with `retryable: true` of section 14.5 (unreachable after V-3 and V-5, and handled by FinalizeHvRequestCommand as a changed state); the eight receipt and reviewer checks of foundation section 6.3 are replaced by the verification steps V-1 to V-8 of policy HvFinalVerification (section 15), which finalization runs before it dispatches the grade; the receipt path, rollout, and thread columns of `AgentResponseGradeEntity` become the run reference `RunId` and the evidence hashes (section 12).
- A-06 Foundation section 3.1, "Review record" ("One record per kind per turn; latest write wins") and section 5.2, RecordSessionTurnReviewCommand: on agent surfaces a `hostileReview` write may carry only `status` `Other`; `Pass`, `Fail`, `Running`, and `hvVerdict` are refused with 403 `review_hostile_pipeline_only` (R-06); `Unnecessary` and `Unstarted` stay server-managed as before. Policy HvVerdictMapping is applied only by FinalizeHvRequestCommand. Section 5.4, process ConvertHvReceiptToReview, and its plugin function `ConvertFrom-McpHvReceipt` are retired, with the `hvReceiptPath` parameter of the plugin verbs.
- A-07 Foundation section 3.1, "Reviews remain writable on terminal turns", section 3.4 (section PUT and DELETE), and section 3.5.3, TurnWritePipeline: every turn write, review write, review reset, section replace or clear, dialog append, title change, and session-level replace or delete that would change a turn whose HV state is `Pending` or `Final`, or any of that turn's children, is refused with 409 `turn_immutable` (`details.reason` `hv_pending` or `hv_final`, `details.hvRequestId`), from every origin except `HvFinalization` (policy HvTurnFreeze, R-04). TurnWritePipeline runs that policy after TurnLocator and before every other policy.
- A-08 Foundation section 3.5.3, `WriteOrigin`: a fifth origin `HvFinalization` is added; it is exempt from the completion gate (it never moves a turn's status) and is the only origin that may write a turn whose HV state is `Pending`.
- A-09 Foundation section 6.3, check 7 (`reviewer_independent`, "not a different agent name, so an independently launched Codex reviewer grades a Codex-authored response"): replaced by V-5, which requires both a separate session and a reviewer id different from the submitter's and the author's (R-08, S-17, P-01, P-07).
- A-10 Foundation section 7.8 (UC-7, "Record a hostile validation review with receipts") is replaced by the use cases of section 18 (UC-HV-03 to UC-HV-05); foundation section 7.12 (UC "Submit and grade an agent response") keeps its flow, and its model grade now comes from UC-HV-05.
- A-11 Foundation section 5.1, `SessionLogTurnEntity`: two columns are added, `HvState` (16: `None`, `Pending`, `Final`; database default `None`) and `HvRequestId` (128, null while `None`), written only by the migration that adds them, which sets `HvState` `Final` (with `HvRequestId` null) on every turn whose hostile review is already `Pass` or `Fail`, because S-07 forbids updates to a turn with an HV review, and then by SubmitHvRequestCommand, CancelHvRequestCommand, CloseHvRunOutputCommand, FailHvRunCommand, and ExpireHvRunLeasesCommand (the last three through policy HvRetryDecision), and FinalizeHvRequestCommand, through policy HvTurnFreeze's state changes (Addendum B.8).
- A-12 Foundation section 6.1, "Existing records and stores", the FR-MCP-HOSTILEREVIEW bullet: unchanged in substance (R-25); its phrase "because this effort builds no grading worker" is replaced by "because turn HV runs through the HostileValidation pipeline of Amendment 1".

## 7. Lifecycles

### 7.1 HV request

![HV request lifecycle (state diagram), part 1 of 7](c4/rendered/AM1-07-01.svg)

View: AM1-07-01

![HV request lifecycle (state diagram), part 2 of 7](c4/rendered/AM1-07-01-part2.svg)

View: AM1-07-01-part2

![HV request lifecycle (state diagram), part 3 of 7](c4/rendered/AM1-07-01-part3.svg)

View: AM1-07-01-part3

![HV request lifecycle (state diagram), part 4 of 7](c4/rendered/AM1-07-01-part4.svg)

View: AM1-07-01-part4

![HV request lifecycle (state diagram), part 5 of 7](c4/rendered/AM1-07-01-part5.svg)

View: AM1-07-01-part5

![HV request lifecycle (state diagram), part 6 of 7](c4/rendered/AM1-07-01-part6.svg)

View: AM1-07-01-part6

![HV request lifecycle (state diagram), part 7 of 7](c4/rendered/AM1-07-01-part7.svg)

View: AM1-07-01-part7

- `Queued` and `Running` hold the turns `Pending`. `Completed`, `Failed`, and `Canceled` are terminal and never change again (S-06, S-20). A request whose attempt fails returns to `Queued` with its assignment re-evaluated (R-07); with no eligible reviewer it stays `Queued`, its ETA `Unknown`, until a reviewer passes a NONCE or the request is canceled, so no work is lost to an outage.
- `CancelRequested` is not terminal: the request is canceled when the worker has stopped the process, when its run closes or fails first (CloseHvRunOutputCommand and FailHvRunCommand through policy HvRetryDecision, R-28), or when the lease of a dead worker expires.

### 7.2 Run attempt

![Run attempt lifecycle (state diagram), part 1 of 8](c4/rendered/AM1-07-02.svg)

View: AM1-07-02

![Run attempt lifecycle (state diagram), part 2 of 8](c4/rendered/AM1-07-02-part2.svg)

View: AM1-07-02-part2

![Run attempt lifecycle (state diagram), part 3 of 8](c4/rendered/AM1-07-02-part3.svg)

View: AM1-07-02-part3

![Run attempt lifecycle (state diagram), part 4 of 8](c4/rendered/AM1-07-02-part4.svg)

View: AM1-07-02-part4

![Run attempt lifecycle (state diagram), part 5 of 8](c4/rendered/AM1-07-02-part5.svg)

View: AM1-07-02-part5

![Run attempt lifecycle (state diagram), part 6 of 8](c4/rendered/AM1-07-02-part6.svg)

View: AM1-07-02-part6

![Run attempt lifecycle (state diagram), part 7 of 8](c4/rendered/AM1-07-02-part7.svg)

View: AM1-07-02-part7

![Run attempt lifecycle (state diagram), part 8 of 8](c4/rendered/AM1-07-02-part8.svg)

View: AM1-07-02-part8

A run is either a `Review` run of an HV request or a `Probe` run of a reviewer's NONCE (R-10); both use the same lease, fence, output, and close units, so the probe needs no second execution path.

### 7.3 Reviewer readiness

![Reviewer readiness (state diagram), part 1 of 2](c4/rendered/AM1-07-03.svg)

View: AM1-07-03

![Reviewer readiness (state diagram), part 2 of 2](c4/rendered/AM1-07-03-part2.svg)

View: AM1-07-03-part2

`Enabled` is a separate flag set by UpdateHvReviewerCommand: a disabled reviewer is never eligible and is never NONCEd automatically, whatever its readiness. Eligibility (policy HvReviewerEligibility) is enabled, `Online`, and neither the submitter nor the author of the reviewed turns (R-08).

### 7.4 HV state of a turn

![HV state of a turn (state diagram), part 1 of 2](c4/rendered/AM1-07-04.svg)

View: AM1-07-04

![HV state of a turn (state diagram), part 2 of 2](c4/rendered/AM1-07-04-part2.svg)

View: AM1-07-04-part2

While `Pending` or `Final`, policy HvTurnFreeze refuses every write to the turn and its children except finalization's own (A-07, A-08). A turn that already holds a `Pass` or `Fail` hostile review when the column is added starts `Final`, with no request id (A-11).

## 8. Invariants and limits

Invariants, each owned by the named component and proved by the acceptance criteria of section 19:

- I-01 A turn is `Pending` in at most one open request, and a `Final` turn is never admitted again (R-27, HvAdmissionEligibility).
- I-02 The content a reviewer reviewed is the content finalized: no write reaches a `Pending` turn between admission and finalization (HvTurnFreeze), and finalization re-hashes each turn's snapshot and refuses on any difference (V-2).
- I-03 A request is finalized at most once; a second finalization returns the stored result and writes nothing (FinalizeHvRequestCommand).
- I-04 Nothing of a finalization is visible unless all of it is: result, per-turn assessments, holistic evaluation, failures, turn reviews, turn finality, grades, trust decisions, request state, notification, and audit rows commit together (R-15, S-23).
- I-05 Only the run that holds the current fence can append output, close output, or fail; a stale fence changes nothing (R-13).
- I-06 The response log of a finalized run is complete: its chunks are contiguous from sequence 1, each chunk's SHA-256 matches its decompressed bytes, the chain hash over the chunks' SHA-256 values equals the run's `OutputChainSha256` (section 12), every line parses as one JSON object, and the log contains exactly one `hv_verdict` record (V-1).
- I-07 The reviewer is never the submitter or the author of the reviewed turns, by canonical id (V-5, R-08).
- I-08 Varying a turn's `agentReview` (absent, any status, any score) changes neither admission, nor assignment, nor the brief's bytes, nor the verdict mapping (R-21).
- I-09 No HV unit changes TODO state (R-22).
- I-10 No HV write happens without its audit ledger rows (R-19).
- I-11 Evidence bytes are removed from the evidence tables only by RemoveHvEvidenceCommand (R-20).
- I-12 Every notification row is committed with the change it reports, and sequences are gap-free per workspace (R-16).
- I-13 The audit ledger is append-only: no HV unit or process updates or deletes a ledger row, and a removal of evidence leaves every earlier ledger row unchanged (R-19, R-20).

Limits stated plainly:

- L-01 Agent identity rests on the workspace key: every caller holds the same key, so the server cannot prove which agent sent a request. Exclusion is enforced against the agent of the submitting turn the server finds and the agent of the reviewed session; an `Operator` submission excludes only the author (R-09). This is the foundation's existing identity model (foundation section 6.3: "every agent of a workspace authenticates with the workspace key"), not a new weakness.
- L-02 A reviewer can run more than once for one request (at-least-once execution): a worker crash after the reviewer finished but before its output was closed leads to a new attempt. The request still finalizes at most once (I-03), and every attempt's output stays as evidence.
- L-03 Incident deduplication is per triage group: a filing retried after a crash between the triage commit and the recording of its report id can add a second report to the same group, never a second group (R-17).
- L-04 A reviewer CLI that cannot report its runtime model and effort cannot pass a NONCE, so it stays `Unverified` or `Offline` (R-10, P-04). Verified on 2026-10-16 (VI-01): Codex, Claude, Grok, and Copilot record both in their own run records; Cline records the model and only whether thinking was on, never the effort level, so Cline cannot pass a NONCE until it records the effort of a run (D-06, settled: P-04 fails closed).

## 9. Crash windows and recovery

Each window names what is durable at the crash and what recovers it. "Sweep" is the worker host's periodic pass (process HvWorkerHost).

- W-01 Crash after the submit commit, before the worker wakes: the request is `Queued` in the store; the next poll claims it.
- W-02 Crash after a claim, before RecordHvRunStartCommand commits (the process may or may not have started; a started one ends with the worker, W-03): the run's lease expires; the sweep marks the attempt `Abandoned` and the request `Queued`, or `Failed` or `Canceled` with its turns released when its attempts are spent or a cancel was requested (ExpireHvRunLeasesCommand, policy HvRetryDecision).
- W-03 Worker crash while the reviewer runs: the reviewer runs inside a containment bound to its worker process, so the reviewer and every descendant end when the worker dies, however abruptly (process RunReviewerProcess, FR-MCP-HVPIPE-007-AC008); no process is ever ended by a recorded process id, which the operating system can reuse. This is not a review deadline: only the worker's own death ends the run this way. The output already appended is durable; the lease expires and the sweep queues a new attempt. A worker that is alive but lost its lease (a pause longer than the lease) gets `stale_fence` on its next write and stops its process (I-05).
- W-04 Crash after the reviewer exits, before CloseHvRunOutputCommand commits: as W-03, a new attempt runs (L-02).
- W-05 Crash after CloseHvRunOutputCommand commits (request `Finalizing`), before finalization: the sweep dispatches FinalizeHvRequestCommand for every `Finalizing` request (idempotent, I-03).
- W-06 Crash or failure inside the finalization transaction: it rolls back; the request stays `Finalizing` and the sweep runs finalization again.
- W-07 Crash after the finalization commit, before any stream delivers its notification: the notification row is durable; streams and pollers replay it by sequence (R-16).
- W-08 Crash after an incident row commits, before triage filing: the incident stays `Pending`; the sweep files it (FileHvIncidentCommand).
- W-09 Crash after triage accepts a filing, before the report id is recorded: the retry files again under the same `dedupeKey`, which joins the same triage group (L-03).
- W-10 Cancel racing finalization, claim, or close: all of them change the request row inside SerializableWrite, so one commits first and the other sees the committed state (R-28).
- W-11 Two workers claiming one request: the claim is a conditional update on the request's state and version inside SerializableWrite; exactly one claim commits.
- W-12 The audit ledger becomes unavailable during a run: AppendHvRunOutputCommand refuses with 503 `audit_unavailable` and writes nothing; the worker stops the reviewer process (a hard stop, S-29), cannot record the failure while the ledger is down, and the lease expiry later queues a new attempt; nothing is ever reported complete without its audit rows.

## 10. Configuration: `HostileValidationOptions`

Section `Mcp:HostileValidation`, declared under the canonical root as `QBrainAi: HostileValidation:` in `appsettings.yaml` and projected onto `Mcp:` by `CanonicalSectionProjectionProvider`, as foundation section 6.2 declares `AgentContext`; class `HostileValidationOptions` in `src/QBrainAi.Services/HostileValidation/HostileValidationOptions.cs` with `SectionName = "Mcp:HostileValidation"`, `Validate()`, and `ValidateOnStart()`; per instance through `McpInstanceResolver`.

- `Reviewers` (list): exactly one entry for each of `Cline`, `Copilot`, `Claude`, `Codex`, and `Grok` (S-02); each entry has `Enabled` (default true), `ExecutionStrategy` (an existing strategy name, `src/QBrainAi.Services/Services/IAgentExecutionStrategy.cs:6-46`), `AgentPath` (the CLI to launch), `Model` (null means the CLI's default model, recorded from the runtime provenance; Copilot needs a configured model, because its default `auto` routes each prompt to a model of its own choosing, which two probe prompts of 2026-10-16 showed by landing on `mai-code-1.1-flash` and `gpt-6-luna`, and it refuses `--reasoning-effort` without an explicit model, so V-7 could not hold, VI-02), `Effort` (null means the highest effort the reviewer's CLI accepts, verified on 2026-10-16 (VI-08): Codex `max` (its rollout recorded `effort` `max`), Claude `max` (its transcript recorded `effort` `max`), Cline `xhigh` (the highest `--thinking` level), Grok `xhigh` (Grok 1.0.60 refuses `max`: "unknown effort level 'max'; use one of: xhigh, high, medium, low"), and Copilot `max` (the highest `--reasoning-effort` value, which the configured model's NONCE proves); develop's one-shot constants at `OneShotCliAgentExecutionStrategy.cs:23-26` differ for Codex (`xhigh`) and Grok (`max`, which Grok refuses), problem P9), and `MaxConcurrentRuns` (default 1, 1..8).
- `MaxTurnsPerRequest` (default 200, 1..2000).
- `MaxAttemptsPerRequest` (default 3, 1..10).
- `LeaseDuration` (default 5 minutes) and `HeartbeatInterval` (default 60 seconds; must be less than half of `LeaseDuration`).
- `OutputFlushInterval` (default 250 milliseconds, 50..5000) and `OutputFlushBytes` (default 65536, 1024..4194304); the default interval is kept only after its storage cost is measured (VI-05).
- `PollInterval` (default 2 seconds) and `WorkerEnabled` (default true).
- There is no execution timeout and no output size limit (R-11, S-22: "Output should be captured live and streamed").
- Startup validation fails on an unknown or duplicate reviewer id, a missing one of the five, an unknown execution strategy, an enabled Copilot entry without a configured `Model`, or a value outside its range.
- Seeding: at startup each reviewer entry is inserted as an `HvReviewer` row when the workspace has none for its id (readiness `Unverified`); an existing row is never overwritten, because UpdateHvReviewerCommand is the real-time management surface (S-02), the pattern develop uses for agent definitions seeded per workspace (`SeedWorkspaceAgentsAsync`, `src/QBrainAi.Services/Services/IAgentPoolService.cs:83`).

## 11. Shared components (new, each defined once)

The foundation's shared components (foundation section 3.5) are reused unchanged: SerializableWrite, IdempotentCreate with PayloadFingerprint, ReadableKeyAllocator, PagedQuery with PageBounds, ClassifiedErrorException with ErrorCodes, SecretRedactor, ContentHasher, TurnLocator, TurnWritePipeline, ScoreLexeme, ScoreVerdict, and WorkspaceResolutionBehavior. The components below are new; each has one implementation, one test class named for it, and one diagram here, and every unit that uses one draws it as a single call node.

### 11.1 FencedLease (namespace `QBrainAi.Support.Mcp.Storage.Leases`)

Claims, renews, and takes over the lease of one row, and guards every fenced write, following develop's handoff-ingestion pattern (`HandoffIngestionService.cs:724-735,782-807`). Operations: `Claim(row, owner, duration)` (only when the row has no lease or an expired one; the fence token increments), `Renew(row, owner, fence, duration)`, `Guard(row, owner, fence)` (the precondition of every fenced write), `Release(row, owner, fence)`, and `Expire(row, fence, liveStatuses)` (a sweep's takeover of a dead worker's row: in the caller's transaction the row must still be in one of the live statuses, with the fence the sweep read and an expired lease; otherwise the result is `skipped` and nothing changes, because the row was renewed, settled, or already taken over; when it holds, the fence token increments and owner and expiry are cleared, so the dead worker's next write is `stale_fence` and no second sweep can take the same row over again). Each runs inside the caller's SerializableWrite. Used by ClaimHvRunCommand, RecordHvRunStartCommand, RecordHvRunHeartbeatCommand, AppendHvRunOutputCommand, CloseHvRunOutputCommand, FailHvRunCommand, and ExpireHvRunLeasesCommand, and by the HostileReview worker's ClaimHostileReviewAttemptCommand, RenewHostileReviewAttemptCommand, CompleteHostileReviewAttemptCommand, and ExpireHostileReviewLeasesCommand (section 16.7).

![FencedLease (activity), part 1 of 6](c4/rendered/FencedLease.svg)

View: FencedLease

![FencedLease (activity), part 2 of 6](c4/rendered/FencedLease-part2.svg)

View: FencedLease-part2

![FencedLease (activity), part 3 of 6](c4/rendered/FencedLease-part3.svg)

View: FencedLease-part3

![FencedLease (activity), part 4 of 6](c4/rendered/FencedLease-part4.svg)

View: FencedLease-part4

![FencedLease (activity), part 5 of 6](c4/rendered/FencedLease-part5.svg)

View: FencedLease-part5

![FencedLease (activity), part 6 of 6](c4/rendered/FencedLease-part6.svg)

View: FencedLease-part6

### 11.2 GzipCodec (namespace `QBrainAi.Support.Mcp.Storage.Compression`)

`Compress(bytes)` returns one GZIP member; `Decompress(bytes)` returns the original bytes; `RoundTrip(original, compressed)` decompresses and compares length and SHA-256 (through ContentHasher's hex form over raw bytes) and fails with `evidence_corrupt` on any difference, so no compressed blob is stored unverified. It follows `DataAuditLogEntity`'s encode and decode (`DataAuditLogEntity.cs:119-147`); that entity's private copy is context, not work. Used by SubmitHvRequestCommand (request evidence), AppendHvRunOutputCommand (chunks), FinalizeHvRequestCommand (result document, chunk verification), RunReviewerProcess (the accepted request of the brief), CloseHvRunOutputCommand (a probe's chunks), and GetHvRequestQuery, GetHvResultQuery, GetHvEvidenceQuery, and OrderedEventReplay (public reads).

![GzipCodec (activity), part 1 of 4](c4/rendered/GzipCodec.svg)

View: GzipCodec

![GzipCodec (activity), part 2 of 4](c4/rendered/GzipCodec-part2.svg)

View: GzipCodec-part2

![GzipCodec (activity), part 3 of 4](c4/rendered/GzipCodec-part3.svg)

View: GzipCodec-part3

![GzipCodec (activity), part 4 of 4](c4/rendered/GzipCodec-part4.svg)

View: GzipCodec-part4

### 11.3 AuditLedgerGuard (namespace `QBrainAi.Support.Mcp.Storage.Auditing`)

`EnsureAvailable()` checks, inside the caller's transaction, that the `DataAuditLogs` table is writable and refuses with 503 `audit_unavailable` (`retryable: true`) when it is not, closing for HV the skip of `McpDbContext.AppendAuditRows` (`McpDbContext.cs:1507-1538`); `RecordRead(entityKind, entityKey, actor)` adds one ledger row with action `read` for an evidence access (R-19), always in a transaction of its own that commits before the caller consumes any byte, and never inside a caller's transaction: a caller that writes after a read calls it before it opens its own transaction, so the read row survives any rollback or failed commit of the caller. Every HV unit calls `EnsureAvailable()` before any other step that writes. `RecordRead` is the one boundary of every evidence read (R-19), called before any byte is returned or consumed: by GetHvEvidenceQuery, GetHvResultQuery, the output stream, and GetHvRequestQuery when it returns output lines (directly, or for a same-key replay of SubmitHvRequestCommand), and by RunReviewerProcess (the accepted request of the brief), FinalizeHvRequestCommand (the run's chunks), and CloseHvRunOutputCommand (a probe's chunks); each caller states what it does when the read is refused.

![AuditLedgerGuard (activity), part 1 of 2](c4/rendered/AuditLedgerGuard.svg)

View: AuditLedgerGuard

![AuditLedgerGuard (activity), part 2 of 2](c4/rendered/AuditLedgerGuard-part2.svg)

View: AuditLedgerGuard-part2

### 11.4 AgentIdentity (namespace `QBrainAi.Client.Agents`)

`Normalize(name)` returns the reviewer id (`Cline`, `Copilot`, `Claude`, `Codex`, `Grok`) of a known alias, case-insensitively and with separators removed, or `Unknown` with the input's PascalCase form; `IsSameReviewer(a, b)` compares normalized ids. The alias table is R-08's; the PowerShell core's copy is kept equal by the golden contract `docs/context/agent-identity-contract.json` (`AgentIdentityGoldenContractTests` in C# and Pester), the foundation's language-boundary rule (foundation section 3.5.4).

![AgentIdentity.Normalize (activity), part 1 of 2](c4/rendered/AgentIdentity.svg)

View: AgentIdentity

![AgentIdentity.Normalize (activity), part 2 of 2](c4/rendered/AgentIdentity-part2.svg)

View: AgentIdentity-part2

### 11.5 TurnSnapshotBuilder (namespace `QBrainAi.Support.Mcp.HostileValidation`)

Builds the canonical review snapshot of one turn: its turn key, status, `requestClass`, query title and text, interpretation, response, actions, design decisions, files modified, requirements discovered, blockers, tags, context list, and processing dialog, in stored order, with `agentReview` and `hostileReview` left out (R-21), every text through SecretRedactor, serialized as canonical JSON, and hashed through ContentHasher. Used by SubmitHvRequestCommand (the snapshot and its hash on each `HvRequestTurn`) and FinalizeHvRequestCommand (V-2 re-hash).

![TurnSnapshotBuilder (activity), part 1 of 2](c4/rendered/TurnSnapshotBuilder.svg)

View: TurnSnapshotBuilder

![TurnSnapshotBuilder (activity), part 2 of 2](c4/rendered/TurnSnapshotBuilder-part2.svg)

View: TurnSnapshotBuilder-part2

### 11.6 RequirementReferenceExtractor (namespace `QBrainAi.Support.Mcp.Requirements`)

Extracts the requirement and use-case references of R-23 from the submitted context, in order of first appearance and without duplicates, and resolves each against the workspace's effective requirements (`client.Requirements.GetEffectiveRequirementsAsync`, the marker's requirements visibility rule) and use cases (by store id or exact title). Returns each reference with `kind`, `id`, `resolved`, and, when resolved, its title and the SHA-256 of its effective record. Used by SubmitHvRequestCommand.

![RequirementReferenceExtractor (activity), part 1 of 2](c4/rendered/RequirementReferenceExtractor.svg)

View: RequirementReferenceExtractor

![RequirementReferenceExtractor (activity), part 2 of 2](c4/rendered/RequirementReferenceExtractor-part2.svg)

View: RequirementReferenceExtractor-part2

### 11.7 NotificationOutbox (namespace `QBrainAi.Support.Mcp.HostileValidation`)

`Append(kind, hvRequestId, reviewerId, runId, submitter, payload)` (`submitter` the request's submitting turn key, null where section 12 says so) runs inside the caller's SerializableWrite: it increments the workspace's `HvWorkspaceState.NextNotificationSequence`, inserts the `HvNotification` row with that sequence, and registers a post-commit wake-up on `IChangeEventBus` (R-16). Used by every unit whose change a caller observes.

![NotificationOutbox.Append (activity)](c4/rendered/NotificationOutbox.svg)

View: NotificationOutbox

### 11.8 OrderedEventReplay (namespace `QBrainAi.Support.Mcp.Streaming`)

Serves a Server-Sent Events stream from durable ordered rows: parses `Last-Event-ID` (absent means from the beginning of the requested stream, a malformed value is 400 `validation_error` with `details.field` `Last-Event-ID`), sends every stored event after it in order with its `id:`, then waits on the `IChangeEventBus` wake-up (or a poll interval) and sends new rows; it sends a comment line every 15 seconds while idle so proxies keep the connection, and ends when the caller disconnects or the stream's source reaches a terminal event. Develop's event stream has no replay and no event ids (`src/QBrainAi.Support.Mcp/Controllers/EventStreamController.cs:27-92`); this component adds both without changing that controller. Used by StreamHvRunOutputQuery and StreamHvNotificationsQuery.

![OrderedEventReplay (activity), part 1 of 4](c4/rendered/OrderedEventReplay.svg)

View: OrderedEventReplay

![OrderedEventReplay (activity), part 2 of 4](c4/rendered/OrderedEventReplay-part2.svg)

View: OrderedEventReplay-part2

![OrderedEventReplay (activity), part 3 of 4](c4/rendered/OrderedEventReplay-part3.svg)

View: OrderedEventReplay-part3

![OrderedEventReplay (activity), part 4 of 4](c4/rendered/OrderedEventReplay-part4.svg)

View: OrderedEventReplay-part4

## 12. Data model (namespace `QBrainAi.Support.Mcp.Storage.Entities.HostileValidation`)

Every entity is new, in `src/QBrainAi.Storage/Entities/HostileValidation/`, keyed by `WorkspaceId` first (every read is workspace-scoped and no read crosses workspaces, foundation section 3), soft-deleted through `IsDeleted` so that `McpDbContext` writes its audit ledger rows for it (R-19), and carries the root ids of every parent it hangs from (foundation section 3.1, "Root ids"). Lengths are the foundation's: an agent 64, a session id or request id 256, a readable key 128. Scores are `decimal` with precision 29 and scale 28, written and read as exact lexemes (ScoreLexeme). Every timestamp is UTC with offset, and the identifiers that carry a time use the UTC form `yyyyMMddTHHmmssZ` (D-08). Diagrams are in Addendum B.

`HvWorkspaceState` (table `HvWorkspaceStates`, key `WorkspaceId`): one row per workspace, created on first use. `LastAssignedReviewerId` (16, null before the first assignment; the round-robin cursor, R-07), `NextNotificationSequence` (long, starts at 1, R-16), `StateVersion` (int).

`HvReviewer` (table `HvReviewers`, key (`WorkspaceId`, `ReviewerId`)):

- `ReviewerId` (16): `Cline`, `Copilot`, `Claude`, `Codex`, or `Grok`.
- `Enabled` (bool), `ExecutionStrategy` (32), `AgentPath` (512), `ConfiguredModel` (128, null means the CLI default), `ConfiguredEffort` (32, null means the highest effort of section 10), `MaxConcurrentRuns` (int).
- `ResolvedModel` (128) and `ResolvedEffort` (32): the runtime model and effort the last passing NONCE recorded from the reviewer's own provenance (P-04); null until a NONCE passes.
- `Readiness` (16: `Unverified`, `Online`, `Offline`), `ReadinessReason` (64: the failure code that made it `Offline`, or the change that made it `Unverified`; null while `Online`), `ReadinessChangedUtc` (set by every write that changes `Readiness`), `LastProbeRunId` (128, set by ClaimHvRunCommand when it inserts a probe run), `OpenIncidentId` (128, the incident that made it `Offline`; null unless `Offline`, so a passing probe and a configuration change clear it while the incident row itself is kept).
- `ProbeRequestedUtc`: set when a NONCE is requested and cleared when ClaimHvRunCommand creates its probe run; a requested probe waits here, so a NONCE survives a restart before it is claimed.
- `ConfigurationVersion` (int, starts at 1): incremented by UpdateHvReviewerCommand whenever the strategy, agent path, model, or effort changes, so a probe qualifies only the configuration it ran (HvRun `ReviewerConfigurationVersion`).
- `StateVersion` (int: optimistic concurrency for UpdateHvReviewerCommand), `CreatedUtc`, `UpdatedUtc`.

`HvRequest` (table `HvRequests`, key (`WorkspaceId`, `HvRequestId`), `HvRequestId` = `hvr-<sessionId>-<yyyyMMddTHHmmssZ>`, `-<nn>` from the second key of one second, ReadableKeyAllocator):

- Scope: `ScopeKind` (16: `Turn`, `Range`, `Session`), `Agent` (64) and `SessionId` (256) of the reviewed MCP session, `FromRequestId` and `ToRequestId` (256; equal for `Turn`, null for `Session`), `ExcludedSubmittingTurn` (bool: a session scope left the submitting turn out, R-05).
- Submitter: `SubmitterKind` (16: `Agent`, `Operator`), the submitting turn key `SubmitterAgent` (64), `SubmitterSessionId` (256), `SubmitterRequestId` (256) (null for `Operator`), `SubmitterReviewerId` (16, AgentIdentity of `SubmitterAgent`, `Unknown` allowed), `OperatorName` (128, `Operator` only), `AuthorReviewerId` (16, AgentIdentity of the reviewed session's `Agent`, `Unknown` allowed; R-08).
- Context: `Context` (text, as submitted; read surfaces redact it through SecretRedactor), `ContextFormat` (16: `Markdown`, `Yaml`, `Text`).
- Idempotency: `IdempotencyKey` (128, unique with `WorkspaceId`), `PayloadSha256` (64, PayloadFingerprint of the client inputs: scope, submitter, context, context format).
- State: `Status` (16: `Queued`, `Running`, `CancelRequested`, `Finalizing`, `Completed`, `Failed`, `Canceled`), `AssignedReviewerId` (16), `AttemptCount` (int), `FailureCode` (64), `CancelRequestedUtc`, `CreatedUtc`, `StartedUtc`, `CompletedUtc`, `StateVersion` (int).
- Result (set only by FinalizeHvRequestCommand): `FinalizedRunId` (128), `HolisticCorrectness`, `HolisticCompleteness`, `HolisticVerdict` (16: `AGREE`, `DISAGREE`), `HolisticSummary` (text), `OverallVerdict` (16), `ThresholdCorrectness` and `ThresholdCompleteness` (the values in force, R-31), `ResultSha256` (64).
- Indexes: (`WorkspaceId`, `Status`, `CreatedUtc`) for the claim order and the running query; (`WorkspaceId`, `Agent`, `SessionId`).

`HvRequestTurn` (table `HvRequestTurns`, `long Id`): `WorkspaceId`, `HvRequestId` (FK (`WorkspaceId`, `HvRequestId`)), `Ordinal` (unique with the request; the turn's position in the session's turn order), the turn key `Agent`, `SessionId`, `RequestId` (root ids), `SnapshotSha256` (64, TurnSnapshotBuilder), `SnapshotBytes` (int), `RequestClassAtAdmission` (16), `HostileStatusAtAdmission` (16: shows an included `Unnecessary` turn, S-09), `Disposition` (16: `Pending`, `Reviewed`, `Released`). Unique filtered index (`WorkspaceId`, `Agent`, `SessionId`, `RequestId`) where `Disposition` is `Pending` (I-01).

`HvRequestRequirementRef` (table `HvRequestRequirementRefs`, `long Id`): `WorkspaceId`, `HvRequestId` (FK), `Ordinal` (unique with the request), `Kind` (8: `FR`, `TR`, `TEST`, `AC`, `UC`), `RefId` (256), `Resolved` (bool), `Title` (512), `RecordSha256` (64).

`HvRequestResponse` (table `HvRequestResponses`, `long Id`; replaces `AgentContextHvRequestItem`, A-04): `WorkspaceId`, `HvRequestId` (FK), `ResponseId` (128, FK (`WorkspaceId`, `ResponseId`) to `AgentResponses`), `PackageId` (128) and the response's turn key `Agent`, `SessionId`, `RequestId` (root ids; always a member turn), `Ordinal`, `Disposition` (16: `Pending`, `Graded`, `Closed`, `NotGraded`), `ClosedReason` (32: `rejected` or `superseded` when the response left the review through ResponseStatusTransition, `request_ended` when the request ended `Canceled` or `Failed`), `NotGradedReason` (32: `agent_context_disabled`), `GradeId` (128). Unique (`WorkspaceId`, `HvRequestId`, `ResponseId`); unique filtered (`WorkspaceId`, `ResponseId`) where `Disposition` is `Pending`.

`HvRun` (table `HvRuns`, key (`WorkspaceId`, `RunId`); `RunId` = `hvrun-<hvRequestId>-<nn>` for a review, `hvprobe-<reviewerId>-<yyyyMMddTHHmmssZ>` with `-<nn>` from the second key of one second for a probe):

- `Kind` (8: `Review`, `Probe`), `HvRequestId` (128, FK; null for `Probe`), `ReviewerId` (16), `ReviewerConfigurationVersion` (int, the reviewer's `ConfigurationVersion` at claim), `Attempt` (int), `ExpectedModel` (128), `ExpectedEffort` (32), `ProbeNonce` (64, `Probe` only).
- Ownership: `LeaseOwner` (256), `LeaseExpiresUtc`, `FenceToken` (long, FencedLease).
- `Status` (16: `Claimed`, `Running`, `OutputClosed`, `Succeeded`, `Failed`, `Abandoned`, `Canceled`), `ClaimedUtc` (set by ClaimHvRunCommand), `ProcessId` and `StartedUtc` (set by RecordHvRunStartCommand with the move to `Running`; information only, never used to end a process), `LastHeartbeatUtc`, `LastOutputUtc`, `EndedUtc`, `ExitCode` (set by CloseHvRunOutputCommand; a nonzero exit code is in the `FailureDetail` of `reviewer_exited_nonzero`), `FailureCode` (64), `FailureDetail` (text, sanitized).
- Output manifest: `OutputChunkCount` (int), `OutputLineCount` (long), `OutputBytes` (long, decompressed), `OutputChainSha256` (64: the chain hash after the last chunk, defined here once; for chunk n, SHA-256 over the previous chain value followed by chunk n's SHA-256, both lower-case hex, with the empty string before chunk 1).
- Progress (R-18): `ProgressPercent` (decimal), `ProgressEtaSeconds` (int), `ProgressReportedUtc`.
- Provenance: `RuntimeModel` (128), `RuntimeEffort` (32), and the reviewer's own turn `ReviewerAgent` (64), `ReviewerSessionId` (256), `ReviewerRequestId` (256), set by AppendHvRunOutputCommand from the `hv_reviewer_turn` record, and `VerdictRecordSha256` (64), set by FinalizeHvRequestCommand from the verified `hv_verdict` record.
- `StateVersion` (int). Unique filtered (`WorkspaceId`, `HvRequestId`) where `Status` is `Claimed`, `Running`, or `OutputClosed`: one live run per request.

`HvRunOutputChunk` (table `HvRunOutputChunks`, `long Id`): `WorkspaceId`, `RunId` (FK), `HvRequestId` and `ReviewerId` (root ids), `Sequence` (unique with `RunId`, from 1 without gaps), `Payload` (bytes: one GZIP member of the batch's UTF-8 JSONL lines; null after removal), `LineCount` (int), `ByteCount` (int, decompressed), `Sha256` (64, decompressed bytes), `ChainSha256` (64), `FenceToken` (long, the fence the append held), `CreatedUtc`, `RemovedUtc`.

`HvEvidence` (table `HvEvidence`, `long Id`): `WorkspaceId`, `HvRequestId` (FK), `Kind` (16: `Request`, the complete accepted request of section 13.3 written at admission; `Result`, the result document of section 13.4 without its embedded log and without its own `evidence.result` digest, written at finalization, so the stored payload never contains its own hash: its `Sha256` is the request's `ResultSha256`, which the returned document carries as `evidence.result.sha256`; unique with the request), `Payload` (bytes, one GZIP member; null after removal), `ByteCount`, `CompressedBytes`, `Sha256` (64, decompressed), `ContentType` (64), `CreatedUtc`, `RemovedUtc`, `RemovedBy` (128), `RemovalReason` (2048).

`HvTurnAssessment` (table `HvTurnAssessments`, `long Id`): `WorkspaceId`, `HvRequestId` (FK), `RunId` (128), `Ordinal` (the `HvRequestTurn` ordinal; unique with the request), the turn key (root ids), `Correctness`, `Completeness`, `Verdict` (16: `AGREE`, `DISAGREE`), `MappedReviewStatus` (16: the `hostileReview` status policy HvVerdictMapping stored: `Pass`, `Fail`, or `Other`), `Summary` (text).

`HvFinding` (table `HvFindings`, `long Id`): `WorkspaceId`, `HvRequestId` (FK), `RunId`, `Ordinal` (unique with the request), `Scope` (16: `Turn`, `Holistic`, `Response`), `TurnOrdinal` and the turn key (null for `Holistic`; for `Response`, the turn of the response), `ResponseId` (128, `Response` only), `Check` (64, `Response` only: the foundation check the finding answers), `Outcome` (16: `FAIL`, `UNKNOWN`), `Category` (32: `defect`, `risk`, `missing-tests`, `unclear-requirements`, `insufficient-disclosure`, `out-of-scope`, `uncertainty`, the categories of develop's hostile-review findings, `src/QBrainAi.Services/Services/HostileReviewService.cs:19-22`), `Location` (2048), `RequirementRef` (256), `ClaimId` (64), `Evidence` (text), `Remediation` (text, required: S-13 "adequest description to remediate").

`HvNotification` (table `HvNotifications`, key (`WorkspaceId`, `Sequence`)): `Kind` (32: `hv_queued`, `hv_started`, `hv_cancel_requested`, `hv_finalizing`, `hv_completed`, `hv_failed`, `hv_canceled`, `reviewer_online`, `reviewer_offline`, `reviewer_unverified`, `incident_filed`), `HvRequestId`, `ReviewerId`, `RunId`, the submitter's turn key `SubmitterAgent`, `SubmitterSessionId`, `SubmitterRequestId` (root ids of the request's submitting turn; null for an `Operator` submission and for reviewer and incident kinds), `PayloadJson` (text), `CreatedUtc`.

`HvIncident` (table `HvIncidents`, key (`WorkspaceId`, `IncidentId`), `IncidentId` = `hvi-<reviewerId>-<yyyyMMddTHHmmssZ>` with `-<nn>` from the second key of one second): `ReviewerId`, `RunId`, `HvRequestId` (null for a probe), `FailureCode` (64), `DedupeKey` (256: `hv-reviewer-<reviewerId>-<failureCode>` for a failure attributable to the reviewer, `hv-server-<failureCode>` otherwise; the attribution is FailHvRunCommand's), `Summary` (2048), `Evidence` (text, sanitized), `FilingStatus` (16: `Pending`, `Filed`), `FilingAttempts` (int), `LastFilingAttemptUtc` (the write-ahead record of the latest filing attempt), `LastFilingError` (2048), `TriageReportId` (128), `TriageGroupId` (128), `CreatedUtc`, `FiledUtc`.

Changed foundation entities (Addendum B.8):

- `SessionLogTurn` gains `HvState` (16: `None`, `Pending`, `Final`, database default `None`; a turn already reviewed `Pass` or `Fail` starts `Final`) and `HvRequestId` (128), A-11.
- `AgentResponseGrade`: `HvRequestId` becomes the composite foreign key (`WorkspaceId`, `HvRequestId`) to `HvRequests`; `RunId` (128) is added; `RequestJsonlPath`, `ResponseJsonlPath`, `RolloutJsonlPath`, and `ReviewerThreadId` are removed and `RequestJsonlSha256`, `ResponseJsonlSha256`, and `RolloutJsonlSha256` become `RequestEvidenceSha256` and `ResponseLogChainSha256` (A-05); `AgentContextHvRequests` and `AgentContextHvRequestItems` are not created (A-04).

Export and federation: every HV table gets an explicit wiki dump policy in `WikiDumpTablePolicyRegistry.PolicyFor`, `COPY-SANITIZE+HISTORY+INERT` (the HostileReview tables' policy) except `HvRunOutputChunks` and `HvEvidence`, which are `OMIT-LOCAL` because they hold the workspace's evidence bytes; `hostile_validation` joins the federation domains as a local-only domain, as the foundation registers `agent_context` (foundation section 6.1, FR-MCP-103), so HV writes are never queued during a hub outage and never applied from hub fanout.

## 13. Wire contracts

DTOs are defined once in `src/QBrainAi.Client/Models/HostileValidation/` (namespace `QBrainAi.Client.Models.HostileValidation`), registered in `McpClientJsonContext`, camelCase on the wire; scores travel as their exact lexemes (ScoreLexemeJsonConverter); unset fields are `null`.

### 13.1 Routes and surfaces

Every REST route exists under `/qbrainai/hostile-validation` and the alias `/mcpserver/hostile-validation` (controller `HostileValidationController`, `[ApiController]`, authenticated by the workspace key, workspace from `X-Workspace-Path`). The MCP tools live in `src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.HostileValidation.cs`, the client in `src/QBrainAi.Client/HostileValidationClient.cs` (property `HostileValidation` of `QBrainAiClient`), the REPL verbs in `src/QBrainAi.Repl.Core/HostileValidationWorkflow.cs` with `client.HostileValidation.*` passthrough, the Director commands in `src/QBrainAi.Cqrs.Mvvm/HostileValidationDirectorCommands.cs`, and the plugin verbs in `plugins/core/lib-ps/repl-invoke.ps1`. The REPL client mutation policy gives the `hostileValidation` namespace the classification the foundation gives `agentContext` (service-gated writes, reads on the read allowlist; foundation section 3.1, "Transaction-gated REPL clients").

- Reviewers: `GET /reviewers` (ListHvReviewersQuery; MCP `hv_reviewer_list`; client `ListReviewersAsync`; REPL `workflow.hv.reviewers`; Director `hv-reviewers`); `GET /reviewers/{reviewerId}` (GetHvReviewerQuery; `hv_reviewer_get`; `GetReviewerAsync`; `workflow.hv.reviewer`); `PATCH /reviewers/{reviewerId}` (UpdateHvReviewerCommand; `hv_reviewer_update`; `UpdateReviewerAsync`; `workflow.hv.updateReviewer`; Director `hv-reviewer-update`); `POST /reviewers/{reviewerId}/nonce` (StartHvReviewerNonceCommand; `hv_reviewer_nonce`; `StartNonceAsync`; `workflow.hv.nonce`; Director `hv-nonce`).
- Requests: `POST /requests` (SubmitHvRequestCommand; `hv_submit`; `SubmitAsync`; `workflow.hv.submit`, which the plugin binds to the submitting turn; Director `hv-submit` as `Operator`); `GET /requests/{hvRequestId}` (GetHvRequestQuery; `hv_get`; `GetAsync`; `workflow.hv.get`; Director `hv-request`); `GET /requests?status&agent&sessionId&reviewerId&submitterAgent&limit&offset` (QueryHvRequestsQuery; `hv_query`; `QueryAsync`; `workflow.hv.query`; Director `hv-requests`, which lists the running HVs with `status=Running`, S-18); `POST /requests/{hvRequestId}/cancel` (CancelHvRequestCommand; `hv_cancel`; `CancelAsync`; `workflow.hv.cancel`; Director `hv-cancel`).
- Results and evidence: `GET /requests/{hvRequestId}/result` (GetHvResultQuery; `hv_result`; `GetResultAsync`; `workflow.hv.result`; Director `hv-result`); `GET /requests/{hvRequestId}/evidence/{kind}` with `kind` `request`, `response-log`, or `result` (GetHvEvidenceQuery; `hv_evidence`, which returns the GZIP bytes base64-encoded with their SHA-256 and sizes; `GetEvidenceAsync` as a stream; `workflow.hv.evidence`, which writes a file; Director `hv-evidence`); `POST /requests/{hvRequestId}/evidence/remove` (RemoveHvEvidenceCommand; `hv_evidence_remove`; `RemoveEvidenceAsync`; `workflow.hv.removeEvidence`; Director `hv-evidence-remove`).
- Streams: `GET /requests/{hvRequestId}/output/stream` (StreamHvRunOutputQuery, SSE; client `StreamOutputAsync` as `IAsyncEnumerable`; REPL streaming verb `workflow.hv.watchOutput`; Director `hv-output`); `GET /notifications/stream` (StreamHvNotificationsQuery, SSE; `StreamNotificationsAsync`; `workflow.hv.watchNotifications`; Director `hv-notifications`); `GET /notifications?afterSequence&limit` (QueryHvNotificationsQuery; `hv_notifications`; `QueryNotificationsAsync`; `workflow.hv.notifications`). MCP has no stream tool; MCP callers poll `hv_notifications` and `hv_get`.
- Incidents: `GET /incidents?reviewerId&filingStatus&limit&offset` (QueryHvIncidentsQuery; `hv_incidents`; `QueryIncidentsAsync`; `workflow.hv.incidents`; Director `hv-incidents`).

Errors follow the foundation's rule (foundation section 3, "Errors"): every refusal is the `ClassifiedError` problem+json body through `McpErrorClassifier`, MCP tools return `{ code, error, message, retryable, details }`, and the REPL envelope is `type: error` with `code`, `message`, `retryable`, and `details`. Every unit's error list includes 400 `validation_error` with `details.field` `workspacePath` (WorkspaceResolutionBehavior), drawn after the dispatch in the sequences and not in the activity diagrams.

### 13.2 Requests and responses

- `HvReviewerDto { reviewerId, enabled, executionStrategy, agentPath, configuredModel, configuredEffort, resolvedModel, resolvedEffort, readiness, readinessReason, readinessChangedUtc, lastProbeRunId, openIncidentId, maxConcurrentRuns, activeRuns, stateVersion }`.
- `UpdateHvReviewerRequest { enabled, executionStrategy, agentPath, model, effort, maxConcurrentRuns, expectedStateVersion }`: every field but `expectedStateVersion` optional; `model` and `effort` accept an empty string to return to the default.
- `StartHvReviewerNonceResult { reviewerId, status, probeRequestedUtc, runId }` (202 Accepted): `status` `Requested` (a new probe is waiting for the worker), `AlreadyRequested`, or `Running` (with the probe's `runId`); the outcome arrives as a `reviewer_online` or `reviewer_offline` notification and on the reviewer.
- `SubmitHvRequestRequest { scope: { kind, agent, sessionId, requestId, fromRequestId, toRequestId }, submitter: { kind, agent, sessionId, requestId, operatorName }, context, contextFormat, idempotencyKey }`: `scope.kind` `Turn` needs `requestId`, `Range` needs `fromRequestId` and `toRequestId`, `Session` needs neither (S-03, S-11); `submitter.kind` `Agent` needs the full turn key, `Operator` needs `operatorName` (R-09); `context` is optional, at most 65536 characters; `contextFormat` defaults to `Markdown`.
- `HvRequestResult { hvRequestId, status, scope, submitter, assignedReviewerId, attemptCount, excludedSubmittingTurn, turns: [{ ordinal, agent, sessionId, requestId, snapshotSha256, requestClassAtAdmission, hostileStatusAtAdmission, disposition }], requirementRefs: [{ kind, refId, resolved, title }], responses: [{ responseId, agent, sessionId, requestId, disposition, closedReason, notGradedReason, gradeId }], currentRun: { runId, reviewerId, status, attempt, startedUtc, lastHeartbeatUtc, lastOutputUtc, progress: { percentComplete, etaSeconds, etaStatus, reportedUtc }, lastOutput: [string], outputRemoved }, overallVerdict, holisticVerdict, failureCode, createdUtc, startedUtc, completedUtc }`: `lastOutput` holds the last 20 output lines of the current run (S-18, "their last reported output"), and after evidence removal it is empty with `outputRemoved` true (R-20); `etaStatus` is `Reported` or `Unknown` (R-18); summaries in a page leave `turns`, `requirementRefs`, `responses`, and `lastOutput` null.
- `HvRequestQuery { status, agent, sessionId, reviewerId, submitterAgent, limit, offset }` returns `PageResult<HvRequestResult>` (PagedQuery, `PageBounds` `Refuse`, order `CreatedUtc` descending then `HvRequestId`).
- `CancelHvRequestResult { hvRequestId, previousStatus, status }`.
- `HvNotificationDto { sequence, kind, hvRequestId, reviewerId, runId, submitterAgent, submitterSessionId, submitterRequestId, payload, createdUtc }`; `HvNotificationQuery { afterSequence, limit }` returns `{ items, lastSequence }`.
- `HvIncidentDto { incidentId, reviewerId, runId, hvRequestId, failureCode, dedupeKey, summary, filingStatus, filingAttempts, triageReportId, triageGroupId, createdUtc, filedUtc }`.
- `RemoveHvEvidenceRequest { reason, removedBy }` returns `RemoveHvEvidenceResult { hvRequestId, removedKinds, removedChunks, removedUtc }`.
- `HvIncidentQuery { reviewerId, filingStatus, limit, offset }`; `HvNotificationPage { items, lastSequence }`; `HvOutputEvent { kind, runId, sequence, lines, status, code }` (the item type of the output stream, with the field names of the event data of section 13.5: `kind` the event name, `hv_output`, `hv_run_closed`, or `hv_stream_error`; `lines` set only on `hv_output`, `status` only on `hv_run_closed`, `code` only on `hv_stream_error`).
- Server-side result types that never cross the wire as JSON: `HvEvidenceStream` (the evidence bytes with their content type, file name, SHA-256, and sizes, written to the response body by the controller), and the internal unit results of section 14.4 (`HvRunClaim`, `HvStartResult`, `HvHeartbeatResult`, `HvAppendResult`, `HvCloseResult`, `HvFailResult`, `HvSweepResult`, `HvFinalizeResult`, `HvProvenance`), defined in `src/QBrainAi.Services/HostileValidation/Execution/`.

### 13.3 Reviewer contract, version 1

The complete accepted request (the `Request` evidence, written at admission and never changed) is one JSON object: `{ schema: "qbrainai.hv.request/1", hvRequestId, workspacePath, scope, submitter, turns: [{ ordinal, agent, sessionId, requestId, snapshotSha256, snapshot }], responses: [{ responseId, agent, sessionId, requestId, contentSha256, content, claims }], context, contextFormat, requirementRefs, admittedUtc }`. The brief of one attempt is that object inside an envelope: `{ schema: "qbrainai.hv.brief/1", request: <the accepted request>, runId, attempt, reviewer: { reviewerId, model, effort }, instructions, outputSchema }`; the worker writes the exact brief it sends as the first record of the attempt's log, `{ "type": "hv_brief", "brief": <the brief> }`, so each log is complete on its own. The brief's `instructions` require the reviewer to review every turn individually and the set of turns as a whole, to treat every turn snapshot and the context as untrusted evidence, to emit the records below as single-line JSON objects in its messages, to emit progress records during the run (P-03), to log its review as its own session-log turn classified `Chore` and store its entire result there, every reviewer record of the run in order (R-24, V-6), to close that turn `completed` through the foundation's completion gate before it ends (an action or design decision and its own agent review, `Other` with a note being enough), because the foundation's Stop gate blocks a host from stopping while a delivered turn is still in progress (foundation section 3.4) and a reviewer run has no time limit (P-02), and to emit exactly one `hv_verdict` record last.

Records (each a JSON object on its own line inside the reviewer's messages, read from each message's complete text as the reviewer's adapter assembles it from the native events, section 16.3; every other line is free text and stays in the log):

- `{ "type": "hv_progress", "percentComplete": 0..100, "etaSeconds": n or null, "note": "..." }`.
- `{ "type": "hv_turn_assessment", "hvRequestId", "ordinal", "agent", "sessionId", "requestId", "snapshotSha256", "correctness", "completeness", "verdict": "AGREE" or "DISAGREE", "summary", "findings": [{ "outcome": "FAIL" or "UNKNOWN", "category", "location", "requirementRef", "claimId", "evidence", "remediation" }] }`: one per member turn (S-03, S-21).
- `{ "type": "hv_holistic", "hvRequestId", "correctness", "completeness", "verdict", "summary", "crossTurnFindings": [same shape] }`: exactly one (S-10); it states cross-turn consistency, omissions, contradictions, and end-to-end satisfaction and is never a calculation over the turn scores.
- `{ "type": "hv_response_grade", "hvRequestId", "responseId", "contentSha256", "correctness", "completeness", "verdict", "findings": [{ "claimId", "check", "outcome": "PASS", "FAIL", or "UNKNOWN", "category", "location", "evidence", "remediation" }] }`: one per response of the accepted request, the set admitted with the request whatever has happened to its responses since (foundation `verdict_match` rules: every claim named at least once); `category`, `location`, `evidence`, and `remediation` are required on a FAIL or UNKNOWN finding and may be omitted on a PASS.
- `{ "type": "hv_reviewer_turn", "agent", "sessionId", "requestId" }`: the reviewer's own session-log turn (R-24).
- `{ "type": "hv_verdict", "hvRequestId", "runId", "overallVerdict", "turnCount", "responseGradeCount", "reviewer": { "reviewerId", "model", "effort" } }`: exactly one, last.
- `{ "type": "hv_probe", "nonce", "model", "effort" }`: the answer to a NONCE probe brief (R-10). A probe brief asks for this record and, as a review brief does, for the reviewer's own `Chore` turn and its `hv_reviewer_turn` record (R-24); policy ReadinessProbeVerdict reads both.

The response log is the reviewer process's complete output as JSONL: the worker's `hv_brief` record first; then, when the adapter runs the CLI in a JSON streaming mode, each native event line as written, and otherwise each output line wrapped as `{ "type": "hv_raw", "stream": "stdout" or "stderr", "line": "..." }`; and, after the process exits, the adapter's provenance lines `{ "type": "hv_provenance", "model", "effort", "source", "sourceSha256" }`, which name the runtime receipt they were read from and its hash (P-04). The adapter extracts the reviewer's records from the agent message text of the native events (section 16.3). `hv_brief`, `hv_raw`, and `hv_provenance` are written only by the worker as top-level log lines and are never accepted from the reviewer's message text, so a reviewer cannot forge them.

### 13.4 Standard result document (S-13)

`GET /requests/{hvRequestId}/result` returns `HvResultDocument`:

`{ schema: "qbrainai.hv.result/1", hvRequestId, status, scope, submitter, reviewer: { reviewerId, expectedModel, expectedEffort, runtimeModel, runtimeEffort, runId, attempt, reviewerAgent, reviewerSessionId, reviewerRequestId }, request: <the complete accepted request of section 13.3>, responseLog: [<every JSONL record of the finalized run, in order, starting with its hv_brief record>], turnAssessments: [{ ordinal, agent, sessionId, requestId, correctness, completeness, verdict, mappedReviewStatus, summary, findings }], holistic: { correctness, completeness, verdict, summary, crossTurnFindings }, failures: [{ ordinal, scope, turnOrdinal, agent, sessionId, requestId, responseId, check, outcome, category, location, requirementRef, claimId, evidence, remediation }], responseGrades: [{ responseId, disposition, notGradedReason, gradeId, verdict, correctness, completeness, findings }], overallVerdict, thresholds: { correctness, completeness }, evidence: { request: { sha256, bytes, compressedBytes }, responseLog: { chainSha256, bytes, lines, chunks }, result: { sha256 } }, todoIds: [string], finalizedUtc }`.

- `turnAssessments` lists every requested turn with its score (S-21); `failures` is the delineated list of every FAIL and UNKNOWN of every scope (`Turn`, `Holistic`, `Response`), each with its remediation (S-13); `responseGrades` lists every response of the accepted request with its grade record and findings, and its `disposition`, the response item's at commit: `Graded` (its model grade recorded, `gradeId` set), `Closed` (the response left the review before the commit, so its grade is applied to nothing), or `NotGraded` with `notGradedReason` `agent_context_disabled` (Mode `Off` at finalization); every FAIL and UNKNOWN finding of a response grade, whatever its disposition, is also an `HvFinding` row of scope `Response`, so it is kept with the result rows after evidence removal (R-20); `request` and `responseLog` are complete (S-13); `evidence.result.sha256` is `ResultSha256`, the digest of the stored `Result` payload, which excludes this field (section 12); `todoIds` is information only (R-22).
- With `Accept-Encoding: gzip` the document is sent compressed; the `responseLog` array is streamed from the chunks, so the server holds no uncompressed copy (R-14).
- Before finalization the route answers 409 `hv_result_not_ready` with the request's status; after evidence removal it answers 410 `hv_evidence_removed` with the hashes and sizes it still holds.

### 13.5 Streams

- Output stream: event `hv_output` with `id` `<runId>:<sequence>` and data `{ runId, sequence, lines: [string] }`, one event per stored chunk; event `hv_run_closed` with data `{ runId, status }` when the run ends; event `hv_stream_error` with data `{ runId, sequence, code }` when a stored chunk fails to decompress (`evidence_corrupt`), after which the stream ends; a new attempt starts a new run id in the same stream, so `Last-Event-ID` `<runId>:<sequence>` resumes after that chunk of that run. Per S-18 a caller attaches to "any running hv": the stream serves the request's runs in attempt order.
- Notification stream: event `<kind>` with `id` `<sequence>` and data `HvNotificationDto`; `Last-Event-ID` `<sequence>` resumes after it. The stream is workspace-scoped (S-19: notifications of HV completion in the plugins).

### 13.6 Error codes

New entries of the one `ErrorCodes` table (foundation section 3.5.2), each with its status and retryable flag:

- `audit_unavailable` 503, retryable (AuditLedgerGuard).
- `review_hostile_pipeline_only` 403 (A-06).
- `turn_immutable` 409 with `details.reason` `hv_pending` or `hv_final` and `details.hvRequestId` (the code exists in the foundation's typed failures, C9; A-07 adds the reasons).
- `hv_no_reviewer_available` 409 with `details.reviewers` (each reviewer's id, enabled flag, readiness, reason, and open incident) (S-16).
- `hv_scope_invalid` 400 (`details.field`: an unknown kind, a missing or extra scope field, an unknown session, an endpoint turn not in the session, or a range whose start is after its end).
- `hv_scope_too_large` 400 (`details.count`, `details.max`).
- `hv_scope_includes_submitting_turn` 400.
- `hv_scope_empty` 400 (a session scope with no turn left after the submitting turn's exclusion).
- `hv_turns_not_terminal`, `hv_turns_pending`, `hv_turns_already_reviewed` 409, each with `details.turns` (and `details.hvRequestIds` for the second and third).
- `hv_request_not_cancelable` 409 (`details.status`).
- `hv_request_active` 409 (`details.status`: evidence removal of a request that is not `Completed`, `Failed`, or `Canceled`).
- `hv_reviewer_disabled` 409 (a NONCE requested for a disabled reviewer).
- `hv_result_not_ready` 409 (`details.status`).
- `hv_evidence_not_available` 404 (a kind not produced yet) and `hv_evidence_removed` 410.
- `conflict` 409: `retryable: false` for an idempotency key reused with other inputs or a reviewer update whose `expectedStateVersion` is stale (`details.field` `expectedStateVersion`), `retryable: true` when SerializableWrite or ReadableKeyAllocator is exhausted.
- `not_found` 404 with `details.resource` `reviewer`, `hv_request`, or `run`.
- `evidence_corrupt` 500, public and stored: a GZIP round trip failed inside admission (SubmitHvRequestCommand) and nothing was saved, or a stored payload failed to decompress on a public read that decompresses it (GetHvRequestQuery's last output, directly or for a submission replay, GetHvResultQuery, GetHvEvidenceQuery with `Decompressed`; the output stream instead sends `hv_stream_error` with `code` `evidence_corrupt` and ends, section 13.5), after its read was recorded; on runs and incidents it is also the stored failure code of an internal read or write that failed (an append, the brief, a probe's chunks, finalization), always a server fault (FailHvRunCommand's attribution).
- Internal codes, stored on runs and incidents and never returned by a public route (`evidence_corrupt`, the one code that is both public and stored, is the entry above): `stale_fence`, `probe_stale_configuration`, `reviewer_launch_failed`, `reviewer_exited_nonzero`, `reviewer_output_invalid`, `reviewer_turn_unverified`, `reviewer_turn_logging_unsupported`, `runtime_identity_mismatch`, `verdict_binding_failed`, `reviewer_not_independent`, `snapshot_changed`, `model_grade_conflict`, `probe_nonce_mismatch`, `lease_expired`, `attempts_exhausted`, `canceled`, `worker_shutdown`.

## 14. Units of work (CQRS handlers)

Each unit is one command or query record and its handler, two files in the folders of section 4. WorkspaceResolutionBehavior runs after the dispatch and before every handler (foundation section 3.5.1); each unit lists its 400 `validation_error` with `details.field` `workspacePath` and does not draw it. Every command runs its checks and its save as one SerializableWrite delegate (foundation section 3.5.2) and calls AuditLedgerGuard first inside it (R-19), so each command lists 503 `audit_unavailable` and 409 `conflict` with `retryable: true` (attempts exhausted, nothing saved) and draws them once as the exits of its SerializableWrite call. Reviewer ids in routes and filters pass through AgentIdentity, so an alias addresses its reviewer.

### 14.1 Reviewer pool and readiness

#### ListHvReviewersQuery: list the reviewer pool [FR-MCP-HVPIPE-001, -003]

- Handler: `ListHvReviewersQuery(string WorkspacePath) : IQuery<IReadOnlyList<HvReviewerDto>>`.
- Surfaces: section 13.1. Response 200: the five reviewers in pool order (Cline, Copilot, Claude, Codex, Grok), each with `activeRuns` (its runs in `Claimed` or `Running`).
- Errors: 400 `validation_error` (`workspacePath`).

![ListHvReviewersQuery: list the reviewer pool (activity)](c4/rendered/ListHvReviewersQuery.svg)

View: ListHvReviewersQuery

#### GetHvReviewerQuery: get one reviewer [FR-MCP-HVPIPE-001, -003]

- Handler: `GetHvReviewerQuery(string WorkspacePath, string ReviewerId) : IQuery<HvReviewerDto>`.
- Response 200 `HvReviewerDto`. Errors: 400 `validation_error` (`workspacePath`); 404 `not_found` (`details.resource` `reviewer`) for an id AgentIdentity does not map to a reviewer.

![GetHvReviewerQuery: get one reviewer (activity), part 1 of 2](c4/rendered/GetHvReviewerQuery.svg)

View: GetHvReviewerQuery

![GetHvReviewerQuery: get one reviewer (activity), part 2 of 2](c4/rendered/GetHvReviewerQuery-part2.svg)

View: GetHvReviewerQuery-part2

#### UpdateHvReviewerCommand: manage a reviewer in real time [FR-MCP-HVPIPE-001]

- Handler: `UpdateHvReviewerCommand(string WorkspacePath, string ReviewerId, UpdateHvReviewerRequest Request) : ICommand<HvReviewerDto>` (S-02).
- Validation before the transaction: `expectedStateVersion` required; `executionStrategy`, when present, a supported strategy name; `maxConcurrentRuns` in 1..8; `model` at most 128 and `effort` at most 32 characters; any violation is 400 `validation_error` naming the field.
- Delegate: AuditLedgerGuard; the reviewer row (404 when absent); `StateVersion` equal to `expectedStateVersion`, otherwise 409 `conflict` with `retryable: false` and `details.field` `expectedStateVersion`; apply the fields; when the strategy, agent path, model, or effort changed, increment `ConfigurationVersion`, set `Readiness` `Unverified` with reason `configuration_changed` and `OpenIncidentId` cleared (the incident row stays), and append `reviewer_unverified` (NotificationOutbox); when the reviewer is enabled and `Unverified` after the change (a launch change, or enabling an `Unverified` reviewer), set `ProbeRequestedUtc`, a durable probe request that survives a restart; increment `StateVersion`. After the commit the worker is woken, and its next claim creates the probe run from that request (ClaimHvRunCommand), which is the NONCE of R-10.
- A disabled reviewer stays assigned to the requests it holds until their next claim, which reassigns them (R-07); a run already running continues.
- Response 200 `HvReviewerDto`. Errors: 400 `validation_error`; 404 `not_found` (`reviewer`); 409 `conflict` (`retryable: false` stale version, `retryable: true` exhausted); 503 `audit_unavailable`.

![UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 1 of 6](c4/rendered/UpdateHvReviewerCommand.svg)

View: UpdateHvReviewerCommand

![UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 2 of 6](c4/rendered/UpdateHvReviewerCommand-part2.svg)

View: UpdateHvReviewerCommand-part2

![UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 3 of 6](c4/rendered/UpdateHvReviewerCommand-part3.svg)

View: UpdateHvReviewerCommand-part3

![UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 4 of 6](c4/rendered/UpdateHvReviewerCommand-part4.svg)

View: UpdateHvReviewerCommand-part4

![UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 5 of 6](c4/rendered/UpdateHvReviewerCommand-part5.svg)

View: UpdateHvReviewerCommand-part5

![UpdateHvReviewerCommand: manage a reviewer in real time (activity), part 6 of 6](c4/rendered/UpdateHvReviewerCommand-part6.svg)

View: UpdateHvReviewerCommand-part6

#### StartHvReviewerNonceCommand: request a NONCE probe [FR-MCP-HVPIPE-003]

- Handler: `StartHvReviewerNonceCommand(string WorkspacePath, string ReviewerId) : ICommand<StartHvReviewerNonceResult>` (S-27, R-10). Any caller may request it (no roles, S-29).
- Delegate: AuditLedgerGuard; the reviewer row (404); a disabled reviewer is 409 `hv_reviewer_disabled`; a probe run of this reviewer in `Claimed` or `Running` returns 202 `Running` with its run id and writes nothing; a `ProbeRequestedUtc` already set returns 202 `AlreadyRequested` and writes nothing; otherwise `ProbeRequestedUtc` is set and the result is 202 `Requested`. After the commit the worker is woken; ClaimHvRunCommand creates the probe run.
- Errors: 400 `validation_error` (`workspacePath`); 404 `not_found` (`reviewer`); 409 `hv_reviewer_disabled`, `conflict` (`retryable: true`); 503 `audit_unavailable`.

![StartHvReviewerNonceCommand: request a NONCE probe (activity), part 1 of 4](c4/rendered/StartHvReviewerNonceCommand.svg)

View: StartHvReviewerNonceCommand

![StartHvReviewerNonceCommand: request a NONCE probe (activity), part 2 of 4](c4/rendered/StartHvReviewerNonceCommand-part2.svg)

View: StartHvReviewerNonceCommand-part2

![StartHvReviewerNonceCommand: request a NONCE probe (activity), part 3 of 4](c4/rendered/StartHvReviewerNonceCommand-part3.svg)

View: StartHvReviewerNonceCommand-part3

![StartHvReviewerNonceCommand: request a NONCE probe (activity), part 4 of 4](c4/rendered/StartHvReviewerNonceCommand-part4.svg)

View: StartHvReviewerNonceCommand-part4

### 14.2 Admission, queries, and cancellation

#### SubmitHvRequestCommand: admit an HV request [FR-MCP-HVPIPE-002, -004, -005, -006, -007, -011, -013, -018]

- Handler: `SubmitHvRequestCommand(string WorkspacePath, SubmitHvRequestRequest Request) : ICommand<HvRequestResult>`.
- Callers: any agent or operator; the plugin verb `workflow.hv.submit` binds the submitting turn automatically (process SubmitHvFromPlugin, section 16.4).
- Order of checks:
  1. Shape (no store read): the scope fields for its kind (400 `hv_scope_invalid` with `details.field`), the submitter fields for its kind (400 `validation_error` with `details.field` under `submitter`), a nonblank `idempotencyKey` of at most 128 characters, `contextFormat` known and `context` at most 65536 characters (400 `validation_error`).
  2. Reads before the transaction (foundation section 3.1, "Database-only delegates"): RequirementReferenceExtractor over `context`.
  3. SerializableWrite delegate: AuditLedgerGuard; IdempotentCreate on `IdempotencyKey` (policy `Conflict`, fingerprint of the client inputs): the same inputs end the delegate with nothing written and the unit answers the replay with GetHvRequestQuery for the stored request, dispatched after the delegate, so its output read is recorded on its own first (R-19) and that query's answers are the replay's; other inputs are 409 `conflict` with `retryable: false`; TurnLocator on the submitting turn for `submitter.kind` `Agent` (`InvalidIdentifier`, `SessionNotFound`, or `TurnNotFound` is 400 `validation_error` with `details.field` `submitter`); policy HvScopeResolution; policy HvAdmissionEligibility; policy RoundRobinAssignment; then the inserts.
  4. Inserts in the delegate: the `hvr-` key (ReadableKeyAllocator); `HvRequest` `Queued` with `AssignedReviewerId` and `AuthorReviewerId`; one `HvRequestTurn` per member, each with its TurnSnapshotBuilder snapshot hash, class, and hostile status at admission (S-09); the requirement references; one `HvRequestResponse` `Pending` per agent response of a member turn whose status is `Submitted`, `Grading`, `Trusted`, or `Stale` and that is not `Pending` in another open request (when Phase C has recorded responses); the `Request` evidence through GzipCodec (section 13.3); policy HvTurnFreeze `Freeze` on every member turn (`HvState` `Pending`, `HvRequestId`); the round-robin cursor advance; NotificationOutbox `hv_queued`.
  5. After the commit the worker is woken (S-24, auto-start).
- Response 201 `HvRequestResult` with Location `/qbrainai/hostile-validation/requests/{hvRequestId}` (a new request has no run, so it returns no output line and records no read); a repeat with the same key and inputs is 200 with the stored request and its current state exactly as GetHvRequestQuery returns it, its last output included with its audited read.
- Concurrency: two submissions over overlapping turns are serialized by SerializableWrite; the second sees the first's `Pending` items and is refused with `hv_turns_pending` (I-01; the unique filtered index ends a racing insert, and the retried attempt finds the winner). Two submissions over disjoint turns take consecutive cursor values, so round-robin order holds under concurrency.
- Errors, in order: 400 `hv_scope_invalid`, `validation_error` (submitter, key, context, `workspacePath`), `hv_scope_too_large`, `hv_scope_includes_submitting_turn`, `hv_scope_empty`; 409 `conflict` (`retryable: false` key reused, `retryable: true` exhausted), `hv_turns_not_terminal`, `hv_turns_pending`, `hv_turns_already_reviewed`, `hv_no_reviewer_available` (with `details.reviewers`); 500 `evidence_corrupt` (the accepted request failed its GZIP round trip, nothing saved; or, on a replay, a stored output chunk failed to decompress, as GetHvRequestQuery answers); 503 `audit_unavailable` (also a replay whose output read cannot be recorded).

![SubmitHvRequestCommand: admit an HV request (activity), part 1 of 7](c4/rendered/SubmitHvRequestCommand.svg)

View: SubmitHvRequestCommand

![SubmitHvRequestCommand: admit an HV request (activity), part 2 of 7](c4/rendered/SubmitHvRequestCommand-part2.svg)

View: SubmitHvRequestCommand-part2

![SubmitHvRequestCommand: admit an HV request (activity), part 3 of 7](c4/rendered/SubmitHvRequestCommand-part3.svg)

View: SubmitHvRequestCommand-part3

![SubmitHvRequestCommand: admit an HV request (activity), part 4 of 7](c4/rendered/SubmitHvRequestCommand-part4.svg)

View: SubmitHvRequestCommand-part4

![SubmitHvRequestCommand: admit an HV request (activity), part 5 of 7](c4/rendered/SubmitHvRequestCommand-part5.svg)

View: SubmitHvRequestCommand-part5

![SubmitHvRequestCommand: admit an HV request (activity), part 6 of 7](c4/rendered/SubmitHvRequestCommand-part6.svg)

View: SubmitHvRequestCommand-part6

![SubmitHvRequestCommand: admit an HV request (activity), part 7 of 7](c4/rendered/SubmitHvRequestCommand-part7.svg)

View: SubmitHvRequestCommand-part7

##### Nested activity: AdmitRequestChecks (the checks of SubmitHvRequestCommand, inside its delegate)

![Nested activity: AdmitRequestChecks (the checks of SubmitHvRequestCommand, inside its delegate) (activity), part 1 of 3](c4/rendered/AdmitRequestChecks.svg)

View: AdmitRequestChecks

![Nested activity: AdmitRequestChecks (the checks of SubmitHvRequestCommand, inside its delegate) (activity), part 2 of 3](c4/rendered/AdmitRequestChecks-part2.svg)

View: AdmitRequestChecks-part2

![Nested activity: AdmitRequestChecks (the checks of SubmitHvRequestCommand, inside its delegate) (activity), part 3 of 3](c4/rendered/AdmitRequestChecks-part3.svg)

View: AdmitRequestChecks-part3

##### Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate)

![Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate) (activity), part 1 of 4](c4/rendered/InsertAdmittedRequest.svg)

View: InsertAdmittedRequest

![Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate) (activity), part 2 of 4](c4/rendered/InsertAdmittedRequest-part2.svg)

View: InsertAdmittedRequest-part2

![Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate) (activity), part 3 of 4](c4/rendered/InsertAdmittedRequest-part3.svg)

View: InsertAdmittedRequest-part3

![Nested activity: InsertAdmittedRequest (the inserts of SubmitHvRequestCommand, inside its delegate) (activity), part 4 of 4](c4/rendered/InsertAdmittedRequest-part4.svg)

View: InsertAdmittedRequest-part4

#### GetHvRequestQuery: get an HV request with its progress [FR-MCP-HVPIPE-015, -016]

- Handler: `GetHvRequestQuery(string WorkspacePath, string HvRequestId) : IQuery<HvRequestResult>`.
- Loads the request, its turns, references, responses, and current run with its progress (R-18) and its last 20 output lines (decompressed from the last chunks through GzipCodec). Because the last output is evidence, the unit records the access through AuditLedgerGuard `RecordRead` before it decompresses any chunk, whenever it returns output lines (R-19). After evidence removal it returns no output line, `outputRemoved` true, and records no read (R-20). A chunk that fails to decompress is 500 `evidence_corrupt`, after the read was recorded.
- Response 200 `HvRequestResult`. Errors: 400 `validation_error` (`workspacePath`); 404 `not_found` (`hv_request`); 500 `evidence_corrupt`; 503 `audit_unavailable` (output lines present and the ledger unwritable).

![GetHvRequestQuery: get an HV request with its progress (activity), part 1 of 4](c4/rendered/GetHvRequestQuery.svg)

View: GetHvRequestQuery

![GetHvRequestQuery: get an HV request with its progress (activity), part 2 of 4](c4/rendered/GetHvRequestQuery-part2.svg)

View: GetHvRequestQuery-part2

![GetHvRequestQuery: get an HV request with its progress (activity), part 3 of 4](c4/rendered/GetHvRequestQuery-part3.svg)

View: GetHvRequestQuery-part3

![GetHvRequestQuery: get an HV request with its progress (activity), part 4 of 4](c4/rendered/GetHvRequestQuery-part4.svg)

View: GetHvRequestQuery-part4

#### QueryHvRequestsQuery: list HV requests, running ones included [FR-MCP-HVPIPE-015]

- Handler: `QueryHvRequestsQuery(string WorkspacePath, HvRequestQuery Query) : IQuery<PageResult<HvRequestResult>>`.
- PagedQuery with `PageBounds` `Refuse` (limit 1..1000, default 50); filters combine with AND; order `CreatedUtc` descending, then `HvRequestId`. Summaries carry no output lines, so no read is recorded.
- Errors: 400 `validation_error` (`workspacePath`, `limit`, `offset`, an unknown `status`, an unknown `reviewerId`).

![QueryHvRequestsQuery: list HV requests, running ones included (activity), part 1 of 2](c4/rendered/QueryHvRequestsQuery.svg)

View: QueryHvRequestsQuery

![QueryHvRequestsQuery: list HV requests, running ones included (activity), part 2 of 2](c4/rendered/QueryHvRequestsQuery-part2.svg)

View: QueryHvRequestsQuery-part2

#### CancelHvRequestCommand: cancel any HV [FR-MCP-HVPIPE-008]

- Handler: `CancelHvRequestCommand(string WorkspacePath, string HvRequestId) : ICommand<CancelHvRequestResult>` (S-18, R-28). Any caller (no roles).
- Delegate: AuditLedgerGuard; the request (404); by status: `Queued` becomes `Canceled` with `CompletedUtc`, policy HvTurnFreeze `Release` returns its turns to `None`, its `Pending` responses become `Closed` (`request_ended`), and `hv_canceled` is appended; `Running` becomes `CancelRequested` with `CancelRequestedUtc` and `hv_cancel_requested` is appended (the worker stops the process at its next heartbeat, section 16.2); `CancelRequested` and `Canceled` return 200 unchanged; `Finalizing`, `Completed`, and `Failed` are 409 `hv_request_not_cancelable`.
- Response 200 `CancelHvRequestResult`. Errors: 400 `validation_error` (`workspacePath`); 404 `not_found` (`hv_request`); 409 `hv_request_not_cancelable`, `conflict` (`retryable: true`); 503 `audit_unavailable`.

![CancelHvRequestCommand: cancel any HV (activity), part 1 of 6](c4/rendered/CancelHvRequestCommand.svg)

View: CancelHvRequestCommand

![CancelHvRequestCommand: cancel any HV (activity), part 2 of 6](c4/rendered/CancelHvRequestCommand-part2.svg)

View: CancelHvRequestCommand-part2

![CancelHvRequestCommand: cancel any HV (activity), part 3 of 6](c4/rendered/CancelHvRequestCommand-part3.svg)

View: CancelHvRequestCommand-part3

![CancelHvRequestCommand: cancel any HV (activity), part 4 of 6](c4/rendered/CancelHvRequestCommand-part4.svg)

View: CancelHvRequestCommand-part4

![CancelHvRequestCommand: cancel any HV (activity), part 5 of 6](c4/rendered/CancelHvRequestCommand-part5.svg)

View: CancelHvRequestCommand-part5

![CancelHvRequestCommand: cancel any HV (activity), part 6 of 6](c4/rendered/CancelHvRequestCommand-part6.svg)

View: CancelHvRequestCommand-part6

### 14.3 Results, evidence, streams, and incidents

#### GetHvResultQuery: the standard result document [FR-MCP-HVPIPE-010, -015]

- Handler: `GetHvResultQuery(string WorkspacePath, string HvRequestId) : IQuery<HvResultDocument>` (S-13, section 13.4).
- Order: the request (404); `Completed` required, otherwise 409 `hv_result_not_ready` with the status; evidence removed is 410 `hv_evidence_removed` with the hashes and sizes it keeps; AuditLedgerGuard `RecordRead`; then the document is assembled from the `Result` evidence and the `Request` evidence (GzipCodec) and the finalized run's chunks, streamed in order. A payload that fails to decompress before the first byte is sent is 500 `evidence_corrupt`; a chunk that fails after the document has started ends the response with an incomplete body, which the caller detects against `evidence.responseLog.chainSha256`.
- Errors: 400 `validation_error` (`workspacePath`); 404 `not_found` (`hv_request`); 409 `hv_result_not_ready`; 410 `hv_evidence_removed`; 500 `evidence_corrupt`; 503 `audit_unavailable`.

![GetHvResultQuery: the standard result document (activity), part 1 of 4](c4/rendered/GetHvResultQuery.svg)

View: GetHvResultQuery

![GetHvResultQuery: the standard result document (activity), part 2 of 4](c4/rendered/GetHvResultQuery-part2.svg)

View: GetHvResultQuery-part2

![GetHvResultQuery: the standard result document (activity), part 3 of 4](c4/rendered/GetHvResultQuery-part3.svg)

View: GetHvResultQuery-part3

![GetHvResultQuery: the standard result document (activity), part 4 of 4](c4/rendered/GetHvResultQuery-part4.svg)

View: GetHvResultQuery-part4

#### GetHvEvidenceQuery: download evidence [FR-MCP-HVPIPE-011, -015]

- Handler: `GetHvEvidenceQuery(string WorkspacePath, string HvRequestId, string Kind, string? RunId, bool Decompressed) : IQuery<HvEvidenceStream>`.
- `Kind` `request` (from admission), `response-log` (the finalized run, or `runId` for any run of the request, including a running one, so a caller can fetch output before finalization), or `result` (only when `Completed`). The default answer is `application/gzip`: the stored GZIP member for `request` and `result`, and for `response-log` the run's chunks served as consecutive GZIP members, which is a valid GZIP stream (RFC 1952, section 2.2); with `Decompressed` the bytes are decompressed on the way (`application/x-ndjson` or `application/json`). Nothing uncompressed is stored (R-14). AuditLedgerGuard `RecordRead` runs before the first byte. With `Decompressed`, a payload that fails to decompress before the first byte is sent is 500 `evidence_corrupt`, and one that fails later ends the body incomplete (its stored SHA-256 shows it); the compressed answer serves the stored bytes as they are, with their stored SHA-256 and sizes.
- Errors: 400 `validation_error` (`workspacePath`, an unknown `kind`); 404 `not_found` (`hv_request`; `run` for a `runId` that does not exist or belongs to another request), `hv_evidence_not_available` (a kind not produced yet); 410 `hv_evidence_removed`; 500 `evidence_corrupt` (`Decompressed` only); 503 `audit_unavailable`.

![GetHvEvidenceQuery: download evidence (activity), part 1 of 5](c4/rendered/GetHvEvidenceQuery.svg)

View: GetHvEvidenceQuery

![GetHvEvidenceQuery: download evidence (activity), part 2 of 5](c4/rendered/GetHvEvidenceQuery-part2.svg)

View: GetHvEvidenceQuery-part2

![GetHvEvidenceQuery: download evidence (activity), part 3 of 5](c4/rendered/GetHvEvidenceQuery-part3.svg)

View: GetHvEvidenceQuery-part3

![GetHvEvidenceQuery: download evidence (activity), part 4 of 5](c4/rendered/GetHvEvidenceQuery-part4.svg)

View: GetHvEvidenceQuery-part4

![GetHvEvidenceQuery: download evidence (activity), part 5 of 5](c4/rendered/GetHvEvidenceQuery-part5.svg)

View: GetHvEvidenceQuery-part5

#### RemoveHvEvidenceCommand: remove evidence on instruction [FR-MCP-HVPIPE-019]

- Handler: `RemoveHvEvidenceCommand(string WorkspacePath, string HvRequestId, RemoveHvEvidenceRequest Request) : ICommand<RemoveHvEvidenceResult>` (S-29, R-20). Any caller (no roles); `reason` required (1..2048 characters) and recorded with the caller's `removedBy` name (required, 128).
- Delegate: AuditLedgerGuard; the request (404); a request not `Completed`, `Failed`, or `Canceled` is 409 `hv_request_active`; every `HvEvidence` payload is set to null with `RemovedUtc`, `RemovedBy`, and `RemovalReason`, and every `HvRunOutputChunk` payload of the request's runs is set to null with `RemovedUtc` (the who and why stay on the evidence rows, section 12); hashes, sizes, counts, result rows, findings, and turn reviews stay; each change writes its own ledger rows, and no earlier ledger row is changed or deleted (I-13). A repeat finds nothing left and returns 200 with empty `removedKinds`.
- Errors: 400 `validation_error` (`workspacePath`, `reason`, `removedBy`); 404 `not_found` (`hv_request`); 409 `hv_request_active`, `conflict` (`retryable: true`); 503 `audit_unavailable`.

![RemoveHvEvidenceCommand: remove evidence on instruction (activity), part 1 of 4](c4/rendered/RemoveHvEvidenceCommand.svg)

View: RemoveHvEvidenceCommand

![RemoveHvEvidenceCommand: remove evidence on instruction (activity), part 2 of 4](c4/rendered/RemoveHvEvidenceCommand-part2.svg)

View: RemoveHvEvidenceCommand-part2

![RemoveHvEvidenceCommand: remove evidence on instruction (activity), part 3 of 4](c4/rendered/RemoveHvEvidenceCommand-part3.svg)

View: RemoveHvEvidenceCommand-part3

![RemoveHvEvidenceCommand: remove evidence on instruction (activity), part 4 of 4](c4/rendered/RemoveHvEvidenceCommand-part4.svg)

View: RemoveHvEvidenceCommand-part4

#### StreamHvRunOutputQuery: attach to the live output of any HV [FR-MCP-HVPIPE-016]

- Handler: `StreamHvRunOutputQuery(string WorkspacePath, string HvRequestId, string? LastEventId) : IQuery<IAsyncEnumerable<HvOutputEvent>>`, served by the controller as `text/event-stream` (S-18, S-22).
- Order: the request (404); evidence removed is 410 `hv_evidence_removed` (R-20); AuditLedgerGuard `RecordRead` once per connection; then OrderedEventReplay over the request's runs in attempt order and each run's chunks in sequence order (section 13.5), ending when the request is terminal and every chunk has been sent; a chunk that fails to decompress is sent as `hv_stream_error` with `evidence_corrupt`, and the stream ends.
- Errors (before the stream starts): 400 `validation_error` (`workspacePath`, `Last-Event-ID`); 404 `not_found` (`hv_request`); 410 `hv_evidence_removed`; 503 `audit_unavailable`.

![StreamHvRunOutputQuery: attach to the live output of any HV (activity), part 1 of 3](c4/rendered/StreamHvRunOutputQuery.svg)

View: StreamHvRunOutputQuery

![StreamHvRunOutputQuery: attach to the live output of any HV (activity), part 2 of 3](c4/rendered/StreamHvRunOutputQuery-part2.svg)

View: StreamHvRunOutputQuery-part2

![StreamHvRunOutputQuery: attach to the live output of any HV (activity), part 3 of 3](c4/rendered/StreamHvRunOutputQuery-part3.svg)

View: StreamHvRunOutputQuery-part3

#### StreamHvNotificationsQuery: the HV notification stream [FR-MCP-HVPIPE-017]

- Handler: `StreamHvNotificationsQuery(string WorkspacePath, string? LastEventId) : IQuery<IAsyncEnumerable<HvNotificationDto>>`, served as `text/event-stream` (S-19, R-16). Notifications carry ids and verdicts, not evidence, so no read row is recorded.
- Errors (before the stream starts): 400 `validation_error` (`workspacePath`, `Last-Event-ID`).

![StreamHvNotificationsQuery: the HV notification stream (activity)](c4/rendered/StreamHvNotificationsQuery.svg)

View: StreamHvNotificationsQuery

#### QueryHvNotificationsQuery: poll notifications [FR-MCP-HVPIPE-017]

- Handler: `QueryHvNotificationsQuery(string WorkspacePath, HvNotificationQuery Query) : IQuery<HvNotificationPage>`: rows with `Sequence` greater than `afterSequence` (at least 0), at most `limit` (1..1000, default 100), in sequence order, with the workspace's `lastSequence`.
- Errors: 400 `validation_error` (`workspacePath`, `afterSequence`, `limit`).

![QueryHvNotificationsQuery: poll notifications (activity), part 1 of 2](c4/rendered/QueryHvNotificationsQuery.svg)

View: QueryHvNotificationsQuery

![QueryHvNotificationsQuery: poll notifications (activity), part 2 of 2](c4/rendered/QueryHvNotificationsQuery-part2.svg)

View: QueryHvNotificationsQuery-part2

#### QueryHvIncidentsQuery: list incidents [FR-MCP-HVPIPE-009]

- Handler: `QueryHvIncidentsQuery(string WorkspacePath, HvIncidentQuery Query) : IQuery<PageResult<HvIncidentDto>>`: PagedQuery with `PageBounds` `Refuse`, filters `reviewerId` and `filingStatus` combined with AND, order `CreatedUtc` descending then `IncidentId`.
- Errors: 400 `validation_error` (`workspacePath`, `limit`, `offset`, an unknown `reviewerId` or `filingStatus`).

![QueryHvIncidentsQuery: list incidents (activity), part 1 of 2](c4/rendered/QueryHvIncidentsQuery.svg)

View: QueryHvIncidentsQuery

![QueryHvIncidentsQuery: list incidents (activity), part 2 of 2](c4/rendered/QueryHvIncidentsQuery-part2.svg)

View: QueryHvIncidentsQuery-part2

### 14.4 Execution (internal units, dispatched only by the worker host)

Every unit here is internal (R-29): its caller is process HvWorkerHost (section 16.1) or another unit of this section, never a route, tool, or client. Each returns a typed result to its caller; the codes in its list are those results, and none reaches a public surface.

#### ClaimHvRunCommand: claim the next probe or review [FR-MCP-HVPIPE-003, -007]

- Handler: `ClaimHvRunCommand(string WorkspacePath, string WorkerId) : ICommand<HvRunClaim?>`; `HvRunClaim { runId, kind, reviewerId, attempt, fenceToken, leaseExpiresUtc, hvRequestId, expectedModel, expectedEffort, probeNonce }`.
- Delegate: AuditLedgerGuard; then, in this order: (1) a probe: the first enabled reviewer, in pool order, with `ProbeRequestedUtc` set and fewer active runs than its `MaxConcurrentRuns`: insert its `Probe` run (`hvprobe-` key, a fresh 32-byte random `ProbeNonce` in lower-case hex), FencedLease `Claim`, clear `ProbeRequestedUtc`, and set the reviewer's `LastProbeRunId`; (2) otherwise a review: the oldest `Queued` request (by `CreatedUtc`, then `HvRequestId`) whose assignee is eligible with capacity, or whose assignee is no longer eligible and for which policy RoundRobinAssignment, with its capacity filter, finds an eligible reviewer with capacity (the reassignment and the cursor are recorded; with none it writes nothing and the next `Queued` request is considered); a request whose assignee is still eligible but at its `MaxConcurrentRuns` is not reassigned and waits for its assignee, and the next `Queued` request is considered; insert its `Review` run (`hvrun-<hvRequestId>-<nn>`, `Attempt` = `AttemptCount` + 1), FencedLease `Claim`, set the request `Running` (with `StartedUtc` on the first attempt) and `AttemptCount` + 1, and append `hv_started`. Every run records, at claim, `ClaimedUtc` and the reviewer's `ConfigurationVersion`, `ExpectedModel`, and `ExpectedEffort`. With nothing claimable it returns null and writes nothing; a request whose turns are all frozen stays `Queued` while no reviewer is eligible (ETA `Unknown`, R-18).
- Concurrency: the request's state, version, and the one-live-run index are checked and written in the delegate, so two workers never claim one request (W-11).
- Results: a claim; null; 409 `conflict` (`retryable: true`, the worker tries again at its next poll); 503 `audit_unavailable`.

![ClaimHvRunCommand: claim the next probe or review (activity), part 1 of 11](c4/rendered/ClaimHvRunCommand.svg)

View: ClaimHvRunCommand

![ClaimHvRunCommand: claim the next probe or review (activity), part 2 of 11](c4/rendered/ClaimHvRunCommand-part2.svg)

View: ClaimHvRunCommand-part2

![ClaimHvRunCommand: claim the next probe or review (activity), part 3 of 11](c4/rendered/ClaimHvRunCommand-part3.svg)

View: ClaimHvRunCommand-part3

![ClaimHvRunCommand: claim the next probe or review (activity), part 4 of 11](c4/rendered/ClaimHvRunCommand-part4.svg)

View: ClaimHvRunCommand-part4

![ClaimHvRunCommand: claim the next probe or review (activity), part 5 of 11](c4/rendered/ClaimHvRunCommand-part5.svg)

View: ClaimHvRunCommand-part5

![ClaimHvRunCommand: claim the next probe or review (activity), part 6 of 11](c4/rendered/ClaimHvRunCommand-part6.svg)

View: ClaimHvRunCommand-part6

![ClaimHvRunCommand: claim the next probe or review (activity), part 7 of 11](c4/rendered/ClaimHvRunCommand-part7.svg)

View: ClaimHvRunCommand-part7

![ClaimHvRunCommand: claim the next probe or review (activity), part 8 of 11](c4/rendered/ClaimHvRunCommand-part8.svg)

View: ClaimHvRunCommand-part8

![ClaimHvRunCommand: claim the next probe or review (activity), part 9 of 11](c4/rendered/ClaimHvRunCommand-part9.svg)

View: ClaimHvRunCommand-part9

![ClaimHvRunCommand: claim the next probe or review (activity), part 10 of 11](c4/rendered/ClaimHvRunCommand-part10.svg)

View: ClaimHvRunCommand-part10

![ClaimHvRunCommand: claim the next probe or review (activity), part 11 of 11](c4/rendered/ClaimHvRunCommand-part11.svg)

View: ClaimHvRunCommand-part11

#### RecordHvRunStartCommand: record that the reviewer process started [FR-MCP-HVPIPE-007, -008]

- Handler: `RecordHvRunStartCommand(string WorkspacePath, string RunId, string WorkerId, long FenceToken, int ProcessId) : ICommand<HvStartResult>`; `HvStartResult { startedUtc }`.
- Caller: process RunReviewerProcess, right after the reviewer process started and before it appends the `hv_brief` record (section 16.2).
- Delegate: AuditLedgerGuard; FencedLease `Guard`; a run already `Running` with the same `ProcessId` returns its stored start and writes nothing (a retried call); otherwise the run must be `Claimed` and becomes `Running` with `StartedUtc` and `ProcessId` (information only, never used to end a process, W-03).
- Crash outcomes: a crash after the process started and before this commit leaves the run `Claimed`; the reviewer ends with its worker (W-03), the lease expires, and the sweep queues a new attempt (W-02).
- Results: started; `stale_fence` (the worker stops its process); 409 `conflict` (`retryable: true`); 503 `audit_unavailable`.

![RecordHvRunStartCommand: record that the reviewer process started (activity), part 1 of 4](c4/rendered/RecordHvRunStartCommand.svg)

View: RecordHvRunStartCommand

![RecordHvRunStartCommand: record that the reviewer process started (activity), part 2 of 4](c4/rendered/RecordHvRunStartCommand-part2.svg)

View: RecordHvRunStartCommand-part2

![RecordHvRunStartCommand: record that the reviewer process started (activity), part 3 of 4](c4/rendered/RecordHvRunStartCommand-part3.svg)

View: RecordHvRunStartCommand-part3

![RecordHvRunStartCommand: record that the reviewer process started (activity), part 4 of 4](c4/rendered/RecordHvRunStartCommand-part4.svg)

View: RecordHvRunStartCommand-part4

#### RecordHvRunHeartbeatCommand: renew the lease and read cancellation [FR-MCP-HVPIPE-007, -008]

- Handler: `RecordHvRunHeartbeatCommand(string WorkspacePath, string RunId, string WorkerId, long FenceToken) : ICommand<HvHeartbeatResult>`; `HvHeartbeatResult { leaseExpiresUtc, cancelRequested }`.
- Delegate: AuditLedgerGuard; FencedLease `Renew`; `LastHeartbeatUtc`; `cancelRequested` is true when the request is `CancelRequested` (a probe run is never canceled this way).
- Results: renewed; `stale_fence` (the worker stops its process, section 16.2); 409 `conflict` (`retryable: true`); 503 `audit_unavailable`.

![RecordHvRunHeartbeatCommand: renew the lease and read cancellation (activity), part 1 of 3](c4/rendered/RecordHvRunHeartbeatCommand.svg)

View: RecordHvRunHeartbeatCommand

![RecordHvRunHeartbeatCommand: renew the lease and read cancellation (activity), part 2 of 3](c4/rendered/RecordHvRunHeartbeatCommand-part2.svg)

View: RecordHvRunHeartbeatCommand-part2

![RecordHvRunHeartbeatCommand: renew the lease and read cancellation (activity), part 3 of 3](c4/rendered/RecordHvRunHeartbeatCommand-part3.svg)

View: RecordHvRunHeartbeatCommand-part3

#### AppendHvRunOutputCommand: persist live output [FR-MCP-HVPIPE-008, -011, -016]

- Handler: `AppendHvRunOutputCommand(string WorkspacePath, string RunId, string WorkerId, long FenceToken, int Sequence, IReadOnlyList<string> Lines, IReadOnlyList<string> CompletedRecords) : ICommand<HvAppendResult>` (S-22, R-12); `CompletedRecords` are the reviewer records whose messages the adapter's extraction completed with this batch (section 16.3), often none.
- Validation: `Sequence` at least 1; at least one line; every line one UTF-8 JSON object (the adapter has already wrapped raw lines, section 13.3); every completed record one JSON object of a reviewer record type.
- Delegate: AuditLedgerGuard; FencedLease `Guard`; a chunk with this `Sequence` already stored whose SHA-256 equals the batch's returns it unchanged (a retried append), and one with another SHA-256 is 409 `conflict` with `retryable: false`; a `Sequence` other than `OutputChunkCount` + 1 is 409 `conflict` with `details.field` `sequence`; GzipCodec `Compress`; insert the chunk with its SHA-256 and chain hash; update the run's manifest (`OutputChunkCount`, `OutputLineCount`, `OutputBytes`, `OutputChainSha256`) and `LastOutputUtc`; the last `hv_progress` among the batch's completed records sets the run's progress (R-18) and an `hv_reviewer_turn` among them its reviewer turn ids. After the commit the output streams are woken.
- Results: appended (`sequence`, `chainSha256`); unchanged; `stale_fence`; 409 `conflict`; 500 `evidence_corrupt` (the round trip failed; nothing saved); 503 `audit_unavailable`.

![AppendHvRunOutputCommand: persist live output (activity), part 1 of 6](c4/rendered/AppendHvRunOutputCommand.svg)

View: AppendHvRunOutputCommand

![AppendHvRunOutputCommand: persist live output (activity), part 2 of 6](c4/rendered/AppendHvRunOutputCommand-part2.svg)

View: AppendHvRunOutputCommand-part2

![AppendHvRunOutputCommand: persist live output (activity), part 3 of 6](c4/rendered/AppendHvRunOutputCommand-part3.svg)

View: AppendHvRunOutputCommand-part3

![AppendHvRunOutputCommand: persist live output (activity), part 4 of 6](c4/rendered/AppendHvRunOutputCommand-part4.svg)

View: AppendHvRunOutputCommand-part4

![AppendHvRunOutputCommand: persist live output (activity), part 5 of 6](c4/rendered/AppendHvRunOutputCommand-part5.svg)

View: AppendHvRunOutputCommand-part5

![AppendHvRunOutputCommand: persist live output (activity), part 6 of 6](c4/rendered/AppendHvRunOutputCommand-part6.svg)

View: AppendHvRunOutputCommand-part6

#### CloseHvRunOutputCommand: close a run's output [FR-MCP-HVPIPE-003, -008, -011]

- Handler: `CloseHvRunOutputCommand(string WorkspacePath, string RunId, string WorkerId, long FenceToken, int ChunkCount, HvProvenance Provenance) : ICommand<HvCloseResult>`; the worker calls it only after the reviewer process exited with code 0 and every batch was appended; any other exit goes to FailHvRunCommand. Exit code 0 only routes the run here; whether it produced a review or a probe answer is decided by V-8 or policy ReadinessProbeVerdict (R-11).
- Reads, for a `Probe` run only, before any transaction opens (as FinalizeHvRequestCommand reads, R-19): AuditLedgerGuard `RecordRead` of its chunks, committed on its own (a refusal is 503 `audit_unavailable` and nothing else is written), then the chunks read and checked by V-1 and policy ReadinessProbeVerdict, so no evidence byte is consumed before its read is durable.
- Delegate: AuditLedgerGuard; FencedLease `Guard`; `ChunkCount` equal to `OutputChunkCount`, otherwise 409 `conflict` with `details.field` `chunkCount` (the worker appends what is missing and closes again); for a `Probe` run, the stored `OutputChunkCount` and `OutputChainSha256` equal to those the reads saw, otherwise the attempt ends and the unit starts again from its reads; the run becomes `OutputClosed` with `EndedUtc`, `ExitCode` 0, `RuntimeModel`, and `RuntimeEffort`; FencedLease `Release`. A `Review` run whose request is `CancelRequested` (a cancel that committed after the last heartbeat): the run becomes `Canceled` with code `canceled` instead, and policy HvRetryDecision makes the request `Canceled`, so an accepted cancellation is never lost (R-28). Any other `Review` run: the request becomes `Finalizing` and `hv_finalizing` is appended; after the commit the worker dispatches FinalizeHvRequestCommand. A `Probe` run: policy HvProbeOutcome settles it in this transaction with the verdict the reads produced (section 15): a probe of an older configuration (the reviewer was reconfigured while it ran) fails with `probe_stale_configuration` and changes nothing of the reviewer; a probe of the current configuration is decided by policy ReadinessProbeVerdict, a pass making the run `Succeeded` and the reviewer `Online` with `ResolvedModel` and `ResolvedEffort`, and a failure making the run `Failed` with the policy's code, attributed by FailHvRunCommand's one list (a reviewer code makes the reviewer `Offline` with its incident; `evidence_corrupt` is a server fault with a server incident and the readiness unchanged).
- Results: closed (`Finalizing`, request canceled, probe passed, probe failed, or stale probe); `stale_fence`; 409 `conflict`; 503 `audit_unavailable`.

![CloseHvRunOutputCommand: close a run's output (activity), part 1 of 10](c4/rendered/CloseHvRunOutputCommand.svg)

View: CloseHvRunOutputCommand

![CloseHvRunOutputCommand: close a run's output (activity), part 2 of 10](c4/rendered/CloseHvRunOutputCommand-part2.svg)

View: CloseHvRunOutputCommand-part2

![CloseHvRunOutputCommand: close a run's output (activity), part 3 of 10](c4/rendered/CloseHvRunOutputCommand-part3.svg)

View: CloseHvRunOutputCommand-part3

![CloseHvRunOutputCommand: close a run's output (activity), part 4 of 10](c4/rendered/CloseHvRunOutputCommand-part4.svg)

View: CloseHvRunOutputCommand-part4

![CloseHvRunOutputCommand: close a run's output (activity), part 5 of 10](c4/rendered/CloseHvRunOutputCommand-part5.svg)

View: CloseHvRunOutputCommand-part5

![CloseHvRunOutputCommand: close a run's output (activity), part 6 of 10](c4/rendered/CloseHvRunOutputCommand-part6.svg)

View: CloseHvRunOutputCommand-part6

![CloseHvRunOutputCommand: close a run's output (activity), part 7 of 10](c4/rendered/CloseHvRunOutputCommand-part7.svg)

View: CloseHvRunOutputCommand-part7

![CloseHvRunOutputCommand: close a run's output (activity), part 8 of 10](c4/rendered/CloseHvRunOutputCommand-part8.svg)

View: CloseHvRunOutputCommand-part8

![CloseHvRunOutputCommand: close a run's output (activity), part 9 of 10](c4/rendered/CloseHvRunOutputCommand-part9.svg)

View: CloseHvRunOutputCommand-part9

![CloseHvRunOutputCommand: close a run's output (activity), part 10 of 10](c4/rendered/CloseHvRunOutputCommand-part10.svg)

View: CloseHvRunOutputCommand-part10

#### FailHvRunCommand: record an execution failure [FR-MCP-HVPIPE-009]

- Handler: `FailHvRunCommand(string WorkspacePath, string RunId, string? WorkerId, long FenceToken, int? ExpectedRequestStateVersion, string FailureCode, string Detail) : ICommand<HvFailResult>`. Callers: the worker (`WorkerId` set, a `Claimed` or `Running` run, `ExpectedRequestStateVersion` null) and FinalizeHvRequestCommand (`WorkerId` null, an `OutputClosed` run with its released lease, whose stored fence the finalizer passes, with the request's `StateVersion` its reads saw).
- Delegate: AuditLedgerGuard; FencedLease `Guard`; for the finalizer's call, the run must still be `OutputClosed` and its request still `Finalizing` at `ExpectedRequestStateVersion`, otherwise nothing changes and the result is the stored outcome (`already_completed` when another finalization committed, `not_finalizable` otherwise), so a late verification failure never undoes a committed finalization (I-03); the run becomes `Failed` (or `Canceled` for the code `canceled`) with the code and the detail (through SecretRedactor) and `EndedUtc`, and its lease is released. Then, by run kind. A `Review` run: `canceled` and `worker_shutdown` insert no incident and change no reviewer; every other failure inserts an `HvIncident` (`Pending`) with `DedupeKey` `hv-reviewer-<reviewerId>-<failureCode>` when the failure is attributable to the reviewer (`reviewer_launch_failed`, `reviewer_exited_nonzero`, `reviewer_output_invalid`, `reviewer_turn_unverified`, `runtime_identity_mismatch`, `verdict_binding_failed`, and the probe codes `probe_nonce_mismatch` and `reviewer_turn_logging_unsupported`, which CloseHvRunOutputCommand records), and `hv-server-<failureCode>` otherwise (`evidence_corrupt`, `snapshot_changed`, `model_grade_conflict`, `reviewer_not_independent`); this one attribution list holds for every incident of a run, review or probe; a reviewer-attributable failure also makes the reviewer `Offline` with `ReadinessReason` the code and `OpenIncidentId`, and appends `reviewer_offline` (S-15); then the request goes through policy HvRetryDecision, which decides and writes its next state: `Queued` again (its assignee re-evaluated at the next claim), `Failed` with `attempts_exhausted`, or, for a `CancelRequested` request, `Canceled`, each ended request with its turns released, its `Pending` responses closed, and its notification (section 15). A `Probe` run has no request, so HvRetryDecision does not apply: policy HvProbeOutcome settles it with the same attribution list, after its configuration check, so a failing probe of an older configuration changes nothing of the reviewer; for the current configuration, `worker_shutdown` requests the probe again, and every other failure leaves the reviewer as the attribution sets it (`Offline` with its incident for a reviewer code, readiness unchanged with a server incident otherwise), with no new probe until a caller requests one (S-15). After the commit the worker is woken to file the incident (FileHvIncidentCommand) and claim again.
- Results: failed with the request's next state, or the probe outcome; `already_completed` or `not_finalizable` (finalizer's call, nothing changed); `stale_fence`; 409 `conflict`; 503 `audit_unavailable`.

![FailHvRunCommand: record an execution failure (activity), part 1 of 10](c4/rendered/FailHvRunCommand.svg)

View: FailHvRunCommand

![FailHvRunCommand: record an execution failure (activity), part 2 of 10](c4/rendered/FailHvRunCommand-part2.svg)

View: FailHvRunCommand-part2

![FailHvRunCommand: record an execution failure (activity), part 3 of 10](c4/rendered/FailHvRunCommand-part3.svg)

View: FailHvRunCommand-part3

![FailHvRunCommand: record an execution failure (activity), part 4 of 10](c4/rendered/FailHvRunCommand-part4.svg)

View: FailHvRunCommand-part4

![FailHvRunCommand: record an execution failure (activity), part 5 of 10](c4/rendered/FailHvRunCommand-part5.svg)

View: FailHvRunCommand-part5

![FailHvRunCommand: record an execution failure (activity), part 6 of 10](c4/rendered/FailHvRunCommand-part6.svg)

View: FailHvRunCommand-part6

![FailHvRunCommand: record an execution failure (activity), part 7 of 10](c4/rendered/FailHvRunCommand-part7.svg)

View: FailHvRunCommand-part7

![FailHvRunCommand: record an execution failure (activity), part 8 of 10](c4/rendered/FailHvRunCommand-part8.svg)

View: FailHvRunCommand-part8

![FailHvRunCommand: record an execution failure (activity), part 9 of 10](c4/rendered/FailHvRunCommand-part9.svg)

View: FailHvRunCommand-part9

![FailHvRunCommand: record an execution failure (activity), part 10 of 10](c4/rendered/FailHvRunCommand-part10.svg)

View: FailHvRunCommand-part10

#### ExpireHvRunLeasesCommand: recover from dead workers [FR-MCP-HVPIPE-007]

- Handler: `ExpireHvRunLeasesCommand(string WorkspacePath) : ICommand<HvSweepResult>`; `HvSweepResult { abandonedRunIds, finalizationPending }`.
- For each run read in `Claimed` or `Running` whose `LeaseExpiresUtc` has passed, one SerializableWrite (so one stuck run never blocks the others): AuditLedgerGuard; FencedLease `Expire` with the fence the sweep read and the live statuses `Claimed` and `Running` (skipped when its worker renewed it, the run was settled, or another sweep already took it over, so a run is abandoned at most once and a second sweep never acts on its request); the run becomes `Abandoned` with `lease_expired`, and its incremented fence makes the old worker's next write `stale_fence` (W-03); a server incident (`hv-server-lease_expired`) is inserted; for a `Review` run, policy HvRetryDecision decides and writes its request's next state (`Queued`; `Failed` on its last counted attempt; `Canceled` when a cancel was requested, which completes the cancel of a dead worker's run, R-28), the ended request with its turns released, its `Pending` responses closed, and `hv_failed` or `hv_canceled`, as after FailHvRunCommand; a `Probe` run is settled by policy HvProbeOutcome as `lease_expired`, which requests its probe again only while it is of the reviewer's current configuration and leaves the reviewer as it was. Then it lists the `Finalizing` requests (W-05) for the worker to finalize.
- Results: the sweep result; 503 `audit_unavailable` ends the sweep (W-12).

![ExpireHvRunLeasesCommand: recover from dead workers (activity), part 1 of 7](c4/rendered/ExpireHvRunLeasesCommand.svg)

View: ExpireHvRunLeasesCommand

![ExpireHvRunLeasesCommand: recover from dead workers (activity), part 2 of 7](c4/rendered/ExpireHvRunLeasesCommand-part2.svg)

View: ExpireHvRunLeasesCommand-part2

![ExpireHvRunLeasesCommand: recover from dead workers (activity), part 3 of 7](c4/rendered/ExpireHvRunLeasesCommand-part3.svg)

View: ExpireHvRunLeasesCommand-part3

![ExpireHvRunLeasesCommand: recover from dead workers (activity), part 4 of 7](c4/rendered/ExpireHvRunLeasesCommand-part4.svg)

View: ExpireHvRunLeasesCommand-part4

![ExpireHvRunLeasesCommand: recover from dead workers (activity), part 5 of 7](c4/rendered/ExpireHvRunLeasesCommand-part5.svg)

View: ExpireHvRunLeasesCommand-part5

![ExpireHvRunLeasesCommand: recover from dead workers (activity), part 6 of 7](c4/rendered/ExpireHvRunLeasesCommand-part6.svg)

View: ExpireHvRunLeasesCommand-part6

![ExpireHvRunLeasesCommand: recover from dead workers (activity), part 7 of 7](c4/rendered/ExpireHvRunLeasesCommand-part7.svg)

View: ExpireHvRunLeasesCommand-part7

#### FinalizeHvRequestCommand: verify and commit the result atomically [FR-MCP-HVPIPE-010, -011, -012, -013, -014, -021]

- Handler: `FinalizeHvRequestCommand(string WorkspacePath, string HvRequestId) : ICommand<HvFinalizeResult>` (S-03, S-06, S-07, S-10, S-13, S-21, S-23, R-15). Callers: the worker after CloseHvRunOutputCommand, and the worker's sweep for every `Finalizing` request (W-05, W-06).
- Reads, with no transaction open (foundation section 3.1, "Database-only delegates"): the request (`Completed` returns the stored outcome and writes nothing, I-03; any status other than `Finalizing` returns `not_finalizable` and writes nothing); its `OutputClosed` run; AuditLedgerGuard `RecordRead` on the run's chunks before any is decompressed (a refusal is 503 `audit_unavailable` and the request stays `Finalizing`, R-19); every chunk decompressed and checked by policy HvFinalVerification V-1, then the reviewer's records, extracted from the stored log by the reviewer's adapter (`ExtractReviewerRecords`, the same function the worker ran, section 16.3), checked by V-2 to V-8 (section 15); when the request links responses and `Mcp:AgentContext:Mode` is not `Off`, EvaluateStalenessCommand is dispatched for them, as the foundation's grade units do before their write (foundation section 3.1).
- Verification failure: FailHvRunCommand is dispatched with the run's stored fence, the request `StateVersion` the reads saw, and the failing check's code; nothing of the result is written, and each of its results is this unit's: failed is `verification_failed` with the request's next state (`Queued` or `Failed`); `already_completed` (another finalization committed first) and `not_finalizable` are returned as they are, with nothing changed, and so is `stale_fence` as `not_finalizable`; 409 `conflict` and 503 `audit_unavailable` are returned as they are, with the request still `Finalizing` for the sweep (W-06). A result compression that fails its round trip inside the transaction is handled the same way: the transaction rolls back and FailHvRunCommand is dispatched with `evidence_corrupt`, a server fault, so the request's attempts bound the retries (FR-MCP-HVPIPE-011-AC007); and so is a RecordModelGradeCommand answered 409 `conflict` with `retryable: false` (a stored grade with other input), with `model_grade_conflict` (FR-MCP-HVPIPE-021-AC007).
- Delegate (SerializableWrite, S-23): AuditLedgerGuard; re-check that the request is `Finalizing` at the version read, the run is `OutputClosed` with the fence and chunk manifest read, every member turn is `Pending` for this request, and each linked response's status is the one the reads saw (a difference ends the attempt, and the unit starts again from its request status check); then, all in this transaction: the `HvTurnAssessment` and `HvFinding` rows; the holistic scores and verdict and the overall verdict on the request (the one predicate of R-31); for each member turn RecordSessionTurnReviewCommand (kind hostile, origin `HvFinalization`, dispatched in the delegate so it joins the transaction) with `hvVerdict` the turn's verdict, its scores, `reasons` its FAIL and UNKNOWN findings prefixed `FAIL:` or `UNKNOWN:`, `reviewer` `<reviewerId>/<runtimeModel>/<runtimeEffort>`, `reviewedAt` the run's `EndedUtc`, and `requestJsonlPath` and `responseJsonlPath` the evidence references `hv-evidence:<hvRequestId>/request` and `hv-evidence:<hvRequestId>/response-log/<runId>` (policy HvVerdictMapping stores the status, A-06); policy HvTurnFreeze `Finalize` (`HvState` `Final`); each `HvRequestTurn` `Reviewed`; for each admitted response still `Pending` at commit, when Mode is not `Off`, RecordModelGradeCommand (the internal unit of section 14.5: origin `HvFinalization`, the run's reviewer as grader, the response's `hv_response_grade` record) with its trust decision, which marks the response item `Graded`, and when Mode is `Off`, the item `NotGraded` (`agent_context_disabled`); an admitted response that left the review since admission (its item `Closed`, ResponseStatusTransition) gets no grade, and its grade record is kept in the result document (`responseGrades`, disposition `Closed`); the FAIL and UNKNOWN findings of every response grade, whatever the item's disposition, are among the `HvFinding` rows of scope `Response` above; the `Result` evidence (GzipCodec) and `ResultSha256`; the request `Completed` with `CompletedUtc` and `FinalizedRunId`; the run `Succeeded` with `VerdictRecordSha256`, the SHA-256 of the verified `hv_verdict` record in its stored normal form (the form V-6 compares); NotificationOutbox `hv_completed` with the overall verdict. After the commit, the post-commit dispatches the foundation's grade units make (EvaluateStalenessCommand for dependents, the projection pass) run as they do for a grade (foundation section 3.1, "Projection pass").
- Results: `completed` (with the overall verdict); `already_completed`; `not_finalizable`; `verification_failed` (code, `evidence_corrupt` included for a failed result compression and `model_grade_conflict` for a stored grade with other input); 409 `conflict` (`retryable: true`, the sweep finalizes again); 503 `audit_unavailable` (the request stays `Finalizing`, W-06).

![FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 1 of 7](c4/rendered/FinalizeHvRequestCommand.svg)

View: FinalizeHvRequestCommand

![FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 2 of 7](c4/rendered/FinalizeHvRequestCommand-part2.svg)

View: FinalizeHvRequestCommand-part2

![FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 3 of 7](c4/rendered/FinalizeHvRequestCommand-part3.svg)

View: FinalizeHvRequestCommand-part3

![FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 4 of 7](c4/rendered/FinalizeHvRequestCommand-part4.svg)

View: FinalizeHvRequestCommand-part4

![FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 5 of 7](c4/rendered/FinalizeHvRequestCommand-part5.svg)

View: FinalizeHvRequestCommand-part5

![FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 6 of 7](c4/rendered/FinalizeHvRequestCommand-part6.svg)

View: FinalizeHvRequestCommand-part6

![FinalizeHvRequestCommand: verify and commit the result atomically (activity), part 7 of 7](c4/rendered/FinalizeHvRequestCommand-part7.svg)

View: FinalizeHvRequestCommand-part7

##### Nested activity: ReadFinalizationInputs (reads of FinalizeHvRequestCommand, no transaction open)

![Nested activity: ReadFinalizationInputs (reads of FinalizeHvRequestCommand, no transaction open) (activity), part 1 of 3](c4/rendered/ReadFinalizationInputs.svg)

View: ReadFinalizationInputs

![Nested activity: ReadFinalizationInputs (reads of FinalizeHvRequestCommand, no transaction open) (activity), part 2 of 3](c4/rendered/ReadFinalizationInputs-part2.svg)

View: ReadFinalizationInputs-part2

![Nested activity: ReadFinalizationInputs (reads of FinalizeHvRequestCommand, no transaction open) (activity), part 3 of 3](c4/rendered/ReadFinalizationInputs-part3.svg)

View: ReadFinalizationInputs-part3

##### Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate)

![Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 1 of 6](c4/rendered/CommitFinalResult.svg)

View: CommitFinalResult

![Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 2 of 6](c4/rendered/CommitFinalResult-part2.svg)

View: CommitFinalResult-part2

![Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 3 of 6](c4/rendered/CommitFinalResult-part3.svg)

View: CommitFinalResult-part3

![Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 4 of 6](c4/rendered/CommitFinalResult-part4.svg)

View: CommitFinalResult-part4

![Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 5 of 6](c4/rendered/CommitFinalResult-part5.svg)

View: CommitFinalResult-part5

![Nested activity: CommitFinalResult (the writes of FinalizeHvRequestCommand, inside its delegate) (activity), part 6 of 6](c4/rendered/CommitFinalResult-part6.svg)

View: CommitFinalResult-part6

#### FileHvIncidentCommand: file an incident through the triage endpoint [FR-MCP-HVPIPE-009]

- Handler: `FileHvIncidentCommand(string WorkspacePath, string IncidentId) : ICommand<HvIncidentDto>` (S-15, S-26, R-17).
- Order, so that no write reaches triage while the ledger is down (R-19): the incident (`Filed` returns it unchanged); first one SerializableWrite with AuditLedgerGuard records the attempt (`FilingAttempts` plus 1, `LastFilingAttemptUtc`), the write-ahead record of the filing, and a 503 `audit_unavailable` here ends the unit before triage is called; then, outside any transaction, `ITriageService` submits the report (title `Hostile validation <failureCode> on <reviewerId>`, summary, component `hostile-validation`, `dedupeKey`, `errorSignature` the failure code, the sanitized evidence, the run and request ids, reporter `QBrainAi`), the service behind `POST /qbrainai/triage/reports`; then a second SerializableWrite with AuditLedgerGuard records the outcome inside its transaction: accepted sets `TriageReportId`, `TriageGroupId`, `FilingStatus` `Filed`, `FiledUtc`, and appends `incident_filed`; refused or unreachable sets `LastFilingError`, and the sweep tries again (W-08, W-09). A ledger failure after triage accepted leaves the incident `Pending`, and the retry files under the same `dedupeKey`, which joins the same triage group (L-03).
- Results: filed; filing pending with the error; 409 `conflict` (`retryable: true`); 503 `audit_unavailable` (before triage is called, or after it with the incident still `Pending`).

![FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 1 of 5](c4/rendered/FileHvIncidentCommand.svg)

View: FileHvIncidentCommand

![FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 2 of 5](c4/rendered/FileHvIncidentCommand-part2.svg)

View: FileHvIncidentCommand-part2

![FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 3 of 5](c4/rendered/FileHvIncidentCommand-part3.svg)

View: FileHvIncidentCommand-part3

![FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 4 of 5](c4/rendered/FileHvIncidentCommand-part4.svg)

View: FileHvIncidentCommand-part4

![FileHvIncidentCommand: file an incident through the triage endpoint (activity), part 5 of 5](c4/rendered/FileHvIncidentCommand-part5.svg)

View: FileHvIncidentCommand-part5

### 14.5 Amended foundation units

- RecordSessionTurnReviewCommand (foundation section 5.2): before policy ReviewValidation it applies the hostile-review rule of A-06 (403 `review_hostile_pipeline_only` for `Pass`, `Fail`, `Running`, or `hvVerdict` on a hostile review from any origin other than `HvFinalization`) and, through TurnWritePipeline, policy HvTurnFreeze (A-07); its error list gains both codes. ResetSessionTurnReviewCommand, the section replace and clear routes and MCP tools, the dialog, title, and turn write units of foundation section 4.2, and the session-level replace and delete gain the 409 `turn_immutable` refusal of HvTurnFreeze.
- RecordModelGradeCommand (foundation section 6.5): its contract is replaced by the internal unit below (A-05).
- CreateAgentContextHvRequestCommand, GetAgentContextHvRequestQuery, and QueryAgentContextHvRequestsQuery (foundation section 6.5): superseded, A-04.

#### RecordModelGradeCommand: record a model grade inside finalization (amended, internal) [FR-MCP-HVPIPE-021]

- Handler: `RecordModelGradeCommand(string WorkspacePath, string ResponseId, HvModelGradeInput Input) : ICommand<AgentResponseGradeResult>`; `HvModelGradeInput { hvRequestId, runId, reviewerId, runtimeModel, runtimeEffort, requestEvidenceSha256, responseLogChainSha256, correctness, completeness, verdict, findings }`, taken from the finalized run and the response's verified `hv_response_grade` record; no receipt path, thread, or reviewer turn field remains.
- Caller: FinalizeHvRequestCommand only, inside its SerializableWrite delegate (origin `HvFinalization`); there is no REST route, MCP tool, client or REPL method, or PowerShell builder (A-05).
- Guards, read inside the caller's transaction from the database only: a grade already stored for the natural key (`ResponseId`, `HvRequestId`, kind `Model`) with equal input returns it and writes nothing (a retried finalization); another stored input is 409 `conflict` with `retryable: false`, which cannot occur while the request is `Finalizing` (finalization is the only writer of model grades, A-05, and commits a request's grades with its completion) and, when the store shows it anyway, makes FinalizeHvRequestCommand roll back and fail the run with `model_grade_conflict`, a server fault, so the request's attempts bound it; otherwise the response is neither `Rejected` nor `Superseded`, its `HvRequestResponse` item of this request is `Pending`, the HV request is `Finalizing` (never `Open`: the foundation's HV request record no longer exists, A-04), and the grader is the finalized run's reviewer, and a failed guard is 409 `conflict` with `retryable: true`, which ends the caller's attempt so it reads again.
- Not done here: the foundation's eight receipt checks, replaced by V-1 to V-8 before the transaction (A-05), and the staleness dispatch, which ReadFinalizationInputs runs before the transaction.
- Writes: the grade (`grd-` key through ReadableKeyAllocator and IdempotentCreate) with `HvRequestId`, `RunId`, `RequestEvidenceSha256`, `ResponseLogChainSha256`, the findings, the thresholds in force, and `RecordedUtc`; the item `Graded` with its `GradeId`; DecideAgentResponseTrustCommand dispatched in the same transaction. It never changes the HV request's state: FinalizeHvRequestCommand completes the request.
- Results: the grade; 409 `conflict` (`retryable: false` or `true` as above).

![RecordModelGradeCommand: record a model grade inside finalization (amended, internal) (activity), part 1 of 4](c4/rendered/AM1-RecordModelGradeCommand.svg)

View: AM1-RecordModelGradeCommand

![RecordModelGradeCommand: record a model grade inside finalization (amended, internal) (activity), part 2 of 4](c4/rendered/AM1-RecordModelGradeCommand-part2.svg)

View: AM1-RecordModelGradeCommand-part2

![RecordModelGradeCommand: record a model grade inside finalization (amended, internal) (activity), part 3 of 4](c4/rendered/AM1-RecordModelGradeCommand-part3.svg)

View: AM1-RecordModelGradeCommand-part3

![RecordModelGradeCommand: record a model grade inside finalization (amended, internal) (activity), part 4 of 4](c4/rendered/AM1-RecordModelGradeCommand-part4.svg)

View: AM1-RecordModelGradeCommand-part4

## 15. Policies

### Policy HvScopeResolution: resolve the member turns [FR-MCP-HVPIPE-004]

Applied by SubmitHvRequestCommand inside its delegate, reading only the database. TurnLocator finds the reviewed session by `agent` and `sessionId` (`SessionNotFound` is `hv_scope_invalid`); `Turn`: the one turn by `requestId`; `Range`: both endpoint turns, then every turn between them inclusive in the session's turn order (the order develop reads a session's turns in: ascending turn row `Id`, VI-06), refused when an endpoint is missing or the start is after the end; `Session`: every turn of the session at this moment (S-11). The submitting turn is removed from a `Session` scope (`ExcludedSubmittingTurn` true) and refuses a `Turn` or `Range` scope that contains it. More than `MaxTurnsPerRequest` members is `hv_scope_too_large` with the count; no member left is `hv_scope_empty`.

![Policy HvScopeResolution: resolve the member turns (activity), part 1 of 7](c4/rendered/HvScopeResolution.svg)

View: HvScopeResolution

![Policy HvScopeResolution: resolve the member turns (activity), part 2 of 7](c4/rendered/HvScopeResolution-part2.svg)

View: HvScopeResolution-part2

![Policy HvScopeResolution: resolve the member turns (activity), part 3 of 7](c4/rendered/HvScopeResolution-part3.svg)

View: HvScopeResolution-part3

![Policy HvScopeResolution: resolve the member turns (activity), part 4 of 7](c4/rendered/HvScopeResolution-part4.svg)

View: HvScopeResolution-part4

![Policy HvScopeResolution: resolve the member turns (activity), part 5 of 7](c4/rendered/HvScopeResolution-part5.svg)

View: HvScopeResolution-part5

![Policy HvScopeResolution: resolve the member turns (activity), part 6 of 7](c4/rendered/HvScopeResolution-part6.svg)

View: HvScopeResolution-part6

![Policy HvScopeResolution: resolve the member turns (activity), part 7 of 7](c4/rendered/HvScopeResolution-part7.svg)

View: HvScopeResolution-part7

### Policy HvAdmissionEligibility: admit only reviewable turns [FR-MCP-HVPIPE-005, -013]

Applied by SubmitHvRequestCommand after HvScopeResolution. It reads each member's status and HV state and never its `agentReview` (R-21). It collects every violation before it refuses, so the refusal names all affected turns: a non-terminal turn (`TurnStatus.IsTerminal` false) is `hv_turns_not_terminal`; a `Pending` turn is `hv_turns_pending` with its open request; a `Final` turn is `hv_turns_already_reviewed` with its request (null for a turn the A-11 migration made `Final`). When more than one kind applies, the first in that order is the code and `details` lists every kind (R-05, R-27).

![Policy HvAdmissionEligibility: admit only reviewable turns (activity), part 1 of 3](c4/rendered/HvAdmissionEligibility.svg)

View: HvAdmissionEligibility

![Policy HvAdmissionEligibility: admit only reviewable turns (activity), part 2 of 3](c4/rendered/HvAdmissionEligibility-part2.svg)

View: HvAdmissionEligibility-part2

![Policy HvAdmissionEligibility: admit only reviewable turns (activity), part 3 of 3](c4/rendered/HvAdmissionEligibility-part3.svg)

View: HvAdmissionEligibility-part3

### Policy HvReviewerEligibility: who may review [FR-MCP-HVPIPE-002, -003]

A reviewer is eligible for a request when it is enabled, `Online`, and its id differs from both the request's `SubmitterReviewerId` and its `AuthorReviewerId` (AgentIdentity, R-08). Capacity (`MaxConcurrentRuns`) is a claim-time condition (ClaimHvRunCommand), not eligibility, so admission never refuses a request only because reviewers are busy.

![Policy HvReviewerEligibility: who may review (activity), part 1 of 2](c4/rendered/HvReviewerEligibility.svg)

View: HvReviewerEligibility

![Policy HvReviewerEligibility: who may review (activity), part 2 of 2](c4/rendered/HvReviewerEligibility-part2.svg)

View: HvReviewerEligibility-part2

### Policy RoundRobinAssignment: pick the next reviewer [FR-MCP-HVPIPE-002, -007]

Applied inside a SerializableWrite (admission, and reassignment at claim). The pool order is Cline, Copilot, Claude, Codex, Grok. Starting after `HvWorkspaceState.LastAssignedReviewerId` (or at the start when null), it takes the first reviewer in cyclic order that HvReviewerEligibility accepts and, when the caller passes the capacity filter, that also has fewer runs in `Claimed` or `Running` than its `MaxConcurrentRuns`, and sets the cursor to it in the same transaction (S-17, R-07). Admission passes no filter, so a request is admitted while every reviewer is busy (capacity is a claim condition, HvReviewerEligibility); reassignment at claim passes it, so a full reviewer is skipped for a later free one. With none at admission it refuses with `hv_no_reviewer_available` and `details.reviewers`, each reviewer's enabled flag, readiness, reason, and open incident, the context S-16 asks the plugin to show; with none at claim it returns none and writes nothing, so the cursor and the request stay as they were and the request waits `Queued`.

![Policy RoundRobinAssignment: pick the next reviewer (activity), part 1 of 5](c4/rendered/RoundRobinAssignment.svg)

View: RoundRobinAssignment

![Policy RoundRobinAssignment: pick the next reviewer (activity), part 2 of 5](c4/rendered/RoundRobinAssignment-part2.svg)

View: RoundRobinAssignment-part2

![Policy RoundRobinAssignment: pick the next reviewer (activity), part 3 of 5](c4/rendered/RoundRobinAssignment-part3.svg)

View: RoundRobinAssignment-part3

![Policy RoundRobinAssignment: pick the next reviewer (activity), part 4 of 5](c4/rendered/RoundRobinAssignment-part4.svg)

View: RoundRobinAssignment-part4

![Policy RoundRobinAssignment: pick the next reviewer (activity), part 5 of 5](c4/rendered/RoundRobinAssignment-part5.svg)

View: RoundRobinAssignment-part5

### Policy HvTurnFreeze: freeze, release, finalize, and guard [FR-MCP-HVPIPE-012]

- `Freeze(turns, hvRequestId)`: `HvState` `Pending` and `HvRequestId` on each member turn (SubmitHvRequestCommand). `Release(hvRequestId)`: back to `None` for the turns of a request that ended `Canceled` or `Failed`. `Finalize(hvRequestId)`: `Final` (FinalizeHvRequestCommand). Each writes the turn's HV columns only, never its content, inside the caller's transaction.
- `Guard(turn, origin)`: applied by TurnWritePipeline after TurnLocator and before every other policy, and by every unit of A-07: a turn whose `HvState` is `Pending` or `Final` refuses every write and delete of the turn or its children, from every origin except `HvFinalization` writing a `Pending` turn of its own request, with 409 `turn_immutable`, `details.reason` `hv_pending` or `hv_final`, and `details.hvRequestId` (S-06, S-07, R-04). A session-level replace or delete that would change such a turn is refused the same way.

![Policy HvTurnFreeze: freeze, release, finalize, and guard (activity), part 1 of 2](c4/rendered/HvTurnFreeze.svg)

View: HvTurnFreeze

![Policy HvTurnFreeze: freeze, release, finalize, and guard (activity), part 2 of 2](c4/rendered/HvTurnFreeze-part2.svg)

View: HvTurnFreeze-part2

### Policy HvFinalVerification: the eight verification steps [FR-MCP-HVPIPE-010, -011, -021]

Applied by FinalizeHvRequestCommand to the run's stored chunks before any transaction; each failed step returns its code, and the steps run in the diagram's order (V-1, V-8, V-2, V-3, V-4, V-5, V-6, V-7), so the log's integrity and completeness are known before any record is bound. They replace the foundation's eight receipt checks (A-05): `receipt_path` becomes V-1 (the evidence is held by the server, not found on disk), `request_meta` becomes V-3 and V-7 (the expected reviewer is recorded on the run), `verdict_block` becomes V-8, `verdict_binding` becomes V-2 and V-3, `verdict_match` becomes V-4, `reviewer_turn` becomes V-6, `reviewer_independent` becomes V-5 (amended, A-09), and `runtime_identity` becomes V-7.

- V-1 Evidence integrity (`evidence_corrupt`): the chunks run 1 to `OutputChunkCount` without a gap; each decompresses (GzipCodec), its SHA-256 equals the stored one, the chain hash equals `OutputChainSha256`, and every line parses as one JSON object (I-06).
- V-2 Snapshot binding: each `hv_turn_assessment` names a member's ordinal, turn key, and admission snapshot hash (`verdict_binding_failed` otherwise), and TurnSnapshotBuilder over each turn now yields that hash (`snapshot_changed` otherwise; HvTurnFreeze makes it unreachable, and the step proves it, I-02).
- V-3 Verdict binding (`verdict_binding_failed`): the `hv_verdict` record names this `hvRequestId` and run, its counts equal the members and the responses of the accepted request (the set admitted, whatever has happened to those responses since), its reviewer equals the run's reviewer, and each `hv_response_grade` names a response of the accepted request with the `ContentSha256` the accepted request recorded.
- V-4 Verdict consistency (`reviewer_output_invalid`): every score is a valid lexeme (ScoreLexeme); each turn's verdict is `AGREE` exactly when ScoreVerdict derives `Pass` at the hostile thresholds and its findings hold no FAIL or UNKNOWN; the holistic verdict likewise at `max(threshold, 0.98)` (R-31); each response grade follows the foundation's `verdict_match` rule; and the overall verdict is the one predicate of R-31 over the holistic verdict, every turn's verdict, the response grades, and the findings of every scope.
- V-5 Reviewer independence (`reviewer_not_independent`): the run's reviewer id differs from the request's submitter id and from its author id (S-17, S-28, P-01, P-07).
- V-6 Reviewer turn (`reviewer_turn_unverified`): the `hv_reviewer_turn` record names a turn TurnLocator finds, whose agent AgentIdentity maps to the run's reviewer, whose session is neither the reviewed session nor the submitter's, created at or after the run's `ClaimedUtc`, persisted (not a failsafe or queued record), closed `completed` with class `Chore`, and whose stored result (its response, then its dialog parts in order with each part's header line removed, as foundation check 6 reads it) contains every reviewer record of the run in the order the extraction returned them (each `hv_turn_assessment`, the `hv_holistic`, each `hv_response_grade`, and the `hv_verdict`), each byte for byte in its stored normal form; a summary-only, open, or failed turn fails (P-06, R-24).
- V-7 Runtime identity (`runtime_identity_mismatch`): the log holds at least one top-level `hv_provenance` line written by the worker from the CLI's runtime receipt of this run, and every one names the run's expected model (or, when the reviewer has no configured model, the model the last NONCE resolved) and expected effort (P-04).
- V-8 Completeness (`reviewer_output_invalid`): exactly one `hv_verdict`, the last reviewer record; exactly one `hv_holistic`; exactly one `hv_turn_assessment` per member and one `hv_response_grade` per response of the accepted request; every FAIL and UNKNOWN finding of every scope (turn, holistic, and response) has a category, a location, evidence, and a remediation (S-13); the failures list equals every FAIL and UNKNOWN finding of every scope.

![Policy HvFinalVerification: the eight verification steps (activity), part 1 of 2](c4/rendered/HvFinalVerification.svg)

View: HvFinalVerification

![Policy HvFinalVerification: the eight verification steps (activity), part 2 of 2](c4/rendered/HvFinalVerification-part2.svg)

View: HvFinalVerification-part2

##### Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification)

![Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification) (activity), part 1 of 4](c4/rendered/VerifyLogAndBinding.svg)

View: VerifyLogAndBinding

![Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification) (activity), part 2 of 4](c4/rendered/VerifyLogAndBinding-part2.svg)

View: VerifyLogAndBinding-part2

![Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification) (activity), part 3 of 4](c4/rendered/VerifyLogAndBinding-part3.svg)

View: VerifyLogAndBinding-part3

![Nested activity: VerifyLogAndBinding (V-1, V-8, V-2, V-3 of HvFinalVerification) (activity), part 4 of 4](c4/rendered/VerifyLogAndBinding-part4.svg)

View: VerifyLogAndBinding-part4

##### Nested activity: VerifyVerdictAndReviewer (V-4 to V-7 of HvFinalVerification)

![Nested activity: VerifyVerdictAndReviewer (V-4 to V-7 of HvFinalVerification) (activity), part 1 of 3](c4/rendered/VerifyVerdictAndReviewer.svg)

View: VerifyVerdictAndReviewer

![Nested activity: VerifyVerdictAndReviewer (V-4 to V-7 of HvFinalVerification) (activity), part 2 of 3](c4/rendered/VerifyVerdictAndReviewer-part2.svg)

View: VerifyVerdictAndReviewer-part2

![Nested activity: VerifyVerdictAndReviewer (V-4 to V-7 of HvFinalVerification) (activity), part 3 of 3](c4/rendered/VerifyVerdictAndReviewer-part3.svg)

View: VerifyVerdictAndReviewer-part3

### Policy ReadinessProbeVerdict: the NONCE decision [FR-MCP-HVPIPE-003]

Applied by CloseHvRunOutputCommand to a `Probe` run's chunks, which it reads after their `RecordRead` commits and before its transaction opens (R-19); its verdict is handed to policy HvProbeOutcome in that transaction. The probe passes when V-1 holds for its chunks, its log holds exactly one `hv_probe` record whose `nonce` equals the run's `ProbeNonce` (`probe_nonce_mismatch` otherwise), an `hv_provenance` line names the expected model and effort (`runtime_identity_mismatch` otherwise; with no configured model the runtime model is accepted and becomes `ResolvedModel`), and its `hv_reviewer_turn` names a persisted turn TurnLocator finds for that reviewer created at or after the run's `ClaimedUtc` (`reviewer_turn_logging_unsupported` otherwise, R-24) (S-27: "the agent is available and can execute an HV"). HvProbeOutcome uses this verdict only for a probe of the reviewer's current `ConfigurationVersion`.

![Policy ReadinessProbeVerdict: the NONCE decision (activity), part 1 of 3](c4/rendered/ReadinessProbeVerdict.svg)

View: ReadinessProbeVerdict

![Policy ReadinessProbeVerdict: the NONCE decision (activity), part 2 of 3](c4/rendered/ReadinessProbeVerdict-part2.svg)

View: ReadinessProbeVerdict-part2

![Policy ReadinessProbeVerdict: the NONCE decision (activity), part 3 of 3](c4/rendered/ReadinessProbeVerdict-part3.svg)

View: ReadinessProbeVerdict-part3

### Policy HvProbeOutcome: settle a probe run and its reviewer [FR-MCP-HVPIPE-003, -009]

Applied inside the caller's transaction for a `Probe` run by CloseHvRunOutputCommand, FailHvRunCommand, and ExpireHvRunLeasesCommand, so a probe ends the same way whichever unit ends it, as HvRetryDecision does for a review (R-10). It is the only writer of a reviewer's readiness and probe request on behalf of a probe run. First the configuration check: a run whose `ReviewerConfigurationVersion` is not the reviewer's `ConfigurationVersion` changes nothing of the reviewer (readiness, reason, resolved model and effort, probe request, open incident), inserts no reviewer incident, and requests no probe; a stale close ends its run `Failed` with `probe_stale_configuration`, and a stale failure or expiry keeps the run outcome its caller recorded. Otherwise, by the outcome its caller hands it: a pass (from ReadinessProbeVerdict) makes the run `Succeeded` and the reviewer `Online` with `ResolvedModel` and `ResolvedEffort`, its `ReadinessReason` and `OpenIncidentId` cleared (the incident row stays), and appends `reviewer_online`; a reviewer-attributable code (FailHvRunCommand's one attribution list) makes the reviewer `Offline` with `ReadinessReason` the code and an `HvIncident` `hv-reviewer-<reviewerId>-<code>` as `OpenIncidentId`, and appends `reviewer_offline`; `evidence_corrupt` is a server fault: an `HvIncident` `hv-server-evidence_corrupt`, and the readiness unchanged; `worker_shutdown` and `lease_expired` are no outcome (the worker stopped or died), so `ProbeRequestedUtc` is set again and the same configuration is probed again, with no incident for `worker_shutdown` and the sweep's server incident for `lease_expired`. A probe that ended with a pass, a reviewer code, or `evidence_corrupt` is the settled probe of its configuration: process HvWorkerHost requests a startup NONCE only for an enabled `Unverified` reviewer whose current configuration has no settled probe (the run rows' `Kind`, `ReviewerConfigurationVersion`, `Status`, and `FailureCode` show it), so a restart never retries a settled probe, and a caller asks for a new one (S-15).

![Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 1 of 5](c4/rendered/HvProbeOutcome.svg)

View: HvProbeOutcome

![Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 2 of 5](c4/rendered/HvProbeOutcome-part2.svg)

View: HvProbeOutcome-part2

![Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 3 of 5](c4/rendered/HvProbeOutcome-part3.svg)

View: HvProbeOutcome-part3

![Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 4 of 5](c4/rendered/HvProbeOutcome-part4.svg)

View: HvProbeOutcome-part4

![Policy HvProbeOutcome: settle a probe run and its reviewer (activity), part 5 of 5](c4/rendered/HvProbeOutcome-part5.svg)

View: HvProbeOutcome-part5

### Policy HvRetryDecision: requeue, fail, or cancel [FR-MCP-HVPIPE-007, -009]

Applied inside the caller's transaction by FailHvRunCommand and ExpireHvRunLeasesCommand for a review run's request, and by CloseHvRunOutputCommand for a review run whose request is `CancelRequested` at close; it decides the request's next state and writes it, so a request ends the same way whether its attempt failed, its worker died, or its cancel was found at close. It acts only on a request whose current attempt is the caller's run (the run's `Attempt` equals the request's `AttemptCount`, and the request is `Running` or `CancelRequested`, or `Finalizing` for the finalizer's FailHvRunCommand); any other request is left unchanged and the caller's own run outcome stands, so no late or repeated caller requeues a newer attempt or revives a terminal request. A `CancelRequested` request becomes `Canceled`; otherwise, with the counted attempts (the request's runs except those that ended `worker_shutdown`, which a graceful restart causes and no reviewer does) below `MaxAttemptsPerRequest` the request returns to `Queued` (its assignee re-evaluated at the next claim, R-07); otherwise it becomes `Failed` with `attempts_exhausted`. A canceled or failed request gets `CompletedUtc`, releases its turns (HvTurnFreeze `Release`), closes its `Pending` responses with `request_ended`, and appends `hv_canceled` or `hv_failed` (NotificationOutbox).

![Policy HvRetryDecision: requeue, fail, or cancel (activity), part 1 of 5](c4/rendered/HvRetryDecision.svg)

View: HvRetryDecision

![Policy HvRetryDecision: requeue, fail, or cancel (activity), part 2 of 5](c4/rendered/HvRetryDecision-part2.svg)

View: HvRetryDecision-part2

![Policy HvRetryDecision: requeue, fail, or cancel (activity), part 3 of 5](c4/rendered/HvRetryDecision-part3.svg)

View: HvRetryDecision-part3

![Policy HvRetryDecision: requeue, fail, or cancel (activity), part 4 of 5](c4/rendered/HvRetryDecision-part4.svg)

View: HvRetryDecision-part4

![Policy HvRetryDecision: requeue, fail, or cancel (activity), part 5 of 5](c4/rendered/HvRetryDecision-part5.svg)

View: HvRetryDecision-part5

### Reused policy HvVerdictMapping

Foundation section 5.3, unchanged in its rule; its only caller is now RecordSessionTurnReviewCommand dispatched by FinalizeHvRequestCommand with origin `HvFinalization` (A-06).

## 16. Processes

### 16.1 Process HvWorkerHost: the durable worker [FR-MCP-HVPIPE-007, -008, -009]

A hosted `BackgroundService` per workspace application (`src/QBrainAi.Services/HostileValidation/Execution/HvWorkerHost.cs`), with a worker id of the host name, process id, and a random suffix. It is the only dispatcher of the internal units (R-29). At start, while `WorkerEnabled`, it requests a NONCE for every enabled `Unverified` reviewer whose current configuration has no settled probe (StartHvReviewerNonceCommand, R-10, policy HvProbeOutcome), so a probe that already settled, for example on a server fault, is not retried by a restart; a request refused with 503 `audit_unavailable` or a retryable `conflict` is made again at the next iteration, after the sweep, and a repeat that finds the probe requested or running changes nothing (202 `AlreadyRequested` or `Running`); a repeat answered 409 `hv_reviewer_disabled` (the reviewer was disabled meanwhile) is not made again. Then it loops, woken by an `IChangeEventBus` wake-up or at `PollInterval`: a sweep (ExpireHvRunLeasesCommand, FinalizeHvRequestCommand for every `Finalizing` request it lists, FileHvIncidentCommand for every `Pending` incident), then claims until ClaimHvRunCommand returns null, starting one process RunReviewerProcess per claim. On a graceful shutdown it stops claiming and ends each of its running runs with FailHvRunCommand code `worker_shutdown`, which files no incident, marks no reviewer offline, and does not count as an attempt (policy HvRetryDecision); after a crash the leases expire instead (W-03). A 503 `audit_unavailable` from any unit stops the loop iteration and the next iteration starts from the sweep (W-12).

![Process HvWorkerHost: the durable worker (activity), part 1 of 8](c4/rendered/HvWorkerHost.svg)

View: HvWorkerHost

![Process HvWorkerHost: the durable worker (activity), part 2 of 8](c4/rendered/HvWorkerHost-part2.svg)

View: HvWorkerHost-part2

![Process HvWorkerHost: the durable worker (activity), part 3 of 8](c4/rendered/HvWorkerHost-part3.svg)

View: HvWorkerHost-part3

![Process HvWorkerHost: the durable worker (activity), part 4 of 8](c4/rendered/HvWorkerHost-part4.svg)

View: HvWorkerHost-part4

![Process HvWorkerHost: the durable worker (activity), part 5 of 8](c4/rendered/HvWorkerHost-part5.svg)

View: HvWorkerHost-part5

![Process HvWorkerHost: the durable worker (activity), part 6 of 8](c4/rendered/HvWorkerHost-part6.svg)

View: HvWorkerHost-part6

![Process HvWorkerHost: the durable worker (activity), part 7 of 8](c4/rendered/HvWorkerHost-part7.svg)

View: HvWorkerHost-part7

![Process HvWorkerHost: the durable worker (activity), part 8 of 8](c4/rendered/HvWorkerHost-part8.svg)

View: HvWorkerHost-part8

### 16.2 Process RunReviewerProcess: one attempt, live [FR-MCP-HVPIPE-008, -016]

Runs one claimed run. It builds the brief (section 13.3: the accepted request read from the `Request` evidence inside the run envelope, after AuditLedgerGuard `RecordRead` records that read, or the probe brief with its nonce, which reads no evidence); a refused read starts no process and ends the attempt without another write, so the lease expiry recovers it (W-02, W-12); a `Request` evidence that fails to decompress starts no process and fails the run with `evidence_corrupt` (FailHvRunCommand: a server fault, so a server incident and the reviewer unchanged, and the request through policy HvRetryDecision, whose attempts bound the retries, FR-MCP-HVPIPE-011-AC009). It asks the reviewer's adapter for the launch (section 16.3), starts the process with the workspace as its working directory and its plugin as a host session of its own (R-24), inside a containment bound to the worker process, so the reviewer and every descendant end when the worker dies, however abruptly (W-03; the containment, for example a process group or job object, is the adapter's, section 16.3), records the start (RecordHvRunStartCommand), and appends the `hv_brief` record as sequence 1. While the process runs it has four concurrent duties and no time limit (R-11, P-02): every line the adapter delivers goes into a buffer that is appended (AppendHvRunOutputCommand) when `OutputFlushBytes` accumulate; a flush timer appends a buffer that is not empty once `OutputFlushInterval` has passed since its first line, whether or not another line arrives, so a single line followed by silence is still appended within the interval (R-12); a heartbeat every `HeartbeatInterval` (RecordHvRunHeartbeatCommand) renews the lease and reads cancellation; and the process exit is awaited. `stale_fence` from any unit kills the process and ends the attempt without another write (W-03); `audit_unavailable` kills the process and ends the attempt (W-12); `evidence_corrupt` from an append (its round trip failed and nothing was saved) kills the process and fails the run with `evidence_corrupt` (FailHvRunCommand: a server fault, so a server incident and the reviewer unchanged, and the request through policy HvRetryDecision), and so does a non-retryable `conflict` from an append (a different batch under a stored sequence, or a sequence out of order), because the log's integrity can no longer be shown, and so does a 400 `validation_error` from an append, which only a worker defect can cause because the adapter wraps every line as one JSON object (section 16.3); a retryable `conflict` repeats the same unit call with the same sequence. Every FailHvRunCommand call of this process, and the worker host's shutdown call, goes through the nested activity FailAttempt, drawn once below: `failed` ends the attempt with the request's next state (or the probe's outcome) as that unit wrote it; a retryable `conflict` repeats the same call; `stale_fence` and 503 `audit_unavailable` end the attempt with no further write and the request as it was, which the lease expiry recovers (W-03, W-12); so no exit of this process names a request state that a refused write did not produce. Every AppendHvRunOutputCommand call of this process (sequence 1 with the `hv_brief` record, each flush, and the final flush of the remaining lines and the provenance) goes through the nested activity AppendBatch, drawn once below: `appended` (or `unchanged`, a retried batch) lets the caller go on; a retryable `conflict` repeats the same call with the same sequence; `stale_fence` or 503 `audit_unavailable` stops the attempt with no further write; 500 `evidence_corrupt`, a non-retryable `conflict`, or a 400 `validation_error` stops it for integrity, and the caller ends the process and fails the run with `evidence_corrupt` through FailAttempt, so no later sequence is appended after a refused one. RecordHvRunStartCommand and RecordHvRunHeartbeatCommand repeat on a retryable `conflict`. A close answered 409 `conflict` with `details.field` `chunkCount` appends what is missing and closes again, and a retryable `conflict` repeats the close. Every append carries the records the adapter's extraction completed with that batch. On exit the remaining buffer is appended, the adapter's provenance lines are appended (section 16.3), and the run is closed (exit code 0, CloseHvRunOutputCommand, then FinalizeHvRequestCommand for a review whose request is `Finalizing`; a close that finds the request `CancelRequested` ends it `Canceled` instead, R-28; exit code 0 is not evidence of a review, R-11) or failed (FailHvRunCommand with `reviewer_exited_nonzero` and the exit code in its detail, or `reviewer_launch_failed` when the process never started). A cancellation read at a heartbeat kills the process and fails the run with `canceled`, which policy HvRetryDecision turns into the request's `Canceled` (R-28). Killing the process, here and in the diagrams, ends the reviewer's whole process tree, descendants included, also when the root process has already exited; and when the root exits while a descendant still holds its output pipes, the worker ends that descendant before it reads the output to its end. Without that, a descendant that outlives the reviewer would hold the run open forever, because no time limit ends a run (P-02); P14 is the same defect class in the plugin core (FR-MCP-HVPIPE-008-AC011).

![Process RunReviewerProcess: one attempt, live (activity), part 1 of 17](c4/rendered/RunReviewerProcess.svg)

View: RunReviewerProcess

![Process RunReviewerProcess: one attempt, live (activity), part 2 of 17](c4/rendered/RunReviewerProcess-part2.svg)

View: RunReviewerProcess-part2

![Process RunReviewerProcess: one attempt, live (activity), part 3 of 17](c4/rendered/RunReviewerProcess-part3.svg)

View: RunReviewerProcess-part3

![Process RunReviewerProcess: one attempt, live (activity), part 4 of 17](c4/rendered/RunReviewerProcess-part4.svg)

View: RunReviewerProcess-part4

![Process RunReviewerProcess: one attempt, live (activity), part 5 of 17](c4/rendered/RunReviewerProcess-part5.svg)

View: RunReviewerProcess-part5

![Process RunReviewerProcess: one attempt, live (activity), part 6 of 17](c4/rendered/RunReviewerProcess-part6.svg)

View: RunReviewerProcess-part6

![Process RunReviewerProcess: one attempt, live (activity), part 7 of 17](c4/rendered/RunReviewerProcess-part7.svg)

View: RunReviewerProcess-part7

![Process RunReviewerProcess: one attempt, live (activity), part 8 of 17](c4/rendered/RunReviewerProcess-part8.svg)

View: RunReviewerProcess-part8

![Process RunReviewerProcess: one attempt, live (activity), part 9 of 17](c4/rendered/RunReviewerProcess-part9.svg)

View: RunReviewerProcess-part9

![Process RunReviewerProcess: one attempt, live (activity), part 10 of 17](c4/rendered/RunReviewerProcess-part10.svg)

View: RunReviewerProcess-part10

![Process RunReviewerProcess: one attempt, live (activity), part 11 of 17](c4/rendered/RunReviewerProcess-part11.svg)

View: RunReviewerProcess-part11

![Process RunReviewerProcess: one attempt, live (activity), part 12 of 17](c4/rendered/RunReviewerProcess-part12.svg)

View: RunReviewerProcess-part12

![Process RunReviewerProcess: one attempt, live (activity), part 13 of 17](c4/rendered/RunReviewerProcess-part13.svg)

View: RunReviewerProcess-part13

![Process RunReviewerProcess: one attempt, live (activity), part 14 of 17](c4/rendered/RunReviewerProcess-part14.svg)

View: RunReviewerProcess-part14

![Process RunReviewerProcess: one attempt, live (activity), part 15 of 17](c4/rendered/RunReviewerProcess-part15.svg)

View: RunReviewerProcess-part15

![Process RunReviewerProcess: one attempt, live (activity), part 16 of 17](c4/rendered/RunReviewerProcess-part16.svg)

View: RunReviewerProcess-part16

![Process RunReviewerProcess: one attempt, live (activity), part 17 of 17](c4/rendered/RunReviewerProcess-part17.svg)

View: RunReviewerProcess-part17

##### Nested activity: FailAttempt (every FailHvRunCommand call of the worker, drawn once)

![Nested activity: FailAttempt (every FailHvRunCommand call of the worker, drawn once) (activity)](c4/rendered/FailAttempt.svg)

View: FailAttempt

##### Nested activity: AppendBatch (every AppendHvRunOutputCommand call of the worker, drawn once)

![Nested activity: AppendBatch (every AppendHvRunOutputCommand call of the worker, drawn once) (activity), part 1 of 2](c4/rendered/AppendBatch.svg)

View: AppendBatch

![Nested activity: AppendBatch (every AppendHvRunOutputCommand call of the worker, drawn once) (activity), part 2 of 2](c4/rendered/AppendBatch-part2.svg)

View: AppendBatch-part2

##### Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above)

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 1 of 10](c4/rendered/StreamReviewerOutput.svg)

View: StreamReviewerOutput

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 2 of 10](c4/rendered/StreamReviewerOutput-part2.svg)

View: StreamReviewerOutput-part2

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 3 of 10](c4/rendered/StreamReviewerOutput-part3.svg)

View: StreamReviewerOutput-part3

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 4 of 10](c4/rendered/StreamReviewerOutput-part4.svg)

View: StreamReviewerOutput-part4

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 5 of 10](c4/rendered/StreamReviewerOutput-part5.svg)

View: StreamReviewerOutput-part5

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 6 of 10](c4/rendered/StreamReviewerOutput-part6.svg)

View: StreamReviewerOutput-part6

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 7 of 10](c4/rendered/StreamReviewerOutput-part7.svg)

View: StreamReviewerOutput-part7

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 8 of 10](c4/rendered/StreamReviewerOutput-part8.svg)

View: StreamReviewerOutput-part8

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 9 of 10](c4/rendered/StreamReviewerOutput-part9.svg)

View: StreamReviewerOutput-part9

![Nested activity: StreamReviewerOutput (the loop of RunReviewerProcess, collapsed above) (activity), part 10 of 10](c4/rendered/StreamReviewerOutput-part10.svg)

View: StreamReviewerOutput-part10

### 16.3 Reviewer adapter contract [FR-MCP-HVPIPE-001, -003, -008]

`IHvReviewerAdapter` (namespace `QBrainAi.Support.Mcp.HostileValidation.Reviewers`), one implementation per reviewer id:

- `BuildLaunch(brief, reviewer)`: a `ProcessStartInfo` built as an argument list, never a shell string, with the brief on standard input or in a temporary file the adapter deletes after the run; the launch arguments come from one shared launch builder that develop's one-shot strategy also uses, so the per-CLI arguments of `OneShotCliAgentExecutionStrategy` (`AddCodexArguments`, `AddClaudeArguments`, `AddGrokArguments`, `AddClineArguments`, `OneShotCliAgentExecutionStrategy.cs:238-262`) are written once (DRY); processes start through the existing `IProcessSpawner` and `IProcessEnvironmentService` (`OneShotCliAgentExecutionStrategy.cs:18-21`), inside a containment the adapter creates at launch and binds to the worker process (for example a process group with a parent-death signal on Linux or a kill-on-close job object on Windows, settled in H1's contract step), so the reviewer and every descendant end when the worker dies, and so killing the run ends the whole tree even after the root has exited (W-03, FR-MCP-HVPIPE-007-AC008, FR-MCP-HVPIPE-008-AC011); develop's `DefaultProcessSpawner` starts an ordinary process and offers only an explicit kill (`src/QBrainAi.Common.AgentCli/DefaultProcessSpawner.cs:12-16,48-51`), so H1 adds the containment. The shared launch builder holds the one table of highest efforts of section 10 (P9). The reviewer runs behind a write barrier verified for its CLI (VI-03, probes of 2026-10-16): Codex `exec -s read-only` (a write attempt was refused by its sandbox), Grok `--permission-mode plan` (a write call was refused; a headless Grok run then ends `cancelled` at the first call that needs approval, so its launch allows the read tools a review uses), and Copilot `--available-tools` naming only read tools (write and shell tools are not offered at all). Claude's and Cline's plan modes are not accepted as barriers on that evidence (Claude's model declined before its barrier was exercised; Cline ran a shell command in plan mode), so Claude's launch restricts its tools structurally (`--tools`) and Cline stays unqualified until H1 proves a barrier. Whether each reviewer's plugin can still log its turn behind its barrier (the plugin writes host-session state and calls the server) is verified in H1 (VI-03, VI-04); a reviewer that cannot fails its NONCE (R-24).
- Live lines (R-12, P8): standard output and standard error are read line by line as the process writes them (asynchronous line reads on the redirected streams), and each line is handed to the run's callback at once; nothing waits for the process to end.
- `NormalizeLine(line, stream)`: a native JSON event line of a CLI run in a JSON streaming mode is kept as written; any other line becomes `{ "type": "hv_raw", "stream", "line" }`.
- `ExtractReviewerRecords(lines)`: one pure function over a run's ordered native lines. It assembles each agent message's complete text by the CLI's event model (Grok's per-token `text` events, Copilot's `assistant.message_delta` events followed by the whole `assistant.message`, Claude's whole assistant messages, Codex's `agent_message` items, Cline's accumulated content) and returns, in message order, every line of that text that is exactly one JSON object of a reviewer record type of section 13.3; the fence lines of a fenced block are ignored, and a line holding other text beside a record is free text. The worker runs it as lines arrive and passes each batch's completed records to AppendHvRunOutputCommand; finalization runs it over the stored log; both get the same records (R-12). Record types the worker writes itself (`hv_brief`, `hv_provenance`, `hv_raw`) are never accepted from message text, so a reviewer cannot forge them.
- `ReadProvenance(run)`: `hv_provenance` lines read from the CLI's own runtime receipt after the run (P-04), sources verified on 2026-10-16 (VI-01): for Codex, the thread's rollout `turn_context` (fields `model` and `effort`; observed `model` `gpt-6-astra`, `effort` `max`), the source the foundation's `runtime_identity` check reads (foundation section 6.3, check 8); for Claude, the session transcript under `~/.claude/projects` (the assistant message `model` and the request `effort`), written only when session persistence is on, so the launch never passes `--no-session-persistence`; for Grok, the session store `~/.grok/sessions/<cwd>/<sessionId>/` (`summary.json` `reasoning_effort`, and a model id that `chat_history.jsonl` names `grok-4.7-build` while the events name `grok-4.7`, so H1 pins the field V-7 compares); for Copilot, the run's own JSON stream (each call's `assistant.message.model` and `user.message.responsesReasoning.effort`; `--usage-output-file` holds the model only); for Cline, none, because its run records hold the model and only whether thinking was on, so V-7 and the NONCE fail closed (L-04, D-06).
- Highest effort when none is configured: the verified values of section 10 (VI-08), never develop's one-shot constants (P9).

### 16.4 Process SubmitHvFromPlugin: the plugin verb [FR-MCP-HVPIPE-005, -020]

`workflow.hv.submit` in the PowerShell core (`plugins/core/lib-ps/repl-invoke.ps1`): process BindVerbToTurn (foundation section 4.4) binds the invocation to its turn; the verb sends SubmitHvRequestCommand through the REPL with `submitter` that turn's key and `idempotencyKey` `hv-` followed by the first 32 lower-case hex characters of the SHA-256 of the canonical JSON of the whole invocation identity `{ submitter: { agent, sessionId, requestId }, scope, context, contextFormat }`, so the key has 35 characters whatever the length of the ids (within the 128 of section 12), two sessions with equal request ids get different keys, and a repeat of the same call is the same request. A created or stored request is reported with its id, status, and reviewer; the watcher needs no record of it, because a terminal notification carries the submitter's turn key (R-30, section 16.5). `hv_no_reviewer_available` is reported with the context S-16 asks for: each reviewer's enabled flag, readiness, and reason from `details.reviewers`, the open incident of each `Offline` reviewer (through `workflow.hv.incidents`), and the remedy text "after the cause is fixed, NONCE the reviewer: workflow.hv.nonce <reviewerId>". Every other refusal is reported with its code, final. A retryable answer (transport outage, 503, 409 with `retryable: true`) is reported with `retryable: true` and exit 1 and is not queued in the failsafe, because the same call with the same key can simply be repeated (the reviews' no-failsafe rule, foundation section 5.2).

![Process SubmitHvFromPlugin: the plugin verb (activity), part 1 of 4](c4/rendered/SubmitHvFromPlugin.svg)

View: SubmitHvFromPlugin

![Process SubmitHvFromPlugin: the plugin verb (activity), part 2 of 4](c4/rendered/SubmitHvFromPlugin-part2.svg)

View: SubmitHvFromPlugin-part2

![Process SubmitHvFromPlugin: the plugin verb (activity), part 3 of 4](c4/rendered/SubmitHvFromPlugin-part3.svg)

View: SubmitHvFromPlugin-part3

![Process SubmitHvFromPlugin: the plugin verb (activity), part 4 of 4](c4/rendered/SubmitHvFromPlugin-part4.svg)

View: SubmitHvFromPlugin-part4

### 16.5 Process WatchHvNotifications: completions into the plugin [FR-MCP-HVPIPE-017, -020]

Started by process StartHostSession (foundation section 4.4) for each host session and stopped by EndHostSession; one instance per host session (`sessions/<hostSessionKey>/hv-watcher.pid`, the arrival bridge's pattern). It runs the REPL streaming verb `workflow.hv.watchNotifications` with `Last-Event-ID` the `lastSequence` of `sessions/<hostSessionKey>/hv-notifications.yaml`. That one state file holds both `lastSequence` and the `inbox`, and every change to it is one atomic replace (temp file, flush, rename) under `hv-notifications.lock`. For each event the watcher writes, in that one replace, the new `lastSequence` and, when the event is `hv_completed`, `hv_failed`, or `hv_canceled` and its submitter session is this host session's MCP session (the submitter turn key the notification carries, section 12), an inbox entry keyed by the event's sequence; so a crash leaves either both or neither, and an event replayed after a crash is not added twice. Matching by the carried key needs no record of the submission, so a completion that arrives before the submit verb has returned is still inboxed. A disconnect reconnects with backoff from the recorded sequence, so nothing is missed (R-16). The prompt hook adds each undelivered inbox entry to the next prompt's context ("HV <hvRequestId> <status>, overall verdict <verdict>, sequence <n>; result: workflow.hv.result <hvRequestId>") and then marks it delivered in the same state file; a crash between the two repeats the entry at a later prompt, so delivery to the model is at least once, and the sequence makes a repeat recognizable (R-30). Plugins never call REST; the REPL holds the stream (R-30).

![Process WatchHvNotifications: completions into the plugin (activity), part 1 of 5](c4/rendered/WatchHvNotifications.svg)

View: WatchHvNotifications

![Process WatchHvNotifications: completions into the plugin (activity), part 2 of 5](c4/rendered/WatchHvNotifications-part2.svg)

View: WatchHvNotifications-part2

![Process WatchHvNotifications: completions into the plugin (activity), part 3 of 5](c4/rendered/WatchHvNotifications-part3.svg)

View: WatchHvNotifications-part3

![Process WatchHvNotifications: completions into the plugin (activity), part 4 of 5](c4/rendered/WatchHvNotifications-part4.svg)

View: WatchHvNotifications-part4

![Process WatchHvNotifications: completions into the plugin (activity), part 5 of 5](c4/rendered/WatchHvNotifications-part5.svg)

View: WatchHvNotifications-part5

### 16.6 Director HV commands [FR-MCP-HVPIPE-020]

The Director commands of section 13.1 run through `IHostileValidationDirectorExecutor` in `src/QBrainAi.Cqrs.Mvvm/HostileValidationDirectorCommands.cs`, the pattern of the existing hostile-review Director commands (`src/QBrainAi.Cqrs.Mvvm/HostileReviewDirectorCommands.cs:22`); `hv-output` and `hv-notifications` consume the client streams and reconnect from their last event id; `hv-submit` submits as `Operator` with the operator's name (R-09). Each command is a thin call of one client method and carries no logic of its own.

### 16.7 Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02)

The HostileReview subsystem keeps its own bounded context, queue, entities, and surfaces (R-25); this section is the contract of its missing worker, fixed in H3. The requirements are the stored FR-MCP-HOSTILEREVIEW-002 and -003 with their texts unchanged; H0 writes the named tests below for their criteria, against mocks first, and H3 binds them to production.

- Host: a hosted `BackgroundService` of its own per workspace application (`src/QBrainAi.Services/HostileReview/Execution/HostileReviewWorkerHost.cs`), not HvWorkerHost, looping on a wake-up or `PollInterval`: ExpireHostileReviewLeasesCommand, then ClaimHostileReviewAttemptCommand until it returns null or `MaximumConcurrentReviews` attempts run, running each claimed attempt with a renewal every `RenewalInterval` (RenewHostileReviewAttemptCommand) until the reviewer exits, and then completing it. It reads the lease, renewal, and attempt options `HostileReviewWorkerOptions` already declares (`LeaseDuration`, `RenewalInterval`, `MaximumAttempts`, `PollInterval`, `MaximumConcurrentReviews`), which nothing reads today (P1). It shares with this pipeline only the reviewer adapters' launch builder, live-line path, containment, and provenance reader (section 16.3) and FencedLease (section 11.1).
- Agent: the agent, model, and effort configured for hostile review when they are configured, and otherwise the pooled default agent with its default model and effort (FR-MCP-HOSTILEREVIEW-003 criterion 4, store text); P5's part for this worker removes the single-reviewer pin of `HostileReviewWorkerOptions.Validate` in H3 for this rule. HV's reviewer pool, round-robin, and NONCE do not apply.
- No time limit: an artifact review runs until the reviewer exits, with no execution timeout (P-02, R-11); the options' `ExecutionTimeout` is not read. Liveness is the attempt's lease, renewed while the reviewer runs.
- Storage (the subsystem keeps its entities and their single-column keys, R-25; in `src/QBrainAi.Storage/Entities/`): `HostileReviewAttemptEntity` (table `HostileReviewAttempts`, key `AttemptId` (128), with `WorkspaceId` and the root id `RequestId`, a foreign key to the request like the subsystem's execution and diagnostic rows, `HostileReviewExecutionEntity.cs:63-65`): `AttemptNumber` (int), `PoolJobId` (128), `ExecutionId` (128, written only by CompleteHostileReviewAttemptCommand together with the execution row it names, so it never names a row that does not exist; null for an attempt that launched no process, was abandoned, or whose provenance showed no model or effort), `Status` (16: `Claimed`, `Running`, `Succeeded`, `Failed`, `Abandoned`, `Canceled`), `LeaseOwner` (256), `LeaseExpiresUtc`, `FenceToken` (long), `ClaimedUtc`, `EndedUtc`, `FailureCode` (64, sanitized), `IsDeleted`; unique filtered (`WorkspaceId`, `RequestId`) where `Status` is `Claimed` or `Running`, one live attempt per request. `HostileReviewRequestEntity` gains `AcceptedResultId` (128, set once, a key of its own, with the request's existing `Verdict` and findings as the accepted result), `AttemptCount` (int), and `IsDeleted`; its `Status` takes `Queued`, `Running`, `Completed`, and `Failed`. The existing execution row (`HostileReviewExecutionEntity`) is the model execution, one per launched process whose provenance shows its model and effort, written at its completion.
- Internal result types (`src/QBrainAi.Services/HostileReview/Execution/`): `HostileReviewAttemptClaim { attemptId, requestId, attemptNumber, poolJobId, fenceToken, leaseExpiresUtc }`; `HostileReviewRenewResult { leaseExpiresUtc }`; `HostileReviewAttemptOutcome { kind (Success, Failure, Canceled), execution, verdict, findings, failureCode, diagnostics, counted }` (the parsed result and sanitized diagnostics only; never an artifact, a prompt, or an output line; `execution` null when no process started or its provenance showed no model or effort; `diagnostics` stored whatever the `kind`); `HostileReviewExecutionMetadata { executionId, reviewerAgent, model, effort, sourceSurface, promptTemplateId, promptVersion, startedUtc, endedUtc, inputTokens, outputTokens }` (the columns of `HostileReviewExecutionEntity`, `src/QBrainAi.Storage/Entities/HostileReviewExecutionEntity.cs:10-61`, filled as the next item states); `HostileReviewAttemptResult { status (accepted, alreadyAccepted, requeued, failed), acceptedResultId }`; `HostileReviewSweepResult { abandonedAttemptIds }`.
- Execution metadata (FR-MCP-HOSTILEREVIEW-003 criteria 1 to 3), produced by the worker for every launched process and carried as the outcome's `execution` only when its provenance shows the model and effort: `executionId`, created when the worker launches the process; `reviewerAgent` and `sourceSurface`, the selected agent and its adapter's id; `promptTemplateId` and `promptVersion`, the worker's prompt template; `startedUtc` and `endedUtc`, the process start and exit as the worker observed them; `model` and `effort`, the values actually used, read after the exit by the adapter's `ReadProvenance` (section 16.3, the source V-7 reads for HV, P-04), never the configured ones; `inputTokens` and `outputTokens` when the CLI reports them, otherwise null with the diagnostic `token_metadata_unavailable` (criterion 3: omitted with a diagnostic, never fabricated). Because criteria 1 and 2 record the model and effort actually used, an agent with no reviewer adapter (section 16.3 has one per reviewer id), or whose adapter has no provenance source for them (Cline today, L-04 and D-06), launches no process and completes the attempt as a failure with `runtime_identity_unavailable`, and a run whose provenance shows no model or effort completes as the same failure, with its execution id, agent, and times in the diagnostic, no execution row, and the attempt's `ExecutionId` null. None of this depends on a renewal: a process that exits before its first renewal is completed from `Claimed` with the same writes (`HostileReviewFastExitRecordsExecutionAndTerminalFields`, `HostileReviewLaunchWithoutProvenanceLeavesNoExecutionRow`). The diagnostics of every outcome, success included (for example `token_metadata_unavailable` beside an accepted result), are stored by the completion.
- Identities, distinct as criterion 4 requires: the request (`RequestId`); one attempt per claim (`AttemptId`, with lease, fence, and attempt number); the AgentPool job that runs it (`PoolJobId`); one model execution per launched process whose provenance shows its model and effort (`ExecutionId`, which is the run id criterion 2 names, the execution row with model, effort, agent, prompt template and version, and times, criteria 1 to 3); and the one accepted terminal result (`AcceptedResultId`).
- Storage boundary (FR-MCP-HOSTILEREVIEW-002 criterion 4): the worker assembles the prompt in memory from links it reauthorizes at dispatch, reads the reviewer's output lines only to parse the result, and stores neither the prompt nor any output line nor any artifact content: only normalized identity, type, version or content hash, the parsed result, and sanitized diagnostics reach queue rows, notifications, logs, receipts, or result metadata.
- Links (P3): every artifact type FR-MCP-HOSTILEREVIEW-002 lists resolves through its supported workspace-scoped service, the stored hash is of the resolved content (never of its id), and the limits of criterion 4 refuse more than 64 links, an artifact over 1 MiB decoded, aggregate input over 8 MiB, or uninspectable content. A link that fails its reauthorization at dispatch (missing, unauthorized, stale, or ambiguous, criteria 1, 2, and 4), or whose resolved content now exceeds a limit, launches no process: the attempt completes as a failure whose sanitized diagnostic names the link's normalized identity and the code, and no link is ever left out of a prompt silently.
- Execution: bounded at-least-once across process loss (an expired lease abandons the attempt and a new one runs, up to `MaximumAttempts` counted attempts); the reviewer runs contained with its worker (W-03), and a stopped attempt ends its whole process tree (section 16.2). Cancellation is internal: a graceful host or workspace shutdown ends the attempt `Canceled` with `worker_shutdown`, which is not counted, and the existing authorized AgentPool cancellation of its pool job ends it `Canceled` with `canceled`, which is counted (counted attempts are a request's attempts except those that ended `worker_shutdown`, as for HV, policy HvRetryDecision); either way the request is queued again while counted attempts remain and otherwise fails; there is no public cancel mutation.
- Caller handling of every result: Claim null waits for the next wake-up; Renew `stale_fence` (the sweep took the attempt over) stops the reviewer and ends the attempt with no further write; Renew 503 `audit_unavailable` stops the reviewer and leaves the attempt to the lease expiry; a retryable `conflict` repeats the same call; every Complete call goes through the nested activity CompleteAttempt, drawn once below: `accepted`, `alreadyAccepted`, `requeued`, `failed`, and `stale_fence` end the attempt, a retryable `conflict` repeats the call, and 503 `audit_unavailable` ends it with the attempt left to the lease expiry.
- Audit (P4): every unit below calls AuditLedgerGuard first, and the HostileReview entities gain `IsDeleted` in H3, so each of their writes reaches the ledger and an unwritable ledger stops the unit.
- One accepted terminal result (P2): only CompleteHostileReviewAttemptCommand writes a result, and only while the request has none; a duplicate or late completion returns the accepted result and changes nothing, even when its fence is stale, and the sweep never changes a request that has an accepted result, replacing `AcceptReviewerOutputAsync`'s unfenced overwrite.
- Named tests (H0 against mocks, H3 production, recorded on TEST-MCP-HOSTILEREVIEW-002 and -003 in H0 after approval, D-09): `HostileReviewWorkerUsesConfiguredOrPooledDefaultAgent`, `HostileReviewIdentitiesDistinctRequestAttemptPoolJobExecutionResult`, `HostileReviewAttemptRerunsAfterProcessLossBounded`, `HostileReviewDuplicateOrLateCompletionOneAcceptedResult`, `HostileReviewCancellationInternalOnly`, `HostileReviewLongRunRenewsLeaseNoTimeLimit`, `HostileReviewCompletionVersusSweepOneOutcome`, `HostileReviewSweepNeverChangesAcceptedRequest`, `HostileReviewShutdownRequeuesUncounted` (FR-MCP-HOSTILEREVIEW-003 criterion 4); `HostileReviewFastExitRecordsExecutionAndTerminalFields`, `HostileReviewExecutionMetadataFromProvenanceMissingTokensDiagnosed`, `HostileReviewAgentWithoutProvenanceFailsBeforeLaunch`, `HostileReviewLaunchWithoutProvenanceLeavesNoExecutionRow`, `HostileReviewEveryOutcomeStoresItsDiagnostics` (FR-MCP-HOSTILEREVIEW-003 criteria 1 to 3); `HostileReviewStoresNoRawArtifactPromptOrOutput`, `HostileReviewLinkLimitsRefused`, `HostileReviewEveryListedArtifactTypeResolvesWithContentHash`, `HostileReviewDispatchRefusedLinkFailsWithDiagnosticNeverOmitted` (FR-MCP-HOSTILEREVIEW-002 criteria 2 and 4 and P3).

![Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02) (activity), part 1 of 4](c4/rendered/HostileReviewWorkerHost.svg)

View: HostileReviewWorkerHost

![Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02) (activity), part 2 of 4](c4/rendered/HostileReviewWorkerHost-part2.svg)

View: HostileReviewWorkerHost-part2

![Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02) (activity), part 3 of 4](c4/rendered/HostileReviewWorkerHost-part3.svg)

View: HostileReviewWorkerHost-part3

![Process HostileReviewWorkerHost: the artifact worker of FR-MCP-HOSTILEREVIEW-003 (P1 to P3, D-02) (activity), part 4 of 4](c4/rendered/HostileReviewWorkerHost-part4.svg)

View: HostileReviewWorkerHost-part4

##### Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost)

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 1 of 12](c4/rendered/RunHostileReviewAttempt.svg)

View: RunHostileReviewAttempt

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 2 of 12](c4/rendered/RunHostileReviewAttempt-part2.svg)

View: RunHostileReviewAttempt-part2

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 3 of 12](c4/rendered/RunHostileReviewAttempt-part3.svg)

View: RunHostileReviewAttempt-part3

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 4 of 12](c4/rendered/RunHostileReviewAttempt-part4.svg)

View: RunHostileReviewAttempt-part4

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 5 of 12](c4/rendered/RunHostileReviewAttempt-part5.svg)

View: RunHostileReviewAttempt-part5

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 6 of 12](c4/rendered/RunHostileReviewAttempt-part6.svg)

View: RunHostileReviewAttempt-part6

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 7 of 12](c4/rendered/RunHostileReviewAttempt-part7.svg)

View: RunHostileReviewAttempt-part7

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 8 of 12](c4/rendered/RunHostileReviewAttempt-part8.svg)

View: RunHostileReviewAttempt-part8

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 9 of 12](c4/rendered/RunHostileReviewAttempt-part9.svg)

View: RunHostileReviewAttempt-part9

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 10 of 12](c4/rendered/RunHostileReviewAttempt-part10.svg)

View: RunHostileReviewAttempt-part10

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 11 of 12](c4/rendered/RunHostileReviewAttempt-part11.svg)

View: RunHostileReviewAttempt-part11

![Nested activity: RunHostileReviewAttempt (one claimed attempt of HostileReviewWorkerHost) (activity), part 12 of 12](c4/rendered/RunHostileReviewAttempt-part12.svg)

View: RunHostileReviewAttempt-part12

##### Nested activity: CompleteAttempt (every CompleteHostileReviewAttemptCommand call of the worker, drawn once)

![Nested activity: CompleteAttempt (every CompleteHostileReviewAttemptCommand call of the worker, drawn once) (activity)](c4/rendered/CompleteAttempt.svg)

View: CompleteAttempt

#### ClaimHostileReviewAttemptCommand: claim the next queued artifact review [FR-MCP-HOSTILEREVIEW-003]

- Handler: `ClaimHostileReviewAttemptCommand(string WorkspacePath, string WorkerId) : ICommand<HostileReviewAttemptClaim?>`; internal, dispatched only by HostileReviewWorkerHost.
- Delegate (SerializableWrite with AuditLedgerGuard): the oldest `Queued` request with no accepted result, no live attempt, and fewer counted attempts than `MaximumAttempts`; a new attempt `Claimed` with `AttemptId`, the next attempt number, the AgentPool job (`PoolJobId`), and `ClaimedUtc`, FencedLease `Claim`; the request `Running` with `AttemptCount` plus 1. Nothing claimable returns null and writes nothing. Two workers never claim one request: the request's state and the one-live-attempt index are checked and written in the delegate.
- Results: a claim; null; 409 `conflict` (`retryable: true`); 503 `audit_unavailable`.

![ClaimHostileReviewAttemptCommand: claim the next queued artifact review (activity), part 1 of 4](c4/rendered/ClaimHostileReviewAttemptCommand.svg)

View: ClaimHostileReviewAttemptCommand

![ClaimHostileReviewAttemptCommand: claim the next queued artifact review (activity), part 2 of 4](c4/rendered/ClaimHostileReviewAttemptCommand-part2.svg)

View: ClaimHostileReviewAttemptCommand-part2

![ClaimHostileReviewAttemptCommand: claim the next queued artifact review (activity), part 3 of 4](c4/rendered/ClaimHostileReviewAttemptCommand-part3.svg)

View: ClaimHostileReviewAttemptCommand-part3

![ClaimHostileReviewAttemptCommand: claim the next queued artifact review (activity), part 4 of 4](c4/rendered/ClaimHostileReviewAttemptCommand-part4.svg)

View: ClaimHostileReviewAttemptCommand-part4

#### RenewHostileReviewAttemptCommand: keep a running artifact review's lease [FR-MCP-HOSTILEREVIEW-003]

- Handler: `RenewHostileReviewAttemptCommand(string WorkspacePath, string AttemptId, string WorkerId, long FenceToken, bool ProcessStarted) : ICommand<HostileReviewRenewResult>`; internal. The host calls it every `RenewalInterval` while the reviewer runs, with `ProcessStarted` true once the process has started.
- Delegate (SerializableWrite with AuditLedgerGuard): FencedLease `Renew`; the attempt must be `Claimed` or `Running`; with `ProcessStarted`, a `Claimed` attempt becomes `Running`. It never writes `ExecutionId`, which only the completion writes with its execution row.
- Results: renewed (`leaseExpiresUtc`); `stale_fence` (nothing changed; the host stops the reviewer); 409 `conflict` (`retryable: true`); 503 `audit_unavailable`.

![RenewHostileReviewAttemptCommand: keep a running artifact review's lease (activity), part 1 of 4](c4/rendered/RenewHostileReviewAttemptCommand.svg)

View: RenewHostileReviewAttemptCommand

![RenewHostileReviewAttemptCommand: keep a running artifact review's lease (activity), part 2 of 4](c4/rendered/RenewHostileReviewAttemptCommand-part2.svg)

View: RenewHostileReviewAttemptCommand-part2

![RenewHostileReviewAttemptCommand: keep a running artifact review's lease (activity), part 3 of 4](c4/rendered/RenewHostileReviewAttemptCommand-part3.svg)

View: RenewHostileReviewAttemptCommand-part3

![RenewHostileReviewAttemptCommand: keep a running artifact review's lease (activity), part 4 of 4](c4/rendered/RenewHostileReviewAttemptCommand-part4.svg)

View: RenewHostileReviewAttemptCommand-part4

#### CompleteHostileReviewAttemptCommand: accept one terminal result [FR-MCP-HOSTILEREVIEW-003]

- Handler: `CompleteHostileReviewAttemptCommand(string WorkspacePath, string AttemptId, string WorkerId, long FenceToken, HostileReviewAttemptOutcome Outcome) : ICommand<HostileReviewAttemptResult>`; internal.
- Delegate (SerializableWrite with AuditLedgerGuard), in this order: a request that already has an accepted result returns `alreadyAccepted` with it and writes nothing, whatever the caller's fence; then FencedLease `Guard` (`stale_fence`: nothing changed); the attempt must be `Claimed` or `Running`; the outcome's sanitized diagnostics as the request's existing diagnostic rows (`HostileReviewDiagnosticEntity`, code and message, `src/QBrainAi.Storage/Entities/HostileReviewDiagnosticEntity.cs:23-31`), whatever the outcome; when `Outcome.execution` is set, the execution row from it and the attempt's `ExecutionId`, written together (this unit is the only writer of both); then by outcome, each setting the attempt's `EndedUtc`: a success writes the result once (`AcceptedResultId`, `Verdict`, findings), makes the attempt `Succeeded` and the request `Completed`, and releases the lease (`accepted`); a failure, including one where no process started or the provenance showed no model or effort, makes the attempt `Failed` with its sanitized code and releases the lease, and the request returns to `Queued` while counted attempts remain (`requeued`) and otherwise becomes `Failed` (`failed`); a cancellation makes the attempt `Canceled` with `worker_shutdown` (not counted) or `canceled` (counted) as the outcome says, and the request is decided as for a failure.
- Results: `accepted`; `alreadyAccepted`; `requeued`; `failed`; `stale_fence`; 409 `conflict` (`retryable: true`); 503 `audit_unavailable`.

![CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 1 of 6](c4/rendered/CompleteHostileReviewAttemptCommand.svg)

View: CompleteHostileReviewAttemptCommand

![CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 2 of 6](c4/rendered/CompleteHostileReviewAttemptCommand-part2.svg)

View: CompleteHostileReviewAttemptCommand-part2

![CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 3 of 6](c4/rendered/CompleteHostileReviewAttemptCommand-part3.svg)

View: CompleteHostileReviewAttemptCommand-part3

![CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 4 of 6](c4/rendered/CompleteHostileReviewAttemptCommand-part4.svg)

View: CompleteHostileReviewAttemptCommand-part4

![CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 5 of 6](c4/rendered/CompleteHostileReviewAttemptCommand-part5.svg)

View: CompleteHostileReviewAttemptCommand-part5

![CompleteHostileReviewAttemptCommand: accept one terminal result (activity), part 6 of 6](c4/rendered/CompleteHostileReviewAttemptCommand-part6.svg)

View: CompleteHostileReviewAttemptCommand-part6

#### ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers [FR-MCP-HOSTILEREVIEW-003]

- Handler: `ExpireHostileReviewLeasesCommand(string WorkspacePath) : ICommand<HostileReviewSweepResult>`; internal.
- For each attempt read as `Claimed` or `Running` with an expired lease, one SerializableWrite with AuditLedgerGuard and FencedLease `Expire` with the fence the sweep read and the live statuses `Claimed` and `Running` (skipped when it renewed, completed, or was already taken over by another sweep), the same operation the HV sweep uses (section 11.1); the attempt becomes `Abandoned` with `EndedUtc` and `FailureCode` `lease_expired`, and its incremented fence makes the old worker's renewal or completion `stale_fence`; the request is then left unchanged when it has an accepted result, and otherwise returns to `Queued` while counted attempts remain and becomes `Failed` when they are spent.
- Results: the sweep result; 503 `audit_unavailable` ends the sweep.

![ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 1 of 7](c4/rendered/ExpireHostileReviewLeasesCommand.svg)

View: ExpireHostileReviewLeasesCommand

![ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 2 of 7](c4/rendered/ExpireHostileReviewLeasesCommand-part2.svg)

View: ExpireHostileReviewLeasesCommand-part2

![ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 3 of 7](c4/rendered/ExpireHostileReviewLeasesCommand-part3.svg)

View: ExpireHostileReviewLeasesCommand-part3

![ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 4 of 7](c4/rendered/ExpireHostileReviewLeasesCommand-part4.svg)

View: ExpireHostileReviewLeasesCommand-part4

![ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 5 of 7](c4/rendered/ExpireHostileReviewLeasesCommand-part5.svg)

View: ExpireHostileReviewLeasesCommand-part5

![ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 6 of 7](c4/rendered/ExpireHostileReviewLeasesCommand-part6.svg)

View: ExpireHostileReviewLeasesCommand-part6

![ExpireHostileReviewLeasesCommand: recover artifact reviews from dead workers (activity), part 7 of 7](c4/rendered/ExpireHostileReviewLeasesCommand-part7.svg)

View: ExpireHostileReviewLeasesCommand-part7

## 17. Sequence diagrams

Participants: `Caller` is any surface's caller (a plugin through the REPL, the REPL, Director, an MCP client, or a REST client); `API` is the controller or MCP tool, which dispatches through `IDispatcher`; `Pipe` is WorkspaceResolutionBehavior; `H` is the unit's handler; `DB` is the relational store; `W` is process HvWorkerHost with RunReviewerProcess; `AD` is the reviewer adapter and its process; `TG` is `ITriageService`. Every sequence that dispatches draws the workspace refusal after the dispatch (foundation section 3, "Errors"); a body that does not bind keeps the existing validation problem. Shared components appear as one message.

### 17.1 ListHvReviewersQuery and GetHvReviewerQuery

![ListHvReviewersQuery and GetHvReviewerQuery, part 1 of 2](c4/rendered/AM1-17-01.svg)

View: AM1-17-01

![ListHvReviewersQuery and GetHvReviewerQuery, part 2 of 2](c4/rendered/AM1-17-01-part2.svg)

View: AM1-17-01-part2

![ListHvReviewersQuery and GetHvReviewerQuery, alt 1 branch 1 of 2: workspace unresolved](c4/rendered/AM1-17-01-alt1-1.svg)

View: AM1-17-01-alt1-1

![ListHvReviewersQuery and GetHvReviewerQuery, alt 1 branch 2 of 2: resolved, part 1 of 2](c4/rendered/AM1-17-01-alt1-2.svg)

View: AM1-17-01-alt1-2

![ListHvReviewersQuery and GetHvReviewerQuery, alt 1 branch 2 of 2: resolved, part 2 of 2](c4/rendered/AM1-17-01-alt1-2-part2.svg)

View: AM1-17-01-alt1-2-part2

![ListHvReviewersQuery and GetHvReviewerQuery, alt 2 branch 1 of 2: unknown reviewer id](c4/rendered/AM1-17-01-alt2-1.svg)

View: AM1-17-01-alt2-1

![ListHvReviewersQuery and GetHvReviewerQuery, alt 2 branch 2 of 2: known, part 1 of 2](c4/rendered/AM1-17-01-alt2-2.svg)

View: AM1-17-01-alt2-2

![ListHvReviewersQuery and GetHvReviewerQuery, alt 2 branch 2 of 2: known, part 2 of 2](c4/rendered/AM1-17-01-alt2-2-part2.svg)

View: AM1-17-01-alt2-2-part2

### 17.2 UpdateHvReviewerCommand

![UpdateHvReviewerCommand, part 1 of 2](c4/rendered/AM1-17-02.svg)

View: AM1-17-02

![UpdateHvReviewerCommand, part 2 of 2](c4/rendered/AM1-17-02-part2.svg)

View: AM1-17-02-part2

![UpdateHvReviewerCommand, alt 1 branch 1 of 2: workspace unresolved](c4/rendered/AM1-17-02-alt1-1.svg)

View: AM1-17-02-alt1-1

![UpdateHvReviewerCommand, alt 1 branch 2 of 2: resolved](c4/rendered/AM1-17-02-alt1-2.svg)

View: AM1-17-02-alt1-2

![UpdateHvReviewerCommand, alt 2 branch 1 of 3: invalid field or no expectedStateVersion](c4/rendered/AM1-17-02-alt2-1.svg)

View: AM1-17-02-alt2-1

![UpdateHvReviewerCommand, alt 2 branch 2 of 3: unknown reviewer id](c4/rendered/AM1-17-02-alt2-2.svg)

View: AM1-17-02-alt2-2

![UpdateHvReviewerCommand, alt 2 branch 3 of 3: valid](c4/rendered/AM1-17-02-alt2-3.svg)

View: AM1-17-02-alt2-3

![UpdateHvReviewerCommand, alt 3 branch 1 of 4: audit ledger unavailable, part 1 of 2](c4/rendered/AM1-17-02-alt3-1.svg)

View: AM1-17-02-alt3-1

![UpdateHvReviewerCommand, alt 3 branch 1 of 4: audit ledger unavailable, part 2 of 2](c4/rendered/AM1-17-02-alt3-1-part2.svg)

View: AM1-17-02-alt3-1-part2

![UpdateHvReviewerCommand, alt 3 branch 2 of 4: stale expectedStateVersion](c4/rendered/AM1-17-02-alt3-2.svg)

View: AM1-17-02-alt3-2

![UpdateHvReviewerCommand, alt 3 branch 3 of 4: attempts exhausted](c4/rendered/AM1-17-02-alt3-3.svg)

View: AM1-17-02-alt3-3

![UpdateHvReviewerCommand, alt 3 branch 4 of 4: committed, part 1 of 3](c4/rendered/AM1-17-02-alt3-4.svg)

View: AM1-17-02-alt3-4

![UpdateHvReviewerCommand, alt 3 branch 4 of 4: committed, part 2 of 3](c4/rendered/AM1-17-02-alt3-4-part2.svg)

View: AM1-17-02-alt3-4-part2

![UpdateHvReviewerCommand, alt 3 branch 4 of 4: committed, part 3 of 3](c4/rendered/AM1-17-02-alt3-4-part3.svg)

View: AM1-17-02-alt3-4-part3

### 17.3 StartHvReviewerNonceCommand and the probe it starts

![StartHvReviewerNonceCommand and the probe it starts, part 1 of 2](c4/rendered/AM1-17-03.svg)

View: AM1-17-03

![StartHvReviewerNonceCommand and the probe it starts, part 2 of 2](c4/rendered/AM1-17-03-part2.svg)

View: AM1-17-03-part2

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 1 of 6: unknown reviewer](c4/rendered/AM1-17-03-alt1-1.svg)

View: AM1-17-03-alt1-1

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 2 of 6: disabled](c4/rendered/AM1-17-03-alt1-2.svg)

View: AM1-17-03-alt1-2

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 3 of 6: probe already running](c4/rendered/AM1-17-03-alt1-3.svg)

View: AM1-17-03-alt1-3

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 4 of 6: probe already requested](c4/rendered/AM1-17-03-alt1-4.svg)

View: AM1-17-03-alt1-4

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 5 of 6: audit ledger unavailable](c4/rendered/AM1-17-03-alt1-5.svg)

View: AM1-17-03-alt1-5

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 1 of 11](c4/rendered/AM1-17-03-alt1-6.svg)

View: AM1-17-03-alt1-6

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 2 of 11](c4/rendered/AM1-17-03-alt1-6-part2.svg)

View: AM1-17-03-alt1-6-part2

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 3 of 11](c4/rendered/AM1-17-03-alt1-6-part3.svg)

View: AM1-17-03-alt1-6-part3

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 4 of 11](c4/rendered/AM1-17-03-alt1-6-part4.svg)

View: AM1-17-03-alt1-6-part4

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 5 of 11](c4/rendered/AM1-17-03-alt1-6-part5.svg)

View: AM1-17-03-alt1-6-part5

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 6 of 11](c4/rendered/AM1-17-03-alt1-6-part6.svg)

View: AM1-17-03-alt1-6-part6

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 7 of 11](c4/rendered/AM1-17-03-alt1-6-part7.svg)

View: AM1-17-03-alt1-6-part7

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 8 of 11](c4/rendered/AM1-17-03-alt1-6-part8.svg)

View: AM1-17-03-alt1-6-part8

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 9 of 11](c4/rendered/AM1-17-03-alt1-6-part9.svg)

View: AM1-17-03-alt1-6-part9

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 10 of 11](c4/rendered/AM1-17-03-alt1-6-part10.svg)

View: AM1-17-03-alt1-6-part10

![StartHvReviewerNonceCommand and the probe it starts, alt 1 branch 6 of 6: requested, part 11 of 11](c4/rendered/AM1-17-03-alt1-6-part11.svg)

View: AM1-17-03-alt1-6-part11

![StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 1 of 5: reviewer reconfigured while the probe ran](c4/rendered/AM1-17-03-alt2-1.svg)

View: AM1-17-03-alt2-1

![StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 2 of 5: probe passed](c4/rendered/AM1-17-03-alt2-2.svg)

View: AM1-17-03-alt2-2

![StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 3 of 5: nonce, provenance, or reviewer turn failed, or the launch failed or the process exited nonzero, part 1 of 3](c4/rendered/AM1-17-03-alt2-3.svg)

View: AM1-17-03-alt2-3

![StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 3 of 5: nonce, provenance, or reviewer turn failed, or the launch failed or the process exited nonzero, part 2 of 3](c4/rendered/AM1-17-03-alt2-3-part2.svg)

View: AM1-17-03-alt2-3-part2

![StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 3 of 5: nonce, provenance, or reviewer turn failed, or the launch failed or the process exited nonzero, part 3 of 3](c4/rendered/AM1-17-03-alt2-3-part3.svg)

View: AM1-17-03-alt2-3-part3

![StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 4 of 5: stored chunks fail V-1, part 1 of 2](c4/rendered/AM1-17-03-alt2-4.svg)

View: AM1-17-03-alt2-4

![StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 4 of 5: stored chunks fail V-1, part 2 of 2](c4/rendered/AM1-17-03-alt2-4-part2.svg)

View: AM1-17-03-alt2-4-part2

![StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 5 of 5: the read of the chunks cannot be recorded, part 1 of 2](c4/rendered/AM1-17-03-alt2-5.svg)

View: AM1-17-03-alt2-5

![StartHvReviewerNonceCommand and the probe it starts, alt 2 branch 5 of 5: the read of the chunks cannot be recorded, part 2 of 2](c4/rendered/AM1-17-03-alt2-5-part2.svg)

View: AM1-17-03-alt2-5-part2

### 17.4 SubmitHvRequestCommand from the plugin verb

![SubmitHvRequestCommand from the plugin verb, part 1 of 2](c4/rendered/AM1-17-04.svg)

View: AM1-17-04

![SubmitHvRequestCommand from the plugin verb, part 2 of 2](c4/rendered/AM1-17-04-part2.svg)

View: AM1-17-04-part2

![SubmitHvRequestCommand from the plugin verb, alt 1 branch 1 of 2: binding refused](c4/rendered/AM1-17-04-alt1-1.svg)

View: AM1-17-04-alt1-1

![SubmitHvRequestCommand from the plugin verb, alt 1 branch 2 of 2: bound, part 1 of 2](c4/rendered/AM1-17-04-alt1-2.svg)

View: AM1-17-04-alt1-2

![SubmitHvRequestCommand from the plugin verb, alt 1 branch 2 of 2: bound, part 2 of 2](c4/rendered/AM1-17-04-alt1-2-part2.svg)

View: AM1-17-04-alt1-2-part2

![SubmitHvRequestCommand from the plugin verb, alt 2 branch 1 of 2: workspace unresolved](c4/rendered/AM1-17-04-alt2-1.svg)

View: AM1-17-04-alt2-1

![SubmitHvRequestCommand from the plugin verb, alt 2 branch 2 of 2: resolved, part 1 of 4](c4/rendered/AM1-17-04-alt2-2.svg)

View: AM1-17-04-alt2-2

![SubmitHvRequestCommand from the plugin verb, alt 2 branch 2 of 2: resolved, part 2 of 4](c4/rendered/AM1-17-04-alt2-2-part2.svg)

View: AM1-17-04-alt2-2-part2

![SubmitHvRequestCommand from the plugin verb, alt 2 branch 2 of 2: resolved, part 3 of 4](c4/rendered/AM1-17-04-alt2-2-part3.svg)

View: AM1-17-04-alt2-2-part3

![SubmitHvRequestCommand from the plugin verb, alt 2 branch 2 of 2: resolved, part 4 of 4](c4/rendered/AM1-17-04-alt2-2-part4.svg)

View: AM1-17-04-alt2-2-part4

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 1 of 9: malformed scope or submitter](c4/rendered/AM1-17-04-alt3-1.svg)

View: AM1-17-04-alt3-1

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 2 of 9: scope too large, includes the submitting turn, or empty](c4/rendered/AM1-17-04-alt3-2.svg)

View: AM1-17-04-alt3-2

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 3 of 9: turns not terminal, pending, or already reviewed, part 1 of 2](c4/rendered/AM1-17-04-alt3-3.svg)

View: AM1-17-04-alt3-3

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 3 of 9: turns not terminal, pending, or already reviewed, part 2 of 2](c4/rendered/AM1-17-04-alt3-3-part2.svg)

View: AM1-17-04-alt3-3-part2

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 4 of 9: no eligible reviewer, part 1 of 2](c4/rendered/AM1-17-04-alt3-4.svg)

View: AM1-17-04-alt3-4

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 4 of 9: no eligible reviewer, part 2 of 2](c4/rendered/AM1-17-04-alt3-4-part2.svg)

View: AM1-17-04-alt3-4-part2

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 5 of 9: same key and same inputs, part 1 of 3](c4/rendered/AM1-17-04-alt3-5.svg)

View: AM1-17-04-alt3-5

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 5 of 9: same key and same inputs, part 2 of 3](c4/rendered/AM1-17-04-alt3-5-part2.svg)

View: AM1-17-04-alt3-5-part2

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 5 of 9: same key and same inputs, part 3 of 3](c4/rendered/AM1-17-04-alt3-5-part3.svg)

View: AM1-17-04-alt3-5-part3

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 6 of 9: the accepted request fails its GZIP round trip](c4/rendered/AM1-17-04-alt3-6.svg)

View: AM1-17-04-alt3-6

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 7 of 9: key reused with other inputs](c4/rendered/AM1-17-04-alt3-7.svg)

View: AM1-17-04-alt3-7

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 8 of 9: audit unavailable or attempts exhausted, part 1 of 2](c4/rendered/AM1-17-04-alt3-8.svg)

View: AM1-17-04-alt3-8

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 8 of 9: audit unavailable or attempts exhausted, part 2 of 2](c4/rendered/AM1-17-04-alt3-8-part2.svg)

View: AM1-17-04-alt3-8-part2

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 9 of 9: admitted, part 1 of 4](c4/rendered/AM1-17-04-alt3-9.svg)

View: AM1-17-04-alt3-9

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 9 of 9: admitted, part 2 of 4](c4/rendered/AM1-17-04-alt3-9-part2.svg)

View: AM1-17-04-alt3-9-part2

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 9 of 9: admitted, part 3 of 4](c4/rendered/AM1-17-04-alt3-9-part3.svg)

View: AM1-17-04-alt3-9-part3

![SubmitHvRequestCommand from the plugin verb, alt 3 branch 9 of 9: admitted, part 4 of 4](c4/rendered/AM1-17-04-alt3-9-part4.svg)

View: AM1-17-04-alt3-9-part4

### 17.5 GetHvRequestQuery and QueryHvRequestsQuery

![GetHvRequestQuery and QueryHvRequestsQuery, part 1 of 2](c4/rendered/AM1-17-05.svg)

View: AM1-17-05

![GetHvRequestQuery and QueryHvRequestsQuery, part 2 of 2](c4/rendered/AM1-17-05-part2.svg)

View: AM1-17-05-part2

![GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 1 of 4: one request, not found](c4/rendered/AM1-17-05-alt1-1.svg)

View: AM1-17-05-alt1-1

![GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 2 of 4: one request, found, part 1 of 2](c4/rendered/AM1-17-05-alt1-2.svg)

View: AM1-17-05-alt1-2

![GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 2 of 4: one request, found, part 2 of 2](c4/rendered/AM1-17-05-alt1-2-part2.svg)

View: AM1-17-05-alt1-2-part2

![GetHvRequestQuery and QueryHvRequestsQuery, alt 2 branch 1 of 4: output present and audit ledger unavailable](c4/rendered/AM1-17-05-alt2-1.svg)

View: AM1-17-05-alt2-1

![GetHvRequestQuery and QueryHvRequestsQuery, alt 2 branch 2 of 4: evidence removed](c4/rendered/AM1-17-05-alt2-2.svg)

View: AM1-17-05-alt2-2

![GetHvRequestQuery and QueryHvRequestsQuery, alt 2 branch 3 of 4: read recorded, and a last chunk fails to decompress](c4/rendered/AM1-17-05-alt2-3.svg)

View: AM1-17-05-alt2-3

![GetHvRequestQuery and QueryHvRequestsQuery, alt 2 branch 4 of 4: read recorded or no output](c4/rendered/AM1-17-05-alt2-4.svg)

View: AM1-17-05-alt2-4

![GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 3 of 4: query with an unknown filter or out-of-bounds page, part 1 of 2](c4/rendered/AM1-17-05-alt1-3.svg)

View: AM1-17-05-alt1-3

![GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 3 of 4: query with an unknown filter or out-of-bounds page, part 2 of 2](c4/rendered/AM1-17-05-alt1-3-part2.svg)

View: AM1-17-05-alt1-3-part2

![GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 4 of 4: query, part 1 of 2](c4/rendered/AM1-17-05-alt1-4.svg)

View: AM1-17-05-alt1-4

![GetHvRequestQuery and QueryHvRequestsQuery, alt 1 branch 4 of 4: query, part 2 of 2](c4/rendered/AM1-17-05-alt1-4-part2.svg)

View: AM1-17-05-alt1-4-part2

### 17.6 CancelHvRequestCommand

![CancelHvRequestCommand, part 1 of 3](c4/rendered/AM1-17-06.svg)

View: AM1-17-06

![CancelHvRequestCommand, part 2 of 3](c4/rendered/AM1-17-06-part2.svg)

View: AM1-17-06-part2

![CancelHvRequestCommand, part 3 of 3](c4/rendered/AM1-17-06-part3.svg)

View: AM1-17-06-part3

![CancelHvRequestCommand, alt 1 branch 1 of 6: not found](c4/rendered/AM1-17-06-alt1-1.svg)

View: AM1-17-06-alt1-1

![CancelHvRequestCommand, alt 1 branch 2 of 6: Finalizing, Completed, or Failed](c4/rendered/AM1-17-06-alt1-2.svg)

View: AM1-17-06-alt1-2

![CancelHvRequestCommand, alt 1 branch 3 of 6: already CancelRequested or Canceled](c4/rendered/AM1-17-06-alt1-3.svg)

View: AM1-17-06-alt1-3

![CancelHvRequestCommand, alt 1 branch 4 of 6: audit unavailable or attempts exhausted](c4/rendered/AM1-17-06-alt1-4.svg)

View: AM1-17-06-alt1-4

![CancelHvRequestCommand, alt 1 branch 5 of 6: Queued, part 1 of 2](c4/rendered/AM1-17-06-alt1-5.svg)

View: AM1-17-06-alt1-5

![CancelHvRequestCommand, alt 1 branch 5 of 6: Queued, part 2 of 2](c4/rendered/AM1-17-06-alt1-5-part2.svg)

View: AM1-17-06-alt1-5-part2

![CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 1 of 5](c4/rendered/AM1-17-06-alt1-6.svg)

View: AM1-17-06-alt1-6

![CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 2 of 5](c4/rendered/AM1-17-06-alt1-6-part2.svg)

View: AM1-17-06-alt1-6-part2

![CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 3 of 5](c4/rendered/AM1-17-06-alt1-6-part3.svg)

View: AM1-17-06-alt1-6-part3

![CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 4 of 5](c4/rendered/AM1-17-06-alt1-6-part4.svg)

View: AM1-17-06-alt1-6-part4

![CancelHvRequestCommand, alt 1 branch 6 of 6: Running, part 5 of 5](c4/rendered/AM1-17-06-alt1-6-part5.svg)

View: AM1-17-06-alt1-6-part5

![CancelHvRequestCommand, alt 2 branch 1 of 2: failed](c4/rendered/AM1-17-06-alt2-1.svg)

View: AM1-17-06-alt2-1

![CancelHvRequestCommand, alt 2 branch 2 of 2: stale_fence or audit_unavailable, part 1 of 2](c4/rendered/AM1-17-06-alt2-2.svg)

View: AM1-17-06-alt2-2

![CancelHvRequestCommand, alt 2 branch 2 of 2: stale_fence or audit_unavailable, part 2 of 2](c4/rendered/AM1-17-06-alt2-2-part2.svg)

View: AM1-17-06-alt2-2-part2

### 17.7 A review attempt, from claim to finalization

![A review attempt, from claim to finalization, part 1 of 2](c4/rendered/AM1-17-07.svg)

View: AM1-17-07

![A review attempt, from claim to finalization, part 2 of 2](c4/rendered/AM1-17-07-part2.svg)

View: AM1-17-07-part2

![A review attempt, from claim to finalization, alt 1 branch 1 of 2: nothing claimable](c4/rendered/AM1-17-07-alt1-1.svg)

View: AM1-17-07-alt1-1

![A review attempt, from claim to finalization, alt 1 branch 2 of 2: claimed, part 1 of 3](c4/rendered/AM1-17-07-alt1-2.svg)

View: AM1-17-07-alt1-2

![A review attempt, from claim to finalization, alt 1 branch 2 of 2: claimed, part 2 of 3](c4/rendered/AM1-17-07-alt1-2-part2.svg)

View: AM1-17-07-alt1-2-part2

![A review attempt, from claim to finalization, alt 1 branch 2 of 2: claimed, part 3 of 3](c4/rendered/AM1-17-07-alt1-2-part3.svg)

View: AM1-17-07-alt1-2-part3

![A review attempt, from claim to finalization, alt 2 branch 1 of 3: read refused, part 1 of 3](c4/rendered/AM1-17-07-alt2-1.svg)

View: AM1-17-07-alt2-1

![A review attempt, from claim to finalization, alt 2 branch 1 of 3: read refused, part 2 of 3](c4/rendered/AM1-17-07-alt2-1-part2.svg)

View: AM1-17-07-alt2-1-part2

![A review attempt, from claim to finalization, alt 2 branch 1 of 3: read refused, part 3 of 3](c4/rendered/AM1-17-07-alt2-1-part3.svg)

View: AM1-17-07-alt2-1-part3

![A review attempt, from claim to finalization, alt 2 branch 2 of 3: Request evidence fails to decompress](c4/rendered/AM1-17-07-alt2-2.svg)

View: AM1-17-07-alt2-2

![A review attempt, from claim to finalization, alt 2 branch 3 of 3: brief built, part 1 of 2](c4/rendered/AM1-17-07-alt2-3.svg)

View: AM1-17-07-alt2-3

![A review attempt, from claim to finalization, alt 2 branch 3 of 3: brief built, part 2 of 2](c4/rendered/AM1-17-07-alt2-3-part2.svg)

View: AM1-17-07-alt2-3-part2

![A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 1 of 7](c4/rendered/AM1-17-07-alt3-1.svg)

View: AM1-17-07-alt3-1

![A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 2 of 7](c4/rendered/AM1-17-07-alt3-1-part2.svg)

View: AM1-17-07-alt3-1-part2

![A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 3 of 7](c4/rendered/AM1-17-07-alt3-1-part3.svg)

View: AM1-17-07-alt3-1-part3

![A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 4 of 7](c4/rendered/AM1-17-07-alt3-1-part4.svg)

View: AM1-17-07-alt3-1-part4

![A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 5 of 7](c4/rendered/AM1-17-07-alt3-1-part5.svg)

View: AM1-17-07-alt3-1-part5

![A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 6 of 7](c4/rendered/AM1-17-07-alt3-1-part6.svg)

View: AM1-17-07-alt3-1-part6

![A review attempt, from claim to finalization, alt 3 branch 1 of 2: launch failed, part 7 of 7](c4/rendered/AM1-17-07-alt3-1-part7.svg)

View: AM1-17-07-alt3-1-part7

![A review attempt, from claim to finalization, alt 3 branch 2 of 2: started](c4/rendered/AM1-17-07-alt3-2.svg)

View: AM1-17-07-alt3-2

![A review attempt, from claim to finalization, alt 4 branch 1 of 3: stale fence or audit unavailable at the start, part 1 of 2](c4/rendered/AM1-17-07-alt4-1.svg)

View: AM1-17-07-alt4-1

![A review attempt, from claim to finalization, alt 4 branch 1 of 3: stale fence or audit unavailable at the start, part 2 of 2](c4/rendered/AM1-17-07-alt4-1-part2.svg)

View: AM1-17-07-alt4-1-part2

![A review attempt, from claim to finalization, alt 4 branch 2 of 3: retryable conflict at the start, part 1 of 2](c4/rendered/AM1-17-07-alt4-2.svg)

View: AM1-17-07-alt4-2

![A review attempt, from claim to finalization, alt 4 branch 2 of 3: retryable conflict at the start, part 2 of 2](c4/rendered/AM1-17-07-alt4-2-part2.svg)

View: AM1-17-07-alt4-2-part2

![A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 1 of 7](c4/rendered/AM1-17-07-alt4-3.svg)

View: AM1-17-07-alt4-3

![A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 2 of 7](c4/rendered/AM1-17-07-alt4-3-part2.svg)

View: AM1-17-07-alt4-3-part2

![A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 3 of 7](c4/rendered/AM1-17-07-alt4-3-part3.svg)

View: AM1-17-07-alt4-3-part3

![A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 4 of 7](c4/rendered/AM1-17-07-alt4-3-part4.svg)

View: AM1-17-07-alt4-3-part4

![A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 5 of 7](c4/rendered/AM1-17-07-alt4-3-part5.svg)

View: AM1-17-07-alt4-3-part5

![A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 6 of 7](c4/rendered/AM1-17-07-alt4-3-part6.svg)

View: AM1-17-07-alt4-3-part6

![A review attempt, from claim to finalization, alt 4 branch 3 of 3: start recorded, part 7 of 7](c4/rendered/AM1-17-07-alt4-3-part7.svg)

View: AM1-17-07-alt4-3-part7

![A review attempt, from claim to finalization, alt 5 branch 1 of 5: stale fence or audit unavailable during the run, part 1 of 2](c4/rendered/AM1-17-07-alt5-1.svg)

View: AM1-17-07-alt5-1

![A review attempt, from claim to finalization, alt 5 branch 1 of 5: stale fence or audit unavailable during the run, part 2 of 2](c4/rendered/AM1-17-07-alt5-1-part2.svg)

View: AM1-17-07-alt5-1-part2

![A review attempt, from claim to finalization, alt 5 branch 2 of 5: cancel requested, part 1 of 2](c4/rendered/AM1-17-07-alt5-2.svg)

View: AM1-17-07-alt5-2

![A review attempt, from claim to finalization, alt 5 branch 2 of 5: cancel requested, part 2 of 2](c4/rendered/AM1-17-07-alt5-2-part2.svg)

View: AM1-17-07-alt5-2-part2

![A review attempt, from claim to finalization, alt 5 branch 3 of 5: evidence_corrupt, a non-retryable conflict, or a 400 validation_error from an append, part 1 of 3](c4/rendered/AM1-17-07-alt5-3.svg)

View: AM1-17-07-alt5-3

![A review attempt, from claim to finalization, alt 5 branch 3 of 5: evidence_corrupt, a non-retryable conflict, or a 400 validation_error from an append, part 2 of 3](c4/rendered/AM1-17-07-alt5-3-part2.svg)

View: AM1-17-07-alt5-3-part2

![A review attempt, from claim to finalization, alt 5 branch 3 of 5: evidence_corrupt, a non-retryable conflict, or a 400 validation_error from an append, part 3 of 3](c4/rendered/AM1-17-07-alt5-3-part3.svg)

View: AM1-17-07-alt5-3-part3

![A review attempt, from claim to finalization, alt 5 branch 4 of 5: exited with a nonzero code](c4/rendered/AM1-17-07-alt5-4.svg)

View: AM1-17-07-alt5-4

![A review attempt, from claim to finalization, alt 5 branch 5 of 5: exited with code 0](c4/rendered/AM1-17-07-alt5-5.svg)

View: AM1-17-07-alt5-5

![A review attempt, from claim to finalization, alt 6 branch 1 of 5: request CancelRequested at close](c4/rendered/AM1-17-07-alt6-1.svg)

View: AM1-17-07-alt6-1

![A review attempt, from claim to finalization, alt 6 branch 2 of 5: 409 conflict with details.field chunkCount, part 1 of 2](c4/rendered/AM1-17-07-alt6-2.svg)

View: AM1-17-07-alt6-2

![A review attempt, from claim to finalization, alt 6 branch 2 of 5: 409 conflict with details.field chunkCount, part 2 of 2](c4/rendered/AM1-17-07-alt6-2-part2.svg)

View: AM1-17-07-alt6-2-part2

![A review attempt, from claim to finalization, alt 6 branch 3 of 5: retryable conflict at the close](c4/rendered/AM1-17-07-alt6-3.svg)

View: AM1-17-07-alt6-3

![A review attempt, from claim to finalization, alt 6 branch 4 of 5: stale fence or audit unavailable](c4/rendered/AM1-17-07-alt6-4.svg)

View: AM1-17-07-alt6-4

![A review attempt, from claim to finalization, alt 6 branch 5 of 5: otherwise](c4/rendered/AM1-17-07-alt6-5.svg)

View: AM1-17-07-alt6-5

#### Nested sequence: StreamOneAttempt (the live part of the review attempt)

![Nested sequence: StreamOneAttempt (the live part of the review attempt), part 1 of 2](c4/rendered/StreamOneAttempt.svg)

View: StreamOneAttempt

![Nested sequence: StreamOneAttempt (the live part of the review attempt), part 2 of 2](c4/rendered/StreamOneAttempt-part2.svg)

View: StreamOneAttempt-part2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), loop 1: until the process exits, a stale fence, an audit failure, evidence_corrupt, or a cancel, part 1 of 3](c4/rendered/StreamOneAttempt-loop1.svg)

View: StreamOneAttempt-loop1

![Nested sequence: StreamOneAttempt (the live part of the review attempt), loop 1: until the process exits, a stale fence, an audit failure, evidence_corrupt, or a cancel, part 2 of 3](c4/rendered/StreamOneAttempt-loop1-part2.svg)

View: StreamOneAttempt-loop1-part2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), loop 1: until the process exits, a stale fence, an audit failure, evidence_corrupt, or a cancel, part 3 of 3](c4/rendered/StreamOneAttempt-loop1-part3.svg)

View: StreamOneAttempt-loop1-part3

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 1 of 3: a line arrives, part 1 of 2](c4/rendered/StreamOneAttempt-alt1-1.svg)

View: StreamOneAttempt-alt1-1

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 1 of 3: a line arrives, part 2 of 2](c4/rendered/StreamOneAttempt-alt1-1-part2.svg)

View: StreamOneAttempt-alt1-1-part2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), opt 1: flush bytes reached, part 1 of 3](c4/rendered/StreamOneAttempt-opt1.svg)

View: StreamOneAttempt-opt1

![Nested sequence: StreamOneAttempt (the live part of the review attempt), opt 1: flush bytes reached, part 2 of 3](c4/rendered/StreamOneAttempt-opt1-part2.svg)

View: StreamOneAttempt-opt1-part2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), opt 1: flush bytes reached, part 3 of 3](c4/rendered/StreamOneAttempt-opt1-part3.svg)

View: StreamOneAttempt-opt1-part3

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 2 of 3: the flush timer fires with lines buffered, whether or not another line came, part 1 of 2](c4/rendered/StreamOneAttempt-alt1-2.svg)

View: StreamOneAttempt-alt1-2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 2 of 3: the flush timer fires with lines buffered, whether or not another line came, part 2 of 2](c4/rendered/StreamOneAttempt-alt1-2-part2.svg)

View: StreamOneAttempt-alt1-2-part2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 1 of 4: appended, part 1 of 2](c4/rendered/StreamOneAttempt-alt2-1.svg)

View: StreamOneAttempt-alt2-1

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 1 of 4: appended, part 2 of 2](c4/rendered/StreamOneAttempt-alt2-1-part2.svg)

View: StreamOneAttempt-alt2-1-part2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 2 of 4: retryable conflict](c4/rendered/StreamOneAttempt-alt2-2.svg)

View: StreamOneAttempt-alt2-2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 3 of 4: stale fence or audit unavailable](c4/rendered/StreamOneAttempt-alt2-3.svg)

View: StreamOneAttempt-alt2-3

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 4 of 4: round trip failed, a non-retryable conflict, or a malformed batch, part 1 of 2](c4/rendered/StreamOneAttempt-alt2-4.svg)

View: StreamOneAttempt-alt2-4

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 2 branch 4 of 4: round trip failed, a non-retryable conflict, or a malformed batch, part 2 of 2](c4/rendered/StreamOneAttempt-alt2-4-part2.svg)

View: StreamOneAttempt-alt2-4-part2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 1 branch 3 of 3: the heartbeat timer fires](c4/rendered/StreamOneAttempt-alt1-3.svg)

View: StreamOneAttempt-alt1-3

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 3 branch 1 of 4: renewed](c4/rendered/StreamOneAttempt-alt3-1.svg)

View: StreamOneAttempt-alt3-1

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 3 branch 2 of 4: cancel requested](c4/rendered/StreamOneAttempt-alt3-2.svg)

View: StreamOneAttempt-alt3-2

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 3 branch 3 of 4: retryable conflict](c4/rendered/StreamOneAttempt-alt3-3.svg)

View: StreamOneAttempt-alt3-3

![Nested sequence: StreamOneAttempt (the live part of the review attempt), alt 3 branch 4 of 4: stale fence or audit unavailable](c4/rendered/StreamOneAttempt-alt3-4.svg)

View: StreamOneAttempt-alt3-4

#### Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt)

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt)](c4/rendered/FinalizeOneRequest.svg)

View: FinalizeOneRequest

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 1 branch 1 of 3: already Completed](c4/rendered/FinalizeOneRequest-alt1-1.svg)

View: FinalizeOneRequest-alt1-1

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 1 branch 2 of 3: not Finalizing](c4/rendered/FinalizeOneRequest-alt1-2.svg)

View: FinalizeOneRequest-alt1-2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 1 branch 3 of 3: Finalizing, part 1 of 2](c4/rendered/FinalizeOneRequest-alt1-3.svg)

View: FinalizeOneRequest-alt1-3

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 1 branch 3 of 3: Finalizing, part 2 of 2](c4/rendered/FinalizeOneRequest-alt1-3-part2.svg)

View: FinalizeOneRequest-alt1-3-part2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 2 branch 1 of 2: read refused](c4/rendered/FinalizeOneRequest-alt2-1.svg)

View: FinalizeOneRequest-alt2-1

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 2 branch 2 of 2: read recorded, part 1 of 3](c4/rendered/FinalizeOneRequest-alt2-2.svg)

View: FinalizeOneRequest-alt2-2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 2 branch 2 of 2: read recorded, part 2 of 3](c4/rendered/FinalizeOneRequest-alt2-2-part2.svg)

View: FinalizeOneRequest-alt2-2-part2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 2 branch 2 of 2: read recorded, part 3 of 3](c4/rendered/FinalizeOneRequest-alt2-2-part3.svg)

View: FinalizeOneRequest-alt2-2-part3

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 1 of 2: a step failed, part 1 of 3](c4/rendered/FinalizeOneRequest-alt3-1.svg)

View: FinalizeOneRequest-alt3-1

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 1 of 2: a step failed, part 2 of 3](c4/rendered/FinalizeOneRequest-alt3-1-part2.svg)

View: FinalizeOneRequest-alt3-1-part2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 1 of 2: a step failed, part 3 of 3](c4/rendered/FinalizeOneRequest-alt3-1-part3.svg)

View: FinalizeOneRequest-alt3-1-part3

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 4 branch 1 of 3: failed](c4/rendered/FinalizeOneRequest-alt4-1.svg)

View: FinalizeOneRequest-alt4-1

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 4 branch 2 of 3: already_completed, not_finalizable, or stale_fence, part 1 of 2](c4/rendered/FinalizeOneRequest-alt4-2.svg)

View: FinalizeOneRequest-alt4-2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 4 branch 2 of 3: already_completed, not_finalizable, or stale_fence, part 2 of 2](c4/rendered/FinalizeOneRequest-alt4-2-part2.svg)

View: FinalizeOneRequest-alt4-2-part2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 4 branch 3 of 3: 503 audit_unavailable or 409 conflict](c4/rendered/FinalizeOneRequest-alt4-3.svg)

View: FinalizeOneRequest-alt4-3

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 2 of 2: verified, part 1 of 2](c4/rendered/FinalizeOneRequest-alt3-2.svg)

View: FinalizeOneRequest-alt3-2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 3 branch 2 of 2: verified, part 2 of 2](c4/rendered/FinalizeOneRequest-alt3-2-part2.svg)

View: FinalizeOneRequest-alt3-2-part2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), opt 1: linked responses and Mode not Off, part 1 of 2](c4/rendered/FinalizeOneRequest-opt1.svg)

View: FinalizeOneRequest-opt1

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), opt 1: linked responses and Mode not Off, part 2 of 2](c4/rendered/FinalizeOneRequest-opt1-part2.svg)

View: FinalizeOneRequest-opt1-part2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 1 of 5: state differs from the reads](c4/rendered/FinalizeOneRequest-alt5-1.svg)

View: FinalizeOneRequest-alt5-1

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 2 of 5: audit unavailable or attempts exhausted](c4/rendered/FinalizeOneRequest-alt5-2.svg)

View: FinalizeOneRequest-alt5-2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 3 of 5: result compression failed its round trip, or a stored model grade with other input, part 1 of 3](c4/rendered/FinalizeOneRequest-alt5-3.svg)

View: FinalizeOneRequest-alt5-3

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 3 of 5: result compression failed its round trip, or a stored model grade with other input, part 2 of 3](c4/rendered/FinalizeOneRequest-alt5-3-part2.svg)

View: FinalizeOneRequest-alt5-3-part2

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 3 of 5: result compression failed its round trip, or a stored model grade with other input, part 3 of 3](c4/rendered/FinalizeOneRequest-alt5-3-part3.svg)

View: FinalizeOneRequest-alt5-3-part3

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 4 of 5: a model grade guard failed, retryable](c4/rendered/FinalizeOneRequest-alt5-4.svg)

View: FinalizeOneRequest-alt5-4

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 5 of 5: committed, part 1 of 2](c4/rendered/FinalizeOneRequest-alt5-5.svg)

View: FinalizeOneRequest-alt5-5

![Nested sequence: FinalizeOneRequest (FinalizeHvRequestCommand after a closed attempt), alt 5 branch 5 of 5: committed, part 2 of 2](c4/rendered/FinalizeOneRequest-alt5-5-part2.svg)

View: FinalizeOneRequest-alt5-5-part2

### 17.8 An execution failure, its incident, and the reviewer going offline

![An execution failure, its incident, and the reviewer going offline, part 1 of 2](c4/rendered/AM1-17-08.svg)

View: AM1-17-08

![An execution failure, its incident, and the reviewer going offline, part 2 of 2](c4/rendered/AM1-17-08-part2.svg)

View: AM1-17-08-part2

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 1 of 3: stale fence](c4/rendered/AM1-17-08-alt1-1.svg)

View: AM1-17-08-alt1-1

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 2 of 3: audit unavailable or attempts exhausted, part 1 of 3](c4/rendered/AM1-17-08-alt1-2.svg)

View: AM1-17-08-alt1-2

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 2 of 3: audit unavailable or attempts exhausted, part 2 of 3](c4/rendered/AM1-17-08-alt1-2-part2.svg)

View: AM1-17-08-alt1-2-part2

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 2 of 3: audit unavailable or attempts exhausted, part 3 of 3](c4/rendered/AM1-17-08-alt1-2-part3.svg)

View: AM1-17-08-alt1-2-part3

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 1 of 7](c4/rendered/AM1-17-08-alt1-3.svg)

View: AM1-17-08-alt1-3

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 2 of 7](c4/rendered/AM1-17-08-alt1-3-part2.svg)

View: AM1-17-08-alt1-3-part2

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 3 of 7](c4/rendered/AM1-17-08-alt1-3-part3.svg)

View: AM1-17-08-alt1-3-part3

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 4 of 7](c4/rendered/AM1-17-08-alt1-3-part4.svg)

View: AM1-17-08-alt1-3-part4

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 5 of 7](c4/rendered/AM1-17-08-alt1-3-part5.svg)

View: AM1-17-08-alt1-3-part5

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 6 of 7](c4/rendered/AM1-17-08-alt1-3-part6.svg)

View: AM1-17-08-alt1-3-part6

![An execution failure, its incident, and the reviewer going offline, alt 1 branch 3 of 3: guarded, part 7 of 7](c4/rendered/AM1-17-08-alt1-3-part7.svg)

View: AM1-17-08-alt1-3-part7

![An execution failure, its incident, and the reviewer going offline, alt 2 branch 1 of 3: request CancelRequested](c4/rendered/AM1-17-08-alt2-1.svg)

View: AM1-17-08-alt2-1

![An execution failure, its incident, and the reviewer going offline, alt 2 branch 2 of 3: attempts left](c4/rendered/AM1-17-08-alt2-2.svg)

View: AM1-17-08-alt2-2

![An execution failure, its incident, and the reviewer going offline, alt 2 branch 3 of 3: attempts exhausted](c4/rendered/AM1-17-08-alt2-3.svg)

View: AM1-17-08-alt2-3

![An execution failure, its incident, and the reviewer going offline, alt 3 branch 1 of 2: audit ledger unavailable](c4/rendered/AM1-17-08-alt3-1.svg)

View: AM1-17-08-alt3-1

![An execution failure, its incident, and the reviewer going offline, alt 3 branch 2 of 2: attempt recorded](c4/rendered/AM1-17-08-alt3-2.svg)

View: AM1-17-08-alt3-2

![An execution failure, its incident, and the reviewer going offline, alt 4 branch 1 of 2: triage accepted, part 1 of 2](c4/rendered/AM1-17-08-alt4-1.svg)

View: AM1-17-08-alt4-1

![An execution failure, its incident, and the reviewer going offline, alt 4 branch 1 of 2: triage accepted, part 2 of 2](c4/rendered/AM1-17-08-alt4-1-part2.svg)

View: AM1-17-08-alt4-1-part2

![An execution failure, its incident, and the reviewer going offline, alt 4 branch 2 of 2: triage refused or unreachable, part 1 of 2](c4/rendered/AM1-17-08-alt4-2.svg)

View: AM1-17-08-alt4-2

![An execution failure, its incident, and the reviewer going offline, alt 4 branch 2 of 2: triage refused or unreachable, part 2 of 2](c4/rendered/AM1-17-08-alt4-2-part2.svg)

View: AM1-17-08-alt4-2-part2

### 17.9 Recovery after a worker crash

![Recovery after a worker crash, part 1 of 6](c4/rendered/AM1-17-09.svg)

View: AM1-17-09

![Recovery after a worker crash, part 2 of 6](c4/rendered/AM1-17-09-part2.svg)

View: AM1-17-09-part2

![Recovery after a worker crash, part 3 of 6](c4/rendered/AM1-17-09-part3.svg)

View: AM1-17-09-part3

![Recovery after a worker crash, part 4 of 6](c4/rendered/AM1-17-09-part4.svg)

View: AM1-17-09-part4

![Recovery after a worker crash, part 5 of 6](c4/rendered/AM1-17-09-part5.svg)

View: AM1-17-09-part5

![Recovery after a worker crash, part 6 of 6](c4/rendered/AM1-17-09-part6.svg)

View: AM1-17-09-part6

![Recovery after a worker crash, alt 1 branch 1 of 2: renewed, settled, or taken over by another sweep in the meantime, part 1 of 2](c4/rendered/AM1-17-09-alt1-1.svg)

View: AM1-17-09-alt1-1

![Recovery after a worker crash, alt 1 branch 1 of 2: renewed, settled, or taken over by another sweep in the meantime, part 2 of 2](c4/rendered/AM1-17-09-alt1-1-part2.svg)

View: AM1-17-09-alt1-1-part2

![Recovery after a worker crash, alt 1 branch 2 of 2: still Claimed or Running with the fence as read and its lease expired, part 1 of 2](c4/rendered/AM1-17-09-alt1-2.svg)

View: AM1-17-09-alt1-2

![Recovery after a worker crash, alt 1 branch 2 of 2: still Claimed or Running with the fence as read and its lease expired, part 2 of 2](c4/rendered/AM1-17-09-alt1-2-part2.svg)

View: AM1-17-09-alt1-2-part2

![Recovery after a worker crash, alt 2 branch 1 of 4: the request has a newer attempt or is terminal](c4/rendered/AM1-17-09-alt2-1.svg)

View: AM1-17-09-alt2-1

![Recovery after a worker crash, alt 2 branch 2 of 4: request CancelRequested](c4/rendered/AM1-17-09-alt2-2.svg)

View: AM1-17-09-alt2-2

![Recovery after a worker crash, alt 2 branch 3 of 4: attempts left](c4/rendered/AM1-17-09-alt2-3.svg)

View: AM1-17-09-alt2-3

![Recovery after a worker crash, alt 2 branch 4 of 4: attempts exhausted](c4/rendered/AM1-17-09-alt2-4.svg)

View: AM1-17-09-alt2-4

![Recovery after a worker crash, opt 1: worker A was alive but paused past its lease, part 1 of 2](c4/rendered/AM1-17-09-opt1.svg)

View: AM1-17-09-opt1

![Recovery after a worker crash, opt 1: worker A was alive but paused past its lease, part 2 of 2](c4/rendered/AM1-17-09-opt1-part2.svg)

View: AM1-17-09-opt1-part2

### 17.10 GetHvResultQuery and GetHvEvidenceQuery

![GetHvResultQuery and GetHvEvidenceQuery, part 1 of 2](c4/rendered/AM1-17-10.svg)

View: AM1-17-10

![GetHvResultQuery and GetHvEvidenceQuery, part 2 of 2](c4/rendered/AM1-17-10-part2.svg)

View: AM1-17-10-part2

![GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 1 of 7: unknown kind](c4/rendered/AM1-17-10-alt1-1.svg)

View: AM1-17-10-alt1-1

![GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 2 of 7: request or run not found](c4/rendered/AM1-17-10-alt1-2.svg)

View: AM1-17-10-alt1-2

![GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 3 of 7: result before Completed](c4/rendered/AM1-17-10-alt1-3.svg)

View: AM1-17-10-alt1-3

![GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 4 of 7: evidence not produced yet](c4/rendered/AM1-17-10-alt1-4.svg)

View: AM1-17-10-alt1-4

![GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 5 of 7: evidence removed](c4/rendered/AM1-17-10-alt1-5.svg)

View: AM1-17-10-alt1-5

![GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 6 of 7: audit ledger unavailable](c4/rendered/AM1-17-10-alt1-6.svg)

View: AM1-17-10-alt1-6

![GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 7 of 7: available, part 1 of 2](c4/rendered/AM1-17-10-alt1-7.svg)

View: AM1-17-10-alt1-7

![GetHvResultQuery and GetHvEvidenceQuery, alt 1 branch 7 of 7: available, part 2 of 2](c4/rendered/AM1-17-10-alt1-7-part2.svg)

View: AM1-17-10-alt1-7-part2

![GetHvResultQuery and GetHvEvidenceQuery, alt 2 branch 1 of 3: a payload it decompresses fails before the first byte](c4/rendered/AM1-17-10-alt2-1.svg)

View: AM1-17-10-alt2-1

![GetHvResultQuery and GetHvEvidenceQuery, alt 2 branch 2 of 3: it fails after the body started](c4/rendered/AM1-17-10-alt2-2.svg)

View: AM1-17-10-alt2-2

![GetHvResultQuery and GetHvEvidenceQuery, alt 2 branch 3 of 3: decompressed or served compressed](c4/rendered/AM1-17-10-alt2-3.svg)

View: AM1-17-10-alt2-3

### 17.11 RemoveHvEvidenceCommand

![RemoveHvEvidenceCommand, part 1 of 2](c4/rendered/AM1-17-11.svg)

View: AM1-17-11

![RemoveHvEvidenceCommand, part 2 of 2](c4/rendered/AM1-17-11-part2.svg)

View: AM1-17-11-part2

![RemoveHvEvidenceCommand, alt 1 branch 1 of 5: reason or removedBy missing or too long](c4/rendered/AM1-17-11-alt1-1.svg)

View: AM1-17-11-alt1-1

![RemoveHvEvidenceCommand, alt 1 branch 2 of 5: not found](c4/rendered/AM1-17-11-alt1-2.svg)

View: AM1-17-11-alt1-2

![RemoveHvEvidenceCommand, alt 1 branch 3 of 5: request not terminal](c4/rendered/AM1-17-11-alt1-3.svg)

View: AM1-17-11-alt1-3

![RemoveHvEvidenceCommand, alt 1 branch 4 of 5: audit unavailable or attempts exhausted](c4/rendered/AM1-17-11-alt1-4.svg)

View: AM1-17-11-alt1-4

![RemoveHvEvidenceCommand, alt 1 branch 5 of 5: removed or nothing left, part 1 of 2](c4/rendered/AM1-17-11-alt1-5.svg)

View: AM1-17-11-alt1-5

![RemoveHvEvidenceCommand, alt 1 branch 5 of 5: removed or nothing left, part 2 of 2](c4/rendered/AM1-17-11-alt1-5-part2.svg)

View: AM1-17-11-alt1-5-part2

### 17.12 StreamHvRunOutputQuery with a reconnect

![StreamHvRunOutputQuery with a reconnect, part 1 of 2](c4/rendered/AM1-17-12.svg)

View: AM1-17-12

![StreamHvRunOutputQuery with a reconnect, part 2 of 2](c4/rendered/AM1-17-12-part2.svg)

View: AM1-17-12-part2

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 1 of 5: not found](c4/rendered/AM1-17-12-alt1-1.svg)

View: AM1-17-12-alt1-1

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 2 of 5: evidence removed](c4/rendered/AM1-17-12-alt1-2.svg)

View: AM1-17-12-alt1-2

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 3 of 5: malformed Last-Event-ID](c4/rendered/AM1-17-12-alt1-3.svg)

View: AM1-17-12-alt1-3

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 4 of 5: audit ledger unavailable](c4/rendered/AM1-17-12-alt1-4.svg)

View: AM1-17-12-alt1-4

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 1 of 9](c4/rendered/AM1-17-12-alt1-5.svg)

View: AM1-17-12-alt1-5

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 2 of 9](c4/rendered/AM1-17-12-alt1-5-part2.svg)

View: AM1-17-12-alt1-5-part2

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 3 of 9](c4/rendered/AM1-17-12-alt1-5-part3.svg)

View: AM1-17-12-alt1-5-part3

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 4 of 9](c4/rendered/AM1-17-12-alt1-5-part4.svg)

View: AM1-17-12-alt1-5-part4

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 5 of 9](c4/rendered/AM1-17-12-alt1-5-part5.svg)

View: AM1-17-12-alt1-5-part5

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 6 of 9](c4/rendered/AM1-17-12-alt1-5-part6.svg)

View: AM1-17-12-alt1-5-part6

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 7 of 9](c4/rendered/AM1-17-12-alt1-5-part7.svg)

View: AM1-17-12-alt1-5-part7

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 8 of 9](c4/rendered/AM1-17-12-alt1-5-part8.svg)

View: AM1-17-12-alt1-5-part8

![StreamHvRunOutputQuery with a reconnect, alt 1 branch 5 of 5: attached, part 9 of 9](c4/rendered/AM1-17-12-alt1-5-part9.svg)

View: AM1-17-12-alt1-5-part9

![StreamHvRunOutputQuery with a reconnect, alt 2 branch 1 of 2: a stored chunk fails to decompress](c4/rendered/AM1-17-12-alt2-1.svg)

View: AM1-17-12-alt2-1

![StreamHvRunOutputQuery with a reconnect, alt 2 branch 2 of 2: every chunk sent](c4/rendered/AM1-17-12-alt2-2.svg)

View: AM1-17-12-alt2-2

### 17.13 StreamHvNotificationsQuery through the plugin watcher

![StreamHvNotificationsQuery through the plugin watcher, part 1 of 2](c4/rendered/AM1-17-13.svg)

View: AM1-17-13

![StreamHvNotificationsQuery through the plugin watcher, part 2 of 2](c4/rendered/AM1-17-13-part2.svg)

View: AM1-17-13-part2

![StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 1 of 2: malformed Last-Event-ID](c4/rendered/AM1-17-13-alt1-1.svg)

View: AM1-17-13-alt1-1

![StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 1 of 7](c4/rendered/AM1-17-13-alt1-2.svg)

View: AM1-17-13-alt1-2

![StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 2 of 7](c4/rendered/AM1-17-13-alt1-2-part2.svg)

View: AM1-17-13-alt1-2-part2

![StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 3 of 7](c4/rendered/AM1-17-13-alt1-2-part3.svg)

View: AM1-17-13-alt1-2-part3

![StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 4 of 7](c4/rendered/AM1-17-13-alt1-2-part4.svg)

View: AM1-17-13-alt1-2-part4

![StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 5 of 7](c4/rendered/AM1-17-13-alt1-2-part5.svg)

View: AM1-17-13-alt1-2-part5

![StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 6 of 7](c4/rendered/AM1-17-13-alt1-2-part6.svg)

View: AM1-17-13-alt1-2-part6

![StreamHvNotificationsQuery through the plugin watcher, alt 1 branch 2 of 2: attached, part 7 of 7](c4/rendered/AM1-17-13-alt1-2-part7.svg)

View: AM1-17-13-alt1-2-part7

### 17.14 QueryHvNotificationsQuery and QueryHvIncidentsQuery

![QueryHvNotificationsQuery and QueryHvIncidentsQuery, part 1 of 2](c4/rendered/AM1-17-14.svg)

View: AM1-17-14

![QueryHvNotificationsQuery and QueryHvIncidentsQuery, part 2 of 2](c4/rendered/AM1-17-14-part2.svg)

View: AM1-17-14-part2

![QueryHvNotificationsQuery and QueryHvIncidentsQuery, alt 1 branch 1 of 2: a parameter out of range or an unknown filter](c4/rendered/AM1-17-14-alt1-1.svg)

View: AM1-17-14-alt1-1

![QueryHvNotificationsQuery and QueryHvIncidentsQuery, alt 1 branch 2 of 2: valid, part 1 of 2](c4/rendered/AM1-17-14-alt1-2.svg)

View: AM1-17-14-alt1-2

![QueryHvNotificationsQuery and QueryHvIncidentsQuery, alt 1 branch 2 of 2: valid, part 2 of 2](c4/rendered/AM1-17-14-alt1-2-part2.svg)

View: AM1-17-14-alt1-2-part2

### 17.15 A write to a frozen turn, and a hostile review from an agent surface

![A write to a frozen turn, and a hostile review from an agent surface, part 1 of 9](c4/rendered/AM1-17-15.svg)

View: AM1-17-15

![A write to a frozen turn, and a hostile review from an agent surface, part 2 of 9](c4/rendered/AM1-17-15-part2.svg)

View: AM1-17-15-part2

![A write to a frozen turn, and a hostile review from an agent surface, part 3 of 9](c4/rendered/AM1-17-15-part3.svg)

View: AM1-17-15-part3

![A write to a frozen turn, and a hostile review from an agent surface, part 4 of 9](c4/rendered/AM1-17-15-part4.svg)

View: AM1-17-15-part4

![A write to a frozen turn, and a hostile review from an agent surface, part 5 of 9](c4/rendered/AM1-17-15-part5.svg)

View: AM1-17-15-part5

![A write to a frozen turn, and a hostile review from an agent surface, part 6 of 9](c4/rendered/AM1-17-15-part6.svg)

View: AM1-17-15-part6

![A write to a frozen turn, and a hostile review from an agent surface, part 7 of 9](c4/rendered/AM1-17-15-part7.svg)

View: AM1-17-15-part7

![A write to a frozen turn, and a hostile review from an agent surface, part 8 of 9](c4/rendered/AM1-17-15-part8.svg)

View: AM1-17-15-part8

![A write to a frozen turn, and a hostile review from an agent surface, part 9 of 9](c4/rendered/AM1-17-15-part9.svg)

View: AM1-17-15-part9

![A write to a frozen turn, and a hostile review from an agent surface, alt 1 branch 1 of 2: turn Pending](c4/rendered/AM1-17-15-alt1-1.svg)

View: AM1-17-15-alt1-1

![A write to a frozen turn, and a hostile review from an agent surface, alt 1 branch 2 of 2: turn Final](c4/rendered/AM1-17-15-alt1-2.svg)

View: AM1-17-15-alt1-2

## 18. Use cases

The foundation's notation (foundation section 7): actors are stadium shapes on the left, the use case is the central oval, included and extending behaviors are ovals linked `include` or `extend`. Labels `UC-HV-01` to `UC-HV-12` are document labels; the use-case store keys a use case by a numeric surrogate and its title (`src/QBrainAi.Storage/Entities/UseCaseEntity.cs:12-13`), so H0 creates each under its section title and records the store id beside the label (section 20). Actor names describe participants, not access roles (S-29). One alternate applies to every use case and is not repeated: an unresolved workspace is refused with 400 `validation_error`, `details.field` `workspacePath`. A flow names in parentheses the unit, policy, process, or component that performs a step.

### 18.1 Overview

![Use cases: overview (use cases), part 1 of 10](c4/rendered/AM1-18-01.svg)

View: AM1-18-01

![Use cases: overview (use cases), part 2 of 10](c4/rendered/AM1-18-01-part2.svg)

View: AM1-18-01-part2

![Use cases: overview (use cases), part 3 of 10](c4/rendered/AM1-18-01-part3.svg)

View: AM1-18-01-part3

![Use cases: overview (use cases), part 4 of 10](c4/rendered/AM1-18-01-part4.svg)

View: AM1-18-01-part4

![Use cases: overview (use cases), part 5 of 10](c4/rendered/AM1-18-01-part5.svg)

View: AM1-18-01-part5

![Use cases: overview (use cases), part 6 of 10](c4/rendered/AM1-18-01-part6.svg)

View: AM1-18-01-part6

![Use cases: overview (use cases), part 7 of 10](c4/rendered/AM1-18-01-part7.svg)

View: AM1-18-01-part7

![Use cases: overview (use cases), part 8 of 10](c4/rendered/AM1-18-01-part8.svg)

View: AM1-18-01-part8

![Use cases: overview (use cases), part 9 of 10](c4/rendered/AM1-18-01-part9.svg)

View: AM1-18-01-part9

![Use cases: overview (use cases), part 10 of 10](c4/rendered/AM1-18-01-part10.svg)

View: AM1-18-01-part10

### 18.2 UC-HV-01 Manage the reviewer pool [FR-MCP-HVPIPE-001]

Actors: Operator (through Director or the REPL), server. Trigger: the operator lists, inspects, enables, disables, or retunes a reviewer. Preconditions: the server started with the five reviewers configured and seeded (section 10). Main flow: the operator lists the pool (ListHvReviewersQuery), reads one reviewer (GetHvReviewerQuery), and changes its enabled flag, strategy, agent path, model, effort, or concurrency with the state version it read (UpdateHvReviewerCommand); a change to the strategy, agent path, model, or effort makes the reviewer `Unverified`, and the worker NONCEs it (UC-HV-02). Alternates: a stale state version is refused with 409 `conflict` and the operator reads again; an unknown reviewer is 404; an invalid field is 400; the audit ledger is unavailable and nothing changes (503). Postconditions: the change is stored, audited, and announced (`reviewer_unverified` when the launch changed); a disabled reviewer gets no new assignment, and the requests it held are reassigned at their next claim. Invariants: I-10; no change applies without its ledger rows.

![UC-HV-01 Manage the reviewer pool (use cases), part 1 of 3](c4/rendered/UC-HV-01.svg)

View: UC-HV-01

![UC-HV-01 Manage the reviewer pool (use cases), part 2 of 3](c4/rendered/UC-HV-01-part2.svg)

View: UC-HV-01-part2

![UC-HV-01 Manage the reviewer pool (use cases), part 3 of 3](c4/rendered/UC-HV-01-part3.svg)

View: UC-HV-01-part3

### 18.3 UC-HV-02 NONCE a reviewer [FR-MCP-HVPIPE-003]

Actors: operator or any agent, HV worker, reviewer agent. Trigger: a reviewer is `Unverified` (startup or a launch change, requested by the worker) or `Offline` after its cause was fixed (requested by a caller, S-15). Preconditions: the reviewer is enabled. Main flow: the caller requests a NONCE (StartHvReviewerNonceCommand); the worker claims the probe (ClaimHvRunCommand), launches the reviewer with a probe brief carrying a fresh nonce (RunReviewerProcess), streams its output (AppendHvRunOutputCommand), appends the adapter's provenance, and closes the run (CloseHvRunOutputCommand); policy ReadinessProbeVerdict passes: the nonce is echoed, the provenance shows the expected model and effort, and the reviewer logged its own turn; the reviewer becomes `Online` with its resolved model and effort. Alternates: a probe already requested or running is reported without a second probe; a disabled reviewer is refused with 409 `hv_reviewer_disabled`; the probe fails (wrong nonce, wrong or missing provenance, no reviewer turn, launch failure, nonzero exit) and the reviewer stays or becomes `Offline` with an incident filed through triage (UC-HV-09); the probe's stored log fails its integrity check, a server fault, and a server incident is filed while the reviewer's readiness is unchanged; the reviewer is reconfigured while the probe runs, and the old probe, whether it closes, fails, ends with a shutdown, or loses its lease, changes nothing of the reviewer while the newer probe request waits (HvProbeOutcome); the worker dies during a probe of the current configuration and the sweep requests it again; the worker restarts after a probe settled on a server fault and requests no new probe until a caller asks; a startup NONCE request refused while the audit ledger is down is made again at the worker's next iteration, unless the reviewer was disabled meanwhile. Postconditions: the reviewer's readiness reflects a probe of this moment (S-27); `reviewer_online` or `reviewer_offline` is announced. Invariants: only a passing NONCE moves a reviewer to `Online` (R-10).

![UC-HV-02 NONCE a reviewer (use cases), part 1 of 7](c4/rendered/UC-HV-02.svg)

View: UC-HV-02

![UC-HV-02 NONCE a reviewer (use cases), part 2 of 7](c4/rendered/UC-HV-02-part2.svg)

View: UC-HV-02-part2

![UC-HV-02 NONCE a reviewer (use cases), part 3 of 7](c4/rendered/UC-HV-02-part3.svg)

View: UC-HV-02-part3

![UC-HV-02 NONCE a reviewer (use cases), part 4 of 7](c4/rendered/UC-HV-02-part4.svg)

View: UC-HV-02-part4

![UC-HV-02 NONCE a reviewer (use cases), part 5 of 7](c4/rendered/UC-HV-02-part5.svg)

View: UC-HV-02-part5

![UC-HV-02 NONCE a reviewer (use cases), part 6 of 7](c4/rendered/UC-HV-02-part6.svg)

View: UC-HV-02-part6

![UC-HV-02 NONCE a reviewer (use cases), part 7 of 7](c4/rendered/UC-HV-02-part7.svg)

View: UC-HV-02-part7

### 18.4 UC-HV-03 Submit an HV request [FR-MCP-HVPIPE-002, -004, -005, -006, -013, -018]

Actors: model (through its plugin) or operator (through Director), any client, server. Trigger: a caller wants a turn, an inclusive range of turns, or a whole session reviewed (S-03, S-11). Preconditions: the turns to review are terminal; for an agent caller, its own turn is bound by the plugin (BindVerbToTurn). Main flow: the caller submits the scope, optional context in Markdown or YAML, and an idempotency key (SubmitHvRequestCommand); the server resolves the requirement references of the context (RequirementReferenceExtractor), resolves the members (HvScopeResolution: the session's turns at admission time for a session scope, without the submitting turn), checks that every member is terminal, not under review, and never reviewed (HvAdmissionEligibility), picks the next eligible reviewer round-robin, never the submitter, the author of the reviewed session, or an alias of either (RoundRobinAssignment, HvReviewerEligibility), snapshots every member without its review records (TurnSnapshotBuilder), stores the complete accepted request compressed (GzipCodec), freezes the members (HvTurnFreeze), announces `hv_queued`, and wakes the worker (auto-start, S-24). Alternates: the same key and inputs return the stored request as GetHvRequestQuery returns it, its last output read audited; the key with other inputs is a conflict; a malformed scope, a scope over the limit, a scope naming the submitting turn, or an empty session scope is refused (400); turns that are not terminal, under review, or already reviewed are refused with every affected turn listed (409); no reviewer is eligible and the request is refused with every reviewer's status, and the plugin adds the reviewers' open incidents and the NONCE remedy (409, S-16); a turn whose class is `Chore` and whose hostile review is `Unnecessary` is admitted and graded like any other (S-09); a turn has no self-review, or any self-review grade or status, and admission is the same (S-28); the audit ledger is unavailable and nothing is written (503, S-29). Postconditions: one `Queued` request with its members frozen `Pending`, its accepted request stored, and its reviewer assigned. Invariants: I-01, I-07, I-08, I-10.

![UC-HV-03 Submit an HV request (use cases), part 1 of 7](c4/rendered/UC-HV-03.svg)

View: UC-HV-03

![UC-HV-03 Submit an HV request (use cases), part 2 of 7](c4/rendered/UC-HV-03-part2.svg)

View: UC-HV-03-part2

![UC-HV-03 Submit an HV request (use cases), part 3 of 7](c4/rendered/UC-HV-03-part3.svg)

View: UC-HV-03-part3

![UC-HV-03 Submit an HV request (use cases), part 4 of 7](c4/rendered/UC-HV-03-part4.svg)

View: UC-HV-03-part4

![UC-HV-03 Submit an HV request (use cases), part 5 of 7](c4/rendered/UC-HV-03-part5.svg)

View: UC-HV-03-part5

![UC-HV-03 Submit an HV request (use cases), part 6 of 7](c4/rendered/UC-HV-03-part6.svg)

View: UC-HV-03-part6

![UC-HV-03 Submit an HV request (use cases), part 7 of 7](c4/rendered/UC-HV-03-part7.svg)

View: UC-HV-03-part7

### 18.5 UC-HV-04 Run a review live [FR-MCP-HVPIPE-007, -008]

Actors: HV worker, reviewer agent, server. Trigger: a `Queued` request whose reviewer is eligible and has capacity. Preconditions: UC-HV-03 completed. Main flow: the worker claims the request with a lease and a fence (ClaimHvRunCommand); it launches the reviewer with the brief (RunReviewerProcess) and records the brief as the log's first record; every line the reviewer writes is appended within the flush interval (AppendHvRunOutputCommand) and is visible on the output stream while the reviewer runs (S-22); progress records set the request's progress and ETA (R-18); heartbeats renew the lease (RecordHvRunHeartbeatCommand); the reviewer reviews each turn and the whole set, logs its own `Chore` turn holding its result, and emits its verdict record last; on exit with code 0 the worker appends the provenance and closes the output (CloseHvRunOutputCommand), and the request becomes `Finalizing`. Alternates: the assignee is no longer eligible and the claim reassigns round-robin to an eligible reviewer with capacity, skipping one at its `MaxConcurrentRuns`, or writes nothing and the request waits when none has capacity; the assignee is still eligible but at its `MaxConcurrentRuns`, and the request keeps it and waits while later requests are claimed; a cancel arrives and the worker stops the reviewer (UC-HV-08); the reviewer fails to start or exits nonzero (UC-HV-09); the stored accepted request fails to decompress when the brief is built, so no reviewer starts and the attempt fails with `evidence_corrupt` as a server fault (UC-HV-09); the worker loses its lease and its writes are refused as stale, so it stops (UC-HV-09); no reviewer is eligible and the request waits `Queued` with an unknown ETA; the reviewer runs for any length of time without a timeout (P-02). Postconditions: the attempt's complete log is stored, compressed, contiguous, and hashed. Invariants: I-05, I-06.

![UC-HV-04 Run a review live (use cases), part 1 of 6](c4/rendered/UC-HV-04.svg)

View: UC-HV-04

![UC-HV-04 Run a review live (use cases), part 2 of 6](c4/rendered/UC-HV-04-part2.svg)

View: UC-HV-04-part2

![UC-HV-04 Run a review live (use cases), part 3 of 6](c4/rendered/UC-HV-04-part3.svg)

View: UC-HV-04-part3

![UC-HV-04 Run a review live (use cases), part 4 of 6](c4/rendered/UC-HV-04-part4.svg)

View: UC-HV-04-part4

![UC-HV-04 Run a review live (use cases), part 5 of 6](c4/rendered/UC-HV-04-part5.svg)

View: UC-HV-04-part5

![UC-HV-04 Run a review live (use cases), part 6 of 6](c4/rendered/UC-HV-04-part6.svg)

View: UC-HV-04-part6

### 18.6 UC-HV-05 Finalize the HV result [FR-MCP-HVPIPE-010, -011, -012, -014, -021]

Actors: HV worker, server. Trigger: a request is `Finalizing` (after UC-HV-04, or found by the sweep after a crash). Main flow: the server records its read of the attempt's log (AuditLedgerGuard), reads it, and verifies it (HvFinalVerification: integrity, completeness, binding to the request and to each turn's admission snapshot, verdict consistency at 98/98, reviewer independence, the reviewer's own turn, and runtime identity); then, in one transaction (S-23), it stores the per-turn assessments, the holistic evaluation, and the delineated failures with their remediation, writes each turn's hostile review through HvVerdictMapping, makes every member turn final, grades the linked agent responses and decides their trust when agent context is on, stores the result document compressed, completes the request, and announces `hv_completed`. Alternates: a verification step fails, the attempt fails with that code (UC-HV-09), and the request is queued again or fails when its attempts are spent; recording that failure is itself refused or conflicts, and the request stays `Finalizing` for the sweep; the result document fails its compression round trip, the transaction rolls back, and the attempt fails with `evidence_corrupt` as a server fault; a stored model grade with other input is found, the transaction rolls back, and the attempt fails with `model_grade_conflict` as a server fault; the store changed since the reads and the unit starts again from its status check; the audit ledger is unavailable or the transaction keeps conflicting and the request stays `Finalizing` for the sweep; the request was already finalized and the stored outcome returns; agent context is `Off` and the linked responses are recorded `NotGraded`. Postconditions: either every part of the result is committed or none is (I-04); the TODOs the turns name are unchanged (S-25); the self-review records were never read (S-28). Invariants: I-02, I-03, I-04, I-09.

![UC-HV-05 Finalize the HV result (use cases), part 1 of 5](c4/rendered/UC-HV-05.svg)

View: UC-HV-05

![UC-HV-05 Finalize the HV result (use cases), part 2 of 5](c4/rendered/UC-HV-05-part2.svg)

View: UC-HV-05-part2

![UC-HV-05 Finalize the HV result (use cases), part 3 of 5](c4/rendered/UC-HV-05-part3.svg)

View: UC-HV-05-part3

![UC-HV-05 Finalize the HV result (use cases), part 4 of 5](c4/rendered/UC-HV-05-part4.svg)

View: UC-HV-05-part4

![UC-HV-05 Finalize the HV result (use cases), part 5 of 5](c4/rendered/UC-HV-05-part5.svg)

View: UC-HV-05-part5

### 18.7 UC-HV-06 Watch an HV live [FR-MCP-HVPIPE-015, -016]

Actors: any client (Director `hv-output`, the REPL, a REST client). Trigger: a caller wants to see a running HV (S-18). Main flow: the caller lists running HVs (QueryHvRequestsQuery with `status` `Running`), reads one with its progress, ETA, and last 20 lines (GetHvRequestQuery), and attaches to its output stream (StreamHvRunOutputQuery), which replays the stored chunks and then sends each new chunk as it is appended; the stream ends after the request reaches a terminal state. Alternates: the connection drops and the caller reconnects with its last event id, receiving exactly the events it missed (OrderedEventReplay); the reviewer reports no progress and the ETA is `Unknown` (R-18); a new attempt starts and the stream continues with the new run's events; the audit ledger is unavailable and the attach is refused (503, evidence access is audited). Postconditions: the caller saw every output line in order. Invariants: I-06.

![UC-HV-06 Watch an HV live (use cases), part 1 of 4](c4/rendered/UC-HV-06.svg)

View: UC-HV-06

![UC-HV-06 Watch an HV live (use cases), part 2 of 4](c4/rendered/UC-HV-06-part2.svg)

View: UC-HV-06-part2

![UC-HV-06 Watch an HV live (use cases), part 3 of 4](c4/rendered/UC-HV-06-part3.svg)

View: UC-HV-06-part3

![UC-HV-06 Watch an HV live (use cases), part 4 of 4](c4/rendered/UC-HV-06-part4.svg)

View: UC-HV-06-part4

### 18.8 UC-HV-07 Receive completion notifications [FR-MCP-HVPIPE-017, -020]

Actors: the plugin's watcher and the model, Director, any client. Trigger: an HV completes, fails, or is canceled (S-19). Main flow: the plugin's watcher follows the notification stream from its last sequence (WatchHvNotifications, StreamHvNotificationsQuery); a terminal notification whose submitter turn key names the host session's MCP session goes to the host session's inbox; at the next prompt the plugin adds it to the model's context with its sequence, the overall verdict, and how to read the result, and marks it delivered. Alternates: the stream drops and the watcher reconnects from its recorded sequence, missing nothing; the server restarts and the outbox rows are still there; a client without streams polls (QueryHvNotificationsQuery); a notification arrives twice (at least once) and its sequence keeps it out of the inbox the second time; a completion arrives before the submit verb has returned and is still inboxed; the plugin crashes between adding an entry to a prompt and marking it delivered, and the entry is added once more. Postconditions: every terminal outcome of a request the host session's MCP session submitted reaches that session's model at least once, each time with its sequence. Invariants: I-12.

![UC-HV-07 Receive completion notifications (use cases), part 1 of 5](c4/rendered/UC-HV-07.svg)

View: UC-HV-07

![UC-HV-07 Receive completion notifications (use cases), part 2 of 5](c4/rendered/UC-HV-07-part2.svg)

View: UC-HV-07-part2

![UC-HV-07 Receive completion notifications (use cases), part 3 of 5](c4/rendered/UC-HV-07-part3.svg)

View: UC-HV-07-part3

![UC-HV-07 Receive completion notifications (use cases), part 4 of 5](c4/rendered/UC-HV-07-part4.svg)

View: UC-HV-07-part4

![UC-HV-07 Receive completion notifications (use cases), part 5 of 5](c4/rendered/UC-HV-07-part5.svg)

View: UC-HV-07-part5

### 18.9 UC-HV-08 Cancel an HV [FR-MCP-HVPIPE-008]

Actors: any caller (S-18, no roles). Main flow: the caller cancels (CancelHvRequestCommand); a `Queued` request is canceled at once and its turns released; a `Running` request becomes `CancelRequested`, the worker reads it at its next heartbeat, stops the reviewer, and the request becomes `Canceled` with its turns released. Alternates: the request is already `CancelRequested` or `Canceled` and nothing changes; it is `Finalizing`, `Completed`, or `Failed` and the cancel is refused (409); the reviewer exits with code 0 before the next heartbeat, and the close finds the cancel and ends the request `Canceled` instead of finalizing it (CloseHvRunOutputCommand, R-28); the run fails before the next heartbeat, and the failure ends the request `Canceled` (FailHvRunCommand, HvRetryDecision); the worker died and the lease expiry completes the cancel (ExpireHvRunLeasesCommand). Postconditions: a canceled request's turns are writable again; nothing of an unfinished review is recorded on them. Invariants: I-04.

![UC-HV-08 Cancel an HV (use cases), part 1 of 4](c4/rendered/UC-HV-08.svg)

View: UC-HV-08

![UC-HV-08 Cancel an HV (use cases), part 2 of 4](c4/rendered/UC-HV-08-part2.svg)

View: UC-HV-08-part2

![UC-HV-08 Cancel an HV (use cases), part 3 of 4](c4/rendered/UC-HV-08-part3.svg)

View: UC-HV-08-part3

![UC-HV-08 Cancel an HV (use cases), part 4 of 4](c4/rendered/UC-HV-08-part4.svg)

View: UC-HV-08-part4

### 18.10 UC-HV-09 Recover from failures [FR-MCP-HVPIPE-007, -009]

Actors: HV worker, triage, server. Trigger: an execution failure (launch failure, nonzero exit, invalid output, failed verification, missing reviewer turn, runtime identity mismatch, corrupt stored evidence) or a dead worker. Main flow: the failure is recorded on the attempt (FailHvRunCommand); a failure attributable to the reviewer marks it `Offline` until a NONCE passes (S-15); an incident is recorded and filed through the triage endpoint with a deduplication key (FileHvIncidentCommand, S-26); the request returns to `Queued` and the next claim assigns the next eligible reviewer. Alternates: attempts are spent and the request fails, releasing its turns; triage is unreachable and the filing is retried at each sweep; a worker died and its expired leases are swept (ExpireHvRunLeasesCommand), its runs abandoned and fenced out; two sweeps read the same expired run, and the second skips it (FencedLease `Expire`), so the run is abandoned once and a newer attempt or a terminal request is never touched (HvRetryDecision); a graceful shutdown ends runs with `worker_shutdown`, which is not counted as an attempt; the audit ledger is unavailable and the failed run cannot be recorded until it returns, after which the lease expiry recovers it. Postconditions: no failure is silent, no incident group is duplicated, and no reviewer is used again before it proves itself. Invariants: I-05.

![UC-HV-09 Recover from failures (use cases), part 1 of 5](c4/rendered/UC-HV-09.svg)

View: UC-HV-09

![UC-HV-09 Recover from failures (use cases), part 2 of 5](c4/rendered/UC-HV-09-part2.svg)

View: UC-HV-09-part2

![UC-HV-09 Recover from failures (use cases), part 3 of 5](c4/rendered/UC-HV-09-part3.svg)

View: UC-HV-09-part3

![UC-HV-09 Recover from failures (use cases), part 4 of 5](c4/rendered/UC-HV-09-part4.svg)

View: UC-HV-09-part4

![UC-HV-09 Recover from failures (use cases), part 5 of 5](c4/rendered/UC-HV-09-part5.svg)

View: UC-HV-09-part5

### 18.11 UC-HV-10 Retrieve the result and evidence [FR-MCP-HVPIPE-011, -015]

Actors: any client. Main flow: the caller reads the standard result document (GetHvResultQuery: the complete accepted request, the complete response log, every turn with its score, the holistic evaluation, the delineated failures with remediation, the overall verdict, and the evidence hashes, S-13, S-21) or downloads one evidence item compressed or decompressed (GetHvEvidenceQuery); every read is audited. Alternates: the result is not ready (409); the evidence was removed (410, with the hashes and sizes kept); the audit ledger is unavailable (503); a stored payload fails to decompress (500 `evidence_corrupt` before the first byte, or a body that ends incomplete after it, which its stored hash shows). Postconditions: the bytes returned decompress to exactly the bytes stored at admission and during the run. Invariants: I-06, I-11.

![UC-HV-10 Retrieve the result and evidence (use cases), part 1 of 4](c4/rendered/UC-HV-10.svg)

View: UC-HV-10

![UC-HV-10 Retrieve the result and evidence (use cases), part 2 of 4](c4/rendered/UC-HV-10-part2.svg)

View: UC-HV-10-part2

![UC-HV-10 Retrieve the result and evidence (use cases), part 3 of 4](c4/rendered/UC-HV-10-part3.svg)

View: UC-HV-10-part3

![UC-HV-10 Retrieve the result and evidence (use cases), part 4 of 4](c4/rendered/UC-HV-10-part4.svg)

View: UC-HV-10-part4

### 18.12 UC-HV-11 Remove evidence on instruction [FR-MCP-HVPIPE-019]

Actors: operator (S-29: "Retain until told to remove."). Main flow: the operator removes a terminal request's evidence with a reason and a name (RemoveHvEvidenceCommand); the evidence and chunk bytes are removed, while the result rows, findings, turn reviews, hashes, and sizes stay; the removal is audited. Alternates: the request is still active (409); the evidence was already removed and nothing changes. Postconditions: nothing else ever removes evidence (I-11). Invariants: I-10, I-11.

![UC-HV-11 Remove evidence on instruction (use cases), part 1 of 2](c4/rendered/UC-HV-11.svg)

View: UC-HV-11

![UC-HV-11 Remove evidence on instruction (use cases), part 2 of 2](c4/rendered/UC-HV-11-part2.svg)

View: UC-HV-11-part2

### 18.13 UC-HV-12 Remediate after a reviewed turn [FR-MCP-HVPIPE-012, -014]

Actors: model. Trigger: an HV found failures in reviewed turns. Main flow: the model reads the result's failures and their remediation (UC-HV-10), does the corrective work in a later turn, and requests a new HV for the new turns (UC-HV-03) (S-06). Alternates: the model tries to change a reviewed turn and every write is refused with 409 `turn_immutable` (HvTurnFreeze); it tries to write a hostile review itself and is refused with 403 `review_hostile_pipeline_only`; it tries to submit the reviewed turns again and is refused with 409 `hv_turns_already_reviewed` (S-20); an AGREE verdict never moves a TODO, so the coordinator decides completion (S-25). Postconditions: the reviewed history is unchanged; the remediation is new work with its own HV. Invariants: I-01, I-09.

![UC-HV-12 Remediate after a reviewed turn (use cases), part 1 of 4](c4/rendered/UC-HV-12.svg)

View: UC-HV-12

![UC-HV-12 Remediate after a reviewed turn (use cases), part 2 of 4](c4/rendered/UC-HV-12-part2.svg)

View: UC-HV-12-part2

![UC-HV-12 Remediate after a reviewed turn (use cases), part 3 of 4](c4/rendered/UC-HV-12-part3.svg)

View: UC-HV-12-part3

![UC-HV-12 Remediate after a reviewed turn (use cases), part 4 of 4](c4/rendered/UC-HV-12-part4.svg)

View: UC-HV-12-part4

## 19. Requirements, acceptance criteria, and traceability (proposed)

Every identifier here is proposed, and the requirement records exist: the requirement store answered `not_found` for all 63 of `FR-MCP-HVPIPE-001` to `-021`, `TR-MCP-HVPIPE-001` to `-021`, and `TEST-MCP-HVPIPE-001` to `-021` at 2026-10-16T14:50:04 local and again in the pre-check of 2026-10-16T16:32:30 local, and the records and the 21 mapping edges below were then created through the plugin's requirements workflow (`createFrBatch`, `createTrBatch`, `createTestBatch`, `createMapping`), each with status `pending` and every acceptance criterion not satisfied, and read back with 0 mismatches. The plugin's requirements skill directs exactly that: "Capture requirements as they emerge; do not defer to end of session." H0 updates them to the text the hostile validation and the operator's approval settle. Each FR has one TR (`TR-MCP-HVPIPE-<same number>`, the implementation contract: the units, components, and entities named in its line) and one TEST (`TEST-MCP-HVPIPE-<same number>`, the named test cases of its acceptance criteria); the one-to-one numbering is a naming convenience, not a rule, and the mapping edges are explicit below. Acceptance criteria use Given, When, Then; each names the test that proves it, in PascalCase without underscores (CA1707), in the test class named after the unit or component (foundation section 3.5). Cases that need three storage providers run in G3 (`MigrationIntegrationTests` cases), the rest in G2, and the plugin cases in Pester (G4).

### 19.1 Catalog

Problem domains (the foundation's rule of 2026-10-13, "Dividing the new reqs into problem domains would be best split."; each domain owns its requirements, their acceptance criteria, and their mappings): reviewer pool and readiness (FR-001 to FR-003), admission (FR-004 to FR-006, FR-013), scheduling and execution (FR-007 to FR-009), evidence and finalization (FR-010 to FR-012, FR-021), outcomes (FR-014, FR-015), observability (FR-016, FR-017), audit and retention (FR-018, FR-019), and surfaces (FR-020). The domains follow the bounded contexts of section 20.1.

- FR-MCP-HVPIPE-001 Reviewer pool configuration and real-time management. Sources S-02, R-08. UC-HV-01. TR-MCP-HVPIPE-001: HostileValidationOptions, HvReviewer, ListHvReviewersQuery, GetHvReviewerQuery, UpdateHvReviewerCommand, IHvReviewerAdapter.
  - AC001 Given options missing one of the five reviewers, naming an unknown or duplicate reviewer, an unknown strategy, or an enabled Copilot reviewer without a configured model (section 10), when the server starts, then startup fails naming the entry (`HostileValidationOptionsMissingUnknownOrDuplicateReviewerFailsStartup`).
  - AC002 Given a fresh workspace, when the server starts, then five `HvReviewer` rows exist `Unverified`, and a second start does not overwrite a row changed through the API (`ReviewerSeedingInsertsFiveOnceNeverOverwrites`).
  - AC003 Given a reviewer with no configured effort, and a reviewer other than Copilot with no configured model, when its launch is built, then the effort is the highest its CLI accepts (Codex `max`, Claude `max`, Cline `xhigh`, Grok `xhigh`, Copilot `max`; VI-08), never a lower or a refused value, and the model is the CLI default recorded from provenance (`ReviewerAdapterDefaultsToHighestEffortAndCliDefaultModel`).
  - AC004 Given an update with a stale `expectedStateVersion`, when it is applied, then 409 `conflict` with `retryable: false` and `details.field` `expectedStateVersion` and nothing changes (`UpdateReviewerStaleStateVersion409NothingChanged`).
  - AC005 Given an update that changes the strategy, agent path, model, or effort of an enabled reviewer, when it commits, then the reviewer is `Unverified` with reason `configuration_changed` and `ReadinessChangedUtc` set, an `Offline` reviewer's `OpenIncidentId` is cleared while its incident row stays, its `ConfigurationVersion` is one higher, `reviewer_unverified` is appended, and a probe request is stored in the same commit, which a server restart before the next claim does not lose (H1: `UpdateReviewerLaunchChangeUnverifiedNotifiesAndRequestsProbe`).
  - AC006 Given the REST, MCP, client, REPL, and Director surfaces, when each lists, gets, and updates a reviewer, then each returns the same record and the same error codes (`ReviewerSurfacesParityGoldenContract`).
- FR-MCP-HVPIPE-002 Identity normalization, submitter and author exclusion, and round-robin assignment. Sources S-17, S-28, P-01, P-07, R-07, R-08. UC-HV-03, UC-HV-04. TR: AgentIdentity, HvReviewerEligibility, RoundRobinAssignment, HvWorkspaceState.
  - AC001 Given each alias of the alias table in any case and with any separators, when normalized, then it maps to its reviewer id, in C# and in PowerShell from one golden contract (`AgentIdentityGoldenContractTests`, C# and Pester).
  - AC002 Given a submitting turn whose agent is `ClaudeCode` (or `ClaudeCowork`, `claude-code`, `cowork`), when a request is admitted, then Claude is never assigned, and V-5 refuses a run whose reviewer is Claude (`SubmitterAliasesNeverAssignedClaudeFamily`, `FinalVerificationSubmitterReviewerRefusedNotIndependent`).
  - AC003 Given a free-text submitter name that differs from the submitting turn's agent, when a request is admitted, then exclusion uses the turn's agent (`SubmitterExclusionUsesTurnAgentNeverFreeText`).
  - AC004 Given the cursor at Copilot, every reviewer enabled and `Online`, and three requests that Cline submits over Cline sessions (so only Cline is excluded), when they are admitted one after another, then Claude, Codex, and Grok are assigned and the cursor ends at Grok (`RoundRobinAssignsInPoolOrderAfterCursor`).
  - AC005 Given the server restarts between two submissions, when the second is admitted, then it takes the reviewer after the cursor stored before the restart (`RoundRobinCursorSurvivesRestart`).
  - AC006 Given two concurrent submissions, when both commit, then they take the next two eligible reviewers in cyclic order, never the same cursor value, on SQLite, PostgreSQL, and SQL Server (`RoundRobinConcurrentSubmissionsConsecutiveReviewersThreeProviders`).
  - AC007 Given the assignee becomes `Offline` before the claim, when the request is claimed, then it is reassigned to the next eligible reviewer and the reassignment is recorded (`ClaimReassignsWhenAssigneeNoLongerEligible`).
  - AC008 Given a request over a Grok session submitted by Claude, and another submitted by an `Operator`, when each is admitted and claimed, then Grok is never assigned to either, `AuthorReviewerId` is `Grok`, and V-5 refuses a run whose reviewer is Grok with `reviewer_not_independent` (`AuthorAgentNeverAssignedAndRefusedByVerification`).
  - AC009 Given a `Queued` request whose assignee went `Offline`, reviewer A after the cursor eligible but at its `MaxConcurrentRuns`, and reviewer B later in pool order eligible with capacity, when the request is claimed, then B is assigned, the cursor is set to B, and A's runs are unchanged; given every eligible reviewer at its `MaxConcurrentRuns`, then the claim of that request writes nothing, the cursor and the request are unchanged, and the request is claimed once a reviewer has capacity; given the oldest `Queued` request's assignee eligible but at its `MaxConcurrentRuns` and a later `Queued` request whose assignee has capacity, then the later request is claimed and the oldest stays `Queued` with its assignee, not reassigned (H3: `ClaimReassignmentSkipsFullReviewerAndAllFullWritesNothing`, `ClaimSkipsRequestWhoseEligibleAssigneeIsFull`).
- FR-MCP-HVPIPE-003 Reviewer readiness: NONCE, offline, and online. Sources S-15, S-27, P-04, R-10, R-24. UC-HV-02. TR: StartHvReviewerNonceCommand, ClaimHvRunCommand (probe), CloseHvRunOutputCommand, ReadinessProbeVerdict, HvProbeOutcome, HvWorkerHost (startup NONCE).
  - AC001 Given an `Offline` reviewer, when nothing but an execution or a configuration change happens, then it never becomes `Online`; only a passing probe makes it `Online` (`ReviewerBecomesOnlineOnlyByPassingProbe`).
  - AC002 Given a probe whose log echoes the run's nonce, whose provenance names the expected model and effort, and whose reviewer turn exists, of a reviewer that is `Unverified` or `Offline` with an open incident, when it closes, then the reviewer is `Online` with the resolved model and effort, `ReadinessChangedUtc` set, `LastProbeRunId` naming the run, `ReadinessReason` and `OpenIncidentId` cleared while the incident row stays, and `reviewer_online` is appended (`ProbePassedReviewerOnlineResolvedModelAndEffort`).
  - AC003 Given a probe with a wrong nonce, missing or different provenance, or no reviewer turn, when it closes, then the reviewer is `Offline` with `probe_nonce_mismatch`, `runtime_identity_mismatch`, or `reviewer_turn_logging_unsupported` and an incident is `Pending` (`ProbeFailuresOfflineWithCodeAndIncident`); given a probe whose stored chunks fail V-1, then the run is `Failed` with `evidence_corrupt`, an incident `hv-server-evidence_corrupt` is `Pending`, the reviewer's readiness is unchanged, and no probe is requested again (`ProbeEvidenceCorruptServerIncidentReadinessUnchanged`).
  - AC004 Given a probe already requested or running, when another NONCE is requested, then 202 `AlreadyRequested` or `Running` and no second probe exists (`NonceRepeatNoSecondProbe`).
  - AC005 Given a disabled reviewer, when a NONCE is requested, then 409 `hv_reviewer_disabled` (`NonceDisabledReviewer409`).
  - AC006 Given enabled `Unverified` reviewers at startup, when the worker starts, then a probe is requested for each and none for a disabled or `Offline` reviewer, and a startup request refused with 503 `audit_unavailable` is made again at the next iteration, and not again once its reviewer is disabled (409 `hv_reviewer_disabled`) (`WorkerStartupProbesUnverifiedEnabledReviewersOnly`); given an `Unverified` reviewer whose probe of its current configuration settled on a server fault (`evidence_corrupt`), when the worker restarts, then no probe is requested until a caller asks (`WorkerRestartNeverRetriesSettledProbe`).
  - AC007 Given a probe running for configuration version 1 and an update that makes it version 2, when the old probe closes with a passing answer, then its run is `Failed` with `probe_stale_configuration`, the reviewer stays `Unverified` with its new probe request, and only a probe of version 2 can make it `Online` (H3: `StaleProbeNeverQualifiesNewConfiguration`); given instead that the old probe fails to launch, exits nonzero, ends `worker_shutdown`, or has its lease expire after the update, then the reviewer's readiness, reason, probe request, and open incident are unchanged, no reviewer incident is filed, and no probe of version 1 is requested (H3: `StaleProbeFailureShutdownOrExpiryLeavesNewConfigurationUnchanged`).
  - AC008 Given a probe of the reviewer's current configuration whose launch fails or whose process exits nonzero, when FailHvRunCommand records it, then the reviewer is `Offline` with the code and an incident and no request state is written; given a probe ended by `worker_shutdown`, then no incident exists, the reviewer is unchanged, and the probe is requested again (H3: `ProbeFailureOfflineWithIncidentAndShutdownRequestsProbeAgain`).
- FR-MCP-HVPIPE-004 Submission scopes and snapshot membership. Sources S-03, S-09, S-11, R-05, R-26. UC-HV-03. TR: SubmitHvRequestCommand, HvScopeResolution, TurnSnapshotBuilder, HvRequestTurn.
  - AC001 Given a turn scope, a range scope, and a session scope, when each is admitted, then the members are that turn, every turn from start to end inclusive in session order, and every turn of the session at admission (`ScopeResolutionTurnRangeAndSessionMembers`).
  - AC002 Given a session scope submitted from a turn of that session, when admitted, then the submitting turn is not a member and `excludedSubmittingTurn` is true; given a turn or range scope that contains the submitting turn, then 400 `hv_scope_includes_submitting_turn` (`ScopeResolutionSubmittingTurnExcludedOrRefused`).
  - AC003 Given a session with `MaxTurnsPerRequest` plus one turns, when a session scope is submitted, then 400 `hv_scope_too_large` with the count and no request exists (`ScopeResolutionOverMaxRefusedNeverTruncated`).
  - AC004 Given a turn added to the session after admission, when the request finalizes, then the turn is not a member (`SessionScopeSnapshotAtAdmissionLaterTurnNotMember`).
  - AC005 Given a member turn with class `Chore` and hostile review `Unnecessary`, when the request finalizes, then the turn has an assessment and its hostile review holds the HV result (`UnnecessaryTurnIncludedAssessedAndReviewed`).
  - AC006 Given a range whose start is after its end, an endpoint outside the session, or an unknown session, when submitted, then 400 `hv_scope_invalid` naming the field (`ScopeResolutionInvalidRangeOrSession400`).
- FR-MCP-HVPIPE-005 Admission eligibility and refusal with context. Sources S-16, S-20, R-05, R-27. UC-HV-03, UC-HV-12. TR: HvAdmissionEligibility, RoundRobinAssignment, SubmitHvFromPlugin.
  - AC001 Given members that are not terminal, under review, and already reviewed, when submitted, then 409 with the first code of that order and `details` listing every affected turn of every kind (`AdmissionRefusalListsEveryAffectedTurn`).
  - AC002 Given two concurrent submissions over overlapping turns, when both run, then exactly one is admitted and the other is refused `hv_turns_pending`, on three providers (`AdmissionConcurrentOverlapOneWinsThreeProviders`).
  - AC003 Given no eligible reviewer, when submitted, then 409 `hv_no_reviewer_available` with every reviewer's enabled flag, readiness, reason, and open incident, and no request exists (`AdmissionNoEligibleReviewer409WithReviewerContext`).
  - AC004 Given that refusal through the plugin verb, when the model receives it, then the report carries the reviewer statuses, the open incidents, and the NONCE remedy text (`HvSubmitVerbNoReviewerAddsContext`, Pester).
  - AC005 Given the same idempotency key and inputs, when submitted twice, then the second returns 200 with the stored request; with other inputs, 409 `conflict` with `retryable: false` (`SubmitIdempotencyKeyReplayAndReuse`).
  - AC006 Given any submission surface, when a caller asks to re-review, then no force or re-review parameter exists in any request shape (`SubmitRequestShapeHasNoForceFlag`).
- FR-MCP-HVPIPE-006 Request context and requirement references. Sources S-14, R-23. UC-HV-03. TR: RequirementReferenceExtractor, HvRequestRequirementRef.
  - AC001 Given a Markdown or YAML context naming FR, TR, and TEST ids, acceptance-criterion forms, and a use case, when admitted, then each reference is stored in order of first appearance without duplicates, resolved against the effective requirements and use cases, and listed in the brief (`RequirementReferenceExtractorFindsResolvesAndOrders`).
  - AC002 Given a reference that resolves to nothing, when admitted, then it is listed unresolved and the request is admitted (`RequirementReferenceUnresolvedListedNeverRefused`).
  - AC003 Given a context over 65536 characters or an unknown format, when submitted, then 400 `validation_error` naming the field (`SubmitContextTooLongOrUnknownFormat400`).
  - AC004 Given a context holding a secret, when the brief is built, then the secret is redacted in the brief and the stored context is returned redacted on every read surface (`ContextSecretRedactedInBriefAndReads`).
- FR-MCP-HVPIPE-007 Durable scheduling: queue, leases, fences, and restart safety. Sources S-17, S-24, handoff requirement 10, R-07, R-13. UC-HV-04, UC-HV-09. TR: ClaimHvRunCommand, RecordHvRunHeartbeatCommand, ExpireHvRunLeasesCommand, FencedLease, HvRetryDecision, HvWorkerHost.
  - AC001 Given a submission, when it commits, then the worker is woken and claims it without a manual trigger, and when the wake-up is lost (a crash after the commit, W-01) the next poll claims it (`SubmitWakesWorkerAutoStart`).
  - AC002 Given two workers claiming at once, when both run, then exactly one run is claimed per request, on three providers (W-11, `ClaimConcurrentWorkersOneRunPerRequestThreeProviders`).
  - AC003 Given a run whose lease expired before its process started, while it ran, or after its process exited but before its close committed (W-02, W-03, W-04), when the sweep runs, then the run is `Abandoned`, its fence token is incremented, its output stays stored as evidence (L-02), and the request is `Queued` with attempts left; on its last counted attempt it is `Failed`, and with a cancel requested it is `Canceled`, each ended request with its turns `None`, its `Pending` responses `Closed` with `request_ended`, and `hv_failed` or `hv_canceled` appended, exactly as after FailHvRunCommand (`LeaseExpiryAbandonsFencesAndRequeues`, `LeaseExpiryEndsRequestLikeFailedRun`).
  - AC004 Given a worker whose lease was taken over, when it records its start, appends, heartbeats, closes, or fails, then each answers `stale_fence` and nothing changes (`StaleFenceEveryWorkerWriteRefusedNothingChanged`).
  - AC005 Given a request `Finalizing` when the server restarted (W-05), when the sweep runs, then FinalizeHvRequestCommand finalizes it once (`SweepFinalizesFinalizingRequestOnce`).
  - AC006 Given a graceful shutdown during a run, when the worker stops, then the run ends `worker_shutdown`, no incident exists, the reviewer is unchanged, and the attempt is not counted (`GracefulShutdownNotCountedNoIncident`).
  - AC007 Given two registered workspaces, each with queued requests, when each workspace's worker claims, then no run or request crosses workspaces (`WorkerClaimsOnlyItsWorkspace`).
  - AC008 Given a reviewer that has started a descendant, when its worker process is killed abruptly, then the reviewer and every descendant end without any call by the dead worker and without a time limit, and after the lease expires the request is queued again (H3, a scripted fake reviewer and a worker process the test kills: `WorkerDeathEndsReviewerTreeAndRequeues`).
  - AC009 Given a claimed run, when its process starts, then RecordHvRunStartCommand makes it `Running` with `StartedUtc` and `ProcessId` before the `hv_brief` append; given a crash between the process start and that commit, then the run stays `Claimed`, the reviewer ends with its worker, and the lease expiry queues a new attempt; given RecordHvRunStartCommand or RecordHvRunHeartbeatCommand answered with a retryable `conflict`, then the worker repeats the same call, and a repeated start with the same process id after its commit returns the stored start and writes nothing (H3: `RunStartRecordedBeforeBriefAndCrashRecovered`, `RunStartAndHeartbeatRetryableConflictRepeated`).
  - AC010 Given two sweeps that both read run R of a request as expired, the first abandoning R and requeuing the request, and a new run of that request claimed before the second sweep's transaction, when the second sweep runs, then FencedLease `Expire` skips R, R is abandoned once with one server incident, the request keeps its new run and stays `Running`, and policy HvRetryDecision was applied once (H3: `SecondSweepSkipsRunAlreadyTakenOver`).
  - AC011 Given a run abandoned by one sweep and a later run of the same request finalized `Completed`, when a second sweep that had read the first run as expired runs, then the request stays `Completed` with its result, no turn is released, and neither `hv_failed` nor `hv_canceled` is appended (H7: `LateSweepNeverRevivesCompletedRequest`).
- FR-MCP-HVPIPE-008 Execution lifecycle: true live output, heartbeats, no time limit, cancellation. Sources S-18, S-22, P-02, R-11, R-12, R-28. UC-HV-04, UC-HV-08. TR: RunReviewerProcess, StreamReviewerOutput, IHvReviewerAdapter live lines and launch barrier, AppendHvRunOutputCommand, CancelHvRequestCommand.
  - AC001 Given a reviewer that writes lines over time, including a single line followed by silence, when it runs, then every line is appended within `OutputFlushInterval` of being written, in order, whether or not another line arrives (H3: `OutputLinesAppendedWithinFlushIntervalInOrder`).
  - AC002 Given a buffer reaching `OutputFlushBytes`, when the next line arrives, then the buffer is appended at once (`OutputFlushedAtByteThreshold`).
  - AC003 Given a scripted fake reviewer that writes one line and then blocks on a gate the test controls, when the test reads the run's output stream, then the line arrives while the gate is closed and the process is still running (operator-confirmed P8, `LiveOutputLineObservableBeforeReviewerExits`).
  - AC004 Given a reviewer running longer than any interval the configuration names, when nothing else happens, then no unit or process ends it (`ReviewerRunHasNoTimeLimit`).
  - AC005 Given a `Queued` request, when canceled, then it is `Canceled`, its turns are `None`, and `hv_canceled` is appended; given a `Running` request, then it becomes `CancelRequested`, and at the next heartbeat the process is stopped and the request is `Canceled` (`CancelQueuedImmediateRunningAtNextHeartbeat`).
  - AC006 Given a cancel racing a finalization, a claim, or a close of one request (W-10), when both run, then exactly one commits and the other sees its state, on three providers (`CancelVersusFinalizeOneCommitsThreeProviders`).
  - AC007 Given a `Finalizing`, `Completed`, or `Failed` request, when canceled, then 409 `hv_request_not_cancelable` (`CancelTerminalOrFinalizing409`).
  - AC008 Given an interactive session of a reviewer's agent live in the workspace, when a review run and a probe run of that reviewer start and end, then the interactive session's plugin state files and its session-log session are byte-for-byte unchanged, and the reviewer's turn is in a session of its own (H1 Pester per plugin host, and the live check: `ReviewerRunLeavesSameAgentLiveSessionUnchanged`).
  - AC009 Given each adapter's launch and a reviewer told to create or change a workspace file, when the run ends, then the attempt was refused or the tool was never offered, and the workspace tree outside the plugin's own host-session state is unchanged (H1, per adapter, with the probe streams of 2026-10-16 as fixtures: `ReviewerRunCannotChangeWorkspaceFilesPerAdapter`).
  - AC010 Given a server-launched reviewer whose host enforces the foundation's Stop gate, when it has emitted its verdict record, then its own `Chore` turn is closed through the completion gate and the process exits on its own, never held open by the gate (H1 Pester per host and the live check: `ReviewerClosesOwnTurnAndExitsUnderStopGate`).
  - AC011 Given a scripted fake reviewer that starts a child process which keeps the reviewer's output pipes open, when its run is canceled, its fence goes stale, or the ledger fails, and also when the reviewer's root process exits on its own first, then the reviewer and every descendant are ended, the output is read to its end, and the attempt ends with no process left running (H3: `ReviewerStopEndsWholeProcessTree`).
  - AC012 Given a cancel that commits after the run's last heartbeat and before its process exits with code 0, when CloseHvRunOutputCommand runs, then the run is `Canceled`, the request is `Canceled` with its turns released and `hv_canceled` appended, and no finalization starts; a repeat cancel of a `CancelRequested` or `Canceled` request returns 200 unchanged, on three providers (H3: `CancelCommittedBeforeCloseWinsOverFinalizing`).
- FR-MCP-HVPIPE-009 Execution failures: incidents through triage, offline, retry bound. Sources S-15, S-26, R-17. UC-HV-09. TR: FailHvRunCommand, FileHvIncidentCommand, HvIncident, HvRetryDecision.
  - AC001 Given a reviewer-attributable failure, when recorded, then the reviewer is `Offline` with the code and incident, and an incident is `Pending` with `dedupeKey` `hv-reviewer-<reviewerId>-<code>` (`ReviewerFailureOfflineWithIncident`).
  - AC002 Given a pending incident, when filed, then the triage service receives its `dedupeKey` and the incident records the report and group ids (`IncidentFiledThroughTriageWithDedupeKey`).
  - AC003 Given the same failure twice, or one filing retried after triage accepted it but before its report id was recorded (W-09, L-03), when both are filed, then triage groups them in one group (`RepeatedFailureSameTriageGroup`).
  - AC004 Given triage unreachable, or a crash after the incident committed and before its filing (W-08), when filing, then the incident stays `Pending` with the error and the next sweep files it (`IncidentFilingRetriedUntilFiled`).
  - AC005 Given attempts spent, when the last attempt fails, then the request is `Failed` with `attempts_exhausted`, its turns are `None`, and `hv_failed` is appended (`AttemptsExhaustedRequestFailedTurnsReleased`).
  - AC006 Given a server-attributable failure (`evidence_corrupt`, `snapshot_changed`, `model_grade_conflict`, `reviewer_not_independent`), when recorded, then an incident with `hv-server-<code>` exists and the reviewer is unchanged (`ServerFailureIncidentReviewerUnchanged`).
  - AC007 Given an unwritable ledger, when an incident is filed, then the triage service is never called and the incident stays `Pending`; given a ledger that fails after triage accepted, then the incident stays `Pending` and the retry joins the same triage group; given a filing that succeeds, then `Filed` and `incident_filed` commit in one transaction (H3: `IncidentFilingNeverCallsTriageWhileLedgerUnavailable`).
  - AC008 Given a worker FailHvRunCommand (for a corrupt brief, a launch failure, a cancel, a corrupt append, or a nonzero exit) answered 503 `audit_unavailable` or `stale_fence`, when the worker handles it (FailAttempt), then the attempt ends with no further write, the request's status and version are unchanged, and the lease expiry later recovers the run; given one answered with a retryable `conflict`, then the same call is repeated; given FileHvIncidentCommand answered 503 `audit_unavailable` at the sweep, then the worker host claims nothing in that iteration (H3: `FailAttemptRefusalEndsAttemptWithoutStateClaim`, `WorkerIterationStopsWhenIncidentFilingAuditUnavailable`).
- FR-MCP-HVPIPE-010 Reviewer output contract, per-turn and holistic evaluation. Sources S-03, S-10, S-13, S-21, R-31. UC-HV-05. TR: reviewer contract v1, IHvReviewerAdapter ExtractReviewerRecords, HvFinalVerification V-4 and V-8, HvTurnAssessment, HvFinding.
  - AC001 Given a log with one assessment per member, one holistic record, and one verdict record last, when finalized, then each requested turn has its score, the holistic evaluation is stored separately, and the run's `VerdictRecordSha256` is the SHA-256 of the verdict record in its stored normal form (`FinalizeStoresPerTurnScoresAndSeparateHolistic`).
  - AC002 Given a log missing an assessment, with two verdict records, without a holistic record, or with a finding without remediation, when finalized, then `reviewer_output_invalid` and nothing is committed (`FinalizeIncompleteOutputRefusedNothingCommitted`).
  - AC003 Given a holistic correctness of 0.97 with every turn AGREE, when finalized, then the overall verdict is DISAGREE (`OverallVerdictHolisticBelow098Disagree`); given an AGREE verdict with a FAIL finding, then `reviewer_output_invalid` (`VerdictAgreeWithFailFindingRefused`).
  - AC004 Given turn scores whose mean differs from the holistic scores, when finalized, then the stored holistic scores are the reviewer's own, never recomputed (`HolisticScoresNeverComputedFromTurnScores`).
  - AC005 Given a record the reviewer forged as `hv_provenance` or `hv_brief` inside its message text, when the log is read, then it is not accepted as a worker record (`ForgedWorkerRecordInMessageTextIgnored`).
  - AC006 Given native streams of each CLI, recorded from the probe runs of 2026-10-16, in which a record is split across events (Grok's per-token text), repeated in a delta and in the whole message (Copilot), inside a fenced block, or beside other text on its line, when the records are extracted as the lines arrive and again over the stored log, then each record is extracted exactly once, the line with other text is free text, and both extractions return the same records (`ReviewerRecordsExtractedOnceFromAssembledMessagesPerCli`).
  - AC007 Given a run that exits with code 0 and holds no reviewer records (an empty output, or a stream its CLI ended `cancelled`), when it closes, then a review fails with `reviewer_output_invalid`, the reviewer is `Offline` with an incident, and a probe fails with `probe_nonce_mismatch`; an exit code never stands in for a verdict (H7, where V-8 refuses the review: `ExitZeroWithoutReviewerRecordsIsNotAReview`).
  - AC008 Given a reviewer turn that holds only the `hv_verdict` record, one that is still `in_progress`, one closed `failed`, one not classified `Chore`, or one created before the run's claim, when the request finalizes, then V-6 refuses each with `reviewer_turn_unverified`; given a completed `Chore` turn holding every reviewer record of the run in order, then V-6 passes (H7: `ReviewerTurnMustBeCompletedChoreHoldingEveryRecord`).
  - AC009 Given an AGREE holistic verdict at 0.99 and every turn AGREE, but one response grade DISAGREE, or one response finding UNKNOWN, when finalized, then the overall verdict is DISAGREE, and a reviewer that reported AGREE there is refused by V-4 with `reviewer_output_invalid` (H7: `OverallVerdictOnePredicateOverTurnsHolisticResponsesFindings`).
  - AC010 Given an admitted response whose grade is a score-only DISAGREE (scores below 0.98, every claim finding PASS) and whose response is rejected or superseded before V-4 reads it, or after V-4 and before the commit, with every other verdict AGREE, when finalized, then V-4 accepts the reviewer's overall DISAGREE, the stored overall verdict is DISAGREE, and no model grade is applied to the closed response (H7: `OverallVerdictBindsAdmittedResponsesNotLaterState`).
- FR-MCP-HVPIPE-011 Evidence completeness, GZIP at rest, round-trip verification. Sources S-13, S-30, R-12, R-14. UC-HV-04, UC-HV-05, UC-HV-10. TR: GzipCodec, HvEvidence, HvRunOutputChunk, V-1.
  - AC001 Given an accepted request and a run's lines, when stored, then every payload is a GZIP member that decompresses to the exact bytes, and no uncompressed copy is stored (`EvidenceStoredGzipOnlyRoundTripExact`).
  - AC002 Given a chunk whose bytes, SHA-256, or chain hash was altered, or a missing sequence, when finalized, then `evidence_corrupt` and nothing is committed (`FinalizeCorruptOrTruncatedLogRefused`).
  - AC003 Given a compression whose round trip fails, when admitted or appended, then 500 `evidence_corrupt` and nothing is saved (`CompressionRoundTripFailureNothingSaved`); for an append (the sequence 1 `hv_brief` append, a flush, or the final flush), and for one answered with a non-retryable `conflict` or a 400 `validation_error`, the worker appends no later sequence, stops the reviewer and fails the run with `evidence_corrupt`, so an incident `hv-server-evidence_corrupt` is `Pending`, the reviewer is unchanged, and the request goes through policy HvRetryDecision (`AppendEvidenceCorruptFailsRunAsServerFault`).
  - AC004 Given a finalized run, when the response log is downloaded compressed and read back as one GZIP stream (VI-07), then decompression yields every stored line in order and its SHA-256 chain equals the run's (`ResponseLogDownloadMultiMemberGzipMatchesChain`).
  - AC005 Given a reviewer output of any size, when appended, then no limit truncates it (`OutputHasNoSizeLimitNeverTruncated`).
  - AC006 Given a finalized request, when its `Result` evidence is read back, then the stored payload holds no `evidence.result` digest, its SHA-256 equals `ResultSha256`, and the returned document's `evidence.result.sha256` equals that value (H7: `ResultEvidenceDigestExcludesItselfAndRoundTrips`).
  - AC007 Given a result document whose compression fails its round trip inside the finalization transaction, when finalized, then the transaction rolls back and nothing of the result is visible, and the run fails with `evidence_corrupt` as a server fault (an incident `hv-server-evidence_corrupt`, the reviewer unchanged) through policy HvRetryDecision, so the request's attempts bound the retries (H7: `ResultCompressionFailureFailsRunAsServerFault`).
  - AC008 Given a stored payload that fails to decompress, when GetHvRequestQuery returns its last output (directly or for a SubmitHvRequestCommand replay), GetHvResultQuery assembles the result, GetHvEvidenceQuery decompresses it, or the output stream sends it, then the read is recorded first, each query and the replay answer HTTP 500 with code `evidence_corrupt` before their first byte or end their body incomplete after it, and the stream sends the event `hv_stream_error` whose data field `code` is `evidence_corrupt` and ends (H6: `CorruptStoredPayloadReadAnswersEvidenceCorrupt`).
  - AC009 Given a claimed review run whose `Request` evidence fails to decompress, when the worker builds its brief, then the read is recorded first, no reviewer process starts, and the run fails with `evidence_corrupt` as a server fault (an incident `hv-server-evidence_corrupt`, the reviewer unchanged) through policy HvRetryDecision, so the request's attempts bound the retries (H3: `CorruptRequestEvidenceAtBriefFailsRunAsServerFault`).
- FR-MCP-HVPIPE-012 Atomic finalization and turn finality. Sources S-06, S-07, S-23, R-04, R-15. UC-HV-05, UC-HV-12. TR: FinalizeHvRequestCommand, CommitFinalResult, HvTurnFreeze, A-07, A-08, A-11.
  - AC001 Given a fault injected after each write of CommitFinalResult (W-06), when finalization runs, then nothing of the result is visible after the rollback, the request stays `Finalizing`, and the next sweep finalizes it, on three providers (`FinalizeFaultAfterEachWriteNothingVisibleThreeProviders`).
  - AC002 Given an admitted turn, when any turn write, review write, review reset, section write, dialog append, title change, or session replace or delete targets it from any origin but `HvFinalization`, then 409 `turn_immutable` with `hv_pending`, and after finalization with `hv_final` (`FrozenTurnEveryWritePathRefused`).
  - AC003 Given a canceled or failed request, when it ends, then its turns are writable again (`EndedRequestReleasesTurns`).
  - AC004 Given a request finalized, when FinalizeHvRequestCommand runs again, then it returns the stored outcome and writes nothing (`FinalizeTwiceSecondReturnsStoredNothingWritten`).
  - AC005 Given a member turn changed by a path that bypasses the guard (a fixture writing the store directly), when finalized, then V-2 refuses with `snapshot_changed` (`FinalizeSnapshotChangedRefused`).
  - AC006 Given a `Code` turn completed with hostile review `Other` "HV requested", when its HV finalizes, then its hostile review holds the HV result and its status stays `completed` (`FinalizeReplacesOtherPendingHvReviewKeepsStatus`).
  - AC007 Given turns whose hostile review is `Pass` or `Fail`, `Other`, or absent when the A-11 migration adds the HV columns, when the migration runs, then exactly the `Pass` and `Fail` turns are `Final` with no request id, every write to them is refused with 409 `turn_immutable` `hv_final`, and an HV over them is refused with 409 `hv_turns_already_reviewed`, on three providers (the migration is built in HvStorageSchema; the criterion binds in H2, where the write guard and admission exist: `MigrationMarksHvReviewedTurnsFinalThreeProviders`).
  - AC008 Given two finalizers of one request, one committing the result and one whose verification failed on reads taken before that commit, when both run, then the request stays `Completed` with its result and the failing finalizer's FailHvRunCommand changes nothing and returns `already_completed`, on three providers (H7: `LateVerificationFailureCannotUndoCommittedFinalization`).
  - AC009 Given a verification failure whose FailHvRunCommand returns 503 `audit_unavailable`, 409 `conflict`, `stale_fence`, `not_finalizable`, or `already_completed`, when FinalizeHvRequestCommand reports, then it returns that outcome (`stale_fence` as `not_finalizable`), the request stays as FailHvRunCommand left it (`Finalizing` for a refusal), the worker reports that state, and the next sweep finalizes it; given a request whose state changed between the reads and the commit, then the unit starts again from its status check (H7: `FinalizerPropagatesEveryFailRunOutcome`).
- FR-MCP-HVPIPE-013 Self-review independence. Sources S-28, R-21. UC-HV-03, UC-HV-05. TR: HvAdmissionEligibility, TurnSnapshotBuilder, HvFinalVerification.
  - AC001 Given one turn with no agent review, one with `Unstarted`, one with `Running`, one with `Pass` at 1.0, one with `Fail` at 0.1, and one with `Other`, all otherwise equal, when each is submitted, then admission, assignment, and the brief bytes are identical (`SelfReviewVariedAdmissionAssignmentAndBriefIdentical`).
  - AC002 Given those turns reviewed with the same reviewer output, when finalized, then the stored hostile reviews and verdicts are identical (`SelfReviewVariedVerdictIdentical`).
  - AC003 Given the HostileValidation assemblies, when scanned, then no HV unit, policy, or process reads an `agentReview` field (architecture test `HostileValidationNeverReadsAgentReview`).
- FR-MCP-HVPIPE-014 Verdict only: no TODO automation. Sources S-25, R-22. UC-HV-05, UC-HV-12. TR: FinalizeHvRequestCommand.
  - AC001 Given member turns naming TODOs, when an HV finalizes AGREE or DISAGREE, then every TODO is byte-for-byte unchanged (`FinalizeNeverChangesTodos`).
  - AC002 Given the HostileValidation assemblies, when scanned, then none references the TODO write services (architecture test `HostileValidationHasNoTodoWriteDependency`).
- FR-MCP-HVPIPE-015 Results, evidence retrieval, and the standard result document. Sources S-13, S-18, S-21. UC-HV-06, UC-HV-10. TR: GetHvRequestQuery, QueryHvRequestsQuery, GetHvResultQuery, GetHvEvidenceQuery, HvResultDocument.
  - AC001 Given a completed request, when its result is read, then it holds the complete accepted request, every log record in order, every requested turn with its score, the holistic evaluation, and every FAIL and UNKNOWN with remediation (`ResultDocumentCompleteAndDelineated`).
  - AC002 Given a request not completed, when its result is read, then 409 `hv_result_not_ready` (`ResultBeforeCompletion409`).
  - AC003 Given running requests, when queried with `status` `Running`, then each has its reviewer, progress, ETA status, and, when read singly, its last 20 lines (`RunningRequestsQueryAndLastOutput`).
  - AC004 Given the REST, MCP, client, REPL, and Director surfaces, when each submits, reads, queries, cancels, and retrieves, then each returns the same shapes and codes (`HvSurfacesParityGoldenContract`).
- FR-MCP-HVPIPE-016 Live output stream, progress, and honest ETA. Sources S-18, S-22, P-03, R-12, R-18. UC-HV-06. TR: StreamHvRunOutputQuery, OrderedEventReplay.
  - AC001 Given a stream attached mid-run, when the caller reconnects with its last event id, then it receives exactly the missed chunks and then live chunks (`OutputStreamReconnectExactlyMissedEvents`).
  - AC002 Given a progress record with a percentage and an ETA, when read, then `etaStatus` is `Reported` with those values; given none, then `Unknown` with no number (`EtaReportedOrUnknownNeverEstimated`).
  - AC003 Given a second attempt, when streamed, then the stream continues with the new run's events (`OutputStreamContinuesAcrossAttempts`).
  - AC004 Given a malformed `Last-Event-ID`, when attaching, then 400 `validation_error` (`OutputStreamMalformedLastEventId400`).
  - AC005 Given a request whose evidence was removed, when a caller attaches to its output stream, then 410 `hv_evidence_removed` and no chunk is sent (H6: `OutputStreamAfterRemoval410`).
- FR-MCP-HVPIPE-017 Completion notification stream. Sources S-19, R-16, R-30. UC-HV-07. TR: NotificationOutbox, StreamHvNotificationsQuery, QueryHvNotificationsQuery, WatchHvNotifications.
  - AC001 Given a state change and its notification in one transaction, when the transaction rolls back, then no notification is visible; when it commits, the notification is visible with the next sequence and no gap (`NotificationOutboxCommittedWithChangeGapFree`).
  - AC002 Given a notification stream that drops, or a server that stops after a commit and before any stream delivered its notification (W-07), when the stream reconnects with its last sequence, then it receives every later notification exactly once (`NotificationStreamReconnectNoLossNoDuplicate`).
  - AC003 Given the plugin watcher and a completion of a request its host session submitted, when the next prompt arrives, then the model's context holds the completion once (`HvWatcherDeliversOwnCompletionOnce`, Pester).
  - AC004 Given a completion of another host session's request, when the watcher receives it, then it records the sequence and inboxes nothing (`HvWatcherIgnoresOtherSessionsRequests`, Pester).
  - AC005 Given a watcher killed after receiving a completion and before writing its state, and one killed during the write, when it restarts, then its state file holds either the old cursor and inbox or the new ones, never one without the other, and the replay from the recorded sequence inboxes the completion exactly once by its sequence (H5, after H6: `HvWatcherCursorAndInboxOneReplaceCrashSafe`, Pester).
  - AC006 Given a completion whose notification arrives before the submit verb has returned, when the watcher receives it, then it is inboxed for this host session by the submitter turn key it carries (H5, after H6: `HvWatcherInboxesCompletionBeforeSubmitReturns`, Pester).
  - AC007 Given a plugin that crashed after adding an inbox entry to a prompt and before marking it delivered, when the next prompt arrives, then the entry is added again with the same sequence (at least once, R-30) (H5: `HvWatcherRedeliversUndeliveredEntryWithSequence`, Pester).
- FR-MCP-HVPIPE-018 Audit participation and hard stop. Sources S-29, R-19. Every UC. TR: AuditLedgerGuard, the HV entities' `IsDeleted`.
  - AC001 Given every HV command, when it commits, then a ledger row exists for each entity it changed (`EveryHvMutationHasLedgerRows`).
  - AC002 Given the ledger unwritable, when any HV command runs, then 503 `audit_unavailable` and nothing is written (`AuditUnavailableEveryHvCommandRefusedNothingWritten`).
  - AC003 Given an evidence read (result, evidence, output stream, last output, and the last output of a same-key submission replay after the run has output), when it returns bytes, then a committed `read` ledger row exists, and with the ledger unwritable the read is refused (`EvidenceReadsAuditedOrRefused`); given each internal read (the accepted request the worker puts in a brief, the chunks finalization verifies, the chunks of a probe at close) with the ledger unwritable, then no byte is read: no reviewer is launched and the lease expiry recovers the attempt, the request stays `Finalizing`, and the close is refused with nothing written; given a probe close or a finalization whose transaction rolls back, conflicts, or fails to commit after the chunks were read, then the read row stays committed (`InternalEvidenceReadsAuditedOrRefused`).
  - AC004 Given the ledger unwritable during a run (W-12), when the worker appends the sequence 1 `hv_brief` record, a flush, or the final flush, then it stops the reviewer, appends no later sequence, and writes nothing, and the lease expiry recovers the request later (`AuditFailureDuringRunHardStop`).
  - AC005 Given evidence written (an appended chunk and a stored evidence item) and then removed, when the removal commits, then every ledger row written before it is byte-for-byte unchanged, the ledger still holds the history of those writes, the removal adds its own rows, and an architecture scan finds no HV unit or process that updates or deletes a ledger row (I-13, `EvidenceRemovalLeavesAuditLedgerUnchanged`).
- FR-MCP-HVPIPE-019 Retention until explicit removal. Sources S-29, R-20. UC-HV-11. TR: RemoveHvEvidenceCommand.
  - AC001 Given evidence of any age, when any unit or process other than RemoveHvEvidenceCommand runs, then no evidence or chunk payload is removed (`NoAutomaticEvidenceRemoval`, with an architecture scan for payload writes).
  - AC002 Given a terminal request, when its evidence is removed with a reason and a name, then the payloads in the evidence tables are null, every audit ledger row is unchanged (I-13), the hashes, sizes, result rows, findings, and turn reviews stay, and the removal is audited (`RemoveEvidenceKeepsResultAndHashes`).
  - AC003 Given an active request, when removal is asked, then 409 `hv_request_active` (`RemoveEvidenceActiveRequest409`).
  - AC004 Given removed evidence, when read, then 410 `hv_evidence_removed` with the kept hashes and sizes (`RemovedEvidenceRead410WithHashes`).
  - AC005 Given removed evidence, when every surface that reads a payload is used (GetHvRequestQuery's last output, StreamHvRunOutputQuery, GetHvResultQuery, GetHvEvidenceQuery), then none decompresses a removed payload: the request read returns no output line with `outputRemoved` true and records no read, and the other three answer 410 `hv_evidence_removed`; the chunks keep `RemovedUtc` and the evidence rows keep `RemovedBy` and `RemovalReason` (H6: `RemovedEvidenceEveryReadSurfaceAnswersRemoved`).
- FR-MCP-HVPIPE-020 Surface parity: REST, MCP, client, REPL, Director, and plugin. Sources S-02, S-16, S-19, R-29, R-30. Every UC. TR: HostileValidationController, FwhMcpTools.HostileValidation, HostileValidationClient, HostileValidationWorkflow, HostileValidationDirectorCommands, the plugin verbs and watcher.
  - AC001 Given the golden contract of the public units, when each surface runs it, then the shapes, statuses, and codes match (`HvSurfacesParityGoldenContract`, shared with FR-MCP-HVPIPE-015-AC004).
  - AC002 Given the internal units, when the routes, tools, client, and REPL are scanned, then none exposes them (`HvInternalUnitsHaveNoPublicSurface`).
  - AC003 Given the plugin's submit verb, when it runs, then it binds the submitting turn and builds a stable idempotency key for the same call (`HvSubmitVerbBindsTurnStableKey`, Pester).
  - AC004 Given a retryable answer to the plugin's submit verb, when it returns, then it reports `retryable: true` with exit 1 and writes no failsafe record (`HvSubmitVerbRetryableNotQueued`, Pester).
  - AC005 Given the REPL client mutation policy, when it classifies `hostileValidation`, then writes are service-gated and reads are allowed (`ReplPolicyHostileValidationClassification`).
  - AC006 Given a submitting turn whose request id has 256 characters, and two host sessions whose turns share one request id, when the submit verb builds its keys, then each key has 35 characters, the two sessions' keys differ, and changing only `contextFormat` changes the key (H5: `HvSubmitKeyBoundedAndCoversInvocationIdentity`, Pester).
- FR-MCP-HVPIPE-021 Response grading and trust through finalization. Sources the foundation's Phase C, R-02, R-03, A-04, A-05. UC-HV-05. TR: HvRequestResponse, FinalizeHvRequestCommand, RecordModelGradeCommand (amended).
  - AC001 Given member turns with agent responses and Mode not `Off`, when finalized, then each pending response gets one model grade from the run's reviewer and its trust decision, in the finalization transaction (`FinalizeGradesLinkedResponsesInSameTransaction`).
  - AC002 Given Mode `Off` at finalization, when finalized, then the linked responses are `NotGraded` with `agent_context_disabled`, the result's `responseGrades` entries carry disposition `NotGraded` with that `notGradedReason`, and the turns are finalized (`FinalizeModeOffResponsesNotGraded`).
  - AC003 Given a response rejected or superseded while pending, when the transition commits, then its item is `Closed` with that reason and the request's state is unchanged (`ResponseTransitionClosesHvItemOnly`).
  - AC004 Given the public model-grade route and tool of the foundation, when the surfaces are scanned, then neither exists (`ModelGradeHasNoPublicSurface`).
  - AC005 Given a response grade with a FAIL finding that lacks a remediation, when finalized, then V-8 refuses with `reviewer_output_invalid`; given complete FAIL and UNKNOWN response findings, then each is stored as an `HvFinding` of scope `Response` with its response id and check, appears in the result's `failures` and in its `responseGrades` entry, and the overall verdict is DISAGREE (H7: `ResponseGradeFailingFindingsKeptWithRemediation`).
  - AC006 Given an admitted response that is rejected or superseded while the review runs, when the reviewer grades it as admitted, then V-3 and V-8 pass, the grade record is kept in the result with disposition `Closed`, its FAIL and UNKNOWN findings are `HvFinding` rows of scope `Response` that remain after evidence removal, no model grade is recorded for it, and the other responses are graded (H7: `ResponseClosedMidReviewGradeKeptNotApplied`).
  - AC007 Given finalization with Mode not `Off`, when it dispatches RecordModelGradeCommand inside its transaction, then the unit takes no receipt path or thread, runs no staleness dispatch and no receipt check, accepts the request in `Finalizing`, and never changes the HV request's state, and a repeat with equal input returns the stored grade; given a stored grade with other input for the same response and request (a fixture writing the store directly), then the finalization transaction rolls back and the run fails with `model_grade_conflict` as a server fault, so the request's attempts bound the retries (H7: `ModelGradeInternalUnitInsideFinalization`).

### 19.2 Settled requirements to the catalog

- S-01 FR-001 to FR-021 (the pipeline). S-02 FR-001, FR-020. S-03 FR-004, FR-010, FR-012. S-04 foundation Phase B (unchanged), FR-012. S-05 section 1. S-06 FR-012, FR-005. S-07 FR-012, FR-012-AC007. S-08 superseded by S-28. S-09 FR-004-AC005. S-10 FR-010-AC001, -AC004. S-11 FR-004-AC001. S-12 R-21 (the foundation's Stop-gate remedy; not an HV input). S-13 FR-010, FR-011, FR-015. S-14 FR-006. S-15 FR-003, FR-009. S-16 FR-005-AC003, -AC004. S-17 FR-002. S-18 FR-008, FR-015, FR-016. S-19 FR-017. S-20 FR-005-AC006, FR-012. S-21 FR-010-AC001, FR-015-AC001. S-22 FR-008-AC001 to -AC003, FR-016. S-23 FR-012-AC001. S-24 FR-007-AC001. S-25 FR-014. S-26 FR-009-AC002, -AC003. S-27 FR-003. S-28 FR-013. S-29 FR-018, FR-019. S-30 FR-011. S-31 section 20 (the implementation worktree). S-32 sections 19 and 20 (H0). S-33 section 20 (BDPv4).
- P-01 FR-002, V-5. P-02 FR-008-AC004, -AC011. P-03 FR-016-AC002. P-04 FR-003, V-7. P-05 FR-010-AC003. P-06 V-6, R-24. P-07 FR-002-AC008, V-5.
- The handoff's adversarial coverage list: alias exclusion FR-002-AC002; round-robin persistence FR-002-AC005; snapshot consistency FR-004-AC004, FR-012-AC005; stale-worker rejection FR-007-AC004; cancellation and finalization races FR-008-AC006; evidence completeness FR-011-AC002; lossless GZIP round trips FR-011-AC001; corrupt or truncated input FR-011-AC002; audit failures FR-018-AC002, -AC004; separate per-turn and holistic outputs FR-010-AC001; indefinite retention FR-019-AC001; no permanent raw duplicate FR-011-AC001; streaming FR-008-AC003, FR-016-AC001; immutable results and turns FR-012-AC002; remediation as new work FR-005-AC001 and UC-HV-12; self-review variance FR-013; no TODO completion FR-014.

### 19.3 Mapping edges

Each FR-MCP-HVPIPE-NNN maps to TR-MCP-HVPIPE-NNN and TEST-MCP-HVPIPE-NNN (21 FR-to-TR edges and 21 FR-to-TEST edges, created on 2026-10-16 as 21 mappings); FR-MCP-HVPIPE-015-AC004 and FR-MCP-HVPIPE-020-AC001 share one case, recorded on both TESTs. Existing records this amendment updates, each through H0's requirements workflow and never by editing the exported markdown: the foundation's reserved FR-MCP-SESSIONREVIEW-005 (A-06), FR-MCP-AGENTCONTEXT-004 (A-01 to A-05), and the use cases of foundation sections 7.8 and 7.12 (A-10), which are reserved for the foundation's P0 and absent from the store today (not found on 2026-10-16T14:02:23 local), so H0 coordinates their creation with PLAN-SESSIONTURNREVIEW-001's P0 instead of updating records that do not exist. The existing records through which the problems of section 21 are fixed, each updated the same way and only in H0, after the operator's approval (D-09):
- TEST-MCP-HOSTILEREVIEW-002 and -003 gain the named tests of section 16.7, beside the test names their descriptions already hold; FR and TR-MCP-HOSTILEREVIEW-001 to -006 keep their texts (R-25).
- FR, TR, and TEST-MCP-PATHNORM-001 are imported into this workspace's store with their stored text (section 20.4; `not_found` here on 2026-10-17), then their criteria are strengthened for P10, P12, and P13 and the TEST gains their named tests.
- FR-MCP-SESSIONLIFE-004 criterion 1 (P14) and FR-MCP-SESSIONLIFE-005 criteria 1 (P15) and 2 (P11) are strengthened for the case each problem shows, and TEST-MCP-SESSIONLIFE-002, which both map to, gains the named tests of section 20.4.

### 19.4 The phase that binds each acceptance criterion

Every criterion's test is written in H0 and proven there against mocks with its negative controls (BDPv4). The phase named below binds the same test to production: it is the first phase of section 20.1 in which everything the criterion depends on exists, so no criterion binds before its dependency. "Live check" marks a criterion that also runs end to end in the live check of section 20.2, on a separate server instance; for FR-MCP-HVPIPE-008-AC008 to AC010, the H1 binding is the per-host Pester test of the adapter and the plugin launched by a test harness, and the server-launched form waits for the live check, after H3 and H6 exist.

- FR-MCP-HVPIPE-001: AC001 to AC005 H1; AC006 H5.
- FR-MCP-HVPIPE-002: AC001 H1; AC002 H7 (its admission half binds in H2, its V-5 half in H7); AC003 to AC006 H2; AC007 and AC009 H3; AC008 H7.
- FR-MCP-HVPIPE-003: AC001 to AC003 H3; AC004 and AC005 H1; AC006 to AC008 H3; AC002 and AC003 also live check.
- FR-MCP-HVPIPE-004: AC001 to AC003 H2; AC004 and AC005 H7; AC006 H2.
- FR-MCP-HVPIPE-005: AC001 to AC003 and AC005 H2; AC004 and AC006 H5.
- FR-MCP-HVPIPE-006: AC001 to AC003 H2; AC004 H5 (the redacted brief binds in H2, the read surfaces in H5).
- FR-MCP-HVPIPE-007: AC001 to AC004 and AC006 to AC010 H3; AC005 and AC011 H7; AC003 and AC008 also live check.
- FR-MCP-HVPIPE-008: AC001 and AC002 H3; AC003 H6 (the chunk is stored while the gate is closed from H3; the line is observed on the output stream from H6), also live check; AC004 and AC005 H3; AC006 H7; AC007 H2; AC008 to AC010 H1 and live check (AC010 after P11, P14, and P15 are fixed, section 20.4); AC011 and AC012 H3.
- FR-MCP-HVPIPE-009: AC001 to AC005, AC007, and AC008 H3; AC006 H7.
- FR-MCP-HVPIPE-010: AC001 to AC004 and AC007 to AC010 H7 (AC007's review half is V-8's refusal, which exists only in H7); AC005 and AC006 H1.
- FR-MCP-HVPIPE-011: AC001, AC003, AC005, and AC009 H3; AC002, AC006, and AC007 H7; AC004 and AC008 H6.
- FR-MCP-HVPIPE-012: AC001, AC004 to AC006, AC008, and AC009 H7; AC002 and AC007 H2 (AC007's migration is built in HvStorageSchema, and its write-guard and admission assertions need H2); AC003 H3.
- FR-MCP-HVPIPE-013: AC001 H2; AC002 and AC003 H7.
- FR-MCP-HVPIPE-014: AC001 and AC002 H7.
- FR-MCP-HVPIPE-015: AC001 and AC002 H6; AC003 H3; AC004 H5.
- FR-MCP-HVPIPE-016: AC001 and AC003 to AC005 H6; AC002 H3.
- FR-MCP-HVPIPE-017: AC001 HvSharedComponents; AC002 H6; AC003 to AC007 H5, also live check for AC003.
- FR-MCP-HVPIPE-018: AC001 to AC003 and AC005 H6 (the last phase that adds an HV command or read); AC004 H3.
- FR-MCP-HVPIPE-019: AC001 to AC005 H6.
- FR-MCP-HVPIPE-020: AC001 to AC006 H5.
- FR-MCP-HVPIPE-021: AC001 to AC003 and AC005 to AC007 H7; AC004 H5.

## 20. Delivery

The plan `PLAN-HVPIPELINE-001` derives from this section and cites it; the architecture is the authority (foundation, introduction). The delivery follows the operator's structure for this architecture (operator, 2026-10-11, verbatim: "I want the implementation plan to be organized into requirements as a phase, interfaces and code stubs as a phase for the entire plan, test-driven development unit tests with mocks as a single phase, and then you can begin implementation coding in a domain-driven design breakdown so that agents can be run in parallel for the coding.") and the Byrd Development Process v4 (S-33), and keeps the dependency order of the handoff: H0, H1, H2, H3, H7, H4, then H6 and H5, then independent validation. H6 precedes H5 because the plugin watcher and the client streams of H5 consume the streams H6 delivers, so no part of H5 ships before its dependency and none needs an activation contract (section 19.4).

### 20.1 Phases

![Delivery order of the HV phases, part 1 of 4](c4/rendered/AM1-20-01.svg)

View: AM1-20-01

![Delivery order of the HV phases, part 2 of 4](c4/rendered/AM1-20-01-part2.svg)

View: AM1-20-01-part2

![Delivery order of the HV phases, part 3 of 4](c4/rendered/AM1-20-01-part3.svg)

View: AM1-20-01-part3

![Delivery order of the HV phases, part 4 of 4](c4/rendered/AM1-20-01-part4.svg)

View: AM1-20-01-part4

- H0 (Phases 1 to 3 for the whole HV catalog): the requirement records and mappings of section 19 (created 2026-10-16 as `pending`) updated to the text the hostile validation and the operator's approval settle, and the use cases of section 18 created (store ids recorded beside the labels), all through the plugin's workflows; the import of FR, TR, and TEST-MCP-PATHNORM-001 into this workspace's store and the strengthened acceptance criteria, each with a named test, of the existing requirements through which P10 to P15 are fixed (section 20.4), and the named tests of the HostileReview worker on TEST-MCP-HOSTILEREVIEW-002 and -003 (sections 16.7 and 19.3); the verification items of section 20.5; every interface, DTO, entity class, option, and stub of sections 10 to 16 compiling and unregistered, as the foundation's Phase 2 does (foundation section 8.2); every test of section 19 written from its acceptance criteria and proven against mocks with negative controls before any production code (the profile's BDPv4 procedure: tests and their mocks first, a green mock gate with every planned negative control rejected, then production code); the decision on the foundation amendments A-01 to A-12, which is a change to the approved PLAN-SESSIONTURNREVIEW-001 and needs its new hostile validation (section 1). The implementation worktree is cut from `develop` when Phase 2 starts (S-31), never from the foundation's worktree.
- HvStorageSchema: the entities of section 12, their configurations, migrations for SQLite, PostgreSQL, and SQL Server (Up, Down, Up with schema and data equal, as the foundation's StorageSchema), the two turn columns of A-11 with their database default and their migration rule for turns already reviewed (FR-MCP-HVPIPE-012-AC007), the `AgentResponseGrade` change of A-05, the dump policies, and the federation domain; after the foundation's StorageSchema. Before it creates any HV entity, it fixes the workspace-key problems P10, P12, and P13 (D-07, section 20.4), because every HV entity is keyed by `WorkspaceId` first (section 12).
- HvSharedComponents: sections 11.1 to 11.8, each with its one test class.
- H1 HvReviewerPool: options and seeding, AgentIdentity with its golden contract, the reviewer adapters with the shared launch builder (its one table of highest efforts, P9), the true live-line path (problem P8), the record extraction and the write barriers of section 16.3, the Copilot qualification (P6), the PowerShell plugin core fixes P11, P14, and P15, synced to every plugin that carries the core, before FR-MCP-HVPIPE-008-AC010 is bound to the real hosts (D-07, section 20.4), StartHvReviewerNonceCommand, the reviewer queries and update.
- H2 HvAdmission: SubmitHvRequestCommand with its policies, TurnSnapshotBuilder use, the freeze guard wired into TurnWritePipeline and every unit of A-07, the request queries, CancelHvRequestCommand.
- H3 HvExecution: HvWorkerHost, RunReviewerProcess with its worker-bound containment, the claim, start, heartbeat, append, close, fail, and expiry units, FileHvIncidentCommand, HvRetryDecision, the probe policies ReadinessProbeVerdict and HvProbeOutcome, and verification step V-1 (log integrity), which ReadinessProbeVerdict uses at close and HvFinalVerification reuses in H7; and the HostileReview worker of section 16.7 (P1 to P3, P5's part for its options, with the `IsDeleted` audit of P4 for its entities).
- H7 HvFinalization: HvFinalVerification (its V-2 to V-8, with H3's V-1), FinalizeHvRequestCommand with CommitFinalResult, the amended RecordSessionTurnReviewCommand and RecordModelGradeCommand callers (A-05, A-06), and the retirement of ConvertHvReceiptToReview. It precedes the public surfaces, because no review result may be published before finalization is atomic (handoff, H7).
- H4 HvRestSurfaces: HostileValidationController with the error mapping.
- H5 HvClientParity: the client, REPL verbs and policy classification, MCP tools, Director commands, and the plugin verbs and watcher in the PowerShell core, synced to every plugin that carries the core (foundation section 3.2), with their golden contracts and documentation.
- H6 HvStreamsAndOperations: the two streams, the notification poll, the incident query, evidence retrieval and removal, the read audit rows, and the operations documentation (recovery, NONCE, removal).

### 20.2 Gates

The foundation's gates apply unchanged (foundation section 8.4): G1 targeted tests, G2 the unit gate, G3 the integration suites with the three-provider cases, G4 the PowerShell plugin core, G5 configuration and traceability (strict), G6 the plugin suites, every one through Nuke, with zero failures and zero skips in the executed scope; the full unit suite of the current and earlier work passes before a context is left (AGENTS.md, "Byrd Test Gate"). The cases marked "three providers" in section 19 run in G3. The live check runs on a separate server instance started for it, never on a shared server (foundation section 8.6): it NONCEs every reviewer and records which pass and why (VI-01), submits a session-scope HV, watches the output stream receive lines while the reviewer runs (the end-to-end form of FR-MCP-HVPIPE-008-AC003), kills the worker mid-run and observes the lease recovery, cancels a second request, finalizes the first, reads its result and evidence, and confirms that writes to its turns are refused. Build, test, and deploy run through Nuke; releases through Octopus; GitHub Actions and Azure Pipelines are no part of any gate.

### 20.3 Implementation discipline

- Each implementation slice stops at `Validating`; the coordinator decides completion after the independent validation agrees (handoff, "Validation and completion gate").
- Every hostile validation of this plan's work is run by the reviewer the operator names, never of the requesting agent's family (P-01), passes only at accuracy and completeness of at least 98 with no FAIL or UNKNOWN (P-05), has unbounded review time (P-02), and keeps its request and response jsonl and its entire result in the session log (the operator's HV receipts rules).
- The new pipeline does not validate its own plan's work until its live check has passed and the operator approves the switch (handoff: "Do not prematurely use the new HV pipeline to validate itself").
- No code is written for this plan until the operator approves it; the standing code freeze applies (section 1).

### 20.4 Problems pulled into the plan

REMED-HVPIPELINE-001 (section 21) is pulled into the plan as follows: P5 in two parts, the foundation's grader pin in H0 (its text and plan change with A-01 to A-03; the foundation's `ModelGrader` options are architecture only, section 1) and the pin of `HostileReviewWorkerOptions.Validate` in H3 with the HostileReview worker, which replaces it with that worker's own rule, the configured or the pooled default agent of FR-MCP-HOSTILEREVIEW-003 criterion 4, never this pipeline's reviewer pool (section 16.7, named test `HostileReviewWorkerUsesConfiguredOrPooledDefaultAgent`); P7 in H0 (the requirement id cited by `EventStreamController` is corrected before H6 builds on that controller, named check `EventStreamControllerCitesItsChangeStreamRequirement`, a traceability scan that the cited id is the change stream's requirement and never FR-MCP-045); P6, P8, and P9 in H1; P4 in HvSharedComponents (AuditLedgerGuard) and HvStorageSchema (`IsDeleted` on every HV entity); P1, P2, and P3 in H3 (D-02 settled: the HostileReview subsystem stays separate), where the worker FR-MCP-HOSTILEREVIEW-003 already specifies is built to the contract of section 16.7, sharing only the reviewer adapters and FencedLease, and P2 and P3 are fixed in that subsystem. P10 to P15 (the findings of the Codex review of pull request 81) sit in code that other plans own or have redesigned, and this plan fixes all six (D-07, settled), each under the owning plan's approved design and through its existing requirement, whose acceptance criteria H0 strengthens with a named test for the case the problem shows:

- P10, P12, and P13 in HvStorageSchema, before any HV entity is created, because every HV entity is keyed by `WorkspaceId` first (section 12). P10 and P13 are fixed as the federation plan's P1.5 decided them: an explicit rekey maintenance command with its startup guard and the token-store key rewrite (its D5 and D7, `docs/plans/2026-10-07-federation-fixes.md:166-175`), never an automatic startup migration and never a dual-key lookup; P12 in the normalizer. Their requirements are FR, TR, and TEST-MCP-PATHNORM-001, which this workspace's store does not hold (requirement reads of 2026-10-17: `not_found`) and the federation handoff places in the `F:\GitHub\McpServer` store (`docs/handoffs/federation-fixes-handoff-20261016T121400.md:97`); H0 imports them into this workspace's store through the requirements workflow, with their stored text, before strengthening them.
- P11, P14, and P15 in H1, in the PowerShell plugin core and synced to every plugin that carries it, before FR-MCP-HVPIPE-008-AC010 (a reviewer closes its own turn and exits under the Stop gate) is bound to the real hosts: P11 and P15 break the Stop path that criterion runs through, and P14 is in the REPL wrapper that every hook call runs through (`plugins/core/lib-ps/plugin-hook.ps1:230`, `plugins/core/lib-ps/repl-invoke.ps1:771,959`). P11 and P15 follow the foundation's stop gate, which blocks while a delivered request is in progress and never closes a turn (its FR-MCP-SESSIONTURN-005, slice A7), never Codex's suggestion to complete the turn in the hook. Their requirement in the store is FR-MCP-SESSIONLIFE-005: P11 is its criterion 2 (a stale local turn is reconciled only by authoritative `Completed` proof for the exact workspace, agent, session, and request) and P15 its criterion 1 (failed-build and incomplete-audit enforcement before any allow), although the hook cites FR-MCP-SESSIONLIFE-004 for both (`plugins/core/lib-ps/plugin-hook.ps1:972,1044`), a citation H1 corrects with the fix, as P7 corrects `EventStreamController`'s (requirement reads of 2026-10-17). P14's is FR-MCP-SESSIONLIFE-004 criterion 1.
- Named tests, H0 against mocks and bound in the phase that fixes each problem, recorded on the TEST of the requirement named: `WorkspaceRekeyRewritesStoredBackslashKeysThreeProviders` and `StartupGuardRefusesUnconvertedWorkspaceKeys` (P10), `CombineResolvesRootRelativeWindowsSegmentAgainstDrive` (P12), and `RekeyRewritesGitHubTokenEntriesReadUpdateDelete` (P13), on TEST-MCP-PATHNORM-001; `StopHookStaleCacheReconciledByExactTurnCompletedProof` (P11), `StopOverrideMarkerNeverPassesInProgressTurn` (P15), and `ReplChildTreeKillsDescendantsAfterRootExit` (P14) on TEST-MCP-SESSIONLIFE-002, the TEST that both FR-MCP-SESSIONLIFE-004 and -005 map to and that holds their child-deadline, cleanup, Stop-ordering, and stop-hook pin tests (its criterion 2; store reads of 2026-10-17: TEST-MCP-SESSIONLIFE-005 belongs to the drain-timeout work of FR-MCP-REPL-011).
- Where an owning plan has already delivered the same fix under its approved design when the phase starts, the phase proves it with the named test and does not change that code a second time (the DRY ruling).

### 20.5 Verification items (H0 and H1)

Each item states what it asks and, where the drafting session settled it, the result of 2026-10-16 with its receipt. The probe runs ran each CLI headless in an empty scratch directory outside every workspace, each bounded by a timeout (probes, not hostile validations), and their complete outputs are kept under `/home/sharpninja/.claude/receipts/McpServer/20261016T153600-hv-amendment-vi-probes/`. What stays open is verified in H1 or H0 as stated.

- VI-01 For each of the five CLIs, the runtime receipt that names the model and effort of a run. Settled: Codex records both in the rollout (`model` `gpt-6-astra`, `effort` `max`); Claude in its session transcript (`model` `claude-opus-5-5`, `effort` `max`), only when session persistence is on; Grok in its session store (`summary.json` `reasoning_effort`; the model id differs between `chat_history.jsonl` and the events, which H1 pins); Copilot in its JSON stream (per call `assistant.message.model` and `user.message.responsesReasoning.effort`); Cline records the model (`run_start.modelId`, `run_result.model.id`) and only `thinking` on or off: no record a run writes (stream, session file, `sessions.db`, `cline.log`) holds its effort level, and the configured `providers.json` effort (`xhigh`) differed from the run's `--thinking high`, so Cline cannot pass a NONCE (L-04, D-06). Sources per CLI: section 16.3, `ReadProvenance`.
- VI-02 Copilot's non-interactive launch, its highest effort, and its live output. Settled for GitHub Copilot CLI 1.0.98: `copilot -p <prompt> --allow-all-tools --output-format json --no-ask-user` runs unattended (exit 0, one JSON object per line, `assistant.message_delta` events while the model writes); `--reasoning-effort` accepts `none` to `max` but only with an explicit `--model` (`auto` refuses it, and `auto` routed two prompts to two models), so a Copilot reviewer needs a configured model (section 10); `--model gpt-5.4` is not available on this account. Open for H1: the new branch of the shared launch builder (P6) and the configured model's acceptance of `max`, proven by its NONCE.
- VI-03 Each CLI's write barrier, and whether its plugin can still log the reviewer's turn behind it. Settled for the barrier (section 16.3): exercised for Codex `exec -s read-only` (pwsh `Set-Content` failed "Read-only file system", and Codex recorded a sandbox violation) and Grok `--permission-mode plan` (a `search_replace` call was refused, "a headless run cannot ask for approval", and the run ended `stopReason` `cancelled` with exit 0); structural for Copilot `--available-tools view grep glob` (its session lists `bash`, `create`, and `edit` as disabled, `tool_count` 3); not established for Claude `--permission-mode plan` (its model declined before any write call; a read-only Bash call was accepted) or Cline `--plan` (a `run_commands` shell call ran in plan mode); Copilot's `--deny-tool write --deny-tool shell` and `--mode plan` exited 0 with no output at all and are not used. Open for H1: whether each plugin logs its turn behind its barrier, verified in the live check on a separate server instance (section 20.2), never against the live server, because the operator's rule puts integration checks on a temporary instance and the probe writes reviewer turns.
- VI-04 That each reviewer host's plugin logs a `Chore` turn when the server launches it (R-24, V-6). Observed: a headless Grok run loads the MCP plugin and its hooks and wrote `.mcpServer/grok/session-state.yaml` into its working directory (FR-MCP-HVPIPE-008-AC008); `claude -p` loads plugins and hooks unless `--bare`, and its `--safe-mode` init still listed the mcpserver plugin; Codex runs hooks only with persisted trust or `--dangerously-bypass-hook-trust` (its help); Cline reads `~/.cline/hooks`; Copilot not observed. Open for H1: the logged turn itself.
- VI-05 The storage cost of appending with the default flush settings and `MaxConcurrentRuns` runs on SQLite, measured before the defaults are kept. The append count is not bounded by the interval alone: the flush timer adds at most one append per run per `OutputFlushInterval` (14400 per run hour at 250 ms), and the byte flush adds one more for each `OutputFlushBytes` written, so the count grows with the output rate. The measurement therefore states its workload (lines per second and bytes per line of a scripted reviewer, at a low, a typical, and a heavy rate) and reports, per run hour, the appends, the chunk rows, the manifest updates, the ledger rows each of those writes, and the bytes stored, the ledger rows included, each chunk's ledger row carrying its own compressed copy of the chunk (R-14). Open for H1: the measurement.
- VI-06 The column or ordinal that gives a session's turn order. Settled: develop has no ordinal column on `SessionLogTurnEntity` and reads a session's turns by ascending row `Id` (`src/QBrainAi.Services/Services/SessionLogService.cs:504,693,2002`; `src/QBrainAi.Support.Mcp/Services/TransactionGatedSessionLogService.cs:445,476,694`); one federation adapter orders by `Timestamp` instead (`src/QBrainAi.Services/Services/FederationAdapters/FederationStateAdapterImplementations.cs:1063`), which HvScopeResolution does not use.
- VI-07 That a multi-member response log reads back as one stream. Settled on .NET 10.0.13: one `GZipStream` over three concatenated members returned the exact concatenation (305136 bytes, equal SHA-256), and the first member alone did not (negative control); FR-MCP-HVPIPE-011-AC004 repeats the check on the server's runtime.
- VI-08 The highest effort each CLI accepts. Settled: Codex `max` (accepted and recorded in its rollout), Claude `max` (recorded in its transcript), Grok `xhigh` (Grok 1.0.60 refuses `max` and lists `xhigh`, `high`, `medium`, `low`), Cline `xhigh` (its `--thinking` levels end at `xhigh`), Copilot `max` (its `--reasoning-effort` values, subject to the configured model); section 10 holds the table, and P9 records where develop's constants differ.

## 21. Problem register (REMED-HVPIPELINE-001)

The single TODO of problems to fix found while writing this amendment (operator, 2026-10-16: "keep track of problems to fix as a single todo item that will be pulled into the plan."), created and read back through the plugin on 2026-10-16; P10 to P15, the six findings of the Codex review of pull request 81 (review 5554472359 on commit `6dbc384323`, posted 2026-10-16T17:48:12 local, after the merge), were added on 2026-10-17 at the operator's answer "1. Yes", each checked against the source of this branch. Each item's full text and receipts are in the TODO; summaries:

- P1 Requirement drift and a missing worker: the exported projection `docs/Project/Functional-Requirements.md:1389` shows FR-MCP-HOSTILEREVIEW-003 acceptance criterion 4 (a server-owned leased worker) checked with an older text, while the store holds it with `isSatisfied` false and a newer text (the configured or the pooled default agent, no named model; requirement read of 2026-10-17); either way no worker exists: none is registered and `AcceptReviewerOutputAsync` has no caller in `src` (`HostileReviewServiceCollectionExtensions.cs:11-42`; lease options unread, `HostileReviewWorkerOptions.cs:30-46`).
- P2 Defect: `AcceptReviewerOutputAsync` overwrites the verdict on every call with no lease, attempt, or fence (`HostileReviewService.cs:196-273`).
- P3 Requirement drift: FR-MCP-HOSTILEREVIEW-002 lists many artifact types, but only `todo` resolves, and the stored hash is of the TODO id, not its content (`HostileReviewService.cs:294-314`).
- P4 Audit gap: the ledger is skipped when the audit table is unavailable, and entities without `IsDeleted` are never audited (`McpDbContext.cs:1507-1538,1620-1627`).
- P5 Reviewer pin drift: the foundation pins one grader and model, contrary to P-01 and S-02 (A-01 to A-03), and `HostileReviewWorkerOptions.Validate` pins `Astra`, `gpt-6-astra`, and `xhigh` (`HostileReviewWorkerOptions.cs:77-82`), contrary to FR-MCP-HOSTILEREVIEW-003 criterion 4 (the configured or the pooled default agent, no named model); the two parts are fixed in H0 and H3 (section 20.4).
- P6 Capability gap: the one-shot strategy has no Copilot branch; Copilot runs only through the interactive `copilot-cli` strategy, unqualified for HV (`OneShotCliAgentExecutionStrategy.cs:238-262`, `CopilotCliAgentExecutionStrategy.cs:5-45`).
- P7 Traceability drift: `EventStreamController` cites FR-MCP-045, which is "Cross-Workspace TODO Move" in the store (`EventStreamController.cs:11`).
- P8 No true live streaming: the one-shot strategy yields output only after the process ends (`OneShotCliAgentExecutionStrategy.cs:57-75`); operator: "Explains a lot.  Gotta be fixed in the plan." Proven fixed only by FR-MCP-HVPIPE-008-AC003.
- P9 Defect and duplicated constants: develop's highest-effort constants disagree, and one is refused. The one-shot strategy passes Grok `--effort max` and `--reasoning-effort max` (`src/QBrainAi.Services/Services/OneShotCliAgentExecutionStrategy.cs:26,333,335`), which Grok 1.0.60 refuses with exit 1 ("unknown effort level 'max'; use one of: xhigh, high, medium, low", probe of 2026-10-16), while `src/QBrainAi.Services/Services/GrokCliAgentExecutionStrategy.cs:25` uses `high`; Codex is pinned to `xhigh` twice (`OneShotCliAgentExecutionStrategy.cs:23`, `src/QBrainAi.Services/Services/CodexCliAgentExecutionStrategy.cs:21`) although Codex accepts and records `max`; Cline's `xhigh` is defined twice (`src/QBrainAi.Common.AgentCli/AgentCliClient.cs:17`, `OneShotCliAgentExecutionStrategy.cs:25`).
- P10 Unconverted legacy workspace keys (Codex priority 1): `WorkspaceKeyConversion` canonicalizes workspace keys on write and in query parameters but reads stored values unchanged (`src/QBrainAi.Storage/WorkspaceKeyConversion.cs:24-26`), and nothing rewrites stored backslash keys: no migration since 2026-07-01 on any of the three providers, and no other code in `src`, rewrites them (content searches of 2026-10-17). This is the federation plan's undelivered P1.5, the `--workspace-path-rekey` command with its startup guard and token-store key rewrite, by its decisions D5 (a maintenance command, no automatic startup migration) and D7 (no dual-key lookup; the startup guard refuses an unconverted store) (`docs/plans/2026-10-07-federation-fixes.md:166-175`); its handoff says P1.5 "has not started" and not to deploy `develop` to a server whose database still holds backslash workspace ids (`docs/handoffs/federation-fixes-handoff-20261016T121400.md:14-16`). Codex's suggested provider migration contradicts D5.
- P11 Stop-hook proof unusable (Codex priority 2): the stale-cache proof calls `client.SessionLog.GetAsync` (`plugins/core/lib-ps/plugin-hook.ps1:1065`), whose `Task<UnifiedSessionLogDto>` is not among the result types the REPL passthrough awaits, so the call ends in "Unsupported task result type" (`src/QBrainAi.Repl.Core/GenericClientPassthrough.cs:243-640`) and the hook reads no proof and answers `Active` (`plugin-hook.ps1:1063-1087`); a supported call would still not carry the `workspacePath`, `requestId`, `outcome`, and `serverStatus` the hook reads (`plugin-hook.ps1:1089-1110`; `src/QBrainAi.Client/Models/SessionLogModels.cs:8-85`). Inference (Codex's, not reproduced): a completed turn behind a stale cache keeps the Stop gate blocked. The behavior is FR-MCP-SESSIONLIFE-005 criterion 2 in the store, although the hook cites FR-MCP-SESSIONLIFE-004 (`plugin-hook.ps1:1044`), in the stop path the foundation's slice A7 rewrites.
- P12 Root-relative Windows segment not rooted (Codex priority 2): `IsWindowsRooted` accepts a drive-qualified path and the two UNC spellings only (`src/QBrainAi.Client/WorkspacePathNormalizer.cs:161-164`), so `Combine` (`:120-144`) appends a segment `\repo` to the root `C:/content` as `C:/content/repo` instead of resolving it to `C:/repo` (inferred from the code, not run). The federation plan delivered `Combine` and `ResolveRepoRootWorkspacePath` (P1.3b, P1.4d, `docs/plans/2026-10-07-federation-fixes.md:109,157`).
- P13 Legacy GitHub token entries unreadable (Codex priority 2): the token store compares stored entry paths with the normalized key without normalizing the stored side (`src/QBrainAi.Services/Services/FileGitHubWorkspaceTokenStore.cs:53,89,125`; the document read at `:142` normalizes nothing), so an entry stored as `C:\repo` no longer matches `C:/repo`. The federation plan's P1.5 includes this rewrite (`docs/plans/2026-10-07-federation-fixes.md:172`).
- P14 Descendants survive the root (Codex priority 2): `Stop-ReplChildTree` calls `Kill(true)` only while the root process is still running (`plugins/core/lib-ps/repl-invoke.ps1:736-738`), so a descendant that outlives the root is not killed by it, contrary to FR-MCP-SESSIONLIFE-004 acceptance criterion 1 ("process-tree cleanup without a stranded child"). No open TODO in this workspace names FR-MCP-SESSIONLIFE-004 (keyword queries of 2026-10-17); the foundation plan lists it as related and unchanged.
- P15 Stop override returns before the turn is handled (Codex priority 2): when the stop hook consumes an acceptance marker for a failed build or an incomplete audit (`plugins/core/lib-ps/plugin-hook.ps1:991-994,1009-1012`), it returns `allow` at `:1172-1174`, before the completion branch at `:1253`, so the stop passes while the turn is still in progress. Codex suggests continuing to the completion; the approved foundation plan instead makes the stop gate block while a delivered request is in progress and never close a turn (PLAN-SESSIONTURNREVIEW-001, slice A7: its FR-MCP-SESSIONTURN-005 acceptance criteria 1 and 4, which that plan creates and the store does not hold yet (requirement read of 2026-10-17), and its change to TR-MCP-SESSIONLIFE-005, "`Close-PluginTurnIfNeeded` blocks instead of closing"). The behavior is FR-MCP-SESSIONLIFE-005 criterion 1 in the store, although the hook cites FR-MCP-SESSIONLIFE-004 (`plugin-hook.ps1:972`).

## 22. Decisions

On 2026-10-16 the operator asked the drafting agent to settle every decision it could from the operator's own instructions ("look at the totality of the project, of all of my instructions, and see if you can solve them yourself"). Four are settled from those instructions, each with its source; the operator settled D-04, D-07, D-08, and D-09 on 2026-10-17; D-03 stays with the operator, because the operator's rules place it there. A settled decision is part of this proposed amendment and is approved with it (D-03).

- D-01 (settled) Reviewer exclusion by model vendor: exclusion is by agent family and its aliases, not by model vendor (R-08). Sources: S-17 ("excluding the agent submitting the review"); P-01, which constrains the agent family and asks separately for the agent and the model; the operator's own configuration, which runs Cline on `gpt-6-astra` (provider `openai-codex`, VI-01 receipt) while keeping Cline and Codex as two reviewers (S-02). Inference, labeled: the operator treats the agent, not the model vendor, as the unit of independence.
- D-02 (settled) The HostileReview subsystem stays separate (R-25), and P1 to P3 are fixed in it (section 20.4). Sources: FR-MCP-HOSTILEREVIEW-002 (it reviews artifacts) against S-03 (this pipeline reviews session turns); the approved foundation's position (foundation section 5.4); the operator rule that new scope the operator has not named needs approval, which consolidation would be. Its missing worker has its own contract (section 16.7) and shares only the compatible execution primitives (the DRY ruling).
- D-03 (open, the operator's) The foundation amendments A-01 to A-12, with this amendment and its plan: approve them as a change to PLAN-SESSIONTURNREVIEW-001, which then needs its new hostile validation (section 1). The operator's rules make plan approval the operator's act: a requirement change is planned, presented, and implemented only after explicit approval.
- D-04 (settled by the operator, 2026-10-17) The reviewer for the hostile validation of this amendment and its plan is Codex `gpt-6-astra` at effort `max`. Source: the operator's answer "2. A" to the question asked under P-01 ("If not specified by operator, ask what agent and model to use for HV in a session."). Codex is neither the requesting agent's family (ClaudeCode) nor the agent that drafted the first version (Cline); its model and effort are verified from its rollout (VI-01).
- D-05 (settled) Exclusion covers the author of the reviewed turns as well as the submitter (R-08, FR-MCP-HVPIPE-002-AC008). Sources: S-28 (a self-review "is not relevant to the HV"), P-07 ("Self-check alone is not validation."), and the handoff's settled requirement that self-review is independent of HV; a review by the agent that wrote the turns is a self-review. Because one request names turns of one MCP session (R-26), the author is that session's agent.
- D-06 (settled) Cline stays unqualified until it records the effort of a run: its NONCE fails `runtime_identity_mismatch` (L-04). Sources: P-04 ("Verify the reviewer's model and effort from its own runtime receipts"), which leaves no accepted remedy for a declared, unobserved effort; the operator rule to fail closed under an existing rule when the inputs cannot prove a fact; the handoff's settled requirement that not every request needs all five reviewers. The same holds for the HostileReview worker of section 16.7, whose execution row records the model and effort actually used (FR-MCP-HOSTILEREVIEW-003 criteria 1 and 2): an agent without a provenance source fails its attempt with `runtime_identity_unavailable` before launch, and a run whose provenance shows no model or effort fails the same way with no execution row.
- D-07 (settled by the operator, 2026-10-17) This plan fixes P10 to P15 itself, in HvStorageSchema and H1, each under the owning plan's approved design (the federation plan's D5 and D7; the foundation's FR-MCP-SESSIONTURN-005), as section 20.4 assigns them (operator, verbatim: "D-07 B: this plan fixes all six (P10 to P15) itself."). The question arose because each sits in code another plan owns or has redesigned: P10 and P13 are the federation plan's undelivered P1.5, P12 is in that plan's normalizer code, P11 and P15 are in the stop path the foundation's slice A7 rewrites, and P14 is under FR-MCP-SESSIONLIFE-004, which the foundation lists as related and unchanged. Rejected: option A, which left each fix to its owning plan with this plan holding only entry conditions.
- D-08 (settled by the operator, 2026-10-17) Timestamps in HV data: the identifiers `hvr-`, `hvprobe-`, and `hvi-` keep the UTC `yyyyMMddTHHmmssZ` stamp, as session and request ids do, and every stored timestamp is UTC with offset (section 12); local time is for display only (operator, verbatim: "HV ids keep the UTC yyyyMMddTHHmmssZ stamp: timestamps are UTC in the data and local only for display."). The question arose because the operator profile's rule of 2026-10-13 ("Local everywhere") asks that a UTC identifier be raised to the operator.
- D-09 (settled by the operator, 2026-10-17) The existing requirement records through which the problems of section 21 are fixed (TEST-MCP-HOSTILEREVIEW-002 and -003, FR-MCP-SESSIONLIFE-004 and -005, TEST-MCP-SESSIONLIFE-002, and the import of FR, TR, and TEST-MCP-PATHNORM-001; section 19.3) change in H0, after the operator approves the plan, and not before (operator, verbatim: "B", choosing option B of the question asked that day). The question arose because the workspace rule to capture requirements as they emerge argued for changing them at once, while FR-MCP-HOSTILEREVIEW-003 criterion 4 cites PLAN-SESSIONTRIAGEFIX-001, which the operator paused on 2026-10-16, so a change now could resume that plan. Until H0, the strengthened criteria and their named tests are stated in sections 16.7 and 20.4 and in REMED-HVPIPELINE-001. Rejected: option A, which updated those records in the store before hostile validation round 2.

## 23. Diagram index

Views (PLAN-C4MODEL-001; operator, 2026-10-19: "Every diagram in every doc should come from the same C4 definition."): every diagram is a view of `docs/architecture/c4/workspace.dsl` (this amendment's elements in `model/am1.dsl`, its views in `views/am1.dsl`, reusing the foundation's elements of `model/str.dsl`), shown as the image `c4/rendered/<view key>.svg` followed by the line `View: <view key>`, and rendered by `./build.ps1 ArchitectureDiagrams` on one US letter page at 8 to 16 point text (TR-MCP-NUKE-017 AC-5). A diagram named by its heading (a unit of work, policy, process, shared component, nested activity, or nested sequence) has its name as view key, except the amended RecordModelGradeCommand of section 14.5, whose key is `AM1-RecordModelGradeCommand` because the foundation's diagram keeps `RecordModelGradeCommand`; a use case keeps its label (`UC-HV-01` to `UC-HV-12`); every other diagram has the key `AM1-<section>-<NN>` named below. A diagram larger than one page continues in the views `<key>-part2`, `<key>-part3`, and so on; each branch of an `alt` block of a sequence is its own view `<key>-alt<k>-<i>`, an `opt` block is the view `<key>-opt<k>`, and a loop body is the view `<key>-loop<k>`, each placed after the view that holds the block.

- Section 4: the system context (view AM1-04-01).
- Section 7: the state diagrams of the HV request (7.1, view AM1-07-01), the run attempt (7.2, view AM1-07-02), reviewer readiness (7.3, view AM1-07-03), and a turn's HV state (7.4, view AM1-07-04).
- Section 11, the shared components, each drawn once: FencedLease, GzipCodec, AuditLedgerGuard, AgentIdentity, TurnSnapshotBuilder, RequirementReferenceExtractor, NotificationOutbox, OrderedEventReplay.
- Section 14, units of work: ListHvReviewersQuery, GetHvReviewerQuery, UpdateHvReviewerCommand, StartHvReviewerNonceCommand; SubmitHvRequestCommand (nested activities AdmitRequestChecks and InsertAdmittedRequest), GetHvRequestQuery, QueryHvRequestsQuery, CancelHvRequestCommand; GetHvResultQuery, GetHvEvidenceQuery, RemoveHvEvidenceCommand, StreamHvRunOutputQuery, StreamHvNotificationsQuery, QueryHvNotificationsQuery, QueryHvIncidentsQuery; ClaimHvRunCommand, RecordHvRunStartCommand, RecordHvRunHeartbeatCommand, AppendHvRunOutputCommand, CloseHvRunOutputCommand, FailHvRunCommand, ExpireHvRunLeasesCommand, FinalizeHvRequestCommand (nested activities ReadFinalizationInputs and CommitFinalResult), FileHvIncidentCommand; section 14.5, the amended internal RecordModelGradeCommand (view AM1-RecordModelGradeCommand).
- Section 15, policies: HvScopeResolution, HvAdmissionEligibility, HvReviewerEligibility, RoundRobinAssignment, HvTurnFreeze, HvFinalVerification (nested activities VerifyLogAndBinding and VerifyVerdictAndReviewer), ReadinessProbeVerdict, HvProbeOutcome, HvRetryDecision.
- Section 16, processes: HvWorkerHost, RunReviewerProcess (nested activities FailAttempt, AppendBatch, and StreamReviewerOutput), SubmitHvFromPlugin, WatchHvNotifications, and HostileReviewWorkerHost (nested activities RunHostileReviewAttempt and CompleteAttempt) with its internal units ClaimHostileReviewAttemptCommand, RenewHostileReviewAttemptCommand, CompleteHostileReviewAttemptCommand, and ExpireHostileReviewLeasesCommand (section 16.7).
- Section 17, sequences: 17.1 to 17.15 (views AM1-17-01 to AM1-17-15), with the nested sequences StreamOneAttempt and FinalizeOneRequest after 17.7.
- Section 18, use cases: the overview (view AM1-18-01) and UC-HV-01 to UC-HV-12 (views UC-HV-01 to UC-HV-12).
- Section 20: the delivery order (view AM1-20-01).
- Addendum B, entity diagrams by functionality: B.1 references between the groups (view AM1-B-01), B.2 reviewer pool (AM1-B-02), B.3 requests and membership (AM1-B-03), B.4 runs and output (AM1-B-04), B.5 results and findings (AM1-B-05), B.6 evidence (AM1-B-06), B.7 notifications and incidents (AM1-B-07), B.8 changed foundation entities (AM1-B-08), B.9 HostileReview worker storage (AM1-B-09).

## Addendum B. Entity diagrams with relationships

Grouped by functionality, the foundation's rule (Addendum A, "Grouping"): each entity's columns appear in exactly one group; an entity of another group appears without columns only as a relationship target. Solid lines are database foreign keys (each the composite (`WorkspaceId`, parent key)); dotted lines are logical references by root ids, enforced by the units. Diagrams draw key columns and the columns this amendment adds; section 12 lists every column of the HV entities, and section 16.7 those of the HostileReview worker (B.9, whose single-column foreign keys are that subsystem's own).

### B.1 References between the groups

- An HV request names its reviewed session by `Agent` and `SessionId`, its submitting turn by the submitter turn key, and each member turn by its turn key; a run names the reviewer's own turn by `ReviewerAgent`, `ReviewerSessionId`, and `ReviewerRequestId`; a requirement reference names a requirement or use case by `RefId`; an incident names its triage report by `TriageReportId`.

![References between the groups (entities), part 1 of 5](c4/rendered/AM1-B-01.svg)

View: AM1-B-01

![References between the groups (entities), part 2 of 5](c4/rendered/AM1-B-01-part2.svg)

View: AM1-B-01-part2

![References between the groups (entities), part 3 of 5](c4/rendered/AM1-B-01-part3.svg)

View: AM1-B-01-part3

![References between the groups (entities), part 4 of 5](c4/rendered/AM1-B-01-part4.svg)

View: AM1-B-01-part4

![References between the groups (entities), part 5 of 5](c4/rendered/AM1-B-01-part5.svg)

View: AM1-B-01-part5

### B.2 Reviewer pool

Not drawn: `HvReviewer` `ConfigurationVersion`, `ExecutionStrategy`, `AgentPath`, `ConfiguredModel`, `ConfiguredEffort`, `ResolvedModel`, `ResolvedEffort`, `ReadinessReason`, `ReadinessChangedUtc`, `LastProbeRunId`, `OpenIncidentId`, `ProbeRequestedUtc`, `MaxConcurrentRuns`, `CreatedUtc`, `UpdatedUtc`.

![Reviewer pool (entities)](c4/rendered/AM1-B-02.svg)

View: AM1-B-02

### B.3 Requests and membership

Not drawn: `HvRequest` scope, submitter, context, state, and result columns of section 12; unique (`WorkspaceId`, `IdempotencyKey`); indexes (`WorkspaceId`, `Status`, `CreatedUtc`) and (`WorkspaceId`, `Agent`, `SessionId`). `HvRequestTurn`: unique (`WorkspaceId`, `HvRequestId`, `Ordinal`) and the unique filtered turn-key index where `Disposition` is `Pending`. `HvRequestResponse`: unique (`WorkspaceId`, `HvRequestId`, `ResponseId`) and unique filtered (`WorkspaceId`, `ResponseId`) where `Disposition` is `Pending`.

![Requests and membership (entities), part 1 of 2](c4/rendered/AM1-B-03.svg)

View: AM1-B-03

![Requests and membership (entities), part 2 of 2](c4/rendered/AM1-B-03-part2.svg)

View: AM1-B-03-part2

### B.4 Runs and output

Not drawn: `HvRun` timestamps, `ReviewerConfigurationVersion`, `ProcessId`, `ExitCode`, `FailureDetail`, `ExpectedModel`, `ExpectedEffort`, `ProbeNonce`, the output manifest, progress, and provenance columns of section 12; the unique filtered (`WorkspaceId`, `HvRequestId`) where `Status` is live. `HvRunOutputChunk`: unique (`WorkspaceId`, `RunId`, `Sequence`); `LineCount`, `ByteCount`, `FenceToken`, `CreatedUtc`, `RemovedUtc`.

![Runs and output (entities)](c4/rendered/AM1-B-04.svg)

View: AM1-B-04

### B.5 Results and findings

Not drawn: `HvTurnAssessment` `Summary`, `RunId`; `HvFinding` `ResponseId`, `Check`, `Location`, `RequirementRef`, `ClaimId`, `Evidence`, `Remediation`, `RunId`; each unique with (`WorkspaceId`, `HvRequestId`, `Ordinal`).

![Results and findings (entities)](c4/rendered/AM1-B-05.svg)

View: AM1-B-05

### B.6 Evidence

Not drawn: `ByteCount`, `CompressedBytes`, `ContentType`, `CreatedUtc`, `RemovedBy`, `RemovalReason`; unique (`WorkspaceId`, `HvRequestId`, `Kind`).

![Evidence (entities)](c4/rendered/AM1-B-06.svg)

View: AM1-B-06

### B.7 Notifications and incidents

Not drawn: `HvNotification` `SubmitterAgent`, `SubmitterSessionId`, `SubmitterRequestId`, `PayloadJson`, `CreatedUtc`; `HvIncident` `Summary`, `Evidence`, `FilingAttempts`, `LastFilingAttemptUtc`, `LastFilingError`, `TriageGroupId`, `CreatedUtc`, `FiledUtc`.

![Notifications and incidents (entities)](c4/rendered/AM1-B-07.svg)

View: AM1-B-07

### B.8 Changed foundation entities

`SessionLogTurn` gains `HvState` and `HvRequestId` (A-11); `AgentResponseGrade` gains `RunId` and its `HvRequestId` becomes a foreign key to `HvRequest` (A-05); its removed receipt-path columns are listed in section 12.

![Changed foundation entities (entities)](c4/rendered/AM1-B-08.svg)

View: AM1-B-08

### B.9 HostileReview worker storage (section 16.7)

The HostileReview subsystem keeps its single-column keys (R-25), so its solid lines are foreign keys on `RequestId` alone, as its existing execution and diagnostic rows have (`src/QBrainAi.Storage/Entities/HostileReviewExecutionEntity.cs:63-65`, `HostileReviewDiagnosticEntity.cs:33-35`). Not drawn: `HostileReviewAttempt` `LeaseOwner`, `LeaseExpiresUtc`, `ClaimedUtc`, `EndedUtc`, `FailureCode`, `IsDeleted`, and its unique filtered (`WorkspaceId`, `RequestId`) where `Status` is `Claimed` or `Running`; the other columns of the request, execution, and diagnostic rows, which exist today (`src/QBrainAi.Storage/Entities/HostileReviewRequestEntity.cs:8-85`).

![HostileReview worker storage (section 16.7) (entities), part 1 of 2](c4/rendered/AM1-B-09.svg)

View: AM1-B-09

![HostileReview worker storage (section 16.7) (entities), part 2 of 2](c4/rendered/AM1-B-09-part2.svg)

View: AM1-B-09-part2
