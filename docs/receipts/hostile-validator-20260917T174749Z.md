# Hostile validation receipt (rereview)

TimestampUtc: 2026-09-17T17:51:15Z
LaunchTimestampUtc: 2026-09-17T17:47:49Z
ValidatorIdentity: GrokSubagentHostile
WorkspacePath: F:\GitHub\McpServer
Gate: txnkey-all-adapters-rereview
TodoId: PLAN-TXNKEYSERVER-001
PriorReceipt: F:\GitHub\McpServer\docs\receipts\hostile-validator-20260917T172702Z.md
PriorFail: C-TEST161-not-retargeted
WorkClass: 1 (project implementation rereview of TEST-MCP-161 retarget)
AddProfileExecuted: true
AddProfileFileCount: 19
AddProfileExcludedSkillPort: C:\Users\kingd\.claude\profile\add-profile.grok.md

HvSessionId: GrokSubagentHostile-20260917T174749Z-txnkey-test161-rereview
HvRequestId: req-20260917T174749Z-001-txnkey-test161-rereview
HvTurnId: 45219
RequestJsonl: F:\GitHub\McpServer\docs\receipts\hv\20260917T174749Z-txnkey-test161-rereview.request.jsonl
ResponseJsonl: F:\GitHub\McpServer\docs\receipts\hv\20260917T174749Z-txnkey-test161-rereview.response.jsonl
ReceiptMarkdown: F:\GitHub\McpServer\docs\receipts\hostile-validator-20260917T174749Z.md
ReceiptJson: F:\GitHub\McpServer\docs\receipts\hostile-validator-20260917T174749Z.json

OverallVerdict: AGREE
Accuracy: 99
Completeness: 99
PASS: 16
FAIL: 0
UNKNOWN: 0

Prior FAIL C-TEST161-not-retargeted is remediated in the MCP store. HV did not mark PLAN-TXNKEYSERVER-001 done. Remaining gates still open: live Nuke deploy, this AGREE does not by itself close the TODO, full unit suite not run.

## Classification

Class 1 project implementation rereview. Surface C applies. Default FAIL until store GET.

## FAIL list

none.

## UNKNOWN list

none.

## Surface A (requested remediations)

### A1. PASS. Prior FAIL is gone in the MCP store (GET TEST-MCP-161).

Evidence (REST GET `/mcpserver/requirements/test/TEST-MCP-161`, not parent chat):
- title = "QuadBrain brain-slot turn-transaction coordinator tests"
- notes = "Amended 2026-09-17 after HV DISAGREE C-TEST161-not-retargeted. Retargeted from all first-party fail-closed gates to QuadBrain/brain-slot remaining scope. TEST-MCP-221 covers the FR-MCP-173 carve-out."
- acCount = 3: ac-test161-001, ac-test161-002, ac-test161-003, all isSatisfied=false
- Old generic isSatisfied=true AC ("Focused and full Support.Mcp/Repl.Core test suites cover transaction gating and fail-closed behavior") is absent
- Condition (from the GET payload) requires coordinator commit/degraded/rollback/timeout/pub-sub for brain-slot.invoke and brain-slot.weight-update (and other RequiresKeyserver operations). It states first-party non-QuadBrain mutations are governed by FR-MCP-173 / TEST-MCP-221 and SHALL persist without coordinator/keyserver; they are not fail-closed under this TEST.

### A2. PASS. docs/Project/Testing-Requirements.md matches the store, not the stale general-adapter fail-closed obligation.

Evidence:
- Testing-Requirements.md LastWriteTimeUtc 2026-09-17T17:47:50Z (same second as Functional-Requirements.md, Technical-Requirements.md, TR-per-FR-Mapping.md, Requirements-Matrix.md: generate doc=all)
- TEST-MCP-161 block carves TODO/requirements/session-log including QBAgent/memory/repo/tools/GitHub/GraphRAG/voice/agent pool/ingest/context/federation apply/control to FR-MCP-173 / TEST-MCP-221
- Structured markdown ACs match ac-test161-001..003
- Phrase scan: "they are not fail-closed under this TEST"

Residual (not FAIL): docs/Project/wiki/github/Testing-Requirements.md and wiki/azure/Testing-Requirements.md still have the old TEST-MCP-161 fail-closed adapter list. Wiki is not the claimed generate-markdown surface.

### A3. PASS. PLAN-TXNKEYSERVER-001 still Done=false. No done or live-deploy claim.

Evidence: MCP todo_get Done=false CompletedDate=null. Title still "Keyserver signs QuadBrain/QBAgent session-log only". Live pid 138544 still running McpServer.Support.Mcp (marker started 2026-09-16).

## Surface B (workspace rules)

### B1. PASS. Honesty. Parent does not claim PLAN done or live deploy.

### B2. PASS. HV re-GET store and re-read markdown. Did not trust the prior receipt or parent narrative.

### B3. PASS. MCP-only TODO/requirements. HV used todo_get and REST GET after MCP requirements_get is not a dedicated tool.

### B4. PASS. PowerShell only. No Python.

### B5. PASS (N/A). No deletes.

### B6. PASS. No post-hoc FR-vs-file timestamp FAIL. This is a requirements-text rereview, not a claimed implementation-phase complete.

## Surface C (requirements)

### C1. PASS. TEST-MCP-161 no longer conflicts with FR-MCP-173 / FR-MCP-120. Store FR-MCP-120 still carves non-QuadBrain mutations to FR-MCP-173. TEST-161 now agrees.

### C2. PASS. Mapping FR-MCP-120 -> TR-MCP-TXN-001 + TEST-MCP-161,TEST-MCP-168 is acceptable because TEST-161 is brain-slot scoped. Mapping GET FR-MCP-173 -> TR-MCP-TXNKEY-001 + TEST-MCP-221 still intact.

### C3. Residual, not FAIL. Requirements-Matrix.md still lists TEST-MCP-161 as Complete with TransactionGated* coverage from the first-slice matrix row. Generate rewrote the file at 17:47:50Z; the matrix coverage cell was not retargeted. That is listing lag, not a TEST Condition that still requires general-adapter fail-closed.

## Surface D (plan holistically)

### D1. PASS. PLAN-TXNKEYSERVER-001 Done=false. FR-MCP-173 structured ACs remain isSatisfied=false.

### D2. PASS. Remaining open work is still live Nuke deploy, HV AGREE before done (this rereview AGREE authorizes the TEST-161 FAIL close, not the whole PLAN), and full unit suite not run. Honest.

## Verdict

OverallVerdict=AGREE. Accuracy 99 Completeness 99. Prior FAIL C-TEST161-not-retargeted is gone in the store and in docs/Project/Testing-Requirements.md. Do not mark PLAN-TXNKEYSERVER-001 done on this AGREE alone: live service is still pid 138544 from 2026-09-16, full unit suite was not run, FR-173 ACs are unsatisfied.

=== VERDICT JSON ===
{"OverallVerdict":"AGREE","Accuracy":99,"Completeness":99,"PassCount":16,"FailCount":0,"UnknownCount":0,"FailList":[],"UnknownList":[],"WorkClass":1,"AddProfileFileCount":19,"PriorFailRemediated":"C-TEST161-not-retargeted","HvSessionId":"GrokSubagentHostile-20260917T174749Z-txnkey-test161-rereview","HvRequestId":"req-20260917T174749Z-001-txnkey-test161-rereview","HvTurnId":45219}
