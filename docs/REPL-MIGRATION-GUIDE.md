# Migrating from Direct API to REPL Host Workflows

This guide tells agents how to replace direct `McpServerClient` HTTP calls for session logging and TODO management with the REPL-backed workflow tools now available in `McpAgent`.

## Why Migrate

The hosted McpAgent now exposes **43 tools** through the AI function surface. The REPL-backed tools provide:

- **Requirements management** (FR/TR/TEST list and get) without raw HTTP calls
- **TODO create/delete** alongside existing query/get/update
- **Session log history** queries across agents
- **Generic client passthrough** for any sub-client method not covered by a dedicated tool

Using these tools instead of raw API calls ensures consistent identifier validation, canonical formatting, and proper audit trails.

## Agent Plugin Boundary

In workspaces whose marker declares `agent_plugins.policy: required`, hosted tools and direct `mcpserver-repl --agent-stdio` are not substitutes for the required per-agent plugin. Codex, Claude Code, GitHub Copilot, and Cline must use their matching plugin wrapper for normal session log, TODO, requirements, import/export, and traceability operations. Direct REPL use remains appropriate for plugin implementation and fallback diagnostics.

When direct `--agent-stdio` is used, send one YAML envelope per document and separate multiple documents with `---`. Do not send `type: batch`; unsupported batch envelopes are rejected with `unsupported_batch_envelope`.

## Tool Inventory

### Session Log (6 tools)

| Tool | Replaces | Description |
|------|----------|-------------|
| `mcp_session_bootstrap` | `POST /mcpserver/sessionlog` | Bootstrap a new session log |
| `mcp_session_update` | `POST /mcpserver/sessionlog` | Update session-level metadata |
| `mcp_session_turn_begin` | `POST /mcpserver/sessionlog` | Create a new turn |
| `mcp_session_turn_update` | `POST /mcpserver/sessionlog` | Update an existing turn |
| `mcp_session_turn_complete` | `POST /mcpserver/sessionlog` | Complete a turn |
| `mcp_session_query_history` | `GET /mcpserver/sessionlog` | **NEW** - Query session history |

### TODO (8 tools)

| Tool | Replaces | Description |
|------|----------|-------------|
| `mcp_todo_query` | `GET /mcpserver/todo` | Query TODO items with filters |
| `mcp_todo_get` | `GET /mcpserver/todo/{id}` | Get a single TODO by ID |
| `mcp_todo_update` | `PUT /mcpserver/todo/{id}` | Update a TODO item |
| `mcp_todo_create` | `POST /mcpserver/todo` | **NEW** - Create a TODO item |
| `mcp_todo_delete` | `DELETE /mcpserver/todo/{id}` | **NEW** - Delete a TODO item |
| `mcp_todo_plan` | `GET /mcpserver/todo/{id}/plan` | Get buffered plan text |
| `mcp_todo_status` | `GET /mcpserver/todo/{id}/status` | Get buffered status report |
| `mcp_todo_implementation` | `GET /mcpserver/todo/{id}/implementation` | Get implementation guide |

### Requirements (6 tools, all NEW)

| Tool | Description |
|------|-------------|
| `mcp_requirements_list_fr` | List functional requirements (optional area/status filter) |
| `mcp_requirements_list_tr` | List technical requirements (optional area/subarea/status filter) |
| `mcp_requirements_list_test` | List test requirements (optional area/status filter) |
| `mcp_requirements_get_fr` | Get a specific FR by ID (e.g. `FR-MCP-001`) |
| `mcp_requirements_get_tr` | Get a specific TR by ID (e.g. `TR-MCP-ARCH-001`) |
| `mcp_requirements_get_test` | Get a specific TEST by ID (e.g. `TEST-MCP-001`) |

### Repository (3 tools)

| Tool | Description |
|------|-------------|
| `mcp_repo_read` | Read file content by relative path |
| `mcp_repo_list` | List files/directories |
| `mcp_repo_write` | Write file content by relative path |

### Desktop and PowerShell (4 tools)

| Tool | Description |
|------|-------------|
| `mcp_desktop_launch` | Launch a local desktop process |
| `mcp_powershell_session_create` | Create a persistent PowerShell session |
| `mcp_powershell_session_command` | Run a command in a PowerShell session |
| `mcp_powershell_session_close` | Close a PowerShell session |

### Generic Passthrough (1 tool, NEW)

| Tool | Description |
|------|-------------|
| `mcp_client_invoke` | Dynamically invoke any McpServerClient sub-client method |

### Quad Brain (0 tools)

QuadBrain is not exposed to hosted agents or agent plugins. The former
`mcp_quadbrain_coding_execute` tool was removed under FR-MCP-142, along with the
`brain_slot_*` MCP tools, the shared plugin-core brain-slot descriptors, and the
`BRAINSLOTS` client-passthrough route. QuadBrain is reachable only by QBAgent, as the
OpenAI-compatible model at `POST /v1/chat/completions`.

### GraphRAG (14 tools)

| Tool | Description |
|------|-------------|
| `mcp_graphrag_ingest_text` | Ingest raw text into the workspace GraphRAG index |
| `mcp_graphrag_list_documents` | List indexed GraphRAG documents |
| `mcp_graphrag_get_document_chunks` | Get the chunks for an indexed document |
| `mcp_graphrag_delete_document` | Delete a document from the index |
| `mcp_graphrag_create_entity` | Create a graph entity |
| `mcp_graphrag_list_entities` | List graph entities |
| `mcp_graphrag_get_entity` | Get a graph entity by id |
| `mcp_graphrag_update_entity` | Update a graph entity |
| `mcp_graphrag_delete_entity` | Delete a graph entity |
| `mcp_graphrag_create_relationship` | Create a relationship between entities |
| `mcp_graphrag_list_relationships` | List graph relationships |
| `mcp_graphrag_get_relationship` | Get a relationship by id |
| `mcp_graphrag_update_relationship` | Update a relationship |
| `mcp_graphrag_delete_relationship` | Delete a relationship |

## Migration Patterns

### Before: Direct Session Log API Calls

```
# Old pattern - raw HTTP via PowerShell or curl
POST /mcpserver/sessionlog
{
  "sourceType": "Copilot",
  "sessionId": "Copilot-20260402T...",
  ...
}
```

### After: Use Session Log Tools

```
# Bootstrap
mcp_session_bootstrap({
  sessionId: null,  // auto-generated
  title: "Implement auth flow",
  model: "claude-opus-4-6",
  status: "in_progress"
})

# Begin turn
mcp_session_turn_begin({
  requestId: null,  // auto-generated
  queryTitle: "Add login endpoint",
  queryText: "Create POST /auth/login with JWT response"
})

# Complete turn
mcp_session_turn_complete({
  requestId: "req-20260402T120000Z-add-login",
  response: "Created LoginController with JWT token generation"
})

# Query history (NEW)
mcp_session_query_history({
  agent: "Copilot",
  limit: 5
})
```

### Before: Direct TODO API Calls

```
# Old pattern
GET /mcpserver/todo?keyword=auth&priority=high
POST /mcpserver/todo  { id: "PLAN-AUTH-001", ... }
DELETE /mcpserver/todo/PLAN-AUTH-001
```

### After: Use TODO Tools

```
# Query
mcp_todo_query({ keyword: "auth", priority: "high" })

# Create (NEW)
mcp_todo_create({
  id: "PLAN-AUTH-001",
  title: "Implement OAuth2 device flow",
  section: "Authentication",
  priority: "high",
  estimate: "4h"
})

# Delete (NEW)
mcp_todo_delete({ id: "PLAN-AUTH-001" })
```

### Before: Raw Requirements API Calls

```
# Old pattern
GET /mcpserver/requirements/fr
GET /mcpserver/requirements/tr/TR-MCP-ARCH-001
```

### After: Use Requirements Tools

```
# List FRs filtered by area
mcp_requirements_list_fr({ area: "MCP" })

# Get specific TR
mcp_requirements_get_tr({ id: "TR-MCP-ARCH-001" })

# Get all test requirements
mcp_requirements_list_test({})
```

### Generic Passthrough for Uncovered Operations

For any McpServerClient sub-client method not covered by a dedicated tool:

```
# Search workspace context
mcp_client_invoke({
  clientName: "context",
  methodName: "SearchAsync",
  arguments: { query: "authentication flow", limit: 10 }
})

# List GitHub issues
mcp_client_invoke({
  clientName: "github",
  methodName: "ListIssuesAsync",
  arguments: { state: "open" }
})

# Check workspace health
mcp_client_invoke({
  clientName: "health",
  methodName: "CheckAsync",
  arguments: {}
})
```

## Identifier Rules (Unchanged)

These canonical formats are enforced by both the old API and the new tools:

- **Session ID**: `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>` (e.g. `Copilot-20260402T120000Z-authflow`)
- **Request ID**: `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>` (e.g. `req-20260402T120000Z-add-login-001`)
- **TODO ID**: uppercase kebab-case ending in `-###` or `ISSUE-{number}` (e.g. `PLAN-AUTH-001`, `MCP-TODO-CREATE-001`, `ISSUE-42`)
- **FR ID**: `FR-<AREA>-###` (e.g. `FR-MCP-001`)
- **TR ID**: `TR-<AREA>-<SUBAREA>-###` (e.g. `TR-MCP-ARCH-001`)
- **TEST ID**: `TEST-<AREA>-###` (e.g. `TEST-MCP-001`)

When `sessionId` or `requestId` is passed as `null`, the tool auto-generates a canonical ID.

## Summary of Changes for Agent Authors

1. **Stop making raw HTTP calls** to `/mcpserver/sessionlog`, `/mcpserver/todo`, and `/mcpserver/requirements`. Use the named tools instead.
2. **Use `mcp_todo_create` and `mcp_todo_delete`** for full TODO lifecycle instead of raw POST/DELETE.
3. **Use `mcp_requirements_list_*` and `mcp_requirements_get_*`** for requirements queries instead of raw GET.
4. **Use `mcp_session_query_history`** to review past sessions instead of raw query endpoints.
5. **Use `mcp_client_invoke`** as an escape hatch for any sub-client method not covered by dedicated tools (context search, GitHub, workspace management, voice, tunnels, etc.).
6. **PowerShell helper modules** (`McpContext.psm1`) are still valid for interactive shell workflows but agents running inside McpAgent should prefer the tool surface.
