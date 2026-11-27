# FR-MCP-QBEXEC

[Back to Functional Requirements](Functional-Requirements)

## FR-MCP-QBEXEC-001 QuadBrain server-side MCP-tool execution with AoT transaction interception

QuadBrain SHALL execute every MCP-internal tool (every tool whose name starts with mcp_ that QBrainAi exposes: session, TODO including query/get/plan/status/implementation, requirements including list/get/create/update, repo including read/list/write/edit, GraphRAG, desktop, PowerShell session, client invoke, and mutating git) directly server-side during orchestration through existing in-process application/CQRS services. QuadBrain SHALL NOT emit mcp_ tools to QBAgent as OpenAI tool_calls. Successful internal execution SHALL return a normal assistant message (finish_reason stop) carrying the service result. Tools OUTSIDE QBrainAi (non-mcp_ names) SHALL be emitted as OpenAI tool_calls for QBAgent. Internal execution SHALL NOT call HTTP endpoints. Mutating TODO and Requirements tools SHALL still gate through the turn transaction coordinator. QuadBrain orchestration SHALL perform session logging.
Scope: layer-1+
**Acceptance Criteria:**
- [x] Tools are classified MCP-internal (mcp_ prefix) versus external. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainToolInterceptionTests.cs)
- [x] The interceptor executes handled internal tools server-side and strips them; only non-mcp_ external calls are emitted to the agent as tool commands. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainToolInterceptionTests.cs)
- [x] Internal tool failures and unhandled internal tools are surfaced to the agent as a note and earmarked as Session Log failures, never as tool commands. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainOpenAiChatServiceTests.cs)
- [x] A concrete executor routes TODO and Requirements mutations through the transaction-gated services so the AoT commit applies them server-side. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs)
- [x] QuadBrain orchestration performs the Session Log write for the turn including internal-tool failure entries. (evidence: tests/McpServer.Support.Mcp.Tests/Services/BrainInteractionSessionLoggerTests.cs; tests/McpServer.Support.Mcp.Tests/Services/QuadBrainOpenAiChatServiceTests.cs)
- [x] Every mcp_* tool name published by McpHostedAgentToolAdapter and QBAgentDefinition (AllowedTools plus BlockedTools) is executed server-side when elected. None of those names appear in the OpenAI tool_calls array returned to QBAgent. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainOpenAiChatServiceTests.cs CompleteAsync_CatalogName_StopsWithoutEmittingToolCall; docs/receipts/hostile-validator-20260910T233311Z.md)
- [x] When every elected tool is mcp_* and execution succeeds, the OpenAI response is finish_reason stop with assistant content equal to the in-process service result (not an empty message and not a tool_call). (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainOpenAiChatServiceTests.cs CompleteAsync_CatalogName_StopsWithoutEmittingToolCall)
- [x] Internal tool execution dispatches in-process application/CQRS services (ITodoService, IRepoFileService, session-log, requirements, GraphRAG, desktop, PowerShell session, and so on). It does not HTTP-call /mcpserver/*, /v1/*, or /mcp-transport, and tests proving this do not depend on the Windows service. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Executor_DoesNotDependOnHttpClient)

## FR-MCP-QBEXEC-002 Concrete QuadBrain internal-tool executor

QuadBrain SHALL provide a concrete IQuadBrainInternalToolExecutor (replacing NoopInternalToolExecutor in DI) that dispatches every mcp_* tool name QuadBrain may elect. Reads and queries SHALL call the same in-process application/CQRS services controllers use (ITodoService, IRepoFileService, session-log, requirements, GraphRAG). Mutations SHALL call the transaction-gated services (ITransactionGatedTodoMutationService, the transaction-gated requirements document service, TransactionGatedRepoFileService, mutating mcp_git) so each mutation commits through the turn transaction coordinator. Results map to InternalToolExecutionOutcome.Ok/Fail. An mcp_ name that is not in the hosted MCP tool catalog returns Unhandled and is surfaced as a note, never as a QBAgent tool command. The executor SHALL NOT invoke HTTP endpoints.
Scope: layer-1+
**Acceptance Criteria:**
- [x] mcp_todo_query, mcp_todo_get, mcp_todo_plan, mcp_todo_status, mcp_todo_implementation, mcp_todo_create, mcp_todo_update, and mcp_todo_delete are handled. Mutations go through ITransactionGatedTodoMutationService; reads/queries go through ITodoService. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Execute_CatalogName_IsHandled)
- [x] Every mcp_requirements_* name (list/get/create/update for FR, TR, and TEST) is handled through the requirements application/CQRS services. Mutations remain transaction-gated. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Execute_CatalogName_IsHandled)
- [x] mcp_repo_read, mcp_repo_list, mcp_repo_write, and mcp_repo_edit are handled through IRepoFileService. Writes/edits remain transaction-gated with snapshot rollback on reject. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Execute_CatalogName_IsHandled)
- [x] An mcp_ name that is not in the hosted MCP tool catalog returns InternalToolExecutionOutcome.Unhandled and is surfaced as a note, never as a tool command. The executor replaces NoopInternalToolExecutor in DI. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Execute_UnknownCatalogAbsentMcpName_ReturnsUnhandled; src/McpServer.Support.Mcp/Program.cs)
- [x] mcp_session_bootstrap, mcp_session_update, mcp_session_turn_begin, mcp_session_turn_update, mcp_session_turn_complete, and mcp_session_query_history are handled through the session-log application services. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Execute_CatalogName_IsHandled)
- [x] Every mcp_graphrag_* name from McpHostedAgentToolAdapter is handled through GraphRAG application services. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Execute_CatalogName_IsHandled)
- [x] mcp_desktop_launch, mcp_powershell_session_create, mcp_powershell_session_command, mcp_powershell_session_close, mcp_client_invoke, and mutating mcp_git are handled through their existing in-process services. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs; IQuadBrainPowerShellSessions)
- [x] No handled route uses HttpClient or REST against /mcpserver/*, /v1/*, or /mcp-transport. Unit and integration tests proving these routes use mocks or WebApplicationFactory and do not require the Windows service. (evidence: tests/McpServer.Support.Mcp.Tests/Services/QuadBrainInternalToolExecutorTests.cs Executor_DoesNotDependOnHttpClient)

## FR-MCP-QBEXEC-003 Full-fidelity inter-brain session logging

QuadBrain SHALL log all interaction between the brains in full. Every brain-slot invocation (LeftHemisphere, RightHemisphere, CuriosityEngine, ArbiterOfTruth) and the AoT reconciliation SHALL write its full prompt and full output text to the session log, correlated by the turn's TurnId, in addition to the existing durable hashed audit row (BrainSlotInvocationEntity). Internal-tool execution outcomes and internal-tool failure notes SHALL also be recorded to the session log. Secrets SHALL be redacted. This is a primary reason the model runs inside the MCP Server.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Each brain-slot invocation writes full prompt + full output text to the session log (not only SHA-256 hashes), under the correct TurnId.
- [ ] AoT reconciliation input and output are logged in full to the session log.
- [ ] Internal-tool executed and failed outcomes (interception Executed/Failed) are recorded to the session log as the turn proceeds.
- [ ] Secret values (api keys, bearer tokens) are redacted before logging; the existing hashed BrainSlotInvocationEntity audit row is retained as the durable index.

