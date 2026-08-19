# Session Log Schema Reference

Load this file when you need to create, update, or query session logs.
For specific agent operational instructions, follow `AGENTS-README-FIRST.yaml`.

## Endpoints

- `POST /mcpserver/sessionlog` — create or update a session log
- `GET /mcpserver/sessionlog?limit=N&offset=M&planFile=&todoId=` — query recent session logs; optional exact `planFile` and `todoId` filters after the same normalize/expand rules as persist
- `POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/begin` — first persist of a turn; body `SessionLifecycleBeginRequest` requires `planFile` and `todoId` (`None` when none)
- `POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog` — stream reasoning dialog

## Required turn context (normative)

- First persist and replace of a turn require both `planFile` and `todoId`.
- Accepted `planFile`: workspace-relative path, exact absolute path, `~/` home-relative path, or the exact sentinel `None` (case-sensitive). `..` is rejected.
- Accepted `todoId`: canonical MCP TODO id (`PHASE-AREA-###` or `ISSUE-N`) or `None`. FR/TR/TEST ids are rejected.
- Additive updates that omit either field keep the stored value. Replace that omits either field is rejected, except the canceled/cancelled hook-supersede persist: when the incoming turn status is `canceled` or `cancelled` and either field is omitted or blank, the server stamps `None` then validates. That is the only first-persist omission path (FR-MCP-SESSIONLOGCTX-001 AC-003 plus STORE-006).
- Reads never return null for these fields. Import, ingest, and federation persist a validated pair (`None` if extraction finds nothing).

## Naming Conventions (Normative)

- `sessionId` must match `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`.
- `sessionId` regex: `^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`
- `sessionId` must start with the exact `sourceType`/`agent` prefix (case-sensitive).
- `requestId` must match `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`.
- `requestId` regex: `^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`
- Valid IDs:
  - `sessionId`: `Copilot-20260304T113901Z-namingconv`
  - `requestId`: `req-20260304T113901Z-plan-namingconventions-001`
- Invalid IDs:
  - `sessionId`: `copilot-20260304T113901Z-namingconv`, `Copilot-2026-03-04-namingconv`
  - `requestId`: `req-plan-namingconventions-001`, `request-20260304T113901Z-task-01`

## SessionLog (POST body)

```json
{
  "sourceType": "string — YOUR agent name (e.g. 'Copilot', 'Cline', 'Cursor')",
  "sessionId": "string — required format <Agent>-<yyyyMMddTHHmmssZ>-<suffix> (e.g. 'Copilot-20260304T113901Z-feature-audit')",
  "agentSessionId": "string|null — provider-native root session identifier from the host payload; empty when unknown (the MCP sessionId is never echoed here)",
  "agentSessionTranscriptFile": "string|null — provider-native transcript path; only set when the file exists on disk, empty otherwise (never synthesized)",
  "agentExecutablePath": "string|null — resolved executable path for the agent host",
  "agentExecutableVersion": "string|null — executable version resolved from the agent host, or 'unknown' when discovery fails (never the plugin version)",
  "title": "string — brief session summary, keep updated",
  "model": "string — AI model name (e.g. 'claude-sonnet-4-20250514')",
  "started": "string — ISO 8601 timestamp when session began",
  "lastUpdated": "string — ISO 8601 timestamp of latest activity",
  "status": "string — 'in_progress', 'completed', 'failed', 'canceled', or 'cancelled'",
  "tags": ["string array — session-scoped tags; persist and return on query (not silently dropped)"],
  "turns": [ "array of RequestTurn objects (see below)" ]
}
```

## RequestTurn (each element in `turns`)

```json
{
  "requestId": "string — required format req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>",
  "timestamp": "string — ISO 8601",
  "queryText": "string — full user query or task description",
  "queryTitle": "string — short summary of the query",
  "response": "string — your response text",
  "interpretation": "string — your understanding of what was asked",
  "status": "string — 'completed', 'in_progress', 'failed', 'canceled', or 'cancelled' (canceled/cancelled are first-class terminal statuses)",
  "model": "string — model used for this turn",
  "tokenCount": "integer|null — approximate token count",
  "tags": ["string array — e.g. 'refactor', 'bugfix', 'feature'"],
  "contextList": ["string array — files or resources referenced"],
  "designDecisions": ["string array — decisions made during this turn"],
  "requirementsDiscovered": ["string array — requirement IDs e.g. 'TR-MCP-001'"],
  "filesModified": ["string array - file paths changed. Paths that resolve outside the workspace root require a foreign: / foreign-repo: / cross-workspace: prefix or a turn tag foreign-repo / cross-workspace / foreign-workspace (FR-MCP-SESSIONATTR-001). Completeness audits can filter those prefixes and tags. Forward-only: historical turns are not rewritten."],
  "blockers": ["string array — issues preventing progress"],
  "actions": [ "array of Action objects (see below)" ],
  "processingDialog": [ "array of DialogItem objects (see below)" ],
  "planFile": "string — required on new persist and replace: current plan file path (workspace-relative, exact, or ~/...) or the exact sentinel None",
  "todoId": "string - required on new persist and replace: canonical MCP TODO id (PHASE-AREA-### or ISSUE-N) or the exact sentinel None"
}
```

## Foreign filesModified and commits (FR-MCP-SESSIONATTR-001)

A session-log turn for workspace W must not list `filesModified` or commit `filesChanged` paths that resolve outside W unless the turn explicitly marks them:

- Item prefix on the path: `foreign:`, `foreign-repo:`, or `cross-workspace:`
- Turn tags: `foreign-repo`, `cross-workspace`, or `foreign-workspace`

Unmarked foreign paths are rejected with `validation_error`. Relative paths are resolved against the workspace root (`..` that escapes is foreign). Empty workspace context (import/ingest) skips this check. Forward-only: existing rows are not rewritten. Completeness audits filter the prefixes and tags above.

Commit SHA/message without `filesChanged` cannot be proven foreign; mark the turn with `foreign-repo` when attributing another repository's commit.

## Action (each element in `actions`)

```json
{
  "order": "integer — sequence number",
  "description": "string — what was done",
  "type": "string — action type (see action-types.md)",
  "status": "string — 'completed', 'in_progress', or 'failed'",
  "filePath": "string — affected file path, or empty string"
}
```

## DialogItem (each element in `processingDialog`, or POST body to dialog endpoint)

```json
{
  "timestamp": "string — ISO 8601",
  "role": "string — 'model', 'tool', 'system', or 'user'",
  "content": "string — reasoning text, tool output, or observation",
  "category": "string — 'reasoning', 'tool_call', 'tool_result', 'observation', or 'decision'"
}
```

## McpSession Module — PowerShell Lifecycle

```powershell
# Query recent logs at session start
Get-McpSessionLog -Limit 5

# Create session
$s = New-McpSessionLog -SourceType "Copilot" -Title "Implementing feature X" -Model "claude-sonnet-4"

# Add turn for each user request
$t = Add-McpSessionTurn -Session $s -QueryTitle "Add auth" -QueryText "Add JWT authentication"

# Record actions during work
Add-McpAction -Turn $t -Description "Created TokenService" -Type create -FilePath "src/TokenService.cs"

# Stream reasoning dialog as you work
Send-McpDialog -Session $s -RequestId $t.requestId -Content "Analyzing the issue..." -Category reasoning

# Complete the turn
Set-McpSessionTurn -Turn $t -Session $s -Response "Done" -Status completed

# Before compaction, persist the current session state.
Update-McpSessionLog -Session $s

# After compaction, record the compaction outcome and push again.
Send-McpDialog -Session $s -RequestId $t.requestId -Content "Compaction completed; recovered context has been restored." -Category observation
Update-McpSessionLog -Session $s

# Final push at session end
Update-McpSessionLog -Session $s -Status completed
```

## McpSession Module — Bash Lifecycle

```bash
# Query recent logs at session start
mcp_session_query 5

# Create session
mcp_session_create "Copilot" "Implementing feature X" "claude-sonnet-4"

# Add turn, record actions, stream dialog, complete
mcp_session_add_turn "req-001" "Add auth" "Add JWT authentication" "in_progress"
mcp_session_add_action "req-001" "Created TokenService" "create" "src/TokenService.cs"
mcp_session_send_dialog "req-001" "Analyzing the issue..." "reasoning"
mcp_session_update_turn "req-001" "status" "completed"
mcp_session_complete
```
