Independent hostile validation: SessionLife P2

- TimestampUtc: 2026-10-03T00:09:35.3375192+00:00
- ValidatorIdentity: Codex/gpt-6-astra/xhigh
- Work class: project implementation, P2 SessionLife contracts
- tipSha: 07de090308b9560b2be9d4b00239ba54744f7538
- RunId: p2-unit-legion-20261002T233121Z
- add-profile: executed first; all 21 non-skill profile Markdown files read in full.
- OverallVerdict: **DISAGREE**. Accuracy 99; completeness 85 (34/40); confidence 99. Claims: 14 PASS / 5 FAIL / 2 UNKNOWN.

The recorded cumulative unit gate and fresh HV19/HV20 probes pass. Three prior raw-metadata mapping gaps are repaired. The new Pester cases really execute, but their manifest links have a double-dot naming error and four criteria still lack the required assertions. No runtime product regression was established. These are acceptance-evidence and traceability failures.

Completeness uses 35 core plus eight governing legacy minus three wholly deferred integration criteria. Strict mapped coverage is 34/40 = 85 percent. Even accepting unique-name reconciliation for the erroneous links would only reach 36/40 = 90 percent because four semantic gaps remain. Neither reaches 98; B7/B8 do not drive DISAGREE.

**FAIL list**
- A1 / HV07: Only three of nine prior deficient rows now have sufficient exact mappings; six remain uncredited.
- A6 / HV09: Completeness is 85 percent (34/40), below 98. Even resolving the name typos leaves only 90 percent (36/40).
- A7: Seventeen new links use six double-dot Pester names absent from XML; the six corresponding single-dot tests pass, but the exact mapping claim is false.
- C2 / HV07: Four semantic gaps remain in TR-MCP-SESSIONLIFE-001-AC001/002 and TEST-MCP-SESSIONLIFE-001-AC001/002; two additional criteria fail exact mapping.
- D1: Holistic P2 acceptance remains blocked by the mapping/coverage gaps and sub-98 completeness.

**Central re-attack evidence**
- acceptance-manifest.json:337,1720-1721,1774-1775,2635-2637,2694,2842-2843 and supplementary metadata rows use six names with `outcomes..`. Native XML uses `outcomes.`. Across the 43 scoped rows, 48 of 65 new links resolve exactly and 17 do not. The JSON twin lists both names for each discrepancy.
- `SessionLogP2Contracts.Tests.ps1:317` checks only queryText, queryTitle, planFile, todoId, openedAt and turnRequestId. It omits sessionId, status, context, codeEdits, lastBuildStatus and all audit counters. The fresh independent probe invokes that exact helper: a reduced six-field object passes; a wrong-query control throws. The complete-object claim is therefore disproved, regardless of test naming.
- The consumer It at :1347 exercises actual prompt/stop and update/dialog wrappers, but uses the six-field helper. Its empty-query branch at :1391 asserts a cached nonempty query and absence of an empty-string serialization, not omission of queryText. Stop receipt method/exit and status transition are not asserted. These checks cannot establish all claimed shared-transition invariants.
- The new missing-turn It at :1404 begins without current-turn.yaml and fails locally before transport. It does not drive a degraded remote 404, count one recovery submit, or assert one retained session_dialog envelope. The older correct bounded recovery test still maps other criteria; it was removed from TEST-MCP-SESSIONLIFE-001-AC001 and is not silently reattached.
- New raw service tests at SessionLogServiceTurnContextTests.cs:90-228 cover empty planFile, null/empty/whitespace todoId, both canceled/cancelled omission-to-None, and zero/new-row counts on rejection. The existing omission-preservation test at :270 executes Upsert and reads stored fields. Those exact C# names pass in the gate, repairing three raw/service/document criteria. ValidateForNewEntry_OmittedTodoIdEmpty still supplies two spaces and is counted only as whitespace.
- The fresh prompt-hook test at :1308 does assert first-open primary versus degraded status and retained artifact. Its name says readback, but its transport is a local executable; no remote readback is claimed. This correct behavior does not validate the malformed exact manifest link.

**UNKNOWN list**
- B7: original historical Python JSON provenance remains unestablished, nonblocking.
- B8: original historical P2 Red-test AGREE remains unestablished, nonblocking.

**Live model/effort proof**
Live session file C:\Users\kingd\.codex\sessions\2026\10\02\rollout-2026-10-02T18-44-59-01a0ff01-b9c4-7582-bf70-e7317c803b53.jsonl: session_meta line 1 identifies Codex execution 01a0ff01-b9c4-7582-bf70-e7317c803b53, originator codex_exec, CLI 0.159.3, this workspace and requested git tip. turn_context line 8 identifies turn 01a0ff01-bf38-7d03-9ecf-319cf4481924, model gpt-6-astra, effort xhigh. Exact turn context is retained in the JSON twin. A separate guardian session is not used as proof.

**Surface A**

- **A1 FAIL**: The prior traceability/completeness failures are closed.
  Three of the nine prior deficient rows now have sufficient exact service/document mappings. Six remain uncredited: two solely from malformed Pester links and four with independent semantic gaps as well. See CriterionDispositions. A green gate was not substituted for criterion coverage.

- **A2 PASS**: HV19 and HV20 still enforce Ordinal PersistTurn identity and beginTurn reopen binding.
  repl-invoke.ps1:1766-1767 and :2184-2191 retain Ordinal comparisons; git diff ef49be87^ 07de0903 -- plugins/core/lib-ps/repl-invoke.ps1 is empty. Fresh identity probes pass 18/18 assertions; begin boundary probes pass 9/9. Four exact successes, eight case-only response rejections, four wrong-request rejections and two zero-call byte-identical begin rejections. Raw validator 10/10 and durability booleans 8/8 also pass. Only immediate transport is doubled.

- **A3 PASS**: The recorded cumulative unit gate is accepted for the requested source tip.
  Independent XML parsing yields Pester 248/248, seven Nuke projects 4310/4310, Build.Tests 321/321, zero nonpassing results or adverse counters, every TRX outcome Completed. Command exits all zero. gate.log:5384 accepted run, :5389 CheckSessionLifeUnitGate Succeeded, :5397 GATE_EXIT=0; SHA256 355D57E0604B3A360F9151CC008A4C50D666E1C6E4AA76015FEA6CDD295C2368 matches. All 2237 extant source hashes match; only disclosed deleted build/Directory.Build.targets is missing among 2238. No rerun or product restoration was performed.

- **A4 PASS**: ef49be87 and 07de0903 have the stated bounded test/document scope and preserve product identity code.
  git show/diff confirms ef49be87 changes only SessionLogP2Contracts.Tests.ps1, SessionLogServiceTurnContextTests.cs and SessionLogTurnContextValidatorTests.cs; 07de0903 changes only acceptance-manifest.json. Their source bytes match the run manifest. Actual test bodies were read; this file-scope PASS does not accept their coverage claims.

- **A5 PASS**: Unit inventory, frozen history and acceptance boundaries are preserved.
  selected-projects.json contains exactly seven unit projects and no PluginIntegration. history-r1 manifest hash B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4 and all five archived source hashes match. Manifest has 35 not-accepted TODO rows; native MCP todo_list confirms all 35 IDs Done=false. No TODO/goal/requirements state was changed.

- **A6 FAIL**: Concrete P2 completeness is at least 98 percent.
  Strict executed-map completeness is 34/40 = 85 percent. Denominator remains 35 core + 8 governing legacy - 3 wholly deferred integration criteria. Even if all double-dot references were reconciled to their uniquely identified passing Its, four semantic gaps remain, giving only 36/40 = 90 percent. Neither score passes 98, and 39/40 would also fail.

- **A7 FAIL**: Every newly mapped name is an executed passing name in this gate.
  07de0903 adds 65 links in the 43 inspected rows: 48 resolve exactly to passing native names, 17 do not. The 17 use six unique Pester names with outcomes.. instead of the actual outcomes. XML separator. Those six Its really executed and passed; this receipt does not falsely call them unrun. The exact mapping claim is nevertheless false. Text, acceptanceState and storeSatisfied fields are unchanged.


**Surface B**

- **B1 PASS**: Mandatory profile load and live requested-model proof are satisfied.
  First tool action read the specified add-profile SKILL.md. All 21 non-skill profile Markdown files were read in full, with truncated output recovered by full rereads. Hostile-validator SKILL.md was read and A-D applied. Live session_meta line 1 and turn_context line 8 identify current Codex execution 01a0ff01-b9c4-7582-bf70-e7317c803b53, turn 01a0ff01-bf38-7d03-9ecf-319cf4481924, gpt-6-astra/xhigh, this workspace and requested git tip. No guardian/auto-review model was substituted.

- **B2 PASS**: This pass follows the operator review-only, shell and single-round overrides.
  Used pwsh 7.5.4 in the Codex runtime and PowerShell.Mcp pwsh 7.6.6 on PAYTON-LEGION2. No Python, remediation, product edits, commit, push, merge, deployment, state completion or second review round. Authored scripts, fixtures and receipts are confined to docs/receipts/hv. A WindowsApps pwsh launch from the restricted exec shell was denied, so those empty probe outputs were not treated as evidence; the probes then ran successfully through the documented PowerShell.Mcp host. Its expected negative-probe stderr is not a probe failure.

- **B3 PASS**: Source, PR and evidence statements are independently checked and qualified.
  Local HEAD/branch match the requested tip/branch. Authenticated gh pr view 72 at 2026-10-02T23:57:39Z confirms OPEN, draft, unmerged, head 07de090308b9560b2be9d4b00239ba54744f7538 and base develop. Native counts/hash were reparsed. A mistakenly future-stamped 23:50 progress message was explicitly corrected; this receipt timestamp is generated from the live clock. Gate acceptance, semantic AC coverage and remote durability are kept distinct.

- **B4 PASS**: Historical and unrelated workspace artifacts remain intact.
  All five history-r1 hashes match. The initial and final tracked dirty set outside receipts is .nuke/build.schema.json plus the two pre-existing memory benchmark result JSON files. No reset, staging, cleanup or historical rewrite occurred. PowerShell.Mcp automatically spilled one oversized read result under F:\GitHub\McpServer\.mcpServer\tmp\PowerShell.MCP.Output; that host diagnostic is disclosed, not an authored outside-scope edit.

- **B5 PASS**: Authoritative requirement/TODO observations use the MCP interface and current persistence waiver.
  Native memory_recall, requirements_list and todo_list targeted F:\GitHub\McpServer, the manifest owner. No raw MCP HTTP or direct durable-store edit. All scoped TODOs are open; scoped core criteria remain unsatisfied/pending. Legacy REPL-009 prior satisfaction is retained, not created by this review. MCP session-log persistence is explicitly waived for this Astra pass.

- **B6 PASS**: The three governing metadata contract documents agree and parse.
  Fresh trace audit parses module-bootstrap 15 JSON/YAML fences and REPL-USER-GUIDE 33 with zero errors, all five begin examples contain a nonempty pair, and repl-yaml-message.schema.json parses. Exact mapped document test also passes. Ordinary rejection, explicit/cache/None, canceled/cancelled exception and durable omission are documented consistently.

- **B7 UNKNOWN**: Authoritative historical Python JSON provenance is established.
  p2-contracts-verdict.json:53 still declares Python json serialization. No authoritative original execution was found or inferred. Per operator instruction, this UNKNOWN is not a FAIL and does not affect the completeness arithmetic or cause DISAGREE.

- **B8 UNKNOWN**: An original historical P2 Red-test AGREE is established.
  p2-contracts-plan references P1 and does not claim P2 AGREE; historical p2-contracts-verdict.json retains phaseComplete=false. No original P2 Red AGREE was established. No phase chronology is fabricated from file times. This UNKNOWN is nonblocking under the operator override.

- **B9 PASS**: Durable review receipts preserve the complete verdict and disclose the runner stream boundary.
  Markdown/JSON twins hold every claim, score, failure, criterion disposition, model proof and exact source/run. The request seed hash 062DB6DD7A00792023B6CFCE9641146141C425AFD4F3ACCAFDB227FB3C4CF2E5 matches its full prompt, which was appended as a request record. A public-event checkpoint and complete verdict are saved before return. The runner owns the final response JSONL after return; future bytes are not claimed inspected. Full MCP persistence is waived.


**Surface C**

- **C1 PASS**: Applicable structured FR/TR/TEST/AC and governing mappings are identified.
  Manifest and native MCP requirements_list identify 35 core and eight governing legacy criteria for this fixed P2 denominator. Structured text and current mapping were checked. Later dense-session FR003-AC007/TR003-AC004 are storage/provider scope, not denominator additions. Three wholly later integration criteria are excluded; mixed criteria retain only their explicit P2 local portion.

- **C2 FAIL**: Every applicable criterion has a semantically sufficient executed mapping.
  Six rows lack sufficient exact executed mappings: FR-MCP-SESSIONLIFE-001-AC002; TR-MCP-SESSIONLIFE-001-AC001/002; TEST-MCP-SESSIONLIFE-001-AC001/002; TEST-MCP-SESSIONLIFE-002-AC001. Four additionally lack sufficient assertions even under unique-name reconciliation. Complete-object loss and bounded degraded dialog recovery cannot be proven by a six-field helper, an absent-local-cache rejection or labels. See every CriterionDisposition and the independent helper counterexample.

- **C3 PASS**: The credited unit behaviors have real executed implementation assertions within stated boundaries.
  Read raw metadata service/validator, real builder/process, lifecycle recovery, coordinator/filesystem and gate-validator/consumer tests; exact native names pass. Fresh current-code identity/boundary probes pass. Direct assertions repair raw metadata and durable reopen evidence, while fake transport/EF InMemory tests are explicitly not provider/integration durability proof.


**Surface D**

- **D1 FAIL**: P2 holistically meets its acceptance definition of done.
  Plan P2:250-260 and revision-4 Section 7 require mapped degraded preservation, omission and truthful hook/outcome contracts, complete units and independent review. Recorded units and identity pass, but six mapping/coverage rows remain uncredited and completeness is 85. Even the 90 percent semantic ceiling fails 98. B7/B8 are not the cause. P2 acceptance is therefore blocked.

- **D2 PASS**: Later-phase and whole-batch duties remain open without false integration/closure claims.
  P2 expressly defers durable query. P3 Stop/deadlines, P4 replay/security/storage, P5 synchronization, P6 providers/integration and P7 integration/deployment/individual closure remain separate. PluginIntegration is excluded from units; 35 TODOs remain not-accepted/open. This single review stops at receipts and does not authorize merge, closure or batch completion.

**Concrete criterion dispositions**

- **FR-MCP-SESSIONLIFE-001-AC001 PASS**: The mapped lifecycle helper case seeds query/title/metadata/time and nonzero audit counters and checks retention after Complete-ReplBeginTurnAfterPersist; the real process retry preserves creation metadata, openedAt and auditActions. Inspected production keeps the cached map instead of reconstructing a reduced object. This behavior credit does not imply the new Assert-P2FullCache helper asserts every field.

- **FR-MCP-SESSIONLIFE-001-AC002 FAIL**: The new fresh prompt-hook It at SessionLogP2Contracts.Tests.ps1:1308 does semantically distinguish turn-opened from turn-opened-degraded, verifies typed disposition and a retained artifact, and passes in XML. However, its sole manifest link at acceptance-manifest.json:337 contains outcomes.. while XML contains outcomes. Exact executed mapping is absent. This is a mapping-only failure; the old duplicate-turn It is not re-credited.

- **FR-MCP-SESSIONLIFE-001-AC003 PASS**: Lifecycle real-builder tests check queryText, then queryTitle, then Recovered session-log turn; queryTitle is not forced. The mapped shim/builder omission case checks Contains(queryText)=false and Contains(queryTitle)=false. Credit is the local builder contract.

- **FR-MCP-SESSIONLIFE-001-AC004 PASS**: The exact mapped lifecycle degraded-404 test at SessionLogLifecycle.Tests.ps1:165 drives a real appendDialog workflow, asserts one PersistCallCount and one session_dialog artifact; its never-degraded branch asserts zero submit attempts, zero artifacts, nonretryable text and unchanged auditDialog. The new absent-cache It is not used for this credit.

- **FR-MCP-SESSIONLIFE-001-AC005 PASS**: Mapped cached retry and primary retry cases verify creation metadata and retained state; mapped builder case normalizes cancelled to canceled. Typed primary confirmation remains necessary. P2 local behavior, not remote provider proof.

- **FR-MCP-SESSIONLIFE-002-AC001 PASS**: Mapped first-create, cache-binding, explicit update and None cases exercise the real resolver and captured outgoing requests; fresh boundary probes additionally show durability-first metadata selection. Provider readback remains later scope.

- **FR-MCP-SESSIONLIFE-002-AC002 PASS**: Repaired by exact passing C# mappings independent of the malformed new Pester links: valid-plan/null, empty and whitespace todoId reject with unchanged row count; empty planFile rejects with unchanged count; Submit invalid metadata inserts zero rows. Both canceled and cancelled omit metadata and store exact None, and ExistingTurnOmittingFields actually persists an update and reads preserved values. Validator null/empty/whitespace cases cover the common guard. Omitted DTO values are null at this service boundary. The misleading OmittedTodoIdEmpty name is credited only as whitespace, never omitted or empty.

- **FR-MCP-SESSIONLIFE-002-AC003 PASS**: Exact mapped HV10/HV11 and explicit-update cases check omission only for proven durable identity and update cache plus outgoing metadata. Real process counterparts and fresh exact/case controls corroborate the shortcut consumer tests.

- **FR-MCP-SESSIONLIFE-002-AC004 PASS**: Exact mapped workspace/marker/inherited-agent/native transition and HV12/13/16/17/19/20 tests pass. Current-code probes independently reject case-only request/session changes without rebinding.

- **FR-MCP-SESSIONLIFE-002-AC005 PASS**: Exact mapped documentation test passes. Fresh independent parse finds 48 valid JSON/YAML fences, five begin examples with nonempty metadata pairs, and valid message schema; text covers ordinary rejection, explicit/cache/None, canceled/cancelled and durable omission.

- **FR-MCP-SESSIONLIFE-003-AC001 PASS**: Real subprocess matrix executes eight verbs over primary/queued/lost, checking exits, typed outcomes and retained-file/server-capture differences. Its transport executable is a stand-in, explicitly not proof of live server durability.

- **FR-MCP-SESSIONLIFE-003-AC002 PASS**: Assert-P2Receipt verifies each required receipt field, verb/request/code/message, while the process matrix checks durability/queue/retryability and lost stderr. Identity-specific tests supplement the matrix.

- **FR-MCP-SESSIONLIFE-003-AC003 PASS**: Exact mapped mismatch tests check rejection and unchanged cache/capture; fresh begin mismatch controls yield zero transport calls and byte-identical cached turn. Same-identity local update changes are not falsely described as byte-identical.

- **FR-MCP-SESSIONLIFE-003-AC004 DEFERRED**: Wholly deferred exact remote durable readback criterion. P2 item 4 explicitly assigns durable query to later integration proof; excluded from the fixed denominator without changing satisfaction.

- **FR-MCP-SESSIONLIFE-003-AC005 PASS**: Mapped duplicate-update/additive test asserts one unchanged-update queue record, two distinct additive methods, one replay of each, then no capture change on a second drain. Only the P2 local dedupe portion is credited; durable duplicate-free child rows remain later.

- **FR-MCP-SESSIONLIFE-003-AC006 PASS**: Actual persistence/rejection tests retain a write-ahead artifact until confirmed primary disposition; primary matrix checks no pending artifact. No queue result is used as TODO acceptance.

- **TR-MCP-SESSIONLIFE-001-AC001 FAIL**: Both replacement links have the double-dot name defect. Independently of naming, the consumer It calls Assert-P2FullCache, which checks six fields only. It neither seeds nor asserts full identity/status/context/build/audit preservation. The fresh-hook case does not fill that gap. The actual helper accepts a reduced six-field object in the independent probe, with a failing wrong-query control.

- **TR-MCP-SESSIONLIFE-001-AC002 FAIL**: Both replacement links have the name defect. Even after name reconciliation, the consumer assertions cannot detect reduced-object loss beyond six fields. The empty-query branch sends a cached nonempty query and forbids an empty string; it does not prove absent queryText at the persistence boundary. Stop has no asserted new receipt method, exit, or completed/degraded status, so a stale prior receipt or early return can satisfy its checks. The absent-cache dialog case does not establish all hook transition consumption.

- **TR-MCP-SESSIONLIFE-001-AC003 PASS**: The exact mapped older lifecycle degraded-404 test has real one-submit/one-artifact counters and a separate never-degraded nonretryable branch. Those assertions are still executed in this gate; this criterion is not conflated with TEST-001-AC001 replacement mappings.

- **TR-MCP-SESSIONLIFE-002-AC001 PASS**: Mapped resolver and real payload tests cover explicit, verified cache, and exact None selection, with durable omission tested separately. Current source uses shared resolution; no new alternate product implementation appears in the two commits.

- **TR-MCP-SESSIONLIFE-002-AC002 PASS**: Exact mapped isolation tests and current Ordinal source checks cover immutable workspace/agent/session/request binding. Fresh identity and boundary probes all pass.

- **TR-MCP-SESSIONLIFE-002-AC003 PASS**: Repaired: exact raw validator/service mappings now cover both metadata fields, ordinary no-insert rejection, both spelling variants with exact None, and an executed existing-turn omission/preservation test. Existing wrapper and document cases remain exact and passing. Two malformed supplementary Pester links remain an A7 defect but are not necessary to the sufficient mapped service/document combination.

- **TR-MCP-SESSIONLIFE-003-AC001 PASS**: Exact all-verb process test rejects bare boolean output and asserts one classified result envelope; malformed and mismatched identity cases cannot become primary success.

- **TR-MCP-SESSIONLIFE-003-AC002 PASS**: Real serialized receipt tests cover method, identity, durability, queue, retryability, diagnostics and artifact path. No provider durability is inferred from a fake transport response.

- **TR-MCP-SESSIONLIFE-003-AC003 PASS**: P2 credit is for exact dedupe and sequential method/queue-count tests. One operation deadline and process-tree cleanup remain explicit P3 duties, not a claim of whole-criterion satisfaction.

- **TEST-MCP-SESSIONLIFE-001-AC001 FAIL**: All three replacement links have the name defect. Independently, the new missing-turn It at :1404 starts without current-turn.yaml, receives lost before transport, and asserts no resubmit count or session_dialog artifact. It is not the degraded remote-404 recovery scenario. The correctly bounded older lifecycle case was removed from this criterion-specific list. Six-field cache checks and cached-query resubmission also fail the full-object and omission assertions required here.

- **TEST-MCP-SESSIONLIFE-001-AC002 FAIL**: Its only replacement link has the name defect. Independently, :1417 invokes a primary begin, calls the six-field Assert-P2FullCache and checks five payload fields. It never asserts sessionId, status, context, build state or audit counters as a complete cached object. MCP_PLUGIN_PERSIST_LOG really is unset, but that alone does not satisfy the explicit complete-object assertion requirement. Independent execution proves the helper accepts a reduced object.

- **TEST-MCP-SESSIONLIFE-001-AC003 PASS**: The cumulative native gate has zero nonpassing results and concrete passing scope names/report paths. This gate-recording criterion is credited for its exact existing links; it does not certify the erroneous new per-criterion links, separately failed under A7/C2.

- **TEST-MCP-SESSIONLIFE-002-AC001 FAIL**: The new todoId-rejection and canceled/cancelled Pester Its actually pass under single-dot names, but both appended names in this criterion-specific list use double dots. Existing exact Pester names still omit the new todoId input matrix. C# tests repair the raw-service criteria but do not supply the named-Pester mappings this criterion explicitly requires. Name reconciliation would repair this row for P2; no silent manifest correction is credited.

- **TEST-MCP-SESSIONLIFE-002-AC002 PASS**: Exact mapped process cases prove P2 wrapper exits, serialization and dialog classification. Deadline/Stop ordering/quarantine/provider audit obligations stay in P3/P4/P6. This is partial-scope unit evidence, not whole-criterion acceptance.

- **TEST-MCP-SESSIONLIFE-002-AC003 PASS**: Existing exact consumer cases have real process/builder counterparts and recorded exits/receipts; all candidates remain not-accepted. Coverage holes of the more specific cache assertion criteria are not repaired by this aggregate observation.

- **TEST-MCP-SESSIONLIFE-002-AC004 PASS**: Mapped Build.Tests theories invoke the actual gate validator on adverse artifact/command fixtures and check classified rejection; consumer tests verify exact delegation. Fresh native parsing gives no missing, empty, failed or skipped project.

- **TEST-MCP-SESSIONLIFE-003-AC001 DEFERRED**: Wholly deferred integration/coordinator/durable collection proof; excluded from the fixed P2 denominator without granting satisfaction.

- **TEST-MCP-SESSIONLIFE-003-AC002 DEFERRED**: Wholly deferred exact durable child readback and duplicate-free additive-complete proof; excluded from the fixed P2 denominator.

- **TEST-MCP-SESSIONLIFE-003-AC003 PASS**: MCP live mapping retains FR-MCP-173 to TR-MCP-TXNKEY-001 and both TEST-MCP-221 and TEST-MCP-SESSIONLIFE-003; broader first-party adapter obligations remain pending.

- **FR-MCP-170-AC002 PASS**: Exact mapped PluginPowerShellRuntime case invokes the real appendDialog workflow and asserts AppendDialogAsync occurs while SubmitAsync does not. Freshness/transport are boundary doubles, not a real remote service claim.

- **FR-MCP-170-AC003 PASS**: Controller not_found/retryable=false and the lifecycle never-degraded branch are exact passing mappings; no failsafe and no audit increment are asserted, distinct from the degraded recovery branch.

- **AC-FR-MCP-SESSIONLOGCTX-001-002 PASS**: Exact-case None validation, lowercase-none rejection, actual service stored None/None and wrapper sentinel cases are passing mappings.

- **AC-FR-MCP-SESSIONLOGCTX-001-003 PASS**: Repaired by exact passing service mappings: null/empty/whitespace todoId with valid plan and empty plan rejection preserve row counts; Submit ordinary rejection inserts zero rows; canceled and cancelled omission each store exact None. ExistingTurnOmittingFields is real executed persistence plus readback, not a proposed label. Shared guard tests cover plan whitespace/null; omitted DTO fields deserialize to null. This is EF InMemory unit evidence, not remote/provider integration.

- **FR-MCP-REPL-009-AC001 PASS**: Five exact dispatcher/coordinator cases invoke actual ReplCommandDispatcher with failed primary and successful independent failsafe strategy for open/begin/update/dialog/actions. The process outcome matrix corroborates wrapper queued success.

- **FR-MCP-REPL-009-AC002 PASS**: Exact filesystem strategy test reads and deserializes the V4 envelope and asserts identity/status/path; exact drain test checks oldest-first replay and confirmation-before-delete. Their current report results pass.

- **FR-MCP-REPL-009-AC003 PASS**: Exact coordinator test loops complete/fail, checks degraded, filesystem-failsafe strategy and absolute path; real process matrix separately checks persisted=false for queued writes.

- **FR-MCP-REPL-009-AC004 PASS**: Exact coordinator primary test proves failsafe strategy not called; actual PowerShell boundary case checks artifact exists before submit and is removed after confirmation; primary process cases agree.

Every criterion text, exact mapped name, matching native result/report, unchanged acceptance flags, source audit and rationale is retained in the JSON twin. Proposed labels are not credited. Passing mixed criteria receive only the stated P2 portion, never broad requirement satisfaction.

**Operator overrides**
- Codex/gpt-6-astra/xhigh replaces Grok for this one pass; live proof required and verified.
- On PAYTON-LEGION2 Windows PowerShell 5.1 is approved when pwsh is unavailable/remapped; available pwsh was used.
- MCP session-log persistence is not required; receipt-only JSONL and Markdown/JSON suffice.
- Review only. Authored writes only under docs/receipts/hv. No remediation, commit/push, merge, goal/TODO done or bulk closure.
- One Astra round. Stop after the receipt; no second round or delegated reviewer.
- B7/B8 remain nonblocking UNKNOWN. AGREE requires accuracy and completeness >=98 and applicable in-scope A/C/D claims PASS.
- Judge recorded gate artifacts. The disclosed temporary build/Directory.Build.targets deletion is not a product commit or failed recorded gate. No current gate rerun was performed.

**Limits and stream ownership**
- The strict score does not silently normalize incorrect mapped names. The counterfactual semantic ceiling is disclosed so the verdict does not hinge on separator spelling.
- Six actual new Pester Its are verified executed and passing; they are not described as proposed or unrun. Four of their mapped criteria still have substantive assertion gaps.
- No full-suite rerun. This review reparsed and hashed the supplied native reports and independently ran receipt-scoped probes.
- Immediate transport doubles and EF InMemory tests do not prove durable remote/provider storage. The three excluded integration criteria remain unaccepted.
- The runner writes the final native response JSONL after return. The pre-return checkpoint excludes private reasoning and records public tool/message evidence plus the complete verdict.
- PowerShell.Mcp created an automatic oversized-output diagnostic outside the worktree; authored writes remained under the receipt directory.
- The standalone six-field assertion probe establishes a test-helper false positive, not a production bug. No runtime product regression was established.

**Durable evidence**
- docs/receipts/hv/20261002T234447Z-audit.ps1
- docs/receipts/hv/20261002T234447Z-audit.json
- docs/receipts/hv/20261002T234447Z-trace-audit.ps1
- docs/receipts/hv/20261002T234447Z-trace-audit.json
- docs/receipts/hv/20261002T234447Z-remaining-identity-probes-v2.ps1
- docs/receipts/hv/20261002T234447Z-remaining-identity-probes-v2.json
- docs/receipts/hv/20261002T234447Z-boundary-probes.ps1
- docs/receipts/hv/20261002T234447Z-boundary-probes.json
- docs/receipts/hv/20261002T234447Z-cache-assertion-probe.ps1
- docs/receipts/hv/20261002T234447Z-cache-assertion-probe.json
- docs/receipts/hv/20261002T234447Z-public-checkpoint.jsonl

Request JSONL: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20261002T234447Z-sessionlife-p2-contracts-hv.request.jsonl
Runner-owned final response JSONL: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20261002T234447Z-sessionlife-p2-contracts-hv.response.jsonl

=== VERDICT JSON ===
{
  "overallVerdict": "DISAGREE",
  "accuracy": 99,
  "completeness": 85,
  "confidence": 99,
  "failList": [
    "A1 / HV07: Only three of nine prior deficient rows now have sufficient exact mappings; six remain uncredited.",
    "A6 / HV09: Completeness is 85 percent (34/40), below 98. Even resolving the name typos leaves only 90 percent (36/40).",
    "A7: Seventeen new links use six double-dot Pester names absent from XML; the six corresponding single-dot tests pass, but the exact mapping claim is false.",
    "C2 / HV07: Four semantic gaps remain in TR-MCP-SESSIONLIFE-001-AC001/002 and TEST-MCP-SESSIONLIFE-001-AC001/002; two additional criteria fail exact mapping.",
    "D1: Holistic P2 acceptance remains blocked by the mapping/coverage gaps and sub-98 completeness."
  ],
  "passCount": 14,
  "failCount": 5,
  "unknownCount": 2,
  "tipSha": "07de090308b9560b2be9d4b00239ba54744f7538",
  "receiptPaths": [
    "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\hostile-validator-sessionlife-p2-20261002T234447Z.md",
    "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\hostile-validator-sessionlife-p2-20261002T234447Z.json",
    "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20261002T234447Z-sessionlife-p2-contracts-hv.request.jsonl",
    "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20261002T234447Z-sessionlife-p2-contracts-hv.response.jsonl"
  ]
}
