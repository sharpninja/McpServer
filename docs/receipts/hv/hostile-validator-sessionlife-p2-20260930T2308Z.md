# P2 SessionLife hostile validation: post-HV01-HV14 re-attack

TimestampUtc: 2026-09-30T23:27:03.9690142+00:00
ValidatorIdentity: Codex/gpt-6-astra/xhigh. Work class: project implementation.
TipSha: 8f28c385e1eecce3e07a96d3b0b4f5d1fe6f886c. RunId: p2-unit-legion-20260930T224636Z.
OverallVerdict: **DISAGREE**. Accuracy **99**, completeness **96**, confidence **99**.
Primary A-D claims: **12 PASS / 6 FAIL / 2 UNKNOWN**. Finding counts are separate.
Add-profile executed first: **21 non-skill profile files read in full**. All pass-specific overrides recorded in JSON twin.

## Verified evidence and scope

Native artifacts independently parsed: Pester **208/0/0**, seven Nuke projects **4252/0/0**, Build.Tests **321/0/0**. All individual outcomes and all three command exits pass; native failed blocks/containers are zero. Direct compiled validator **ACCEPTED**. Unit project totals: Support.Mcp2879, Client301, Cqrs33, Launcher20, McpAgent63, Repl.Core866, QBAgent90. This reviewer reparsed the full gate and ran targeted real-function probes; it did not rerun the entire suite.
All **2228** source hashes and **17** raw gate artifact hashes remain unchanged. P2 runtime/docs inputs are bound. Frozen history-r1 manifest and five archived files match; history-manifest SHA256 **B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4**. All95 inherited P1 validator cases plus exclusion regression pass. PluginIntegration remains excluded and assigned to P6.
HV10-HV13 exact counterexamples pass. Ordinary first-persist metadata passes all10 actual compiled server validator checks. Probe adjudication has39PASS/8FAIL assertions; all eight failures concern missing or whitespace typed identity across dialog/title. All35 TODO acceptance rows remain **not-accepted**, and historical hvAgreeClaimed remains false.

## Explicit FAIL findings

### HV14 [high] Dialog and title still accept missing or whitespace typed identity and claim primary persistence.

repl-invoke.ps1:1533/1536 only reject mismatches when the returned identity is nonempty. Eight real-function probes (four missing/blank ID variants per verb) return true with persisted=true. setTurnTitle deletes its write-ahead file, queueCount=0. Empty and contradictory dialog envelopes also return persisted=true. Exact old wrong-identity and retitled=false probes are repaired; this is the remaining fail-open branch of the same contract.

Requirements: FR-MCP-SESSIONLIFE-003-AC001; FR-MCP-SESSIONLIFE-003-AC002; FR-MCP-SESSIONLIFE-003-AC006; TR-MCP-SESSIONLIFE-003-AC001; TR-MCP-SESSIONLIFE-003-AC002

Remediation direction only, not implemented: Require present, nonblank matching typed sessionId and requestId for both verbs before primary success; retain title recovery until confirmation. Extend regression cases to absent, partial and whitespace identities.

Evidence: docs/receipts/hv/20260930T2308Z-supplemental-probes.json; docs/receipts/hv/20260930T2308Z-additional-probes.json; docs/receipts/hv/20260930T2308Z-probe-evaluation.json

### HV07 [high] Per-AC mappings and regression oracles remain incomplete despite 35 populated arrays.

acceptance-manifest.json is unchanged from419e0e40 to8f28c385. FR001-AC003(line348) maps retry/path tests rather than complete-query fallback; FR002-AC002(line558) maps marker-proof rejection rather than raw metadata validation; TR001-AC003(line1744) lacks bounded404 recovery assertions; TEST002-AC004(line2917) maps docs/hook tests rather than cumulative gate validation. FR001-AC004(line399) still names nonexistent native label SessionLogLifecycle: degraded 404 queues session_dialog. Five HV10-HV14 native names are unmapped. SessionLogP2Contracts.Tests.ps1:545-550 regex-matches guide text instead of parsing every YAML document. Current examples independently parse, but that does not repair the required regression oracle.

Requirements: TEST-MCP-SESSIONLIFE-001-AC001; TEST-MCP-SESSIONLIFE-001-AC003; TEST-MCP-SESSIONLIFE-002-AC001; TEST-MCP-SESSIONLIFE-002-AC003; TEST-MCP-SESSIONLIFE-002-AC004; Plan:371

Remediation direction only, not implemented: Map actual existing tests to the correct criteria and add missing criterion assertions; parse all document examples with completeness accounting. Preserve not-accepted TODO states.

Evidence: docs/receipts/hv/20260930T2308Z-trace-review.json; docs/receipts/hv/20260930T2308Z-sessionlife-p2-trace-audit.json

### HV15 [medium] New test helpers handwrite an operational repository marker and corrupt YAML paths containing space plus hash.

Get-TestMarkerSnapshot in SessionLogLifecycle.Tests.ps1:8-16, SessionLogLifecycleMetadata.Tests.ps1:8-16 and SessionLogAuditReconcile.Tests.ps1:9-17 defaults Workspace to actual RepoRoot and writes AGENTS-README-FIRST.yaml with File.WriteAllText and apiKey:test if absent. This violates plan:19 no handwritten YAML and AGENTS.md rule12. Executing the unchanged helper in a receipt-owned directory ending in space+#path creates the marker but parsed workspacePath loses that suffix. No existing marker overwrite is alleged. The root marker was already present before this review; its historical creator is not independently attributed.

Requirements: AGENTS.md rule12; Plan:19; Plan:21

Remediation direction only, not implemented: Keep test markers inside isolated fixtures and construct them through the required YAML object serializer. Do not create a fake operational marker at the actual repository root.

Evidence: docs/receipts/hv/20260930T2308Z-marker-helper-probe.json

### HV09 [gate] Completeness96 is below the mandatory98 threshold.

Remaining typed-result counterexamples, criterion-mapping and oracle gaps, new marker-helper defect, and unresolved historical provenance/inter-phase evidence prevent qualifying acceptance. Accuracy99, completeness96, confidence99. These are reviewer assessments of supported acceptance evidence, not measured product-correctness percentages.

Requirements: Operator98/98 gate

Remediation direction only, not implemented: Resolve the concrete failures and unknowns, then obtain a new independent review. No completion-state change is authorized.

Evidence: docs/receipts/hv/20260930T2308Z-trace-review.json

## HV01-HV14 dispositions

- **HV01 PASS**: Nine explicit/cache/None first-persist payloads plus degraded404 recovery payload pass actual compiled ValidateForNewEntry; bounded recovery submit observed.

- **HV02 PASS**: Empty session, wrong workspace, missing marker proof and wrong-marker degraded reopen reject without transport calls. Bound-session rotation probes preserve sessionA in cache and payload.

- **HV03 PASS**: Six original caller-mismatch verbs reject with zero calls and identical current-turn hashes.

- **HV04 PASS**: Actual Open-PluginTurn body reports existing artifact for initial degraded, duplicate degraded and space+#path; duplicate exposes degraded=true.

- **HV05 PASS**: Identical degraded begin retry makes a second primary call and clears matching recovery after confirmed success.

- **HV06 PASS**: Identical and changed successful retries clear degraded/failsafePath cache fields. Changed retry retains its older distinct envelope; broader P4 replay-order guarantees not claimed.

- **HV07 FAIL**: Wrong/missing per-AC mappings and nonexhaustive document parser remain. Detailed35-row assessment in trace-review JSON.

- **HV08 PASS**: Original runtime failsafePath append replaced with object YAML; real hash-path hook round-trips. Distinct new test-helper violation is HV15.

- **HV09 FAIL**: Completeness96 remains below98.

- **HV10 PASS**: Actual serialized Submit turns for durable update/appendActions/complete have neither planFile nor todoId key when omitted.

- **HV11 PASS**: Explicit append updates cache before completion; subsequent durable complete omits metadata and cannot resend the stale pair.

- **HV12 PASS**: Both required missing-marker and wrong-marker-degraded begin probes reject with zero calls and identical hashes.

- **HV13 PASS**: All six missing-marker caller mismatches reject before freshness, with zero calls and identical hashes.

- **HV14 FAIL**: Original wrong identity and retitled=false reject correctly. Eight absent/partial/blank identity probes still report primary; title loses recovery. Empty/contradictory dialog cases also accept.

## A-D adjudication

- **A1 FAIL**: All prior HV01-HV14 failures and A1/C2/C3/D1 are remediated without weakening. HV01-HV06/HV08/HV10-HV13 exact counterexamples pass. HV14 still fails absent/blank typed identity; HV07 mapping/oracle gaps and HV09 sub98 completeness remain. New HV15 is independently reproduced. See per-finding and reattack records.

- **A2 PASS**: The named fresh cumulative unit gate is green and accepted at the exact requested tip. Reparsed208 NUnit cases and4573 TRX outcomes: Pester208/0/0, seven Nuke projects4252/0/0, Build.Tests321/0/0; all individual outcomes pass. Native failed blocks/containers0; all three command exits0. Direct compiled SessionLifeUnitGateValidator.Validate ACCEPTED. Tip committed22:45:14Z before22:46:45Z Pester start; gate accepted23:05:27Z. Full suite reparsed, not rerun by this reviewer.

- **A3 PASS**: PluginIntegration remains excluded from units and owned by P6. build/Build.Test.cs:14-25 excludes PluginIntegration; the same selected array drives inventory and execution. Actual inventory contains exactly seven projects. Exclusion regression passes. Plan:295-327,400 retains PluginSessionLogIntegration in P6.

- **A4 PASS**: Gate source binding covers relevant P2 runtime and document inputs. All2228 current source hashes match before and after review. Eight named runtime/orchestration/docs/schema inputs are present. SessionLifeUnitGateManifest.cs:22 includes plugins/core/lib-ps, tools/validation and docs roots. All17 raw gate artifacts remain byte-identical.

- **A5 PASS**: history-r1 remains frozen and inherited P1 validator coverage passes. All five frozen file hashes match. history-manifest SHA256 B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4; no history diff.95 inherited SessionLifeUnitGate cases plus exclusion regression pass96/0.

- **A6 FAIL**: Completeness reaches98 through actual per-AC coverage, while all35 TODO rows remain not-accepted. All35 TODO rows correctly remain not-accepted and all35 core mapping arrays are populated. HV07 shows population is not criterion coverage; HV14 remains a real failing branch. Completeness96 fails the threshold.

- **B1 PASS**: Mandatory add-profile ran first and every non-skill profile was read in full. First action attempted specified Claude add-profile SKILL.md. Sandbox error206 occurred before execution; approved escalated pwsh read succeeded. All21 non-skill profile Markdown files read fully, with truncated sections reread. Exact hashes in initial-state/trace-audit. Independent trace reviewer also read21 first.

- **B2 PASS**: This review uses the authorized evidence shell and no Python. PAYTON-LEGION2, Codex-runtime pwsh7.6.5. PowerShell-only evidence scripts; no Python/Bash. WindowsPowerShell5.1 fallback authorized but unused. rg unavailable, so PowerShell and git searches used.

- **B3 PASS**: Receipt-only review persistence follows the explicit exception. MCP session-log persistence expressly waived. Exact request and launcher, visible response/tool snapshot, complete verdict Markdown/JSON and independent evidence stream retained under docs/receipts/hv. Runner separately writes final CLI stream to the requested responseJSONL after process exit. Root marker contains only workspacePath/apiKey:test and is not treated as trusted MCP bootstrap.

- **B4 PASS**: Live validator is Codex/gpt-6-astra/xhigh. Exact root thread01a0f493-6a5b-7851-b061-ef669892b598: session_meta line1 codex_exec CLI0.158.0-alpha.2.1 and requested cwd; turn_context line8 model=gpt-6-astra effort=xhigh. Separate codex-auto-review/low guardian is not the validator. Trace subagent independently proves Astra/xhigh.

- **B5 PASS**: No prior P2 HV AGREE, acceptance or later-phase completion is invented. Historical p2-contracts-verdict.json retains accepted=false,phaseComplete=false,hvAgreeClaimed=false. GitHub PR72 independently read as draft/open/unmerged at8f28c385 and explicitly disclaims P2 exit/HV AGREE. Older red/DISAGREE evidence is preserved.

- **B6 FAIL**: Remediation follows object-first YAML and workspace isolation rules. Original HV08 runtime failsafePath repair uses Read/Write-McpYamlObject and passes real hook hash-path probe. New test helpers violate the same rule by handwriting repository-root marker YAML; unchanged helper reproduces path truncation in isolated probe. HV15.

- **B7 UNKNOWN**: Historical implementation automation fully complied with no-Python and MCP-only storage. p2-contracts-verdict.json:53 and p2-unit-gate-failures.json:1037 still declare Python json module serializing native objects. Original author execution and live-store provenance not independently established. File existence or historical narrative is not execution proof.

- **B8 UNKNOWN**: Required P2 Red-test inter-phase hostile AGREE is established. No linked P2 Red-test AGREE located in current P2 plans/receipts. Plan:259,384 requires that gate. P1 acceptance is not P2 acceptance. No FR-versus-file timestamp inference. Historical phaseComplete remains false.

- **B9 PASS**: This validator preserved review-only scope and existing dirty work. Only receipt scripts, isolated fixtures and audit artifacts under docs/receipts/hv were written. No product fixes, merge, commit/push, deployment, TODO/goal/requirement mutation or real MCP-store write. Three pre-existing tracked dirty files outside receipts have identical before/after hashes. HEAD unchanged.

- **C1 PASS**: Structured governing requirements, AC and mapping snapshots exist. Acceptance manifest has109 AC rows,47 distinct mapping edges,35 unique TODO rows; selected FR/TR/TEST-SESSIONLIFE001..003 contains35 core criteria. Full individual dispositions in trace-review and JSON twin. Verified exported snapshot, not a fresh authoritative MCP readback.

- **C2 FAIL**: Criterion-specific P2 mapping and test coverage are complete. HV07 persists: wrong mappings, absent native label, missing new HV10-HV14 links, and incomplete YAML-document parser oracle. Suite totals do not prove AC coverage. Future P3/P4/P6 obligations are explicitly separated.

- **C3 FAIL**: Applicable implemented P2 behavior satisfies AC independently of suite green. HV14 real dialog/title branches report persisted=true without typed identity; title recovery file deleted.47 explicit audit assertions:39PASS/8FAIL, eight failures all missing/blank typed identities. Ordinary creation metadata now passes all10 compiled server validator checks.

- **D1 FAIL**: P2 definition of done is met holistically. Plan:254-260,371,384,392 requires real metadata/identity/outcome contracts, concrete AC coverage, correct docs, Red-test review and cumulative green plus independent acceptance. Gate and current examples pass, but HV14/HV07/HV15 and unresolved B7/B8 prevent P2 exit. No next-slice acceptance.

- **D2 PASS**: Later-phase scope and individual acceptance discipline are preserved. P3 Stop/deadline, P4 replay/security/provider, P5 activation and P6 durable/integration/provider work remain deferred. PluginIntegration stays excluded. All35 TODO rows remain not-accepted; this review changes no goal or TODO.

## Audit, model proof and limitations

Live root session: C:\Users\kingd\.codex\sessions\2026\09\30\rollout-2026-09-30T18-08-16-01a0f493-6a5b-7851-b061-ef669892b598.jsonl
session_meta line1 identifies codex_exec, CLI0.158.0-alpha.2.1 and this worktree. turn_context line8 identifies model=gpt-6-astra, effort=xhigh, thread01a0f493-6a5b-7851-b061-ef669892b598. The separate codex-auto-review/low approval helper is not the validator.

PAYTON-LEGION2: C:\Users\kingd\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe 7.6.5. Initial sandbox helper failed with OS206 before command execution; approved escalated PowerShell worked. WindowsPowerShell5.1 fallback was authorized but not needed. No Python or Bash used. MCP session-log persistence was explicitly waived; receipt-only persistence applies.

UNKNOWN B7: historical no-Python/MCP-store provenance is not established. UNKNOWN B8: required P2 Red-test AGREE linkage is not established. These remain acceptance blockers. P3/P4/P6 deferred server/provider/Stop/security obligations are not counted as present P2 implementation failures. Exact per-AC dispositions for all35 core criteria are in the JSON twin and trace-review.json.

Current documentation examples independently parse: two bootstrap JSON and three user-guide YAML beginTurn examples, including indented/multidocument blocks, all with valid metadata pairs; schema parses. The remaining regression oracle gap is distinct from current document correctness. Newly added HV10/HV11 suite tests use MCP_PLUGIN_PERSIST_LOG; this reviewer separately checked actual serialized runtime payloads with that seam disabled.

The marker-helper reproduction executed unchanged test code only in a reviewer-owned receipt fixture. It does not establish who created the already-present worktree marker. Three pre-existing dirty tracked files outside receipts retain identical hashes. No implementation, TODO/goal change, merge, commit, push or deploy occurred.

PR state independently retrieved through the GitHub connector: [PR72](https://github.com/sharpninja/McpServer/pull/72), draft/open/unmerged at the requested tip, base develop. No comment/review was posted.

Reproduction commands from the named worktree (PowerShell7, -NoProfile -NonInteractive -File):
- docs/receipts/hv/20260930T2308Z-sessionlife-p2-native-audit.ps1
- docs/receipts/hv/20260930T2308Z-sessionlife-p2-expanded-probes.ps1
- docs/receipts/hv/20260930T2308Z-additional-probes.ps1
- docs/receipts/hv/20260930T2308Z-typed-outcome-probes.ps1
- docs/receipts/hv/20260930T2308Z-supplemental-probes.ps1
- docs/receipts/hv/20260930T2308Z-sessionlife-p2-trace-audit.ps1
- docs/receipts/hv/20260930T2308Z-marker-helper-probe.ps1
- docs/receipts/hv/20260930T2308Z-evaluate-probes.ps1

All evidence hashes and before/after checks are in the JSON twin and final-verification.json. The responseJSONL is a review-time snapshot until the runner writes its final full CLI stream after process exit; evidenceJSONL separately retains the full visible snapshot and complete verdict. Private reasoning is excluded.

markdown: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\hostile-validator-sessionlife-p2-20260930T2308Z.md
json: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\hostile-validator-sessionlife-p2-20260930T2308Z.json
requestJsonl: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20260930T2308Z-sessionlife-p2-contracts-hv.request.jsonl
responseJsonl: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20260930T2308Z-sessionlife-p2-contracts-hv.response.jsonl
evidenceJsonl: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20260930T2308Z-sessionlife-p2-contracts-hv.evidence.jsonl

=== VERDICT JSON ===
{
  "overallVerdict": "DISAGREE",
  "accuracy": 99,
  "completeness": 96,
  "confidence": 99,
  "failList": [
    "HV14: Dialog and title still accept missing or whitespace typed identity and claim primary persistence.",
    "HV07: Per-AC mappings and regression oracles remain incomplete despite 35 populated arrays.",
    "HV15: New test helpers handwrite an operational repository marker and corrupt YAML paths containing space plus hash.",
    "HV09: Completeness96 is below the mandatory98 threshold."
  ],
  "passCount": 12,
  "failCount": 6,
  "unknownCount": 2,
  "tipSha": "8f28c385e1eecce3e07a96d3b0b4f5d1fe6f886c",
  "receiptPaths": {
    "markdown": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\hostile-validator-sessionlife-p2-20260930T2308Z.md",
    "json": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\hostile-validator-sessionlife-p2-20260930T2308Z.json",
    "requestJsonl": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20260930T2308Z-sessionlife-p2-contracts-hv.request.jsonl",
    "responseJsonl": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20260930T2308Z-sessionlife-p2-contracts-hv.response.jsonl",
    "evidenceJsonl": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20260930T2308Z-sessionlife-p2-contracts-hv.evidence.jsonl"
  }
}
