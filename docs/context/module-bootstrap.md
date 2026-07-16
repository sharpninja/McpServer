# Module Bootstrap Reference

Load this file when setting up helper modules at session start.

## Overview

**Preferred: `mcpserver-repl` (YAML-over-STDIO)** — The `mcpserver-repl` CLI tool is the recommended agent entrypoint. It eliminates shell quoting failures by using a YAML-over-STDIO protocol: agents send structured YAML requests on stdin and receive YAML responses on stdout. No escaping, no quoting, no PowerShell/bash syntax conflicts.

**Supported: PowerShell and Bash helper modules** — `McpSession.psm1` and `McpTodo.psm1` remain supported until feature parity is validated. Use these modules when you need direct PowerShell integration or when REPL usage is not practical.

Helper modules handle workspace routing (`X-Workspace-Path` header) automatically. Raw `Invoke-RestMethod` / `curl` calls to `/mcpserver/sessionlog` and `/mcpserver/todo` endpoints will target the wrong workspace. Use REPL or modules instead.

## REPL Bootstrap (Preferred)

The REPL accepts YAML documents on stdin and emits YAML responses on stdout. Each request is a single YAML document terminated by `---`.

### Starting the REPL

```powershell
# Launch the REPL in the workspace directory
mcpserver-repl --workspace "C:\projects\MyWorkspace"

# Or use the default workspace if registered as primary
mcpserver-repl
```

### REPL Request Format

All REPL requests use a typed envelope. The request body is `type: request` with a `payload` that carries a unique `requestId`, the namespaced `method`, and its `params`:

```yaml
type: request
payload:
  requestId: <unique-request-id>
  method: workflow.sessionlog.* | workflow.todo.* | client.<Client>.<Method>
  params:
    <param1>: <value1>
    <param2>: <value2>
---
```

Methods are namespaced: `workflow.sessionlog.*` and `workflow.todo.*` are plugin-local workflow verbs that update the cache and `current-turn.yaml` and persist through the real client; `client.<Client>.<Method>` is a passthrough to any typed sub-client method.

### Session Log Operations

```yaml
# Bootstrap the session runtime (marker + trust verification)
type: request
payload:
  requestId: req-20260304T113901Z-001
  method: workflow.sessionlog.bootstrap
  params: {}
---

# Open a session (persists sessionId to session-state.yaml)
type: request
payload:
  requestId: req-20260304T113901Z-002
  method: workflow.sessionlog.openSession
  params:
    sessionId: Copilot-20260304T113901Z-example
    title: Implement new feature
    agent: Copilot
    sourceType: Copilot
    model: gpt-5.3-codex
---

# Begin a turn
type: request
payload:
  requestId: req-20260304T113901Z-003
  method: workflow.sessionlog.beginTurn
  params:
    requestId: req-20260304T113901Z-003
    queryTitle: Implement feature X
    queryText: User requested feature X
---

# Update a turn (response, interpretation, tags, contextList)
type: request
payload:
  requestId: req-20260304T113901Z-003
  method: workflow.sessionlog.updateTurn
  params:
    response: Implementing feature X
    interpretation: User requested feature X
---

# Append actions to a turn (canonical type/status values)
type: request
payload:
  requestId: req-20260304T113901Z-003
  method: workflow.sessionlog.appendActions
  params:
    actions:
      - description: Create the feature module
        type: create
        status: completed
        filePath: src/feature-x.ts
      - description: Wire the feature into main
        type: edit
        status: completed
        filePath: src/main.ts
---

# Complete the turn
type: request
payload:
  requestId: req-20260304T113901Z-003
  method: workflow.sessionlog.completeTurn
  params:
    response: Feature X implemented successfully
---
```

### TODO Operations

```yaml
# Query TODOs (optional filters)
type: request
payload:
  requestId: req-20260304T113901Z-010
  method: workflow.todo.query
  params: {}
---

# Get a TODO by ID
type: request
payload:
  requestId: req-20260304T113901Z-011
  method: workflow.todo.get
  params:
    id: MVP-MCP-001
---

# Create a TODO
type: request
payload:
  requestId: req-20260304T113901Z-012
  method: workflow.todo.create
  params:
    id: MVP-MCP-042
    title: Implement feature Y
    section: Development
    priority: high
    description: Feature Y implementation
---

# Update a TODO
type: request
payload:
  requestId: req-20260304T113901Z-013
  method: workflow.todo.update
  params:
    id: MVP-MCP-042
    done: true
---
```

### REPL Benefits

- **No Shell Quoting Issues**: YAML values can contain quotes, newlines, JSON, and other special characters without escaping.
- **Structured Data**: Complex nested structures (arrays, objects) are native to YAML.
- **Idempotent**: REPL state is isolated; no global shell variables or module imports to manage.
- **Cross-Platform**: Works identically on Windows (PowerShell), Linux (bash), and macOS.
- **Error Handling**: YAML responses include structured error information.

### REPL Response Format

A successful response is `type: result` with the matching `requestId` and a command-specific `result`:

```yaml
type: result
payload:
  requestId: <matching-request-id>
  result:
    <command-specific-data>
---
```

On error the response is `type: error` with a structured code, message, and details:

```yaml
type: error
payload:
  requestId: <matching-request-id>
  code: ERROR_CODE
  message: Error description
  details: {}
---
```

## PowerShell Bootstrap

```powershell
# 1. Discover and download modules from the Tool Registry
$headers = @{ "X-Api-Key" = "<apiKey from AGENTS-README-FIRST.yaml>" }
Invoke-RestMethod -Uri "http://localhost:7147/mcpserver/tools/search?keyword=session" -Headers $headers
Invoke-RestMethod -Uri "http://localhost:7147/mcpserver/tools/search?keyword=todo" -Headers $headers
# Save the downloaded files as McpSession.psm1 and McpTodo.psm1

# 2. Import and initialize
Import-Module ./McpSession.psm1
Import-Module ./McpTodo.psm1
$sessionSlug = Initialize-McpSession -Agent "Copilotcli" -Model "gpt-5.3-codex"  # returns only the reusable session-slug string; does not create a session object
Initialize-McpTodo             # reads the marker file, verifies the marker signature, and performs the /health nonce handshake
```

`Initialize-McpSession` configures module-scoped connection state, verifies the marker
signature when a marker file is used, performs the `/health` nonce handshake, and persists or
reuses the session-slug metadata in local state files. It does not create a session-log
record and it does not return a session object. Call
`New-McpSessionLog` when you need the actual session object that `Add-McpSessionTurn`,
`Set-McpSessionTurn`, and `Update-McpSessionLog` operate on explicitly.

The marker signature is self-verifiable: bootstrap modules recompute the HMAC-SHA256 marker
signature by using the workspace API key already present in `AGENTS-README-FIRST.yaml` as the
verifier. After signature verification succeeds, the module calls `/health?nonce=<random>`
and requires the response to echo that exact nonce before any MCP endpoint is trusted.

## Bash Bootstrap

```bash
source ./mcp-session.sh && mcp_session_init
source ./mcp-todo.sh   && mcp_todo_init
```

## Error Recovery

If signature verification, the `/health` request, or nonce verification fails, the helper
modules emit `MCP_UNTRUSTED`, clear their MCP connection state, and stop before probing any
additional MCP endpoints.

If module initialization or session log push fails after a previously trusted bootstrap
(e.g., 401), re-read the `AGENTS-README-FIRST.yaml` marker file for a fresh API key,
re-initialize the modules, and retry. The API key rotates on each server restart.

If module download fails, retry with exponential backoff.

## Comparison: REPL vs. PowerShell Module

| Feature | REPL | PowerShell Module |
|---------|------|-------------------|
| Shell quoting issues | None | Frequent (JSON in strings) |
| Cross-platform | Identical on all OS | PowerShell only |
| Setup complexity | Launch one process | Import + Initialize |
| State management | Self-contained | Module-scoped globals |
| Nested data | Native YAML | Hashtable/PSObject |
| Preferred for agents | Yes | No (legacy) |

`McpSession.psm1` and `McpTodo.psm1` remain supported until parity is validated. New agent workflows should prefer `mcpserver-repl`.
