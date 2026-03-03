# Module Bootstrap Reference

Load this file when setting up helper modules at session start.

## Overview

Helper modules handle workspace routing (`X-Workspace-Path` header) automatically. Raw `Invoke-RestMethod` / `curl` calls to `/mcpserver/sessionlog` and `/mcpserver/todo` endpoints will target the wrong workspace. Use modules instead.

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
Initialize-McpSession          # reads marker file, verifies server health
Initialize-McpTodo             # reads marker file, verifies server health
```

## Bash Bootstrap

```bash
source ./mcp-session.sh && mcp_session_init
source ./mcp-todo.sh   && mcp_todo_init
```

## Error Recovery

If module initialization or session log push fails (e.g., 401), re-read the `AGENTS-README-FIRST.yaml` marker file for a fresh API key, re-initialize the modules, and retry. The API key rotates on each server restart.

If module download fails, retry with exponential backoff.
