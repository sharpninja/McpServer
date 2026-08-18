# Hostile Validator Receipt

TimestampUtc: 2026-08-17T23:28:18Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
add-profile: executed yes; profile file count read: 18
WorkClass: class-2 user-directed general action (live Agent Help config plus official docs lookup)
ActivePlan: none claimed; none evaluated
RequirementIDs: FR-MCP-HELP-011 exists (grok-cli default). Surface C scored N/A.
ReviewSessionId: GrokCode-20260817T232439Z-hostile-model-slug
ReviewRequestId: req-20260817T232439Z-001-hostile-validate-model-slug
ServerTurnId: 41529
OverallVerdict: AGREE

## add-profile

Executed first, before claim checks. Read every non-skill `*.md` under `C:\Users\kingd\.claude\profile\` in full (18 files). Excluded skill port `add-profile.grok.md`.

## Classification

Class 2. Operator-directed live service configuration and official-docs validation. Implementer did not claim a product plan step done and did not ship product implementation in this turn. Surface C is N/A. Surface D is N/A. Byrd v4 is not applied to the ops action.

## Session-log persistence proof (required reviewer process)

Native MCP Streamable HTTP tools at `http://PAYTON-LEGION2:7147/mcp-transport` (not raw `/mcpserver/sessionlog` REST):

- `initialize` HTTP 200, protocolVersion 2025-03-26, serverInfo McpServer.Support.Mcp 1.4.26.0, Mcp-Session-Id `kY68tPNYCoBaskqJYP_YhQ`
- `sessionlog_open`: success=true, created=true, sessionId=`GrokCode-20260817T232439Z-hostile-model-slug`
- `sessionlog_begin_turn`: success=true, turnId=41529, status=in_progress, requestId=`req-20260817T232439Z-001-hostile-validate-model-slug`, planFile=None, todoId=None
- `sessionlog_dialog`: success=true, totalDialogItems=2
- `sessionlog_complete_turn`: success=true, turnId=41529, status=completed
- `sessionlog_query` with `text` equal to the exact sessionId: totalCount=0 (text filter does not match the id string)
- `sessionlog_query` agent=GrokCode, from=2026-08-17T23:20:00Z: totalCount=1, sessionId=`GrokCode-20260817T232439Z-hostile-model-slug`, turn requestId=`req-20260817T232439Z-001-hostile-validate-model-slug`, status=completed, queryTitle=`Hostile validate Grok model slug and live HelperModel`, 4 actions (orders 1-4 including design_decision), 2 processingDialog items (observation + decision)

Persistence is proved by the from-date `sessionlog_query` result, not by the exact-id text filter.

## Explicit FAIL list

None.

## Explicit UNKNOWN list

None on applicable surfaces. Notes that are not FAILs:

- `agent_help_get_status` for the implementer's session does not return `modelRequested` / `modelResolved` (status DTO has no those fields). Those fields were independently reproduced by a new `agent_help_create_session` with no `agentModel` override.
- Workspace working tree is dirty with unrelated handoff/product files. Those files were not written in the implementer's 23:15Z window.

## A. Requested validation

### A1. Class 2 ops; no product code / plan-step done claim

Verdict: PASS

Evidence:

- Implementer receipt work class is live config plus docs lookup.
- Repo `appsettings.yaml` LastWriteTimeUtc=2026-07-11T15:32:14.5183453Z. Not in `git status --porcelain`.
- `AgentHelpOptions.cs` LastWriteTimeUtc=2026-07-12T06:56:52.6706727Z.
- `FwhMcpTools.AgentHelp.cs` LastWriteTimeUtc=2026-08-17T00:54:14.8651150Z (hours before this ops turn).
- Live mutation target was `C:\ProgramData\McpServer\appsettings.yaml` (LastWriteTimeUtc=2026-08-17T23:15:04.3549203Z).
- No plan-step done claim. No MCP TODO `CompletedDate` on 2026-08-17 (DONE_COMPLETED_20260817=0 via `todo_list` done=true).
- `docs/Project/TODO.yaml` LastWriteTimeUtc=2026-07-10T00:56:30.7156679Z; git porcelain empty for that path.

### A2. Official Grok 4.5 slug is grok-4.5; aliases grok-4.5-latest, grok-build-latest

Verdict: PASS

Evidence from live fetch of https://docs.x.ai/developers/models/grok-4.5:

- Model name: `grok-4.5`
- Aliases: `grok-4.5-latest`, `grok-build-latest`
- Pattern search found those alias lines; no `grok-4.5-high` on that page.

### A3. grok-4.5-high is not a listed model slug in models.md or grok models

Verdict: PASS

Evidence:

- https://docs.x.ai/developers/models.md catalog lists `grok-4.6`, `grok-4.5`, `grok-4.3`, `grok-4.20-0309-reasoning`, `grok-4.20-0309-non-reasoning`, `grok-build-0.1`, `grok-4.20-multi-agent-0309`, plus Imagine/Voice ids.
- Pattern search `grok-4\.5-high` on that page: no matches.
- Local `grok models` exit 0: available models are `grok-4.6` (default) and `grok-4.5` only.

### A4. high is reasoning_effort (default high), not a model suffix

Verdict: PASS

Evidence from https://docs.x.ai/developers/model-capabilities/text/reasoning:

- `grok-4.6` and `grok-4.5` support `reasoning_effort`.
- If not specified, default is `"high"`.
- `grok-4.5` effort levels: low / medium / high (default). `xhigh` is documented for `grok-4.6` and later; on `grok-4.5`, `xhigh` is treated as `high`.
- `high` is a parameter value, not a model id suffix.

### A5. Local `grok models` lists only grok-4.6 (default) and grok-4.5

Verdict: PASS

Command: `grok models` (pwsh.exe, exit 0)

Output:

- Default model: `grok-4.6`
- Available models: `grok-4.6` (default), `grok-4.5`

### A6. Live ProgramData appsettings AgentHelp.DefaultExecutionStrategy=grok-cli and HelperModel=grok-4.5

Verdict: PASS

Evidence:

- File exists. LastWriteTimeUtc=2026-08-17T23:15:04.3549203Z.
- Raw YAML at file end (only AgentHelp section):
  - DefaultExecutionStrategy: grok-cli
  - HelperModel: grok-4.5
- Object-first parse via `Read-McpYamlObject`: AgentHelp key count=2, values grok-cli and grok-4.5.
- LIVE_HAS_GROK45HIGH=False. LIVE_HAS_HELPER_AUTO=False.
- Service uses `IOptionsMonitor<AgentHelpOptions>`, so the live YAML can apply without a restart.

### A7. Live create-session help-20260817231518-ef612ec964cc4be998bf30ce1c8b9f0f returned grok-cli and grok-4.5

Verdict: PASS

Evidence:

- MCP `agent_help_get_status` for `help-20260817231518-ef612ec964cc4be998bf30ce1c8b9f0f`: session exists, status=idle, createdUtc=2026-08-17T23:15:18.2155333+00:00, executionStrategy=grok-cli, topic=config-verify-grok-4.5, turnCounter=0. Status DTO has no model fields.
- Independent MCP `agent_help_create_session` with no agentModel and no executionStrategy override created `help-20260817232444-d85dbaa7c8964e2d8f9bc6d93029c40d` with executionStrategy=grok-cli, modelRequested=grok-4.5, modelResolved=grok-4.5.
- Claimed session timestamp is 14 seconds after the live YAML write. CreateSession reads `_options.CurrentValue.HelperModel` when AgentModel is omitted.

### A8. Repo appsettings.yaml still one-shot-cli; no TODO/plan marked done

Verdict: PASS

Evidence:

- Repo AgentHelp.DefaultExecutionStrategy=one-shot-cli. No HelperModel key (code default remains gpt-5.3-codex in AgentHelpOptions).
- `git status --porcelain -- appsettings.yaml` empty. Last commit touching it: fe3049bc 2026-07-09.
- MCP `todo_list` done=true: 207 items. DONE_COMPLETED_20260817=0.
- Related Agent Help TODOs that are already done have older completion summaries (2026-07-09 / 2026-07-11 era), not this ops turn.
- Open related items remain open: BUG-TRIAGE-149, BUG-TRIAGE-150, MCP-PLUGINCORE-004.
- `docs/Project/TODO.yaml` not git-dirty.

## B. Workspace rules

### B1. Byrd v4 phase-order

Verdict: PASS (N/A to class-2 ops)

No product implementation slice was claimed complete. Not scored by FR-vs-file timestamps.

### B2. Always bring the receipts

Verdict: PASS

Implementer receipt exists at `docs/receipts/agenthelp-model-slug-20260817T231518Z.md`. This review re-ran official docs, `grok models`, live YAML object parse, MCP Agent Help create/status, MCP todo_list, and git status. Claims match the re-verified artifacts.

### B3. MCP-only storage

Verdict: PASS

No TODO.yaml or session-log file edits by this implementer turn. TODO.yaml mtime is 2026-07-10 and git-clean. Reviewer used MCP `todo_list` / `requirements_list` / sessionlog_* tools only.

### B4. PowerShell-only / no Python

Verdict: PASS

Implementer mutation script `docs/receipts/_set-agenthelp-model-20260817T231236Z.ps1` uses `Update-McpYamlObject` (object-first). Reviewer used pwsh.exe only. No python/py invocations.

### B5. Honesty / look-before-delete

Verdict: PASS

Live AgentHelp remains a two-key map (same shape as the prior grok-cli ops turn that set HelperModel=auto). Mutation set keys on the existing map; it did not delete unrelated top-level sections. Claims about not writing `grok-4.5-high` and not editing repo appsettings match disk.

## C. Requirement violations

Verdict: N/A

Not project-implementation completion. FR-MCP-HELP-011 exists (`Agent Help grok-cli execution strategy and default`) and is consistent with live DefaultExecutionStrategy=grok-cli. Missing FR/TR for this live HelperModel ops change is not a FAIL.

## D. Current plan holistically

Verdict: N/A

Implementer claimed no plan-step completion. No active plan path was asserted. Not scored against an unrelated product DoD.

## Live health (review process)

GET `/health?nonce=c9cfcd964392450abaf1a1bdd819f18c`: status=Healthy, version=1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e, nonce echoed exactly. Service was not restarted.

## Design decisions (this review)

- Classified the request as class-2 ops. Consequence: surface C and Byrd v4 cannot FAIL this turn.
- Treated native `/mcp-transport` tools/call as the required MCP tool path (stateless; does not clobber the shared Grok plugin cache used by a concurrent hostile-effort review).
- Used an independent Agent Help create-session (no model override) as the proof of HelperModel resolution because get_status does not return model fields.

## Overall

All applicable A and B claims PASS. C and D are N/A. OverallVerdict=AGREE.
