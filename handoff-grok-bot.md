# Handoff: QBrain.AI rebrand Phase 2

Written 2026-10-08 06:53 CT by Grok Bot (MCP Server chat).

## Codex takeover: verified 2026-10-08

The original handoff below is retained for context but its r3-only status is stale.

- Operator requested that Codex take over this work, authorized one GPT-6 Astra Extra High reviewer, and approved restoring existing requirements through MCP without closing them.
- All 11 Phase 2 GitHub PRs were queried live and remain OPEN/draft. No merge, remote rename, deployment, Phase 3, or Phase 4-L action is authorized here.
- Manager is clean at `d0082d0e02a8e3f10bab5dfd5e5920784ecf8233`, matching draft PR #10. Director/hermeticity was fixed in `8e1550d`; subsequent timer fixes are in `6c97627`, `b979309`, and `d0082d0`.
- Latest completed review is r5, not r3. The later r6 review started at stamp `20261007T203500Z`, then ended with `turn.failed` after a platform safety rejection, without a final verdict. Do not treat its partial evidence as AGREE or bypass that rejection.
- The latest implementation proof is `20261007T202100Z-phase2-green-proof.md`: it reports 3,670 passes across 23 Manager suite runs and no failed timer-stress iterations. This is prior evidence, not a fresh Codex test result.
- The authorized Astra reviewer is stopped, with no final r7 verdict. Its brief is `docs/receipts/hv/20261008-phase2-r7-review.prompt.md` in the locked worktree. Reviewer ID: `01a11b6f-33ae-75e0-964e-b5bb9d6c4ec7`; configured model `gpt-6-astra`, effort `xhigh`. The operator subsequently directed continuation without HV until noon America/Chicago on 2026-10-08, then Claude catch-up review of all missed implementation work. Do not restart Astra or introduce an implementation agent. The one-time `claude-noon-review-catch-up` automation fired; its model-blocked result is recorded below. It was not rescheduled.
- The r7 reviewer initially stopped before tests because its isolated `openSession` lacked a durable receipt and triage failed. The blocked receipt is not a code-defect verdict. Parent retried the original triage failsafe through the installed Codex wrapper against the registered workspace; accepted report `triage-report-fb4f2b995210460690cdb9c527e0d397`, request `req-20261008T123702Z-7d31`. The same reviewer was directed to recover logging before continuing validation. Keep its audit cache isolated from the parent's active turn.
- Audit recovery subsequently succeeded. Supported `workflow.sessionlog.queryHistory` readback returned reviewer session `Codex-20261008T123907Z-qbrain-phase2-r7-review`, agent `Codex`, status `in_progress`, `turnCount: 1`; the hook receipt reported `persisted: true`, `degraded: false`. Evidence: `docs/receipts/hv/20261008-phase2-r7-session-readback.receipt.json`. Validation resumed with the same reviewer; the earlier blocked receipt is historical, not the final Phase 2 verdict.
- Reviewer disclosed accidentally emitting the marker API token in a tool result. Do not reproduce it in handoffs or receipts. Rotation is recommended; no rotation was performed as part of this work.
- Review workspace remains `F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007`. Never use the federation worktree for this work.

### Current immutable PR heads

- grok #6: `b2a0ec7e03869f0f2863728ccdc7da75ba1b0bda`
- grok-bot #1: `6e1edf99f6b5c40fd3576e364b7cd725261b7df1`
- claude-code #6: `2510cc1be501b028e59736fb638cf38df3d29ccb`
- cowork #5: `95bce132f1c71e4c4d10bf3943e0c700ed5968f8`
- codex #5: `ae35f225bd896fdc2bea2fdd6b923703beaaec39`
- copilot #5: `a0792f4fcb1cca8e7ac8d5f20d6917218a30dbd0`
- cline #5: `c7f2449550aadf66f24ce8153138c0411614efc9`
- cline-v2 #5: `1e13d8e12f74d92b5bdc6f367e86cbd2201f066e`
- opencode #4: `fc20aafda2da612d72ad0171792e369e4438a121`
- McpServerTools #1: `11ee91241943c3032ce5ec6ea66fcb374f6dc518`. Its local checkout is instead at `e7dbfc32`; validate the PR head by archive, not local HEAD.
- McpServerManager #10: `d0082d0e02a8e3f10bab5dfd5e5920784ecf8233`

### Requirements restoration and remaining gaps

- The live MCP store initially had none of the rebrand records. With operator approval, Codex restored 5 FR, 8 TR, 8 TEST records, 70 structured acceptance criteria, and the five original mapping groups through the Codex plugin. Fresh effective-requirements readback confirmed exact AC and mapping values. All statuses are `pending`; every AC remains unsatisfied.
- Source: original registration commit `9855402b0df90fb06284840fd5f50977a444927e`. Later bulk rename `6de989f3` corrupted old-name examples in the projections, including contradictory solution assertions and duplicated route names. Those damaged projections were not used as recovery truth and were not hand-edited.
- Receipt: locked worktree `docs/receipts/hv/20261008-rebrand-requirements-recovery.receipt.json`; original inputs and preparation script are adjacent.
- Restoring records now does not retroactively prove requirements-first execution. With explicit operator approval, all five original mapping groups now include `TEST-MCP-QBRAIN-008`. Effective readback confirms all 21 requirement records and 70 acceptance criteria are otherwise unchanged and remain open. Receipt: locked worktree `docs/receipts/validation/20261008-phase2-resume/resume-and-mapping-receipt.json`.
- Docker is not a runtime prerequisite for this work. Historical static container-file naming clauses do not authorize installing Docker or running unrelated TruckMate packaging tests. Android Manager has no AAB requirement here.
- The initial Codex turn write was degraded and triaged as `triage-report-0a1858f65f4347c88223bfd50e0109e6`; subsequent action writes confirmed `persisted=true`, `degraded=false`.
- Log as Codex through the Codex plugin against the registered workspace `F:\GitHub\McpServer`, not the unregistered locked worktree. Resolve active session and turn from the supported wrapper; the service restart at 14:13 UTC rotated marker session state, so older identifiers above are historical evidence only.

### Parent validation continuation, 2026-10-08

- Parent evidence directory: locked worktree `docs/receipts/validation/20261008-phase2-resume`. This is validation evidence, not an independent HV verdict.
- Original r7 plugin runs: 193 PowerShell and 126 Node tests passed. Original Manager TRX readback: 826 passed across five projects, zero failures/skips/errors/aborts.
- Parent second fresh Manager archive run: 826 passed across all five projects, zero failures/skips/errors/aborts. Six PowerShell plugin suites rerun with worktree-local TEMP: 193 passed, zero failures/skips/not-run/errors. Manager deployment-script Pester: 3 passed, zero failures/skips; no deployment performed.
- At 13:57-13:59 UTC, audit submission timed out, failsafe replay failed, and triage reported storage backend unreachable. Work stopped. The operator then requested Agent Help; its configured Grok helper failed with HTTP 402 (usage balance exhausted), not a diagnosis.
- A separate operator-directed installed-service configuration change replaced all four Grok selections with Codex and `gpt-6.1-sol`, then restarted MCP Server. Plugin health was Healthy and the pending audit failsafe replayed successfully. Receipt: `F:\GitHub\McpServer\.mcpServer\codex\receipts\20261008T141342Z-installed-grok-to-codex.json`. This was not a rebrand deployment.
- Operator resumed branding at 09:22 CDT. Manager extension compilation/tests passed 11/11 with zero failures/skips and an unchanged lockfile. Node discovery parity proved that the runner selected the exact committed test-file sets (Cline 5/5, Cline-v2 4/4, OpenCode 4/4), covering all 126 passing tests. Raw Git blob hashes matched every core-manifest entry: 144/144 across nine plugins.
- Compatibility validation passed 45/45 ReplBridge cases, three vendored-package SRI checks, 30 Tools manifest structural checks, and nine legacy/new alias comparisons. The aliases preserve source repository, commit, schema, and command template. Diff inspection found no private-profile changes, QuadBrain/QBAgent identity changes, host identity changes, or premature GitHub repository URL renames. These checks do not establish live deployment behavior. Receipts are in the parent evidence directory and its `compatibility` subdirectory.
- Timer validation found a remaining release blocker on pinned Manager commit `d0082d0e02a8e3f10bab5dfd5e5920784ecf8233`: all four Restart races failed (Core and UI.Core, each against Stop and Dispose). Every case required external rescue. Standalone Dispose cancellation passed 2/2. See `timer-probes.jsonl`, `race-probe.log`, and `timer-fixture-provenance.json` in the parent evidence directory. The probe process exited zero, but its semantic results are four failures; do not call this gate green.
- Reproduction: cancellation of the old token blocks in a registered callback; concurrent Restart creates a new token and starts a callback holding the invocation gate; that callback waits for cancellation. Stop/Dispose then waits for the gate before canceling the new token, deadlocking until the probe supplies external rescue. Existing broad suite success does not cover or override this failure.
- Interval-test discrimination passed all four expected-result controls: Core control 12/12, UI.Core control 8/8; each disposable mutant failed only `Restart_WithNewInterval_UsesNewInterval` after interval assignment was disabled. Those two failures are expected negative controls inside the passing outer harness, not the four genuine race failures. Product sources and immutable snapshots were not edited.
- The operator answered: "Yes, Stop/Dispose take precedence." Restart overlapping shutdown is ignored; a new Restart after Stop returns remains valid, and Dispose is final. Codex created `FR-TIMER-001`, `TR-TIMER-LIFECYCLE-001`, and `TEST-TIMER-001`, nine structured acceptance criteria, and their mapping through the plugin. All remain open. The bounded repair is in a separate Manager worktree at `F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\.repair\mgr`, branch `codex/qbrain-timer-precedence-20261008`, based on the pinned Manager head. Its plan is `docs/plans/timer-shutdown-precedence-20261008.md`.
- Before production edits, the complete shared mock contract passed 11/11 with zero failures/skips: five positive scenarios and six expected negative controls. Production base hashes and test hashes are retained in `timer-repair/mock-gate.json` and `timer-repair/mock-gate-complete.json`. The repair then added a short-lived lifecycle-state lock, active-Stop count, and shutdown generation to both timer implementations, retaining the reentrant callback gate and cancel-before-drain ordering. `ITimerHandle` XMLDocs describe precedence.
- The same unchanged test file then passed 21/21: 11 mock cases plus 10 real Core/UI.Core cases, including all four shutdown-overlap combinations, queued Restart, concurrent shutdowns, fresh token after Stop, interval preservation, final Dispose, and callback reentrancy. Receipt: `timer-repair/production-contract.json`; exact TRX: `timer-repair/timer-contract.trx`.
- Final repair regression at 10:08 CDT passed 861 tests with zero failures/skips: Core 168, Desktop 6, Director 140, UI.Core 411, Web 122 (847 managed total), extension 11, deployment-script 3. The first deployment-script attempt failed before assertions because two versions of its module were loaded in the persistent console; both initial failure and passing rerun receipts are retained. Only those test modules were unloaded; no deployment occurred. See `timer-repair/validation-summary.json`, `full-suite.jsonl`, `extension-test.log`, and `deployment-script-clean-session.xml`.
- Generated extension outputs were retained with SHA-256 verification under `timer-repair/retained-generated-output`, then only this validation's generated tracked files were restored to the pinned base. `.repair/mgr/.test-tmp` contains retained test artifacts, not product changes. Source edits are limited to the two timers, shared interface documentation, one shared contract-test file, and the bounded plan. No repair commit or push has occurred, so the draft Manager PR still points to the failing base until an explicitly authorized integration step.
- Documentation inspection also found a minor Grok Bot README article typo (`for an QBrain.AI`) introduced by the rename. Record it for the bounded repair/review; no product documentation was changed in the pinned snapshot.
- The prompt hook initially selected the locked worktree marker instead of the registered root. Triage accepted `triage-report-6b8b26384c7140a490f4801e9bd95483`. Recovery used the supported `plugin-hook.ps1 -HookName user-prompt-submit -WorkspacePath F:\GitHub\McpServer` with root cwd after the deduplication window. No cache file was hand-edited. Current continuation audit writes confirmed `persisted=true`, `degraded=false`.
- Requirements and Phase 2 remain open. No merge, push, deployment, new reviewer, or later-phase action occurred. Claude catch-up review at noon must include all parent validation scripts and receipts, the timer race failure and any subsequently approved repair, interval negative controls, compatibility inspection, requirements/mapping restoration evidence, and these handoff changes. No independent acceptance verdict exists for this continuation.

### Noon Claude catch-up attempt, 2026-10-08

- The one-time noon request was processed. Current pinned source and repair hashes were checked against the final validation receipt. Review scope covered the 11 Phase 2 snapshots, the uncommitted Manager repair, tests, plans, validation scripts, requirements/mapping evidence, and handoff changes. Operator requests, permissions, service configuration/priority, and startup inventories were excluded.
- Claude CLI `2.1.292` was authenticated. Global MCP `MEMORY-PREFERENCE-005` selected `opus-5.5-max` per invocation; the launch requested that model with `--effort max` without changing the global `opus[1m]`/`high` defaults. PowerShell.Mcp launched it without a visible command window at 12:16 CDT, native session ID `37d0910a-60f6-4070-bbcc-d4f39a662c2a`.
- Claude exited 1 before any review tool call. Stderr: `[claude-code:unrecognized_model]` for `opus-5.5-max`. Its complete result states: `There's an issue with the selected model (opus-5.5-max). It may not exist or you may not have access to it. Run --model to pick a different model.` The init event names the selected model, but successful inference and runtime effort were not verified. A stream result subtype of `success` does not override `is_error: true` and exit 1.
- Durable receipts are in locked-worktree `docs/receipts/hv/20261008-noon-claude-catch-up`: `review.prompt.md`, `review.request.jsonl`, the full `review.response.jsonl`, `review.stderr.log`, and `review.blocked.json`. No verdict or scores were produced; no reviewer-owned MCP review turn was opened. The parent stored the complete CLI result and blocked receipt through its supported MCP plugin in session `Codex-20261008T142247Z-plugin-session`, turn `req-20261008T170123Z-prompt-efaf`.
- All review gates remain open. No alternate Claude model or OpenAI reviewer was substituted, and no merge, push, deployment, service change, later phase, or rescheduling occurred. Resume catch-up only after the operator resolves the configured reviewer-model availability or explicitly selects a supported Claude model.

## Original Grok handoff (superseded where contradicted above)

## Goal

Phase 2 needs to pass the BDPv4 exit gate. That means every applicable suite is green (Failed 0, Skipped 0) with nothing quarantined or waived. After that, Astra xhigh hostile validation (HV) has to return AGREE with Accuracy and Completeness both at 98 or higher.

Rules: no merges, PRs stay draft, no Phase 3 or 4-L unless the operator asks for it, and the work is not capped to one slice.

## HV workspace lock (MEMORY-PROCEDURE-003)

The workspace, the codex `-C` directory and `$hvDir` must all be `F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007`. Never use `.worktrees\federation-fixes-20261007`. That rule makes HV r2 invalid.

## Latest HV result (r3)

- Stamp `20261007T183000Z-qbrain-ai-rebrand-phase-2-hv-r3` (gpt-6-astra, xhigh) finished 2026-10-07 at 1:40 PM CT.
- Verdict: **DISAGREE**, Accuracy 97, Completeness 97.
- Receipt and evidence are in `F:\GitHub\McpServer\.worktrees\qbrain-rebrand-hv-20261007\docs\receipts\hv\`, under `...-hv-r3.md` and the `...-hv-r3-evidence\` folder. The evidence includes `director-failures.json`, `manager-isolated\McpServerManager.Director.Tests.test.log` and `manager-isolation-proof.json`.
- Totals: 1143 tests, 1074 passed, 69 failed, 0 skipped.
- All 319 plugin tests pass. Manager Core, UI.Core, Web and Desktop pass. The ReplBridge harness passes 45/45.

## Open blockers

1. **HV-R3-01 (content).** In McpServerManager, 69 of 138 Director tests fail. The Director test runner finds its repo root by walking up parent folders. When it runs from an isolated archive or worktree, that walk ends outside Manager. Failures cover CLI help, configuration, agent, health and exec-area cases. The fix is to find the root from Manager itself, using a marker file, the solution file or the assembly location.
2. **Hermeticity (related).** The HV run hit NU1008 because the Manager archive picked up McpServer's `Directory.Packages.props` from a parent folder. Manager needs its own guard (a root `Directory.Packages.props` and/or the matching `Directory.Build.*` settings) so it builds the same anywhere.
3. **Infrastructure, not content.** The GitHub Actions core-integrity checks on the six shell PRs fail only because the account is billing-locked. Standing direction is not to depend on GitHub Actions.

## State of the fix

Two attempts to fix Director were interrupted because PAYTON-LEGION2 went offline. Nobody has checked whether any local or unpushed change exists in `F:\GitHub\McpServerManager`. HV r4 has **not** been launched. The last confirmed Manager tip is still `43d3158` on draft PR #10.

## Current PR heads

All are draft and open on branch `cursor/qbrain-ai-rebrand-phase2-91dd`.

- claude-code #6 at `2510cc1b`
- cowork #5 at `95bce132`
- codex #5 at `ae35f225`
- copilot #5 at `a0792f4f`
- cline #5 at `c7f24495`
- cline-v2 #5 at `1e13d8e1`
- opencode #4 at `fc20aafd`
- McpServerTools #1 at `11ee9124`
- McpServerManager #10 at `43d31581`
- grok-bot #1 and grok #6: heads were not captured, so re-read them from `F:\GitHub\McpServer\docs\receipts\hv\_phase2-tips-now.json` or git.

## Next steps, in order

1. On Legion, check `git status` and `git log` in `F:\GitHub\McpServerManager` for partial Director work. Also check the HV folder for any green-proof or r4 files, and whether a codex process is running. Pick up from whatever is there.
2. Reproduce the failure the way the HV did: git-archive the Manager tip and expand it under a McpServer-like parent folder. Confirm the same 69 failures.
3. Fix the Director root lookup and the Manager hermeticity. Write tests first wherever behavior changes.
4. Prove all five Manager suites at Failed 0 in both the normal checkout and the isolated archive. Write a green-proof receipt to the locked HV folder.
5. Commit and push as sharpninja to PR #10.
6. Copy the r3 runner (`...-hv-r3.runner.ps1`) under a new r4 stamp with the locked workspace. Include all current tips in the prompt, then run it unbounded. If it returns DISAGREE, fix and repeat.
7. Log to the MCP session through mcpserver-grok-plugin as GrokCode, with `MCP_WORKSPACE_PATH=F:\GitHub\mcpserver`. Do not log in-flight work as complete.

## Other pending items

- Add-profile SHA purge ticket 4773744 (a daily routine is checking it).
- Astra HV on the rebrand plan itself.
