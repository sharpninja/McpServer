# QBrain.AI rebrand Phase 2 hostile validation (R1-R3 remediation re-HV round 2)

Workspace: F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
Operator override: Just use astra on legion2 (follow plan model lock gpt-6-astra xhigh)
Plan subject: docs\Project\QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md
Plan SHA-256 on Legion at launch: 6ca0c9e34ec70b906f2538027bf8791c7729fbf8586a2257b76f8f1aec7a57e8
Prep brief: F:\GitHub\McpServer\docs\receipts\_qbrain-phase2-hv-prep-20261007\HV-BRIEF.md
Prep brief SHA-256: a91e040993fe8b6bab68dbc6eee0d6556b6570b6fb2ec2e97ca57779f6e7a548
Prior Phase 2 HV: docs/receipts/hv/20261007T150848Z-qbrain-ai-rebrand-phase-2-hv.md (DISAGREE R1-R3)
Prior remediation HV incomplete: docs/receipts/hv/20261007T160847Z-qbrain-ai-rebrand-phase-2-hv-r123.md (DISAGREE; EINTEGRITY on vendor lockfiles; formal md stubbed from last-message)

You are the hostile reviewer for Phase 2 only after remediation of R1-R3 plus vendor package-lock integrity refresh. Do not rename namespaces, packages, or repositories. Do not merge any PR. Do not start Phase 3 or Phase 4-L. Do not mark Phase 2 done in the plan. Do not edit the plan.

IMPORTANT: Write the full verdict receipt BEFORE any triage_report / failsafe / AGENTS stop hooks. Do not ask permission to write the receipt. Create the file even if triage submission fails.

Write your verdict into this new file (create it; do not overwrite prior receipts):
1) docs/receipts/hv/20261007T162924Z-qbrain-ai-rebrand-phase-2-hv-r123b.md

Also copy the same receipt to F:\GitHub\McpServer\docs\receipts\hv\20261007T162924Z-qbrain-ai-rebrand-phase-2-hv-r123b.md when finished.

Prove model and effort from your own rollout turn_context: both payload.model / payload.effort and collaboration_mode.settings must show gpt-6-astra and xhigh. If they do not, OverallVerdict is DISAGREE.

## Remediation under review

R1: Node ReplBridge prefers qbrain-ai-repl then mcpserver-repl in vendor/sharpninja-mcpserver-plugin-core.tgz (Phase 1 pattern).
R2: CORE-MANIFEST.yaml hashes refreshed for lib/ensure-repl.ps1, lib/plugin-env.ps1, lib/repl-invoke.ps1 on all nine plugins.
R3: Cline status identity expectation updated to @qbrainai/qbrain-ai-cline-plugin.
Follow-up: package-lock.json integrity for the vendor tarball refreshed; npm ci must succeed on cline, cline-v2, and opencode.

Re-score Phase 2 content readiness and specifically verify R1-R3 and npm ci. AGREE requires Accuracy >= 98 and Completeness >= 98. Drafts are not merges.

## Draft PRs / tips to score

- https://github.com/sharpninja/mcpserver-grok-plugin/pull/6 @ 6ed6d0b6
- https://github.com/sharpninja/mcpserver-grok-bot-plugin/pull/1 @ 289a28bc
- https://github.com/sharpninja/mcpserver-claude-code-plugin/pull/6 @ bf39281b
- https://github.com/sharpninja/mcpserver-claude-cowork-plugin/pull/5 @ b46de28b
- https://github.com/sharpninja/mcpserver-cline-plugin/pull/5 @ a4b029fc
- https://github.com/sharpninja/mcpserver-cline-v2-plugin/pull/5 @ 36c95e48
- https://github.com/sharpninja/mcpserver-codex-plugin/pull/5 @ 3490cc1d
- https://github.com/sharpninja/mcpserver-copilot-plugin/pull/5 @ b374c0a1
- https://github.com/sharpninja/mcpserver-opencode-plugin/pull/4 @ fc20aafd
- https://github.com/sharpninja/McpServerManager/pull/10 @ 242a8968
- https://github.com/sharpninja/McpServerTools/pull/1 @ 11ee9124

## Required receipt fields

Include OverallVerdict (AGREE/DISAGREE), Accuracy (0-100), Completeness (0-100), model proof, and explicit R1/R2/R3 PASS/FAIL with evidence. Use no em-dashes.