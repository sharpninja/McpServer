# Hostile validator receipt

TimestampUtc: 2026-08-19T16:13:22Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation of existing FR-MCP-TRIAGEPLUGIN-001; surfaces A-D apply)
add-profile: executed yes. Profile files read: 18 non-skill markdown files under C:\Users\kingd\.claude\profile\ (excluded add-profile.grok.md).
OverallVerdict: AGREE
PASS: 15
FAIL: 0
UNKNOWN: 0
AccuracyRating: 93 (independent Pester re-run, SHA256 of both hook files, live isolation function eval, plugin sessionlog query without exact-requestId filter, git HEAD/upstream, current-turn.yaml before and after this hostile UserPromptSubmit and isolated session persist)
CompletenessRating: 90 (TEST-MCP-TRIAGEPLUGIN-001 store/markdown AC text still does not name the new It block; red Pester output was not independently captured; isolation detector is hostile-prompt regex plus subagent env)

AdversarialSessionId: GrokCode-20260819T161322Z-hostile-triageplugin
AdversarialRequestId: req-20260819T161322Z-001-hostile-validate-isolation
planFile: None
todoId: None
SessionProof: docs/receipts/_hv-20260819T160006Z/iso2-client-SessionLog-QueryAsync.txt (client.SessionLog.QueryAsync text "Hostile validate TRIAGEPLUGIN isolation remediATION", totalCount 1)

Trust: marker HMAC-SHA256 signatureOk true. GET /health nonce nonce-hv-20260819111141-164 echoed. status Healthy. version 1.4.28+f4060f037e62e64974026aff9d24e11b2f481952. Evidence: docs/receipts/_hv-20260819T160006Z/00-trust.json

## FAIL list

Empty.

## Residual observations (not FAILs)

- TEST-MCP-TRIAGEPLUGIN-001 AC text still lists the original five Pester proofs and does not name `UserPromptSubmit.BackgroundPrompt_DoesNotSupersedeRootInProgressTurn`. FR-MCP-TRIAGEPLUGIN-001 ac-1 already includes "Root UserPromptSubmit stays on the root session while background agents run." The new It block covers that sentence.
- 019 action 1 claims a red Pester run. This review re-ran green only. Pester file LastWriteUtc 2026-08-19T15:50:19.2624328Z precedes core hook 2026-08-19T15:51:14.6374302Z. Byrd phase-order is not failed from FR createdAt vs file mtime.
- Isolation detector: `MCP_SUBAGENT_ID` / `GROK_SUBAGENT_ID`, payload subagent fields, and hostile prompt regex. This spawn matched `You are the HOSTILE VALIDATOR` and reused 019.
- PLAN-TRIAGECLUSTER-001 is already `done: true` from docs/receipts/hostile-validator-20260819T013000Z.md. This remediATION did not flip it. Plan markdown still says "S0 in progress."
- Plugin Status pendingCount/failsafeCount 6. Query invokes logged failsafe drain SubmitAsync 30s timeouts. That did not rewrite current-turn.yaml.

## A. Requested validation

### A1 Pester TriagePluginIdentity 10 passed, 0 failed, 0 skipped, includes UserPromptSubmit.BackgroundPrompt_DoesNotSupersedeRootInProgressTurn. PASS

Independent re-run: `Invoke-Pester` on `plugins/core/test-fixtures/pester/TriagePluginIdentity.Tests.ps1`. Pester v5.7.1. Discovery found 10 tests. Result Passed. Passed=10 Failed=0 Skipped=0 NotRun=0. Last test name `UserPromptSubmit.BackgroundPrompt_DoesNotSupersedeRootInProgressTurn` Passed 191ms. Evidence: docs/receipts/_hv-20260819T160006Z/pester-TriagePluginIdentity.txt and pester-TriagePluginIdentity.nunit.xml.

### A2 Both plugin-hook.ps1 files define Test-PluginPromptIsBackgroundAgent and Get-PluginRootTurnIsolationDecision. Isolation mapping reuse / isolate-skip / open-new. PASS

SHA256 of `F:\GitHub\McpServer\plugins\core\lib-ps\plugin-hook.ps1` and `F:\GitHub\mcpserver-grok-plugin\lib\plugin-hook.ps1` are equal: 602316F539C8D641900890AC8DA54F2C624F7F932BD82DF5135E9F9C3ED1F3C3. Functions at lines 642 and 674 in both files. Open-PluginTurn calls Get-PluginRootTurnIsolationDecision and returns without rewriting current-turn.yaml on reuse or isolate-skip.

Independent function eval (docs/receipts/_hv-20260819T160006Z/isolation-eval.json):
- hostile prompt + in_progress 019 => reuse
- hostile prompt + completed 018 => isolate-skip
- real user remediATE prompt + in_progress 019 => open-new
- Test-PluginPromptIsBackgroundAgent(hostile)=true, remediATE=false

Uncommitted core diff adds those functions plus Set-PluginWorkspaceIdentity wiring (92 lines). Grok plugin lib/plugin-hook.ps1 is modified uncommitted the same way. Live hashes match.

### A3 After this hostile UserPromptSubmit, root 019 is still in_progress and current-turn.yaml still names 019. PASS

Cache `F:\GitHub\McpServer\.mcpServer\grok\current-turn.yaml` before plugin work, after queries, after isolated hostile begin/complete, and at receipt write:
- turnRequestId: req-20260819T153500Z-019-remediate-hook-cache-isolation
- status: in_progress
- sessionId: GrokCode-20260818T182741Z-plugin-session
- planFile: None
- todoId: None
- LastWriteTimeUtc unchanged: 2026-08-19T15:54:40.4249271Z
- Not a req-*-prompt-* hostile id

Server query without exact-requestId filter (`client.SessionLog.QueryAsync` agent=GrokCode text="Remediate hook cache isolation"): turn 019 status=in_progress, planFile None, todoId None, queryTitle "Remediate hook cache isolation FAILs". Not canceled. Not superseded. Five actions and processingDialog present. Evidence: docs/receipts/_hv-20260819T160006Z/019-turn-block.txt and 02-query-title.txt.

Workspace session-state.yaml still sessionId GrokCode-20260818T182741Z-plugin-session. LastWriteTimeUtc 2026-08-19T15:06:55.2113188Z (prior hostile 41a3 era). This hostile UserPromptSubmit did not rewrite it.

Isolated review persist used CacheRoot docs/receipts/_hv-20260819T160006Z/hostile-cache. Root cache lastWrite stayed 15:54:40Z.

### A4 Implementer did not mark PLAN-TRIAGECLUSTER-001 done in this remediATION. PASS

workflow.todo.get id PLAN-TRIAGECLUSTER-001: done true. doneSummary still cites docs/receipts/hostile-validator-20260819T013000Z.md and H-done 000500Z. remaining: "None. Full-goal AGREE 013000Z." Not a new done flip in this remediATION. 019 binds planFile None and todoId None. Evidence: docs/receipts/_hv-20260819T160006Z/07-todo-plan.txt.

### A5 Implementer did not commit or push. HEAD still f4060f037e62e64974026aff9d24e11b2f481952. PASS

git rev-parse HEAD = f4060f037e62e64974026aff9d24e11b2f481952.
@{u} / origin/develop = same SHA.
Branch: develop...origin/develop (no ahead/behind annotation).
Staged diff: empty.
Working tree has uncommitted plugin-hook.ps1, untracked TriagePluginIdentity.Tests.ps1, and other cluster files. That is not a commit or push.
Host /health version 1.4.28+f4060f037e62e64974026aff9d24e11b2f481952.

## B. Workspace rules

### B1 Always bring the receipts. PASS

Claims were re-verified from Pester output, file hashes, live function eval, plugin QueryAsync, git, and on-disk current-turn.yaml. Implementer 019 actions name the same files.

### B2 Byrd v4 phase-order. PASS

This is a late review of an already-written isolation slice. Do not FAIL from FR createdAt vs file mtime. Implementer 019 claimed tests-first. Independently: Pester file mtime precedes hook mtime; green re-run is 10/0/0. Red output was not re-captured. That is not proof that tests-first is false.

### B3 MCP-only storage. PASS

TODO/session/requirements via Invoke-McpPlugin.ps1 (workflow.todo.get, workflow.requirements.getFr/getTest, client.SessionLog.QueryAsync, workflow.sessionlog.*). No direct edit of todo.yaml or session-log store. Isolated CacheRoot used so this review's beginTurn would not clobber 019.

### B4 PowerShell only / no Python. PASS

pwsh.exe -NoProfile. Pester, git, plugin wrapper, marker-resolver, health nonce. No python.

### B5 Honesty. PASS

Live artifacts match the five numbered remediATION claims. Cache pointer audit counters are 0 while server 019 has actions; the claim was requestId and in_progress, which match.

## C. Requirements

### C1 FR-MCP-TRIAGEPLUGIN-001 exists with AC covering root UserPromptSubmit isolation. PASS

workflow.requirements.getFr: title "Plugin root session, cache, console, and persist identity". ac-1 text includes "Root UserPromptSubmit stays on the root session while background agents run." status pending, isSatisfied false. Implementer did not claim store completed. Notes: closes BUG-TRIAGE-111, 124, 126, 131, 143. Evidence: docs/receipts/_hv-20260819T160006Z/08-getFr.txt.

### C2 TEST mapping exists. PASS

TEST-MCP-TRIAGEPLUGIN-001 through 005 exist in docs/Project/TR-per-FR-Mapping.md and getTest. TEST-001 AC names the original five S5 Pester proofs. The new It block is additional coverage of FR ac-1, not a missing FR/TR.

### C3 Tests cover the remediATED AC sentence. PASS

`UserPromptSubmit.BackgroundPrompt_DoesNotSupersedeRootInProgressTurn` asserts background detection and reuse/isolate-skip/open-new. Independent Pester passed that test. Live this hostile UserPromptSubmit reused 019.

## D. Current plan holistically

### D1 No plan-done flip this remediATION. PASS

Implementer said this completes an already-specified TRIAGEPLUGIN AC, not PLAN-TRIAGECLUSTER-001 done. todo_get confirms doneSummary still 013000Z. 019 planFile None todoId None.

### D2 Plan DoD not claimed complete here. PASS

docs/plans/triage-cluster-001.md still says S0 in progress in the header. This review does not treat the whole cluster plan as closed by this isolation remediATION. The isolation sentence of FR-MCP-TRIAGEPLUGIN-001 is evidenced live.

## Adversarial session-log turn proof

Dedicated review session (isolated CacheRoot), not the root plugin-session:
- sessionId GrokCode-20260819T161322Z-hostile-triageplugin
- requestId req-20260819T161322Z-001-hostile-validate-isolation
- queryTitle Hostile validate TRIAGEPLUGIN isolation remediATION
- turn status completed
- planFile None
- todoId None
- actions order 1-5 (integer), including design_decision
- processingDialog observation + decision
- response contains OverallVerdict AGREE and this receipt path

Proof query: client.SessionLog.QueryAsync agent=GrokCode text="Hostile validate TRIAGEPLUGIN isolation remediATION" totalCount=1. File: docs/receipts/_hv-20260819T160006Z/iso2-client-SessionLog-QueryAsync.txt

Root current-turn.yaml still 019 in_progress after that persist.
