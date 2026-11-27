# TR-MCP-BYRD

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-BYRD-001

**Workspace-Scoped Byrd Execution Store** — The server SHALL persist Byrd iteration phases, execution TODOs, and TODO checkpoints in a workspace-scoped durable store under `.mcpServer`, with stable IDs for phases, TODOs, and checkpoints. The execution store SHALL coexist with the existing TODO providers without breaking legacy TODO CRUD behavior.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Services/Models/TodoExecutionModels.cs`, `src/McpServer.Services/Services/ITodoExecutionService.cs`, `src/McpServer.Services/Services/TodoExecutionService.cs`
Scope: layer-1+

## TR-MCP-BYRD-002

**Bounded Hydration and Delta Queries** — The server SHALL hydrate a bounded execution context for the active Byrd TODO using requirement snippets, recent session-turn summaries, relevant files, artifacts, validation state, and execution pointers. It SHALL also return checkpoint-based delta context that reports only the new turns, artifacts, commits, and next action since a specified checkpoint.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Services/Services/TodoExecutionService.cs`, `src/McpServer.Support.Mcp/Controllers/TodoExecutionController.cs`, `src/McpServer.Support.Mcp/McpStdio/McpServerMcpTools.cs`, `src/McpServer.Client/TodoClient.cs`
Scope: layer-1+

## TR-MCP-BYRD-003

**Byrd Progression Enforcement** — The execution service SHALL enforce Byrd progression rules so implementation cannot begin before unit tests are defined, validation cannot begin without implementation evidence, blocked TODOs require an explicit resume reason, and completion requires passing validation plus satisfied acceptance criteria. Test-plan updates, checkpoints, validation results, and session-turn linking SHALL update the persisted execution pointers used for resumption.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Services/Services/TodoExecutionService.cs`, `src/McpServer.Support.Mcp/Controllers/TodoExecutionController.cs`
Scope: layer-1+

## TR-MCP-BYRD-004

**Structured TODO Execution Surfaces** — The server SHALL expose the Byrd execution workflow through REST endpoints, STDIO MCP tools, and typed client methods, including the safe `adb_step` action surface for Android validation. The exposed contracts SHALL remain structured and bounded for iteration phase creation, plan decomposition, active TODO selection, execution context hydration, checkpoint append, validation result recording, status progression, session-turn linking, and device actions.
**Status:** ✅ Complete

**Covered by:** `src/McpServer.Support.Mcp/Controllers/TodoExecutionController.cs`, `src/McpServer.Support.Mcp/McpStdio/McpServerMcpTools.cs`, `src/McpServer.Client/Models/TodoModels.cs`, `src/McpServer.Client/TodoClient.cs`
Scope: layer-1+

## TR-MCP-BYRD-005

**Byrd process plan creation requirements** — The Byrd Development Process V3 document must define plan creation requirements for decision-complete frontier-model handoff plans, including required FR/TR/TEST capture, TDD tests, expected red state, green criteria, validation scope, and acceptance criteria before implementation begins.
**Covered by:** FR: FR-MCP-104; TEST: TEST-MCP-137, TEST-MCP-141
**Status:** pending
Scope: layer-1+

