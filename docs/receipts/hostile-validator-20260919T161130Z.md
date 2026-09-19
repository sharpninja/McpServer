# Hostile validator receipt — txnkeyserver-box-deploy-done-claim

- TimestampUtc: 2026-09-19T16:11:30Z
- ValidatorIdentity: GrokSubagentHostile
- Gate: txnkeyserver-box-deploy-done-claim
- TodoId: PLAN-TXNKEYSERVER-001
- WorkClass: Class 1 product done-claim after live box deploy
- LiveBase: http://localhost:7147
- ImplementerReceipt (untrusted): /workspace/deliverables/txnkeyserver-box-deploy-20260919T154640Z.md

## OverallVerdict

**DISAGREE**

| Metric | Value |
|---|---|
| Accuracy | 99 |
| Completeness | 99 |
| PASS | 9 |
| FAIL | 4 |
| UNKNOWN | 1 |
| Material FAIL | 3 |

**PLAN-TXNKEYSERVER-001 may be marked done: NO** (hostile AGREE required; this review is DISAGREE).

## Material FAIL list

- **B-FR-MCP-173**: FR-MCP-173 present in MCP store with product content
  - GET /mcpserver/requirements/fr/FR-MCP-173 => 200 but title/body are Placeholder requirement backfilled for TODO link FR-MCP-173
  - GET /mcpserver/requirements/mapping/FR-MCP-173 => Mapping row not found
  - Prior Windows HV 20260917T172702Z had full amended FR-MCP-173; Linux box store is degraded
- **B-TR-MCP-TXNKEY-001**: TR-MCP-TXNKEY-001 present in MCP store with product content
  - GET /mcpserver/requirements/tr/TR-MCP-TXNKEY-001 => 200 but Placeholder requirement backfilled for TODO link
- **B-TEST-MCP-221**: TEST-MCP-221 present in MCP store
  - GET /mcpserver/requirements/test/TEST-MCP-221 => 404 TEST not found
  - GET /mcpserver/requirements/test list count=459; has_221=False

## Surface A (done-claim)

### A1-live-version. PASS
Live box MCP redeployed from develop 8f30caf; health version 1.0.0+8f30caf...

- GET http://localhost:7147/health => status=Healthy version=1.0.0+8f30caf98410166bb8cb11958dde5445491d3d61
- handoff worktree /tmp/mcpserver-develop-handoff HEAD=8f30caf98410166bb8cb11958dde5445491d3d61
- live binary mtime Sep 19 15:43; supervisor child pid 1223094 matches marker pid

### A2-binary-KeyserverScope. PASS
Live binary contains TurnTransactionKeyserverScope

- strings /opt/mcpserver/McpServer.Support.Mcp | rg -c TurnTransactionKeyserverScope => 2

### A3-config-TurnTransactions. PASS
Mcp.TurnTransactions.Enabled true (RequiredForMutations true)

- /opt/mcpserver/appsettings.yaml TurnTransactions.Enabled: true RequiredForMutations: true

### A4-live-proof-mutations. PASS
Live proof succeeded without keyserver/Subscriber failures: TODO create+update, sessionlog begin+complete, requirements list+PUT

- Implementer artifacts /tmp/txnkeyserver-proof/ todo-create 201 success failureKind=none; todo-update 200; session complete turnId=5; req FR-MCP-120 PUT notes box-deploy-proof
- HV re-verify: PUT todo BOX-DEPLOY-PROOF-TXNKEYSERVER-001 => 200 success=true failureKind=none
- HV re-verify: POST todo HV-PROOF-TXNKEYSERVER-001 => 201 success=true failureKind=none; PUT update 200
- HV re-verify: sessionlog open/begin/complete GrokSubagentHostile-20260919T160934Z-txnkey-hv req-...-hv-live-proof => 200/201/200 status=completed
- HV re-verify: PUT requirements/fr/FR-MCP-120 notes=hv-hostile-reverify 2026-09-19T16:09Z => 200

### A5-prior-unit-HV-AGREE. PASS
Prior unit-suite HV AGREEs; no Integration/Validation/Review/AiReview required for this done claim

- docs/receipts/hostile-validator-20260917T184144Z.json Gate=nuke-test-full-unit-suite OverallVerdict=AGREE Accuracy=99 Completeness=99 FailCount=0 Passed=3893
- docs/receipts/hostile-validator-20260917T174749Z.json Gate=txnkey-all-adapters-rereview OverallVerdict=AGREE
- HV did not re-run Integration/Validation/Review/AiReview suites (handoff constraint respected)

### A6-keyserver-quadbrain-only. PASS
Keyserver remains required for QuadBrain/brain-slot only (code path Exists)

- TurnTransactionKeyserverScope.RequiresKeyserver true only for brain-slot: party or brain-slot./quadbrain. operation prefixes
- Source present in handoff tree and symbol present in live binary (A2)

## Surface B (requirements store)

### B-FR-MCP-173. FAIL
FR-MCP-173 present in MCP store with product content

- GET /mcpserver/requirements/fr/FR-MCP-173 => 200 but title/body are Placeholder requirement backfilled for TODO link FR-MCP-173
- GET /mcpserver/requirements/mapping/FR-MCP-173 => Mapping row not found
- Prior Windows HV 20260917T172702Z had full amended FR-MCP-173; Linux box store is degraded

### B-FR-MCP-120. PASS
FR-MCP-120 present in MCP store

- GET /mcpserver/requirements/fr/FR-MCP-120 => 200 id=FR-MCP-120 title=MCP Server transaction gating
- Residual: body still describes universal coordinator gating; structured AC empty; notes overwritten by deploy/HV proof stamps

### B-TR-MCP-TXNKEY-001. FAIL
TR-MCP-TXNKEY-001 present in MCP store with product content

- GET /mcpserver/requirements/tr/TR-MCP-TXNKEY-001 => 200 but Placeholder requirement backfilled for TODO link

### B-TEST-MCP-221. FAIL
TEST-MCP-221 present in MCP store

- GET /mcpserver/requirements/test/TEST-MCP-221 => 404 TEST not found
- GET /mcpserver/requirements/test list count=459; has_221=False

### B-TEST-MCP-161. PASS
TEST-MCP-161 present in MCP store

- GET /mcpserver/requirements/test/TEST-MCP-161 => 200; condition present
- Residual: title empty; condition still describes broad adapter compensation (prior C-TEST161-not-retargeted theme)

## Surface C (PLAN not pre-marked done)

### C-plan-not-done. PASS
- GET /mcpserver/todo/PLAN-TXNKEYSERVER-001 => 200 done=False id=PLAN-TXNKEYSERVER-001

## Surface D (residuals / false done)

### D-workspace-tip. FAIL (residual/non-blocking for deploy binary claims)
Workspace repo tip expected develop 8f30caf

- /workspace/repos/McpServer HEAD=e8916da7... develop=384d69d3... (NOT 8f30caf)
- /tmp/mcpserver-develop-handoff HEAD=develop=8f30caf... (deploy source OK)
- Scored as residual vs deploy source: live version still 8f30caf. Materiality: gate checkpoint failed on primary workspace path.

### D-hmac-marker. UNKNOWN (residual/non-blocking for deploy binary claims)
Marker HMAC-SHA256 verifies (if feasible)

- Marker present; algorithm HMAC-SHA256 canonicalization marker-v1; fields list length 29 including agentPlugins.* but agentPlugins block absent from YAML
- Independent HMAC recomputation with raw strings (27 and 29 field variants) did not match signature.value
- Gate minimum met: health Healthy + version 8f30caf...; marker pid=1223094 matches live process

## Explicit constraints respected

- Did **not** mark PLAN-TXNKEYSERVER-001 done.
- Did **not** edit product C#.
- Did **not** run Integration/Validation/Review/AiReview suites.
- Did **not** echo API keys into receipts.

## Verdict rationale

Surface A deploy/live-mutation claims independently re-verified and PASS. Prior unit-suite HV AGREE receipts exist and were not re-demanded. However Surface B shows the Linux box MCP requirements store is **degraded** relative to the 2026-09-17 Windows HV AGREEs: FR-MCP-173 and TR-MCP-TXNKEY-001 are placeholders, TEST-MCP-221 is missing, and FR-MCP-173 mapping is absent. For a Class 1 product done-claim, missing/placeholder requirements that define the feature are **material FAILs**. Therefore OverallVerdict=DISAGREE; do not mark PLAN done.

## Receipt paths

- Markdown: docs/receipts/hostile-validator-20260919T161130Z.md
- JSON: docs/receipts/hostile-validator-20260919T161130Z.json
- JSONL: docs/receipts/hv/20260919T161130Z-txnkeyserver-box-deploy-done-claim.request.jsonl + .response.jsonl
