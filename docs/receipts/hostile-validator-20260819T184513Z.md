# Hostile validation receipt

TimestampUtc: 2026-08-19T18:45:13Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: class 1 project implementation closeout (leftover G11 / BUG-TRIAGE-107 on current develop)
ActivePlan: docs/plans/triage-cluster-002.md (S1 closeout-first, G11 only)
TodoId: BUG-TRIAGE-107
SessionId: GrokCode-20260819T183701Z-hostile-g11-closeout
TurnRequestId: req-20260819T183701Z-001-hostile-g11-closeout
TurnId: 42063
GitBranch: develop
GitHead: 0620078259d0be441d953fbaf457b0fdb670dbbc
VendorFixCommit: 0c7844fcb3d4bd923c93dc516580e31e2a2c0d8f (fix(build): version-less stable name for the Node core vendor tarball)

## add-profile

executed: yes
profileFileCount: 18
excludedSkillPorts: add-profile.grok.md
filesRead:
- C:\Users\kingd\.claude\profile\PROFILE.md
- C:\Users\kingd\.claude\profile\user-payton-byrd.md
- C:\Users\kingd\.claude\profile\accuracy-first-verify-sources.md
- C:\Users\kingd\.claude\profile\approve-before-execute.md
- C:\Users\kingd\.claude\profile\philosophical-dialogue-mode.md
- C:\Users\kingd\.claude\profile\log-decisions-as-conclusions.md
- C:\Users\kingd\.claude\profile\session-turn-title-summary.md
- C:\Users\kingd\.claude\profile\never-skip-explicit-actions.md
- C:\Users\kingd\.claude\profile\adversarial-review-global.md
- C:\Users\kingd\.claude\profile\bring-the-receipts.md
- C:\Users\kingd\.claude\profile\hostile-on-goal-state.md
- C:\Users\kingd\.claude\profile\hostile-ops-vs-requirements.md
- C:\Users\kingd\.claude\profile\hostile-phase-gates.md
- C:\Users\kingd\.claude\profile\lab-authorization.md
- C:\Users\kingd\.claude\profile\no-attitude-honesty-tell.md
- C:\Users\kingd\.claude\profile\no-python-lab.md
- C:\Users\kingd\.claude\profile\no-shortcuts-precision-over-convenience.md
- C:\Users\kingd\.claude\profile\requirement-change-plan-first.md

## Trust bootstrap

Marker: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
Test-MarkerSignature: True (plugins/core/lib-ps/marker-resolver.ps1)
Evidence: docs/receipts/_hv-g11-out/marker-sig-plugin.json
Health GET http://PAYTON-LEGION2:7147/health?nonce=664ac57ea1954d2e8e2b3941537f62bc
HealthStatus: Healthy
NonceEchoOk: True (echo 664ac57ea1954d2e8e2b3941537f62bc)
HealthVersion: 1.4.28+f4060f037e62e64974026aff9d24e11b2f481952
Storage: reachable
MCP_UNTRUSTED: no
Native MCP tools used: sessionlog_open, sessionlog_begin_turn, sessionlog_dialog, sessionlog_query, todo_get. REST GET used only for requirements-by-id after native tools had no get-by-id.

## Classification

Class 1. Closeout of leftover G11 (BUG-TRIAGE-107) on current develop. Surfaces A, B, C, and D all apply. Byrd v4 applies to the shipped product slice. Do not FAIL B2 from FR createdAt versus file mtimes. This review did not mark any MCP TODO done, did not merge, and did not run SyncAgentPlugins.

Original AC under attack (BUG-TRIAGE-107):
- Derive vendored tarball name from packed artifact name or package.json version instead of a constant.
- Optionally assert packed version matches package.json so a future bump cannot silently drift.
- Rename existing vendored files at the next sync (legacy 0.1.0.tgz) is OUT OF SCOPE unless the BUILD still hard-codes a versioned name.

Plan G11 named closeout checks: Build.Tests plus a real npm pack name check. If DISAGREE, only the FAIL list (legacy vendor 0.1.0.tgz rename at next sync only if BUILD still hard-codes a versioned name).

## Session persistence (pre-complete)

sessionlog_open created=true sessionId=GrokCode-20260819T183701Z-hostile-g11-closeout
sessionlog_begin_turn success turnId=42063 status=in_progress planFile=docs/plans/triage-cluster-002.md todoId=BUG-TRIAGE-107
sessionlog_dialog appended 2 items (reasoning + decision)
Live proof: mcpserver__sessionlog_query todoId=BUG-TRIAGE-107 totalCount=1 items[0].sessionId=GrokCode-20260819T183701Z-hostile-g11-closeout turns[0].requestId=req-20260819T183701Z-001-hostile-g11-closeout status=in_progress processingDialog length 2

## Claims

### A. Requested validation

A1 PASS. build/Build.SyncAgentPlugins.cs derives the packed name from plugins/core/lib-node/package.json via ReadNodeCorePackageVersion and fails if npm pack mismatches. Stable vendor file name is version-less sharpninja-mcpserver-plugin-core.tgz. No versioned tarball literal matching sharpninja-mcpserver-plugin-core-digits.digits.digits.tgz remains in that source. The interpolated expectedPackedFileName uses {manifestVersion} for the npm pack output name, then File.Copy to NodeCoreStableVendorFileName.
Evidence: build/Build.SyncAgentPlugins.cs lines 461-514, 577-585. Collector HardcodedVersionedTarballInBuild Count=0 StableConstPresent=true ReadNodeCorePackageVersionPresent=true ExpectedPackedInterpolated=true FailOnMismatchPresent=true BuildFileHardcoded01=false. docs/receipts/_hv-g11-out/collector.json.

A2 PASS. tests/Build.Tests/SyncAgentPluginsVendorNameTests.cs exists (TEST-MCP-194 / TR-MCP-SYNC-001). Independent re-run: Failed 0, Skipped 0, Passed 3, Total 3, ExitCode 0.
Evidence: pwsh docs/receipts/_hv-g11-collect-test.ps1. Filter FullyQualifiedName~SyncAgentPluginsVendorNameTests. TRX docs/receipts/_hv-g11-out/SyncAgentPluginsVendorNameTests.trx outcomes Passed for:
- SyncAgentPlugins_VendorStep_UsesStableVersionlessName
- SyncAgentPlugins_VendorStep_HasNoVersionedTarballLiteral
- SyncAgentPlugins_VendorStep_AssertsPackedVersionAgainstPackageJson
First collector attempt used --no-restore and hit NETSDK1004; that is collector error, not a product test failure. Re-run with restore is the receipt.

A3 PASS. plugins/core/lib-node/package.json version is 0.2.0. Live npm pack --dry-run --json from that directory reports filename sharpninja-mcpserver-plugin-core-0.2.0.tgz version 0.2.0, matching the expectedPackedFileName formula. Build.SyncAgentPlugins.cs has no hard-coded 0.1.0.tgz literal.
Evidence: package.json name=@sharpninja/mcpserver-plugin-core version=0.2.0. collector.json NpmPack.FromLibNodeExit=0 Filename=sharpninja-mcpserver-plugin-core-0.2.0.tgz Version=0.2.0.

A4 PASS. Live MCP todo_get: BUG-TRIAGE-107 Done=false CompletedDate=null DoneSummary=null. PLAN-TRIAGELEFTOVER-001 Done=false CompletedDate=null DoneSummary=null.
Evidence: mcpserver__todo_get both ids. BUG-TRIAGE-107 still lists FunctionalRequirements FR-MCP-TRIAGE-002 and TechnicalRequirements TR-MCP-TRIAGE-004 (generic triage links). Dedicated TR-MCP-SYNC-001 / TEST-MCP-194 exist separately (surface C).

### B. Workspace rules

B1 PASS. Honesty / receipts. Implementer claims A1-A4 match independently re-read source, re-run tests, live npm pack, and live todo_get. No fabricated test counts.

B2 PASS. Byrd v4 for this class-1 closeout of already-shipped code on develop. TEST-MCP-194 exists and the three named methods pass. This is a late closeout review of a shipped slice (0c7844fc), not an inter-phase gate reconstructed from FR createdAt versus file mtimes. S1 is closeout-first with no product code unless DISAGREE; this review did not implement.

B3 PASS. MCP-only storage. TODO and session operations used native MCP tools. Did not read or write todo.yaml or session-log files. Requirements get-by-id used authenticated REST GET /mcpserver/requirements/{type}/{id} only because native MCP tools expose list/create/update, not get-by-id.

B4 PASS. PowerShell only. Collectors are .ps1. No python / python3 / py.

B5 PASS. Review only. Did not mark any MCP TODO done:true. Did not git merge. Did not run SyncAgentPlugins.

### C. Requirements

C1 PASS. Applicable IDs: TR-MCP-SYNC-001, TEST-MCP-194, mapped under FR-MCP-143. Original BUG-TRIAGE-107 description AC is the closeout bar. Live GET /mcpserver/requirements/tr/TR-MCP-SYNC-001 and /test/TEST-MCP-194 returned those records.

C2 PASS. TR-MCP-SYNC-001 body is testable (version-less vendor name, no version in the naming constant, pack-versus-manifest assert). TEST-MCP-194 condition enumerates three source-convention checks. Structured acceptanceCriteria arrays on those two records are empty (store convention for this older pair; the body/condition carry the AC). Original BUG-TRIAGE-107 AC is a subset of that TR/TEST text. FR-MCP-TRIAGE-002 / TR-MCP-TRIAGE-004 on the TODO are generic triage grouping AC and are not the tarball AC; they do not replace TR-MCP-SYNC-001.

C3 PASS. The three TEST-MCP-194 methods cover the three stated TEST bullets. Live npm pack --dry-run covers the plan's real packed-name check and the optional assert that packed version matches package.json 0.2.0. Suite-green-is-not-AC is not used as a substitute: the filter was the named TEST-MCP-194 class.

C4 PASS. Mapping FR-MCP-143 -> TR-MCP-SYNC-001 + TEST-MCP-194 exists (GET /mcpserver/requirements/mapping/FR-MCP-143). Observation, not FAIL: FR-MCP-143 title/body is session-log empty-title persistence (BUG-TRIAGE-086..101), not vendor tarball naming. Plan S0 said reuse existing TESTs for G11 and not duplicate IDs. TR-MCP-SYNC-001 / TEST-MCP-194 are the dedicated existing IDs.

### D. Plan holistically (G11 closeout only)

D1 PASS. Original AC: vendored name is no longer a versioned constant; it is the version-less stable name, with packed name derived from package.json and a loud mismatch failure. Optional pack-versus-manifest assert is present in source and confirmed by live npm pack.

D2 PASS. Plan G11/S1 named checks: Build.Tests packed-name conventions (A2) plus a real npm pack name check (A3). No committed Pester test was required once Build.Tests exist; "Pester/Build.Tests" is satisfied by Build.Tests.

D3 PASS. BUILD does not hard-code a versioned tarball name (A1). Therefore legacy vendor 0.1.0.tgz rename is not a FAIL of shipped source. Observation: the three consumer vendor directories already hold only sharpninja-mcpserver-plugin-core.tgz (40877 bytes, 2026-08-19T13:02:27Z) and package.json dependencies already reference file:vendor/sharpninja-mcpserver-plugin-core.tgz. No leftover 0.1.0.tgz files were found. Next-sync rename is not outstanding on those consumers.

D4 PASS. G11 closeout does not complete PLAN-TRIAGELEFTOVER-001 (master TODO for 27 items). Claim that it remains Done=false is correct. This review does not AGREE S1 as a whole (G1/G2 are out of this brief).

## Explicit FAIL list

(empty)

## Unknown mandatory surfaces

(none)

## Next-sync note (not a FAIL)

None required for G11. Consumer vendor copies are already version-less. BUILD source does not hard-code 0.1.0.tgz.

## OverallVerdict

AGREE

PASS: 17
FAIL: 0
UNKNOWN: 0
N/A: 0

Accuracy: 96 (live source, tests, npm pack, todo_get, requirements GET, sessionlog_query)
Completeness: 94 (G11 only; S1 G1/G2 not in this brief)

## Collectors

- docs/receipts/_hv-g11-collect.ps1
- docs/receipts/_hv-g11-collect-test.ps1
- docs/receipts/_hv-g11-collect-mcp.ps1
- docs/receipts/_hv-g11-collect-sig2.ps1
- docs/receipts/_hv-g11-out/collector.json
- docs/receipts/_hv-g11-out/dotnet-test-summary.json
- docs/receipts/_hv-g11-out/SyncAgentPluginsVendorNameTests.trx
- docs/receipts/_hv-g11-out/mcp-reqs.json
- docs/receipts/_hv-g11-out/marker-sig-plugin.json
