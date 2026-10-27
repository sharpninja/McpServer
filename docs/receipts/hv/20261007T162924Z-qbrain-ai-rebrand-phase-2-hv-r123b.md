# QBrain.AI Phase 2 hostile validation: R1-R3 remediation, round 2

- TimestampUtc: 2026-10-07T16:47:17.8584137Z
- ValidatorIdentity: Codex
- Host: PAYTON-LEGION2
- Workspace: F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
- OverallVerdict: DISAGREE
- Accuracy: 96
- Completeness: 94
- ModelLock: PASS, gpt-6-astra / xhigh in both required rollout locations.
- R1: PASS, targeted command-selection remediation.
- R2: PASS, all 27 targeted raw Git blob hashes.
- R3: PASS, corrected Cline package-identity expectation.
- VendorLockIntegrityAndNpmCi: PASS, all three Node plugins.
- Phase2TestAcceptance: FAIL.
- Phase2Done: no. All eleven PRs remain OPEN DRAFT and unmerged. The plan was not edited.

The specified remediation changes are verified. Overall Phase 2 content readiness still fails the plan's per-repository requirement that existing tests finish with zero failures and zero skips. The current runs produce 63 failed tests and zero skipped tests across nine plugin repositories. Cline's two failures also reproduce at its current PR base. Four non-targeted Grok Bot core-manifest mismatches remain and are unchanged from its base. Existing defects are not attributed to the rebrand, but no baseline-relative exception to the zero-failure gate was supplied.

Accuracy and Completeness are reviewer assessments of content readiness, not arithmetic pass percentages. Accuracy remains below 98 because a claim of fully ready contents would omit the failing tests and remaining integrity failures. Completeness remains below 98 because those acceptance gates are unsatisfied and the Manager, Tools and Bash/Bats test scopes are not fully validated here. Draft status is reported separately and is not, by itself, the reason for the content-readiness DISAGREE.

## Inputs and immutability

- Plan: docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md.
- Plan SHA-256, verified initially and after validation: 6ca0c9e34ec70b906f2538027bf8791c7729fbf8586a2257b76f8f1aec7a57e8.
- Prep brief: F:\GitHub\McpServer\docs\receipts\_qbrain-phase2-hv-prep-20261007\HV-BRIEF.md.
- Prep brief SHA-256, verified initially and after validation: a91e040993fe8b6bab68dbc6eee0d6556b6570b6fb2ec2e97ca57779f6e7a548.
- Prior receipts 20261007T150848Z-qbrain-ai-rebrand-phase-2-hv.md and 20261007T160847Z-qbrain-ai-rebrand-phase-2-hv-r123.md were read, not overwritten.
- Latest user-specified tips supersede the older tips and Grok dispatch language in the prep brief. The operator explicitly authorized Astra on Legion2.
- This review made no source remediation, namespace/package/repository rename, PR merge, PR-ready transition, deployment, Phase 3 work, Phase 4-L work, or plan completion edit.

## Model proof from this review's own rollout

Proof file:

C:\Users\kingd\.codex\sessions\2026\10\07\rollout-2026-10-07T11-29-28-01a11732-cdab-7543-89c3-ebd0d94a8c73.jsonl

The session_meta record at line 1 identifies session 01a11732-cdab-7543-89c3-ebd0d94a8c73 and the requested worktree. Its user message contains this round's exact receipt filename. The turn_context record at line 8 is:

```json
{
  "timestamp": "2026-10-07T16:29:32.098Z",
  "type": "turn_context",
  "payload": {
    "model": "gpt-6-astra",
    "effort": "xhigh",
    "collaboration_mode": {
      "mode": "default",
      "settings": {
        "model": "gpt-6-astra",
        "reasoning_effort": "xhigh",
        "developer_instructions": null
      }
    }
  }
}
```

Both required pairs match. This is not inferred from the launch request. The persistent PowerShell console initially exposed the previous review's inherited CODEX_THREAD_ID; that value was rejected as proof and replaced with the current execution session's own rollout. The extracted proof is in model-proof.json.

## R1: PASS

Cline, Cline v2 and OpenCode contain an identical vendor/sharpninja-mcpserver-plugin-core.tgz:

- SHA-256: 395f140e30b3ace1e1aebffed30e24a0df9a8ad8a34be7d3a0d8192e4d2be1e8.
- Bundled package/dist/transport/repl-bridge.js:52-68 defines resolveReplCommand.
- Explicit MCPSERVER_REPL_BIN or MCPSERVER_REPL_COMMAND wins. Otherwise the resolver checks qbrain-ai-repl, then mcpserver-repl, and defaults to qbrain-ai-repl when neither is found.
- ReplBridge.ensure uses that resolver at line 117 and passes its result to child_process.spawn at line 126.

The independent bridge-harness.cjs executed the committed bundled JavaScript from each tarball in a controlled VM. It tested new-only, legacy-only, both installed, neither installed, explicit command override, and explicit binary override precedence on modeled Windows and Linux paths. Windows also covered .cmd candidates. All 45 cases passed. Each case asserted both the resolver result and the captured spawn command, agent arguments and working directory.

This proves selection behavior with controlled filesystem and child-process dependencies. It does not claim a real installed REPL, real Linux execution, or a real Windows .cmd child process was launched. Evidence: bridge-harness.cjs, bridge-results.jsonl and vendor-integrity.json.

## R2: PASS for the requested 27 entries

Raw bytes were read with git cat-file blob from each immutable PR head. All three requested entries match CORE-MANIFEST.yaml in all nine plugins:

- lib/ensure-repl.ps1.
- lib/plugin-env.ps1.
- lib/repl-invoke.ps1.

Evidence: raw-blob-manifest-hashes.json contains every expected/actual hash and targeted flag. Result: 27/27 targeted matches.

The broader manifest check is 140/144 matches. Grok Bot retains four non-targeted mismatches:

- lib/plugin-hook.ps1.
- lib/resolve-cache-dir.ps1.
- lib/Invoke-McpPlugin.ps1.
- lib/agent-runtime-header.ps1.

Their raw source bytes are identical at Grok Bot base 99fcee9a20d714b88f3afa5cb17090498a277d23 and head 289a28bc70325b5afd54b9263fa6da5a8efba4bc. baseline-hashes.jsonl records both hashes. These are inherited full-core-integrity failures, not failures of the three requested refreshed entries. The core-guard workflow checks all lib entries and fails on mismatches.

An initial archive-file check reported extra Grok Bot mismatches. That measurement is retained in manifest-hashes.json but is superseded by raw-blob-manifest-hashes.json for committed content. Archive materialization can differ from raw committed bytes; no defect is assigned from those extra archive-only mismatches. This review verifies the committed hashes and does not certify how the canonical-core synchronization was performed.

## R3: PASS for the identity fix; original zero-failure gate remains FAIL

At Cline head a4b029fc7ec3aca465a9cafc3a8c4c9ee9eaa8b2, tests/plugin-helpers.test.ts:96-99 now expects pluginName '@qbrainai/qbrain-ai-cline-plugin'. package.json uses that identity. The executed test passes the identity assertion and reaches the later failing assertion at line 101.

Cline still reports 30 passed, 2 failed, 0 skipped:

- plugin-helpers.test.ts:101 expects marker trust 'missing' but receives 'signature_verified'.
- memory-skill.test.ts:54 cannot read hooks/scripts/memory-context.ps1 because the file is absent.

A separate clean snapshot of current PR base f943ca4d23e8f9efdad5b780c4e3a90f2a410c42 also passes npm ci and build, then reports the same two failing tests, 30 passed and 0 skipped. The rebrand-specific identity mismatch is therefore fixed; the broader original R3 demand for a green suite is not closed. No test was removed or skipped to obtain this result.

Evidence: mcpserver-cline-plugin.jest.json, mcpserver-cline-plugin.test.log, mcpserver-cline-plugin-base.jest.json, the corresponding worker.json files, and the saved remediation diff.

## Vendor lockfile refresh and npm ci: PASS

The SHA-512 SRI computed directly from each tarball matches its package-lock.json entry:

```text
sha512-hKO/sAwEz4JYJBBwsk1kwG/t/mF3e7ILNCNihF5/9MhDlbZoATZNJ1BRyjJkIsoXKctQeq95K1obdYIOFHAcFw==
```

Node 20.20.2 and npm 10.8.2 executed, in fresh committed-head snapshots without pre-existing node_modules:

```text
npm ci --no-audit --no-fund
npm run build
npm test -- --runInBand --json --outputFile=<evidence path>
```

Dependency lifecycle scripts were enabled; --ignore-scripts was not used. All three npm ci commands and all three builds exited 0. No EINTEGRITY occurred, and package-lock.json SHA-256 was unchanged before and after each run.

- Cline: 366 packages installed; tests 30 passed / 2 failed / 0 skipped.
- Cline v2: 523 packages installed; tests 27 passed / 0 failed / 0 skipped.
- OpenCode: 306 packages installed; tests 67 passed / 0 failed / 0 skipped.

Cline v2 emitted dependency engine warnings on Node 20; its install, build and tests nevertheless exited 0. No broader supported-engine certification is inferred.

The semantic lockfile comparison against the original Phase 2 tips found no dependency version changes. Besides the three SRI replacements, Cline's root bin metadata gains qbrain-ai-cline, and Cline v2 loses four optional Anthropic Linux-package libc filter fields. These changes are recorded in lock-semantic-diff.jsonl. Linux dependency installation was not tested.

Evidence: vendor-integrity.json, lock-semantic-diff.jsonl, and each repository's npm-ci.log, build.log, test.log, jest.json and worker.json.

## Remaining Phase 2 acceptance failure

Fresh pwsh.exe -NoProfile -NonInteractive processes, Pester 5.7.1 and cleared inherited workspace/plugin overrides executed all tests/*.Tests.ps1 recursively for each shell plugin:

- Grok: 18 passed, 11 failed, 0 skipped, 0 not run.
- Grok Bot: 18 passed, 11 failed, 0 skipped, 0 not run.
- Claude Code: 23 passed, 13 failed, 0 skipped, 0 not run.
- Claude Cowork: 25 passed, 4 failed, 0 skipped, 0 not run.
- Codex: 30 passed, 11 failed, 0 skipped, 0 not run.
- Copilot: 18 passed, 11 failed, 0 skipped, 0 not run.

Pester total: 132 passed / 61 failed / 193 tests. The three Node suites total 124 passed / 2 failed / 126 tests. Combined current-head scope: 256 passed / 63 failed / 319 tests, zero skipped. The 45 controlled resolver cases and Cline base control are separate from these totals.

Failures include absent memory-context wrappers, failsafe-result assertions, workspace-resolution expectations and shell command resolution. The Pester logs and XML results preserve exact failure locations. These current results agree numerically with the prior full Phase 2 receipt, but this review did not run all six PR bases and does not claim all failures were introduced by, or independent of, the rebrand.

The latest GitHub metadata reports failed core-integrity checks for the six shell repositories. Hosted job annotations were not independently re-investigated in this round, so those check labels are not counted as executed tests or used to assign a new cause.

## Other Phase 2 content checks

- PASS: all eleven initial and final live GitHub head checks match the requested tips.
- PASS: each repository has one open PR, the specified draft, targeting its current default branch. Canonical repository names retain the old GitHub names, not just redirecting URLs.
- PASS: all 145 changed-file entries were inventoried, with full diffs and remediation diffs retained.
- PASS: McpServerTools has all nine old mcpserver-*-plugin.json names and nine qbrain-ai aliases. All 18 manifests and parameter-schema strings parse. Eight pre-existing old manifests are byte-identical to base; the old-name Grok Bot manifest is newly added. Repository URLs remain the old names.
- PASS: no semantic added/removed diff line changes QuadBrain, QBAgent or qbagent across the eleven PRs. Inspected Manager changes affect displayed branding and its corresponding title assertion.
- PASS: sharpninja/mcpserver-grok-bot-plugin exists, with PR #1 at the specified head. It was not invented or reported missing.
- PASS at spot-check scope: the changed-file inventory adds no add-profile/private-profile file, and inspected added content contains no copied private operator profile. This is not a comprehensive secret audit.
- PASS: final state for all eleven PRs is OPEN, isDraft=true, mergedAt=null. This independently prevents claiming Phase 2 done.

## Scored immutable heads

- https://github.com/sharpninja/mcpserver-grok-plugin/pull/6: 6ed6d0b6b3d1438fef9238a36092dcf00cbfadf3.
- https://github.com/sharpninja/mcpserver-grok-bot-plugin/pull/1: 289a28bc70325b5afd54b9263fa6da5a8efba4bc.
- https://github.com/sharpninja/mcpserver-claude-code-plugin/pull/6: bf39281b7e371f44639ae8f41c7f12edda7e7af6.
- https://github.com/sharpninja/mcpserver-claude-cowork-plugin/pull/5: b46de28b5bc949ae0b39e0eedcbb571005e000a2.
- https://github.com/sharpninja/mcpserver-cline-plugin/pull/5: a4b029fc7ec3aca465a9cafc3a8c4c9ee9eaa8b2.
- https://github.com/sharpninja/mcpserver-cline-v2-plugin/pull/5: 36c95e48c9452907463b44b5edf5ac6df4c4dbea.
- https://github.com/sharpninja/mcpserver-codex-plugin/pull/5: 3490cc1d9b6f0e0d534dde8217b5914a937cdd4e.
- https://github.com/sharpninja/mcpserver-copilot-plugin/pull/5: b374c0a184e1314ae3b125d9ef4f30dffb51b0a2.
- https://github.com/sharpninja/mcpserver-opencode-plugin/pull/4: fc20aafda2da612d72ad0171792e369e4438a121.
- https://github.com/sharpninja/McpServerManager/pull/10: 242a8968884ce70f064691f19c77b0920a4598af.
- https://github.com/sharpninja/McpServerTools/pull/1: 11ee91241943c3032ce5ec6ea66fcb374f6dc518.

## Evidence, limits and receipt-first audit ordering

Evidence directory, shared by both receipt copies:

F:\GitHub\McpServer\.worktrees\federation-fixes-20261007\docs\receipts\hv\20261007T162924Z-qbrain-ai-rebrand-phase-2-hv-r123b-evidence

inventory.json, per-repository metadata.json, full diffs, model-proof.json, final-pr-state.json, validation scripts, command logs, Jest results and Pester XML are retained there. Clean executed snapshots are under C:\Users\kingd\AppData\Local\Temp\QBrainHv-r123b-20261007. Initial source archives under the console's inherited temporary directory were retained; tests were run from the clean C: snapshots.

Manager .NET suites, a full Tools test command, Bash/Bats suites, Linux installation and real installed-REPL integration were not executed. Manager and Tools tips are unchanged from the original review. No all-repository green gate is asserted.

The worktree marker is absent; the parent workspace marker and installed Codex enforcement skill were read. Review audit hooks that could invoke triage, failsafe submission or the active-turn stop gate were deliberately deferred until after this full receipt, as explicitly directed by the operator. Isolated repository tests of hook/failsafe behavior are validation evidence, not completion of this review's audit lifecycle. The receipt is created and copied before attempting deferred audit handling, so audit failure cannot suppress the verdict artifact.
