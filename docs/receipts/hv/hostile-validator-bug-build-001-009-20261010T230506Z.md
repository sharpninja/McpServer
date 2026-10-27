<!-- SPDX-License-Identifier: Apache-2.0 -->
# Hostile validation: BUG-BUILD-001..007 and -009

TimestampUtc: 2026-10-10T23:05:06.4521150Z
Validator: Codex CLI 0.160.1, gpt-6-sol, xhigh. Native proof: /home/sharpninja/.codex/sessions/2026/10/10/rollout-2026-10-10T17-55-21-01a12807-2cd5-7b12-b05e-cd025532437f.jsonl session_meta ordinal 0 and turn_context ordinal 7 (model gpt-6-sol, effort xhigh)
First actions: add-profile skill and 19 non-skill profile Markdown files read in full; main workspace marker read in full; workflow.memory.list returned 13 memories, all read.
Review is read-only except these HV receipts and redaction of the runner response JSONL. No TODO, requirement, triage, or implementer session-log mutation was made.

OverallVerdict: **DISAGREE**. Accuracy **88%**; Completeness **86%**; Confidence **98%**. Claims: **8 PASS, 5 FAIL, 1 UNKNOWN**. The scores reflect a demonstrated configuration precedence defect, a disproven root-cause provenance claim, incomplete done/traceability gates, stale records, and unverified Windows behavior.

## Claims

- **B1 PASS:** a90e8b4c^ .nuke/parameters.json names missing McpServer.sln; HEAD names QBrainAi.sln. git show confirms the schema diff changes four description strings only. The RED assertion and green result are in the P0.1 logs.
- **B2 PASS:** Build.Compile.cs:31-36 preserves the prior enumerated set and OrdinalIgnoreCase order. Build.Restore.cs:16-23 normalizes Solution.GetAllProjects("*") paths and restores projects absent from that set; the independent Linux Compile run restored exactly seven Compatibility projects and built all 53. QBrainAi.sln lists both other --no-restore target projects. Windows separator/case handling follows Path.GetFullPath and OrdinalIgnoreCase; Windows was not run.
- **B3 PASS:** cb8624f7^ walks past only .mcpServer; HEAD steps past any dot-folder. PluginParentResolutionTests covers .claude, .mcpServer, and .worktrees; independent targeted run passed all cases. Only ResolveAgentPluginParentDirectory calls the helper.
- **B4 PASS:** d6cf2bfd changes only BuildTargetTests sample. Build.InstallReplTool.cs:20,48,83-93 looks up QBrainAI.Repl by package id with OrdinalIgnoreCase, separately from the 1.x alias. The pre-fix test failed Expected 6.1.1/Actual null and now passes.
- **B5 FAIL:** ConfigValidator.cs:61,87 keeps one instances dictionary across both roots, so the later root wins. Read-only invocation of the built _build.dll gave QBrainAi then Mcp => legacy:7999, but reverse order => canonical:7147. McpInstanceResolver.cs:35-36 and CanonicalSectionProjectionProvider.cs:29 require QBrainAi to win within a file layer. Existing tests cover only one root at a time. Live canonical-only appsettings and independent ValidateConfig still pass.
- **B6 PASS:** 789a9e91 introduced Bind at ServiceCollectionExtensions.cs:49. The pre-fix trim log has IL2026 at that line and the post-fix log reports 0 warnings after EnableConfigurationBindingGenerator. The full trim theory receipt says 8/8, 0 skips; McpAgent.Tests remains 8 failed/57 passed/0 skipped as in baseline. TR-MCP-QUALITY-001 covers warning remediation.
- **B7 FAIL:** bug-build-007-evidence.log.txt:3,12-13,20 proves a reusable MSBuild node holds both redirected pipes, but the test process elapsed 425 s and node elapsed 729 s. The node predates the test process by about 304 s, contradicting the assertion that this child build spawned it. The claimed idle-node intermittency mechanism is not established by that snapshot.
- **B8 UNKNOWN:** BoundedProcess.cs:52-106 bounds child exit and post-exit drain, drains both streams concurrently, and returns captured output; six call sites use it. The Linux targeted tests passed 6/6. This Linux host cannot verify whether Start-Process -NoNewWindow inherits both redirected handles in the Windows AC-001 test, or whether the helper and --disable-build-servers behave without race/leak on windows-latest. No Windows receipt is provided.
- **B9 PASS:** 704d5a34 keeps the detached cmd.exe branch on Windows and calls Directory.Delete off Windows. The pre-fix Linux cleanup test failed after 30 s; post-fix passed. The 365-case receipt reports no new trim tree (46 before/after), with the two checksum failures attributed to BUG-BUILD-008.
- **B10 PASS:** P0.1 RED and green logs show assertion failures then passes for 001/002. The 003/005/007/009 RED logs likewise show pre-fix assertion failures and subsequent green. The 004 and 006 RED cases appear in the earlier full Build.Tests baseline, before their respective commits. No skips in the targeted green runs.
- **B11 FAIL:** Independent filtered Build.Tests passed 27/27, 0 skips; independent Compile and ValidateConfig exited 0. Receipt full Build.Tests completed but Failed 2, Passed 363, Skipped 0. BUG-BUILD-001 and -002 live TODOs explicitly leave their full Build.Tests zero-failure infrastructure gate undone, so the batched TODO-done gate is open. BUG-BUILD-008 and all 17 BUG-BASELINE reports/TODOs were verified filed and tracked; their defects are not scored here.
- **B12 FAIL:** Live MCP mappings link NUKE-002/-006/-007/-008/-009/-010 to FR-MCP-BUILD-001, with matching ACs/evidence and no TEST mapping; QUALITY-001 maps to FR-MCP-139 and TEST-MCP-AIUNIT-002. But every reviewed BUG-BUILD TODO has technicalRequirements: [] in the live store, so none is structurally attached to its required TR despite prose references.
- **B13 PASS:** All added files have SPDX-License-Identifier: Apache-2.0; new public test classes/methods and build members have XML docs. Directory.Build.props enables warnings-as-errors globally and overrides GenerateDocumentationFile=false for tests; this does not break the build. Only a property, not a package version, changed in the sole csproj diff. All ten reviewed commits have Signed and Co-Authored-By lines.
- **B14 FAIL:** 7d0c299e, BUG-BUILD-007, TR-MCP-NUKE-009 notes, and the implementer session turn repeat the disproven spawned-by-child account. BUG-BUILD-003..006 TODO descriptions say these failures do not block A1, contrary to p01-baselines receipt line 30 and the stated P0.1 rule. BUG-BUILD-009 TODO still asks removal of 38 trees/4.0 GB though its later task and receipts say 46/5.0 GB. These are material audit-record mismatches.

## Material findings and required changes

- **F-B5 (B5; Implementer edit):** build/ConfigValidator.cs:61,87; tests/Build.Tests/ConfigValidatorTests.cs:57. The validator chooses whichever of QBrainAi or Mcp occurs last for an overlapping instance, unlike the server canonical-over-legacy precedence. Evidence: Read-only reflection call: canonical-first => legacy:7999; legacy-first => canonical:7147. Server precedence is declared at McpInstanceResolver.cs:35-36. To pass: Parse both roots into separate maps, merge per the server precedence regardless of document order, and add a dual-root regression test in both orders.
- **F-B7 (B7; Implementer edit):** docs/receipts/_bug-build-003-009-20261010T224959Z/bug-build-007-evidence.log.txt:3,20; commit 7d0c299e; BUG-BUILD-007; TR-MCP-NUKE-009. The asserted worker provenance and proposed cause of intermittency exceed the evidence and the provenance assertion is contradicted by elapsed time. Evidence: The worker was alive 729 s while the test process was alive 425 s; it could not have been spawned by that test child. Pipe ownership is proven at lines 12-13. To pass: Correct commit/receipt/TODO/TR/session-log wording to say an older reusable node held the pipes; remove or separately prove the spawn and idle-node explanation. Preserve the valid held-pipe observation.
- **F-B11 (B11; Operator decision):** BUG-BUILD-001 and BUG-BUILD-002 implementationTasks; bug-build-009-buildtests.log.txt:25. The full Build.Tests zero-failure gate required by the two TODOs is still open. Evidence: Full suite Failed 2, Passed 363, Skipped 0; the two failures are tracked as BUG-BUILD-008. To pass: Decide BUG-BUILD-008 sync timing: sync in P0 and rerun Build.Tests to 0 fail/0 skip, or explicitly approve deferring/revising the two TODO exit gates and P0.1 rule until A9. No implicit waiver.
- **F-B12 (B12; Implementer edit):** Live TODOs BUG-BUILD-001..007 and BUG-BUILD-009, technicalRequirements field. All eight TODOs have an empty technicalRequirements array despite required TR accountability. Evidence: workflow.todo.get returned technicalRequirements: [] for every reviewed TODO; requirement mappings themselves exist. To pass: Set technicalRequirements to TR-MCP-NUKE-002 for 001/002, -006 for 003, -007 for 004, -008 for 005, TR-MCP-QUALITY-001 for 006, -009 for 007, and -010 for 009; read back.
- **F-B14a (B14; Implementer edit):** BUG-BUILD-003..006 descriptions; docs/receipts/p01-baselines-20261010T215349Z.md:30. The TODOs say their Build.Tests failures do not block A1, while the P0.1 baseline receipt and operator brief require each baseline failure fixed before A1. Evidence: Live TODO descriptions say "does not block slice A1"; baseline receipt line 30 says every failure above must be fixed before A1. To pass: Correct those TODO descriptions to distinguish infrastructure from unit tests without declaring an A1 exemption.
- **F-B14b (B14; Implementer edit plus operator decision):** BUG-BUILD-009 implementationTasks. The pending cleanup task still names 38 leaked directories/4.0 GB after the later gate measured 46/5.0 GB. Evidence: BUG-BUILD-009 fix task and commit 704d5a34 state 46/5.0 GB; earlier cleanup task remains 38/4.0 GB. To pass: Update the TODO to the latest verified count (or a dynamic inventory). Operator decides whether to remove the existing trees now or track cleanup separately.

## UNKNOWN

- **B8:** The Linux tests demonstrate the local process bounds, but Windows handle inheritance for `Start-Process -NoNewWindow`, the process lifetime edges, and `--disable-build-servers` behavior in windows-latest were not observed in a Windows gate. Run the relevant Build.Tests cases on windows-latest and retain the output; if AC-001 does not create held pipes there, replace it with a deterministic Windows fixture.

## Commands and receipts

- Exit 0: `dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter "FullyQualifiedName~NukeParametersTests|FullyQualifiedName~BuildProjectSetTests|FullyQualifiedName~PluginParentResolutionTests|FullyQualifiedName~ConfigValidatorTests|FullyQualifiedName~BoundedProcessTests|FullyQualifiedName~TrimArtifactCleanupTests|FullyQualifiedName~InstallReplTool"` — Failed 0, Passed 27, Skipped 0
- Exit 0: `pwsh.exe -NoProfile -NonInteractive -File ./build.ps1 Compile` — Restore succeeded 0:14; Compile succeeded 2:34; 53 project builds and no NETSDK1004
- Exit 0: `pwsh.exe -NoProfile -NonInteractive -File ./build.ps1 ValidateConfig` — Validated 2 instances
- Exit 0: `Read-only reflection invocation of ConfigValidator.ParseInstances from build/bin/Debug/_build.dll with both roots in both orders` — Canonical-first -> legacy:7999; legacy-first -> canonical:7147
- Exit 2: `git diff --check a90e8b4c^ HEAD` — Whitespace notices in committed raw logs/CRLF files only; not scored as material
- Full Build.Tests and trim theory were assessed from the specified receipts, not rerun: docs/receipts/p01-baselines-20261010T215349Z.md and docs/receipts/bug-build-003-009-20261010T224959Z.md with their raw-log directories.
- Runner request JSONL: /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T225520Z-bug-build-001-009.request.jsonl
- Runner response JSONL: /home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T225520Z-bug-build-001-009.response.jsonl (workspace key redacted in-place; zero remaining occurrences at receipt write).
- Reviewer MCP turn: Codex-20261010T004320Z-plugin-session / req-20261010T225609Z-hv-bug-build.

## Classification

Implementer fixes: B5: correct dual-root precedence and add a regression test; B7/B14: correct worker provenance and unsupported intermittency claims across records; B12: populate eight TODO technicalRequirements links; B14: correct stale A1 and leaked-tree TODO text; B8: obtain a windows-latest run or equivalent direct Windows proof for the process helper.
Operator decisions: B11: BUG-BUILD-008 sync in P0 versus explicit deferred/waived full-suite exit gate at A9; B14: remove the existing trim trees now versus track cleanup as separate deferred work.

## Verdict JSON

```json
{
  "overallVerdict": "DISAGREE",
  "accuracy": 88,
  "completeness": 86,
  "confidence": 98,
  "passCount": 8,
  "failCount": 5,
  "unknownCount": 1,
  "failList": [
    "B5",
    "B7",
    "B11",
    "B12",
    "B14"
  ],
  "unknownList": [
    "B8"
  ],
  "implementerFixes": [
    "B5: correct dual-root precedence and add a regression test",
    "B7/B14: correct worker provenance and unsupported intermittency claims across records",
    "B12: populate eight TODO technicalRequirements links",
    "B14: correct stale A1 and leaked-tree TODO text",
    "B8: obtain a windows-latest run or equivalent direct Windows proof for the process helper"
  ],
  "operatorDecisionFails": [
    "B11: BUG-BUILD-008 sync in P0 versus explicit deferred/waived full-suite exit gate at A9",
    "B14: remove the existing trim trees now versus track cleanup as separate deferred work"
  ],
  "headSha": "e0cb480e9b1ba3b46c74a6ac7b10d13e82be9a24",
  "receiptPaths": [
    "/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-bug-build-001-009-20261010T230506Z.md",
    "/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/hostile-validator-bug-build-001-009-20261010T230506Z.json",
    "/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T225520Z-bug-build-001-009.request.jsonl",
    "/home/sharpninja/github/McpServer/.claude/worktrees/session-20261009/docs/receipts/hv/20261010T225520Z-bug-build-001-009.response.jsonl"
  ],
  "reviewerSessionId": "Codex-20261010T004320Z-plugin-session",
  "reviewerRequestId": "req-20261010T225609Z-hv-bug-build"
}
```

## MCP persistence proof

Codex plugin updateTurn returned persisted=true, degraded=false, queued=false. Read-only client.SessionLog.QueryAsync found reviewer turn req-20261010T225609Z-hv-bug-build in session Codex-20261010T004320Z-plugin-session with a 21,372-character turn block containing the full DISAGREE verdict and B14 finding (status in_progress before terminal completion).
Terminal proof: completeTurn returned persisted=true, degraded=false, queued=false; a subsequent read-only server query found status=completed and the full verdict plus the persistence paragraph (21,777-character turn block).
