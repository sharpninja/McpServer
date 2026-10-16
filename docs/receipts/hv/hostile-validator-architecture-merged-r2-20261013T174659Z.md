<!-- SPDX-License-Identifier: Apache-2.0 -->
# Independent hostile architecture validation, round 2

DISAGREE. Accuracy 95, completeness 94, confidence 98. Six implementer fixes; no operator-decision failures. Claims: 12 PASS, 6 FAIL, 0 UNKNOWN.

TimestampUtc: 2026-10-13T18:34:25.2253917Z. Reviewed HEAD: c3e64d9a4a4a795910526b959d980160114fc05e. Codex gpt-6-astra, effort max, live thread 01a1365f-f247-7621-a64e-943e8cfd3cf6; own turn_context proof at /home/sharpninja/.codex/sessions/2026/10/13/rollout-2026-10-13T12-47-00-01a1365f-f247-7621-a64e-943e8cfd3cf6.jsonl. No review time bound was applied.

Startup completed: add-profile skill and all 20 non-skill profile Markdown files read in full, including operator-rulings-2026-10-11.md; main marker read in full and trusted; workflow.memory.list and all 15 memories read in full. Reviewer MCP turn: Codex-20261013T175033Z-architecture-merged-r2 / req-20261013T174659Z-architecture-merged-r2. Installed Codex wrapper with -ParamsObject and isolated cache; no raw MCP REST or Claude session-log wrapper.

Architecture is authoritative; derived stateless bodies match. Independent Mermaid 12.2.0 rendering establishes all 213 blocks, 58 landscape sequences, at most three letter pages and 8-12 point text. All 102 proposed requirement IDs are free. These checks do not resolve the functional defects below.

## AR2-F1: First-ingress recovery still lacks a durable session identity before its write-ahead record

Locations: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:538; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:544; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1022; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1023; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1024; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1042; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1047; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1056; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1058.

The first-ingress diagram writes the failsafe mint record before it assigns the previously absent mcpSessionId. It then records the new ID only in the enclosing single Update-TurnState change, whose durable replace occurs after OpenTurnUnderLock returns. This contradicts the guarantee that the write-ahead root envelope already contains the stable session ID and that a crash before the open is confirmed cannot change it.

- Update-TurnState applies the change, then writes/flushed-renames the file (538, 544-547). OpenTurnFromPromptHook explicitly makes the whole open one such change (1022, 1027, 1039-1042).

- The current edge order is WA Write-ahead failsafe mint record -> FI no mcpSessionId recorded -> FIW Record a new mcpSessionId in this replace -> Invoke-SessionOpen (1056-1059). At WA a first-ingress host has no session ID to put in its required root envelope.

- A kill after WA or after the server open/mint but before the outer replace leaves the recovery record without the promised durable binding, or leaves the host state without that ID. A later start can allocate a second session instead of replaying the original pair. Moving an in-memory assignment before the network call does not create a durable boundary.

- StartHostSession itself is repaired: its standalone Update-TurnState completes before Invoke-SessionOpen (1391-1392). Final-refusal retention is also repaired (1047). The remaining defect is the first-ingress path, so AR1-F6 and its inherited R4-F8/R3-F6 chain are only partially resolved.

Required change: Make the first-ingress session-ID allocation a durable, completed atomic state write before both the failsafe mint record and any open/mint call. Then write the full immutable recovery envelope and perform the open/mint. Reconcile the one-replace/lock wording and both diagrams with those explicit boundaries; add crash cases after ID persistence, after the write-ahead record, and after a lost open/mint reply that prove the same session/request binding is recovered.

Classification: implementer edit; no operator decision. AR1-F6 remains NOT RESOLVED; final-refusal retention and the standalone SessionStart path pass, but the first-ingress durability claim still does not.

## AR2-F2: The shared turn-write activity still refreshes the session summary inside the turn transaction

Locations: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:152; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:344; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:414; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:438; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:623; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:1678.

The corrected prose makes summary refresh a separate best-effort write after a single-turn commit, but TurnWritePipeline is explicitly run inside SerializableWrite and still ends with Children stamped, summary refreshed: turn id or per-turn outcome. The Upsert sequence calls that pipeline inside SerializableWrite and returns the result, without a separate refresh. The canonical shared activity therefore still specifies the parent-row write that AR1-F9 was meant to remove.

- 152, 344, 414 and 623 now correctly say the single-turn transaction reads the parent row only and changes only its turn and children; the summary refresh is post-commit and cannot fail that turn.

- 414 says the shared pipeline runs inside SerializableWrite. Its sole successful terminal at 438 says summary refreshed without distinguishing Submit from the single-turn modes.

- The sequence at 1678 consumes that component as SerializableWrite: TurnWritePipeline mode Merge, then returns turnId (1680). Develop really does refresh the common parent in the turn save (SessionLogService.cs:762,794,879 at a8d3934b), making this a functional isolation distinction rather than a label preference.

Required change: Remove summary refresh from the single-turn TurnWritePipeline activity and its transaction. State/draw the post-commit refresh as a separate shared call whose failure is swallowed/logged, while retaining Submit mode summary refresh in its session transaction. Make the single-turn sequences agree and keep the three-provider isolation and refresh-failure cases attached to that behavior.

Classification: implementer edit; no operator decision. AR1-F9 is partially fixed in prose but NOT RESOLVED in the shared activity used by the units.

## AR2-F3: Missing GraphRAG documents can remain permanently unprojected after reconciliation

Locations: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:2851; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:2853; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3690; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3713; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3714; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3740.

ReconcileProjectedDocuments returns a Projected row with a missing document to Pending but never clears GraphRagDocumentId or ProjectedContentSha256. ProjectOneResponse skips ingestion solely when the projected hash equals ContentSha256, independent of row state or document existence. The newly Pending row still has that matching hash and is skipped forever.

- The projection model keeps both the document ID and projected hash (2851). Reconciliation only says back to Pending (3690, 3740), while promising the next pass ingests it again.

- Counterexample: a trusted response has Projected(D,H), document D disappears, and reconciliation sets State=Pending. The next pass selects the row (2852), reaches the hash-equality guard (3713), and exits Continue (3714), without calling IngestTextAsync. Subsequent reconciliation considers only Projected rows, so it does not repair this Pending row.

- The live GraphRagService allocates a fresh document ID on ingest and lists/deletes documents separately (a8d3934b, GraphRagService.cs:343-379,411-448,488-492); no service operation implicitly clears the relational projection hash. The design trace is archived in 20261013T174659Z-architecture-counterexamples.json.

Required change: Define the missing-document reset to clear the obsolete document ID and projected hash, or make the skip condition require a confirmed current Projected document. Apply the same invariant to every transition that can lose/remove a document, and add a two-pass case asserting a new document is actually ingested and the row becomes Projected after reconciliation.

Classification: implementer edit; no operator decision. The original AR1-F7 orphan cleanup obligation is now represented by the GraphRAG store and reconciled, but its new missing-document repair path introduces this regression.

## AR2-F4: Supersede retries fail after the replacement changes status

Locations: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3515; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3516; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3532; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3533; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3535; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:4557; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:4561; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:983.

Supersede promises that repeating the same replacement returns 200 unchanged, but both its activity and sequence validate the replacement current status before checking whether the original was already superseded by that replacement. A legitimate subsequent lifecycle change makes a previously applied request fail on retry.

- Counterexample: A is superseded by B, then B is superseded by C (or B is rejected). Retry the successful request A -> B. B is now Superseded/Rejected, so 3533 and 4557-4558 return 409 agent_context_supersede_invalid before reaching the already-by-this-replacement 200 branch at 3535/4561.

- 3515-3516 explicitly promise a repeat naming the same replacement returns 200 unchanged; the plan derives SupersedeRepeatSameReplacementReturns200Unchanged at 983. The current ordering cannot satisfy that promise for a valid chain.

Required change: Inside the serialized/locked operation, recognize the already-applied original/replacement pair and return its stored unchanged result before mutable replacement eligibility checks. Apply those checks only to a new transition; retain whole-turn-key validation, rejection of another replacement, and cycle protection. Add retries after the replacement is subsequently superseded and rejected.

Classification: implementer edit; no operator decision. Holistic finding outside the original AR1-F3 whole-turn-key counterexample; that identity repair itself passes.

## AR2-F5: The token budget does not account for rendered findings

Locations: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:2687; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:2733; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:2770; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:2771; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3078; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3153; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3162; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3163; docs/plans/PLAN-SESSIONTURN-SESSIONREVIEW-001.md:883.

The only token accounting is the per-item byte estimate and the sum/subtraction of item tokens. The canonical renderer additionally emits a line for every contradiction/blocker finding, but the architecture defines no token charge or reserve for those lines. Mandatory item tokens equal to the entire budget are explicitly accepted, so a package with such items plus a finding exceeds its stated bound under the same estimator.

- 2687 defines ceil(utf8ByteCount / BytesPerToken) per item; 2733 puts TokenCount on items. Findings are separate records with a summary up to 2048 characters (2770-2771), not counted items.

- 3078 emits finding lines after the items and says mandatory tokens equal to the budget build. FillBudgetWithCandidates tests/subtracts only the candidate tokens (3153, 3162-3163). No final rendered-size check is defined.

- Even granting that each item estimate includes its header, four mandatory item segments totaling 1024 estimated tokens plus a 256-byte finding at BytesPerToken=4 yield at least 1088, while the current rule accepts the 1024-budget build. This is a design arithmetic trace, not an assertion about a tokenizer or future implementation.

Required change: Define the estimator over all content the bounded package renders, including headers, separators, and finding lines. Reserve/count mandatory rendering overhead before the overflow decision and account for optional-item-induced findings during budget fill; preserve the explicit overflow and deterministic replay rules. Add exact-boundary cases with a contradiction and a blocker so the serialized/rendered package cannot exceed the configured estimate.

Classification: implementer edit; no operator decision. Holistic completeness defect affecting the attached plan bounded-package/explicit-overflow promise; it is not a demand to replace the accepted deterministic estimator with a model tokenizer.

## AR2-F6: Post-commit response orchestration is repeated across units without its required shared component

Locations: docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:22; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:141; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:180; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:182; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:449; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:3045; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:5451; docs/architecture/SessionTurn-Review-AgentContext-Architecture.md:5456.

The grade units, Reject, Supersede and EvaluateStaleness all receive the same post-commit algorithm: detect qualifying status transitions, select response/package dependents, check Mode, dispatch staleness before projection, and log/swallow dispatch failures. It is stated globally, but no section 3.5 component owns and encapsulates that algorithm, exposes its seam, or has its one test class. IDispatcher only dispatches the supplied request; it does not implement this selection, ordering, or failure policy. ResponseStatusTransition is explicitly inside the database transaction, so it cannot itself be this post-commit orchestration under the stated contract.

- 180 names five classes of caller and the complete shared dependent-selection/dispatch rule; 182 adds the shared projection scheduling/failure rule. 3045 repeats both as obligations of those units rather than a call to a named component.

- Section 3.5.5 names ResponseStatusTransition, IResponseStalenessRule, ResponseIdList and RelationshipWriter, but none owns these post-commit steps. The ownership and seam lists at 5451 and 5456 likewise name only each command dispatch.

- The operator rule at 22 and 141 requires logic shared by multiple units to be encapsulated as one section 3.5 component, not merely documented once. The new dependency propagation is functionally appropriate, but its implementation boundary is missing.

Required change: Add one named post-commit response-effects component in section 3.5 with an owner, namespace/folder, input/output/error contract and one test class. It should own dependent selection, Mode checks, ordering, dispatch/failure handling and projection scheduling; each committing unit calls it once with the committed transition set. Keep the actual EvaluateStaleness and ProjectAgentResponses operations as their existing CQRS units.

Classification: implementer edit; no operator decision. The AR1-F2 dependency-status fix introduces additional shared orchestration without carrying it into the DRY component catalog; the original recursive staleness rule itself is repaired.

## Every claim

- **AR1: FAIL**. Round-1 re-attacks are listed individually below. AR1-F6 and AR1-F9 remain partially unresolved; AR1-F7 gains a projection recovery regression and AR1-F2 gains an encapsulation gap. A4 is now resolved by independent rendering and archive verification.
  Findings: AR2-F1, AR2-F2, AR2-F3.

- **A1: PASS**. Architecture authority is explicit (architecture 4-6; plan 4). Alias definitions are now in architecture 3.5.6 and plan 811 references them. Compared stateless section 3.5, section 6, use cases 7.11-7.16 and delivery 8 against the architecture: exact body equality, allowing only final newline. The reviewed plan catalog and new named cases derive from those contracts; no remaining derived-only design decision was identified.

- **A2: FAIL**. Most reused behavior is catalogued with ownership and single test seams in architecture 3.5 and 8.3. The shared post-commit selection/dispatch algorithm introduced for dependent staleness still has no such component (180,182,3045,5451-5456).
  Findings: AR2-F6.

- **A3: PASS**. Architecture 3.5 and 5444 assign new types by RootNamespace plus folder, one command/query and handler per separate file. Rechecked analyzer branch fe5879ba and current config: Storage RootNamespace is QBrainAi.Support.Mcp.Storage; Services is QBrainAi.Support.Mcp; build tests moved to build/Build.Tests. New handler/test declaration inventory has no underscore; cited legacy names are distinguished from new names.

- **A4: PASS**. Manifest verified against architecture, render page and all 322 PNG pages (324 hashed artifacts, zero mismatches); all 213 embedded diagram sources equal architecture. Independently ran the archived page with Mermaid 12.2.0: all 213 rendered, every diagram at most 3 letter tiles, measured text 8-12 pt, all 58 sequences landscape. Inspected activity, nested, sequence and ER pages. Section 9 states native Mermaid sequence layout despite dagre configuration; recipe uses ELK for flowcharts/use cases/ER. Semantic activity defects are scored under M3/M4/M5, not hidden as rendering failures.

- **A5: PASS**. Reconciliation in architecture 3.1 names strengthened/replaced/unchanged records, projected risks and accepted dispositions; plan domains carry updated records and test churn. Compared live 353 FR/487 TR/512 TEST snapshot, approved pre-merge TODO, a8d3934b routes/transaction/GraphRAG/build sources and tests, nine plugin heads and analyzer paths. Completion-gate fixtures, old outcome tests, host audit, retired runners, memory renderer and Build.Tests churn are assigned to the introducing change (architecture 5559-5572). Existing baseline fixes remain out of review scope.

- **A6: PASS**. Eleven plan domains (259-1287) carry their records, updates, mappings, AC ownership and churn and map to architecture 8.3 ownership. Shared storage remains cross-domain, build tooling has TR-only accountability, and coordinator resolutions appear in the appropriate domains (for example requirement dispatch 994, projection dispatch 995, gate-later cross-context tests 964/1052).

- **M1: FAIL**. All attached-plan capability categories and opt-in/stateful pilot choices are represented (architecture 6.1-6.6,8); independent external turn-range HV replaces a future pipeline as directed, and profile/rules, hashes, relationships, trust floor, audit exclusions and publication remain. The bounded package and fail-on-overflow promise is not complete because rendered findings have no budget charge.
  Findings: AR2-F5.

- **M2: PASS**. Inventoried 38 CQRS units: each has a declared handler, request/result and activity; externally callable units have named sequences. Phase A/B models and transport distinctions are in 3.4,4.1-4.2,5.1-5.2; agent-context options/entities/DTOs/errors and every REST alias, MCP signature, typed-client/REPL method, PowerShell builder envelope and descriptor contract are in 6.2-6.5 (3041). Shared alias signatures, binding, results and refusals are now in 554-560. Semantic defects in defined contracts are separately scored under M3-M5.

- **M3: FAIL**. 38 units map one-to-one to CQRS records/handlers, with policies/processes distinguished; every unit has activity and external sequence coverage, 15 use cases have diagrams and Addendum A shows entity relationships. But the first-ingress, turn-write, projection-recovery and supersede diagrams cannot execute their own promised outcomes.
  Findings: AR2-F1, AR2-F2, AR2-F3, AR2-F4.

- **M4: FAIL**. Compared the approved live PLAN-SESSIONTURNREVIEW-001 description and a8d3934b SessionLogController/SessionLogService/MCP shapes. Terminal submit guard and dialog/query/patch/replace distinctions are preserved; new PowerShell tool-audit hook preserves host tool_call/tool_result behavior. First-ingress durability and the summary-refresh activity still contradict the Phase A guarantees.
  Findings: AR2-F1, AR2-F2.

- **M5: FAIL**. Whole-turn identity, runtime grade checks, independent receipts, recursive freshness and scoped storage are specified consistently in the repaired areas. Projection missing-document recovery, supersede retry eligibility, and complete rendered-package budget accounting have concrete counterexamples.
  Findings: AR2-F3, AR2-F4, AR2-F5.

- **M6: PASS**. Architecture 8.1-8.5 and plan 120-219 cover requirements, all interfaces/stubs, mocks-first tests and domain implementation. Ownership, dependency seams, TODO names and gates are present; merge DAG is acyclic. Plan 4 is NOT APPROVED and architecture 4 says pending approval; the code freeze and operator-directed analyzer exception are explicit. Neutral new stubs and delegating existing routes preserve the baseline until implementation.

- **M7: PASS**. Architecture 88,137,5557-5583 and plan process rules exclude GitHub Actions/Azure Pipelines gates, use native Nuke for product builds/tests, direct Build.Tests only as infrastructure, and Octopus only for releases. Verified live MigrationIntegrationTests still includes Build.Tests and the removal is an explicit prerequisite, not claimed done. Windows route is WSMan to PAYTON-LEGION2 in a worktree under F:/GitHub/McpServer/.worktrees, as detailed in the plan.

- **M8: PASS**. git show proves 99da6af8 -> 51d2482e -> 63d4743d -> 33a5a954 -> a8d3934b are consecutive parent-child commits, each changing only docs/Stateless-Agent-Context-Management.md. ls-remote confirms develop=a8d3934bc6238dc8e27f71c1d0561b55f48e88d4; origin reflog records those updates by push without ancestry rewrite. Retained agentcontext-doc-20261011 worktree and docs/agentcontext-plan-20261011 branch exist. Current branch copy is newer and unpublished. Git cannot prove which command flags were historically typed; the observed updates are fast-forwards.

- **M9: PASS**. Live read-only catalog has 353 FR,487 TR,512 TEST. All 102 new definitions (30 FR,39 TR,33 TEST) are unique and free. Plan 241-247 defines exactly-one owner normalization and Build.Tests-only Nuke exceptions; domain ownership lists and named-case catalogs allocate the changed ACs. The accepted Outline now/catalog in P0 boundary is explicit at 255, with P0.9 checking resulting store ownership; no existing/created record is falsely claimed for proposed IDs.

- **M10: PASS**. All three documents have Apache-2.0 SPDX headers and zero en/em dashes. Scanned the full 151547949-character bff6a7aa..HEAD diff, including round-1 receipts: zero current main/worktree marker-key matches, long apiKey/api_key/X-Api-Key assignments, private-key headers or GitHub credential patterns. Reviewed-document diff --check passes. Prior receipt trailing-blank warnings are not functional findings.

- **M11: PASS**. Architecture 137-138,245-257,3.5.6,8.4-8.5 and the derived plan specify one lib-ps core, logic-free required host adapters with real planned core entry-point binders, Pester-only plugin gates, and retirement after host adoption with affected legacy tests changed in the retirement. No new Node/bash/bats/Python tool is designed. Forward HV protocol is gpt-6-astra/max with no review bound. Existing separate HostileReview worker xhigh configuration and historical examples are explicitly not this HV protocol.

## Round-1 and inherited findings re-attacked

- **AR1-F1: RESOLVED**. Architecture 554-560 defines the permanent lookup/prunable delivery model and five PowerShell alias functions, binding, atomicity and typed refusals; plan 811 derives AC-007 from it. Regression check: No material regression: delivery pendingId can be resolved through the defined Get-McpAlias lookup.

- **AR1-F2: RESOLVED**. Architecture 180,469-480,2751 and 3546-3547 require recursive effective Trusted/fresh dependencies, cycle termination and post-commit dependent staleness dispatch. Hash equality alone no longer admits a rejected/stale dependency. Regression check: AR2-F6: new repeated post-commit orchestration is not encapsulated in section 3.5.

- **AR1-F3: RESOLVED**. Architecture 175,2990,3190-3191,3516-3518 and RunStatelessInvocation use agent/sessionId/requestId in recovery, idempotency and replacement validity. Live unique-key scope verified at McpDbContext.cs:391,404. Regression check: No regression in whole-turn scoping. AR2-F4 is the separate repeat-after-replacement-status-change defect.

- **AR1-F4: RESOLVED**. Architecture 245-257 defines core tool-audit for beforeTool/afterTool and tool.execute.before/after, exact dialog shape, invocation binding, failsafe and exit-0 behavior. Verified host plugin sources and lib-node/src/runtime/host-context.ts. Regression check: Dropping the old per-tool design_decision action is explicitly stated and consistent with C6/C7; tool dialog audit is preserved.

- **AR1-F5: RESOLVED**. Architecture 414,434 and Replace sequence 1737 onward consistently run CompletionGate on Replace carrying completed/failed, as well as transitions into those statuses. Regression check: No original terminal-replace bypass remains.

- **AR1-F6: NOT RESOLVED**. Final refusals now retain the mint record with drainAttempts=1 (1023,1047). Standalone StartHostSession persists its ID before open (1391-1392). First-ingress still writes its envelope before assigning the ID and durably replaces the outer state only after the open (538,1042,1056-1059). Regression check: AR2-F1 retains the first-ingress identity/durability defect.

- **AR1-F7: RESOLVED**. Architecture 182,2853,3689-3690 and ReconcileProjectedDocuments enumerate workspace agent-response documents, delete orphan/duplicate/failed-save documents on a later complete pass, and preserve obligations in GraphRAG itself. Live list/ingest/delete contracts verified. Regression check: AR2-F3: the new missing-document Pending reset fails to make the next pass reingest.

- **AR1-F8: RESOLVED**. Architecture 2840 and 2843 require at least one live turn_context, matching model/effort/thread, and per-response AGREE/DISAGREE consistent with scores/findings plus aggregate verdict consistency. Mixed failing/passing ranges have explicit valid semantics. Regression check: No vacuous-context or inconsistent-verdict counterexample from round 1 remains.

- **AR1-F9: NOT RESOLVED**. Architecture 152,344,414,623 now state parent read-only and separate post-commit summary refresh, but the called pipeline activity at 438 still refreshes it inside SerializableWrite (414,1678). Regression check: AR2-F2: the shared diagram contradicts the isolation fix.

- **A4 (round 1 UNKNOWN): RESOLVED**. Section 9 and archived recipe explicitly state native sequence layout semantics. Verified all manifest hashes, source equality, independent Mermaid 12.2.0 render of 213 blocks/58 landscape sequences, max three letter pages and 8-12 pt, and page images. Regression check: No rendering-standard uncertainty remains; semantic diagram defects are separate findings.

- **R4-F8: NOT RESOLVED**. The inherited final-refusal/bridge counterexamples are fixed, but the first-ingress durable-identity portion carried through AR1-F6 still fails (1023-1047,1056-1059). Regression check: AR2-F1.

- **R3-F6: NOT RESOLVED**. The inherited Phase A diagram-fidelity chain remains unresolved through AR1-F6 first-ingress state/write-ahead ordering. Regression check: AR2-F1.

## Interpretations, evidence and limits

- Opus as Octopus is a reasonable stated reading of the release-only ruling.

- One complete activity per unit, split into parent and immediately following nested diagrams, is a reasonable stated reading.

- Catalog and work by domain is a plan organization ruling, with architecture decisions remaining authoritative.

- Review of proposed architecture, not implementation validation. No product code/build/test changes or product test runs. Counterexamples execute/document the stated design rules only.

- Full redacted command outputs are in the evidence ledger and runner stream. Commands below are the ledger snapshot at verdict generation. Compound command ExitCode is the last observed native exit code or caught PowerShell exception; startup pre-ledger reads and static rejections remain in the runner stream.

- PowerShell parser and PSScriptAnalyzer 1.25.0 checked written scripts before execution. Exploratory parser/analyzer failures and wrong-path/schema/browser-client attempts were corrected; all evidence relied upon has a successful check/readback.

- The first full marker read exposed the marker key in this own runner stream. Three occurrences were subsequently redacted in place with the required marker and padding, preserving the append offset; the value is not repeated in these receipts.

- Full diff whitespace check reports trailing blank lines in round-1 review receipts only; scoped reviewed-document diff --check passes. Those style warnings are not scored.

- MAINT-LIBSHRETIRE-001 is not yet a live main-workspace TODO; it is a proposed retirement item in the plan, with creation/execution deferred under the unapproved plan. Its absence is not evidence of a missing implemented feature.

- Git objects and update-by-push reflog prove actual fast-forward ancestry, not the historical absence of a particular command-line flag.

- Potential alias return-shape concern was rejected: delivery entries carry pendingId, and the defined Get-McpAlias supplies the full permanent lookup record. No operator decision is required for any finding.

The companion JSON contains native structured findings, every claim, live identity proof, startup confirmations and the command/exit-code ledger snapshot. Full outputs and final persistence operations remain in the evidence JSONL and runner streams. The complete JSON result is stored in ordered dialog parts on the reviewer turn. 20261013T174659Z-persistence-proof.json records exact server readback equality and the completion result.

Signed: Codex / gpt-6-astra / max, independent hostile validator.

Persistence note: the live secret scrubber changed two harmless key-name regex expressions in the initial command snapshot. Those expressions were redacted in the final JSON, and the entire corrected result was appended with a distinct V2 prefix for exact server readback. Findings and verdict are unchanged.
