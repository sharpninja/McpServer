# Frenemy capability probe

Requested tool: mcp__claude__ask_claude. Requested model: opus. Requested effort: high.
This receipt captures the returned tool text, not the reviewer's full event stream. The tool response contained only content and isError keys. No current-invocation event-stream path was returned.
Parent correction: the earlier 20261110T103309 packet receipt was created by our PowerShell CLI launcher, not frenemy. It provides no evidence of frenemy's stream capture. Reviewer-reported low effort conflicts with requested high and is not independently verified.

## Exact submitted prompt

Capability probe explicitly requested by the operator: determine whether this frenemy ask_claude MCP surface can support their HV workflow. This is NOT a federation plan review; do not edit plans, product files, requirements, TODOs, or start other agents. Workspace /home/sharpninja/github/McpServer.
Test these unknowns with actual tools available to YOU, not assumptions:
1. Full event-stream receipts: determine whether this invocation exposes or durably records the complete reviewer event stream (start/tool calls/tool results/messages/completion) as JSONL, and supports meaningful ongoing progress entries. Emit a progress message now and another before completion. If a receipt path is available, inspect it read-only and report its location, event types/counts and evidence that it belongs to this invocation. Distinguish final-answer-only capture from complete event stream. Do not guess hidden server behavior.
2. Profile and live-memory loading: read /home/sharpninja/.agents/skills/add-profile/SKILL.md fully; follow its read-only profile and live Global/active Workspace memory load if tools permit. Exclude MEMORY-PROCESS-007 before rendering/loading. Verify your own required Claude plugin identity, workspace trust and health through supported interfaces if available. Never use Codex identity, raw HTTP, shell workaround, or pretend cached profile equals live memories. Report exact loaded full-text counts, pagination completeness, and concrete tool/error evidence. Do not return private profile/memory contents.
3. Reviewer-owned MCP audit turn: the user authorizes the minimal audit writes necessary for THIS probe. If your available supported Claude plugin/MCP tools permit it, create a dedicated Claude-owned probe turn, record actual probe findings, complete it and verify persistence using a history query. Report real agent/sourceType, sessionId, requestId, and readback result. If your surface forbids writes or no suitable tools exist, report UNAVAILABLE without bypassing the restriction.
Also report actual model and effort if observable, and relevant tool names actually exposed to you. Do not print marker/API keys, tokens, credentials, environment values, or unrelated private data. Do not use ask_claude_write or broader permissions.
Return PASS/FAIL/UNAVAILABLE/UNKNOWN for each capability, precise evidence, and the smallest missing capability for compliant HV. Do not mark a capability PASS just because instructions say it should work. End with machine-readable JSON summary. No review acceptance claim is requested.

## Returned reviewer text

**Progress 2/2:** The tool checks are done and I'm writing up the results. Short version: none of the three capabilities works from this surface.

`★ Insight ─────────────────────────────────────`
- The older receipt in `docs/receipts/hv/` matches Claude CLI `--output-format stream-json` output. It has `system/init`, many `system/thinking_tokens` events, `assistant` messages, a `rate_limit_event` and one `result`. That means the frenemy wrapper captures the CLI's stdout stream, which is more than just the final answer.
- That stream doesn't give a JSONL receipt back to the model that is running. A reviewer can't see or check its own receipt from inside the call.
- In this session, MCP tools would appear as `mcp__<server>__<tool>` entries. A ToolSearch for "mcp" found nothing, so no McpServer plugin tools are attached to this invocation.
`─────────────────────────────────────────────────`

## Results

### 1. Full event-stream receipts: **UNKNOWN** for this invocation (no self-receipt is available)
- **No receipt path for this run.** None of my tools returns a receipt path or ID. The only receipts in the workspace are dated `20261110T103309-federation-plan-packet-r1.*` (request/response JSONL, stderr.log, verdict.md). They belong to the earlier federation plan run, not this probe. Nothing in them mentions this probe, and a file for this run would only be written after I finish.
- **What the earlier receipt shows:** `response.jsonl` has 263 lines:
  - line 1: `system/init`
  - lines 2–259: `system/thinking_tokens`
  - lines 260–261: `assistant`
  - line 262: `rate_limit_event`
  - line 263: one `result`
  - no lines starting with `user` or `tool_result`, so that run either made no tool calls or didn't record them.
- So the wrapper captures more than the final answer. I can't tell from this one file whether it records the full start/tool-call/tool-result/message/completion sequence.
- **Progress entries:** I sent the two progress messages as plain assistant text. Whether they land in a receipt as separate events depends on the server, and I can't observe that.

### 2. Profile and live-memory loading: **FAIL** (blocked by permissions)
- `Read /home/sharpninja/.agents/skills/add-profile/SKILL.md` was refused: *"Claude requested permissions to read from …/SKILL.md, but you haven't granted it yet."* I didn't retry or work around it.
- No MCP memory, profile, workspace-trust or health tools are exposed (ToolSearch for "mcp" and for "memory session turn history" found no matching tools).
- Loaded counts: 0 full-text memories, pagination not attempted. I couldn't apply the MEMORY-PROCESS-007 exclusion because nothing was loaded. I couldn't verify plugin identity, workspace trust or health.

### 3. Reviewer-owned MCP audit turn: **UNAVAILABLE**
- No turn, session or history tools are available, and I have no Write, Edit or Bash. No turn was created, so there is no agent/sourceType, sessionId, requestId or readback to report.

### Model, effort and tools
- **Model:** `claude-opus-5-5` (Opus 5.5), as stated in my system context.
- **Effort:** a reasoning-effort setting of 15 (low) is visible in my context.
- **Tools available directly:** Agent, Glob, Grep, ListAgents, Read, ReportFindings, ScheduleWakeup, Skill, ToolSearch.
- **Tools that load on request:** CronCreate, CronDelete, CronList, DesignSync, EnterWorktree, ExitWorktree, PushNotification, RemoteTrigger, SendMessage, TaskStop.
- There are no `mcp__*` tools, no Bash and no Write. I didn't use agents or SendMessage.

### Smallest missing capability for compliant HV
1. Attach the McpServer Claude plugin's MCP tools (profile/memory read, turn create/complete, history query) to the `ask_claude` invocation.
2. Grant Read access to `~/.agents/skills/add-profile/`.
3. Return the receipt path and an invocation ID to the reviewer.

The first is the main blocker. The last is needed to prove a receipt belongs to a specific run.

```json
{
  "probe": "frenemy-ask_claude-hv-capability",
  "date": "2026-11-10",
  "model": "claude-opus-5-5",
  "effort": "reasoning_effort=15 (low), observed in context",
  "tools_exposed": ["Agent","Glob","Grep","ListAgents","Read","ReportFindings","ScheduleWakeup","Skill","ToolSearch"],
  "deferred_tools": ["CronCreate","CronDelete","CronList","DesignSync","EnterWorktree","ExitWorktree","PushNotification","RemoteTrigger","SendMessage","TaskStop"],
  "mcp_tools_exposed": [],
  "capabilities": {
    "full_event_stream_receipts": {
      "status": "UNKNOWN",
      "self_receipt_path": null,
      "progress_messages_emitted": 2,
      "prior_receipt_observed": {
        "path": "docs/receipts/hv/20261110T103309-federation-plan-packet-r1.response.jsonl",
        "belongs_to_this_invocation": false,
        "lines": 263,
        "events": {"system/init": 1, "system/thinking_tokens": 258, "assistant": 2, "rate_limit_event": 1, "result": 1, "user_or_tool_result": 0},
        "classification": "stream-json stdout capture (more than final answer); completeness for tool-call events unproven"
      }
    },
    "profile_live_memory_load": {
      "status": "FAIL",
      "evidence": "Read of ~/.agents/skills/add-profile/SKILL.md refused: permission not granted; no MCP memory/profile/health tools exposed",
      "memories_loaded_full_text": 0,
      "pagination_complete": false,
      "memory_process_007_excluded": "not_applicable_nothing_loaded",
      "plugin_identity_verified": false
    },
    "reviewer_owned_mcp_audit_turn": {
      "status": "UNAVAILABLE",
      "evidence": "no MCP turn/session/history tools; no Write/Bash",
      "agent": null, "sourceType": null, "sessionId": null, "requestId": null, "readback": null
    }
  },
  "smallest_missing_capability": [
    "attach McpServer Claude plugin MCP tools (profile/memory read, turn create/complete, history query) to ask_claude invocation",
    "grant Read on ~/.agents/skills/add-profile/",
    "expose per-invocation receipt path and invocation id to reviewer"
  ],
  "writes_performed": [],
  "review_acceptance_claimed": false
}
```
