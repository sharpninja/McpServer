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

All REPL requests use this structure:

```yaml
command: <command-name>
params:
  <param1>: <value1>
  <param2>: <value2>
---
```

### Session Log Operations

```yaml
# Initialize session (returns session slug)
command: session.init
params:
  agent: Copilotcli
  model: gpt-5.3-codex
---

# Create new session log
command: session.new
params:
  sessionId: Copilotcli-20260304T113901Z-example
  agent: Copilotcli
  model: gpt-5.3-codex
  purpose: Implement new feature
---

# Add a turn
command: session.turn.add
params:
  sessionId: Copilotcli-20260304T113901Z-example
  requestId: req-20260304T113901Z-001
  interpretation: User requested feature X
  response: Implementing feature X
  status: in_progress
---

# Update a turn
command: session.turn.update
params:
  sessionId: Copilotcli-20260304T113901Z-example
  requestId: req-20260304T113901Z-001
  response: Feature X implemented successfully
  status: complete
  actions:
    - type: file_create
      status: success
      filePath: src/feature-x.ts
---

# Add action to turn
command: session.action.add
params:
  sessionId: Copilotcli-20260304T113901Z-example
  requestId: req-20260304T113901Z-001
  action:
    type: file_edit
    status: success
    filePath: src/main.ts
---

# Save session log
command: session.save
params:
  sessionId: Copilotcli-20260304T113901Z-example
---
```

### TODO Operations

```yaml
# List all TODOs
command: todo.list
---

# Get a TODO by ID
command: todo.get
params:
  id: MVP-MCP-001
---

# Create a TODO
command: todo.create
params:
  id: MVP-MCP-042
  title: Implement feature Y
  section: Development
  priority: high
  description: Feature Y implementation
  implementationTasks:
    - description: Task 1
      done: false
    - description: Task 2
      done: false
---

# Update a TODO
command: todo.update
params:
  id: MVP-MCP-042
  done: true
  implementationTasks:
    - description: Task 1
      done: true
    - description: Task 2
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

```yaml
success: true
result:
  <command-specific-data>
---
```

or on error:

```yaml
success: false
error:
  message: "Error description"
  code: "ERROR_CODE"
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
