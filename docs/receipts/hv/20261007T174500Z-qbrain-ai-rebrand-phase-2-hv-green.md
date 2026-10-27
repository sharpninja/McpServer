# QBrain.AI rebrand Phase 2 hostile verification after green remediation

- TimestampUtc: 2026-10-07T17:59:25.8387165Z
- ReceiptId: 20261007T174500Z-qbrain-ai-rebrand-phase-2-hv-green
- OverallVerdict: DISAGREE
- Accuracy: 95
- Completeness: 92
- ValidatorIdentity: Codex
- ModelLock: PASS — gpt-6-astra / xhigh, independently verified from this execution's rollout.
- Host: PAYTON-LEGION2
- ReviewedBranch: cursor/qbrain-ai-rebrand-phase2-91dd
- ReviewWorkspace: F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
- LoggingWorkspace: F:\GitHub\mcpserver
- PRState: all eleven OPEN, DRAFT, unmerged, and still at the supplied tips on final recheck.
- Phase2Done: no. This reviews draft contents; it does not authorize merging or claim the plan's merged-PR completion condition.

The claimed all-green remediation is not present in the supplied committed PR set. Cline still fails one test because the required helper exists only as an untracked working-tree file. Grok Bot fixes the previous four manifest mismatches but introduces eight different mismatches by recording CRLF hashes for committed LF blobs. Manager has three failing UI.Core tests and three additional suites blocked before execution. No inherited-failure waiver, skipped-test exception, or test quarantine was applied.

Accuracy and Completeness are reviewer assessments of content readiness, not arithmetic pass percentages or claims that the review itself was incomplete. The explicit Failed 0 / Skipped 0 exit gate is not met.

## Inputs and execution method

Read the supplied green proof and previous hostile receipt:
- F:\GitHub\McpServer\docs\receipts\hv\20261007T174000Z-phase2-green-proof.md
- F:\GitHub\McpServer\docs\receipts\hv\20261007T162924Z-qbrain-ai-rebrand-phase-2-hv-r123b.md

The plan remains unchanged: docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md, SHA-256 6ca0c9e34ec70b906f2538027bf8791c7729fbf8586a2257b76f8f1aec7a57e8. Its lines 576–581 require each repository's existing test command to finish with zero failures and zero skips. The latest operator instruction scopes this review to draft content and expressly prohibits merging.

Every PR head was fetched read-only through gh pr view and matched against the full requested SHA. Tests used fresh git archive snapshots of those exact commits under C:\Users\kingd\AppData\Local\Temp\QBrainHv-green-20261007T174500Z. Untracked working-tree files and existing node_modules were not copied. CORE-MANIFEST validation used raw git cat-file blob bytes, independently of archive line-ending materialization.

PowerShell.MCP launched hidden pwsh.exe -NoProfile -NonInteractive test workers. Versions: PowerShell 7.6.6, Pester 5.7.1, Node 20.20.2, npm 10.8.2, .NET SDK 10.0.401.

Commands/configuration:
- Shell: Invoke-Pester with Run.Path=<snapshot>/tests, Run.PassThru=true, detailed output and NUnit XML. No tag/path/test filters.
- Node plugins: npm ci --no-audit --no-fund; npm run build; npm test -- --runInBand --json --outputFile=<evidence>/<repo>.jest.json. Lifecycle scripts enabled.
- Manager: dotnet test <each of five tests/*.csproj> --configuration Debug --logger "trx;LogFileName=<suite>.trx" --results-directory <evidence> --nologo.
- Manager also: Pester scripts/DeployAllTargets.Tests.ps1; npm ci and npm test in extensions/McpServer-mcp-todo.
- Manager's Markdown.Avalonia submodule was materialized from its pinned commit 4b1e1b2800d66c817b3cea80d115b39ca3cb5149 before Desktop testing. No warning-as-error settings were weakened.

## Full-suite evidence

N/E means tests were not executed because restore or compilation failed; it is not a zero-failure result.

| Repository / suite | Total | Passed | Failed | Skipped | Result / evidence file |
| --- | ---: | ---: | ---: | ---: | --- |
| Grok — Pester tests/ | 29 | 29 | 0 | 0 | PASS; mcpserver-grok-plugin.pester.xml |
| Grok Bot — Pester tests/ | 29 | 29 | 0 | 0 | PASS; mcpserver-grok-bot-plugin.pester.xml |
| Claude Code — Pester tests/ | 36 | 36 | 0 | 0 | PASS; mcpserver-claude-code-plugin.pester.xml |
| Claude Cowork — Pester tests/ | 29 | 29 | 0 | 0 | PASS; mcpserver-claude-cowork-plugin.pester.xml |
| Codex — Pester tests/ | 41 | 41 | 0 | 0 | PASS; mcpserver-codex-plugin.pester.xml |
| Copilot — Pester tests/ | 29 | 29 | 0 | 0 | PASS; mcpserver-copilot-plugin.pester.xml |
| Cline — npm test | 32 | 31 | 1 | 0 | FAIL; mcpserver-cline-plugin.jest.json |
| Cline v2 — npm test | 27 | 27 | 0 | 0 | PASS; mcpserver-cline-v2-plugin.jest.json |
| OpenCode — npm test | 67 | 67 | 0 | 0 | PASS; mcpserver-opencode-plugin.jest.json |
| Manager Core — normal TEMP full rerun | 141 | 141 | 0 | 0 | PASS; Manager.Core.normal-temp.trx |
| Manager UI.Core | 403 | 400 | 3 | 0 | FAIL; McpServerManager.UI.Core.Tests.trx |
| Manager Desktop | N/E | N/E | N/E | N/E | BUILD BLOCKED; McpServerManager.Desktop.Tests.test.log |
| Manager Director | N/E | N/E | N/E | N/E | RESTORE BLOCKED; McpServerManager.Director.Tests.test.log |
| Manager Web | N/E | N/E | N/E | N/E | RESTORE BLOCKED; McpServerManager.Web.Tests.test.log |
| Manager DeployAllTargets — Pester | 3 | 3 | 0 | 0 | PASS; Manager.DeployAllTargets.pester.xml |
| Manager extension — npm test | 11 | 11 | 0 | 0 | PASS; Manager.extension.test.log |
| Tools | N/A | N/A | N/A | N/A | Manifest repository; no existing unit-test command found. Static manifest checks below. |
| Effective executed total | 877 | 873 | 4 | 0 | FAIL; excludes blocked suites and duplicate diagnostic run |

The nine plugin suites alone total 319 tests: 318 passed, 1 failed, 0 skipped. All six Pester runs also report NotRun 0 and Inconclusive 0. The Node plugin runs report zero pending/todo tests. No new skip/exclude directives were found in the changed test files.

The first Manager Core run recorded 140 passed / 1 failed / 0 skipped. Its failing assertion compared TEMP casing: expected F:\GitHub\mcpserver\.mcpServer\tmp while cmd returned F:\GitHub\McpServer\.mcpServer\tmp. Plugin bootstrap had changed the reviewer's inherited TEMP. A second FULL Core suite, with normal Windows TEMP/TMP C:\Users\kingd\AppData\Local\Temp, passed 141/141 without source or test changes. Both runs are retained; the effective table counts only the uncontaminated rerun. This correction does not remove any remaining product/test failure.

## Blocking findings

### HV-GREEN-01 — Cline committed tip still fails

At 7a28d0294797bf643bdd23e3ecb7d03c3ac89473:
- Failing test: "cline memory skill and descriptor exposes executable wrapper scripts for workflow.memory.*".
- tests/memory-skill.test.ts:54 reads hooks/scripts/memory-context.ps1 and receives ENOENT.
- git ls-tree -r <tip> -- hooks returns no tracked helper.
- git status --short --untracked-files=all in F:\GitHub\mcpserver-cline-plugin reports: ?? hooks/scripts/memory-context.ps1.
- npm ci and npm run build both exit 0; the full npm test exits 1 with 31/32 passing.

This directly explains how a mutable working tree can pass while the draft PR still fails. The missing file must be committed into the reviewed PR and the committed-tip full suite rerun. The verifier did not copy that untracked file into the test snapshot or modify the PR.

Evidence: cline-untracked-helper-proof.txt, mcpserver-cline-plugin.jest.json, mcpserver-cline-plugin.test.log and mcpserver-cline-plugin.node.worker.log.

### HV-GREEN-02 — Grok Bot has eight new committed-blob manifest mismatches

At abb746cfdefba7cbcf06724a011b1568487ad155, only 8/16 manifest entries match raw committed bytes. These eight entries mismatch:
- lib/cache-manager.ps1
- lib/classified-error.ps1
- lib/final-response.ps1
- lib/marker-resolver.ps1
- lib/mcp-status.ps1
- lib/yaml-object-mutation.ps1
- skills/handoff/invoke.ps1
- skills/handoff/SKILL.md

For every mismatch, the actual committed blob is unchanged from Grok Bot's base and byte-identical to the corresponding Grok blob. Converting those bytes to CRLF reproduces the manifest's expected SHA-256 exactly. The remediation changed previously correct LF hashes to CRLF hashes; this is a manifest regression, not an unexplained difference in the source logic.

Example: lib/cache-manager.ps1 expects e54877f2c275b47425886058dc818791972e4ea79dae8141d182357e9b64d06e but committed SHA-256 is 96ffe0638c7445868bf10e338ba739bc5c8d5f5344092ab9bde1c9d6dd9e762e.

The previous four mismatches (plugin-hook.ps1, resolve-cache-dir.ps1, Invoke-McpPlugin.ps1, agent-runtime-header.ps1) now match. The broader result is nevertheless FAIL: 136/144 entries across nine plugins; 88/96 across the six shell plugins. Each of the other eight plugin manifests matches 16/16. Six of the eight remaining Grok Bot mismatches are lib entries covered by core-guard.yml's hash loop.

Evidence: raw-blob-manifest-hashes.json contains all expected/actual SHA-256 values; manifest-diagnosis.json proves base/Grok equality and CRLF reproduction. Fix the manifest against committed bytes using the repository's object-first YAML workflow, then repeat the complete check.

### HV-GREEN-03 — Manager zero-failure gate is not satisfied

At 242a8968884ce70f064691f19c77b0920a4598af, the full UI.Core suite has three failures:
- HandlerApiDispatchTests.Handler_AuthorizedPath_InvokesApiClient for AddMemoryCommandHandler: unhandled WorkspaceContextViewModel dependency, tests/.../Handlers/HandlerApiDispatchTests.cs:137.
- The same theory for UpdateMemoryCommandHandler: the same unhandled dependency.
- WebUiHandlerApiDispatchMappingTests.Dispatcher_WebUiBackedRequests_RouteToExpectedApiClients: expected one AddMemoryAsync call, received none, tests/.../Handlers/WebUiHandlerApiDispatchMappingTests.cs:112.

Additional executed commands fail before test execution:
- Desktop: seven nullable compilation errors (CS8618, CS8602, CS8604) in the pinned Markdown.Avalonia/ColorTextBlock.Avalonia TextPointer.cs and CTextBlock.cs, under the repository's warnings-as-errors policy.
- Director: restore reports NU1902 for Microsoft.Build.Tasks.Git 8.0.0 and NU1903 for System.Security.Cryptography.Xml 10.0.8, promoted to errors.
- Web: restore reports NU1902 for AngleSharp 1.4.0, promoted to an error.

These failures/build blockers are not established as rebrand-caused regressions; the failing test/source paths are outside Manager's changed rebrand files. They still prevent the requested full-green exit claim. No baseline-relative exception was used. Restore diagnostics are reported as observed tool output; this review does not independently assess the linked advisories.

Evidence: manager-failures.json, Manager/Core/UI.Core TRX files, and all five McpServerManager.*.Tests.test.log files.

## R1–R3, identity and scope checks

- R1 PASS: re-executed the prior independently written bridge-harness.cjs against the current committed vendor tarballs. All 45 cases pass: Windows/Linux modeled command discovery, new-only, legacy-only, both, neither, explicit command override, explicit binary precedence, and Windows .cmd candidates. Assertions cover resolver result and captured spawn command/arguments/cwd. This is controlled dependency testing, not a claim of actual Linux execution or real REPL launch.
- R2 targeted PASS: ensure-repl.ps1, plugin-env.ps1 and repl-invoke.ps1 match their manifest hashes in all nine plugins, 27/27. Full CORE integrity remains FAIL under HV-GREEN-02.
- R3 identity PASS: Cline tests/plugin-helpers.test.ts:96–101 expects @qbrainai/qbrain-ai-cline-plugin and the identity/status test passes. Cline's full-suite gate remains FAIL under HV-GREEN-01.
- All three Node package.json names match package-lock.json top-level and root-package names. Cline retains mcpserver-cline and adds qbrain-ai-cline, both targeting dist/index.js; root lockfile bin metadata agrees.
- The three vendor tarballs have identical SHA-256 395f140e30b3ace1e1aebffed30e24a0df9a8ad8a34be7d3a0d8192e4d2be1e8. Their computed SHA-512 SRI matches all three lockfile entries.
- All three clean npm ci and build commands exit 0. Each package-lock.json hash is unchanged after install/build/test.
- Inspected QBRAINAI_* projection: it takes precedence over MCP_* and excludes the MCP_UNTRUSTED sentinel. Node entry points load the alias module. Shell ensure-repl checks qbrain-ai-repl then mcpserver-repl, retaining the old package fallback and old repository URL.
- Tools has all nine old-name manifests and all nine new-name aliases. The eight old-name manifests that existed at base are byte-unchanged; the ninth old-name Grok Bot manifest is added with its alias. All 18 parameterSchema strings parse as JSON objects; repository URLs retain the old GitHub names.
- Manager changes reviewed are display/UI text and related expectations. Existing namespaces, application IDs, authentication realm/client IDs and repository links are retained.
- Diff checks find no QuadBrain/QBAgent/qbagent name changes, no changed private-profile files, and no added sharpninja/QBrainAi or sharpninja/qbrain-ai-* repository URLs. No repository rename, hostname change, deployment, Phase 3 operation or Phase 4-L operation was performed.
- The existing Bash/Bats suites were not run in this PowerShell-only review; the requested shell full-suite evidence is Pester tests/. No cross-platform runtime certification is claimed.
- Live statusCheckRollup reports failing core-integrity jobs for the six shell PRs. Two attempted gh run view --log-failed reads returned "log not found"; CI root causes are not inferred from the job names. The local raw-blob and suite findings above are independently reproduced.

## Exact tips reviewed

| Repository | Draft PR | Reviewed head SHA |
| --- | --- | --- |
| mcpserver-grok-plugin | https://github.com/sharpninja/mcpserver-grok-plugin/pull/6 | b2a0ec7e03869f0f2863728ccdc7da75ba1b0bda |
| mcpserver-grok-bot-plugin | https://github.com/sharpninja/mcpserver-grok-bot-plugin/pull/1 | abb746cfdefba7cbcf06724a011b1568487ad155 |
| mcpserver-claude-code-plugin | https://github.com/sharpninja/mcpserver-claude-code-plugin/pull/6 | 2510cc1be501b028e59736fb638cf38df3d29ccb |
| mcpserver-claude-cowork-plugin | https://github.com/sharpninja/mcpserver-claude-cowork-plugin/pull/5 | 95bce132f1c71e4c4d10bf3943e0c700ed5968f8 |
| mcpserver-codex-plugin | https://github.com/sharpninja/mcpserver-codex-plugin/pull/5 | ae35f225bd896fdc2bea2fdd6b923703beaaec39 |
| mcpserver-copilot-plugin | https://github.com/sharpninja/mcpserver-copilot-plugin/pull/5 | a0792f4fcb1cca8e7ac8d5f20d6917218a30dbd0 |
| mcpserver-cline-plugin | https://github.com/sharpninja/mcpserver-cline-plugin/pull/5 | 7a28d0294797bf643bdd23e3ecb7d03c3ac89473 |
| mcpserver-cline-v2-plugin | https://github.com/sharpninja/mcpserver-cline-v2-plugin/pull/5 | 36c95e48c9452907463b44b5edf5ac6df4c4dbea |
| mcpserver-opencode-plugin | https://github.com/sharpninja/mcpserver-opencode-plugin/pull/4 | fc20aafda2da612d72ad0171792e369e4438a121 |
| McpServerTools | https://github.com/sharpninja/McpServerTools/pull/1 | 11ee91241943c3032ce5ec6ea66fcb374f6dc518 |
| McpServerManager | https://github.com/sharpninja/McpServerManager/pull/10 | 242a8968884ce70f064691f19c77b0920a4598af |

## Evidence and audit trail

Evidence directory:
F:\GitHub\McpServer\.worktrees\federation-fixes-20261007\docs\receipts\hv\20261007T174500Z-qbrain-ai-rebrand-phase-2-hv-green-evidence

The directory contains initial/final PR metadata, diffs, test workers, complete test logs, NUnit XML/Jest JSON/TRX results, raw-blob hashes, manifest diagnosis, lockfile integrity, package identities, scope scans, model proof and checksums.

Model proof is from this review's own rollout:
C:\Users\kingd\.codex\sessions\2026\10\07\rollout-2026-10-07T12-41-33-01a11774-cc71-7f30-9af1-8da3a6a82233.jsonl
Its line 8 turn_context records model=gpt-6-astra and effort=xhigh; collaboration_mode.settings independently records the same model/effort. The exact current user request also matches. See model-proof.json.

Audit correction: the initial prompt hook reselected the worktree workspace despite the root MCP_WORKSPACE_PATH setting. That worktree turn was closed with a routing-correction note. The verbatim user prompt, full findings and decisions were then persisted via the required wrapper with explicit -WorkspacePath F:\GitHub\mcpserver. Root session: Codex-20261007T164840Z-plugin-session; request: req-20261007T174419Z-prompt-ac7c. See audit-routing-correction.json. The pre-correction environment.json is retained rather than rewritten.

No production source, tests, manifests, PR state or implementation plan was changed by this verification. No merge was attempted. The required receipt is copied, with the source retained, to F:\GitHub\McpServer\docs\receipts\hv\20261007T174500Z-qbrain-ai-rebrand-phase-2-hv-green.md.
