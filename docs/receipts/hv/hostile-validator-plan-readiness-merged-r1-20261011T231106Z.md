# Hostile validator: merged plan readiness, round 1

TimestampUtc: 2026-10-11T23:21:10Z
OverallVerdict: DISAGREE; Accuracy: 89; Completeness: 86; Confidence: 0.97
HeadSha: 5fa1d9849fa42efe1bd27c15d3bc03a6eafe89fa
ReviewerSessionId: Codex-20261011T231246Z-plugin-session
ReviewerRequestId: req-20261011T231300Z-plan-readiness-merged-r1
Live identity: Codex gpt-6-sol xhigh, thread 01a12d3b-f610-7df3-b410-d42642d89f9f; session_meta and turn_context timestamps recorded in JSON receipt.
Add-profile: skill and 19 non-skill profile Markdown files read in full. Main marker read in full. workflow.memory.list returned 13; every memory read.

## Claims

- M1 FAIL: Attached summary, architecture, eight steps, and tests largely mapped, but publication details dropped and every-response grading contradicted (F1-F3).
- M2 FAIL: Section 6 copies are byte-identical (SHA256 64D8F900EDD57B3CAEFD5AFF5E92CBA31149BD3EE3565C3FE609813DB6922682), but two MCP signatures are placeholders and FR-008 overpromises internal units (F4-F5).
- M3 FAIL: Mermaid CLI 11 rendered all 91 blocks; A1-A12, B1-B7, C1-C21, SA1-SA5, SB1-SB4, SC1-SC10 and SC12-SC21, UC1-UC15 present. C11 is internal, so no submit diagram is reasonable. C14/C19 error exits and C20 fallback processing are incomplete (F2,F6).
- M4 FAIL: The architecture-wide REST body contradicts the approved wire-contract convention and live Phase A/B controller response (F7); no other Phase A/B substantive unit change was scored.
- M5 FAIL: Live source supports ClassifiedError, prefixed keys, UTC timestamps, unique indexes, GenericClientPassthrough, and IGraphRagService members; architecture error body, fallback path, empty-grade formulas, and model-grader attestation disagree (F2,F3,F4,F7,F8).
- M6 FAIL: Phase C after B, HV-C, code freeze, rollout/rollback, AC allocation and acyclic TODO chain are present, but P0 approval-before-creation is circular and baseline evidence false (F9-F10).
- M7 FAIL: No GitHub Actions or Azure Pipelines gate found; Octopus release-only and WSMan route stated. Direct non-Build.Tests dotnet test, dotnet publish, and plugin suites violate Nuke rule (F11-F12).
- M8 PASS: origin/develop at 51d2482e; parent chain 7c27457d->99da6af8->51d2482e; both commits touch only the spec; branch blob equals develop blob 458085a592f6a58eb036ff6f83dff9dccc7fcfb6; worktree and branch retained. Second subject is a later operator-directed refinement and acceptable. The final graph proves fast-forward ancestry, not the exact historical push command.
- M9 FAIL: Main MCP store has no AGENTCONTEXT FR/TR/TEST ids or TR-MCP-NUKE-011; all four cited triage reports exist. UC outline and one-TR/TEST-per-FR are present, but circular HV-P0 prerequisite and FR-008 contradiction prevent creation and acceptance under G5 (F5,F9).
- M10 PASS: Both new docs start Apache-2.0 SPDX; no en/em dash in three docs; no marker key or long apiKey/api_key/X-Api-Key assignment in three docs or two reviewed commits.

## Findings and required changes

### F1 [implementer edit] (M1)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:70

Defect: The attached Commit and Push workflow is compressed and drops the second fetch immediately before publishing, conditional rebase, named document checks and on-disk readback, remote SHA and file-list verification, and the explicit stop/report rule for branch protection or non-fast-forward rejection.

Evidence: The attached plan states each step; the merged plan line 70 retains only the first fetch, generic validation, commit, push, no-force, and retention. Neither companion document restores the omitted steps.

Change to pass: Record every attached publication step and its observed result in the merged plan, including the later update commit as an operator-directed follow-up.

### F2 [operator decision] (M1, M3, M5)

Location: docs/Stateless-Agent-Context-Management.md:742-770; docs/Stateless-Agent-Context-Management.md:375-379; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:SC20

Defect: C20 falls back to a stateful answer on budget overflow or package-build error, then terminates without submission or grading. The SC20 sequence submits the fallback answer, but C6 rejects an Overflow package and requires a package for a response. The same path cannot both comply with the every-response grading rule and the stated C6 contract.

Evidence: Spec lines 15, 29 and plan line 534 require every response graded; C20 activity lines 758-761 end at stateful fallback; C6 lines 377-379 reject Overflow packages. SC20 claims fallback submission.

Change to pass: Choose one explicit policy: fail the invocation on package error/overflow, or allow an auditable stateful fallback response with a valid nullable/alternate package link and deterministic plus independent grading. Make C6, C20, SC20, DTOs, and errors match.

### F3 [operator decision] (M1, M5)

Location: docs/Stateless-Agent-Context-Management.md:769; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:SC9-SC10

Defect: The plan assigns the independent model grade for each runtime response to the slice hostile validator. A slice HV runs once at a development gate and cannot grade each agent invocation.

Evidence: Spec C20 line 769 says the slice HV records the model grade; plan line 534 requires every response to receive an independent model grade.

Change to pass: Define a per-response independent grading execution and receipt path. Reserve slice HV for implementation review, or explicitly change the every-response requirement with operator approval.

### F4 [implementer edit] (M2, M5)

Location: docs/Stateless-Agent-Context-Management.md:424,655; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1419,1650

Defect: Two MCP endpoint signatures remain ellipses: agent_context_query_responses(...) and agent_context_query_relationships(...). Their exact arguments and workspacePath-first order are unspecified.

Evidence: Both copies of section 6 use the same literal ellipses while the plan promises every endpoint and MCP workspacePath first.

Change to pass: Write full MCP signatures, parameter types, optionality, ordering, and corresponding client/REPL mappings in both copies.

### F5 [implementer edit] (M2, M9)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:538; docs/Stateless-Agent-Context-Management.md:512,742-746

Defect: FR-MCP-AGENTCONTEXT-008 promises every C1-C21 unit on REST, MCP, client, REPL, and all plugins, while C11 explicitly has no endpoint and C20 is plugin-internal. The catalog cannot be accepted under its own surface AC as written.

Evidence: Plan line 538 says every unit and cites C1-C21; C11 says No endpoint; C20 names only plugin-core functions.

Change to pass: Scope FR-008 to externally callable units and list internal exceptions, or provide genuine endpoints for C11 and C20; allocate ACs accordingly.

### F6 [implementer edit] (M3)

Location: docs/Stateless-Agent-Context-Management.md:584-608,709-740; corresponding architecture section 6

Defect: C14 and C19 contracts state 400 validation_error, but their activity diagrams contain no validation branch or 400 exit. C20 activity also omits the response and grade path after fallback.

Evidence: C14 line 587 and C19 line 710 list 400; diagram nodes C14 591-607 and C19 714-739 only draw workspace, mode, and normal processing.

Change to pass: Draw each contract error exit and align C20 with the resolved fallback contract. Add every C6/C9/C10 error response to SC20 processing branches.

### F7 [implementer edit] (M4, M5)

Location: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:77; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:626; docs/Stateless-Agent-Context-Management.md:226

Defect: The architecture cross-cutting convention gives a REST error body with error/message/details, contradicting the approved plan and live ClassifiedError problem+json contract. It governs Phase A and B diagrams as well as C.

Evidence: Architecture line 77 versus plan line 626 and spec line 226; SessionLogController.cs:714-732 emits type,title,status,detail,code,message,retryable,details.

Change to pass: Change architecture line 77 to the actual ClassifiedError problem+json body and ensure diagram annotations follow it.

### F8 [implementer edit] (M5)

Location: docs/Stateless-Agent-Context-Management.md:447,375-379,479-507

Defect: Deterministic correctness divides by PASS+FAIL and completeness by all claims, yet C6 allows empty claims and all-UNKNOWN evidence; the scores are undefined. C10 accepts caller-supplied grader/model/effort and receipt paths without validating the receipt content or live grader identity, allowing a forged passing model grade to trust a response.

Evidence: C9 line 447 formulas; C6 line 379 validation list; C10 lines 493-505 checks path presence and supplied identity strings only.

Change to pass: Require nonempty claims/evaluable checks and define fail-closed zero-denominator scores; perform the independent grade server-side or verify signed/live grader metadata and receipt-to-grade correspondence before trust.

### F9 [implementer edit] (M6, M9)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:201-206

Defect: P0.4 requires the agent-context catalog to be approved through HV-P0 before P0.4 creates it, while HV-P0 occurs at P0.9 after creation. This makes P0 impossible. P0.5 also says to create only use cases in the earlier Use cases (new) section, omitting UC-10..UC-15 despite P0.4 requiring them there.

Evidence: P0.4 line 201, P0.5 line 202, P0.9 line 206, and UC-10..15 outline line 542.

Change to pass: Put a complete catalog in the plan before P0.4 and use a distinct precreation review, or move approval to the existing end-of-P0 HV. Name UC-10..15 explicitly in P0.5.

### F10 [implementer edit] (M6)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:6

Defect: The plan states that no src/, tests/, build/, or plugins/ paths changed since f56dcf70 and makes P0.1 rely on that claim. It is false at the review HEAD.

Evidence: git diff --name-only f56dcf70..HEAD lists paths in all four directories; d6291a48 and 8009289c are postbaseline source/test commits.

Change to pass: Update the baseline statement and P0.1 source-anchor re-verification rule to the actual HEAD and changed paths.

### F11 [implementer edit] (M7)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:76,546,697

Defect: The process ruling says all non-Build.Tests builds and tests run via Nuke, yet P0 requires direct dotnet test for QBrainAi.Support.Mcp.Tests and the deploy procedure calls dotnet publish directly.

Evidence: Plan line 76 Nuke rule; line 546 direct targeted dotnet test; line 697 direct dotnet publish before UpdateService.

Change to pass: Use the P0.1a Nuke Test parameters for the baseline-green test and a Nuke build/publish target for the deploy artifact, while keeping Build.Tests as the expressly direct exception.

### F12 [implementer edit] (M7)

Location: docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:128-130,176,715; docs/Stateless-Agent-Context-Management.md:1532; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:2702

Defect: Plugin gates still prescribe direct Invoke-Pester, bats, and npm build/test commands despite the stated all-build-and-test-through-Nuke ruling.

Evidence: The cited G4/G6 and plugin matrix commands are direct shell suites, not Nuke targets.

Change to pass: Define native Nuke plugin-test targets or make the commands explicit substeps executed by Nuke, and make every gate invoke those targets.

## Verification commands

- Exit 0: pwsh.exe reads of add-profile skill, 19 profile Markdown files, and main marker
- Exit 0: Codex plugin workflow.memory.list -ParamsObject scope Effective; 13 memory reads
- Exit 0: Codex plugin workflow.requirements.listFr/listTr/listTest and four workflow.triage.getReport, main workspace
- Exit 0: git status/show/diff/ls-remote/hash-object/rev-parse/worktree list/branch checks
- Exit 0: npm install --prefix /tmp/hv-mermaid-20261011 @mermaid-js/mermaid-cli@11 --no-save with cache in /tmp
- Exit 0: mmdc -i architecture doc -o /tmp/hv-mermaid-20261011/architecture-rendered.md -a /tmp/hv-mermaid-20261011/artefacts -j 4 -p puppeteer.json; 91 blocks rendered
- Exit 0: PowerShell SPDX, dash, marker key and long api-key assignment checks over three docs and reviewed patch
- Exit 1: Initial npm install with unwritable default cache (retried with /tmp cache)
- Exit 1: Node mermaid.parse without browser DOM (purify.addHook unavailable; superseded by successful mmdc render)

The two failed setup commands were replaced by successful equivalents; they do not make a claim UNKNOWN. No implementation files were edited.

## MCP persistence

Full result was persisted in the own reviewer turn response (updateTurn persisted true); decision and observation dialog and receipt/design-decision actions also persisted true. client.SessionLog.QueryAsync readback found the own request, M10 PASS, F12, and turn status completed (response envelope 38961 characters). completeTurn returned persisted true. Runner request/response JSONL paths are recorded in JSON receipt.


Runner response JSONL: two marker-key values redacted; a stream event was repaired in place and subsequent read found no matching sensitive values.
M9 live-store addendum: client.UseCases.ListAsync returned an empty list; workflow.todo.query returned no MCP-AGENTCONTEXT-001..009 matches in the main workspace.
