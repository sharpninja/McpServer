# QBrain.AI rebrand Phase 2 hostile verification — r4

- ReceiptId: 20261007T185000Z-qbrain-ai-rebrand-phase-2-hv-r4
- VerificationCompletedUtc: 2026-10-07T19:14:37.6622057Z
- OverallVerdict: **DISAGREE**
- Accuracy: **97**
- Completeness: **97**
- ValidatorIdentity: Codex, **gpt-6-astra / xhigh**
- ModelLock: **PASS**, proved from this rollout.
- Host: **PAYTON-LEGION2**
- ReviewWorkspace / git -C / hvDir: `F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007`
- Final complete repository-suite totals: **Total 1145 / Passed 1144 / Failed 1 / Skipped 0**.
- FullPhase2Green: **FAIL**. **HV-R4-01 remains OPEN**. No quarantine, baseline exemption, or selection of an earlier passing Core run.

Scores assess the submitted Phase 2 acceptance claims, not the percentage of individual tests passing. The zero-failure condition is unsatisfied, so the requested AGREE / >=98 / >=98 target is not met.

This receipt reviews OPEN draft contents under the current operator instruction. It does not assert that the older plan's merged-PR condition is complete. No merge, repository rename, hostname change, deployment, or Phase 3/4-L action was performed.

## ModelLock evidence from this rollout

Source: `C:\Users\kingd\.codex\sessions\2026\10\07\rollout-2026-10-07T13-47-53-01a117b1-88f9-7b93-8f93-dc213da72380.jsonl`.

- Session id: `01a117b1-88f9-7b93-8f93-dc213da72380`; source: `exec`.
- `turn_context` timestamp: `2026-10-07T18:47:56.545Z`.
- `payload.model = gpt-6-astra`; `payload.effort = xhigh`.
- `payload.collaboration_mode.settings.model = gpt-6-astra`; `reasoning_effort = xhigh`.
- `payload.cwd` equals the mandatory worktree.
- This same rollout contains the exact r4 user prompt: `ownPromptMatches=true`.

See `model-proof.json` and `model-proof-current.ps1`. The first discovery selected a separate guardian / codex-auto-review rollout; it was rejected as validator evidence and retained separately in `excluded-guardian-rollout.json`. Only the actual exec rollout above establishes ModelLock.

## Evidence locations and immutable inputs

Primary evidence directory (E):

`F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\20261007T185000Z-qbrain-ai-rebrand-phase-2-hv-r4-evidence`

Second fresh Manager archive and reports (M):

`F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\r4m-185000`

All evidence filenames below are relative to E unless prefixed M. Read-only context included the latest 184500Z green proof, r3 receipt, implementation plan, requirements matrix, and BDPv4 process. Prior receipts and the plan were not edited.

All eleven requested commits were freshly archived using:

```text
git -C <locked-worktree> --git-dir=<sibling-repository-.git> archive --format=zip --output=<new-archive> <exact-tip>
```

`inventory.json` records exact tips, GitHub state, archive SHA-256 values, and snapshot paths. Snapshots were expanded inside the locked worktree. Working checkout helpers, node_modules, bin, and obj were not copied into them. Manager's pinned Markdown.Avalonia gitlink was independently archived at `d71a34d8de3c675cd027920d8d2fe936e8a90455`; see `submodule.json`.

Runtime: PowerShell Core **7.6.6**, Pester **5.7.1**, Node **20.20.2**, npm **10.8.2**, .NET SDK **10.0.401**. Hidden `pwsh.exe -NoProfile -NonInteractive` workers cleared inherited MCP/plugin variables and used normal user TEMP/TMP. All shell execution used PowerShell Core. The standard unified exec launcher failed before process creation; the persistent PowerShell MCP console executed the review. No review command ran in the prohibited federation-fixes worktree.

## Execution commands and initial failures

- Shell plugins: complete Pester `tests/` directories, PassThru and NUnit XML. No test-name/tag/path exclusions.
- Node plugins: `npm ci --no-audit --no-fund`, `npm run build`, `npm test -- --runInBand --json --outputFile=<report>`. All three installs and builds exited 0; package-lock SHA-256 values remained unchanged.
- All three initial Node test invocations exited 1 with **no tests found**. Jest expanded the committed rootDir glob with mixed path escaping at `McpServer\.worktrees`. A full-discovery retry used `--testMatch '**/tests/**/*.test.ts'`, retaining the entire committed test scope and coverage settings. Exact source/executed file equality is proved in `node-scope-proof.json`: Cline 5/5, Cline v2 4/4, OpenCode 4/4. All retries exited 0. Original discovery-failure logs are retained; no zero-test invocation is counted as a pass.
- Manager: unfiltered `dotnet test <tests-project.csproj> --configuration Debug --logger "trx;LogFileName=<suite>.trx" --results-directory <evidence> --nologo`, with restore, build, and NuGet audit enabled, for **all five** projects.
- Manager additional suites: Pester `scripts/DeployAllTargets.Tests.ps1`; extension `npm ci --no-audit --no-fund` then `npm test`, including its compile and Node tests.
- Integrity: raw `git cat-file blob <tip>:<path>` process byte streams were hashed directly; no newline normalization was used for CORE-MANIFEST checks.

The initial Manager archive under E/snapshots restored and built all five suites without NU1008 or an isolation file. Core passed 141/141, Director 140/140, UI.Core 403/403, and Web 122/122. Desktop executed and failed **6/6** with:

```text
System.DllNotFoundException: Unable to load DLL 'libHarfBuzzSharp'
The filename or extension is too long. (0x800700CE)
```

This was not treated as a passing result. A second fresh archive of the same Manager tip and gitlink was expanded under M/McpServerManager, still beneath the same mandatory worktree. No source, test, package, build property, or parent isolation file was changed. **All five Manager suites were rerun**, including Core. The two Manager ZIP SHA-256 values match; see `manager-short-path-proof.json`.

`native-path-proof.json` directly distinguishes the native loader paths: loading the identical x64 DLL fails at the 259-character initial path and succeeds at the 202-character shorter path. Both DLLs hash to `145ADE963EBA427027D5E1381DB5D51A0EF9E98E9FEF9EFBFA6074CA20DA246F`. Desktop then passes 6/6. This establishes the path-dependent harness limitation; it does not erase the original six failures or the subsequent Core failure.

## Complete-suite results

Counts come from NUnit XML, Jest JSON, TRX counters, and Node TAP. Manager rows use the **entire second fresh run**, including its failing Core result. The earlier Core pass is not substituted. `suite-summary.json`, `suite-totals.json`, and `manager-all-runs.json` retain the counts.

| Repository / full suite | Total | Passed | Failed | Skipped | Report / result |
| --- | ---: | ---: | ---: | ---: | --- |
| Grok — Pester tests/ | 29 | 29 | 0 | 0 | E/mcpserver-grok-plugin.pester.xml |
| Grok Bot — Pester tests/ | 29 | 29 | 0 | 0 | E/mcpserver-grok-bot-plugin.pester.xml |
| Claude Code — Pester tests/ | 36 | 36 | 0 | 0 | E/mcpserver-claude-code-plugin.pester.xml |
| Claude Cowork — Pester tests/ | 29 | 29 | 0 | 0 | E/mcpserver-claude-cowork-plugin.pester.xml |
| Codex — Pester tests/ | 41 | 41 | 0 | 0 | E/mcpserver-codex-plugin.pester.xml |
| Copilot — Pester tests/ | 29 | 29 | 0 | 0 | E/mcpserver-copilot-plugin.pester.xml |
| Cline — npm ci/build/full tests | 32 | 32 | 0 | 0 | E/node-discovery-retry/mcpserver-cline-plugin.jest.json |
| Cline v2 — npm ci/build/full tests | 27 | 27 | 0 | 0 | E/node-discovery-retry/mcpserver-cline-v2-plugin.jest.json |
| OpenCode — npm ci/build/full tests | 67 | 67 | 0 | 0 | E/node-discovery-retry/mcpserver-opencode-plugin.jest.json |
| Manager Core | 141 | 140 | **1** | 0 | M/McpServerManager.Core.Tests.trx |
| Manager UI.Core | 403 | 403 | 0 | 0 | M/McpServerManager.UI.Core.Tests.trx |
| Manager Desktop | 6 | 6 | 0 | 0 | M/McpServerManager.Desktop.Tests.trx |
| Manager Director | 140 | 140 | 0 | 0 | M/McpServerManager.Director.Tests.trx |
| Manager Web | 122 | 122 | 0 | 0 | M/McpServerManager.Web.Tests.trx |
| Manager deployment script Pester | 3 | 3 | 0 | 0 | M/Manager.DeployAllTargets.pester.xml |
| Manager extension compile/Node tests | 11 | 11 | 0 | 0 | M/Manager.extension.test.log |
| **Repository tests total** | **1145** | **1144** | **1** | **0** | Full-phase green FAIL |
| Tools static manifest/schema checks | 18 | 18 | 0 | 0 | E/tool-manifests.json; separate static checks |

Additional checks, outside the repository-test total: **45/45** ReplBridge cases, **144/144** manifest digests, and **9/9** Tools alias pairs.

All five Manager suites executed on both fresh expansions. No suite is N/E. Both restore/build passes completed. The initial full repository run had 1139 passed and 6 failed out of 1145; the second complete Manager run plus the verified plugin/additional suites has the 1144/1 result above. Worker exit alone is not a gate: the Manager orchestration process ended 0 after later successful commands, while its captured Core command exit was **1** and TRX recorded the failure.

## HV-R4-01 — Manager Core callback observed after Stop

**P1; OPEN; phase acceptance blocker.**

Exact test:

`McpServerManager.Core.Tests.Services.Infrastructure.TimerServiceTests.Restart_AfterStop_ResumesCallbacks`

At [TimerServiceTests.cs:85](https://github.com/sharpninja/McpServerManager/blob/8e1550d508eee88635b47ce13ff4c9384849120a/tests/McpServerManager.Core.Tests/Services/Infrastructure/TimerServiceTests.cs#L85):

```text
Expected count to be 2, but found 3.
```

The test starts a 50 ms recurring timer, waits 150 ms, calls Stop(), snapshots the count, waits another 150 ms, and asserts that the count did not increase. It failed **before the Restart() call**, with 140 other Core tests passing and zero skips. Evidence: M/McpServerManager.Core.Tests.trx, its test log, and E/all-test-failures.json. Command completion: `2026-10-07T19:00:11.8422319Z`.

[TimerService.cs:49](https://github.com/sharpninja/McpServerManager/blob/8e1550d508eee88635b47ce13ff4c9384849120a/src/McpServerManager.Core/Services/Infrastructure/TimerService.cs#L49) implements Stop() only by changing the timer to infinite due time/period. OnTick checks cancellation, but Stop does not cancel, drain, or otherwise synchronize callbacks. **Inference:** an already queued or in-flight callback can account for the post-Stop increment. This review does not claim an instrumented proof of the exact interleaving.

The first full Core run passed; the second unfiltered fresh-archive run failed. That directly disproves a stable all-green claim at this immutable tip. It is not waived as timing noise, pre-existing behavior, machine load, or a small failure percentage. No third run was used to select a pass.

Required resolution: reconcile and make deterministic the Stop/restart callback contract and its test, following BDPv4, then provide a new immutable Manager tip and rerun the complete suites with zero failures and skips. The validator did not patch the implementation, weaken the assertion, disable the test, or change timeouts.

## HV-R3-01 closure and r4 delta

**HV-R3-01 CLOSED in this verification.**

- The pinned r3-to-r4 diff contains the committed Director root check, two regression tests, and Directory.Packages.props. See `manager-r3-to-r4.diff`.
- Director selects a root containing both McpServerManager.sln and src/McpServerManager.Director.
- Both `DirectorRunnerRepoRootTests` cases executed and passed; the complete Director suite passed **140/140** on both expansions.
- Direct MSBuild inspection confirms `ManagePackageVersionsCentrally=false` and `DirectoryPackagesPropsPath` points at the **committed Manager** Directory.Packages.props.
- `nested-environment-proof.json` records the ancestor chain: Manager props, the enclosing locked-worktree props, and main McpServer props. There is **no empty parent isolation file** between them. Restore succeeded on its own.
- Desktop's Avalonia/DBus dependency changes and Web's dependency fixes compile and execute. UI.Core remains 403/403. The committed Markdown.Avalonia warning exemption is broader than nullable warnings alone; no additional suppression was introduced by the verifier.

## R1–R3, prior closures, and snapshot integrity

- **R1 PASS:** `bridge-harness.cjs` executed against all three freshly extracted committed vendor tarballs. All 45 controlled Windows/Linux cases pass: new-only, legacy-only, both/prefer-new, neither/default-new, command override, binary override precedence, and Windows .cmd variants. It asserts resolved command and captured spawn command, agent arguments, and cwd. This is dependency-controlled testing, not a claim of actual Linux or REPL process certification.
- **R2 PASS:** all **27/27** targeted CORE entries and **144/144** total committed manifest entries match raw Git blob SHA-256 values. Shell subset **96/96**, Grok Bot **16/16**, zero mismatches. Targeted ensure-repl/plugin-env/repl-invoke blobs have zero CR bytes. Some other pre-existing entries contain CR bytes; no claim that every committed file is LF-only is made.
- **R3 PASS:** Cline package.json, package-lock top-level, and root-package identities all equal `@qbrainai/qbrain-ai-cline-plugin`; legacy and new executable aliases both target dist/index.js. Cline passes 32/32. Its committed memory-context helper is present in the archive; no untracked helper was supplied. Cline v2 passes 27/27.
- All three vendor tarball SHA-512 SRI values match their lockfiles. The vendor archive SHA-256 is `395F140E30B3ACE1E1AEBFFED30E24A0DF9A8AD8A34BE7D3A0D8192E4D2BE1E8`; see `vendor-integrity.json`.
- Tools: all 18 old/new manifests and embedded parameter schemas parse. Nine alias pairs match repository URL, sourceCommit, parameterSchema, commandTemplate, and installCommand. Eight legacy manifests already present at base are byte-identical; Grok Bot's old-name manifest is added alongside the new alias. No old-name manifest was removed.
- Committed diff scans found zero QuadBrain/QBAgent/qbagent feature-name deltas, private-profile file deltas, or premature renamed GitHub URLs. See `scope-scan.json`.
- Post-test verification covered **2019** archived files across the eleven repositories, the second Manager expansion (**1301** files), and both submodule expansions (**502** files each). Nothing is missing. The only changed tracked bytes are ten generated extension out/*.js files in each Manager expansion, regenerated by its compile command. Every one is identical after CRLF/LF normalization; `generated-output-proof.json` records this. All other archived bytes, including implementation/test sources, are unchanged. No hand-authored patch was used.
- Plan SHA-256 before and after: `6CA0C9E34EC70B906F2538027BF8791C7729FBF8586A2257B76F8F1AEC7A57E8`. Tracked diff in the locked review worktree remains empty.

## Exact tips and draft status

GitHub state was fetched before archiving, during verification, and again after all workers completed. All eleven heads matched and were **OPEN / draft / unmerged** at the final 19:14 UTC check; see `inventory.json`, `final-pr-state.json`, and `pr-state-at-completion.json`.

- [Grok #6](https://github.com/sharpninja/mcpserver-grok-plugin/pull/6): `b2a0ec7e03869f0f2863728ccdc7da75ba1b0bda`
- [Grok Bot #1](https://github.com/sharpninja/mcpserver-grok-bot-plugin/pull/1): `6e1edf99f6b5c40fd3576e364b7cd725261b7df1`
- [Claude Code #6](https://github.com/sharpninja/mcpserver-claude-code-plugin/pull/6): `2510cc1be501b028e59736fb638cf38df3d29ccb`
- [Claude Cowork #5](https://github.com/sharpninja/mcpserver-claude-cowork-plugin/pull/5): `95bce132f1c71e4c4d10bf3943e0c700ed5968f8`
- [Codex #5](https://github.com/sharpninja/mcpserver-codex-plugin/pull/5): `ae35f225bd896fdc2bea2fdd6b923703beaaec39`
- [Copilot #5](https://github.com/sharpninja/mcpserver-copilot-plugin/pull/5): `a0792f4fcb1cca8e7ac8d5f20d6917218a30dbd0`
- [Cline #5](https://github.com/sharpninja/mcpserver-cline-plugin/pull/5): `c7f2449550aadf66f24ce8153138c0411614efc9`
- [Cline v2 #5](https://github.com/sharpninja/mcpserver-cline-v2-plugin/pull/5): `1e13d8e12f74d92b5bdc6f367e86cbd2201f066e`
- [OpenCode #4](https://github.com/sharpninja/mcpserver-opencode-plugin/pull/4): `fc20aafda2da612d72ad0171792e369e4438a121`
- [Manager #10](https://github.com/sharpninja/McpServerManager/pull/10): `8e1550d508eee88635b47ce13ff4c9384849120a`
- [Tools #1](https://github.com/sharpninja/McpServerTools/pull/1): `11ee91241943c3032ce5ec6ea66fcb374f6dc518`

## Remaining blockers and audit limits

- **Acceptance blocker:** HV-R4-01, one Core timer stop/restart test failure. The complete zero-failure gate is not satisfied.
- **Separate remote CI blocker:** twelve failed core-integrity checks across the six shell PRs have fresh annotations saying the jobs did not start because the GitHub account is locked for billing. See `ci-annotations.json`. Local raw-blob integrity passes; remote CI is not green.
- The original long archive path cannot load HarfBuzz. The shorter fresh expansion inside the same mandated worktree resolves that harness problem and executes Desktop 6/6. Both runs remain evidence.
- Shell execution scope is the explicitly requested Pester tests/. Bash/Bats suites were not executed; no cross-platform certification is claimed.
- The worktree has no AGENTS-README-FIRST.yaml. The plugin prompt hook could not open its cached turn because session-state.yaml was absent. Native MCP created session `Codex-20261007T184755Z-hv-r4`, but both begin-turn attempts returned `backend_unavailable`. No durable turn persistence is claimed.
- The full audit payload is retained through the supported `Write-ReplFailsafe` helper and object-first YAML serializer at `.mcpServer\failsafe\Codex\hv-r4-pending\20261007T190842Z-hv-r4-session-upsert-1182.yaml`, request `req-20261007T184755Z-hv-r4`. See `audit-status.json`. This is queued recovery evidence, not a server acknowledgement.

This receipt is authored only at the requested worktree path. It is copied verbatim to `F:\GitHub\McpServer\docs\receipts\hv\20261007T185000Z-qbrain-ai-rebrand-phase-2-hv-r4.md`; copy SHA-256 verification is recorded separately in E/receipt-copy-proof.json.
