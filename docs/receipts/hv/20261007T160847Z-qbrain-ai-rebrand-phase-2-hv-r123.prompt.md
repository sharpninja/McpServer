# QBrain.AI rebrand Phase 2 hostile validation (R1-R3 remediation re-HV)

Workspace: F:\GitHub\McpServer\.worktrees\federation-fixes-20261007
Operator override: Just use astra on legion2 (follow plan model lock gpt-6-astra xhigh)
Plan subject: docs\Project\QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md
Plan SHA-256 on Legion at launch: 6ca0c9e34ec70b906f2538027bf8791c7729fbf8586a2257b76f8f1aec7a57e8
Prep brief: F:\GitHub\McpServer\docs\receipts\_qbrain-phase2-hv-prep-20261007\HV-BRIEF.md
Prep brief SHA-256: a91e040993fe8b6bab68dbc6eee0d6556b6570b6fb2ec2e97ca57779f6e7a548
Prior Phase 2 HV (DISAGREE on R1-R3): docs/receipts/hv/20261007T150848Z-qbrain-ai-rebrand-phase-2-hv.md

You are the hostile reviewer for Phase 2 only after remediation of R1-R3. Do not rename namespaces, packages, or repositories. Do not merge any PR. Do not start Phase 3 or Phase 4-L. Do not mark Phase 2 done in the plan. Do not edit the plan.

Write your verdict into this new file (create it; do not overwrite prior receipts):
1) docs/receipts/hv/20261007T160847Z-qbrain-ai-rebrand-phase-2-hv-r123.md

Also copy the same receipt to F:\GitHub\McpServer\docs\receipts\hv\20261007T160847Z-qbrain-ai-rebrand-phase-2-hv-r123.md when finished.

Prove model and effort from your own rollout turn_context: both payload.model / payload.effort and collaboration_mode.settings must show gpt-6-astra and xhigh. If they do not, OverallVerdict is DISAGREE.

## Remediation under review

Prior HV (20261007T150848Z) DISAGREE blocked on:
- R1: Node ReplBridge defaulted to mcpserver-repl only inside vendor/sharpninja-mcpserver-plugin-core.tgz. Expected prefer qbrain-ai-repl then mcpserver-repl (Phase 1 pattern).
- R2: Nine plugins changed lib/ensure-repl.ps1, lib/plugin-env.ps1, lib/repl-invoke.ps1 without refreshing CORE-MANIFEST.yaml hashes.
- R3: Cline tests expected @sharpninja/mcpserver-cline-plugin while package identity is @qbrainai/qbrain-ai-cline-plugin.

Operator claims these are fixed on the draft tips below. Re-score Phase 2 content readiness and specifically verify R1-R3 are closed with evidence from the tip trees under F:\GitHub (or gh). AGREE requires Accuracy >= 98 and Completeness >= 98. Drafts are not merges; even on AGREE, Phase 2 is not done until merges.

## Draft PRs / tips to score

- https://github.com/sharpninja/mcpserver-grok-plugin/pull/6 @ 6ed6d0b6
- https://github.com/sharpninja/mcpserver-grok-bot-plugin/pull/1 @ 289a28bc
- https://github.com/sharpninja/mcpserver-claude-code-plugin/pull/6 @ bf39281b
- https://github.com/sharpninja/mcpserver-claude-cowork-plugin/pull/5 @ b46de28b
- https://github.com/sharpninja/mcpserver-cline-plugin/pull/5 @ 7120af40
- https://github.com/sharpninja/mcpserver-cline-v2-plugin/pull/5 @ 111d9483
- https://github.com/sharpninja/mcpserver-codex-plugin/pull/5 @ 3490cc1d
- https://github.com/sharpninja/mcpserver-copilot-plugin/pull/5 @ b374c0a1
- https://github.com/sharpninja/mcpserver-opencode-plugin/pull/4 @ 2268a53c
- https://github.com/sharpninja/McpServerManager/pull/10 @ 242a8968
- https://github.com/sharpninja/McpServerTools/pull/1 @ 11ee9124

## Required receipt fields

Include OverallVerdict (AGREE/DISAGREE), Accuracy (0-100), Completeness (0-100), model proof, and explicit R1/R2/R3 PASS/FAIL with evidence. Use no em-dashes.