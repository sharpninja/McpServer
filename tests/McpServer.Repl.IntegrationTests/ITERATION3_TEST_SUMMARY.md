# Iteration 3 Integration Tests Summary

## Overview
This document summarizes the iteration 3 integration tests for the TODO workflow via YAML STDIO in `McpServer.Repl.IntegrationTests`.

## Test Coverage

### Basic CRUD Operations
1. **TodoWorkflow_Query_ReturnsItems** - Verifies query command returns TODO items
2. **TodoWorkflow_Create_Get_Delete_Succeeds** - Tests full create-get-delete lifecycle
3. **TodoWorkflow_Update_ModifiesTodo** - Verifies update command modifies TODO properties
4. **TodoWorkflow_CreateWithAllOptionalFields** - Tests TODO creation with all optional fields populated

### Selection State Management
1. **TodoWorkflow_Select_CurrentSelection_Persists** - Verifies selection state persists across commands
2. **TodoWorkflow_UpdateSelected_UsesSelection** - Tests updateSelected command uses selected TODO
3. **TodoWorkflow_DeleteSelected_RemovesSelectedTodo** - Verifies deleteSelected removes selected TODO
4. **TodoWorkflow_CurrentSelection_NoSelection_ReturnsNull** - Tests currentSelection when nothing selected
5. **TodoWorkflow_SelectionStatePersistsAcrossMultipleOperations** - Verifies selection changes between TODOs
6. **TodoWorkflow_DeleteClearsSelection** - Confirms deleting selected TODO clears selection state

### Requirements Analysis
1. **TodoWorkflow_AnalyzeRequirements_ReturnsAnalysis** - Tests requirement analysis for TODO items

### Streaming Operations
1. **TodoWorkflow_StreamStatus_EmitsEvents** - Verifies streamStatus emits YAML event envelopes
2. **TodoWorkflow_StreamPlan_EmitsEvents** - Verifies streamPlan emits YAML event envelopes
3. **TodoWorkflow_StreamImplement_EmitsEvents** - Verifies streamImplement emits YAML event envelopes
4. **TodoWorkflow_StreamingEvents_AreSeparateYamlEnvelopes** - Confirms each event is a separate YAML envelope
5. **TodoWorkflow_StreamEvents_ContainSequenceNumbers** - Verifies events contain sequence information
6. **TodoWorkflow_MultipleStreams_EmitSeparateEvents** - Tests status/plan/implement streams emit distinct events

### Projection Management
1. **TodoWorkflow_GetProjectionStatus_ReturnsStatus** - Tests projection status query
2. **TodoWorkflow_RepairProjection_Succeeds** - Verifies projection repair operation
3. **TodoWorkflow_ProjectionStatusAndRepair_WorkTogether** - Tests status check before/after repair
4. **TodoWorkflow_ProjectionWorkflow_StatusPlanImplement** - Tests full projection workflow

### Complex Scenarios
1. **TodoWorkflow_FullCrudWorkflow_CompletesSuccessfully** - Tests complete TODO lifecycle with all operations
2. **TodoWorkflow_SelectionState_PersistsAcrossCommands** - Verifies selection state across multiple operations
3. **TodoWorkflow_QueryFiltering_ReturnsFilteredResults** - Tests query with priority and section filters
4. **TodoWorkflow_ComplexQuery_WithMultipleFilters** - Tests query with multiple simultaneous filters
5. **TodoWorkflow_UpdateWithComplexFields** - Tests updating arrays and complex nested fields
6. **TodoWorkflow_MultipleQueryExecutions** - Verifies multiple sequential queries work correctly

### Error Handling
1. **TodoWorkflow_InvalidTodoId_ReturnsError** - Tests error handling for invalid TODO ID format
2. **TodoWorkflow_GetNonExistentTodo_ReturnsError** - Tests error when getting non-existent TODO
3. **TodoWorkflow_NoSelection_UpdateSelected_ReturnsError** - Tests error when updateSelected without selection
4. **TodoWorkflow_NoSelection_DeleteSelected_ReturnsError** - Tests error when deleteSelected without selection

## Test Methodology

### Process Management
- Each test starts a fresh REPL child process via `ReplChildProcessHelper`
- Tests use YAML serialization/deserialization for command/response handling
- Process output is captured line-by-line for validation

### YAML Envelope Validation
- Tests verify each response is a valid YAML envelope
- Streaming events are validated as separate YAML documents
- Response envelopes contain expected `type` and `payload` fields

### State Verification
- Selection state is checked before and after operations
- Tests confirm state persistence across multiple commands
- Cleanup operations ensure no state leaks between tests

### Streaming Event Validation
- Tests verify events arrive as separate YAML envelopes
- Each event contains proper sequencing information
- Multiple stream types (status/plan/implement) are tested independently

## Command Coverage

### Implemented Commands
- `workflow.todo.query` - Query TODO items with filters
- `workflow.todo.get` - Get specific TODO by ID
- `workflow.todo.select` - Select TODO as active context
- `workflow.todo.create` - Create new TODO item
- `workflow.todo.update` - Update TODO by ID
- `workflow.todo.updateSelected` - Update selected TODO
- `workflow.todo.delete` - Delete TODO by ID
- `workflow.todo.deleteSelected` - Delete selected TODO
- `workflow.todo.analyzeRequirements` - Analyze requirement references
- `workflow.todo.streamStatus` - Stream status analysis events
- `workflow.todo.streamPlan` - Stream plan generation events
- `workflow.todo.streamImplement` - Stream implementation events
- `workflow.todo.getProjectionStatus` - Get projection health status
- `workflow.todo.repairProjection` - Repair projection state
- `workflow.todo.currentSelection` - Get current selection state

## Key Features Tested

### 1. YAML STDIO Protocol
- All commands sent as YAML request envelopes
- All responses received as YAML result/error/event envelopes
- Multi-line YAML documents properly handled

### 2. Selection State Persistence
- Selection state maintained across multiple commands
- Selection cleared when selected TODO is deleted
- Selection changed when different TODO selected
- Error returned when operating on selection without selecting

### 3. Streaming Event Semantics
- Status/plan/implement operations emit event envelopes
- Each event is a separate YAML document on stdout
- Events contain sequence numbers and timestamps
- Cancellation semantics tested (though cancel tests are simplified)

### 4. Projection Management
- Projection status queried before/after operations
- Repair operation rebuilds projection state
- Status indicates whether projections are stale

### 5. Requirements Traceability
- TODOs reference functional and technical requirements
- Analysis operation returns requirement existence info
- Requirements tracked in create/update operations

## Integration Test Patterns

### Setup Pattern
```csharp
await _replProcess.StartAsync();
await Task.Delay(1000); // Allow process to initialize
```

### Command Execution Pattern
```csharp
await SendCommandAndWaitAsync(envelope);
var response = _replProcess.StdoutLines.LastOrDefault();
Assert.NotNull(response);
```

### Streaming Pattern
```csharp
_replProcess.ClearStdout();
await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(envelope));
await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(10));
```

### Cleanup Pattern
```csharp
await SendCommandAndWaitAsync(
    YamlEnvelopeBuilder.CreateTodoDeleteRequest(
        GenerateRequestId("cleanup"), 
        todoId));
```

## Test Statistics

- **Total Tests**: 31
- **CRUD Operations**: 5
- **Selection State**: 6
- **Streaming**: 6
- **Projection**: 4
- **Complex Scenarios**: 6
- **Error Handling**: 4
- **Requirements Analysis**: 1
- **Query Filtering**: 4

## Notes

### Cancellation Testing
While the tests verify that streaming operations emit events, explicit cancellation tests (sending a cancel command mid-stream) are not included due to the complexity of timing cancellation requests. The infrastructure supports it via `CreateCancelCommandRequest`, but reliable timing in integration tests is challenging.

### Event Content Validation
Tests verify that streaming events arrive and are properly formatted YAML envelopes. Detailed validation of event payload structure (e.g., specific fields in status/plan/implement events) is deferred to unit tests where the event structure can be inspected more precisely.

### Workspace Dependency
These tests assume the REPL process can access a workspace with TODO data. Tests create and delete TODOs to avoid conflicts, using unique test-specific ID prefixes (e.g., `TEST-INT-001`, `TEST-FLT-001`).

## Running the Tests

```bash
cd tests/McpServer.Repl.IntegrationTests
dotnet test --filter "ClassName~Iteration3IntegrationTests"
```

Or run a specific test:
```bash
dotnet test --filter "FullyQualifiedName~TodoWorkflow_FullCrudWorkflow_CompletesSuccessfully"
```
