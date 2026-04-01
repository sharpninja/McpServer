# McpServer.Repl.IntegrationTests

Integration tests for iteration 1, 2, and 3 of the REPL functionality.

## Overview

This test project validates end-to-end workflows using the `mcpserver-repl --agent-stdio` mode, which provides a YAML-based STDIO protocol for AI agent integration.

### Iteration 1 Requirements (Legacy)

1. **Child Process Launch**: Launching `mcpserver-repl --agent-stdio` as a child process
2. **YAML Handshake**: Sending and receiving YAML `hello` envelopes
3. **Trust Bootstrap Flow**: Health checks, nonce validation, signature validation
4. **Auth Key Acceptance**: Validating API keys in `X-Api-Key` headers
5. **Workspace Selection**: Using `X-Workspace-Path` header to select workspaces
6. **YAML Envelope Parsing**: Verifying envelope shapes for all message types

### Iteration 2 Requirements

1. **Session Log Workflow**: Full lifecycle via child-process YAML communication
   - Bootstrap subsystem
   - Open session
   - Begin turn
   - Append dialog items
   - Append actions
   - Update turn metadata
   - Complete turn / Fail turn
   - Query history
2. **State Persistence**: Verify session/turn state persists across commands
3. **Reconnect Scenarios**: Test session continuity across reconnects
4. **Error Handling**: Validate error responses for invalid operations
5. **Identifier Validation**: Test canonical sessionId and requestId format enforcement
6. **Turn Immutability**: Verify completed/failed turns reject further updates

### Iteration 3 Requirements

1. **TODO Workflow**: Full CRUD operations via YAML STDIO
   - Query with filtering (keyword, priority, section, done)
   - Create with all optional fields
   - Get by ID
   - Update by ID or selected TODO
   - Delete by ID or selected TODO
2. **Selection State Management**: 
   - Select TODO as active context
   - Selection persists across commands
   - Selection cleared on delete
   - Error when no selection exists
3. **Streaming Operations**:
   - streamStatus emits separate YAML event envelopes
   - streamPlan emits separate YAML event envelopes
   - streamImplement emits separate YAML event envelopes
   - Events contain sequence numbers and timestamps
4. **Projection Management**:
   - getProjectionStatus returns health info
   - repairProjection rebuilds state
   - Status validated before/after repair
5. **Requirements Analysis**:
   - analyzeRequirements returns FR/TR references
   - Verification of requirement existence

## Test Classes

### Iteration1IntegrationTests
Core integration tests covering child process lifecycle and basic YAML communication.

### TrustBootstrapFlowTests
Tests for the complete trust bootstrap flow including health checks, nonce challenges, and signature validation.

### AuthKeyAndWorkspaceTests
Tests for API key validation and workspace selection via headers and YAML requests.

### YamlEnvelopeShapeTests
Unit-style tests validating YAML serialization/deserialization for all envelope types.

### Iteration2IntegrationTests (32 tests)
Comprehensive integration tests for the Session Log workflow:
- Full workflow from bootstrap through completion
- State persistence verification
- Dialog and action append operations
- Turn lifecycle state transitions
- Error handling for invalid identifiers
- Reconnect scenario validation
- Support for all dialog categories and action types

### Iteration3IntegrationTests (31 tests)
Comprehensive integration tests for the TODO workflow:
- **CRUD Operations** (5 tests): create, get, update, delete, full lifecycle
- **Selection State** (6 tests): select, updateSelected, deleteSelected, persistence, clearing
- **Streaming** (6 tests): status, plan, implement events with validation
- **Projection** (4 tests): status query, repair, workflow integration
- **Complex Scenarios** (6 tests): full workflows, filtering, multi-field updates
- **Error Handling** (4 tests): invalid IDs, non-existent items, no selection
- **Requirements Analysis** (1 test): FR/TR traceability
- **Query Filtering** (4 tests): multiple filter combinations

## Running the Tests

```powershell
# Run all integration tests
dotnet test tests/McpServer.Repl.IntegrationTests

# Run iteration 2 session log tests
dotnet test --filter "ClassName~Iteration2IntegrationTests"

# Run iteration 3 TODO workflow tests
dotnet test --filter "ClassName~Iteration3IntegrationTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~TodoWorkflow_FullCrudWorkflow_CompletesSuccessfully"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Prerequisites

- The `mcpserver-repl` host project must be buildable via `dotnet run`
- Tests create/delete TODOs with test-specific ID prefixes
- Each test starts a fresh REPL process for isolation

## Test Architecture

### ReplChildProcessHelper

Core helper class that manages REPL child processes:
- Launches `mcpserver-repl --agent-stdio` as a child process
- Captures stdout/stderr lines in real-time
- Provides async methods to send YAML commands and wait for responses
- Supports waiting for line counts, specific text, or patterns
- Allows clearing captured output for test isolation
- Handles graceful shutdown and cleanup

Key methods:
```csharp
Task StartAsync(CancellationToken)
Task WriteLineAsync(string yamlContent, CancellationToken)
Task<bool> WaitForStdoutLineCountAsync(int count, TimeSpan timeout, CancellationToken)
Task<bool> WaitForStdoutContainsAsync(string expectedText, TimeSpan timeout, CancellationToken)
Task<bool> WaitForStdoutPatternAsync(string pattern, TimeSpan timeout, CancellationToken)
void ClearStdout()
void ClearStderr()
Task StopAsync(CancellationToken)
```

### YamlEnvelopeBuilder

Fluent builder for YAML command envelopes supporting all REPL protocol commands:

#### Session Log Commands
- `CreateSessionLogBootstrapRequest` - Initialize session log projection
- `CreateSessionLogOpenSessionRequest` - Open new session
- `CreateSessionLogCurrentSessionRequest` - Get current session state
- `CreateSessionLogBeginTurnRequest` - Start new turn
- `CreateSessionLogUpdateTurnRequest` - Update turn metadata
- `CreateSessionLogCompleteTurnRequest` - Complete turn successfully
- `CreateSessionLogFailTurnRequest` - Mark turn as failed
- `CreateSessionLogAppendDialogRequest` - Add dialog items
- `CreateSessionLogAppendActionsRequest` - Add action items
- `CreateSessionLogQueryHistoryRequest` - Query session history

#### TODO Workflow Commands
- `CreateTodoQueryRequest` - Query TODO items with filters
- `CreateTodoGetRequest` - Get specific TODO by ID
- `CreateTodoSelectRequest` - Select TODO as active context
- `CreateTodoCreateRequest` - Create new TODO item
- `CreateTodoUpdateRequest` - Update TODO by ID
- `CreateTodoUpdateSelectedRequest` - Update currently selected TODO
- `CreateTodoDeleteRequest` - Delete TODO by ID
- `CreateTodoDeleteSelectedRequest` - Delete currently selected TODO
- `CreateTodoAnalyzeRequirementsRequest` - Analyze requirement references
- `CreateTodoStreamStatusRequest` - Stream status analysis events
- `CreateTodoStreamPlanRequest` - Stream plan generation events
- `CreateTodoStreamImplementRequest` - Stream implementation events
- `CreateTodoGetProjectionStatusRequest` - Get projection health status
- `CreateTodoRepairProjectionRequest` - Repair projection state
- `CreateTodoCurrentSelectionRequest` - Get current selection state

#### Helper Methods
- `CreateDialogItem` - Create dialog item for session log
- `CreateAction` - Create action item for session log
- `CreateTodoSubtask` - Create subtask for TODO items
- `CreateCancelCommandRequest` - Create cancellation command

## Test Patterns

### Standard Test Pattern
```csharp
[Fact]
public async Task MyTest()
{
    await _replProcess.StartAsync();
    await Task.Delay(1000); // Allow process initialization

    var envelope = YamlEnvelopeBuilder.CreateTodoQueryRequest(
        GenerateRequestId("query"),
        priority: "high");
    
    await SendCommandAndWaitAsync(envelope);

    var response = _replProcess.StdoutLines.LastOrDefault();
    Assert.NotNull(response);
    
    var result = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response);
    Assert.NotNull(result);
}
```

### Streaming Test Pattern
```csharp
[Fact]
public async Task StreamingTest()
{
    await _replProcess.StartAsync();
    await Task.Delay(1000);

    _replProcess.ClearStdout();

    var streamEnvelope = YamlEnvelopeBuilder.CreateTodoStreamStatusRequest(
        GenerateRequestId("stream"),
        "TEST-ID-001");
    
    await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(streamEnvelope));
    await _replProcess.WaitForStdoutLineCountAsync(3, TimeSpan.FromSeconds(10));

    var eventLines = _replProcess.StdoutLines.ToList();
    foreach (var line in eventLines)
    {
        var envelope = _yamlDeserializer.Deserialize<Dictionary<string, object>>(line);
        Assert.NotNull(envelope);
        Assert.True(envelope.ContainsKey("type"));
    }
}
```

### Selection State Test Pattern
```csharp
[Fact]
public async Task SelectionStateTest()
{
    await _replProcess.StartAsync();
    await Task.Delay(1000);

    await SendCommandAndWaitAsync(
        YamlEnvelopeBuilder.CreateTodoCreateRequest(
            GenerateRequestId("create"),
            "TEST-ID-001",
            "Test TODO",
            "Testing",
            "high"));

    await SendCommandAndWaitAsync(
        YamlEnvelopeBuilder.CreateTodoSelectRequest(
            GenerateRequestId("select"),
            "TEST-ID-001"));

    await SendCommandAndWaitAsync(
        YamlEnvelopeBuilder.CreateTodoCurrentSelectionRequest(
            GenerateRequestId("check")));

    var selectionResponse = _replProcess.StdoutLines.LastOrDefault();
    Assert.NotNull(selectionResponse);
}
```

## Best Practices

1. **Test Isolation**: Each test starts a fresh REPL process
2. **Unique IDs**: Use unique TODO IDs with test-specific prefixes (e.g., `TEST-INT-001`)
3. **Cleanup**: Delete created TODOs to avoid conflicts
4. **Timing**: Allow ~1s for process startup, use appropriate timeouts for streaming
5. **Output Clearing**: Use `ClearStdout()` before streaming tests
6. **Error Handling**: Wrap YAML deserialization in try-catch when validating events
7. **Assertions**: Always assert responses are not null before deserializing

## Debugging Tests

### View Process Output
```csharp
var allLines = _replProcess.StdoutLines;
foreach (var line in allLines)
{
    Console.WriteLine(line);
}

var errors = _replProcess.StderrLines;
foreach (var error in errors)
{
    Console.WriteLine($"ERROR: {error}");
}
```

### Increase Timeouts
```csharp
// Debugging: use longer timeout
await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(30));
```

## Documentation

- See [ITERATION2_TEST_SUMMARY.md](ITERATION2_TEST_SUMMARY.md) for detailed iteration 2 test coverage
- See [ITERATION3_TEST_SUMMARY.md](ITERATION3_TEST_SUMMARY.md) for detailed iteration 3 test coverage
- See [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) for implementation notes

## Known Limitations

1. **Cancellation**: Explicit mid-stream cancellation tests are not included due to timing complexity
2. **Event Payload**: Detailed event payload validation deferred to unit tests
3. **Workspace**: Tests assume a working TODO workspace is available
4. **Timing**: Some tests may fail on slow systems due to fixed delays

## Notes

- Tests that require a running server handle `HttpRequestException` gracefully
- Child process tests verify process state and YAML communication patterns
- Envelope shape tests validate serialization correctness without requiring a running server
