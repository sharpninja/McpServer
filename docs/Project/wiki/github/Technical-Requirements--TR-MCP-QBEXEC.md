# TR-MCP-QBEXEC

[Back to Technical Requirements](Technical-Requirements)

## TR-MCP-QBEXEC-001

**QuadBrain internal-tool interception** — QuadBrain internal-tool interception: classify mcp_ as internal, execute all catalog mcp_* tools server-side, strip them from tool_calls, return service results as a normal assistant message when no external tools remain, and emit only non-mcp_ tools to QBAgent.
**Covered by:** FR: FR-MCP-QBEXEC-001; TEST: TEST-MCP-QBEXEC-001
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] RemainingToolCalls contains only non-mcp_ names. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainToolInterceptionTests.cs Interceptor_CatalogName_IsNotEmittedToAgent)
- [x] When RemainingToolCalls is empty and internals succeeded, finish_reason is stop and content is the service result. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainOpenAiChatServiceTests.cs CompleteAsync_CatalogName_StopsWithoutEmittingToolCall)

## TR-MCP-QBEXEC-002

**Internal-tool executor dispatch** — QuadBrainInternalToolExecutor dispatches on toolCall.Function.Name for every mcp_* name in McpHostedAgentToolAdapter and QBAgentDefinition. It deserializes Arguments and calls the matching in-process application/CQRS or transaction-gated service. It maps results to InternalToolExecutionOutcome Ok/Fail/Unhandled. It replaces NoopInternalToolExecutor in DI. It does not HTTP-call MCP endpoints. Unknown catalog-absent mcp_ names return Unhandled.
**Covered by:** FR: FR-MCP-QBEXEC-002; TEST: TEST-MCP-QBEXEC-002
**Status:** completed
Scope: layer-1+
**Acceptance Criteria:**
- [x] The executor switch/table includes every mcp_* name from McpHostedAgentToolAdapter.CreateFunctions and QBAgentDefinition AllowedTools plus BlockedTools. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Catalog_ContainsEveryHostedAgentPublishedName; tests/McpServer.McpAgent.Tests/McpHostedAgentAdapterTests.cs)
- [x] Each handled name has a unit test that asserts a mock in-process service was called and that HttpClient was not used. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Execute_CatalogName_IsHandled AssertMatchingServiceInvokedAsync)
- [x] A catalog-absent mcp_ name returns Unhandled and becomes a note, not RemainingToolCalls. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Execute_UnknownCatalogAbsentMcpName_ReturnsUnhandled; QuadBrainToolInterceptionTests Interceptor_UnhandledInternal_IsNoteNotAgentCommand)
- [x] Those tests do not start, query, or depend on the McpServer Windows service. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs mocks only)

## TR-MCP-QBEXEC-003

**Full-text inter-brain session-log capture** — Brain-slot invocations and AoT reconciliation write full prompt+output text to the session log via ISessionLogService correlated by TurnId, retaining the hashed BrainSlotInvocationEntity audit row; internal-tool executed/failed outcomes are logged; secrets are redacted.
**Covered by:** FR: FR-MCP-QBEXEC-003; TEST: TEST-MCP-QBEXEC-003
**Status:** pending
Scope: layer-1+

