# MCP REPL User Guide

## Overview

The MCP REPL (Read-Eval-Print Loop) is a command-line tool for interacting with the Model Context Protocol server. It provides both interactive mode for human users and agent STDIO mode for programmatic integration with AI agents.

## Installation

### Install as .NET Global Tool

```powershell
# From solution root, pack the tool
.\scripts\Pack-ReplTool.ps1

# Install globally
.\scripts\Install-ReplTool.ps1

# Or install manually
dotnet tool install --global SharpNinja.McpServer.Repl --add-source ./local-packages
```

### Update Existing Installation

```powershell
.\scripts\Install-ReplTool.ps1 -Update

# Or manually
dotnet tool update --global SharpNinja.McpServer.Repl --add-source ./local-packages
```

### Uninstall

```powershell
.\scripts\Install-ReplTool.ps1 -Uninstall

# Or manually
dotnet tool uninstall --global SharpNinja.McpServer.Repl
```

### Verify Installation

```bash
mcpserver-repl --version
```

## Usage Modes

### Interactive Mode

Interactive mode provides a guided wizard interface with menus and prompts:

```bash
mcpserver-repl --interactive
```

#### Interactive Features

- **Workspace Selection**: Choose from registered workspaces
- **Session Management**: Bootstrap sessions, begin/complete turns
- **TODO Management**: Create, query, and update TODO items
- **Requirements Tracking**: List and manage FR/TR/TEST requirements
- **Visual Feedback**: Rich terminal UI with tables and progress indicators

### Agent STDIO Mode

Agent STDIO mode implements the MCP protocol over standard input/output for programmatic integration:

```bash
mcpserver-repl --agent-stdio
```

When a workspace marker declares `agent_plugins.policy: required`, agents should normally use their required plugin wrapper instead of invoking `mcpserver-repl --agent-stdio` directly. Direct REPL use is for plugin implementation, plugin diagnostics, and fallback investigation after plugin verification fails.

#### STDIO Features

- **Protocol Compliance**: Full MCP wire protocol support
- **YAML Envelopes**: Structured request/response/error/event messages
- **Command Routing**: Dispatch to all workflow and client namespaces
- **Streaming Events**: Real-time progress and completion notifications
- **Cancellation Support**: Graceful stream cancellation with cleanup

## Configuration

### Server URL

Set the MCP server URL via environment variable (defaults to `http://localhost:7147`):

```powershell
$env:MCP_SERVER_URL = "http://localhost:7147"
mcpserver-repl --interactive
```

### Workspace Path

The tool uses the `X-Workspace-Path` header to route requests to the correct workspace. In interactive mode, you select the workspace from a menu. In STDIO mode, the workspace is set during the handshake.

## YAML Protocol Overview

### Envelope Types

The REPL protocol uses four envelope types:

1. **request**: Command invocation from agent
2. **result**: Successful response from server
3. **error**: Error response with code and details
4. **event**: Server-initiated notifications (streaming, state changes)

Agent STDIO accepts one envelope per YAML document. Multiple request documents may be sent in sequence by separating them with `---`. Do not send a single `type: batch` envelope; unsupported batch envelopes are rejected with `unsupported_batch_envelope`.

### Request Envelope Structure

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-task-001
  method: workflow.sessionlog.beginTurn
  params:
    requestId: req-20260304T113901Z-add-jwt-001
    queryTitle: Add JWT authentication
    queryText: Implement JWT token generation and validation
```

### Result Envelope Structure

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-task-001
  result:
    success: true
    turnRequestId: req-20260304T113901Z-add-jwt-001
    status: in_progress
```

### Error Envelope Structure

```yaml
type: error
payload:
  requestId: req-20260304T113901Z-task-001
  code: invalid_session_id
  message: Session ID does not conform to canonical format
  details:
    providedId: copilot-20260304-feature-auth
    expectedFormat: <Agent>-<yyyyMMddTHHmmssZ>-<suffix>
```

### Event Envelope Structure

```yaml
type: event
payload:
  event: workflow.todo.streamStatus
  data:
    eventType: status.progress
    sequence: 3
    timestamp: 2026-03-04T11:45:30Z
    message: Analyzing TODO dependencies...
    progress: 25
```

## Command Namespaces

### workflow.sessionlog.*

Session logging and turn management. Captures agent activity, reasoning dialog, and work history.

**Common Methods:**
- `workflow.sessionlog.bootstrap` — Initialize subsystem
- `workflow.sessionlog.openSession` — Create new session
- `workflow.sessionlog.beginTurn` — Start new turn
- `workflow.sessionlog.updateTurn` — Update turn metadata
- `workflow.sessionlog.completeTurn` — Complete turn with response
- `workflow.sessionlog.appendDialog` — Add reasoning dialog
- `workflow.sessionlog.appendActions` — Log actions performed
- `workflow.sessionlog.queryHistory` — Query past sessions

### Example: Open Session

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-open-001
  method: workflow.sessionlog.openSession
  params:
    agent: Copilot
    sessionId: Copilot-20260304T113901Z-feature-auth
    title: Implementing JWT authentication
    model: claude-sonnet-4-20250514
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-open-001
  result:
    success: true
    sessionId: Copilot-20260304T113901Z-feature-auth
    agent: Copilot
    title: Implementing JWT authentication
    model: claude-sonnet-4-20250514
    status: in_progress
    started: 2026-03-04T11:39:01Z
```

### workflow.todo.*

TODO item management with structured metadata, dependencies, and requirement traceability.

**Common Methods:**
- `workflow.todo.query` — Query TODO items with filters
- `workflow.todo.get` — Get specific TODO by ID
- `workflow.todo.select` — Select TODO as active context
- `workflow.todo.create` — Create new TODO
- `workflow.todo.update` — Update TODO by ID
- `workflow.todo.updateSelected` — Update currently selected TODO
- `workflow.todo.delete` — Delete TODO by ID
- `workflow.todo.streamStatus` — Stream status analysis events
- `workflow.todo.streamPlan` — Stream plan generation events
- `workflow.todo.streamImplement` — Stream implementation execution

### Example: Create TODO

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-create-001
  method: workflow.todo.create
  params:
    id: MCP-AUTH-001
    title: Implement JWT authentication
    section: Backend
    priority: high
    estimate: 4h
    description:
      - Add JWT token generation
      - Add JWT token validation
    functionalRequirements: [FR-AUTH-001]
    technicalRequirements: [TR-AUTH-001]
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-create-001
  result:
    success: true
    item:
      id: MCP-AUTH-001
      title: Implement JWT authentication
      section: Backend
      priority: high
      done: false
      estimate: 4h
      description:
        - Add JWT token generation
        - Add JWT token validation
      functionalRequirements: [FR-AUTH-001]
      technicalRequirements: [TR-AUTH-001]
```

### workflow.requirements.*

Requirements management for functional (FR), technical (TR), and test (TEST) requirements with traceability matrices.

**Common Methods:**
- `workflow.requirements.listFr` — List functional requirements
- `workflow.requirements.getFr` — Get specific FR by ID
- `workflow.requirements.createFr` — Create new FR
- `workflow.requirements.updateFr` — Update existing FR
- `workflow.requirements.deleteFr` — Delete FR by ID
- `workflow.requirements.listTr` — List technical requirements
- `workflow.requirements.listTest` — List test requirements
- `workflow.requirements.listMappings` — List requirement mappings
- `workflow.requirements.createMapping` — Create new mapping
- `workflow.requirements.generateDocument` — Generate formatted document
- `workflow.requirements.ingestDocument` — Ingest external document

### Example: List Functional Requirements

```yaml
type: request
payload:
  requestId: req-20260304T113901Z-listfr-001
  method: workflow.requirements.listFr
  params:
    area: MCP
    status: in_progress
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T113901Z-listfr-001
  result:
    items:
      - id: FR-MCP-001
        title: Agent authentication
        description: System must authenticate AI agents via API key
        status: completed
        priority: critical
        area: MCP
        createdAt: 2026-03-01T10:00:00Z
        updatedAt: 2026-03-04T11:30:00Z
      - id: FR-MCP-002
        title: Workspace isolation
        description: Each workspace must be isolated from others
        status: in_progress
        priority: high
        area: MCP
        createdAt: 2026-03-01T10:30:00Z
        updatedAt: 2026-03-04T11:45:00Z
    totalCount: 2
```

### client.*

Generic passthrough to all `McpServerClient` sub-clients. Enables dynamic invocation of any server API without compile-time knowledge.

**Supported Sub-Clients:**
- `client.context.*` — Context search and pack operations
- `client.github.*` — GitHub integration (issues, PRs, comments)
- `client.todo.*` — Direct TODO API access
- `client.sessionlog.*` — Direct session log API access
- `client.requirements.*` — Direct requirements API access
- `client.voice.*` — Voice conversation endpoints
- `client.events.*` — Change-event SSE endpoints
- `client.repo.*` — Repository file operations
- `client.desktop.*` — Desktop process launch
- `client.tunnel.*` — Tunnel management
- `client.workspace.*` — Workspace lifecycle management
- `client.configuration.*` — Admin configuration
- `client.tools.*` — Tool registry operations

### Example: Context Search

```yaml
type: request
payload:
  requestId: req-20260304T120000Z-search-001
  method: client.context.SearchAsync
  params:
    query: authentication flow
    limit: 10
```

**Response:**

```yaml
type: result
payload:
  requestId: req-20260304T120000Z-search-001
  result:
    results:
      - key: docs/auth.md
        content: "Authentication flow overview..."
        score: 0.95
      - key: src/AuthService.cs
        content: "public class AuthService..."
        score: 0.87
    totalResults: 2
```

## Common Workflows

### Session Logging Workflow

1. **Bootstrap** (optional, once per session)
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-bootstrap-001
     method: workflow.sessionlog.bootstrap
     params: {}
   ```

2. **Open Session**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-open-001
     method: workflow.sessionlog.openSession
     params:
       agent: Copilot
       sessionId: Copilot-20260304T113901Z-feature-auth
       title: Implementing JWT authentication
       model: claude-sonnet-4-20250514
   ```

3. **Begin Turn**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-begin-001
     method: workflow.sessionlog.beginTurn
     params:
       requestId: req-20260304T113901Z-add-jwt-001
       queryTitle: Add JWT authentication
       queryText: Implement JWT token generation and validation
   ```

4. **Append Dialog** (as work progresses)
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-dialog-001
     method: workflow.sessionlog.appendDialog
     params:
       dialogItems:
         - timestamp: 2026-03-04T11:45:00Z
           role: model
           content: Analyzing requirements...
           category: reasoning
         - timestamp: 2026-03-04T11:45:30Z
           role: tool
           content: Created TokenService.cs
           category: tool_result
   ```

5. **Append Actions** (as files change)
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-action-001
     method: workflow.sessionlog.appendActions
     params:
       actions:
         - order: 1
           description: Created TokenService.cs
           type: create
           status: completed
           filePath: src/TokenService.cs
         - order: 2
           description: Edited Startup.cs to register service
           type: edit
           status: completed
           filePath: src/Startup.cs
   ```

6. **Complete Turn**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-complete-001
     method: workflow.sessionlog.completeTurn
     params:
       response: JWT authentication implemented and tested
   ```

### TODO Workflow

1. **Query TODOs**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-query-001
     method: workflow.todo.query
     params:
       section: Backend
       priority: high
       done: false
   ```

2. **Select TODO**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-select-001
     method: workflow.todo.select
     params:
       id: MCP-AUTH-001
   ```

3. **Update Selected TODO**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-update-001
     method: workflow.todo.updateSelected
     params:
       remaining: Need integration tests
   ```

4. **Stream Status Analysis**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-status-001
     method: workflow.todo.streamStatus
     params:
       id: MCP-AUTH-001
   ```

   **Event Stream:**
   ```yaml
   type: event
   payload:
     event: workflow.todo.streamStatus
     data:
       eventType: status.progress
       sequence: 1
       timestamp: 2026-03-04T11:45:30Z
       message: Analyzing TODO dependencies...
       progress: 25
   ---
   type: event
   payload:
     event: workflow.todo.streamStatus
     data:
       eventType: status.complete
       sequence: 10
       timestamp: 2026-03-04T11:46:00Z
       todoId: MCP-AUTH-001
       status: ready
       blockers: []
       dependencies: [MCP-AUTH-002]
   ```

### Requirements Workflow

1. **List Functional Requirements**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-listfr-001
     method: workflow.requirements.listFr
     params:
       area: MCP
       status: in_progress
   ```

2. **Create Technical Requirement**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-createtr-001
     method: workflow.requirements.createTr
     params:
       id: TR-MCP-PERF-001
       title: Response time SLA
       description: All API endpoints must respond within 500ms p99
       priority: high
       area: MCP
       subarea: PERF
   ```

3. **Create Mapping**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-createmap-001
     method: workflow.requirements.createMapping
     params:
       frId: FR-MCP-001
       trId: TR-MCP-ARCH-001
       testId: TEST-MCP-001
       notes: Core authentication flow
   ```

4. **Generate Traceability Matrix**
   ```yaml
   type: request
   payload:
     requestId: req-20260304T113901Z-gendoc-001
     method: workflow.requirements.generateDocument
     params:
       format: markdown
       docType: matrix
   ```

## Identifier Conventions

### Session IDs

**Format:** `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`

**Regex:** `^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`

**Examples:**
- `Copilot-20260304T113901Z-feature-auth`
- `Cline-20260304T120000Z-bugfix-timeout`
- `Cursor-20260304T150000Z-refactor-session`

### Request IDs

**Format:** `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`

**Regex:** `^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`

**Examples:**
- `req-20260304T113901Z-add-jwt-001`
- `req-20260304T120000Z-query-todos`
- `req-20260304T150000Z-create-fr-002`

### TODO IDs

**Format:** uppercase kebab-case ending in `-###` or `ISSUE-{number}`

**Regex:** `^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$` or `^ISSUE-\d+$`

**Examples:**
- `MCP-AUTH-001`
- `PHASE0-REMOTE-001`
- `MCP-TODO-CREATE-001`
- `PLAN-NAMINGCONVENTIONS-001`
- `ISSUE-17`

### Requirement IDs

**FR Format:** `^FR-[A-Z]+-\d{3}$`  
**TR Format:** `^TR-[A-Z]+-[A-Z]+-\d{3}$`  
**TEST Format:** `^TEST-[A-Z]+-\d{3}$`

**Examples:**
- `FR-MCP-001`
- `TR-MCP-ARCH-001`
- `TEST-MCP-001`

## Error Handling

### Error Codes

All error responses include a standardized error code:

**Session Log Errors:**
- `bootstrap_failed` — Bootstrap initialization failed
- `session_not_found` — No active session exists
- `session_already_exists` — Session ID already in use
- `invalid_session_id` — Session ID format invalid
- `invalid_request_id` — Request ID format invalid
- `turn_not_found` — Turn does not exist
- `turn_already_exists` — Turn with same request ID exists
- `turn_immutable` — Cannot modify completed/failed turn
- `invalid_parameter` — Required parameter missing or invalid
- `storage_error` — Underlying storage failed

**TODO Errors:**
- `todo_not_found` — TODO with specified ID not found
- `todo_already_exists` — TODO with same ID exists
- `invalid_todo_id` — TODO ID format invalid
- `no_selection` — No TODO currently selected
- `projection_error` — Projection operation failed
- `stream_error` — Streaming operation failed

**Requirements Errors:**
- `requirement_not_found` — Requirement not found
- `requirement_already_exists` — Requirement with same ID exists
- `invalid_requirement_id` — Requirement ID format invalid
- `mapping_not_found` — Mapping not found
- `invalid_mapping` — Mapping references non-existent requirements
- `document_generation_error` — Document generation failed
- `document_ingestion_error` — Document ingestion failed

**Client Passthrough Errors:**
- `unknown_client` — Client name not found
- `unknown_method` — Method name not found on client
- `missing_required_parameter` — Required parameter missing
- `type_conversion_error` — Argument type coercion failed
- `invalid_enum_value` — Invalid enum string value
- `method_invocation_error` — Underlying method invocation failed

### Example Error Response

```yaml
type: error
payload:
  requestId: req-20260304T113901Z-open-001
  code: invalid_session_id
  message: Session ID does not conform to canonical format
  details:
    providedId: copilot-20260304-feature-auth
    expectedFormat: <Agent>-<yyyyMMddTHHmmssZ>-<suffix>
```

## Troubleshooting

### Tool Not Found

```powershell
# Check if installed
dotnet tool list -g

# Reinstall
.\scripts\Install-ReplTool.ps1 -Uninstall
.\scripts\Install-ReplTool.ps1
```

### Can't Connect to Server

```powershell
# Check server is running
curl http://localhost:7147/health

# Set correct URL
$env:MCP_SERVER_URL = "http://localhost:7147"
```

### Invalid Session ID Format

Ensure session IDs follow the canonical format:
- Start with PascalCase agent name
- Include ISO 8601 timestamp: `yyyyMMddTHHmmssZ`
- End with lowercase kebab-case suffix

Valid: `Copilot-20260304T113901Z-feature-auth`  
Invalid: `copilot-20260304-feature-auth` (lowercase, missing time)

### YAML Parsing Errors

- Ensure proper indentation (2 spaces per level)
- Use `---` to separate multiple envelopes in a stream
- Quote strings containing special characters
- Use array syntax `[item1, item2]` or block syntax

## Migration from PowerShell Modules

### McpSession.psm1 → workflow.sessionlog.*

**Old (PowerShell):**
```powershell
Import-Module ./McpSession.psm1
Initialize-McpSession -Agent "Copilot" -Model "claude-sonnet-4"
$s = New-McpSessionLog -SourceType "Copilot" -Title "Feature X" -Model "claude-sonnet-4"
$t = Add-McpSessionTurn -Session $s -QueryTitle "Add auth" -QueryText "Add JWT"
Update-McpSessionLog -Session $s
```

**New (REPL YAML):**
```yaml
type: request
payload:
  requestId: req-20260304T113901Z-open-001
  method: workflow.sessionlog.openSession
  params:
    agent: Copilot
    sessionId: Copilot-20260304T113901Z-feature-x
    title: Feature X
    model: claude-sonnet-4
---
type: request
payload:
  requestId: req-20260304T113901Z-begin-001
  method: workflow.sessionlog.beginTurn
  params:
    requestId: req-20260304T113901Z-add-auth-001
    queryTitle: Add auth
    queryText: Add JWT
```

### McpTodo.psm1 → workflow.todo.*

**Old (PowerShell):**
```powershell
Import-Module ./McpTodo.psm1
Initialize-McpTodo
New-McpTodo -Id "MCP-AUTH-001" -Title "Add JWT" -Section "Backend" -Priority high
Update-McpTodo -Id "MCP-AUTH-001" -Remaining "Need tests"
Complete-McpTodo -Id "MCP-AUTH-001" -DoneSummary "Complete"
```

**New (REPL YAML):**
```yaml
type: request
payload:
  requestId: req-20260304T113901Z-create-001
  method: workflow.todo.create
  params:
    id: MCP-AUTH-001
    title: Add JWT
    section: Backend
    priority: high
---
type: request
payload:
  requestId: req-20260304T113901Z-update-001
  method: workflow.todo.update
  params:
    id: MCP-AUTH-001
    remaining: Need tests
---
type: request
payload:
  requestId: req-20260304T113901Z-update-002
  method: workflow.todo.update
  params:
    id: MCP-AUTH-001
    done: true
    doneSummary: Complete
```

## Additional Resources

- **Source Code**: https://github.com/SharpNinja/McpServer
- **API Documentation**: `docs/context/api-capabilities.md`
- **Session Log Schema**: `docs/context/session-log-schema.md`
- **TODO Schema**: `docs/context/todo-schema.md`
- **Module Bootstrap**: `docs/context/module-bootstrap.md`
- **Agent Guide**: `docs/REPL-AGENT-GUIDE.md`
- **Agent Plugin Availability**: `docs/AGENT-PLUGIN-AVAILABILITY.md`
- **Federation Reference**: `docs/context/federation.md`

## License

MIT

## Author

SharpNinja
