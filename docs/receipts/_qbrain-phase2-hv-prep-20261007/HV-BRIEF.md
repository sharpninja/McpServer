# Phase 2 HV brief (pre-merge draft review)

Plan checkpoint (origin/develop docs/Project/QBrain-AI-Rebrand-Implementation-Plan-2026-10-05.md):
- Model lock in plan: gpt-6-astra, effort xhigh (astra-6-xhigh)
- Standing rule: prefer Grok Bot for HV; do not start Astra unless operator overrides
- Claims to score:
  1. one PR per in-scope sibling that exists (NOTE: still DRAFT, not merged; score content readiness, flag merge-not-done)
  2. PRs used old GitHub names (mcpserver-*-plugin, McpServerManager, McpServerTools)
  3. McpServerTools still contains old mcpserver-*-plugin manifest names
  4. QuadBrain and qbagent were not renamed
  5. missing mcpserver-grok-bot-plugin remote recorded rather than invented (remote EXISTS; PR#1 tip 8fad06fa)
- Done rule: OverallVerdict AGREE required for Phase 2 done; drafts mean phase cannot be done yet even on AGREE-with-caveats

Draft PRs / tips (all OPEN draft, verified 2026-10-07 CT):
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

Legion MCP hostile-review path blocked:
POST /mcpserver/hostile-review/query -> 500 SqlException Invalid object name HostileReviewRequests

Dispatch: unbounded Grok Bot HV against the PR URLs above; write receipt under docs/receipts/ (new file only); do not merge; do not start Phase 3/4-L.
