# Independent hostile plan-readiness review, merged round 4

DISAGREE. Accuracy 92, completeness 90, confidence 98. Three claims PASS, nine FAIL, zero UNKNOWN. Fourteen implementer edits; no operator decision is required to correct the findings.

Head: 845a87dfeb05ac2f1acfdf0596a3a5e0dc32326b. Reviewer: Codex CLI / gpt-6-astra / max. No review time bound. Review only; no product or plan changes.
TimestampUtc: 2026-10-12T04:35:40.8681087Z
Reviewer sessionId: Codex-20261012T034800Z-hv-plan-readiness-r4; requestId: req-20261012T034439Z-hv-merged-r4.

## Mandatory startup and live identity proof

Read the requested Claude add-profile SKILL.md and all 20 non-skill profile Markdown files in full, including operator-rulings-2026-10-11.md; read the main-workspace marker in full, verified its signature, and read all 15 MCP memories through the installed Codex wrapper with -ParamsObject. Complete file hashes and memory versions/hashes are in the JSON receipt. The plugin cache and failsafe are isolated under /tmp, outside the worktree.
Live Codex rollout: /home/sharpninja/.codex/sessions/2026/10/11/rollout-2026-10-11T22-44-39-01a12e36-65d8-7b01-b9fc-f99a9ce2ee7e.jsonl; session_meta.id 01a12e36-65d8-7b01-b9fc-f99a9ce2ee7e; CLI 0.160.1.
- turn_context line 8, 10/12/2026 03:44:41: model=gpt-6-astra, effort=max, cwd=/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009.
- turn_context line 366, 10/12/2026 03:59:05: model=gpt-6-astra, effort=max, cwd=/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009.
- turn_context line 658, 10/12/2026 04:13:06: model=gpt-6-astra, effort=max, cwd=/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009.

MEMORY-PROCESS-007 v67 contains the actual 2026-10-12 PowerShell CONTRACT. This review does not invent an ACK or modify the memory. Its protections/alias obligations are retained in TR-MCP-SESSIONTURN-008 AC010..012 and AC007.

## Claim results

R3: FAIL. Eleven of fifteen round-3 findings resolved on their original scope; R3-F5/F6/F7/F10 not resolved. Four earlier residuals are resolved as left by round 3. See every individual re-attack below.
Findings: R4-F4, R4-F5, R4-F7, R4-F8.

M1: FAIL. The attached plan elements are represented: authoritative relational records, GraphRAG fallback, provenance, budgets, grading, trust floor, rejection/staleness audit, all nine relationship types, opt-in pilot, eight delivery topics and publication. Mode/source/token choices are identified. Executable failure/trust/HV behavior still contradicts the intended guarantees.
Findings: R4-F1, R4-F2, R4-F3, R4-F4, R4-F5, R4-F6.

M2: FAIL. Section 6 of the two documents is byte-identical and contains options/entities/DTOs/enums, 23 CQRS handlers, REST alias, MCP, client, REPL, builders and descriptors. The evidence model cannot retain a required verification input, accepted evidence kinds lack check contracts, and A/B MCP surface descriptions are not accurate.
Findings: R4-F2, R4-F3, R4-F9.

M3: FAIL. All 126 architecture Mermaid blocks independently rendered with Mermaid 11.17.2, exit 0. Thirty-eight handlers (11 turn, 4 review/read, 23 context) have activity diagrams; processes/policies are labelled; sequences, 15 use cases, and the entity addendum exist. Semantic branch/response/order completeness fails despite valid syntax.
Findings: R4-F1, R4-F4, R4-F5, R4-F6, R4-F8, R4-F9.

M4: FAIL. Reviewed the approved TODO text and all removed/renamed host allocations against the shared-PowerShell design. Most obligations and tests survive. Live REST upsert/PATCH/PUT/complete/fail semantics and bulk-only terminal guard placement are corrected; OpenCode routing and several A/B diagrams/MCP response paths still diverge.
Findings: R4-F7, R4-F8, R4-F9.

M5: FAIL. Verified cited controllers, service, MCP tools, client binding, context storage, GraphRAG ingest and core entry points. The new serialized response protocol and whole-submission idempotency are specified; the listed contract contradictions prevent acceptance.
Findings: R4-F1, R4-F2, R4-F3, R4-F4, R4-F5, R4-F6, R4-F9.

M6: FAIL. NOT APPROVED/code freeze is explicit; four phases cover the plan; P0 may fill the Phase C catalog; named TODO graph is acyclic and contains no IMPL-PLUGINCORENODE-001. However the neutral-stub/live-surface gate, early adapter adoption, missing ownership and post-completion grade submission prevent executing it as written.
Findings: R4-F6, R4-F10, R4-F11, R4-F14.

M7: PASS. docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:74-78,214-231,241,781 specifies no GitHub Actions/Azure gate/evidence; all non-Build.Tests builds/tests through native Nuke; direct full Build.Tests only, removed from MigrationIntegrationTests in prerequisite P0.1c; Octopus releases only as LAB-OMARCHY container; Windows gates through WSMan in F:/GitHub/McpServer/.worktrees. Opus=Octopus and lab-omarkey=LAB-OMARCHY are reasonable and explicitly stated in context.

M8: PASS. Independently inspected all five commit parents and file lists, remote ls-remote, local reflog, blob identity, and retained worktree/branch. Every publication is one file and consecutive fast-forward ancestry; current develop and HEAD document blobs equal 5f3ed62630d4c8a6594fd34a151cfb72c8efe51d. Reflog says update by push; no force update is evidenced.

M9: FAIL. All 100 proposed new IDs are absent from the complete main-workspace FR/TR/TEST lists (353/487/512 records). Rewritten PS TR/TEST and Nuke ACs have allocations/cases; exactly-one ownership now has a rule, but changed original ACs remain missing from TEST statements.
Findings: R4-F12.

M10: PASS. Both new documents have Apache-2.0 SPDX headers; all three documents contain zero en/em dashes. git diff c681112d..HEAD --check passed. No current marker key or generic long API-key value was found in the reviewed documents or commit diff. Reviewer artifacts are independently redacted before finalization.

M11: FAIL. One lib-ps core for nine hosts, Pester-only gates, explicit argv/stdin/error contract, planned actual entry-point extensions, existing ValidatePluginPowerShellOnly extension, bats/lib-sh/lib-node retirements, governed-host AC010..012 and tests, and Astra/max unbounded review rules are present. MEMORY-PROCESS-007 v67 contains the cited 2026-10-12 CONTRACT; ACK/operator resolution is explicitly required before Node retirement. Actual OpenCode event mapping, Phase 2 suite sequencing and shell preflight still fail.
Findings: R4-F7, R4-F11, R4-F13.

## Prior findings re-attacked

R3-F1: RESOLVED (PASS). docs/Stateless-Agent-Context-Management.md:137,163-174 now bind the completed rollout session id, response thread id, verdict thread id, and every turn_context model/effort. Named wrong-runtime and wrong-thread controls at docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:766.
Regression check: The runtime-check defect is resolved. R4-F6 separately identifies the resulting completion/submission ordering gap.

R3-F2: RESOLVED (PASS). docs/Stateless-Agent-Context-Management.md:171 permits independent Codex reviewer and producer sessions, refuses the reviewed/producing session, and names an acceptance case.
Regression check: No regression of agent/session independence found.

R3-F3: RESOLVED (PASS). docs/Stateless-Agent-Context-Management.md:294 defines request/query nesting, route/scalar parameters, builder types/defaults, descriptor envelopes, and real-binder tests; agrees with GenericClientPassthrough.cs:649-695.
Regression check: No flat-payload binding regression found.

R3-F4: RESOLVED (PASS). docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:120 uses canonical user-prompt-submit, accepted by plugin-hook.ps1:5; planned HostEvent and InvocationFromStdin extensions are explicit at :423.
Regression check: The classic Cline binder error is fixed; R4-F7 concerns the separate OpenCode host event API.

R3-F5: NOT RESOLVED (FAIL). docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:128 fixes separate hostContext, input/output, shell.env and mechanical output mutation, but the real OpenCode event callback still cannot reach the specified session.idle mapping. R4-F7.
Regression check: Remaining exact host-boundary incompatibility.

R3-F6: NOT RESOLVED (FAIL). docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:162-190,460-478,559-599 correct bulk gating, the terminal guard and explicit terminal targets. :513 includes system queue entries and :688 permits withdrawn captured-close warning. R4-F8 identifies remaining counterexamples.
Regression check: Per-content popAll, system remove, final mint retention and unsupported-session refusal precedence still disagree with approved prose.

R3-F7: NOT RESOLVED (FAIL). docs/Stateless-Agent-Context-Management.md:161,876 filters terminal responses and introduces Closed items, but the creation race at R4-F4 still yields an ungradable permanent Pending item.
Regression check: Sequential lifecycle repaired; concurrent lifecycle not closed.

R3-F8: RESOLVED (PASS). docs/Stateless-Agent-Context-Management.md:51 specifies first-write StatusVersion locking, Serializable transactions, whole-operation retries and three-provider forced interleavings for grade/status changes.
Regression check: The original trust race is addressed. Creation of HV items is outside this protocol and is R4-F4.

R3-F9: RESOLVED (PASS). docs/Stateless-Agent-Context-Management.md:110-118,436,451-459 uses the whole-submission SubmissionSha256 on both ordinary replay and unique-index collision, with differing package/claims/evidence cases.
Regression check: No content-only response-idempotency regression found. Package overflow idempotency is separately R4-F1.

R3-F10: NOT RESOLVED (FAIL). docs/Stateless-Agent-Context-Management.md:90,665 adds response-only evidence and supports dependencies; :664,760-794 checks RetrieveCandidates freshness. Get/Query trusted surfaces still bypass it (R4-F5), and claimed-hash information is lost (R4-F2).
Regression check: R4-F2 is introduced by the new evidence-hash handling.

R3-F11: RESOLVED (PASS). docs/Stateless-Agent-Context-Management.md:257-261,760-794 now carries sessionId/requestId/focusRefs, validates the turn, and ranks that explicit neighborhood; named same-text/different-neighborhood and unknown-turn cases.
Regression check: No request-neighborhood regression found.

R3-F12: RESOLVED (PASS). docs/Stateless-Agent-Context-Management.md:921-962; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:128,774 specify authoritative message-list replacement/fresh invocation, P0.2 capability evidence, fallback to Pilot and an actual-model-request sentinel test.
Regression check: No absent-history-operation regression found. Host routing defects are separately R4-F7.

R3-F13: RESOLVED (PASS). docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:438,730 permits exact src/index.ts, tsconfig/package/lock, ignored compiler dist outputs and installed dependencies; names positive compiled-output and negative helper/dist cases against the existing validator.
Regression check: No compiled-adapter allowlist contradiction remains.

R3-F14: RESOLVED (PASS). docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:4185-4205,4253-4264 now links projection to workspace ContextDocument and its chunks. GraphRagService.cs:348-385 creates exactly those rows.
Regression check: Knowledge-graph entities remain separate.

R3-F15: RESOLVED (PASS). docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:445-480 defines one owner, contributor-only references and merged named cases, repairing the specific duplicate-ownership examples.
Regression check: Different missing changed-AC allocations remain under R4-F12; the duplicate-ownership repair itself is accepted.

R2-F2: RESOLVED (PASS). Remaining runtime-verification part is repaired by docs/Stateless-Agent-Context-Management.md:163-174; parsed verdict, content binding, complete findings and reviewer result remain checked. See R3-F1.
Regression check: Execution ordering is R4-F6, not a loss of the new verification checks.

R2-F6: RESOLVED (PASS). As left by round 3, canonical classic Cline HookName is fixed; the plan explicitly adds HostEvent/InvocationFromStdin, UTF-8 opaque invocation transport, exact argv and real-binder tests. docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:120,128,423.
Regression check: R4-F7 is a host callback mismatch beyond the original PowerShell parameter-binding issue.

R2-F11: RESOLVED (PASS). AgentResponseTrustDecision is defined, and docs/Stateless-Agent-Context-Management.md:294 defines the complete builder/descriptor/typed-method envelope. Both section-6 copies are identical.
Regression check: No regression of this definition/binding fix found.

F8: RESOLVED (PASS). Round-1 F8 as left by rounds 2/3: nonempty claims/evidence, zero evaluated denominator yields zero, zero UNKNOWN/FAIL trust rule, and now actual reviewer-runtime verification. docs/Stateless-Agent-Context-Management.md:163-174,436-450,508-535.
Regression check: Other deterministic-evidence holes are reported as R4-F2/R4-F3 rather than claiming the original arithmetic remains broken.

## Material findings and exact corrections

### R4-F1: An idempotent overflow retry becomes a successful invocation

FAIL; Implementer edit; claims M1, M3, M5.
Locations: docs/Stateless-Agent-Context-Management.md:318; docs/Stateless-Agent-Context-Management.md:340; docs/Stateless-Agent-Context-Management.md:346; docs/Stateless-Agent-Context-Management.md:359; docs/Stateless-Agent-Context-Management.md:1577; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:2103; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:86.

Defect: BuildPackage returns 200 for any identical saved package, including Overflow, while the invocation process treats every 200/201 as a usable package and invokes the model. The mandatory fail-the-invocation decision is therefore not invariant under retry.

- Evidence: First build exceeds the mandatory budget, stores status Overflow with no items/content, and returns 422. An identical retry finds the same InputsSha256 and takes the unconditional 200 existing-package branch. The concurrent-winner branch has the same problem.
- Evidence: RunStatelessInvocation at stateless:1577-1596 sends 200/201 through model execution and only errors through prompt refusal/cancellation. SubmitResponse later refusing an Overflow package does not undo the answer already produced.

Specific change to PASS: Return the same 422 agent_context_budget_exceeded, including stored diagnostics, whenever the matching existing or winning package is Overflow; make the host success path require Built defensively. Update both contracts and diagrams and name retry-after-lost-overflow-response and concurrent-overflow cases.
Regression: Newly identified interaction between existing overflow and idempotency contracts.
Operator decision: none required.

### R4-F2: Submission discards the claimed evidence hash required by grading

FAIL; Implementer edit; claims M1, M2, M5.
Locations: docs/Stateless-Agent-Context-Management.md:90; docs/Stateless-Agent-Context-Management.md:123; docs/Stateless-Agent-Context-Management.md:455; docs/Stateless-Agent-Context-Management.md:508; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1853; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1886.

Defect: The server-computed hash and the submitted hash have different required meanings, but only one Sha256 is stored. Submission overwrites it with the server hash, so the later deterministic grade cannot identify a submitted mismatch.

- Evidence: Section 6.3 explicitly requires a submitted hash that differs from the source to make the deterministic evidence check FAIL.
- Evidence: AgentResponseEvidenceEntity has only Sha256 and CommitSha. Submit computes source hashes and saves evidence with those hashes. There is no submitted-hash field, immutable submission snapshot, or persisted mismatch finding. SubmissionSha256 is an irreversible fingerprint, not the missing input.
- Evidence: Counterexample: cite an existing file with deliberately wrong hash Hbad. Submission stores Hactual. After restart the grade compares Hactual with the file and cannot distinguish this submission from one that cited Hactual.

Specific change to PASS: Retain the submitted hash separately from the authoritative source hash, or retain an immutable submission snapshot/mismatch result sufficient for the later grade. Specify null and unavailable-source behavior and use the authoritative hash for staleness. Add a wrong-submitted-hash case that remains FAIL after restart; mirror entity, DTO, grade, and diagram changes.
Regression: Introduced by the R3-F10 server-computed evidence-hash fix.
Operator decision: none required.

### R4-F3: Two accepted evidence kinds have no deterministic verification contract

FAIL; Implementer edit; claims M1, M2, M5.
Locations: docs/Stateless-Agent-Context-Management.md:90; docs/Stateless-Agent-Context-Management.md:123; docs/Stateless-Agent-Context-Management.md:508; docs/Stateless-Agent-Context-Management.md:509.

Defect: Command and Commit are accepted evidence kinds, but the complete deterministic-check list defines neither verification algorithm nor expected result for them. The text says command results are checked only by the deterministic grade, whose contract omits that check.

- Evidence: The enum admits File, Requirement, Receipt, Commit, Command, SessionLogTurn. RunDeterministicGrade names per-kind checks only for File, Requirement, Receipt and SessionLogTurn.
- Evidence: The evidence model supplies Ref, Sha256 and CommitSha but no specified command-result representation or success/claim-validation rule. A claim whose sole evidence is Command or Commit therefore has an unspecified evidence-check set and score outcome.

Specific change to PASS: Define reproducible deterministic checks and required evidence fields for Command and Commit, including PASS/FAIL/UNKNOWN, missing/mismatched receipt or commit, workspace scope, and score contribution. Alternatively explicitly remove unsupported kinds from this effort and update all schemas. Add discriminating cases for every retained kind.
Regression: Holistic completeness finding; not a request for a new grading pipeline.
Operator decision: none required.

### R4-F4: HV-request creation can race terminal response changes and strand a Pending item

FAIL; Implementer edit; claims R3, M1, M3, M5.
Locations: docs/Stateless-Agent-Context-Management.md:51; docs/Stateless-Agent-Context-Management.md:161; docs/Stateless-Agent-Context-Management.md:876; docs/Stateless-Agent-Context-Management.md:879; docs/Stateless-Agent-Context-Management.md:614.

Defect: The R3-F7 fix filters terminal responses and closes existing items, but creation participates in no response serialization protocol. A response can become Rejected/Superseded between selection and item insertion, leaving an ungradable Pending item forever.

- Evidence: Allowed interleaving: CreateHvRequest reads response R as Trusted; Reject locks R, sets Rejected, sees no open HV item, commits; CreateHvRequest saves Open request plus Pending R.
- Evidence: All grading then refuses R with agent_context_response_not_gradable. Repeat rejection returns unchanged. The enumerated StatusVersion/Serializable operations exclude CreateAgentContextHvRequestCommand, which only writes the HV request/items.

Specific change to PASS: Make HV creation and terminal transitions share an atomic eligibility/item-creation protocol. Define ordered response locking or serializable validation/retry across the three providers, and completion behavior if every item becomes terminal. Add forced create-versus-reject and create-versus-supersede tests; update errors and diagrams.
Regression: Residual R3-F7 under concurrency; sequential cases are repaired.
Operator decision: none required.

### R4-F5: Trusted response reads still bypass freshness verification

FAIL; Implementer edit; claims R3, M1, M3, M5.
Locations: docs/Stateless-Agent-Context-Management.md:464; docs/Stateless-Agent-Context-Management.md:497; docs/Stateless-Agent-Context-Management.md:664; docs/Stateless-Agent-Context-Management.md:1197; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:616.

Defect: GetResponse and QueryResponses(trustedOnly=true) return stored Trusted status without evaluating freshness. The GetResponse sequence explicitly authorizes use as context solely from that status.

- Evidence: After a file changes, no requirement-update trigger runs. QueryResponses restricts to status Trusted and returns it; GetResponse loads fields and returns them. Neither calls EvaluateStaleness.
- Evidence: R3-F10 added freshness to BuildPackage and RetrieveCandidates only. The merged FR-MCP-AGENTCONTEXT-005 says every trusted retrieval checks freshness before returning a response, and the attached plan excludes stale responses from trusted retrieval.

Specific change to PASS: Apply the same freshness boundary to every surface returning content as trusted, including GetResponse and trusted/status-filtered QueryResponses, before filtering/counting/paging. Or explicitly make audit reads incapable of authorizing trusted use and provide an authoritative freshness-checked path. Update sequences and add direct get/query-after-file-change tests.
Regression: Residual R3-F10; response-only dependencies and RetrieveCandidates are addressed, but public trusted reads remain a bypass.
Operator decision: none required.

### R4-F6: The HV agent must submit grades after the execution that could submit them has ended

FAIL; Implementer edit; claims M1, M3, M5, M6.
Locations: docs/Stateless-Agent-Context-Management.md:137; docs/Stateless-Agent-Context-Management.md:167; docs/Stateless-Agent-Context-Management.md:542; docs/Stateless-Agent-Context-Management.md:1275; docs/Stateless-Agent-Context-Management.md:1277; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:228; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:229.

Defect: The model-grade sequence requires a completed CLI turn and the runner-copied completed rollout before the same HV execution posts its grades. No continuation or second grading-submission step is specified.

- Evidence: The runner waits for turn.completed and only after reviewer completion copies the rollout. RecordModelGrade requires both completed-turn evidence and that rollout.
- Evidence: The sequence assigns review, final verdict, completed-runner copy, then POST grades/model to the same HV lifeline. An active single codex exec cannot have its own turn.completed receipt before returning; after it returns it cannot make another tool call.
- Evidence: The text explicitly makes the HV agent the caller and excludes a server grading worker/pipeline, so an unstated coordinator submission cannot silently fill this gap.

Specific change to PASS: Specify an executable two-step manual reviewer protocol, for example a grade-only continuation by the configured HV agent after the runner freezes the completed review receipts. Define which execution/turn supplies the immutable verdict and proof, how the continuation receives them, and retry/idempotency behavior. Keep the no-HV-pipeline scope and update the sequence/live check.
Regression: The completed-rollout requirement added for R3-F1 makes this pre-existing ordering ambiguity explicit.
Operator decision: none required.

### R4-F7: OpenCode session.idle is an event payload, not the specified host hook

FAIL; Implementer edit; claims R3, M4, M11.
Locations: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:128; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:423; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:728; /home/sharpninja/github/mcpserver-opencode-plugin/src/plugin.ts:215; /home/sharpninja/github/mcpserver-opencode-plugin/node_modules/@opencode-ai/plugin/dist/index.d.ts:173; https://opencode.ai/docs/plugins/.

Defect: The exact adapter contract registers/forwards a session.idle hook, but OpenCode calls event({event}) for that event. Forwarding the actual hook name event is rejected by the core mapping; registering session.idle is never invoked.

- Evidence: Live Hooks declares event(input: { event: Event }): Promise<void>; the plugin implements event at plugin.ts:215 and extracts the event type from its payload. The official OpenCode plugin example likewise handles session.idle inside event.
- Evidence: The plan allows only chat.message/session.idle/shell.env/messages.transform for this host and says any other hook exits host_event_unknown. Its logic-free adapter cannot secretly rename/filter event.type and still satisfy the exact opaque forwarding rule.
- Evidence: The R3-F5 fixes for separate ToolContext, input/output transport, shell.env and output mutation are present; this remaining host-shape mismatch prevents the approved stop-equivalent from reaching the PowerShell gate.

Specific change to PASS: Register the real event callback and forward its opaque input as HostEvent event; put event.type/session identity routing and irrelevant-event handling in the PowerShell core. Update the binding table, ACs, descriptors if needed, and Pester fixtures to exercise the real Hooks shape and session.idle delivery.
Regression: R3-F5 is only partially resolved at the actual host boundary.
Operator decision: none required.

### R4-F8: Phase A process diagrams still contradict queue, failsafe, and Stop contracts

FAIL; Implementer edit; claims R3, M3, M4.
Locations: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:507; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:513; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:655; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:685; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1321; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:273; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:275; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:298.

Defect: The corrected activities still accept ambiguous queue removal, deliver system entries, delete final mint evidence, and block a session whose unsupported-session exception requires warning after close capture.

- Evidence: popAll compares only total line count with total queued count. Queue [A,B] and popped lines [A,A] pass that test while failing the required per-content matching-count rule. remove(absorbed_mid_turn) sends any unique matched entry straight to delivered/write-target, without the system-kind branch used by dequeue.
- Evidence: DrainFailsafe activity and sequence remove a mint record on session_not_open after confirmed open. FR-MCP-SESSIONTURN-001-AC008 requires retaining it marked final and not retried, with a warning naming the prompt.
- Evidence: EvaluateStopGate tests remembered server gate refusal first and blocks immediately. FR-MCP-SESSIONTURN-005-AC003 explicitly excepts an already-open turn after unsupportedSince; after its close is captured, the current diagram can still block on the old refusal.

Specific change to PASS: Match popAll by attributable content multiplicities, branch on kind for remove, retain final mint failures with their diagnostic state, and evaluate the unsupported-session/captured-close exception before a remembered completion refusal can block. Correct response sequences and trace the existing named bridge/final-mint/unsupported-session cases through every branch.
Regression: R3-F6 is not fully resolved. The bulk-submit gate, terminal enrichment, explicit terminal target, total system-entry count, and simple withdrawn-close warning paths were corrected.
Operator decision: none required.

### R4-F9: The architecture still conflates REST and MCP session-log contracts

FAIL; Implementer edit; claims M2, M3, M4, M5.
Locations: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:315; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:353; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:408; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1209; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1477; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1777; src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.SessionLog.cs:47; src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.SessionLog.cs:103; src/QBrainAi.Support.Mcp/McpStdio/FwhMcpTools.SessionLog.cs:349.

Defect: The updated units and response sequences continue to describe REST bodies/paging as if MCP returned them, and lifecycle payload descriptions omit the live MCP field-selection boundary.

- Evidence: sessionlog_dialog returns {success:true,totalDialogItems:count}; architecture:408 and the shared controller/tool sequence use {agent,sessionId,requestId,totalDialogCount}. These are different field names and envelopes.
- Evidence: sessionlog_query returns only {totalCount,items} and has no offset parameter. Although architecture:1475 notes the missing offset, :1777-1778 still tells an MCP caller to consume limit/offset and page until complete. It cannot retrieve a later page that way.
- Evidence: MCP complete/fail parses turnJson then copies an explicit subset in FinalizeLifecycleTurnToolAsync:349-369. Fields such as planFile and todoId are not copied. REST forwards the body and validates those fields; the shared full-body merge/error description does not disclose this live difference. S5 adds Phase B fields, not an approved unrelated widening of every carried field.

Specific change to PASS: Define each existing REST and MCP request/response/error shape separately and show surface-specific response processing. Keep MCP query limitations explicit and route paging through the typed client/REST when needed. Document the current lifecycle field whitelist plus only the Phase B additions; do not silently redesign existing routes.
Regression: Found while verifying the second holistic read corrections. The REST single-turn bodies and the bulk-only TerminalTurnOutcomeGuard placement themselves are substantially corrected.
Operator decision: none required.

### R4-F10: Neutral registered handlers replace live behavior before the no-regression gate

FAIL; Implementer edit; claims M6.
Locations: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:172; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:173; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:175; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:176; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3732; tests/QBrainAi.Support.Mcp.Tests/Controllers/SessionLogControllerReceiptTests.cs:26; tests/QBrainAi.Support.Mcp.Tests/Controllers/SessionLogControllerReceiptTests.cs:85.

Defect: Phase 2 completes dispatch-only versions of existing session-log surfaces, registers every handler as a neutral stub, and requires the existing tests to remain green. Existing routes now dispatch to no-op handlers instead of performing their current behavior.

- Evidence: Submit/Open/Upsert/Get/Query and the other existing routes are now units of this plan, so every-handler/every-surface includes them.
- Evidence: Existing unit evidence is discriminating: SubmitAsync_AcknowledgesOnlyAfterPersistence awaits real/substituted persistence and asserts id 42; SubmitAsync_RealService_AcknowledgesReadableSession reads the stored session. A neutral 0/null handler cannot satisfy those unchanged assertions.
- Evidence: The unregistered-entity correction prevents schema regressions but does not preserve the existing execution path. No temporary delegation/activation seam is specified.

Specific change to PASS: Explicitly keep current routes and plugin behavior on working implementations through Phases 2 and 3. Define compatibility/delegating handlers or defer activation while exposing new seams/stubs to the RED tests. State the Phase 4 activation point and retain the existing regression suite; do not solve this by weakening tests or implementing new behavior before its tests.
Regression: Whole-plan phase reconstruction remains non-executable under its own gate.
Operator decision: none required.

### R4-F11: Phase 2 adopts TypeScript adapters before their permitted test runner and core exist

FAIL; Implementer edit; claims M6, M11.
Locations: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:175; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:176; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:231; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:241; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:248; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:730.

Defect: Phase 2 writes each TypeScript adapter in final forwarding form and must gate/merge every changed plugin repo, but those repos retain Jest and have no Pester suite until Phase 4 A9. The prerequisite TestAgentPlugins deliberately fails that state.

- Evidence: P0.1b ports bats only. P0.7 explicitly excludes the three TypeScript hosts from the Pester baseline because their suites arrive in A9. TestAgentPlugins refuses npm/Jest test commands in any selected repo.
- Evidence: The live manifests for cline, cline-v2, and opencode still declare Jest test commands. Phase 2 nevertheless changes their adapters and requires G6; each changed plugin PR requires G6 before merging and the next phase.
- Evidence: The actual lib-ps host-event/stdin behavior is implemented in Phase 4 and delivered by its one sync. A final Phase 2 adapter cannot preserve the existing host behavior by forwarding to that absent implementation.

Specific change to PASS: Sequence interface declarations, adapter activation, and Jest-to-Pester adoption so every phase can satisfy its stated gates. Keep live adapters until Phase 4 and specify non-activated contract seams/fixtures in Phases 2-3, or move a complete behavior-preserving Pester migration into an approved prerequisite. State exactly which repos/suites run at each phase and preserve zero skips.
Regression: Bats retirement is correctly sequenced, but it does not solve the separate TypeScript-host Phase 2 gate.
Operator decision: none required.

### R4-F12: Changed existing acceptance criteria still have no owning TEST statement

FAIL; Implementer edit; claims M9.
Locations: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:443; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:445; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:482; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:484; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:534; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:556; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:558; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:671; TEST-MCP-SESSIONLIFE-001; TEST-MCP-SESSIONLIFE-002.

Defect: The new owner/contributor rule resolves duplicate allocations but cannot assign changed ACs that no TEST row names. Several changed original ACs are only assigned to a slice, contrary to the exactly-one TEST-statement requirement.

- Evidence: FR-MCP-SESSIONLIFE-001-AC002 is explicitly changed at :556 and allocated to A3 at :671. TEST-SESSIONTURN-001 and the mapping name the new AC007, not the changed AC002.
- Evidence: TR-MCP-SESSIONLIFE-002-AC002 is explicitly changed at :558 and allocated to A4 at :671. TEST-SESSIONTURN-003 names new AC004, not original AC002. Similar omissions exist for the listed changed original outcome ACs.
- Evidence: Read-only live TEST-MCP-SESSIONLIFE-001/-002 records contain broad old test descriptions, not these changed AC ids plus a named case. The instruction that P0 must verify coverage is not a concrete ownership allocation for the missing entries.

Specific change to PASS: Add every changed original AC in the allocation list to exactly one TEST statement with its named case, reusing the appropriate existing/new test without duplicating ownership. Update mapping brackets and the P0 uniqueness/coverage input list. Preserve the new contributor rule.
Regression: R3-F15 duplicate ownership is repaired; holistic M9 still fails on missing changed-AC coverage.
Operator decision: none required.

### R4-F13: The rollout still specifies a shell script outside PowerShell

FAIL; Implementer edit; claims M11.
Locations: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:88; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:781; CLAUDE.md:235.

Defect: The prescribed deploy preflight invokes sh -c with a two-command script, despite the all-scripts-in-PowerShell ruling and the plan assertion that it designs no shell script/tool.

- Evidence: The forward-looking deploy procedure runs env with PATH followed by sh -c and the script command -v id dotnet; dotnet --list-sdks. This is shell script execution, not merely launching PowerShell or quoting historical code.
- Evidence: The same read-only lookup can run in a PowerShell process under the proposed environment; no host API requires a shell adapter here.

Specific change to PASS: Replace the sh -c preflight with pwsh -NoProfile -NonInteractive using Get-Command for executable resolution and the native dotnet --list-sdks command; keep the environment/SDK evidence and analyzer requirement.
Regression: Residual PowerShell-only nonconformance in the rollout procedure.
Operator decision: none required.

### R4-F14: Required A9 changes fall outside every Phase 4 file owner

FAIL; Implementer edit; claims M6.
Locations: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:180; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:182; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:191; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:192; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:730; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3782; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3784.

Defect: The exclusive bounded-context file rules do not assign A9 server marker changes, shared plugin hook-template changes, or root integration-fixture changes to a context, even though A9 requires them before the gates.

- Evidence: A9 changes src/QBrainAi.Services/Services/MarkerFileService.cs, plugins/core/hooks-templates/generate-wrappers.ps1, and tests/QBrainAi.PluginIntegration.Tests PluginHostKind/PluginHostProcessAdapter.
- Evidence: SessionTurnLogging lists session-log handlers/services; PluginCorePowerShell owns plugins/core/lib-ps; BuildTooling owns build; HostPluginAdoption owns the nine sibling repos. None owns these known paths. Phase 2 shared-file list does not include their new behavior, and Phase 4 says an agent must stop and seek a Phase 2 amendment for changes outside its files.

Specific change to PASS: Assign these already-required changes and their tests to explicit Phase 4 contexts/TODOs, state their dependencies before sync/integration gates, and update the ownership diagrams/list. Audit the remaining scope entries for the same omission without expanding scope.
Regression: Whole-plan allocation gap, not a request to alter approved A/B behavior.
Operator decision: none required.

## Verification and limits

All three review documents were read end to end. Section 6 copies are exactly identical (127884 characters). All 126 architecture Mermaid blocks rendered independently with Mermaid 11.17.2, exit 0; this checks syntax, not branch correctness. The named CQRS/process/use-case/entity diagrams exist. git diff --check passed; both new documents carry Apache-2.0 SPDX; no en/em dashes or detected key values appear in the reviewed documents/commit diff.

All 100 proposed new requirement IDs were checked against the complete main-workspace lists (353 FR, 487 TR, 512 TEST), with zero collisions. The approved A/B TODO was read from the main workspace. Removed host allocations/renamed cases were checked for retained obligations in the shared PowerShell core: aliases and crash replay in A3/A5, binding/isolation in A4, model evidence and decisions in A6, Stop in A7, host fixtures/governance in A8, and review/clear-session behavior in S8/S9/S10. The host transport/diagram defects are reported above; no duplicate Node implementation is demanded.

Publication passes: 99da6af8 -> 51d2482e -> 63d4743d -> 33a5a954 -> a8d3934b are consecutive commits, each changing only the stateless document. Remote develop is a8d3934b; both document blobs are 5f3ed62630d4c8a6594fd34a151cfb72c8efe51d. The retained agentcontext-doc-20261011 worktree and docs/agentcontext-plan-20261011 branch exist. Reflog records update by push; the ancestry contains no force update. The primary checkout and unrelated dirty paths were not changed.

The Opus/Octopus reading and one activity diagram per unit reading are reasonable and stated. No operator decision is needed for these findings. The scope excludes P0.1 baseline-fix merits, plugin-repo commit merits and earlier conduct. No product build/test was executed for this plan-only review. The score is a review judgment, not a statistical measurement; 98/98 remains the required functional bar.

Official host-contract corroboration: [OpenCode plugin documentation](https://opencode.ai/docs/plugins/) shows session.idle delivered to the event callback. The installed SDK and live plugin source are the primary local evidence in R4-F7.

## Commands and execution evidence

Exact PowerShell commands, timestamps, parser/analyzer results and exit codes are in the JSON receipt Commands array and docs/receipts/hv/20261012T034439Z-review-commands.jsonl, whose Output fields retain the observed results. PSScriptAnalyzer 1.25.0 and the PowerShell parser checked authored review scripts before execution. Finalization commands continue that ledger. Startup calls and rejected exploratory syntax are also preserved in the runner response JSONL.

- The command ledger records exact checked PowerShell script text and outer exit codes; embedded command failures are retained in its output, and outer success is not represented as proof that every embedded command succeeded.
- Exploratory corrections: initial wrapper bootstrap/first turn attempt failed before isolated-cache/failsafe setup; corrected bootstrap and turn persisted. One appendDialog used the wrong parameter shape and was corrected. A PowerShell parser error and a PSScriptAnalyzer automatic-variable warning stopped exploratory scripts before execution; corrected versions passed. Some exploratory rg paths did not exist (rg error 2), followed by verified file discovery. A broad requirement-output inspection was accidentally verbose; subsequent query was filtered.
- The independent Mermaid CLI was invoked from PowerShell under the explicit user exception. No authored JavaScript, Bash or Python script was run.
- Final receipt creation, persistence and redaction commands append to the same ledger after this snapshot; see finalization proof sidecar and runner stream for their outputs.

## Reviewer turn and persistence proof

The entire result is stored on reviewer session Codex-20261012T034800Z-hv-plan-readiness-r4, request req-20261012T034439Z-hv-merged-r4, through the installed Codex plugin. The readback is compared byte for byte with this markdown receipt before the turn is completed; docs/receipts/hv/20261012T034439Z-persistence-proof.json records the receipt hash, durable acknowledgements, readback equality and final completed status. No other session-log turn is changed.

Every current marker key and long apiKey/api_key/X-Api-Key value in reviewer receipts is redacted. Six occurrences from the early mandatory full marker reads were removed from the active runner stream. That open file uses space padding only within each replaced secret span, preserving every runner file offset and every non-secret byte; completed JSONL lines remain valid with no NUL bytes. No key value is repeated here. The runner must retain redaction when it freezes the final response and rollout files.

=== VERDICT JSON ===
{"overallVerdict":"DISAGREE","accuracy":92,"completeness":90,"confidence":98,"passCount":3,"failCount":9,"unknownCount":0,"failList":["R3","M1","M2","M3","M4","M5","M6","M9","M11"],"unknownList":[],"implementerFixes":["R4-F1","R4-F2","R4-F3","R4-F4","R4-F5","R4-F6","R4-F7","R4-F8","R4-F9","R4-F10","R4-F11","R4-F12","R4-F13","R4-F14"],"operatorDecisionFails":[],"headSha":"845a87dfeb05ac2f1acfdf0596a3a5e0dc32326b","receiptPaths":["docs/receipts/hv/hostile-validator-plan-readiness-merged-r4-20261012T034439Z.md","docs/receipts/hv/hostile-validator-plan-readiness-merged-r4-20261012T034439Z.json"],"reviewerSessionId":"Codex-20261012T034800Z-hv-plan-readiness-r4","reviewerRequestId":"req-20261012T034439Z-hv-merged-r4"}
