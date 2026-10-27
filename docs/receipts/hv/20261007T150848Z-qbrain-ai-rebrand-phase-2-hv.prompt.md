# Hostile validation prompt - QBrain.AI rebrand Phase 2 (sibling draft PRs)

Stamp: 20261007T150848Z
Required reviewer: Codex
Required model: gpt-6-astra
Required reasoning effort: xhigh
Operator shorthand: astra-6-xhigh
Host for this live run: PAYTON-LEGION2
Workspace: F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
Operator override: Just use astra on legion2 (follow plan model lock gpt-6-astra xhigh)
Plan subject: docs\Project\QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md
Plan SHA-256 on Legion at launch: 6ca0c9e34ec70b906f2538027bf8791c7729fbf8586a2257b76f8f1aec7a57e8
Prep brief: F:\GitHub\mcpserver\docs\receipts\_qbrain-phase2-hv-prep-20261007\HV-BRIEF.md
Prep brief SHA-256: a91e040993fe8b6bab68dbc6eee0d6556b6570b6fb2ec2e97ca57779f6e7a548

You are the hostile reviewer for Phase 2 only. Do not rename namespaces, packages, or repositories. Do not merge any PR. Do not start Phase 3 or Phase 4-L. Do not mark Phase 2 done in the plan. Do not edit the plan.

Write your verdict into this new file (create it; do not overwrite Phase 0 or other receipts):
1) docs/receipts/hv/20261007T150848Z-qbrain-ai-rebrand-phase-2-hv.md

Prove model and effort from your own rollout turn_context: both payload.model / payload.effort and collaboration_mode.settings must show gpt-6-astra and xhigh. If they do not, OverallVerdict is DISAGREE.

## Phase 2 context

Phase 2 acceptance (plan section Phase 2 - one cloud PR per sibling repo) requires one PR per in-scope sibling under the OLD GitHub names, McpServerTools keeping old mcpserver-*-plugin manifests, QuadBrain and qbagent unchanged, and a missing grok-bot remote recorded rather than invented.

Current operator state (verified on Legion before launch): the PRs below are OPEN DRAFT (not merged). Score content readiness against Phase 2 claims. Flag that drafts are not merges. AGREE requires Accuracy >= 98 and Completeness >= 98. Even on AGREE, drafts mean Phase 2 is not done.

Draft PRs / tips to score (unbounded review; use local clones under F:\GitHub when present, else gh):
- https://github.com/sharpninja/mcpserver-grok-plugin/pull/6 @ 632452e7
- https://github.com/sharpninja/mcpserver-grok-bot-plugin/pull/1 @ 8fad06fa
- https://github.com/sharpninja/mcpserver-claude-code-plugin/pull/6 @ 65d992b7
- https://github.com/sharpninja/mcpserver-claude-cowork-plugin/pull/5 @ c63b97a9
- https://github.com/sharpninja/mcpserver-cline-plugin/pull/5 @ 61b3e63b
- https://github.com/sharpninja/mcpserver-cline-v2-plugin/pull/5 @ 17475476
- https://github.com/sharpninja/mcpserver-codex-plugin/pull/5 @ 2054acf3
- https://github.com/sharpninja/mcpserver-copilot-plugin/pull/5 @ 846bd966
- https://github.com/sharpninja/mcpserver-opencode-plugin/pull/4 @ 4fd29839
- https://github.com/sharpninja/McpServerManager/pull/10 @ 242a8968
- https://github.com/sharpninja/McpServerTools/pull/1 @ 11ee9124

Local clones expected at F:\GitHub\<repo> for each of the above except confirm McpServerTools.

## Claims

Score each claim PASS, FAIL, or UNKNOWN. UNKNOWN is not a pass. A FAIL forces OverallVerdict DISAGREE. Do not emit AGREE to be helpful. AGREE only if Accuracy >= 98 and Completeness >= 98.

- C0. Each listed draft PR tip SHA matches the expected tip above (or you record an exact mismatch with evidence).
- C1. There is one open PR per in-scope sibling that exists (the eleven listed). Content targets Phase 2 sibling rebrand work under the old GitHub names.
- C2. Pull requests use the old GitHub repository names (mcpserver-*-plugin, McpServerManager, McpServerTools). No Phase 3 GitHub rename has been performed on these remotes.
- C3. McpServerTools at tip 11ee9124 still contains old mcpserver-*-plugin manifest names (old files still resolve). New qbrain-ai-*-plugin names may be added; old names must not be deleted in this phase.
- C4. QuadBrain and qbagent were not renamed in the scored PR tips.
- C5. mcpserver-grok-bot-plugin remote exists and has PR #1 @ 8fad06fa (not invented; not recorded as missing).
- C6. All eleven PRs remain DRAFT / unmerged. Record this as a Phase 2 done-blocker even if content scores AGREE: drafts mean Phase 2 is not done.
- C7. Private add-profile content was not copied into the public repos by these PRs (spot-check diffs; FAIL only with file evidence).
- C8. Model and effort proof from this reviewer's own rollout turn_context shows gpt-6-astra and xhigh.

## Out of scope

Do not fail Phase 2 because Phase 3 renames have not happened. Do not fail because Phase 1 aliases are incomplete unless a scored PR itself breaks a Phase 2 acceptance item. Do not merge. Do not start Phase 3/4-L.

## Receipt template

Copy this shape into docs/receipts/hv/20261007T150848Z-qbrain-ai-rebrand-phase-2-hv.md. Replace the placeholders. Do not leave NOT RUN if you actually ran.

- TimestampUtc: <utc>
- ValidatorIdentity: Codex
- Model: gpt-6-astra
- Effort: xhigh
- Host: PAYTON-LEGION2
- Workspace: F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
- Proof: <rollout jsonl path and turn_context timestamp>
- PrepBriefSha256: a91e040993fe8b6bab68dbc6eee0d6556b6570b6fb2ec2e97ca57779f6e7a548
- PlanSha256: 6ca0c9e34ec70b906f2538027bf8791c7729fbf8586a2257b76f8f1aec7a57e8
- OverallVerdict: AGREE or DISAGREE
- Accuracy: <0-100>
- Completeness: <0-100>
- PASS / FAIL / UNKNOWN counts
- Phase2Done: no (drafts remain; even AGREE does not mark phase done while drafts are unmerged)
- One line per claim id with PASS, FAIL, or UNKNOWN and the evidence path or PR tip you used