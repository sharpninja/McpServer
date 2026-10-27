# QBrain.AI rebrand Phase 2 hostile validation

- TimestampUtc: 2026-10-07T15:39:25Z
- ValidatorIdentity: Codex
- Model: gpt-6-astra
- Effort: xhigh
- Host: PAYTON-LEGION2
- Workspace: F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
- Proof: C:\Users\kingd\.codex\sessions\2026\10\07\rollout-2026-10-07T10-09-31-01a116e9-9af4-7570-8a23-e84f0b17a42f.jsonl, turn_context at line 8, timestamp 2026-10-07T15:09:36.255Z
- PrepBriefSha256: a91e040993fe8b6bab68dbc6eee0d6556b6570b6fb2ec2e97ca57779f6e7a548
- PlanSha256: 6ca0c9e34ec70b906f2538027bf8791c7729fbf8586a2257b76f8f1aec7a57e8
- OverallVerdict: DISAGREE
- Accuracy: 90
- Completeness: 90
- PASS / FAIL / UNKNOWN counts for C0-C8: 9 / 0 / 0
- Additional Phase 2 content-readiness checks: 0 PASS / 3 FAIL / 0 UNKNOWN
- Combined evaluated checks: 9 PASS / 3 FAIL / 0 UNKNOWN
- Phase2Done: no (all eleven PRs remain OPEN DRAFT and unmerged; even AGREE would not mark this phase done)

The nine enumerated claims pass **as written**. They establish the requested repository, manifest, privacy spot-check, draft-state, and reviewer facts; they do not establish that the PR contents satisfy the plan's Phase 2 runtime and test acceptance. The additional failures below prevent content-readiness AGREE. Accuracy and Completeness score that broader requested readiness, not the arithmetic percentage of C0-C8 that pass. The two scores are reviewer assessments: command selection is incomplete, shared-core synchronization is missing, and the zero-failure acceptance gate is not met. Outstanding test coverage is disclosed below.

The operator's explicit draft-review instruction governs this review. The plan's merged-PR checkpoint is not used to fail draft content solely because the PRs are unmerged. No Phase 1 alias deficiency, missing Phase 3 rename, or Phase 4-L work is used as a failure.

## Claim results

- C0 — PASS. All eleven live full head SHAs match the requested prefixes; initial and final GitHub queries agree. Evidence: review-evidence JSONL records 1-11 and 57-67, and the immutable tips listed below.
- C1 — PASS. Each of the eleven old-name repositories has exactly one open PR, the listed Phase 2 PR; every PR targets its repository's default branch. All 132 changed-file entries were inventoried, and the diffs target sibling rebrand work. This literal claim is not a certification that those contents are ready. Evidence: records 1-22 and 49.
- C2 — PASS. GitHub repository API canonical full_name values remain sharpninja/mcpserver-*-plugin, sharpninja/McpServerManager, and sharpninja/McpServerTools. Old URLs do not merely redirect to renamed repositories. Evidence: records 1-11.
- C3 — PASS. At McpServerTools 11ee91241943c3032ce5ec6ea66fcb374f6dc518, all nine old mcpserver-*-plugin.json names resolve to valid JSON blobs. The eight pre-existing old manifests are byte-for-byte unchanged; the grok-bot old manifest is newly added. Nine qbrain-ai aliases are added beside them. All 18 manifests retain old GitHub repository URLs and valid parameter-schema JSON. Evidence: record 48; non-truncated base and head Git trees.
- C4 — PASS. No semantic addition/removal hunk changes QuadBrain, QBAgent, or qbagent in any of the eleven scored diffs. Manager namespaces/application IDs stay old; its changes are display text. Evidence: records 12-22 and 49. Whitespace-only CRLF/LF churn was separated from semantic changes.
- C5 — PASS. GitHub confirms sharpninja/mcpserver-grok-bot-plugin exists; PR #1 has head 8fad06fa6df805300a3b8e8cb303068d62a62084. It is not recorded as missing and was not invented by this reviewer. Evidence: records 2 and 58; https://github.com/sharpninja/mcpserver-grok-bot-plugin/pull/1.
- C6 — PASS. All eleven PRs are OPEN, isDraft=true, mergedAt=null in the final live recheck. This is an explicit Phase 2 done-blocker, independently of the content verdict. Evidence: records 57-67.
- C7 — PASS, at the requested spot-check scope. No add-profile/private-profile file is introduced or changed in the 132-file inventory, and inspected added content contains no copied private profile. OpenCode's existing /add-profile instruction is unchanged context, not newly copied profile material. Public author metadata and public hook instructions are not treated as private profile content. Evidence: records 12-22 and 49. This is a diff spot-check, not a comprehensive secret audit.
- C8 — PASS. This review's own rollout contains payload.model=gpt-6-astra and payload.effort=xhigh; collaboration_mode.settings.model=gpt-6-astra and collaboration_mode.settings.reasoning_effort=xhigh. The same rollout contains the exact Phase 2 user prompt/stamp. Evidence: the Proof path and timestamp above; record 49. Adjacent codex-auto-review guardian rollouts were excluded.

## Content-readiness failures

- R1 — FAIL, high priority: the Node REPL callers do not support a default installation containing only qbrain-ai-repl. Cline 61b3e63b, Cline v2 17475476, and OpenCode 4fd29839 all ship the same vendor/sharpninja-mcpserver-plugin-core.tgz (SHA-256 9c59a438a71f73151c1153a877ce730bfbf8f47b4f39b808053934300d7d1356). Its package/dist/transport/repl-bridge.js, line 97, selects process.env.MCPSERVER_REPL_COMMAND || 'mcpserver-repl', then spawns that command at line 106. Cline src/index.ts:119 accepts either executable during startup, and the changed PowerShell installer prefers the new executable/package, but the actual bundled Node bridge still selects the old name. A controlled harness executing the committed bundled JS and capturing child_process.spawn selected mcpserver-repl in all three cases without an override. This is a captured command-selection result, not a claim that a real REPL process was launched. When only the new command is installed, the selected legacy command cannot resolve. An explicit MCPSERVER_REPL_COMMAND override is not default dual-name support. This fails the Phase 2 caller requirement in plan line 570. Evidence: records 47, 51-53 and 55; Cline src/index.ts:14,53,119; Cline v2 src/plugin.ts:152; OpenCode src/plugin.ts:154. Remediation is to update and test the bundled core's command resolution, preserving the legacy fallback.

- R2 — FAIL, high priority: all nine plugin PRs change synchronized lib files without refreshing CORE-MANIFEST.yaml through the core synchronization workflow. The three reviewed entries are lib/ensure-repl.ps1, lib/plugin-env.ps1, and lib/repl-invoke.ps1. All 27 head hashes mismatch the committed manifest. Twenty-five of those entries match at the respective PR base and diverge at the PR head; grok-bot's other two mismatches already exist at base. In particular, ensure-repl.ps1 matches at base in all nine repositories: expected/base SHA-256 2d618985fd8fd40f184e3fe2582b475db19241a16a1c55153dae42c9ca12e894, versus head 31bede5ab01c4ed0a475428f3185a92f3faff26e100c6b420aea997b9091c96b. The six shell-plugin core-guard workflows explicitly fail on these mismatches. Direct SHA-256 computation used raw git blob bytes, not newline-normalized PowerShell output. This establishes a content defect independently of hosted CI availability. Evidence: record 40; each plugin CORE-MANIFEST.yaml:7,13,15 and .github/workflows/core-guard.yml where present. Remediation is to apply the shared change in the canonical core and synchronize it, preserving the integrity check rather than disabling it.

- R3 — FAIL, medium priority: the Phase 2 zero-failure test gate is not met. At Cline 61b3e63b, npm test -- --runInBand reports 30 passed and 2 failed, with zero skipped. The rebrand-specific failure is tests/plugin-helpers.test.ts:96-97: it still expects @sharpninja/mcpserver-cline-plugin while the PR's package.json:2 and runtime identity now return @qbrainai/qbrain-ai-cline-plugin. The base control passes that identity assertion and reaches a separate marker-trust failure at line 101; therefore identical overall base/head failure counts do not erase the new assertion mismatch. The missing hooks/scripts/memory-context.ps1 failure is present at both base and head and is not attributed to this PR. Evidence: validation-log records for the head and base Cline logs, and final-test-summary record 104. Update the rebrand expectation and establish the plan's zero-failure/zero-skip gate; do not hide existing failures.

The nine plugin PR descriptions are also stale: they describe fuller commits as unpushed and, for the Node plugins, claim qbrain-env.ts is not imported. The scored tips already contain those fuller changes and imports. The review scores actual head contents, not that stale prose.

## Executed validation and limits

- Live gh pr view, gh pr list --state open, and gh api repository reads covered all eleven repositories. Each open-PR result contained one entry, below the requested limit of 100. Final state/head checks were repeated after review.
- Local git objects supplied ten PR diffs. McpServerTools has a local clone, but the scored head object was absent, so its PR diff and exact Git tree/blob contents were read through gh. No fetch or checkout changed a sibling clone.
- All 132 changed-file entries and all eleven full diffs were captured. Shared library logic was inspected with git diff --ignore-space-at-eol as well as the raw diff; the large repl-invoke.ps1 raw hunks include line-ending churn.
- Node 20.20.2: npm ci --ignore-scripts --no-audit --no-fund succeeded in archived head snapshots for Cline, Cline v2 and OpenCode. npm run build succeeded for all three. Dependency lifecycle scripts were not run by npm ci.
- Clean-location npm test -- --runInBand: Cline 30 passed / 2 failed / 0 skipped; Cline v2 27 passed / 0 failed / 0 skipped; OpenCode 67 passed / 0 failed / 0 skipped. These green suites do not test away the separately reproduced bundled-bridge defect.
- Pester 5.7.1, all *.Tests.ps1 under each shell plugin's tests directory, using fresh pwsh.exe processes and cleared workspace/plugin environment overrides: Grok 18 passed / 11 failed; Grok Bot 18 / 11; Claude Code 23 / 13; Claude Cowork 25 / 4; Codex 30 / 11; Copilot 18 / 11. Every run had zero skipped and zero not-run tests. Combined: 132 passed / 61 failed / 193 total. Grok's base control also reports 18 passed / 11 failed. These are observed suite results, not a claim that the PR introduced all failures.
- Six hosted core-integrity checks report FAILURE because the account billing lock prevented their jobs from starting. GitHub annotations explicitly report that reason; job steps are empty and execution logs are unavailable. These hosted failures are classified as infrastructure blockers, not test executions. The direct manifest-hash failure above is separate evidence. See records 41-46.
- The first Node test runs under F:\GitHub\McpServer\.mcpServer\tmp found no tests because of the test-discovery path. They were superseded by the executed clean-location runs. The first PowerShell runs were also repeated outside that ancestor workspace before reporting final results.
- Clean validation root: C:\Users\kingd\AppData\Local\Temp\QBrainHv20261007T150848ZArchives. Immutable source archives were used. Node dependencies were reused through junctions to the dependencies installed for those same archived tips. A slow intermediate dependency-copy attempt was canceled; its partial copies were retained.
- McpServerManager's .NET suites, McpServerTools' existing test command, and the Bash/Bats suites were not executed. PowerShell was the required shell. No all-repository zero-failure acceptance is asserted. McpServerTools did receive the 18-manifest JSON/tree/blob checks described in C3.
- This review did not edit the plan, merge or ready any PR, alter repository settings, rename a repository/package/namespace, or start Phase 3/4-L. Plan and prep hashes were rechecked and remain exactly as supplied. Existing worktree changes and prior Phase 0 receipts were not edited.

## Scored immutable tips

- [mcpserver-grok-plugin #6](https://github.com/sharpninja/mcpserver-grok-plugin/pull/6): 632452e7c20ddbedd5d712d2f9a099c4fd2cc6c5
- [mcpserver-grok-bot-plugin #1](https://github.com/sharpninja/mcpserver-grok-bot-plugin/pull/1): 8fad06fa6df805300a3b8e8cb303068d62a62084
- [mcpserver-claude-code-plugin #6](https://github.com/sharpninja/mcpserver-claude-code-plugin/pull/6): 65d992b711da6bc84ce5014736f6dada2f6b152e
- [mcpserver-claude-cowork-plugin #5](https://github.com/sharpninja/mcpserver-claude-cowork-plugin/pull/5): c63b97a938bda88ccf6dd0b830eb762d7487548c
- [mcpserver-cline-plugin #5](https://github.com/sharpninja/mcpserver-cline-plugin/pull/5): 61b3e63b30d6711afb21ce47ad51b8aa21e9f5fc
- [mcpserver-cline-v2-plugin #5](https://github.com/sharpninja/mcpserver-cline-v2-plugin/pull/5): 174754767b28ba8e63334e1546fcf0c11f97a04c
- [mcpserver-codex-plugin #5](https://github.com/sharpninja/mcpserver-codex-plugin/pull/5): 2054acf3aa4154f94f6fe2feced2dabe3bed17b8
- [mcpserver-copilot-plugin #5](https://github.com/sharpninja/mcpserver-copilot-plugin/pull/5): 846bd966544f4be995694edaf567efb948b9c7c8
- [mcpserver-opencode-plugin #4](https://github.com/sharpninja/mcpserver-opencode-plugin/pull/4): 4fd298395f5403f8356d455c4041e72f2cb6d2c0
- [McpServerManager #10](https://github.com/sharpninja/McpServerManager/pull/10): 242a8968884ce70f064691f19c77b0920a4598af
- [McpServerTools #1](https://github.com/sharpninja/McpServerTools/pull/1): 11ee91241943c3032ce5ec6ea66fcb374f6dc518

## Evidence and audit

The companion [review-evidence JSONL](20261007T150848Z-qbrain-ai-rebrand-phase-2-hv.review-evidence.jsonl) contains 104 records: raw PR metadata, full diffs, Git blob hashes, manifest validation, CI annotations, bundled JS and reproduction script, complete validation logs, and final live PR rechecks. Record numbers above are one-based physical line numbers. SHA-256: 5030ea24a61b8a0434b36f0ada2b61d0578ad5a0ef5ca780815f3a931f138e41.

The companion [review-progress JSONL](20261007T150848Z-qbrain-ai-rebrand-phase-2-hv.review-progress.jsonl) preserves progress percentages and ETAs emitted during this run.

Both required model/effort pairs were read from the reviewer's own rollout, not inferred from a launch command or the requested model:

```json
{
  "timestamp": "2026-10-07T15:09:36.255Z",
  "type": "turn_context",
  "payload": {
    "model": "gpt-6-astra",
    "effort": "xhigh",
    "collaboration_mode": {
      "mode": "default",
      "settings": {
        "model": "gpt-6-astra",
        "reasoning_effort": "xhigh"
      }
    }
  }
}
```

The workspace marker was absent. The installed per-turn hook returned turn-open-failed because it reported opening req-20261004T200849Z-prompt-d38a instead of its fresh requested ID. The review used the supported native MCP session-log tools to open a distinct Codex audit session successfully: Codex-20261007T150932Z-qbrain-phase2-hv, turn req-20261007T150848Z-qbrain-phase2-hv. This audit recovery did not change the review scope or any PR state.
