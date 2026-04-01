# REPL Host Overview

## Introduction

The REPL Host is a YAML-protocol STDIO transport for the MCP Server that provides an interactive command-line interface for workspace operations. It complements the existing HTTP REST API and MCP STDIO transports with a human-friendly, scriptable interface suitable for automation, debugging, and interactive workflows.

## Architecture

### Transport Triad

The MCP Server supports three transport mechanisms:

1. **HTTP REST** (`/mcpserver/*` endpoints)
   - Web-based integration
   - Standard HTTP/JSON semantics
   - Browser and web client compatible
   - Supports multi-workspace via `X-Workspace-Path` header

2. **MCP STDIO** (`McpStdioHost`)
   - Native Model Context Protocol tool surface
   - JSON-RPC over STDIO
   - AI agent integration (Cline, Copilot, etc.)
   - Tool-based invocation model

3. **REPL YAML STDIO** (`McpReplHost`) ⬅ **NEW**
   - YAML-envelope command protocol
   - Interactive command-line workflows
   - Scriptable automation
   - Human-friendly structured I/O

### Design Principles

The REPL Host follows these architectural principles:

- **DI-Centered**: All services resolved from DI; no direct instantiation
- **Service Delegation**: Handlers delegate to existing service contracts (DRY)
- **Transport Parity**: Functional equivalence with HTTP REST endpoints
- **Trust Bootstrap**: Same marker-file signature and health nonce verification as PowerShell modules
- **Multi-Tenancy**: Workspace context resolution matching HTTP behavior
- **Graceful Degradation**: Structured errors without process crashes

## Protocol

### Command Envelope (YAML, stdin)

```yaml
command: todo.create
correlationId: req-001
args:
  id: IMPL-MCP-001
  title: "Implement REPL host"
  priority: HIGH
```

### Response Envelope (YAML, stdout)

```yaml
status: success
correlationId: req-001
result:
  id: IMPL-MCP-001
  title: "Implement REPL host"
  priority: HIGH
  status: TODO
  createdAt: 2026-03-04T12:00:00Z
```

### Error Response (YAML, stdout)

```yaml
status: error
correlationId: req-001
error:
  code: auth_required
  message: "Bootstrap command must be invoked before operational commands"
```

## Command Namespaces

Commands are organized by functional domain with dot-delimited namespaces:

### Bootstrap and Lifecycle
- `bootstrap` - Verify marker file, cache API key
- `exit` - Graceful shutdown

### TODO Operations
- `todo.list` - Query TODO items
- `todo.create` - Create new TODO
- `todo.update` - Update existing TODO
- `todo.delete` - Delete TODO
- `todo.move` - Move TODO to another workspace

### Session Log Operations
- `session.create` - Create new session log
- `session.append` - Append request/turn
- `session.list` - Query session logs
- `session.query` - Search session logs

### Context Operations
- `context.search` - Hybrid search (vector + FTS)
- `context.ingest` - Ingest repository files
- `context.ingest_website` - Ingest remote website

### Requirements Management
- `requirements.list` - List requirements
- `requirements.create` - Create requirement entry
- `requirements.update` - Update requirement entry
- `requirements.delete` - Delete requirement entry
- `requirements.generate` - Render requirements documents

### Workspace Management
- `workspace.list` - List registered workspaces
- `workspace.status` - Query workspace status
- `workspace.config` - Get/update workspace config
- `workspace.create` - Register new workspace
- `workspace.init` - Initialize workspace scaffold

### Orchestration State (Read-Only)
- `agent_pool.list` - List pooled agents
- `agent_pool.queue` - Query one-shot queue
- `agent_pool.status` - Agent availability snapshot
- `voice.sessions` - List active voice sessions
- `voice.session_status` - Query session details
- `workspace.events.status` - Event subscription status

## Trust and Authentication

### Bootstrap Flow

1. **Read Marker File**
   - Parse `AGENTS-README-FIRST.yaml` from workspace root
   - Extract API key, endpoints, signature

2. **Verify Signature**
   - Validate marker signature using server public key
   - Fail bootstrap if signature mismatch

3. **Health Nonce Challenge**
   - Generate random nonce
   - POST to `/health` with nonce in body
   - Verify response echoes nonce exactly
   - Fail bootstrap if nonce mismatch

4. **Cache API Key**
   - Store validated API key in session state
   - Use cached key for subsequent command authentication

5. **Token Rotation Detection**
   - Periodically re-read marker file
   - Compare cached key against current marker key
   - Emit `token_rotated` warning on mismatch
   - Continue using cached key until re-bootstrap

### Bootstrap Parity

The REPL bootstrap flow uses the same trust semantics as PowerShell modules:
- `McpSession.psm1`
- `McpTodo.psm1`
- `McpContext.psm1`

This ensures consistent security posture across all STDIO-based integrations.

## Lifecycle

### Startup Sequence

1. DI container initialization
2. `McpReplHost` registered as `IHostedService`
3. Host startup emits `repl_started` lifecycle event
4. Command loop begins reading from stdin

### Command Processing

1. Read line from stdin
2. Parse YAML envelope
3. Resolve command handler from DI
4. Create scoped DI container for handler invocation
5. Deserialize `args` to handler parameter types
6. Execute `HandleAsync(args, cancellationToken)`
7. Serialize result to YAML response envelope
8. Write response to stdout
9. Dispose scoped container
10. Continue loop

### Error Handling

- **Malformed YAML**: Emit `yaml_parse_error` response, continue loop
- **Unknown command**: Emit `unknown_command` response, continue loop
- **Auth required**: Emit `auth_required` response, continue loop
- **Handler exception**: Emit `internal_error` response, log exception, continue loop

### Shutdown

- EOF on stdin triggers graceful shutdown
- `exit` command triggers graceful shutdown
- Host emits `repl_stopped` lifecycle event
- Process exits with code 0

## Use Cases

### Interactive Debugging
```bash
echo "command: todo.list" | dotnet McpServer.Support.Mcp.dll --mode repl
```

### Scripted Automation
```bash
cat commands.yaml | dotnet McpServer.Support.Mcp.dll --mode repl > results.yaml
```

### CI/CD Integration
```bash
# Generate requirements snapshot for PR validation
echo "command: requirements.generate
args:
  doc: all" | dotnet McpServer.Support.Mcp.dll --mode repl
```

### Workspace Orchestration
```bash
# Bootstrap and query agent pool state
cat <<EOF | dotnet McpServer.Support.Mcp.dll --mode repl
command: bootstrap
args:
  markerPath: /workspace/AGENTS-README-FIRST.yaml
---
command: agent_pool.list
---
command: agent_pool.queue
EOF
```

## Testing Strategy

### Unit Tests
- YAML envelope parsing and serialization
- Command registry and handler resolution
- Authentication state caching and rotation detection
- Error code coverage for all failure paths

### Integration Tests
- End-to-end REPL sessions with multiple commands
- Workspace data isolation verification
- Service contract parity with REST endpoints
- Trust bootstrap flow validation

### Human Validation
- Interactive terminal sessions
- Manual token rotation workflows
- Error message clarity assessment
- Command discovery and help workflows

## Implementation Phases

The REPL Host is implemented in six iterative phases:

1. **Core Protocol and Lifecycle** - YAML envelope, command loop, lifecycle events
2. **Command Registry and Dispatcher** - Handler infrastructure, DI integration
3. **Trust Bootstrap and Authentication** - Marker verification, health nonce, API key caching
4. **Core Domain Command Handlers** - TODO, session log, context operations
5. **Requirements and Workspace Management Handlers** - Requirements and workspace commands
6. **Orchestration State Visibility** - Agent pool, voice sessions, event status

See [REPL-Implementation-Phases.md](./REPL-Implementation-Phases.md) for detailed phase deliverables and acceptance criteria.

## Requirements Traceability

### Functional Requirements
- **FR-MCP-REPL-001**: YAML Protocol STDIO REPL Host
- **FR-MCP-REPL-002**: REPL Lifecycle Management
- **FR-MCP-REPL-003**: Command Namespace Parity
- **FR-MCP-REPL-004**: Trust Bootstrap and Auth Rotation
- **FR-MCP-REPL-005**: Orchestration State Visibility

### Technical Requirements
- **TR-MCP-REPL-001**: YAML Envelope Protocol
- **TR-MCP-REPL-002**: DI-Integrated REPL Host
- **TR-MCP-REPL-003**: Command Loop Lifecycle
- **TR-MCP-REPL-004**: Command Registry and Dispatcher
- **TR-MCP-REPL-005**: Namespace Organization and Handler Parity
- **TR-MCP-REPL-006**: Trust Bootstrap and Token Validation
- **TR-MCP-REPL-007**: State Query Commands

### Testing Requirements
- **TEST-MCP-REPL-001** through **TEST-MCP-REPL-020**: Comprehensive test coverage for all REPL functionality

See [Functional-Requirements.md](./Functional-Requirements.md), [Technical-Requirements.md](./Technical-Requirements.md), and [Testing-Requirements.md](./Testing-Requirements.md) for complete requirement definitions.

## Migration and Compatibility

The REPL Host is **additive and optional**:

- No existing functionality is deprecated
- HTTP REST API remains unchanged
- MCP STDIO transport remains unchanged
- REPL Host is a third transport option
- Workspaces can use any combination of transports

### Configuration

REPL mode is activated via command-line argument:

```bash
dotnet McpServer.Support.Mcp.dll --mode repl
```

Or environment variable:

```bash
MCP_HOST_MODE=repl dotnet McpServer.Support.Mcp.dll
```

Default mode remains HTTP hosting with optional STDIO transport when invoked without arguments.

## Future Enhancements

Potential future additions (not in current scope):

- Command history and readline support
- Tab completion for command names
- Built-in `help` command with usage examples
- Multi-document YAML streaming (YAML 1.2 document separator)
- Async streaming responses for long-running commands
- Persistent session state across process restarts
- WebSocket-based REPL for remote access
