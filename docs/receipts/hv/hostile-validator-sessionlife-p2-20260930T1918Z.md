# P2 SessionLife hostile validation: remediation re-attack

TimestampUtc: 2026-09-30T19:13:51.1591083+00:00
ValidatorIdentity: Codex/gpt-6-astra/xhigh. Work class: project implementation.
Tip: fdd2d83292bdee285589d7d5347b4a0a8c0e214c. PR #72, cursor/sessionlife-p2-contracts-5cb2 -> develop.
RunId: p2-unit-legion-20260930T1932Z.
OverallVerdict: **DISAGREE**. Accuracy **99**, completeness **94**, confidence **99**.
Primary claim counts: **12 PASS / 6 FAIL / 2 UNKNOWN**. Findings are distinct defects and do not define these claim counts.
Add-profile executed first: **21** non-skill profile Markdown files read in full. Hostile-validator A-D skill applied, with the explicit Astra and receipt-only overrides.

## Verified green evidence and scope

Native artifacts independently parsed: Pester **196/0/0**; seven Nuke projects **4251/0/0**; Build.Tests **321/0/0**. All individual results pass; all three command exits are zero. Current compiled validator independently accepts the existing run. This review did not rerun the full suite. Unit counts: Support.Mcp 2878, Client 301, Cqrs 33, Launcher 20, McpAgent 63, Repl.Core 866, QBAgent 90.
All **2228** source hashes match before receipt completion; all eight formerly omitted critical P2 inputs are bound. All native artifact hashes remain unchanged. All 95 inherited P1 gate cases plus the exclusion regression pass. PluginIntegration stays in P6. Frozen history-r1 manifest and all five files match; SHA256 **B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4**.
All **35** TODO acceptance rows remain **not-accepted**. Historical hvAgreeClaimed remains false. No product remediation, deployment, merge, commit, push, or TODO/goal change was made. Writes are confined to docs/receipts/hv, including isolated probe fixtures. Three pre-existing tracked dirty files outside receipts remain preserved.

## Explicit FAIL findings

### HV01 [high] First-persist metadata is still omitted across append/complete/recovery and update fallback.

repl-invoke.ps1:2157,2276,2407-2420,2473; McpPluginShim.psm1:476-481. Real builder probes metadata-appendActions-* and metadata-completeTurn-* omit both fields even with explicit or cached valid values; metadata-updateTurn-none omits instead of exact None. Degraded appendDialog performs one Submit but its envelope also omits both fields. Real compiled ValidateForNewEntry rejects all eight defective captured payloads with planFile is omitted; explicit/cache update payloads pass.

Ordinary first persistence cannot recover these turns through the specified contract. F5 explicit update is repaired and F6 now makes one attempt, but a green fake transport masked invalid payloads. No live-server persistence is claimed by this probe.

Requirements: FR-MCP-SESSIONLIFE-001-AC004; FR-MCP-SESSIONLIFE-002-AC001/003; TR-MCP-SESSIONLIFE-001-AC003; TR-MCP-SESSIONLIFE-002-AC001

### HV02 [high] Durable reopen accepts incomplete or wrong-workspace identity proof.

repl-invoke.ps1:1992-1994 treats empty cached sessionId as matching; beginTurn does not run Assert-ReplCurrentTurnFresh before deciding omission and overwrites marker fields at 2053-2055. begin-without-identity-proof and begin-wrong-workspace-proof both return true, call Submit once, omit metadata, and rewrite local identity/marker state. Original A-to-B same-request probe now rejects correctly.

A persisted flag alone can select durable omission for an unproven exact workspace/agent/session/request, contrary to the locked identity contract.

Requirements: FR-MCP-SESSIONLIFE-002-AC003/004; TR-MCP-SESSIONLIFE-002-AC002

### HV03 [high] Other mutation verbs still accept mismatched requests or mutate before rejecting.

repl-invoke.ps1:2455 runs title mutation before 2463-2466 rejection; failTurn:2543-2562 never checks caller requestId; setTurnTitle:2630-2647 also ignores it. mismatch-failTurn returns true, submits req-R from a req-OTHER call and deletes current-turn.yaml. mismatch-setTurnTitle returns true, calls backend and changes req-R title. mismatch-completeTurn returns false with zero backend calls but cache SHA changes to the wrong caller title. Repaired update/dialog retain identical cache hashes and make zero calls.

A wrong caller can fail, retitle, or modify another turn. Guarding only update/dialog does not satisfy the all-mutation AC.

Requirements: FR-MCP-SESSIONLIFE-003-AC003; FR-MCP-SESSIONLIFE-002-AC004

### HV04 [medium] Recovery artifact diagnostics are still false or missing on actual hook paths.

repl-invoke.ps1:155-159 writes failsafePath as unquoted YAML text. real-hook-degraded-hash-path uses a valid Windows folder containing space+#: retained artifact exists but returned path truncates before # and Test-Path is false. plugin-hook.ps1:812-824 duplicate path emits turn-already-open without degraded flag or recoveryArtifactPath for the still-degraded turn. The normal initial degraded hook path now correctly reports its artifact.

F7 is only partially repaired: operators still cannot locate the retained artifact for supported paths or duplicate hook delivery. This also substantiates the workspace YAML-rule violation.

Requirements: FR-MCP-SESSIONLIFE-001-AC002; TR-MCP-SESSIONLIFE-001-AC002; AGENTS.md rule 12

### HV05 [medium] An identical degraded retry never attempts primary recovery.

repl-invoke.ps1:1501-1504 returns queued before Invoke-ReplRaw when matching failsafe exists. degraded-identical-retry makes one initial failed call; after transport is switched to primary, second begin returns true/queued with zero additional calls and the retained envelope unchanged.

A same-request retry can remain queued indefinitely unless another independent drain path runs; the explicit retry itself does not perform the promised recovery.

Requirements: FR-MCP-SESSIONLIFE-001-AC005; P2 item 1

### HV06 [medium] A successful changed retry leaves the cached turn degraded.

repl-invoke.ps1:1611-1612 marks persisted=true but does not clear degraded; beginTurn:2085 handles only failed persistence. degraded-changed-retry receives primary confirmation and returns persisted, but current-turn still has degraded=true and the old failsafePath.

Cache and primary outcome disagree; subsequent hook/reopen decisions keep using degraded state after confirmation. This is observed state inconsistency, without claiming P6 durable readback.

Requirements: FR-MCP-SESSIONLIFE-001-AC002/005; TR-MCP-SESSIONLIFE-001-AC001/002

### HV07 [high] F10 concrete AC coverage remains incomplete and some executed mappings do not test their criterion.

trace-audit.json: 16/35 selected rows populated; 0/9 TR rows; one nonexistent native label. SessionLogP2Contracts.Tests.ps1:620-628 constructs hookOutput locally instead of invoking Open-PluginTurn; :528-533 regex-matches YAML instead of exhaustive parsing; added envelope fact tests only {}. No new concrete recovery-count/payload or fingerprint negative tests cover the independently reproduced branches. Relevant P2 TR rows and FR-001-AC003/FR-002-AC002 still lack concrete mapping.

Current test counts cannot establish completeness >=98. Counts include some future-phase criteria and are not presented as a product correctness percentage. The specific current P2 gaps above independently block acceptance.

Requirements: TEST-MCP-SESSIONLIFE-001-AC001/002/003; TEST-MCP-SESSIONLIFE-002-AC001/003; plan section 7 Baseline and Traceability

### HV08 [medium] New runtime YAML text mutation violates the explicit workspace rule.

AGENTS.md:28; repl-invoke.ps1:155-159 newly appends a failsafePath line rather than using the YAML object helper. HV04 reproduces path corruption from that exact new branch.

The implementation should use the required object serializer; this review does not implement that remediation.

Requirements: AGENTS.md rule 12; plan:19

### HV09 [gate] Completeness 94 is below the required 98 threshold.

The incomplete concrete mappings, failing cross-verb and identity/recovery branches, fixture-echo proof, and historical review gaps prevent the requested evidence-completeness score. Accuracy is 99, completeness 94, confidence 99.

OverallVerdict must be DISAGREE irrespective of the green gate. Scores are reviewer assessments of supported evidence, not measured percentages of code correctness.

Requirements: Operator 98/98 gate

## Re-attack of prior F1-F10

originalProbe denotes whether the exact old counterexample is repaired. A PASS there does not certify the broader contract; expanded failing branches are named explicitly.

- **F1 PASS** (original probe PASS): Four real-function bad success envelopes reject, retain one failsafe each and clear none.
- **F2 PASS** (original probe PASS): Cross-session queue keeps A and B with two calls/two records; changed processingDialog produces two calls.
- **F3 FAIL** (original probe PASS): Original A-to-B rebind is refused and absence of persisted no longer qualifies; HV02 still disproves exact identity before omission.
- **F4 PASS** (original probe PASS): updateTurn and appendDialog mismatches reject without backend calls or cache mutation. HV03 records separate remaining verbs.
- **F5 PASS** (original probe PASS): Explicit valid update metadata changes both cache and serialized payload; real server validator accepts that pair.
- **F6 FAIL** (original probe PASS): Original missing recovery attempt is repaired: exactly one Submit now occurs. Real payload lacks required metadata and is rejected; HV01.
- **F7 FAIL** (original probe PASS): Initial simple-path hook now reports retained artifact. Actual hash-containing path and duplicate-degraded branches still fail; HV04.
- **F8 PASS** (original probe PASS): Bootstrap complete JSON fences parse; two beginTurn examples. All three guide beginTurn YAML examples, including indented/multidocument examples, parse with explicit None pair. Schema parses. Nonexhaustive test remains under F10.
- **F9 PASS** (original probe PASS): Eight previously missing critical inputs are included; 2228 current hashes match; compiled validator ACCEPTED the fresh run.
- **F10 FAIL** (original probe FAIL): 16 rows populated but material P2 mappings/branches remain absent or falsely represented by unrelated/fixture-only tests; HV07.

## A-D adjudication

- **A1 FAIL**: All prior F1-F10 contracts are fully remediated without weakening the claims. F1/F2/F4/F5/F8/F9 original defects pass. F3 exact A-to-B rejection is repaired, but exact identity proof is incomplete. F6 attempts recovery but omits creation metadata. F7 still misreports a valid hash-containing recovery path and duplicate degraded hook state. F10 concrete coverage remains incomplete. See reattack and findings.

- **A2 PASS**: RunId p2-unit-legion-20260930T1932Z is an accepted fresh unit gate at the requested candidate. Reparsed all 196 NUnit cases and all 4572 TRX outcomes: Pester 196/0/0; seven Nuke projects 4251/0/0; Build.Tests 321/0/0. All command exits 0, no native failed blocks/containers. Direct compiled SessionLifeUnitGateValidator.Validate independently ACCEPTED the existing artifacts. Gate log explicitly names fdd2d832; commit predates execution. This reviewer reparsed, not reran, the full gate.

- **A3 PASS**: PluginIntegration remains excluded from unit inventory and owned by P6. build/Build.Test.cs:14-25 filters PluginIntegration and IntegrationTests; one selected-project array feeds inventory and execution. Actual inventory has seven projects, no PluginIntegration. Plan:295-327,400 retains PluginSessionLogIntegration. Native exclusion regression passes.

- **A4 PASS**: F9 source binding includes the named P2 runtime, orchestration and contract-document inputs. All 2228 source hashes match current files. All eight formerly omitted inputs now exist in source-manifest.json. Roots include plugins/core/lib-ps, tools/validation, docs/context, docs/REPL-USER-GUIDE.md. Exact before/after artifact hashes and validator acceptance are in native-audit.json.

- **A5 PASS**: history-r1 remains frozen and the P1 validator regression stack is not silently broken. history-manifest SHA256 B56354AA46F36AA092AEC7A38E0849353F77A6D34A4C07552FBF5EBC192C6DA4; all five archived hashes match. No diff in frozen history. All 95 inherited gate cases and the PluginIntegration-exclusion case pass (96/0). Remaining P1 fixture diff concerns the temp root; no ignored/skipped case was substituted.

- **A6 FAIL**: P2 completeness reaches 98 through concrete per-AC mappings and covered branches, with acceptance state preserved. Only 16/35 selected FR/TR/TEST-001..003 rows have criterionSpecificExistingTests, including 13/16 FR rows and 0/9 TR rows. One mapped label is absent from native results. Material P2 branches fail independent probes. All 35 TODO acceptance rows are correctly still not-accepted; preserving them does not establish completeness.

- **B1 PASS**: Required add-profile was executed first and all profile files were read. First attempted command read C:/Users/kingd/.claude/skills/add-profile/SKILL.md. Sandbox helper failed with OS error 206; approved unsandboxed pwsh read succeeded. All 21 non-skill profile Markdown files were read in full; truncated middle/root output was reread. Profile filenames and hashes are in trace-audit.json.

- **B2 PASS**: This review uses the authorized evidence shell and no Python. PAYTON-LEGION2; C:/Users/kingd/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe 7.6.5. No Python or Bash execution. rg was unavailable, so PowerShell and git grep were used. The Windows PowerShell 5.1 override was recorded but not needed.

- **B3 PASS**: Receipt-only review persistence follows the explicit operator exception. MCP session-log persistence is expressly waived. Missing AGENTS-README-FIRST.yaml is recorded, not treated as a logging failure. Full current request, visible response/tool stream snapshot, full verdict Markdown and JSON are retained below docs/receipts/hv. Earlier runner request naming 1915Z is preserved; the appended effective request contains the exact current 1932Z brief.

- **B4 PASS**: Live validator is Codex/gpt-6-astra/xhigh. Exact thread 01a0f3a6-12aa-7142-bce0-433e08fa2f03: session_meta line 1 names codex_exec, CLI 0.158.0-alpha.2.1, correct worktree. turn_context line 8 names model gpt-6-astra and effort xhigh. Separate codex-auto-review/low approval helper is not the validator.

- **B5 PASS**: No historical P2 HV AGREE or future-phase completion is invented. p2-contracts-verdict.json retains accepted=false, phaseComplete=false, hvAgreeClaimed=false. Old red and prior DISAGREE evidence is preserved. P0/P1 historical acceptances are not recast as P2 acceptance.

- **B6 FAIL**: Remediation respects the object-first YAML mutation rule. AGENTS.md:28 requires deserialization/mutation/serialization via yaml-object-mutation.ps1. New repl-invoke.ps1:155-159 appends raw failsafePath text. A real hook probe under a valid directory containing space+# produces a truncated, nonexistent recoveryArtifactPath, proving the concrete serialization defect.

- **B7 UNKNOWN**: Historical implementation automation fully complied with no-Python and MCP-only storage rules. p2-contracts-verdict.json explicitly says structuredSerialization=Python json module serializing native objects, as does historical failure evidence. Original author command trace was not independently established. No inference of compliance or fabricated execution is made from narrative alone.

- **B8 UNKNOWN**: Required P2 red-test inter-phase hostile review is established. No linked P2 red-test AGREE receipt was located in tracked P2 plans/receipts or the remediation commit scope. Plan:172,259,384 requires it. Existing P1 review does not cover P2; phaseComplete remains false. This is an evidence gap, not an inference from source or FR timestamps.

- **B9 PASS**: This validator preserved review-only scope. Only receipt scripts, fixture evidence, Markdown/JSON/JSONL under docs/receipts/hv were written. No product remediation, merge, commit, push, deployment, TODO/goal completion, or real MCP storage write. The three pre-existing tracked dirty files remain the only dirty tracked files outside receipts.

- **C1 PASS**: Structured governing requirements, AC and mapping snapshots exist. Acceptance manifest has 109 AC rows, 47 unique mapping edges, exact 35 unique TODO scope entries; selected FR/TR/TEST-SESSIONLIFE-001..003 comprises 35 rows. The snapshot includes explicit text, real assertion, production paths and integration scenario. This is verified exported content, not fresh MCP store readback.

- **C2 FAIL**: Current P2 AC-to-test traceability and criterion-specific tests are complete. F10 persists. TR-001/002 P2 rows lack concrete mappings; FR-001-AC003 and FR-002-AC002 are unmapped despite existing related tests. FR-001-AC004 maps a nonexistent shorthand plus an unrelated envelope test. TEST-001-AC003 maps three behavioral facts instead of inventory assertion; TEST-002-AC003 maps outcome facts instead of manifest completeness assertion. Hook-path test builds its expected object rather than invoking hook. Guide test regex-matches text without parsing all YAML documents.

- **C3 FAIL**: Implemented P2 behavior satisfies the applicable AC, independent of suite green. HV01-HV06 violate first-persist metadata, exact durable identity, no-mutation request mismatch, recoverability and hook-status AC. Captured payloads were also passed to the real compiled server metadata validator. Deferred server readback/provider/security/Stop work is not used as a present P2 failure.

- **D1 FAIL**: P2 definition of done is met holistically. Plan:254-260,371,392 requires real builder/shim metadata and identity coverage, all verb outcomes, correct docs, red-test review and cumulative gate plus independent acceptance. The gate and current examples pass; metadata/identity/recovery/coverage failures and unresolved red-review evidence prevent P2 exit. No next-slice authorization is provided.

- **D2 PASS**: Future-phase scope and acceptance discipline are preserved. P3 Stop/deadline, P4 security/storage, P5 synchronization and P6 durable/provider/integration proofs remain deferred. PluginIntegration is not reintroduced. All 35 TODO acceptance rows remain not-accepted; no goal or TODO was changed.

## Traceability and review limits

The JSON twin contains individual P2 dispositions and evidence notes for all 35 selected FR/TR/TEST-001..003 AC rows. Exact durable server readback, provider/storage/security and Stop/deadline portions assigned to P3/P4/P6 are marked deferred, not failed merely because they are future work. Actual current P2 missing mappings and reproduced failures independently block completeness. The score 94 is a conservative assessment of supported acceptance evidence, not the fraction 16/35 or a measured product correctness percentage.
The transport doubles captured real serialized payloads and allowed deterministic failures without touching real MCP stores. Ten metadata payloads were passed to the actual compiled SessionLogTurnContextValidator: two pass, eight reject for omitted planFile. Real function probes execute unchanged product workflow/cache/builder/shim code; they are not claims of durable server persistence.
Current documents pass independent complete-fence parsing: two bootstrap JSON beginTurn examples, three user-guide YAML examples including indentation and multidocument blocks, and valid JSON schema. The submitted guide test still uses regex assertions, which is a TEST/coverage defect rather than a remaining malformed current example.
UNKNOWNs remain explicit: historical Python/MCP-only automation provenance (B7), and required P2 red-test AGREE linkage (B8). No prior P2 HV acceptance was inferred. Missing runtime marker is not a session-log failure under this pass-specific waiver.

## Model, overrides and durable audit

Live proof: C:\Users\kingd\.codex\sessions\2026\09\30\rollout-2026-09-30T13-49-02-01a0f3a6-12aa-7142-bce0-433e08fa2f03.jsonl
session_meta line 1: thread 01a0f3a6-12aa-7142-bce0-433e08fa2f03, codex_exec, CLI 0.158.0-alpha.2.1, requested worktree. turn_context line 8: model=gpt-6-astra, effort=xhigh. The separate codex-auto-review/low approval process is not this validator.
Operator overrides applied: Astra instead of Grok; PowerShell 5.1 allowed if necessary on PAYTON-LEGION2; receipt-only persistence allowed; review-only; no TODO closure; AGREE only at 98/98 and all applicable A-D PASS. Actual shell was Codex-runtime pwsh 7.6.5. No Python was run.
The existing request JSONL initially named RunId 1915Z. Its historical line is preserved and an exact current user-message record naming 1932Z is appended. The visible response/tool stream and full verdict are retained in both the evidence JSONL and response JSONL; runner completion may append its final CLI events. Private reasoning is excluded from the reviewer-created snapshot.
Correction: an early commentary repeated prior-run Nuke total 4250; final verified sum is 4251. No report was changed to obtain acceptance.

Evidence commands: pwsh -NoProfile -NonInteractive -File docs/receipts/hv/20260930T1918Z-sessionlife-p2-native-audit.ps1; same for sessionlife-p2-probes.ps1, sessionlife-p2-expanded-probes.ps1 and sessionlife-p2-trace-audit.ps1. Each finished successfully. Expanded probe fixture directory: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20260930T1918Z-probe-fixtures-2dc8f2d0aa444ce09b2de49abccf7d34

- docs/receipts/hv/20260930T1918Z-sessionlife-p2-native-audit.json | SHA256 C691CAE97F0010FD5221808357E884F94E838D33B002BE672EF80E827C5D55F6
- docs/receipts/hv/20260930T1918Z-sessionlife-p2-probes.json | SHA256 4B38B6FA47EFF1208F5F2F12B46E9B833903FCAC23A03FFC87E9E915E7F15C2F
- docs/receipts/hv/20260930T1918Z-sessionlife-p2-expanded-probes.json | SHA256 7ACEC7814CB8BDEBED40973D0F2ED44E068363AE5D2EB3EBCB6E055256BE6623
- docs/receipts/hv/20260930T1918Z-sessionlife-p2-real-validator.json | SHA256 CC2F915959BF342FB68CCDC8BFC90C6B330C410FA2B9830307C7813546DAB46D
- docs/receipts/hv/20260930T1918Z-sessionlife-p2-trace-audit.json | SHA256 6CC6F3D5C2317795177EAC11B60C8C6D8D705AF4D83CEE0A7B48E0479EDFE6AE

Request JSONL: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv/20260930T1918Z-sessionlife-p2-contracts-hv.request.jsonl
Response JSONL: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv/20260930T1918Z-sessionlife-p2-contracts-hv.response.jsonl
Visible evidence JSONL: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv/20260930T1918Z-sessionlife-p2-contracts-hv.evidence.jsonl

=== VERDICT JSON ===
{
  "overallVerdict": "DISAGREE",
  "accuracy": 99,
  "completeness": 94,
  "confidence": 99,
  "failList": [
    "HV01: First-persist metadata is still omitted across append/complete/recovery and update fallback.",
    "HV02: Durable reopen accepts incomplete or wrong-workspace identity proof.",
    "HV03: Other mutation verbs still accept mismatched requests or mutate before rejecting.",
    "HV04: Recovery artifact diagnostics are still false or missing on actual hook paths.",
    "HV05: An identical degraded retry never attempts primary recovery.",
    "HV06: A successful changed retry leaves the cached turn degraded.",
    "HV07: F10 concrete AC coverage remains incomplete and some executed mappings do not test their criterion.",
    "HV08: New runtime YAML text mutation violates the explicit workspace rule.",
    "HV09: Completeness 94 is below the required 98 threshold."
  ],
  "passCount": 12,
  "failCount": 6,
  "unknownCount": 2,
  "tipSha": "fdd2d83292bdee285589d7d5347b4a0a8c0e214c",
  "receiptPaths": {
    "markdown": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv/hostile-validator-sessionlife-p2-20260930T1918Z.md",
    "json": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv/hostile-validator-sessionlife-p2-20260930T1918Z.json",
    "requestJsonl": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv/20260930T1918Z-sessionlife-p2-contracts-hv.request.jsonl",
    "responseJsonl": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv/20260930T1918Z-sessionlife-p2-contracts-hv.response.jsonl",
    "visibleEvidenceJsonl": "F:\\GitHub\\McpServer\\.worktrees\\sessionlife-p2-contracts-5cb2\\docs\\receipts\\hv/20260930T1918Z-sessionlife-p2-contracts-hv.evidence.jsonl"
  }
}
