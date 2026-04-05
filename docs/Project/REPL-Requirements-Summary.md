# REPL Host Requirements Summary

This document provides a quick reference summary of all REPL Host requirements.

## Functional Requirements

| ID | Title | Status |
|----|-------|--------|
| FR-MCP-REPL-001 | YAML Protocol STDIO REPL Host | 🔴 Planned |
| FR-MCP-REPL-002 | REPL Lifecycle Management | 🔴 Planned |
| FR-MCP-REPL-003 | Command Namespace Parity | 🔴 Planned |
| FR-MCP-REPL-004 | Trust Bootstrap and Auth Rotation | 🔴 Planned |
| FR-MCP-REPL-005 | Orchestration State Visibility | 🔴 Planned |

## Technical Requirements

| ID | Title | Status | Components |
|----|-------|--------|------------|
| TR-MCP-REPL-001 | YAML Envelope Protocol | 🔴 Planned | YAML parsing, response serialization |
| TR-MCP-REPL-002 | DI-Integrated REPL Host | 🔴 Planned | McpReplHost, Program.cs, McpReplHostOptions |
| TR-MCP-REPL-003 | Command Loop Lifecycle | 🔴 Planned | McpReplHost, ReplCommandLoop, ReplLifecycleEventPublisher |
| TR-MCP-REPL-004 | Command Registry and Dispatcher | 🔴 Planned | IReplCommandHandler<TArgs, TResult>, ReplCommandRegistry, ReplCommandDispatcher |
| TR-MCP-REPL-005 | Namespace Organization and Handler Parity | 🔴 Planned | TodoCommandHandlers, SessionLogCommandHandlers, ContextCommandHandlers, RequirementsCommandHandlers, WorkspaceCommandHandlers, AgentPoolCommandHandlers |
| TR-MCP-REPL-006 | Trust Bootstrap and Token Validation | 🔴 Planned | BootstrapCommandHandler, ReplAuthenticationState, MarkerFileVerifier, HealthChallengeClient |
| TR-MCP-REPL-007 | State Query Commands | 🔴 Planned | AgentPoolStatusCommandHandler, VoiceSessionListCommandHandler, WorkspaceEventsStatusCommandHandler |

## Testing Requirements

| ID | Category | Description |
|----|----------|-------------|
| TEST-MCP-REPL-001 | Unit | Well-formed YAML command → structured response |
| TEST-MCP-REPL-002 | Unit | Malformed YAML → structured error without crash |
| TEST-MCP-REPL-003 | Unit | No bootstrap → auth_required error |
| TEST-MCP-REPL-004 | Integration | Bootstrap with marker verification and health nonce |
| TEST-MCP-REPL-005 | Integration | Token rotation detection and warnings |
| TEST-MCP-REPL-006 | Integration | TODO command parity with REST |
| TEST-MCP-REPL-007 | Integration | Session log command parity with REST |
| TEST-MCP-REPL-008 | Integration | Context command parity with REST |
| TEST-MCP-REPL-009 | Integration | Requirements command parity with REST |
| TEST-MCP-REPL-010 | Integration | Workspace command parity with REST |
| TEST-MCP-REPL-011 | Integration | Agent pool state queries without blocking |
| TEST-MCP-REPL-012 | Integration | Voice session state queries without blocking |
| TEST-MCP-REPL-013 | Unit | EOF/exit → graceful shutdown with exit code 0 |
| TEST-MCP-REPL-014 | Unit | Handler exception → structured error, loop continues |
| TEST-MCP-REPL-015 | Unit | Correlation ID echo in responses |
| TEST-MCP-REPL-016 | Unit | Handler DI resolution without direct instantiation |
| TEST-MCP-REPL-017 | Integration | Workspace context resolution matches HTTP behavior |
| TEST-MCP-REPL-018 | Unit | Lifecycle events logged with structured properties |
| TEST-MCP-REPL-019 | Unit | Handler dispatch reuses service contracts |
| TEST-MCP-REPL-020 | Integration | Concurrent execution preserves workspace isolation |

## Command Namespaces

### Bootstrap and Lifecycle
- `bootstrap` - Marker file trust bootstrap
- `exit` - Graceful shutdown

### TODO Operations (6 commands)
- `todo.list`
- `todo.create`
- `todo.update`
- `todo.delete`
- `todo.move`
- `todo.query`

### Session Log Operations (4 commands)
- `session.create`
- `session.append`
- `session.list`
- `session.query`

### Context Operations (3 commands)
- `context.search`
- `context.ingest`
- `context.ingest_website`

### Requirements Management (5 commands)
- `requirements.list`
- `requirements.create`
- `requirements.update`
- `requirements.delete`
- `requirements.generate`

### Workspace Management (5 commands)
- `workspace.list`
- `workspace.status`
- `workspace.config`
- `workspace.create`
- `workspace.init`

### Orchestration State (5 commands)
- `agent_pool.list`
- `agent_pool.queue`
- `agent_pool.status`
- `voice.sessions`
- `workspace.events.status`

**Total Commands:** 33

## Implementation Phases

| Phase | Focus Area | Deliverables |
|-------|-----------|--------------|
| 1 | Core Protocol and Lifecycle | YAML envelope, command loop, lifecycle events |
| 2 | Command Registry and Dispatcher | Handler infrastructure, DI integration, error handling |
| 3 | Trust Bootstrap and Authentication | Marker verification, health nonce, API key caching |
| 4 | Core Domain Command Handlers | TODO, session log, context commands |
| 5 | Requirements and Workspace Management | Requirements and workspace commands |
| 6 | Orchestration State Visibility | Agent pool, voice sessions, event status queries |

## Requirements Traceability Matrix

| FR | Primary TRs | Test Coverage |
|----|-------------|---------------|
| FR-MCP-REPL-001 | TR-MCP-REPL-001, TR-MCP-REPL-002 | TEST-MCP-REPL-001, TEST-MCP-REPL-002, TEST-MCP-REPL-015 |
| FR-MCP-REPL-002 | TR-MCP-REPL-003 | TEST-MCP-REPL-013, TEST-MCP-REPL-014, TEST-MCP-REPL-018 |
| FR-MCP-REPL-003 | TR-MCP-REPL-004, TR-MCP-REPL-005 | TEST-MCP-REPL-006 through TEST-MCP-REPL-010, TEST-MCP-REPL-016, TEST-MCP-REPL-019 |
| FR-MCP-REPL-004 | TR-MCP-REPL-006 | TEST-MCP-REPL-003, TEST-MCP-REPL-004, TEST-MCP-REPL-005 |
| FR-MCP-REPL-005 | TR-MCP-REPL-007 | TEST-MCP-REPL-011, TEST-MCP-REPL-012 |

## Related Documentation

- [REPL-Host-Overview.md](./REPL-Host-Overview.md) - Comprehensive architecture and design
- [REPL-Implementation-Phases.md](./REPL-Implementation-Phases.md) - Detailed phase breakdown
- [Functional-Requirements.md](./Functional-Requirements.md#fr-mcp-repl-001) - FR definitions
- [Technical-Requirements.md](./Technical-Requirements.md#tr-mcp-repl-001) - TR definitions
- [Testing-Requirements.md](./Testing-Requirements.md#repl-host-testing-requirements) - TEST definitions
- [TR-per-FR-Mapping.md](./TR-per-FR-Mapping.md) - Traceability mapping
- [Requirements-Matrix.md](./Requirements-Matrix.md) - Implementation status matrix

## Architectural Constraints

The REPL Host implementation must adhere to these architectural principles:

1. **DI-Centered** (FR-MCP-059, TR-MCP-ARCH-002)
   - All services resolved from DI
   - No `new` or `ActivatorUtilities.CreateInstance` outside registration
   - Scoped containers per command invocation

2. **DRY Principle** (TR-MCP-DRY-001)
   - Reuse existing service contracts
   - No duplicated business logic
   - Handlers delegate to `ITodoService`, `ISessionLogService`, etc.

3. **Exception Logging** (TR-MCP-LOG-001)
   - All catch blocks must log exceptions
   - `LogError` for unexpected exceptions
   - `LogWarning` for anticipated exceptions

4. **Single Source of Truth** (FR-MCP-059, TR-MCP-ARCH-002)
   - Pull-based state access from authoritative services
   - No pushed state payloads
   - Observable state via `INotifyPropertyChanged`

5. **Transport Parity**
   - REPL handlers semantically equivalent to REST endpoints
   - Same workspace resolution logic
   - Same authentication/authorization semantics

## Dependencies

### Framework Dependencies
- YamlDotNet (YAML parsing and serialization)
- Microsoft.Extensions.Hosting (IHostedService)
- Microsoft.Extensions.DependencyInjection (DI container)

### Internal Dependencies
- Existing service contracts (ITodoService, ISessionLogService, etc.)
- Existing marker file signature verification logic
- Existing workspace context resolution middleware
- Existing authentication and authorization infrastructure

### No Breaking Changes
- HTTP REST API unchanged
- MCP STDIO transport unchanged
- REPL is additive, optional third transport
- All existing functionality preserved
