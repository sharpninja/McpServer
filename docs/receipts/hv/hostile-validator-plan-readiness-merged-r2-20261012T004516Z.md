<!-- SPDX-License-Identifier: Apache-2.0 -->
# Independent hostile validator: merged plan readiness, round 2

**DISAGREE. Accuracy 90, completeness 87, confidence 97.** Top-level claims: 2 PASS, 10 FAIL, 0 UNKNOWN. The unchanged gate is at least 98/98 and every applicable claim PASS.

TimestampUtc: 2026-10-12T01:25:08.1679514Z
HEAD: 31a6564ec0f07f2581804d093959ea53ea6ccb3c
Reviewer: Codex, gpt-6-astra, effort max. Live proof: /home/sharpninja/.codex/sessions/2026/10/11/rollout-2026-10-11T19-45-17-01a12d92-2c6d-7f02-a1af-48fda2f0819c.jsonl; turn_context 10/12/2026 00:57:11; payload.model=gpt-6-astra, payload.effort=max.
Own MCP session/request: Codex-20261012T004725Z-plugin-session / req-20261012T004516Z-plan-readiness-merged-r2

Startup: read the requested add-profile skill and all 20 non-skill profile Markdown files in full, including operator-rulings-2026-10-11.md; read the main-workspace marker in full; trusted the marker before invoking the installed Codex wrapper; workflow.memory.list returned 15 effective memories, all read in full. Profile hashes and memory IDs are in the JSON receipt. Cache/failsafe files are outside the worktree in /tmp.

The interpretations of Opus as Octopus and one complete activity diagram per unit are reasonable and expressly stated. This review does not score wording polish, the substance of baseline fixes, plugin-repo commits, or earlier implementer conduct.

## Claims

- **R1 FAIL**: F1 and F8 are not resolved. Ten specific original defects are repaired; regressions and new gaps are recorded individually.
  Findings: R2-F1, R2-F2.

- **M1 FAIL**: Relational authority, GraphRAG fallback, provenance, relationships, opt-in pilot, deterministic/model grading, and the eight original implementation activities are retained. Off/Pilot/Stateless, fail-the-invocation, external turn-range HV requests, source defaults and deterministic token estimate are stated choices. Publication rejection handling, mandatory 98 floor, independent evidence and reproducible replay are not preserved completely.
  Findings: R2-F1, R2-F2, R2-F3, R2-F4.

- **M2 FAIL**: Section 6 is byte-identical in the two documents (SHA-256 A009C679FA5330831741F687F8DF964CB95E6859E05D5B1CB22CBD976CB5A763). Equality does not cure the undefined result, missing complete builder/descriptor signatures, invalid adapter dispatch, unsupported ID lengths or missing replay inputs.
  Findings: R2-F4, R2-F5, R2-F6, R2-F11.

- **M3 FAIL**: 34 handler units have exactly one activity diagram each; all 33 externally callable units have submission sequences. Policies/processes are separated, 15 use-case diagrams cover the plan use cases, and Addendum A depicts relationships. All 116 architecture Mermaid blocks rendered successfully using Mermaid 11.17.2; all 59 stateless-document blocks occur verbatim in architecture. Phase A/B branch semantics nevertheless contradict the catalog.
  Findings: R2-F7.

- **M4 FAIL**: Removed +A8/+S9 allocations and renamed tests were re-attacked against 5fa1d984. Most move to the single A3-A7/S8 PowerShell implementation plus host-binding/adapter cases. The governed bridge/outbox protections are dropped or undecided, real adapter argv is invalid, and the two architecture paths in R2-F7 misstate approved Phase A/B behavior.
  Findings: R2-F6, R2-F7, R2-F10.

- **M5 FAIL**: Verified live conventions include Result<T>.Failure(Exception), AddCqrsHandlers, CanonicalSectionProjectionProvider, controller ClassifiedError and workspace/root-id entity fields. Trust verification and thresholds, replay persistence, identifier sizes and the internal trust-result contract are materially inconsistent.
  Findings: R2-F2, R2-F3, R2-F4, R2-F5, R2-F11.

- **M6 FAIL**: The four phases cover A/B/C, every listed bounded context has ownership/dependencies/TODO, and the stated TODO chain is acyclic with no IMPL-PLUGINCORENODE-001. The document is NOT APPROVED and has an explicit code freeze. P0 catalog creation no longer depends on its own later HV. Real backfill is nevertheless scheduled before RED/mocks, and the explicit retirement-only disposition dependency is not reconciled with retained governed obligations and the general contract-ACK rule.
  Findings: R2-F9, R2-F10.

- **M7 FAIL**: GitHub Actions and Azure Pipelines are excluded as requirements/process gates. Native Nuke for application/plugin builds/tests, independent direct Build.Tests, release-only Octopus, and the Windows WSMan worktree route are explicitly stated. The retained Nuke execution of Build.Tests contradicts that exception.
  Findings: R2-F8.

- **M8 PASS**: Live git ls-remote origin refs/heads/develop is 63d4743df28da7bbf277aa06bd8d8c3003570516. 99da6af8 -> 51d2482e -> 63d4743d is a direct single-file parent chain after 7c27457d, each touching only docs/Stateless-Agent-Context-Management.md. The remote-tracking reflog records update by push, with no ancestry rewrite. The develop and HEAD document blobs both equal 622bc676d1f19ed339e7311c63c463aaf88a32a0. Retained worktree agentcontext-doc-20261011 and branch docs/agentcontext-plan-20261011 exist. These prove fast-forward published results; Git objects/reflog alone do not prove which optional push flags were typed. No evidence of a forced rewrite was found.

- **M9 FAIL**: Live main-workspace lists: 353 FR, 487 TR, 512 TEST; all 96 proposed IDs checked are free. New TODO names are absent, as expected before P0. Rewritten host-binding/review AC allocation and Nuke-003 AC-005 evidence are present. The omitted HV-list catalog entry and unnamed prerequisite Nuke TR cases prevent a complete G5 traceability claim.
  Findings: R2-F12.

- **M10 PASS**: Both new documents and the merged plan start with Apache-2.0 SPDX comments. All three have zero en/em dashes. No current main/worktree marker key or long apiKey/api_key/X-Api-Key assignment is present in the three documents or the 5fa1d984..HEAD patch; common credential patterns and the reviewed document changes yielded no secret finding. Review receipts are separately sanitized.

- **M11 FAIL**: The intended core is lib-ps for all nine hosts, Pester is the only future plugin test runner, bats conversion precedes its gates, and lib-sh/lib-node retirement TODOs exist. TR-MCP-NUKE-003 AC-005 explicitly extends the existing Build.SyncAgentPlugins.cs validator, whose live tests-path exclusion was verified. Forward-looking reviewer rules use gpt-6-astra/max; gpt-6-sol occurs only as historical text in the reviewed documents. The concrete adapter command contract is not executable against the real core, and governed-work disposition, retained contracts and adoption dependencies are not reconciled.
  Findings: R2-F6, R2-F10.

## Round-one re-attack

- **F1 NOT RESOLVED**: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:70 restores most steps but still omits rejection/stop/no-force/no-PR behavior; R2-F1. Regression check: None needed to establish the remaining defect.

- **F2 RESOLVED**: docs/Stateless-Agent-Context-Management.md:894-925: any build error/outage refuses the prompt, produces no answer and cancels the turn; submit and deterministic-grade failures have explicit paths. Regression check: No fallback-answer/package mismatch remains.

- **F3 RESOLVED**: docs/Stateless-Agent-Context-Management.md:514,821-884,896: explicit turn-range HV requests and per-response grades, no worker or future pipeline. Regression check: Receipt authenticity remains F8, not a reason to undo the operator-directed HV-request design.

- **F4 RESOLVED**: docs/Stateless-Agent-Context-Management.md:457,711: both former ellipses replaced with explicit workspace-first typed query arguments. Regression check: Other definition gaps are R2-F11.

- **F5 RESOLVED**: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:580 limits surfaces to external units, excludes the internal trust handler, and labels stateless invocation a process. Regression check: The later HV-list query was omitted from the enumerated catalog (R2-F12).

- **F6 RESOLVED**: docs/Stateless-Agent-Context-Management.md:646-647,776-777 contain list validation and 400 exits; 919-925 contain submit/grade outcomes. Regression check: Newly found Phase A/B semantic contradictions are R2-F7; parse and inventory checks alone do not clear them.

- **F7 RESOLVED**: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:77 matches src/QBrainAi.Support.Mcp/Controllers/SessionLogController.cs:714-731 problem+json fields. Regression check: None found in this repaired envelope.

- **F8 NOT RESOLVED**: docs/Stateless-Agent-Context-Management.md:410,481 fix nonempty claims/evidence and zero denominator; 153,531-549 still do not verify actual reviewer verdict/execution; R2-F2. Regression check: New HV-request membership checks do not bind scores/findings to the review.

- **F9 RESOLVED**: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:243-249,571 complete catalog text, create records/use cases, then run P0 HV; Phase C use cases included. Regression check: Separate phase-organization and prerequisite traceability issues remain R2-F9/R2-F12.

- **F10 RESOLVED**: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:6 explicitly marks the old unchanged-baseline assertion false; P0.1 re-verifies affected anchors on approval HEAD. Regression check: No current unchanged-baseline claim is accepted as evidence.

- **F11 RESOLVED**: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:217,239,590: targeted Nuke Test parameters and Nuke Publish replace the specific direct application-test/publish commands. Regression check: The independent Build.Tests prohibition is still contradicted by the retained MigrationIntegrationTests route; R2-F8.

- **F12 RESOLVED**: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:217,239,583-584 maps plugin gates to Nuke TestPluginCore/TestAgentPlugins, Pester only; bats retirement is a prerequisite. Regression check: PowerShell adapter/runtime and governed-work sequencing gaps remain R2-F6/R2-F10; no direct npm/jest/bats gate is accepted.

## Material findings and required changes

### R2-F1 (Implementer edit)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:70

The restored publication instructions still omit the attached plan rule to stop and report the exact error on branch-protection or non-fast-forward rejection, without force-pushing or substituting a PR.

Evidence: The paragraph restores the second fetch, rebase, document checks, readback, remote SHA/file-list checks, and retained worktree. A search of all three documents finds none of branch protection, non-fast-forward, force-push, or Stop and report. This is the remaining part of R1 F1, not a claim that the observed publication rewrote history.

To PASS: Restore the exact rejection behavior in the publication procedure and keep it with the successful publication evidence.

### R2-F2 (Implementer edit)

Location: docs/Stateless-Agent-Context-Management.md:153; docs/Stateless-Agent-Context-Management.md:531; docs/Stateless-Agent-Context-Management.md:545; docs/Stateless-Agent-Context-Management.md:548; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1537; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1929

A caller can still obtain a passing model grade without a verified independent passing review. The verifier checks caller-writable request metadata, an hvRequestId mention, and a response containing the VERDICT JSON marker; it never binds the submitted scores/findings to a parsed response verdict or live reviewer execution.

Evidence: reviewer.agent merely contains the configured agent name. No live turn_context or equivalent execution proof is checked. Findings only require known claimIds and valid outcomes; an empty array satisfies that rule. A response receipt containing DISAGREE or unrelated verdict text plus caller-supplied 1.0/1.0 and empty findings passes the written checks. The code then computes zero FAIL/UNKNOWN and can trust the response when the deterministic grade passes. R1 F8 arithmetic was repaired: submission now requires claims/evidence and PASS+FAIL=0 yields zero. Its independent-grade forgery defect remains.

To PASS: Specify server verification of the actual reviewer execution and independence, parse the final verdict, bind it to hvRequestId, responseId, and reviewed content/evidence, and require submitted scores and complete claim findings to equal that verdict. Reject missing, empty, mismatched, unrelated, non-passing, or unverifiable evidence with named errors and negative tests. This can use the operator-directed external HV request flow; it does not require adding an HV pipeline.

### R2-F3 (Implementer edit)

Location: docs/Stateless-Agent-Context-Management.md:41; docs/Stateless-Agent-Context-Management.md:47; docs/Stateless-Agent-Context-Management.md:124; docs/Stateless-Agent-Context-Management.md:548; docs/Stateless-Agent-Context-Management.md:561; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1425

Agent-context trust can be configured below the attached plan minimum of 98 percent correctness and completeness.

Evidence: Trust:Correctness and Trust:Completeness accept the entire [0,1] interval. Model-grade verdicts and trust use those configured values, not a 0.98 floor. For example, thresholds 0.5 and a model grade 0.5/0.5 with all PASS findings can satisfy the written trust rule. The default 0.98 does not enforce at least 0.98.

To PASS: Validate agent-context trust thresholds in [0.98,1] or apply an explicit immutable 0.98 minimum at acceptance, with boundary and lower-configuration rejection cases. Leave the already approved Phase B configurable review thresholds unchanged.

### R2-F4 (Implementer edit)

Location: docs/Stateless-Agent-Context-Management.md:53; docs/Stateless-Agent-Context-Management.md:59; docs/Stateless-Agent-Context-Management.md:60; docs/Stateless-Agent-Context-Management.md:79; docs/Stateless-Agent-Context-Management.md:223; docs/Stateless-Agent-Context-Management.md:383; docs/Stateless-Agent-Context-Management.md:398; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:573

The promised replay from recorded inputs has no authoritative storage contract for all of those inputs.

Evidence: The package stores a hash of options and all candidate source hashes, but only TokenBudget and BytesPerToken as option values. It does not store focusRefs, the complete options snapshot, or immutable versions/content of omitted candidates and mutable non-Git requirements, TODOs, turns, and profile files. Included sanitized items and a hash cannot reconstruct those values after they change. Replay nevertheless requires rebuilding with recorded options and inputs at the recorded commit.

To PASS: Define an immutable replay-input snapshot or immutable version references, including every option, focusRef, candidate and ranking input the builder consumes. Define unavailable-snapshot errors and add replay cases after config, omitted source, and non-Git source changes. Update entities, DTOs, contracts, and both document copies.

### R2-F5 (Implementer edit)

Location: docs/Stateless-Agent-Context-Management.md:51; docs/Stateless-Agent-Context-Management.md:55; docs/Stateless-Agent-Context-Management.md:142; src/QBrainAi.Storage/Entities/SessionLogEntity.cs:26; src/QBrainAi.Storage/Entities/SessionLogTurnEntity.cs:22

The meaningful-key size guarantee relies on a false 64-character limit for session and request ids.

Evidence: The live session and request entities each allow 256 characters. The new package root fields themselves allow 128. Concatenating pkg-/rsp-/grd-/hvr- and suffixes can therefore exceed the common 128-character key limit or truncate existing root ids; no normalization, collision, compatibility, or explicit rejection contract is supplied.

To PASS: Define a bounded meaningful key scheme, such as a readable prefix with a stable collision-resistant suffix, while preserving the complete existing root ids. Specify lengths, suffix overflow/collision behavior and errors, and test existing maximum-length ids across all storage providers.

### R2-F6 (Implementer edit)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:119; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:127; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:421; docs/Stateless-Agent-Context-Management.md:269; plugins/core/lib-ps/plugin-hook.ps1:5; plugins/core/lib-ps/Invoke-McpPlugin.ps1:4

The exact adapter commands do not match the real core entry points, so the supposedly logic-free adapters cannot dispatch the specified events and tools as written.

Evidence: plugin-hook.ps1 accepts canonical HookName values such as user-prompt-submit and stop-gate, not UserPromptSubmit, beforeRun, afterRun, chat.message, or session.idle. Invoke-McpPlugin.ps1 accepts Command Status, Invoke, or CompleteTurn and a separate Method; the adapter/descriptor contract supplies only -Command <command> -Params <arguments>, omitting Method. Agent-context section 6.5 itself requires -Command Invoke -Method client.AgentContext.<Method>. Params is a string; passing a host arguments object without a defined serialization/argv contract is not an exact Linux/Windows invocation. The proposed adapter tests replace both real entry points with a recording fixture, so asserting the proposed arguments alone would not detect that the real parameter binding rejects them.

To PASS: Lock an event-to-canonical-hook mapping in the descriptor/core contract, or explicitly extend the core accepted event contract. Define descriptor method plus Command=Invoke and complete native argv/stdin serialization, workspace/host binding, and return behavior on both OSes. Exercise the real entry-point parameter binder as well as the recorder fixture. Retain plugin_core_unavailable behavior.

### R2-F7 (Implementer edit)

Location: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:121; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:125; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1053; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1062; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:393; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:417

The Phase A/B architecture and activities contradict approved contracts: invalid mint nonces use the wrong code, and a client can write the server-owned Unstarted review status.

Evidence: TR-MCP-SESSIONTURN-004 AC-005 specifies 400 ingress_nonce_invalid, but the mint contract/activity use validation_error and the architecture contains no ingress_nonce_invalid. FR-MCP-SESSIONREVIEW-006-AC004 refuses both Unnecessary and Unstarted with review_unnecessary_not_client_writable. The activity rejects only Unnecessary, then routes Unstarted, Running, or Other through validation and save. The plan has a named ReviewController_ClientCannotWriteUnstarted case.

To PASS: Use ingress_nonce_invalid for the nonce branch in the mint contract, activity, sequence and response processing. Route both Unstarted and Unnecessary to review_unnecessary_not_client_writable in the review activity and corresponding sequence/contract. Reconcile every changed diagram with the catalog cases. Valid Mermaid alone does not make these paths faithful.

### R2-F8 (Implementer edit)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:77; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:215; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:217; build/Build.TestDependencies.cs:70

The plan still explicitly runs Build.Tests through the Nuke MigrationIntegrationTests target, contrary to the operator rule that Nuke never runs its own tests.

Evidence: Line 215 acknowledges and retains Category=Integration cases of tests/Build.Tests inside MigrationIntegrationTests. The live target project list includes Build.Tests at line 73. Relabelling their results infrastructure does not remove the prohibited execution. The newer general exception for direct Build.Tests contradicts this specific required path.

To PASS: Plan removal of Build.Tests from MigrationIntegrationTests before its first required gate. Run the complete Build.Tests project directly as its independent infrastructure step and update gate diagrams/text accordingly. Keep all other build/test execution under native Nuke.

### R2-F9 (Implementer edit)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:171; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:173; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:177; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:183; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:590

The global four-phase order places real migration/backfill implementation before the phase that must establish RED and mocks-green evidence.

Evidence: Phase 2 says members are neutral stubs, but also completes migrations in full; TurnReviews explicitly says S3 schema, migration, and backfill are completed in Phase 2. Phase 3 then requires every new/changed unit test to be RED against those stubs, including the migration/backfill contract tests. This contradicts both the operator interfaces/stubs -> tests/mocks -> implementation order and the plan rule that all new/changed behavior has RED before code.

To PASS: Keep Phase 2 to contracts and compiling migration/backfill seams; allocate real schema/backfill behavior to a specifically owned and serialized Phase 4 storage work package after Phase 3 evidence. Alternatively specify a different sequencing exception and obtain an operator decision before adopting it; the straightforward conforming fix requires no new decision.

### R2-F10 (Operator decision required)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:143; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:145; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:150; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:155; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:193; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:197; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:246; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:322; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:450; MEMORY-PROCESS-007

The PowerShell rewrite removes approved governed-host guarantees and their discriminating tests while deferring the decision about keeping them. The explicit TODO dependency gates only lib-node deletion; the general contract-ACK rule still blocks dependent integration. The plan therefore either drops guarantees on adoption or blocks that adoption on an unresolved, unallocated disposition.

Evidence: At 5fa1d984, FR-MCP-SESSIONTURN-008-AC001 requires a context-bound bridge with no ambient HTTP fallback, a context-bound durable outbox with pending/rejected/quarantined completion blockers, and no global/foreign-workspace replay. Its TEST row names governedContext_noAmbientHttpFallback, governedFlush_contextBoundOutboxOnly_noForeignReplay, and sessionTool_preservesStructuredCause_C9. Current line 322 substitutes generic forwarding and line 450 removes those cases. Current C8 still retains the governed completion obligation while line 145 says the Node work may move or end and is neither ported nor deleted before a decision. The live 120696-character MEMORY-PROCESS-007 snapshot contains no 2026-10-11 entry or PowerShell-disposition CONTRACT/REQUEST. Line 145 describes a requested/posted disposition whereas P0.7 line 246 still plans that future post. No current operator disposition is evidenced. All other removed A8/S9 tags were checked as consolidation into the common A3-A7/S8 core, with renamed host-specific cases; these governed protections are a material exception.

To PASS: Obtain and record the disposition, then restore concrete owned PowerShell contracts/tests and dependencies for retained guarantees, or explicitly amend the affected approved requirements if the operator cancels them. Correct the posted-versus-planned coordination claim. Gate the first dependent core/host integration on that disposition and implementation, not merely deletion of lib-node.

Operator decision: Disposition of QB-AGENT-001 governed-host protections during the PowerShell-only migration
- Port and preserve the governed bridge, outbox isolation, typed failures, and completion obligations in PowerShell before dependent host adoption.
- Explicitly cancel or narrow the affected work and approve matching amendments to the existing Phase A/B requirements and tests.

### R2-F11 (Implementer edit)

Location: docs/Stateless-Agent-Context-Management.md:232; docs/Stateless-Agent-Context-Management.md:269; docs/Stateless-Agent-Context-Management.md:292; docs/Stateless-Agent-Context-Management.md:559; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1943

The claimed complete public/internal definition set still has an undefined trust result type and name-only PowerShell builder/tool-descriptor contracts.

Evidence: DecideAgentResponseTrustCommand is ICommand<AgentResponseTrustDecision>, but that result has no defined fields/type. The DTO list instead describes its result as the status in AgentResponseGradeResult. Builder entries such as New-McpAgentContextBuildPackageParams supply only names, without a PowerShell parameter signature, optional/default rules and exact output mapping. Tool-descriptor entries name the tool but leave the command/method mapping unresolved; the concrete mismatch is demonstrated in R2-F6. Byte-identical copies repeat the gap rather than resolving it.

To PASS: Define AgentResponseTrustDecision and make the handler, DTO list and diagrams agree. Provide complete builder signatures and descriptor schema/entries including command, method, parameter encoding, defaults and mutation metadata for every external unit; publish the same contract in both section-6 copies.

### R2-F12 (Implementer edit)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:571; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:576; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:580; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:582; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:583; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:584; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:635; docs/Stateless-Agent-Context-Management.md:868

The catalog is not fully traceable under its own G5 rule for the newly added HV-list operation and prerequisite Nuke TRs.

Evidence: QueryAgentContextHvRequestsQuery is an external handler with REST, MCP, client, REPL, builder and tool contracts in section 6, and is owned by ResponsesAndTrust, but is omitted from the implementation-unit catalog list and both the grading and surfaces FR enumerations. TR-MCP-NUKE-011..013 are allocated to P0.1a before Phase 1, but their catalog only says Evidence: Build.Tests cases; no named cases or AC identifiers are supplied. In contrast TR-MCP-NUKE-003 AC-005 names ValidatePluginPowerShellOnly_AllPathsIncludingTests_AdapterEntryOnlyTests. This is a prerequisite traceability gap, not an objection to the authorized Phase C catalog expansion in P0. All 96 proposed requirement IDs checked against the live main-workspace FR/TR/TEST lists are free (29 FR, 36 TR, 31 TEST). The rewritten SESSIONTURN-008 AC-005 is allocated to TEST-SESSIONTURN-009; the remaining ACs to TEST-SESSIONTURN-008; SESSIONREVIEW-008 AC-001..003 have the corresponding score, descriptor and Stop-gate cases. Those portions are not the failure.

To PASS: Add QueryAgentContextHvRequestsQuery consistently to the FR/TR/TEST expansion inputs. Name and allocate the Build.Tests cases and ACs for Nuke targeted-test parameters, Pester core target and all-host plugin target before P0.1a implements them, including missing repo/path, failure, skip and forbidden-runner refusals.

## Verification and persistence

The JSON receipt contains the command inventory with exit codes, live identity proof, profile hashes, complete claim/finding data and score rationale. Full checked command output is in docs/receipts/hv/20261012T004516Z-plan-readiness-merged-r2.commands.jsonl. Some search commands deliberately return rg exit 1 for no matches. Mermaid 11.17.2 rendered all 116 blocks with exit 0; all 59 stateless blocks are included verbatim in architecture. The new contract IDs are free in the live main-workspace store. The publication commits, single-file ancestry, remote tip, matching blob and retained worktree were verified read-only.

Entire-result MCP persistence/readback proof: docs/receipts/hv/20261012T004516Z-plan-readiness-merged-r2.persistence.json. Receipt redaction proof: docs/receipts/hv/20261012T004516Z-plan-readiness-merged-r2.redaction.json. Only own audit files and own reviewer turn are changed.

Implementer edits: R2-F1, R2-F2, R2-F3, R2-F4, R2-F5, R2-F6, R2-F7, R2-F8, R2-F9, R2-F11, R2-F12. Operator decision required: R2-F10. No UNKNOWN claims.
