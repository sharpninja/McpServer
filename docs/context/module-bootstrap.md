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
