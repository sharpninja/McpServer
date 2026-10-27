# QBrain.AI rebrand Phase 2 hostile verification — r3

- ReceiptId: 20261007T183000Z-qbrain-ai-rebrand-phase-2-hv-r3
- VerificationCompletedUtc: 2026-10-07T18:38:05Z
- OverallVerdict: **DISAGREE**
- Accuracy: **97**
- Completeness: **97**
- ValidatorIdentity: Codex, gpt-6-astra / xhigh
- ModelLock: **PASS**, proved from this rollout.
- Host: PAYTON-LEGION2
- ReviewWorkspace / git -C / hvDir: `F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007`
- RepositorySuiteTotals: **Total 1143 / Passed 1074 / Failed 69 / Skipped 0**.
- FullPhase2Green: **FAIL**. Director executed and failed; no quarantine or waiver.

The Manager restore/build blocks from r2 are cleared on the supplied tip once the archive is isolated from the enclosing McpServer package configuration. Desktop, Director, and Web all execute. However, Director's committed test runner selects the wrong repository root in the mandated worktree and 69 tests fail. The operator's zero-failure gate is therefore unmet. Accuracy and Completeness are assessments of the claimed content readiness, not arithmetic test-pass percentages. The requested AGREE / >=98 thresholds have not been reached.

This is review of OPEN draft contents, per the current operator instruction. The older plan's merged-PR completion language does not authorize merging. No PR was merged, no repository was renamed, and no Phase 3 or 4-L action was performed.

## ModelLock proof from this rollout

Source: `C:\Users\kingd\.codex\sessions\2026\10\07\rollout-2026-10-07T13-25-30-01a1179d-0c2e-7f20-be6a-fb9a67aa8c19.jsonl`.

- Session id: `01a1179d-0c2e-7f20-be6a-fb9a67aa8c19`; session source: `exec`.
- `turn_context` at line 8, timestamp `2026-10-07T18:25:33.703Z`.
- `payload.model = gpt-6-astra`; `payload.effort = xhigh`.
- `payload.collaboration_mode.settings.model = gpt-6-astra`; `reasoning_effort = xhigh`.
- `payload.cwd` equals the locked review workspace.
- The same rollout contains this exact r3 user prompt; `ownPromptMatches=true`.

The separate guardian / codex-auto-review rollout was not used as validator proof. Extracted evidence: `model-proof.json`.

## Fresh inputs, isolation, and commands

Evidence root, including scripts, archives, logs and reports:

`F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\20261007T183000Z-qbrain-ai-rebrand-phase-2-hv-r3-evidence`

All evidence filenames below are relative to that root. Every requested head was read from GitHub and freshly archived with `git -C <locked-worktree> --git-dir=<sibling-repository> archive --format=zip --output=<archive> <exact-tip>`. `inventory.json` records the full tips, archive SHA-256 values, snapshot paths and initial PR states. Existing working-tree helpers, node_modules, bin and obj were not copied. The Manager gitlink was independently archived at `d71a34d8de3c675cd027920d8d2fe936e8a90455` and expanded at `lib/Markdown.Avalonia`; see `submodule.json`.

Read-only context: the 182800Z and 180500Z green proofs; the 181500Z r2, 174500Z green and 162924Z r123b receipts; the implementation plan and requirements matrix. The implementation plan was not edited. Its before/after SHA-256 is `6CA0C9E34EC70B906F2538027BF8791C7729FBF8586A2257B76F8F1AEC7A57E8`.

Runtime: PowerShell Core 7.6.6, Pester 5.7.1, Node 20.20.2, npm 10.8.2, .NET SDK 10.0.401. Hidden `pwsh.exe -NoProfile -NonInteractive` workers cleared inherited MCP/plugin variables and used the normal user TEMP/TMP. All review commands ran from the locked worktree or its archived child directories. No review work ran from the prohibited federation-fixes worktree.

Commands:

- Shell plugins: `Invoke-Pester` over the complete `tests/` directory, with PassThru and NUnit XML; no tag, test-name or path exclusions.
- Node plugins: `npm ci --no-audit --no-fund`, `npm run build`, then `npm test -- --runInBand --json --outputFile=<report>`. Every install and build exited 0; all three package-lock hashes were unchanged.
- Node discovery correction: the initial test commands exited 1 with **no tests found**, because Jest's expanded `<rootDir>` glob mixed slash escaping at `McpServer\.worktrees`. Retried with `--testMatch '**/tests/**/*.test.ts'`, preserving all committed test files. `node-scope-proof.json` proves exact source-file / executed-file equality: Cline 5/5 files, Cline v2 4/4, OpenCode 4/4. All three retry test commands exited 0; coverage settings were retained. Initial logs remain intact.
- Manager: unfiltered `dotnet test <each tests/*.csproj> --configuration Debug --logger "trx;LogFileName=<suite>.trx" --results-directory <evidence> --nologo`; restore, build and audit remained enabled.
- Manager initial archive: all five restore attempts exited 1 with NU1008. MSBuild had imported the enclosing McpServer `Directory.Packages.props`, enabling unrelated Central Package Management. Property inspection proved that import; the normal sibling Manager project has no such setting. A fresh expansion of the same archives under `manager-isolated/` uses an empty `<Project />` parent `Directory.Packages.props` outside the Manager snapshot to stop ancestor leakage. No Manager source or package version was changed. See `manager-isolation-proof.json`; initial failure logs are retained at the evidence root. Final Manager reports are under `manager-isolated/`.
- Additional Manager suites: Pester `scripts/DeployAllTargets.Tests.ps1`; extension `npm ci --no-audit --no-fund` and `npm test` (which runs compile and Node tests).
- Integrity: `git cat-file blob <tip>:<path>` captured from the process byte stream, then SHA-256 over the actual committed bytes, without newline conversion.

Post-test archive comparison checked **2017 committed files across all eleven snapshots: zero changed, zero missing**. Generated build outputs are additional files. See `post-test-snapshot-integrity.json`. No source/test patch or fake solution marker was added to obtain a pass.

## Suite results

Counts come from NUnit XML, Jest JSON, TRX counters and Node TAP, not just process exits. The totals below use the final complete run of each suite; unsuccessful initial restore/discovery attempts are disclosed above and are not counted as zero-test passes.

| Repository / complete suite | Total | Passed | Failed | Skipped | Evidence / result |
| --- | ---: | ---: | ---: | ---: | --- |
| Grok — Pester tests/ | 29 | 29 | 0 | 0 | `mcpserver-grok-plugin.pester.xml` |
| Grok Bot — Pester tests/ | 29 | 29 | 0 | 0 | `mcpserver-grok-bot-plugin.pester.xml` |
| Claude Code — Pester tests/ | 36 | 36 | 0 | 0 | `mcpserver-claude-code-plugin.pester.xml` |
| Claude Cowork — Pester tests/ | 29 | 29 | 0 | 0 | `mcpserver-claude-cowork-plugin.pester.xml` |
| Codex — Pester tests/ | 41 | 41 | 0 | 0 | `mcpserver-codex-plugin.pester.xml` |
| Copilot — Pester tests/ | 29 | 29 | 0 | 0 | `mcpserver-copilot-plugin.pester.xml` |
| Cline — npm build/test | 32 | 32 | 0 | 0 | `node-discovery-retry/mcpserver-cline-plugin.jest.json` |
| Cline v2 — npm build/test | 27 | 27 | 0 | 0 | `node-discovery-retry/mcpserver-cline-v2-plugin.jest.json` |
| OpenCode — npm build/test | 67 | 67 | 0 | 0 | `node-discovery-retry/mcpserver-opencode-plugin.jest.json` |
| Manager Core | 141 | 141 | 0 | 0 | `manager-isolated/McpServerManager.Core.Tests.trx` |
| Manager UI.Core | 403 | 403 | 0 | 0 | `manager-isolated/McpServerManager.UI.Core.Tests.trx` |
| Manager Desktop | 6 | 6 | 0 | 0 | `manager-isolated/McpServerManager.Desktop.Tests.trx` |
| Manager Director | 138 | 69 | **69** | 0 | **FAIL**, exit 1; `manager-isolated/McpServerManager.Director.Tests.trx` |
| Manager Web | 122 | 122 | 0 | 0 | `manager-isolated/McpServerManager.Web.Tests.trx` |
| Manager DeployAllTargets — Pester | 3 | 3 | 0 | 0 | `Manager.DeployAllTargets.pester.xml` |
| Manager extension — npm compile/test | 11 | 11 | 0 | 0 | `Manager.extension.test.log` |
| Tools — static manifests | N/A | N/A | N/A | N/A | PASS: 18 plugin manifests, nine alias pairs; no unit-test command found |
| Repository-suite total | **1143** | **1074** | **69** | **0** | `suite-summary.json`, `suite-totals.json` |

Plugin subtotal: 319/319. Five Manager .NET suites: 810 total / 741 passed / 69 failed / 0 skipped. Other result counters (pending/todo, not-run, error, timeout, aborted, inconclusive and cancelled where present) are zero. The separately executed ReplBridge harness passes 45/45 and is not added to the repository-suite total.

## Blocking finding: HV-R3-01 — Director test root discovery escapes the snapshot

**P1; OPEN.** In the reviewed Manager tip, [DirectorRunner.cs:98](https://github.com/sharpninja/McpServerManager/blob/43d3158119c6b9fef38dafdaad3cf3addc2d8609/tests/McpServerManager.Director.Tests/DirectorRunner.cs#L98) searches ancestors for `McpServer.sln`. The Manager snapshot contains `McpServerManager.sln`. Consequently, `FindRepoRoot()` walks out of the archived repository, finds the enclosing `F:\GitHub\McpServer\McpServer.sln`, and never reaches its standard-layout fallback.

The executed failure log records:

```text
Unexpected exit code -2147450751: The application to execute does not exist:
'F:\GitHub\McpServer\src\McpServerManager.Director\bin\Debug\net10.0\director.dll'
```

This affects CLI help, configuration, agent, health and exec-area cases. Director restored and compiled successfully, then **69 tests failed and 69 passed**. Evidence: `manager-isolated/McpServerManager.Director.Tests.test.log` (for example line 96), its TRX, and `director-failures.json` with all 69 failures.

The ordinary sibling checkout can fall back successfully because its ancestors differ; that explains why a local green claim is compatible with this failure. This is an inference from the committed discovery code and the observed wrong path, not a rerun of the author's checkout. It is a real failure in the operator-mandated environment and is not waived as baseline or environment noise.

Required resolution: make the committed runner locate the Manager root correctly, then rerun the complete 138-test Director suite and the required regression gate on a new immutable tip. This review did not implement that change, add `McpServer.sln` to the snapshot, relocate testing outside the worktree, filter tests, or substitute an executable.

## R1–R3, prior fixes, and static integrity

- **R1 PASS:** the reviewed `bridge-harness.cjs` executed against each exact archived vendor tarball. All 45 cases pass: modeled Windows/Linux new-only, legacy-only, both, neither, command override, binary override precedence and Windows `.cmd` variants. It asserts both resolution and the captured ReplBridge spawn command, agent arguments and cwd. This is controlled dependency testing, not an actual Linux or REPL launch certification.
- **R2 PASS:** all nine plugins' three targeted CORE files match their manifest digests: **27/27**. All manifest entries match raw committed bytes: **144/144**, including **96/96 shell entries** and **16/16 Grok Bot entries**. Zero mismatches. The hash check does not normalize line endings: three existing entries per plugin contain CR bytes, so this is not a claim that every committed file is LF-only. The Grok Bot hash finding remains closed.
- **R3 PASS:** Cline package.json, package-lock top-level and root-package identities all equal `@qbrainai/qbrain-ai-cline-plugin`; both legacy and new executable aliases point to `dist/index.js`. Its complete suite passes 32/32. The committed memory-context helper was present in the fresh archive; no untracked helper was supplied. Cline v2 passes 27/27.
- All three vendored core tarballs have SHA-256 `395F140E30B3ACE1E1AEBFFED30E24A0DF9A8AD8A34BE7D3A0D8192E4D2BE1E8`; computed SHA-512 SRI values match their lockfiles. See `vendor-integrity.json` and `package-identities.json`.
- Manager UI.Core remains 403/403. Desktop's pinned Avalonia dependency and DBus changes compile and execute 6/6; Web's restore/build and 122 tests pass. Director's former restore issue is closed, but HV-R3-01 prevents full acceptance.
- `manager-r2-to-r3.diff` records the dependency changes. Precision note: the committed Directory.Build.targets exemption clears warning-as-error settings for projects whose path contains `Markdown.Avalonia`; it is broader than nullable diagnostics alone. The verifier applied no additional warning suppression or audit disablement.
- Tools static checks: all 18 old/new plugin manifests and their embedded parameter schemas parse; all nine alias pairs preserve matching repository URLs, source commits, parameter schemas and command templates. The eight legacy manifests existing at base are byte-identical; Grok Bot's legacy manifest is added alongside its new alias. No old plugin manifest was removed. See `tool-manifests.json` and `tool-alias-pairs.json`.
- Committed diff scans found no QuadBrain/QBAgent/qbagent feature-name deltas, private-profile file deltas, or premature new GitHub repository URLs. See `scope-scan.json`. No hostname/settings/rename/deployment operation was performed.

## Exact tips and final PR states

Initial and final reads match every requested immutable head. Final readback completed at `2026-10-07T18:35:15Z`; all eleven are **OPEN**, **isDraft=true**, **mergedAt=null**. Full records: `final-pr-state.json`.

- [Grok #6](https://github.com/sharpninja/mcpserver-grok-plugin/pull/6): `b2a0ec7e03869f0f2863728ccdc7da75ba1b0bda`
- [Grok Bot #1](https://github.com/sharpninja/mcpserver-grok-bot-plugin/pull/1): `6e1edf99f6b5c40fd3576e364b7cd725261b7df1`
- [Claude Code #6](https://github.com/sharpninja/mcpserver-claude-code-plugin/pull/6): `2510cc1be501b028e59736fb638cf38df3d29ccb`
- [Claude Cowork #5](https://github.com/sharpninja/mcpserver-claude-cowork-plugin/pull/5): `95bce132f1c71e4c4d10bf3943e0c700ed5968f8`
- [Codex #5](https://github.com/sharpninja/mcpserver-codex-plugin/pull/5): `ae35f225bd896fdc2bea2fdd6b923703beaaec39`
- [Copilot #5](https://github.com/sharpninja/mcpserver-copilot-plugin/pull/5): `a0792f4fcb1cca8e7ac8d5f20d6917218a30dbd0`
- [Cline #5](https://github.com/sharpninja/mcpserver-cline-plugin/pull/5): `c7f2449550aadf66f24ce8153138c0411614efc9`
- [Cline v2 #5](https://github.com/sharpninja/mcpserver-cline-v2-plugin/pull/5): `1e13d8e12f74d92b5bdc6f367e86cbd2201f066e`
- [OpenCode #4](https://github.com/sharpninja/mcpserver-opencode-plugin/pull/4): `fc20aafda2da612d72ad0171792e369e4438a121`
- [Manager #10](https://github.com/sharpninja/McpServerManager/pull/10): `43d3158119c6b9fef38dafdaad3cf3addc2d8609`
- [Tools #1](https://github.com/sharpninja/McpServerTools/pull/1): `11ee91241943c3032ce5ec6ea66fcb374f6dc518`

## Remaining limitations and disposition

- **Content gate blocker:** HV-R3-01, 69 Director failures. No AGREE and no complete Phase 2 green claim.
- **Independent CI infrastructure blocker:** all twelve failing core-integrity GitHub checks across the six shell PRs have fresh annotations saying the job did not start because the account is locked for billing. See `ci-annotations.json`; example [Grok Bot check](https://github.com/sharpninja/mcpserver-grok-bot-plugin/actions/runs/37662858906/job/112934501208). These do not contradict the local 144/144 raw-blob integrity results, but remote CI is not green.
- Shell execution scope is the explicitly requested Pester `tests/`. Bash/Bats files were not executed by this PowerShell-only verification. No cross-platform certification is claimed.
- The locked worktree has no AGENTS-README-FIRST.yaml. The plugin prompt hook could not open its cached turn because session-state.yaml was absent. Native MCP tools successfully opened and updated `Codex-20261007T182530Z-hv-r3`, request `req-20261007T182530Z-hv-r3`, preserving the audit independently. Unified shell launcher failed before process creation; actual commands used the verified PowerShell Core MCP console.

The receipt is authored only at the requested worktree path and is to be copied verbatim to `F:\GitHub\McpServer\docs\receipts\hv\20261007T183000Z-qbrain-ai-rebrand-phase-2-hv-r3.md`. Supporting evidence remains under the locked worktree. The implementation plan and prior receipts remain unchanged.
