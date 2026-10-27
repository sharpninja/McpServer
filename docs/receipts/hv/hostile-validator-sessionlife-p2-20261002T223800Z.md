Independent hostile validation: SessionLife P2

- TimestampUtc: 2026-10-02T23:00:09.4180433+00:00
- ValidatorIdentity: Codex/gpt-6-astra/xhigh
- WorkClass: project implementation, P2 SessionLife contracts
- tipSha: 6e3215ffeab727c345739fb452aeaa0abdc416ab
- RunId: p2-unit-legion-20261002T222414Z
- add-profile: executed first; all 21 non-skill profile Markdown files read in full.
- OverallVerdict: **DISAGREE**. Accuracy 99; completeness 77.5; confidence 99. Claims: 15 PASS / 4 FAIL / 2 UNKNOWN.

The recorded cumulative unit gate and fresh HV19/HV20 identity probes pass. Mapping commit c7ed6234 adds real executed names, but several assertions do not cover their mapped criteria. No runtime product regression was established by this review; the blocking findings are incomplete acceptance evidence.

Completeness is 31/40 = 77.5 percent: 35 core plus eight governing legacy minus three wholly deferred integration criteria. Eight of the prior 12 failures are repaired, four remain, and five prior positive credits are revoked after inspecting their actual assertions. B7 and B8 remain nonblocking UNKNOWN under the explicit operator instruction. Neither reduces the completeness numerator or causes DISAGREE.

**FAIL list**
- A1 / HV07: Prior traceability failures are only partly repaired; four remain, and five previously credited hook/cache criteria fail re-attack.
- A6 / HV09: Concrete P2 completeness is 77.5 percent (31/40), below 98.
- C2 / HV07: Nine criteria lack semantically sufficient executed mappings: FR-MCP-SESSIONLIFE-001-AC002; TR-MCP-SESSIONLIFE-001-AC001/002; TEST-MCP-SESSIONLIFE-001-AC001/002; FR-MCP-SESSIONLIFE-002-AC002; TR-MCP-SESSIONLIFE-002-AC003; TEST-MCP-SESSIONLIFE-002-AC001; AC-FR-MCP-SESSIONLOGCTX-001-003.
- D1: Holistic P2 acceptance remains blocked by those coverage gaps and sub-98 completeness. B7/B8 are not the cause.

**All nine deficient criteria**
- **FR-MCP-SESSIONLIFE-001-AC002**: Fresh prompt-hook opened versus opened-degraded status is not asserted by any mapped test. Three mappings drive repl-invoke wrappers, not the prompt hook. The only mapped hook test, SessionLogP2Contracts.Tests.ps1:835-900, seeds an already-open degraded turn and asserts turn-already-open, recoveryArtifactPath and keep-. It never asserts the first-open status or its primary/degraded alternatives. This previously credited row is not fully covered.
- **FR-MCP-SESSIONLIFE-002-AC002**: New mapped raw tests are executed, but the rejection matrix remains incomplete: no mapped service case supplies valid planFile with omitted/null/empty todoId and proves no inserted row; no mapped raw empty-string planFile case exists. ValidateForNewEntry_OmittedTodoIdEmpty actually supplies two spaces, not omitted or empty input (SessionLogTurnContextValidatorTests.cs:40-42). SubmitAsync_NewTurnMissingFields_Throws sets BOTH fields null, so plan validation can short-circuit, and it asserts no row count. UpsertTurnAsync_NewTurnWithoutPlanFile_ThrowsAndDoesNotInsert covers missing planFile only (SessionLogServiceTurnContextTests.cs:43-62). The Pester null/empty test covers planFile at beginTurn, not raw service/no-row behavior. Fresh validator probes do not repair gated criterion mappings.
- **TR-MCP-SESSIONLIFE-001-AC001**: Mapped helper/wrapper tests prove retained cache and typed outcomes, and the duplicate hook test proves an artifact path. They do not prove the shared truthful first-open transition is consumed by the hooks. The missing fresh prompt-hook status assertion in FR-MCP-SESSIONLIFE-001-AC002 remains part of this aggregate technical criterion.
- **TR-MCP-SESSIONLIFE-001-AC002**: The three mapped cases invoke Invoke-ReplTurnUpsertParams and New-McpPluginTurnUpsertRequest for complete/update query omission. They do not invoke the prompt or dialog hook consumers or assert their transition/status behavior. Builder coverage cannot be credited as all named hook-consumer coverage.
- **TR-MCP-SESSIONLIFE-002-AC003**: The aggregate same-resolver criterion retains the unproved raw first-persist cases in FR-MCP-SESSIONLIFE-002-AC002. Executed validator examples plus wrapper/document examples do not prove the raw and wrapper contracts agree for both metadata fields and all stated missing-value forms.
- **TEST-MCP-SESSIONLIFE-001-AC001**: The mapped tests cover cached metadata, rejected persistence, one-attempt dialog recovery, and a duplicate hook artifact path. None proves fresh truthful hook opened/opened-degraded status; the named empty-query builder tests are also absent from this criterion-specific list. This compound consumer-test criterion is incomplete.
- **TEST-MCP-SESSIONLIFE-001-AC002**: This row maps only all-verb outcomes, empty-query builder/shim, and rejected-envelope cases. They exercise real code with the shortcut unset, but do not assert the complete cached object together with the serialized persistence payload. All-verb tests assert selected flags/identity/file state; the empty-query case asserts field omission; the rejected-envelope case asserts throw/no-clear/queue count. The separate complete-cache preservation case is not mapped here. The stricter test-assertion requirement is not satisfied by name overlap.
- **TEST-MCP-SESSIONLIFE-002-AC001**: The added C# tests do not cure the named-Pester acceptance criterion: the mapped Pester rejection cases exercise planFile whitespace/null/empty, but not the corresponding todoId cases. The newly mapped raw tests also have the first-persist coverage gap described in FR-MCP-SESSIONLIFE-002-AC002. Supplementary raw probes are not mapped gated Pester cases.
- **AC-FR-MCP-SESSIONLOGCTX-001-003**: The seven added names cover representative raw rejection, None validation, one missing-plan no-insert check, and normal None persistence. They do not exercise canceled/cancelled supersession or durable reopen at all, although both are explicit in this criterion. Missing-todo/no-insert and raw empty cases also remain uncovered. Normal None/None persistence is not a canceled-supersession test.

**UNKNOWN list**
- B7: historical Python JSON provenance unestablished, nonblocking.
- B8: original historical P2 Red AGREE unestablished, nonblocking.

**Live model/effort proof**
Live session file C:\Users\kingd\.codex\sessions\2026\10\02\rollout-2026-10-02T17-36-54-01a0fec3-64e6-78e1-83cc-54ff10e67563.jsonl: line 1 session_meta identifies this workspace, codex_exec, CLI 0.159.3; line 8 turn_context identifies turn 01a0fec3-6991-7841-8883-a59fce615689, model gpt-6-astra, effort xhigh. Exact metadata is retained in the JSON twin/mapping audit. A different auto-review child was not substituted as identity proof.

**Surface A**

- **A1 FAIL**: The prior traceability/completeness failures are fully closed.
  Eight of the prior 12 deficient criteria now have sufficient executed semantic mappings: TEST-MCP-SESSIONLIFE-002-AC004 and seven governing legacy rows. Three core raw-metadata aggregate rows and AC-FR-MCP-SESSIONLOGCTX-001-003 remain incomplete. Five previously credited hook/complete-cache criteria also fail independent re-attack. See all 43 CriterionDispositions; no prior verdict was copied as evidence.

- **A2 PASS**: HV19 and HV20 still enforce Ordinal persistence identity and reopen binding.
  Fresh remaining-identity-probes-v2 gives 18/18 assertions: four exact successes; eight case-only response and four wrong-request rejections retaining recovery; two case-only begin controls produce zero transport calls and unchanged cache hashes. Fresh boundary probes give nine of nine durable/degraded/unpersisted exact/case-request/case-session cases passing.
  Ten raw-validator and eight durability-boolean probes pass. These invoke current production workflow/builder functions, with only immediate transport doubled; they are not remote durable proof. repl-invoke.ps1 is unchanged by c7ed6234 and 6e3215ff; Ordinal comparisons remain at :1765-1766 and :2184-2190.

- **A3 PASS**: The recorded fresh cumulative unit gate is accepted at the requested product tip.
  Independent XML parsing: Pester 242/242 Success, zero Failed/Skipped/Inconclusive/NotRun; Nuke seven projects total 4298/4298 Passed; Support.Mcp 2914, Client 304, Repl.Core 868, McpAgent 63, QBAgent 96, Cqrs 33, Launcher 20. Build.Tests 321/321, outcome Completed. Every individual result is passing. Recorded command exits are zero.
  Gate log :5366 accepts p2-unit-legion-20261002T222414Z, :5371 CheckSessionLifeUnitGate Succeeded, :5379 GATE_EXIT=0. SHA256 94222BBF9A11DA3D766B9A2BEC566F0CD52B37CD1B1D2598DF726A6027D21937 matches.
  2237 extant source entries match their captured hashes. The sole missing entry among 2238 is the disclosed temporary build/Directory.Build.targets. Direct current compiled validator rejects deleted-included-source for that path, as anticipated by the operator. That post-run deletion is not scored as a failed recorded gate. No source/report restoration, gate overwrite, or full-suite rerun occurred.

- **A4 PASS**: 6e3215ff isolates SQLite memory schema readiness without relaxing pending migration checks.
  Commit changes only SessionLogSchemaGuard.cs and SessionLogSchemaGuardTests.cs. The :memory: branch calls Probe on every invocation and throws SessionLogSchemaPendingMigrationException on false; file-backed connection-string cache is retained. Required column list and provider probes are unchanged. EF InMemory-provider exemption predates this patch.
  The new test creates a ready SQLite :memory: connection, invokes Ensure, then creates a distinct same-string missing-column SQLite connection and asserts the named pending-migration exception. Its exact full name is Passed in the gated Support.Mcp TRX. No weaker ready result is returned for that second database.

- **A5 PASS**: Unit inventory, frozen history, and acceptance boundaries are preserved.
  selected-projects.json names exactly seven unit projects and excludes PluginIntegration. Build.Test.cs exclusion and native report set agree. history-r1 manifest SHA256 B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4 and all five governed file hashes match.
  All 35 manifest TODO rows remain not-accepted. MCP todo_list on authoritative F:\GitHub\McpServer independently returned all 35 exact IDs with Done=false and zero missing IDs. No TODO/goal/requirement state was changed.

- **A6 FAIL**: Concrete P2 completeness is at least 98 percent.
  31/40 applicable criteria = 77.5 percent. Denominator is 35 core + 8 governing legacy - 3 deferred integration criteria. No rounding of 39/40 to 98, no suite-total credit, no proposed-label credit. Nine deficient criteria receive zero credit. B7/B8 do not affect this arithmetic.

- **A7 PASS**: c7ed6234 has the stated file scope and its added names really executed, without fabricated acceptance flags.
  git diff c7ed6234^ c7ed6234 --name-only lists only SessionLogLifecycle.Tests.ps1 and acceptance-manifest.json. Mapping audit finds 62 added criterion-specific links across the reviewed rows; every added and existing mapped name has an exact passing native match.
  All 43 reviewed criterion texts, acceptanceState values and storeSatisfied values are unchanged from the mapping commit parent. Newly strengthened never-degraded Pester assertions check retryable false, turn not found and auditDialog=0. Execution/name accuracy passes; full semantic sufficiency is separately failed in A1/C2.

**Surface B**

- **B1 PASS**: The required profile and actual Astra/xhigh identity are proven.
  First tool action read C:\Users\kingd\.claude\skills\add-profile\SKILL.md; all 21 non-skill profile Markdown files were read in full, recovering the middle truncated output by full reread. Hostile-validator SKILL.md was then read and surfaces A-D applied.
  Live rollout 01a0fec3-64e6-78e1-83cc-54ff10e67563: line 1 session_meta, codex_exec CLI 0.159.3, this workspace; line 8 turn_context, turn 01a0fec3-6991-7841-8883-a59fce615689, model gpt-6-astra, effort xhigh. The more recently modified codex-auto-review child is explicitly not used as validator identity.

- **B2 PASS**: This pass respects the operator shell, review-only and single-round overrides.
  Evidence used PowerShell only: initial/auxiliary exec pwsh 7.5.4 and PowerShell.Mcp pwsh 7.6.6 on PAYTON-LEGION2. No Python, product edits, remediation, commit, push, merge, deployment, goal/TODO completion, or second validator round. Authored writes are confined to docs/receipts/hv.
  Automatic oversized-output spill files were created by PowerShell.Mcp under C:\Users\kingd\AppData\Local\Temp\PowerShell.MCP.Output. These host diagnostics are disclosed; zero incidental outside writes is not claimed. Probe YAML uses the project object-mutation helper.

- **B3 PASS**: Current source/PR/count claims are evidence-based and limits are disclosed.
  Git HEAD is 6e3215ffeab727c345739fb452aeaa0abdc416ab on cursor/sessionlife-p2-contracts-5cb2. Authenticated gh pr view through PowerShell.Mcp confirms PR72 OPEN, draft, unmerged, same head, base develop. Public web fetch failed and sandbox gh returned 401; neither was reported as successful verification.
  Local artifact counts/hashes were parsed independently. The temporary build-target deletion is distinguished from product changes. One progress timestamp was mistakenly written 22:55Z; a subsequent clock read was 22:52:26Z and the error was explicitly corrected. Receipt timestamp is generated from the live clock.

- **B4 PASS**: Historical and unrelated workspace artifacts remain intact.
  All five history-r1 hashes match. git diff --name-only still reports only pre-existing .nuke/build.schema.json and two benchmark-result JSON modifications outside the receipt area. No old evidence was deleted or normalized. No reset/staging/cleanup occurred.

- **B5 PASS**: Requirements and TODO observations use the authoritative MCP interface and current waiver.
  Native requirements_list, todo_list and memory_recall targeted F:\GitHub\McpServer, the acceptance manifest owner; no raw MCP HTTP or direct durable-store edit. All 35 scoped TODOs are open. Core requirements remain pending/unsatisfied; legacy REPL-009 existing satisfied state was preserved, not created by this pass.
  MCP session-log persistence is explicitly waived for this review. Two optional large snapshot-write tool calls returned user cancelled and were not represented as saved; the successful live observations and all scoped IDs are retained in this receipt/public evidence checkpoint.

- **B6 PASS**: The three governing metadata contract documents agree and are parseable.
  Fresh trace audit: module-bootstrap has 15 JSON/YAML fences, REPL guide 33, no parse errors; all five beginTurn examples have nonempty metadata pairs. Message schema parses. module-bootstrap:68, REPL-USER-GUIDE:127 and schema:251 agree on explicit/cache/None, ordinary raw rejection, canceled/cancelled and durable omission.
  Document behavior is assessed independently of the remaining test-link deficiencies. No document or requirement projection was edited.

- **B7 UNKNOWN**: Historical Python JSON provenance is authoritative.
  p2-contracts-verdict.json:53 retains the historical Python json module serialization declaration. The original producing execution has not been established. Neither compliant historical execution nor an actual prohibited invocation is invented from that string. Per operator override this UNKNOWN is not a FAIL and does not drive DISAGREE.

- **B8 UNKNOWN**: An original historical P2 Red-test AGREE is established.
  p2-contracts-plan.md:9 references P1 and expressly does not claim P2 HV AGREE. Historical p2-contracts-verdict.json remains phaseComplete=false. The inspected BUG-UNITFAIL-001 corrected red-gate AGREE is narrowly that later repair, not original P2. No authoritative original P2 Red AGREE was found. No chronology is inferred from timestamps. Per override this UNKNOWN is not a FAIL and does not drive DISAGREE.

- **B9 PASS**: Durable review receipts contain the complete verdict and transparent stream boundary.
  Markdown/JSON twins contain all claim evidence, nine criterion failures, scores, exact source/run, model proof and every criterion disposition. Existing request seed prompt SHA256 3CE5189933DC80E9AA2EE9C1C783FD8461922538E265FE4FFD8AD2C83CF9B3C7 matches its source; a full-body JSONL request record was appended.
  A public-event checkpoint plus full verdict is retained before final return. The runner owns final native response JSONL capture and can replace the checkpoint with its full stream after return. Later runner bytes cannot be inspected before return and are not claimed verified. MCP review persistence is waived.

**Surface C**

- **C1 PASS**: Applicable structured FR/TR/TEST/AC and mapping obligations are identified.
  Manifest: 35 core criteria, eight governing legacy criteria, 35 unique TODOs, 47 frozen mapping edges. Native MCP requirements_list confirms structured core and governing legacy AC; this pass checked their actual text. Newer dense-session FR003-AC007 and TR003-AC004 are later storage/provider scope, not denominator additions or P2 credit.
  Three wholly later integration criteria are excluded, as explicitly directed. Mixed criteria receive only the identified P2 local portion; deadline, Stop, security replay and provider obligations remain P3/P4/P6. No full broad requirement satisfaction is inferred.

- **C2 FAIL**: Every applicable criterion has an executed mapping that semantically covers its P2 text.
  Name matching succeeds, but nine criteria do not satisfy their complete P2 test obligations. Four remaining original failures concern raw metadata and the legacy supersession/reopen mapping. Five newly rejected prior credits concern first-open hook status, hook consumers, and complete-cache assertions. Detailed per-ID reasons and exact mapped names are in CriterionDispositions.
  A passing test for one field, a normal None persistence, a duplicate-turn hook result, or a builder field-omission check is not interchangeable with missing-todo/no-row, canceled supersession, first-open hook status, or complete cache+payload assertions.

- **C3 PASS**: Relevant real implementation and gate assertions exist and pass, with stated boundaries.
  The mapped raw/service, sentinel, incremental-dialog, filesystem recovery, dispatcher, gate-validator and gate-consumer tests are genuinely executed. New schema test is Passed. The gate-validator tests call actual validation against malformed reports and assert classified failure. Fresh production-function probes pass.
  These observations establish tested behavior only. Current raw-validator probes do not supply missing gated-service/no-row assertions or retroactively modify criterion-specific mappings; no code defect is inferred solely from a coverage gap.

**Surface D**

- **D1 FAIL**: P2 holistically meets its acceptance definition of done.
  The current full unit gate, schema-cache fix, and Ordinal identity proofs pass. P2 acceptance still requires the complete mapped metadata/cache/hook/outcome contract (plan P2:253-260 and Section7). Nine applicable criteria lack sufficient evidence, so completeness is 77.5 below 98. This independently blocks acceptance even with B7/B8 treated as nonblocking UNKNOWN.

- **D2 PASS**: Later-phase and whole-batch duties remain open without false P2 integration claims.
  P2 explicitly defers durable query. Stop/deadline is P3, replay/security/storage is P4, plugin synchronization is P5, provider/integration/durable readback is P6, integration/deployment/individual closure is P7. PluginIntegration is not a unit inventory member.
  Review-only scope leaves all 35 TODOs open/not-accepted and acceptanceState/storeSatisfied unchanged. This receipt does not authorize merge, closure, deployment, or claim the entire batch is complete.

**Concrete criterion dispositions**

- **FR-MCP-SESSIONLIFE-001-AC001 P2_PASS**: Mapped real helper and process cases retain query, title, metadata, timestamps and audit counters across degraded creation/retry; inspected production retains the original turn object. This covers cache-preservation behavior, distinct from the stricter complete-object test-assertion requirement below.

- **FR-MCP-SESSIONLIFE-001-AC002 P2_FAIL_TRACEABILITY**: Fresh prompt-hook opened versus opened-degraded status is not asserted by any mapped test. Three mappings drive repl-invoke wrappers, not the prompt hook. The only mapped hook test, SessionLogP2Contracts.Tests.ps1:835-900, seeds an already-open degraded turn and asserts turn-already-open, recoveryArtifactPath and keep-. It never asserts the first-open status or its primary/degraded alternatives. This previously credited row is not fully covered.

- **FR-MCP-SESSIONLIFE-001-AC003 P2_PASS**: Lifecycle builder tests select cached query, then title, then placeholder; assert queryTitle remains omitted and empty update query is omitted. Real shim test corroborates omission.

- **FR-MCP-SESSIONLIFE-001-AC004 P2_PASS**: Lifecycle test asserts exactly one recovery attempt and one dialog envelope for degraded 404; its never-degraded branch now asserts zero attempts/artifacts, retryable false and unchanged auditDialog.

- **FR-MCP-SESSIONLIFE-001-AC005 P2_PASS**: Mapped retry tests send cached creation metadata, supersede uses canceled, and the real builder test normalizes cancelled to canceled. Queued cache is not promoted to durable without a primary result.

- **FR-MCP-SESSIONLIFE-002-AC001 P2_PASS**: Mapped first-create/cache-binding, explicit-update and None cases exercise local resolver precedence. Actual stored provider durability remains later scope.

- **FR-MCP-SESSIONLIFE-002-AC002 P2_FAIL_TRACEABILITY**: New mapped raw tests are executed, but the rejection matrix remains incomplete: no mapped service case supplies valid planFile with omitted/null/empty todoId and proves no inserted row; no mapped raw empty-string planFile case exists. ValidateForNewEntry_OmittedTodoIdEmpty actually supplies two spaces, not omitted or empty input (SessionLogTurnContextValidatorTests.cs:40-42). SubmitAsync_NewTurnMissingFields_Throws sets BOTH fields null, so plan validation can short-circuit, and it asserts no row count. UpsertTurnAsync_NewTurnWithoutPlanFile_ThrowsAndDoesNotInsert covers missing planFile only (SessionLogServiceTurnContextTests.cs:43-62). The Pester null/empty test covers planFile at beginTurn, not raw service/no-row behavior. Fresh validator probes do not repair gated criterion mappings.

- **FR-MCP-SESSIONLIFE-002-AC003 P2_PASS**: Mapped HV10/HV11 and cached metadata cases cover explicit cache/outgoing updates and durable omission; independent current-code identity controls corroborate exact binding.

- **FR-MCP-SESSIONLIFE-002-AC004 P2_PASS**: Mapped request/workspace/inherited-agent/native-transition and HV12/13/16/17/19/20 cases exercise identity rejection. Fresh 18-case and nine-case probes independently preserve exact session/request binding.

- **FR-MCP-SESSIONLIFE-002-AC005 P2_PASS**: Mapped document test parses examples and schema and checks the ordinary/supersession/reopen contract. Independent parse found 48 valid fences and five begin examples with nonempty metadata pairs.

- **FR-MCP-SESSIONLIFE-003-AC001 P2_PASS**: Mapped process test runs eight verbs across primary, queued and lost with exit-code, typed receipt and retained-file assertions; identity-response rejection cases supplement it.

- **FR-MCP-SESSIONLIFE-003-AC002 P2_PASS**: Mapped Assert-P2Receipt checks required field presence/method/request/code/message; process cases check durability/queue/retryability and lost diagnostic stderr.

- **FR-MCP-SESSIONLIFE-003-AC003 P2_PASS**: Mapped caller mismatch cases assert failure and unchanged turn/server capture; case-sensitive response and reopen controls pass fresh probes.

- **FR-MCP-SESSIONLIFE-003-AC004 DEFERRED_P4_P6**: Wholly later integration/provider/exact durable-readback criterion. P2 item 4 explicitly defers durable query; excluded from the fixed denominator, with no satisfaction change.

- **FR-MCP-SESSIONLIFE-003-AC005 P2_PASS**: Mapped duplicate-update case asserts one queued record on repeat and two distinct additive methods, then one replay of each and unchanged second-drain capture. Credit is P2 local dedupe only, not provider child-row proof.

- **FR-MCP-SESSIONLIFE-003-AC006 P2_PASS**: Mapped rejection and all-verb cases retain recovery unless primary confirmation passes and distinguish queued from persisted. No TODO acceptance is inferred.

- **TR-MCP-SESSIONLIFE-001-AC001 P2_FAIL_TRACEABILITY**: Mapped helper/wrapper tests prove retained cache and typed outcomes, and the duplicate hook test proves an artifact path. They do not prove the shared truthful first-open transition is consumed by the hooks. The missing fresh prompt-hook status assertion in FR-MCP-SESSIONLIFE-001-AC002 remains part of this aggregate technical criterion.

- **TR-MCP-SESSIONLIFE-001-AC002 P2_FAIL_TRACEABILITY**: The three mapped cases invoke Invoke-ReplTurnUpsertParams and New-McpPluginTurnUpsertRequest for complete/update query omission. They do not invoke the prompt or dialog hook consumers or assert their transition/status behavior. Builder coverage cannot be credited as all named hook-consumer coverage.

- **TR-MCP-SESSIONLIFE-001-AC003 P2_PASS**: The mapped lifecycle 404 test drives the real dialog workflow with immediate boundary doubles; one recovery attempt and distinct never-degraded nonretryable behavior are asserted.

- **TR-MCP-SESSIONLIFE-002-AC001 P2_PASS**: Mapped resolver inputs and durable omission/explicit update cases exercise the shared production resolver, with real payload cases supplementing shortcut consumer cases.

- **TR-MCP-SESSIONLIFE-002-AC002 P2_PASS**: Mapped workspace/request/session/agent isolation cases and fresh Ordinal probes establish the P2 binding contract.

- **TR-MCP-SESSIONLIFE-002-AC003 P2_FAIL_TRACEABILITY**: The aggregate same-resolver criterion retains the unproved raw first-persist cases in FR-MCP-SESSIONLIFE-002-AC002. Executed validator examples plus wrapper/document examples do not prove the raw and wrapper contracts agree for both metadata fields and all stated missing-value forms.

- **TR-MCP-SESSIONLIFE-003-AC001 P2_PASS**: All-verb subprocess assertions parse one result document and reject bare booleans, while failure paths remain classified; typed identity controls reject false primary results.

- **TR-MCP-SESSIONLIFE-003-AC002 P2_PASS**: Mapped receipt/serialization and typed-identity tests assert P2 result fields. Full server durability is not claimed.

- **TR-MCP-SESSIONLIFE-003-AC003 P2_PASS**: P2 credit is only the mapped dedupe, sequential method-label and queue-count portion. Single deadline and deterministic process cleanup belong to explicit P3 in the governing plan; this is not full-criterion store satisfaction.

- **TEST-MCP-SESSIONLIFE-001-AC001 P2_FAIL_TRACEABILITY**: The mapped tests cover cached metadata, rejected persistence, one-attempt dialog recovery, and a duplicate hook artifact path. None proves fresh truthful hook opened/opened-degraded status; the named empty-query builder tests are also absent from this criterion-specific list. This compound consumer-test criterion is incomplete.

- **TEST-MCP-SESSIONLIFE-001-AC002 P2_FAIL_TRACEABILITY**: This row maps only all-verb outcomes, empty-query builder/shim, and rejected-envelope cases. They exercise real code with the shortcut unset, but do not assert the complete cached object together with the serialized persistence payload. All-verb tests assert selected flags/identity/file state; the empty-query case asserts field omission; the rejected-envelope case asserts throw/no-clear/queue count. The separate complete-cache preservation case is not mapped here. The stricter test-assertion requirement is not satisfied by name overlap.

- **TEST-MCP-SESSIONLIFE-001-AC003 P2_PASS**: Concrete names are linked, every mapped name appears passing, and independent current native report parsing proves the cumulative unit counts with zero non-passing results. Actual artifact paths are recorded by this audit.

- **TEST-MCP-SESSIONLIFE-002-AC001 P2_FAIL_TRACEABILITY**: The added C# tests do not cure the named-Pester acceptance criterion: the mapped Pester rejection cases exercise planFile whitespace/null/empty, but not the corresponding todoId cases. The newly mapped raw tests also have the first-persist coverage gap described in FR-MCP-SESSIONLIFE-002-AC002. Supplementary raw probes are not mapped gated Pester cases.

- **TEST-MCP-SESSIONLIFE-002-AC002 P2_PASS**: P2 credit covers process exit, typed outcomes and dialog classification. Deadline/Stop/quarantine/audit obligations are explicitly P3/P4 and remain unaccepted.

- **TEST-MCP-SESSIONLIFE-002-AC003 P2_PASS**: Mapped consumer cases have real process/builder counterparts, serialized receipts or exits; no candidate is marked accepted. This does not substitute for the missing coverage in the more specific hook/cache criteria.

- **TEST-MCP-SESSIONLIFE-002-AC004 P2_PASS**: New mapping names exact passed Build.Tests invalid-artifact and required-command theory cases plus both consumer tests. Source invokes real Validator.Validate and classified rejection, not source-string presence. Native gate has no missing, skipped, failed or zero-discovery report.

- **TEST-MCP-SESSIONLIFE-003-AC001 DEFERRED_P4_P6**: Wholly later integration/provider/exact durable-readback criterion. P2 item 4 explicitly defers durable query; excluded from the fixed denominator, with no satisfaction change.

- **TEST-MCP-SESSIONLIFE-003-AC002 DEFERRED_P4_P6**: Wholly later integration/provider/exact durable-readback criterion. P2 item 4 explicitly defers durable query; excluded from the fixed denominator, with no satisfaction change.

- **TEST-MCP-SESSIONLIFE-003-AC003 P2_PASS**: FR173 subset linkage remains in the mapping freeze/live mapping; P2 wrapper proof does not close broader adapter/provider criteria or the batch.

- **FR-MCP-170-AC002 P2_PASS**: PluginPowerShellRuntime.Tests.ps1:4960 drives Invoke-WorkflowAppendDialog and asserts AppendDialogAsync present and SubmitAsync absent. Freshness is doubled, but the incremental routing under review is real.

- **FR-MCP-170-AC003 P2_PASS**: Controller test proves not_found/retryable=false; strengthened lifecycle never-degraded branch proves no failsafe and unchanged auditDialog alongside the separate degraded branch.

- **AC-FR-MCP-SESSIONLOGCTX-001-002 P2_PASS**: BothNone and lowercase-none validator cases, actual service stored None/None, and wrapper default/clear cases establish the exact sentinel in P2 scope.

- **AC-FR-MCP-SESSIONLOGCTX-001-003 P2_FAIL_TRACEABILITY**: The seven added names cover representative raw rejection, None validation, one missing-plan no-insert check, and normal None persistence. They do not exercise canceled/cancelled supersession or durable reopen at all, although both are explicit in this criterion. Missing-todo/no-insert and raw empty cases also remain uncovered. Normal None/None persistence is not a canceled-supersession test.

- **FR-MCP-REPL-009-AC001 P2_PASS**: Five real dispatcher/failover consumer tests cover open/begin/update/dialog/actions with primary failure and successful independent strategy; mapped PowerShell process matrix corroborates queued workflow success.

- **FR-MCP-REPL-009-AC002 P2_PASS**: Real FilesystemSessionLogPersistenceStrategy test deserializes its V4 envelope and asserts method/session/request/status, absolute retained path and no temp file; mapped drain test exercises replay order/confirmation.

- **FR-MCP-REPL-009-AC003 P2_PASS**: Dispatcher/failover test loops complete and fail, asserts degraded, filesystem-failsafe strategy and absolute path; process outcome test independently rejects primary-persisted for queued work.

- **FR-MCP-REPL-009-AC004 P2_PASS**: Coordinator test asserts primary does not invoke failsafe. Actual PowerShell persistence test asserts artifact exists before submit and zero pending files after confirmation; process matrix checks primary result.

Every criterion text and exact mapped name, matching native report/result, unchanged acceptance flag, and rationale is in the JSON twin and mapping audit. Passing mixed criteria receive only the named P2 portion, never full later-phase requirement satisfaction.

**Operator overrides and limitations**
- Use Codex Astra gpt-6-astra/xhigh instead of Grok.
- On PAYTON-LEGION2 Windows PowerShell5.1 is approved if pwsh is remapped/unavailable; prefer available pwsh. This pass used pwsh.
- MCP session-log persistence is not required for this pass; receipt JSONL plus Markdown/JSON suffice.
- Live turn_context/session_meta proof is required and verified.
- Review only, receipt writes only under docs/receipts/hv; no remediation, commit, push, merge, goal/TODO done or bulk closure.
- One Astra round; stop after receipt.
- B7/B8 may remain nonblocking UNKNOWN. AGREE requires accuracy/completeness >=98 and all applicable A/C/D claims PASS.
- No cumulative suite rerun; supplied native artifacts were reparsed and hashed.
- Direct current validator fails only on the disclosed post-run deletion; historical acceptance is supported by exact log/hash and report parsing, not a fabricated current ACCEPTED result.
- Transport doubles do not prove remote provider persistence.
- No original historical Red AGREE or Python execution provenance is invented.
- The runner owns the final native response stream after return; this pass preserves a public checkpoint and full verdict before return without claiming future bytes inspected.
- Optional MCP snapshot-write calls were canceled, not reported as persisted. Successful live query observations are recorded here.
- PowerShell.Mcp automatically spilled oversized output to its Temp diagnostic directory. Authored writes remained inside docs/receipts/hv.

**Durable evidence**
- docs/receipts/hv/20261002T223800Z-mapping-audit.ps1
- docs/receipts/hv/20261002T223800Z-mapping-audit.json
- docs/receipts/hv/20261002T223800Z-native-audit-recorded-tools.ps1
- docs/receipts/hv/20261002T223800Z-native-audit.json
- docs/receipts/hv/20261002T223800Z-remaining-identity-probes-v2.ps1
- docs/receipts/hv/20261002T223800Z-remaining-identity-probes-v2.json
- docs/receipts/hv/20261002T223800Z-boundary-probes.ps1
- docs/receipts/hv/20261002T223800Z-boundary-probes.json
- docs/receipts/hv/20261002T223800Z-trace-audit.ps1
- docs/receipts/hv/20261002T223800Z-trace-audit.json
- docs/receipts/hv/20261002T223800Z-public-checkpoint.jsonl
- docs/receipts/hv/20261002T223800Z-evidence.jsonl

Reproduction: run the receipt-only native-audit-recorded-tools script with the recorded installed pwsh; it reads existing reports and does not rerun the unit suite. The two probe scripts create isolated fixtures under this receipt directory. Do not overwrite the accepted run artifacts.

=== VERDICT JSON ===
{
  "overallVerdict": "DISAGREE",
  "accuracy": 99,
  "completeness": 77.5,
  "confidence": 99,
  "failList": [
    "A1 / HV07: Prior traceability failures are only partly repaired; four remain, and five previously credited hook/cache criteria fail re-attack.",
    "A6 / HV09: Concrete P2 completeness is 77.5 percent (31/40), below 98.",
    "C2 / HV07: Nine criteria lack semantically sufficient executed mappings: FR-MCP-SESSIONLIFE-001-AC002; TR-MCP-SESSIONLIFE-001-AC001/002; TEST-MCP-SESSIONLIFE-001-AC001/002; FR-MCP-SESSIONLIFE-002-AC002; TR-MCP-SESSIONLIFE-002-AC003; TEST-MCP-SESSIONLIFE-002-AC001; AC-FR-MCP-SESSIONLOGCTX-001-003.",
    "D1: Holistic P2 acceptance remains blocked by those coverage gaps and sub-98 completeness. B7/B8 are not the cause."
  ],
  "passCount": 15,
  "failCount": 4,
  "unknownCount": 2,
  "tipSha": "6e3215ffeab727c345739fb452aeaa0abdc416ab",
  "receiptPaths": [
    "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\hostile-validator-sessionlife-p2-20261002T223800Z.md",
    "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\hostile-validator-sessionlife-p2-20261002T223800Z.json",
    "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20261002T223800Z-sessionlife-p2-contracts-hv.request.jsonl",
    "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20261002T223800Z-sessionlife-p2-contracts-hv.response.jsonl"
  ]
}