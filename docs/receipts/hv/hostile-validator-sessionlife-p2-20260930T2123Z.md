# P2 SessionLife hostile validation: remediation re-attack

TimestampUtc: 2026-09-30T21:43:16.5756753+00:00
ValidatorIdentity: Codex/gpt-6-astra/xhigh. Work class: project implementation.
TipSha: 419e0e40ca60392438001064208fa7306b9c1011. RunId: p2-unit-legion-20260930T205133Z.
OverallVerdict: **DISAGREE**. Accuracy **99**, completeness **95**, confidence **99**.
Primary A-D claims: **13 PASS / 5 FAIL / 2 UNKNOWN**. Findings are separate from primary claim counts.
Add-profile executed first: **21 non-skill profile files read in full**. All pass-specific operator overrides recorded in JSON twin.

## Verified repairs and gate

Original HV01-HV06 counterexamples are repaired. First-persist explicit/cache/None metadata and degraded404 recovery now produce valid pairs; all 10 captured payloads pass the actual compiled server ValidateForNewEntry. Real hook probes locate retained artifacts, including a space+# path and duplicate degraded delivery. Identical degraded retry calls primary again; primary success clears degraded markers. Expanded identity, omission and mutation-order branches still fail below.

Reparsed native gate: Pester **203/0/0**, seven Nuke unit projects **4252/0/0**, Build.Tests **321/0/0**. All canonical individual outcomes pass. Independent compiled validator **ACCEPTED**. All **2228** source hashes and all gate artifact hashes remained unchanged. Seven-project inventory excludes PluginIntegration. Frozen history-r1 and all five file hashes match; 95 inherited P1 validator cases plus exclusion regression pass.

This is a recovered gate: original Build.Tests hung; a preserved blame run aborted with 319 passes and a Failed summary; final full retry passed 321. A validator retry failed for a missing valid UTC start, then the corrected target accepted at 21:22:45Z. Failed diagnostics are not counted as accepted reports. This validator reparsed the complete suite rather than rerunning it.

## Explicit FAIL findings

### HV10 [high] Durable update, appendActions, and completeTurn replace omitted metadata with cached values or None.

repl-invoke.ps1:1433-1455,2232-2235,2470-2488,2541-2547. additional-probes.json durable-omission-* each submits planFile=None and todoId=None on an already persisted exact turn whose caller omitted both fields. Resolver has no durability branch.

Omitted metadata is turned into an explicit replacement. Existing server metadata is no longer protected by the documented omission contract.

Requirements: FR-MCP-SESSIONLIFE-002-AC002; FR-MCP-SESSIONLIFE-002-AC003; TR-MCP-SESSIONLIFE-002-AC001; TR-MCP-SESSIONLIFE-002-AC003

### HV11 [high] Explicit append metadata is not cached, and the following complete submits the stale pair.

repl-invoke.ps1:2232-2235 and 2539-2547. additional-probes.json explicit-append-then-complete sends new-plan.md/BUG-TRIAGE-245, retains p2.md/BUG-TRIAGE-246 in current-turn, then sends p2.md/BUG-TRIAGE-246 on complete. Real builder/shim and actual YAML cache are used.

A subsequent ordinary mutation reverses a successful explicit metadata change. This contradicts the requirement that explicit metadata updates both cache and payload.

Requirements: FR-MCP-SESSIONLIFE-002-AC003; TR-MCP-SESSIONLIFE-002-AC001

### HV12 [high] Workspace identity proof remains fail-open when absent, or bypassed for degraded reopen.

repl-invoke.ps1:1814-1824,1876-1880,2056-2065,2125-2129. begin-missing-marker-proof returns true, submits once with metadata omitted, and manufactures current marker proof. begin-wrong-marker-degraded returns true, submits once, and overwrites a wrong-workspace marker. Original empty-session and nondegraded wrong-marker probes now correctly reject.

A persisted flag is still enough to select omission with incomplete workspace identity, and a degraded turn can migrate its workspace proof before persistence.

Requirements: FR-MCP-SESSIONLIFE-002-AC003; FR-MCP-SESSIONLIFE-002-AC004; TR-MCP-SESSIONLIFE-002-AC002

### HV13 [medium] Request mismatches still mutate cache through freshness processing before the request guard.

repl-invoke.ps1:1880,2410-2418,2514-2529,2597-2602,2702-2707. All six mismatch-missing-marker-* probes reject with zero backend calls, but current-turn SHA256 changes because freshness writes marker fields before checking req-OTHER. Fully seeded original mismatch probes keep identical hashes.

The strict no-cache-mutation contract on mismatched requests is not met. This finding does not claim that the wrong title or failure reaches the server in the repaired seeded-cache case.

Requirements: FR-MCP-SESSIONLIFE-003-AC003; FR-MCP-SESSIONLIFE-002-AC004

### HV14 [high] Dialog and title mutations infer primary persistence from transport success without checking the typed result.

repl-invoke.ps1:2332-2338,2753-2769. typed-outcome-probes.json: appendDialog and setTurnTitle accept a typed response naming sessionB/req-OTHER and emit persisted=true for sessionA/req-R. title-retitled-false also emits persisted=true and deletes its write-ahead artifact. Expected typed response contracts: SessionLogClient.cs:131/264; SessionLogController.cs:247/353.

A wrong-identity or negative title result becomes a false primary-success receipt. The title recovery copy is removed without confirmation. These are local contract probes, not a claim of observed production-server corruption.

Requirements: FR-MCP-SESSIONLIFE-003-AC001; FR-MCP-SESSIONLIFE-003-AC002; FR-MCP-SESSIONLIFE-003-AC006; TR-MCP-SESSIONLIFE-003-AC001; TR-MCP-SESSIONLIFE-003-AC002

### HV07 [high] Filled criterionSpecificExistingTests arrays still do not establish criterion-specific coverage.

acceptance-manifest.json:348,558,1744,2917. All 35 selected rows are populated, but FR-001-AC003 maps retry/path tests that never test complete query fallback; FR-002-AC002 maps only identity-proof rejection rather than raw metadata/canceled validation; TR-001-AC003 maps generic outcome/path/retry cases rather than degraded 404 recovery counts. The nonexistent native label SessionLogLifecycle: degraded 404 queues session_dialog remains. TEST-002-AC004 maps docs and a duplicate hook test instead of cumulative gate verification. SessionLogP2Contracts.Tests.ps1:528-532 regex-matches guide text instead of parsing every YAML document.

The count 35/35 is metadata population, not AC coverage. Added cache-metadata test claims recovery submit in its name but invokes only appendActions and completeTurn (634-647). The path test seeds failsafePath through Write-McpYamlObject itself (699-718); our independent hook probe verifies the actual runtime path, but the mapped test does not protect it. Existing valid related tests should be mapped to the correct AC, with missing branch assertions supplied by the implementer.

Requirements: TEST-MCP-SESSIONLIFE-001-AC001; TEST-MCP-SESSIONLIFE-001-AC003; TEST-MCP-SESSIONLIFE-002-AC001; TEST-MCP-SESSIONLIFE-002-AC003; TEST-MCP-SESSIONLIFE-002-AC004; plan section 7 Baseline and Traceability

### HV09 [gate] Completeness 95 is below the mandatory 98 threshold.

Remaining reproduced metadata/identity/outcome defects, misleading criterion mappings, and unresolved historical inter-phase evidence prevent >=98 completeness. Accuracy=99, completeness=95, confidence=99.

DISAGREE is required. No TODO, goal, requirement, plan completion, merge, or next-slice acceptance is authorized.

Requirements: Operator 98/98 acceptance rule

## Prior HV01-HV09 re-attack

- **HV01 PASS**: Nine explicit/cache/None metadata cases and degraded404 recovery submit capture valid plan/todo pairs. All10 pass compiled ValidateForNewEntry. Recovery does exactly one Submit before successful retry or retained dialog. New omission/cache defects are HV10/HV11.
- **HV02 FAIL**: Empty cached sessionId and explicit wrong-marker durable reopen now reject with zero calls. Missing-marker proof and wrong-marker degraded reopen still accept (HV12).
- **HV03 FAIL**: All six fully seeded mismatch cases return false with zero calls and identical hashes. Missing marker fields cause freshness to mutate cache before rejection (HV13).
- **HV04 PASS**: Actual hook body with actual begin/cache/builder emits existing recoveryArtifactPath for first degraded, duplicate degraded, and space+# directory cases. Duplicate also exposes degraded=true.
- **HV05 PASS**: Identical degraded begin retry makes one additional primary Submit, returns persisted and clears matching failsafe. Initial and retry calls total 2.
- **HV06 PASS**: Primary success clears degraded and failsafePath in identical and changed begin retries. Changed retry leaves its older distinct envelope retained; no broader replay-order certification is claimed.
- **HV07 FAIL**: 35/35 populated concrete-test arrays still include unrelated assertions and one absent native label. See mapping examples and trace audit.
- **HV08 PASS**: The specific runtime raw YAML append is replaced by Read-McpYamlObject/Write-McpYamlObject and passes real hash-path probe.
- **HV09 FAIL**: Completeness 95 remains below 98.

## A-D adjudication

- **A1 FAIL**: All HV01-HV09 contracts and prior failed claims are remediated without weakening. Original HV01-HV06 probes now pass, but expanded HV02/HV03 branches still fail, HV07 remains, and new HV10/HV11/HV14 disprove P2 contract completeness. See exact prior finding dispositions and receipt-only probes.

- **A2 PASS**: Named source-bound cumulative unit run has accepted green reports at the requested tip. Independent NUnit/native/TRX parse: Pester 203/0/0; seven Nuke projects 4252/0/0; Build.Tests 321/0/0. Direct compiled SessionLifeUnitGateValidator.Validate ACCEPTED. All canonical report result nodes pass and native failed blocks/containers are 0. Three command records exit 0. This is a recovered run with a preserved hang/blame attempt and successful retry, not an uninterrupted original invocation or a fresh suite run by this reviewer.

- **A3 PASS**: PluginIntegration remains deliberately excluded and assigned to P6. build/Build.Test.cs:14-25 uses one selected-project array for inventory and execution. Actual inventory has seven unit projects and no PluginIntegration. Plan:295-327,400 retains PluginSessionLogIntegration. Build.Tests exclusion regression passes.

- **A4 PASS**: Gate source binding includes P2 runtime and documentation inputs. All 2228 source hashes match current files. repl-invoke, McpPluginShim, plugin-hook, resolve-cache-dir, gate orchestration, module-bootstrap, user guide and schema are present. build/SessionLifeUnitGateManifest.cs roots include plugins/core/lib-ps, tools/validation and docs inputs. No bound-source mutation was performed.

- **A5 PASS**: history-r1 is frozen and the inherited P1 stack is not silently broken. All five archived hashes match history-manifest, whose SHA256 is B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4. Current canonical Build.Tests has 95 SessionLifeUnitGate cases plus the exclusion regression passing. No frozen-history diff.

- **A6 FAIL**: Completeness reaches 98 through concrete AC coverage while preserving all 35 not-accepted TODO rows. All 35 selected AC arrays are populated, but material wrong mappings and uncovered failing branches remain (HV07,HV10-HV14). All 35 TODO rows do remain not-accepted; preserving state does not establish acceptance coverage.

- **B1 PASS**: Required add-profile was executed first with every non-skill profile read in full. First action attempted the specified Claude add-profile SKILL.md. Sandbox error 206 and failed PowerShell connector delayed reading; escalated Codex-runtime pwsh succeeded. All 21 non-skill Markdown files read; truncated middle/PROFILE output reread completely. Hash inventory is in trace-audit.json.

- **B2 PASS**: Review evidence uses the authorized shell and no Python. PAYTON-LEGION2, Codex-runtime pwsh 7.6.5. rg absent; used PowerShell and git grep. A failed Node runtime read attempt never executed file code; no JSON/YAML was built through Node. No Python/Bash execution. The authorized Windows PowerShell 5.1 fallback was not needed.

- **B3 PASS**: Receipt-only persistence complies with the pass-specific exception. MCP session-log persistence explicitly waived by operator. Missing runtime marker recorded, not converted to a log failure. Exact user request, visible message/tool stream snapshot, full Markdown and JSON verdict saved under docs/receipts/hv. Runner owns final CLI stream capture; this review also stores its full verdict in response JSONL.

- **B4 PASS**: Live validator identity is Codex/gpt-6-astra/xhigh. Exact thread 01a0f433-2017-7fb0-a086-e7156f47c7a1. session_meta line 1: codex_exec, CLI 0.158.0-alpha.2.1, requested cwd. turn_context line 8: model=gpt-6-astra, effort=xhigh. Separate guardian codex-auto-review/low session is not this validator.

- **B5 PASS**: Historical HV AGREE and completion are not invented. p2-contracts-verdict.json keeps accepted=false, phaseComplete=false, hvAgreeClaimed=false. Prior red/DISAGREE receipts remain. Live PR 72 is draft/open at 419e0e40 and explicitly does not claim P2 HV acceptance.

- **B6 PASS**: The HV08 failsafePath runtime mutation now uses the required YAML object helper. Complete-ReplBeginTurnAfterPersist:150-169 reads and writes full objects; no raw failsafePath line append. Real Open-PluginTurn probes round-trip a directory containing space+# to an existing artifact. Clear-ReplTurnDegradedMarkers also uses object mutation. This is scoped to the remediation, not certification of every legacy YAML fixture in the repository.

- **B7 UNKNOWN**: Historical implementation automation complied fully with no-Python and MCP-only storage. Historical p2-contracts-verdict.json still declares structuredSerialization=Python json module serializing native objects. This reviewer did not independently establish the original author command stream or live store provenance. The current review used only authorized receipt writes.

- **B8 UNKNOWN**: Required P2 Red-test inter-phase hostile AGREE is established. No linked P2 Red-test AGREE receipt found in current P2 plan/receipts or remediation commit scope. Plan:259,384 requires it. P1 acceptance is not P2 acceptance. No timestamp archaeology was used. Historical gate absence remains an evidence gap.

- **B9 PASS**: Review-only scope and existing dirty work are preserved. Only new receipt scripts, isolated fixtures, audit JSONL and Markdown/JSON under docs/receipts/hv were written. No product remediation, commit/push, merge, deployment, TODO/goal mutation or MCP storage write. Initial unrelated dirty .nuke/build.schema.json and two benchmark JSON files preserved.

- **C1 PASS**: Structured governing FR/TR/TEST/AC and mapping snapshots exist. Acceptance manifest contains 109 AC rows, 47 distinct mapping edges and 35 unique TODOs. Selected SESSIONLIFE FR/TR/TEST001..003 contains 35 criteria with concrete names now populated. This proves exported artifact content; a live MCP requirements/TODO readback was not performed with the marker absent.

- **C2 FAIL**: P2 concrete mappings and criterion-specific coverage are complete. HV07: labels were bulk-added by requirement family without proving the actual criterion. Missing native label remains. Genuine current P2 query-fallback, raw-metadata, bounded-recovery and full-gate mapping defects independently fail this claim, without penalizing P3/P4/P6 deferred AC.

- **C3 FAIL**: Applicable implemented P2 behavior satisfies AC independent of suite green. HV10-HV14: omitted metadata becomes replacement, explicit metadata reverts, workspace proof is insufficient, mismatch checks run after cache mutation, and typed title/dialog results are not validated. Ten ordinary first-persist payloads now pass the real server validator; that repair does not establish the separate omission and identity contracts.

- **D1 FAIL**: The P2 definition of done is met holistically. Plan:254-260,371,384,392 requires real metadata/identity/outcome coverage, correct docs, Red-test review and cumulative green plus independent acceptance. The gate and documents pass; reproduced contract defects, incomplete traceability and missing Red-review linkage prevent P2 exit.

- **D2 PASS**: Future-phase scope and acceptance discipline are preserved. P3 Stop/deadline, P4 replay/security/provider work, P5 activation and P6 real durable/provider/integration proof remain deferred. PluginIntegration was not introduced into unit inventory. All 35 TODO rows remain not-accepted; no goal/TODO closure.

## Traceability and review limits

The JSON twin and trace-audit contain every selected FR/TR/TEST001..003 AC text and mapped native name. All 35 selected arrays are populated and all 35 TODO acceptance rows remain not-accepted. At least one name is absent from the native results; several present names test a different behavior. Concrete examples above fail current P2 scope without demanding completion of future phases. Current bootstrap and user-guide examples independently parse:2 JSON and 3 YAML beginTurn examples, all with nonempty None pairs. The schema parses. Current document correctness does not fix the incomplete regression oracle.

- Real runtime functions and YAML cache/builder/shim invoked with immediate transport doubles, no real MCP-store mutation or server durability claim.
- Full gate independently reparsed, not rerun by this reviewer. Compiled validator independently accepted all final canonical reports.
- Build.Tests first run hung; blame run aborted with 319 passing tests and Failed summary. Final full retry had321/0/0 Completed. Failed validator invocation omitted a valid UTC start; corrected target later accepted. Historical failures retained and not counted as passing reports.
- Original author no-Python/MCP-only provenance and P2 Red-test AGREE linkage unresolved.
- Initial reflection harness used a variable name constrained to string by the dot-sourced script. Renamed to validatorMethodInfo, reran successfully; original harness error output retained and not treated as a product failure.
- MCP marker absent; live requirements/TODO state not queried. Exported acceptance state is the verified artifact.
- Request and visible response snapshots exclude private reasoning. Runner may append its final CLI events after this receipt.

## Model proof and durable evidence

Live session: C:\Users\kingd\.codex\sessions\2026\09\30\rollout-2026-09-30T16-23-06-01a0f433-2017-7fb0-a086-e7156f47c7a1.jsonl
session_meta line 1 identifies codex_exec, CLI 0.158.0-alpha.2.1 and this workspace. turn_context line 8 identifies model=gpt-6-astra, effort=xhigh, thread 01a0f433-2017-7fb0-a086-e7156f47c7a1. The separate codex-auto-review/low guardian is not the validator.

Shell: C:/Users/kingd/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe 7.6.5. Initial sandbox launcher error 206 and failed connector/runtime startup were worked around using approved unsandboxed PowerShell. No Python or Bash ran. Scope remained receipt-only; no merge, remediation, commit, push, deployment or completion-state mutation.

PR state independently read through GitHub connector: [PR 72](https://github.com/sharpninja/McpServer/pull/72), draft/open, head 419e0e40, base develop. No external review/comment was posted.

Reproduction commands (from the named worktree):

- pwsh -NoProfile -NonInteractive -File docs/receipts/hv/20260930T2123Z-sessionlife-p2-native-audit.ps1
- pwsh -NoProfile -NonInteractive -File docs/receipts/hv/20260930T2123Z-sessionlife-p2-expanded-probes.ps1
- pwsh -NoProfile -NonInteractive -File docs/receipts/hv/20260930T2123Z-sessionlife-p2-trace-audit.ps1
- pwsh -NoProfile -NonInteractive -File docs/receipts/hv/20260930T2123Z-additional-probes.ps1
- pwsh -NoProfile -NonInteractive -File docs/receipts/hv/20260930T2123Z-typed-outcome-probes.ps1

- docs/receipts/hv/20260930T2123Z-sessionlife-p2-native-audit.json | SHA256 4645D3555F920ED275CA8B045FC48A62FE9257E59014D0B9DFBD647D1DC27404
- docs/receipts/hv/20260930T2123Z-sessionlife-p2-expanded-probes.json | SHA256 B08BAE099B7261CA86F376030652B00F826DAD92FD67E39F8524969400875AB4
- docs/receipts/hv/20260930T2123Z-sessionlife-p2-trace-audit.json | SHA256 AF8133E00C8149AC36F9AD68ED9B1C47568515B3B7A06938CE0BD5C10910946C
- docs/receipts/hv/20260930T2123Z-additional-probes.json | SHA256 F58A5C338CFDA4BC299B0F6A806ACA3A7A5546F82C985D679132C3114CB56D3A
- docs/receipts/hv/20260930T2123Z-typed-outcome-probes.json | SHA256 5A9989715F3E9AE3B84920E2507BE83E646E4716405FF55AD86F0E279261CBFE
- docs/receipts/hv/20260930T2123Z-typed-outcome-probes-harness-initial.json | SHA256 AA16646DF9A7D5F59F3049CE6E214BE5CEE960F3E14ACD22C74851184EFAE18E
- docs/receipts/hv/20260930T2123Z-final-verification.json | SHA256 BFCF8EC83EBB2EA1248A31751B612335492FED910A39AD5EA957E69A68B9D0D2

markdown: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\hostile-validator-sessionlife-p2-20260930T2123Z.md
json: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\hostile-validator-sessionlife-p2-20260930T2123Z.json
requestJsonl: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20260930T2123Z-sessionlife-p2-contracts-hv.request.jsonl
responseJsonl: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20260930T2123Z-sessionlife-p2-contracts-hv.response.jsonl
evidenceJsonl: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20260930T2123Z-sessionlife-p2-contracts-hv.evidence.jsonl

=== VERDICT JSON ===
{
  "overallVerdict": "DISAGREE",
  "accuracy": 99,
  "completeness": 95,
  "confidence": 99,
  "failList": [
    "HV10: Durable update, appendActions, and completeTurn replace omitted metadata with cached values or None.",
    "HV11: Explicit append metadata is not cached, and the following complete submits the stale pair.",
    "HV12: Workspace identity proof remains fail-open when absent, or bypassed for degraded reopen.",
    "HV13: Request mismatches still mutate cache through freshness processing before the request guard.",
    "HV14: Dialog and title mutations infer primary persistence from transport success without checking the typed result.",
    "HV07: Filled criterionSpecificExistingTests arrays still do not establish criterion-specific coverage.",
    "HV09: Completeness 95 is below the mandatory 98 threshold."
  ],
  "passCount": 13,
  "failCount": 5,
  "unknownCount": 2,
  "tipSha": "419e0e40ca60392438001064208fa7306b9c1011",
  "receiptPaths": {
    "markdown": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\hostile-validator-sessionlife-p2-20260930T2123Z.md",
    "json": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\hostile-validator-sessionlife-p2-20260930T2123Z.json",
    "requestJsonl": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20260930T2123Z-sessionlife-p2-contracts-hv.request.jsonl",
    "responseJsonl": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20260930T2123Z-sessionlife-p2-contracts-hv.response.jsonl",
    "evidenceJsonl": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv\\20260930T2123Z-sessionlife-p2-contracts-hv.evidence.jsonl"
  }
}
