# BUG-UNITFAIL-001 Independent Review

UTC: 2026-10-02T00:48:10.1543199Z

**OverallVerdict: DISAGREE**

Accuracy: 95/100. Completeness: 92/100. Approval requires at least 98 on both. These are repair acceptance scores, not statistical confidence.

Claims: 12 PASS, 6 FAIL, 2 UNKNOWN.

Final validation independently verified: .NET 4523 passed / 0 failed / 0 skipped; Pester 180 passed / 0 failed / 0 skipped / 0 notrun.

## Findings

### F1 P1: Supersession overwrites existing plan and TODO metadata
Source: F:\GitHub\McpServer\plugins\core\lib-ps\repl-invoke.ps1:2132
Evidence: Invoke-ReplSupersedeCurrentTurnIfInProgress unconditionally sends PlanFile=None and TodoId=None. The actual-function mock probe started with docs/plans/approved.md and BUG-UNITFAIL-001 and captured None/None on the canceled update. SessionLogService.cs:1651-1652 applies supplied non-null values.
Impact: Beginning the next turn destroys the prior turn plan/TODO association instead of preserving omitted metadata.
Requirements: FR-MCP-SESSIONLIFE-002-AC001, FR-MCP-SESSIONLIFE-002-AC003
Remedy: For a proven durable turn, omit unspecified metadata. For a first or degraded persist, preserve supplied/cached metadata before falling back to exact None. Add supersession tests for meaningful metadata in both durable and degraded cases.

### F2 P1: Session retitling clears recovery data without typed persistence proof
Source: F:\GitHub\McpServer\plugins\core\lib-ps\repl-invoke.ps1:3000
Evidence: After Invoke-ReplRaw returns Success=true, SetSessionTitle clears its failsafe and publishes primary without checking Output. The actual-function mock probe with empty Output returned true, cleared failsafe, and published primary. SessionLogController.cs:324-325 returns agent/sessionId/retitled=true after a successful title update.
Impact: Transport success with an empty, mismatched, or unsuccessful result can discard the only recovery payload and falsely claim persistence.
Requirements: FR-MCP-SESSIONLIFE-003-AC001, FR-MCP-SESSIONLIFE-003-AC006
Remedy: Validate the session-level typed identity and retitled=true before clearing failsafe or publishing primary. Retain recovery data and classify rejection otherwise. Add empty, mismatched identity, false retitled, and valid-result tests.

### F3 P2: Empty mutation receipts throw before fail-closed classification
Source: F:\GitHub\McpServer\plugins\core\lib-ps\repl-invoke.ps1:1507
Evidence: Mandatory string Output rejects empty strings before the parser and missing/unparseable branch at line 1525. Actual extracted function raised ParameterBindingValidationException with ParameterArgumentValidationErrorEmptyStringNotAllowed.
Impact: The caller cannot publish the intended classified rejection receipt; a prior outcome may remain visible. This fails closed as an exception, but not through the required typed failure contract.
Requirements: FR-MCP-SESSIONLIFE-003-AC002, TR-MCP-SESSIONLIFE-003-AC001
Remedy: Allow empty input into the validator and return Ok=false through the ordinary missing-result path. Ensure callers retain failsafe and publish rejected. Add negative empty-output tests; valid typed success mocks alone do not cover this boundary.

### F4 P2: Typed session receipts never cross the public wrapper boundary
Source: F:\GitHub\McpServer\plugins\core\lib-ps\repl-invoke.ps1:1397
Evidence: Publish-ReplSessionVerbReceipt stores session-verb-outcome.yaml and returns only bool at line 1410. Dispatcher lines 3012-3025 consumes bool and emits no receipt. Script entry lines 3053-3058 provides only exit status. Core Invoke-McpPlugin.ps1:308-322 and Codex wrapper:158-173 forward stdout, not the receipt file. Live successful updateTurn and appendActions calls returned empty stdout, while supported server query proved persistence.
Impact: Public callers cannot distinguish confirmed primary persistence from queued/degraded acceptance using the returned contract.
Requirements: TR-MCP-SESSIONLIFE-003-AC001, TR-MCP-SESSIONLIFE-003-AC002
Remedy: Emit exactly one serialized typed receipt at the public wrapper invocation boundary. Keep internal boolean hook contracts where needed. Add public-boundary tests for primary, queued, and rejected outcomes rather than only checking cache-local receipt state.

## Claim Ledger
- A01 PASS: Source identity and bounded changed-file inventory. develop at 35822cd9f028d60762a9822b272cf2f7becfa7ce; all 24 validation receipt file hashes independently matched. Preexisting benchmark JSON and untracked .worktrees excluded.
- A02 PASS: Restored SERVICEUPDATE records and mappings. Live GetEffectiveRequirementsAsync returned FR/TR/TEST-MCP-SERVICEUPDATE-001 pending with four structured unsatisfied AC each and the intended mappings. AC content compared with approved checked-in records.
- A03 PASS: Generated traceability and trim mapping. Five docs/Project projections contain restored service records and TEST-MCP-TRIM-001 mapped to FR-MCP-139/TR-MCP-QUALITY-001. Parent persisted action records typed Requirements.GenerateAsync use. Reviewer did not regenerate or hand-edit them.
- A04 PASS: Exact canonical-to-sibling reconciliation. SHA256 equal for all four canonical files and all five targeted sibling copies. Sibling repositories untouched. Behavioral defects are separately recorded as F1-F4.
- A05 PASS: Byte-preserving relocation and compatibility forwards. git hash-object --no-filters on all three destination files matched HEAD old-path blobs. Five forwards present at ProcessRunnerTypeForwards.cs:5-9; namespaces and signatures unchanged. Reflection compatibility tests pass.
- A06 PASS: QBAgent dependency boundary and documentation. QBAgent.Tools references Common.AgentCli instead of Services. Ownership/deps-manifest regression tests and docs/QBAGENT.md agree. No unrelated implementation changes found in runner bodies.
- A07 PASS: Warning inventory is measured and still exact. Independently counted trim-green.log: 12 IL2026 and 5 IL2104, publish exit 0. Red inventory 60/13/3/3/5. Build test exact dictionary equality and exit assertion remain; no added suppression.
- A08 PASS: New runner regression red-to-green evidence. runner-red.trx has 6 failures; forwarding-red.trx has 1 failure; forwarding-green.trx has 2 passes. Final QBAgent suite has 96 passes.
- A09 PASS: Final eight .NET unit projects. Independently parsed final TRX counters: Build225 Client304 CQRS33 Launcher20 McpAgent63 QBAgent96 REPL868 Support2914 = 4523 passed, 0 failed, 0 skipped. Support uses full-units/McpServer.Support.Mcp.Tests-retry/results.trx. Category!=AiReview&Category!=Integration.
- A10 PASS: Fixture corrections and final Pester suite. Reviewed all five fixture diffs and the Support source-path correction. Final pester-core-retry4 XML and summary: 180 passed, 0 failed, 0 skipped, 0 notrun. Earlier attempts preserved; no assertions removed. These tests do not cover F1-F4.
- A11 UNKNOWN: Original pre-repair full-unit baseline of 4511/5/0. Stated in request, but reviewer did not independently locate and parse the original aggregate raw run. Final green counters independently verified instead.
- B01 PASS: Single authorized reviewer, model, and isolation. Native transcript identifies gpt-6-astra with xhigh effort. PowerShell.Mcp console sa-ef70105b; dedicated cache/session/turn/failsafe. Parent active session and current-turn cache not overwritten.
- B02 PASS: Review-only execution boundary. No additional agents, production edits, sibling edits, commits, pushes, deploys, integration/AI tests, or operator-action review. Reviewer ran isolated mocked function probes and read evidence.
- B03 FAIL: Reviewer no-API-key-printing constraint. I failed to redact an embedded api_key query parameter during a required marker read. I disclosed the mistake immediately. The secret is not repeated in this receipt or the session-log verdict. This is a reviewer procedural failure, not a product finding.
- C01 FAIL: Metadata omission preservation. F1: supersession explicitly overwrites existing plan/TODO values with None.
- C02 FAIL: Typed session-title persistence proof and recovery retention. F2: transport success can clear failsafe without matching typed receipt.
- C03 FAIL: Classified failure for empty typed receipts. F3: parameter binding throws before ordinary typed rejection.
- C04 FAIL: Public typed session mutation receipt contract. F4: receipt is cache-local; public successful workflow calls return no typed receipt.
- D01 FAIL: Holistic bounded repair acceptance. Green test counts do not discharge the four lifecycle defects. BUG-UNITFAIL-001 remains not done; this review does not authorize done-state changes.
- E01 UNKNOWN: Final byte-complete response JSONL capture. Native transcript located and supplied. Parent owns byte-complete preservation after this reviewer finishes; that future capture cannot yet be verified.

## Persistence
Full structured verdict: F:\GitHub\McpServer\docs\receipts\hv\20261002-unit-failure-repair.review-sa-ef70105b.json
MCP session: Codex-20261002T002034Z-bug-unitfail-001-review-sa-ef70105b
MCP turn: req-20261002T002126Z-review-unitfail-001
Exact server readback proof: F:\GitHub\McpServer\docs\receipts\hv\20261002-unit-failure-repair.review-sa-ef70105b.readback.json
Native transcript: C:\Users\kingd\.codex\sessions\2026\10\01\rollout-2026-10-01T19-12-47-01a0f9f4-d605-7301-9b42-d8955d664e56.jsonl
Request JSONL: F:\GitHub\McpServer\docs\receipts\hv\20261002-unit-failure-repair.request.jsonl
Response JSONL: F:\GitHub\McpServer\docs\receipts\hv\20261002-unit-failure-repair.response.jsonl (parent capture after completion)

Do not accept BUG-UNITFAIL-001 or mark it done on this review. Remediate F1-F4 with red-before-green regression evidence and recheck with this same reviewer. User has approved that follow-on repair; it is not included in this pre-remediation verdict.