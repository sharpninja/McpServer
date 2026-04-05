# REPL Host Implementation Phases

This document defines the iterative implementation phases for the YAML-protocol STDIO REPL host (FR-MCP-REPL-001 through FR-MCP-REPL-005).

## Phase 1: Core Protocol and Lifecycle

**Objective:** Establish YAML envelope protocol, command loop infrastructure, and graceful lifecycle management.

**Deliverables:**
- `McpReplHost` as `IHostedService` with DI integration
- YAML envelope parsing (command, args, correlationId)
- YAML response serialization (status, result, error, correlationId)
- Command loop reading from stdin and writing to stdout
- Graceful shutdown on EOF or explicit `exit` command
- Lifecycle events: `repl_started`, `repl_stopped`
- Malformed YAML error handling without process crash

**Acceptance Criteria:**
- TEST-MCP-REPL-001: Well-formed YAML command envelopes return structured responses
- TEST-MCP-REPL-002: Malformed YAML returns structured error without crashing
- TEST-MCP-REPL-013: EOF or `exit` triggers graceful shutdown with exit code 0
- TEST-MCP-REPL-018: Lifecycle events are logged with structured properties

**Requirements Covered:**
- FR-MCP-REPL-001 (YAML protocol)
- FR-MCP-REPL-002 (lifecycle management)
- TR-MCP-REPL-001 (YAML envelope protocol)
- TR-MCP-REPL-002 (DI integration)
- TR-MCP-REPL-003 (command loop lifecycle)

---

## Phase 2: Command Registry and Dispatcher

**Objective:** Build extensible command registration, handler dispatch, and error handling infrastructure.

**Deliverables:**
- `IReplCommandHandler<TArgs, TResult>` interface
- `ReplCommandRegistry` with command-name-to-handler mappings
- `ReplCommandDispatcher` resolving handlers from DI per invocation
- YamlDotNet model binding for args dictionaries to typed handler parameters
- Correlation ID echo in responses
- Unhandled exception handling with `internal_error` responses
- Command loop continuation after handler exceptions

**Acceptance Criteria:**
- TEST-MCP-REPL-014: Handler exceptions emit structured errors and loop continues
- TEST-MCP-REPL-015: Correlation IDs are echoed in responses
- TEST-MCP-REPL-016: All handlers are resolved from DI without direct instantiation
- TEST-MCP-REPL-019: Handler dispatch reuses existing service contracts

**Requirements Covered:**
- TR-MCP-REPL-004 (command registry and dispatcher)

---

## Phase 3: Trust Bootstrap and Authentication

**Objective:** Implement marker-file trust bootstrap with signature verification, health nonce challenge, and API key caching.

**Deliverables:**
- `bootstrap` command with marker file path argument
- `MarkerFileVerifier` for signature verification
- `HealthChallengeClient` for nonce-based `/health` validation
- `ReplAuthenticationState` caching validated API key
- `auth_required` error for operational commands before bootstrap
- Token rotation detection and `token_rotated` warnings
- Bootstrap parity with PowerShell modules (McpSession, McpTodo, McpContext)

**Acceptance Criteria:**
- TEST-MCP-REPL-003: Operational commands before bootstrap return `auth_required`
- TEST-MCP-REPL-004: Bootstrap verifies marker signature and health nonce
- TEST-MCP-REPL-005: Token rotation emits warnings without immediate failure

**Requirements Covered:**
- FR-MCP-REPL-004 (trust bootstrap and auth rotation)
- TR-MCP-REPL-006 (trust bootstrap and token validation)

---

## Phase 4: Core Domain Command Handlers

**Objective:** Implement namespace-organized command handlers for TODO, session log, and context operations with functional parity to REST endpoints.

**Deliverables:**
- TODO commands: `todo.list`, `todo.create`, `todo.update`, `todo.delete`, `todo.move`
- Session log commands: `session.create`, `session.append`, `session.list`, `session.query`
- Context commands: `context.search`, `context.ingest`, `context.ingest_website`
- Handler implementations delegating to `ITodoService`, `ISessionLogService`, `IContextService`
- Workspace context resolution matching HTTP `X-Workspace-Path` behavior
- Response payloads matching REST endpoint semantics

**Acceptance Criteria:**
- TEST-MCP-REPL-006: TODO command results match REST endpoint responses
- TEST-MCP-REPL-007: Session log command results match REST endpoint responses
- TEST-MCP-REPL-008: Context command results match REST endpoint responses
- TEST-MCP-REPL-017: Workspace context resolution matches HTTP behavior

**Requirements Covered:**
- FR-MCP-REPL-003 (command namespace parity)
- TR-MCP-REPL-005 (namespace organization and handler parity)

---

## Phase 5: Requirements and Workspace Management Handlers

**Objective:** Implement command handlers for requirements management and workspace configuration with functional parity to REST endpoints.

**Deliverables:**
- Requirements commands: `requirements.list`, `requirements.create`, `requirements.update`, `requirements.delete`, `requirements.generate`
- Workspace commands: `workspace.list`, `workspace.status`, `workspace.config`, `workspace.create`, `workspace.init`
- Handler implementations delegating to `IRequirementsDocumentService`, `IWorkspaceService`
- Multi-tenant workspace isolation verification
- Response payloads matching REST endpoint semantics

**Acceptance Criteria:**
- TEST-MCP-REPL-009: Requirements command results match REST endpoint responses
- TEST-MCP-REPL-010: Workspace command results match REST endpoint responses
- TEST-MCP-REPL-020: Concurrent execution preserves workspace data isolation

**Requirements Covered:**
- FR-MCP-REPL-003 (command namespace parity)
- TR-MCP-REPL-005 (namespace organization and handler parity)

---

## Phase 6: Orchestration State Visibility

**Objective:** Implement read-only state query commands for agent pool, voice sessions, and event subscriptions.

**Deliverables:**
- Agent pool commands: `agent_pool.list`, `agent_pool.queue`, `agent_pool.status`
- Voice session commands: `voice.sessions`, `voice.session_status`
- Workspace events command: `workspace.events.status`
- Handler implementations delegating to `IAgentPoolService`, `VoiceConversationService`, `IChangeEventBus`
- State snapshot queries without blocking on long-running operations
- No persistent subscription establishment

**Acceptance Criteria:**
- TEST-MCP-REPL-011: Agent pool commands return current snapshots without blocking
- TEST-MCP-REPL-012: Voice session commands return active sessions without blocking

**Requirements Covered:**
- FR-MCP-REPL-005 (orchestration state visibility)
- TR-MCP-REPL-007 (state query commands)

---

## Cross-Phase Testing Requirements

The following testing requirements apply across all phases:

### Unit Test Strategies
- Handler logic unit tests with mocked service dependencies
- YAML envelope parsing/serialization unit tests
- Command registry registration and resolution unit tests
- Authentication state caching and rotation detection unit tests
- Error code coverage for all failure paths

### Integration Test Scenarios
- End-to-end REPL session with bootstrap and operational commands
- Multi-command session maintaining authentication state
- Concurrent REPL processes targeting different workspaces
- Workspace data isolation verification
- Service contract parity between REPL handlers and REST controllers

### Human Validation
- Interactive REPL session using stdio pipe from terminal
- Manual marker-file trust bootstrap workflow
- Token rotation behavior during active session
- Error message clarity and actionability
- Command help/discovery workflow (if implemented)

---

## Implementation Dependencies

### Required Infrastructure
- YamlDotNet for YAML parsing and serialization
- Existing `IHostedService` registration patterns from `McpStdioHost`
- Existing marker file signature and health nonce verification logic
- Existing service contracts (`ITodoService`, `ISessionLogService`, etc.)
- Existing workspace context resolution middleware patterns

### Architectural Constraints
- No direct service instantiation via `new` or `ActivatorUtilities.CreateInstance`
- DI-centered single source of truth (FR-MCP-059, TR-MCP-ARCH-002)
- DRY principle: reuse existing service logic (TR-MCP-DRY-001)
- Exception logging in all catch blocks (TR-MCP-LOG-001)
- Command handler isolation via scoped DI containers per command

---

## Migration Path

The REPL host provides a third transport option alongside HTTP REST and MCP STDIO:

- **HTTP REST**: Web-based integration, standard HTTP/JSON semantics
- **MCP STDIO**: Native Model Context Protocol tool surface for AI agents
- **REPL YAML STDIO**: Interactive command-line workflows, scripting, automation

No existing functionality is deprecated. REPL host is additive and optional.
