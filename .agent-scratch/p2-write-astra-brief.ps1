$ErrorActionPreference = 'Stop'
$Utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$scratch = Join-Path $wt '.agent-scratch'
New-Item -ItemType Directory -Force -Path $scratch | Out-Null
$stampFile = Join-Path $env:USERPROFILE '.grok\p2-astra-stamp.txt'
Set-Content -LiteralPath $stampFile -Value $Utc -Encoding ascii
$briefPath = Join-Path $scratch "hv-p2-astra-brief-$Utc.txt"
$pathsPath = Join-Path $scratch "hv-p2-astra-paths-$Utc.json"
$runnerPath = Join-Path $scratch "run-astra-hv-$Utc.ps1"
$codex = 'C:\Users\kingd\AppData\Local\Programs\OpenAI\Codex\bin\codex.exe'
$tip = 'a5f93023dc10018630ac555929550790ab2880b5'
$runId = 'p2-unit-legion-20261003T010420Z'
$md = Join-Path $wt "docs\receipts\hv\hostile-validator-sessionlife-p2-$Utc.md"
$json = Join-Path $wt "docs\receipts\hv\hostile-validator-sessionlife-p2-$Utc.json"
$req = Join-Path $wt "docs\receipts\hv\${Utc}-sessionlife-p2-contracts-hv.request.jsonl"
$resp = Join-Path $wt "docs\receipts\hv\${Utc}-sessionlife-p2-contracts-hv.response.jsonl"
$last = Join-Path $scratch "hv-p2-astra-lastmsg-$Utc.txt"
$brief = @"
You are the independent HOSTILE VALIDATOR (Codex / gpt-6-astra xhigh) for workspace:
$wt

Requested model: gpt-6-astra with reasoning effort xhigh. Verify live model/effort in the receipt (turn_context / session meta). Prefer DISAGREE when uncertain. AGREE only if accuracy >= 98 AND completeness >= 98.

Work class: project implementation (P2 SessionLife contracts). PR #72 gated tip $tip.
https://github.com/sharpninja/McpServer/pull/72
Branch: cursor/sessionlife-p2-contracts-5cb2
Plan: docs/plans/sessionlife-batch-completion-20260927.md (P2)
Gate RunId: $runId under TestResults/$runId/ and docs/receipts/hv/$runId-gate.log
Prior DISAGREE receipt (must re-attack; do not rubber-stamp): $wt\docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T234447Z.md
That prior receipt: OverallVerdict DISAGREE, Accuracy 99, Completeness 85 (34/40), Confidence 99. HV19/HV20 PASS. B7/B8 UNKNOWN. Six of nine criteria stayed uncredited: two from malformed Pester names (outcomes.. versus outcomes.) and four semantic gaps.

Operator override: use Astra this pass (not Grok). Review-only. Do NOT merge. Do NOT implement remediations. Do NOT mark TODO/goal done. Do NOT bulk-close the 35 sessionlife TODOs. Receipt writes only under docs/receipts/hv/.
Keep PluginIntegration out of unit inventory. Keep history-r1 frozen.
This is ONE Astra round. Stop after the receipt. Do not start a second round.
Do not invent an AGREE for B7 or B8. Leave them UNKNOWN unless you find authoritative provenance yourself. UNKNOWN on B7/B8 is not a FAIL and does not by itself force DISAGREE. OverallVerdict AGREE only if accuracy >= 98 AND completeness >= 98 AND the applicable in-scope A/C/D claims PASS. If completeness stays below 98, DISAGREE.

Operator overrides for evidence (record on receipt):
1. On PAYTON-LEGION2, Windows PowerShell 5.1 is an APPROVED evidence shell when Codex remaps pwsh or pwsh is unavailable. Prefer available pwsh (Codex-runtime path is fine).
2. MCP session-log persistence is NOT required for this Astra HV pass. Receipt-only JSONL + markdown/json under docs/receipts/hv/ suffice.
3. Live model/effort proof via turn_context/session_meta is required (gpt-6-astra / xhigh).
4. Review only. AGREE only if accuracy>=98 AND completeness>=98 AND applicable in-scope claims PASS.

FIRST ACTION (mandatory):
Execute add-profile skill. Read C:\Users\kingd\.claude\skills\add-profile\SKILL.md and follow it literally: read every non-skill profile *.md under C:\Users\kingd\.claude\profile\ in full. Record profile file count in the receipt.
Also follow C:\Users\kingd\.grok\skills\hostile-validator\SKILL.md surfaces A-D.

LOCKED SCOPE - evaluate ALL applicable surfaces:
A. Requested validation (attack every claim below)
B. Workspace rules honesty/receipts
C. Requirements/traceability for P2 SessionLife (FR/TR/TEST/AC/mappings). Suite green is not AC coverage.
D. Plan P2 DoD holistically - not only micro-claims.

Surface A claims to attack:
1. Re-attack the 20261002T234447Z FAIL list. Do not treat a green gate as closing them.
   - Name fix: acceptance-manifest.json commit a5f93023 replaces seventeen outcomes.. links with outcomes. (one dot). Credit a link only when the string equals a passing native Pester or TRX name from this gate. Do not silently repair a remaining mismatch.
   - Semantic fixes are in test commit 3e896874, plugins/core/test-fixtures/pester/SessionLogP2Contracts.Tests.ps1. Attack these exact holes from the prior receipt:
     TR-MCP-SESSIONLIFE-001-AC001 and TEST-MCP-SESSIONLIFE-001-AC002: Assert-P2FullCache must reject a reduced object. A fresh begin writes sessionId, status, codeEdits, lastBuildStatus, and the audit counters. The helper must require those keys and values. A six-field object that only has queryText, queryTitle, planFile, todoId, openedAt, and turnRequestId must fail the helper. Re-run that counterexample.
     TR-MCP-SESSIONLIFE-001-AC002: the empty-query update must show queryText omitted from the serialized turn when the cache has no queryText, not merely that an empty string is absent while a cached query is resent. The stop path must show receipt method workflow.sessionlog.completeTurn, process exit, and a real status result: queued stop stays in_progress and the hook reports that completeTurn did not mark the turn completed; primary stop transitions status from in_progress to completed. Do not accept a stale receipt.
     TEST-MCP-SESSIONLIFE-001-AC001: the missing-turn It must drive a degraded remote HTTP 404, count one recovery SubmitAsync, and retain one session_dialog envelope. A local failure before transport does not count. The prior lost-before-transport behavior was replaced; do not re-credit it.
   - The other previously repaired metadata rows (FR-MCP-SESSIONLIFE-002-AC002, TR-MCP-SESSIONLIFE-002-AC003, AC-FR-MCP-SESSIONLOGCTX-001-003) and TEST-MCP-SESSIONLIFE-002-AC001 stay in scope. TEST-002-AC001 failed last time only because its new Pester names had a double dot. Credit it only if those names now match executed passing tests and still cover the criterion text.
   - A6 / HV09: prior completeness was 85 (34/40). Denominator remains 35 core plus 8 governing legacy minus 3 deferred integration criteria (40). 39/40 = 97.5 still fails 98. Do not inflate by counting name-only or rejected links.
   - D1: holistic P2 acceptance stays blocked unless the gaps are actually closed.
   - B7 UNKNOWN: historical Python json provenance. Leave UNKNOWN.
   - B8 UNKNOWN: no historical P2 Red-test AGREE. Do not invent one.
2. Test commit 3e896874 (test(sessionlife): assert cache and 404 gaps) and mapping commit a5f93023 (docs(sessionlife): fix outcome test names). Diff against d396b3c37e79039aa761d168099b8ebdf49af762. Attack file scope. repl-invoke.ps1 and plugin-hook.ps1 must be unchanged. HV19/HV20 Ordinal identity must still hold. acceptanceState must stay not-accepted. storeSatisfied must not be flipped. Do not bulk-close TODOs.
3. HV19 and HV20 must still PASS. Re-prove PersistTurn Ordinal identity and beginTurn Ordinal reopen. Case-different request or session must not overwrite the bound turn. If identity regresses, FAIL and DISAGREE.
4. Fresh unit gate $runId is recorded ACCEPTED at tip $tip. Re-parse artifacts. Do not trust this brief alone. Implementer-reported counts to attack:
   - Pester Tests Passed 248, Failed 0, Skipped 0. command exit 0.
   - Nuke 7 projects, TRX totals 2926, 304, 33, 20, 63, 868, 96. Sum 4310. No PluginIntegration test total in that set. selected-projects.json excludes PluginIntegration.
   - Build.Tests 321 passed, 0 failed, outcome Completed.
   - CheckSessionLifeUnitGate Succeeded. GATE_EXIT=0. Log line accepted run $runId.
   - gate.log SHA256 AF3DD50B27729D0DF8B49963C051BD96A9802ED4CA40E067B614D11045A6577A.
   - Environment note: during the run only, untracked build/Directory.Build.targets skipped copying the locked build\bin\Debug\_build.dll and replaced IncrementalClean with an empty target. obj\Debug\ref\_build.dll was seeded from the existing obj implementation dll because a live PowerShell host locks the bin dll. That targets file was deleted after the check succeeded. Re-running the validator now can report deleted-included-source for that file. Judge the recorded artifacts and log. Do not treat the post-run deletion as a product commit or as a failed gate.
5. Completeness must be >= 98 for AGREE, from concrete AC mapping, not from a green suite or closed TODOs.
6. PluginIntegration remains excluded from the unit inventory. history-r1 stays frozen. acceptanceState of the 35 sessionlife TODO rows stays not-accepted.

Rules:
1. Do not trust plan checkboxes, agent chat, or implementer counts without re-checking.
2. If this brief is under-scoped, expand B/C/D yourself.
3. Independently inspect files; re-parse gate artifacts. Prefer PowerShell. Do not use Python.
4. Write durable receipts:
   - Markdown: $md
   - JSON twin: $json
   - The runner writes response JSONL to $resp
   - Request JSONL path: $req
5. Receipt MUST include: TimestampUtc, ValidatorIdentity (Codex/gpt-6-astra/xhigh), add-profile executed + profile file count, Claims A/B/C/D with PASS|FAIL|UNKNOWN + evidence, OverallVerdict AGREE|DISAGREE, Accuracy, Completeness, explicit FAIL list, model/effort proof, tipSha=$tip, RunId=$runId.
6. OverallVerdict=AGREE only if accuracy>=98 AND completeness>=98 AND applicable in-scope claims PASS. B7/B8 may stay UNKNOWN. Prefer DISAGREE when uncertain. Do not inflate completeness by counting proposed tests or name-only links.
7. Do not implement product features. Review only. Write receipts only. Do not commit or push. Do not merge.
8. End with === VERDICT JSON === and a JSON object containing overallVerdict, accuracy, completeness, confidence, failList, passCount, failCount, unknownCount, tipSha, receiptPaths.

Evidence seeds (re-verify):
- Tip $tip
- Test commit 3e89687462a602c463f80df01a8245915c6ca7c3
- Mapping commit a5f93023dc10018630ac555929550790ab2880b5
- Prior DISAGREE $wt\docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T234447Z.md
- Accepted gate $runId
"@
[System.IO.File]::WriteAllText($briefPath, $brief.Replace("`n", "`r`n"), [System.Text.UTF8Encoding]::new($false))
$paths = [ordered]@{
    codex = $codex
    promptPath = $briefPath
    runId = $runId
    tip = $tip
    wt = $wt
    lastMsg = $last
    reqPath = $req
    respPath = $resp
    mdReceipt = $md
    jsonReceipt = $json
}
[System.IO.File]::WriteAllText($pathsPath, (($paths | ConvertTo-Json) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
$runner = Get-Content -LiteralPath (Join-Path $wt '.agent-scratch\run-astra-hv-20261002T234447Z.ps1') -Raw
$runner = $runner.Replace('20261002T234447Z', $Utc)
[System.IO.File]::WriteAllText($runnerPath, $runner, [System.Text.UTF8Encoding]::new($false))
Write-Output "UTC=$Utc"
Write-Output "BRIEF=$briefPath"
Write-Output "RUNNER=$runnerPath"
