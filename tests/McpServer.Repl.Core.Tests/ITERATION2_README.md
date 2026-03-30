# Iteration 2: Session Log Workflow Tests - README

## Overview

This directory contains comprehensive mock-passing tests for the Session Log workflow subsystem. The tests validate workflow orchestration, turn lifecycle management, canonical identifier handling, and integration with the SessionLogClient.

## Test Philosophy

These tests follow the **Test-Driven Development (TDD)** approach:

1. **Red Phase** - Tests written first against interfaces (current phase)
2. **Green Phase** - Implementation created to make tests pass (iteration 3)
3. **Refactor Phase** - Code cleaned up while maintaining passing tests

All tests use **mocks and stubs** to:
- Execute fast (no I/O, no HTTP)
- Run deterministically (no flakiness)
- Test in isolation (no external dependencies)
- Cover edge cases easily

## Test Files

### Core Test Files

| File | Tests | Purpose |
|------|-------|---------|
| **SessionLogWorkflowTests.cs** | 46 | Original workflow tests with FakeSessionLogState |
| **SessionLogWorkflowMockValidationTests.cs** | 31 | Stub client validation and workflow routing |
| **SessionLogWorkflowIntegration2Tests.cs** | 32 | Complete end-to-end integration scenarios |
| **Iteration2_AllTestsValidation.cs** | 16 | Final validation of all components |
| **Iteration1_IntegrationTests.cs** | 10+ | Trust bootstrap and auth rotation tests |
| **FakeYamlSerializerTests.cs** | 15+ | YAML serialization validation |
| **Total** | **130+** | Comprehensive test coverage |

### Test Utilities

| Component | Location | Purpose |
|-----------|----------|---------|
| **FakeSessionLogState** | SessionLogWorkflowTests.cs (lines 1051-1147) | In-memory ISessionLogState implementation |
| **StubSessionLogClient** | Embedded in test files | Stub SessionLogClient responses |
| **FakeYamlSerializer** | FakeYamlSerializerTests.cs (lines 244-342) | YAML serialization for tests |

## Test Categories

### 1. Workflow Command Routing (7 tests)
Tests that workflow methods correctly route to SessionLogClient methods:
- OpenSession → SubmitAsync
- BeginTurn → State tracking
- UpdateTurn → State tracking
- CompleteTurn → State tracking
- FailTurn → State tracking
- AppendDialog → AppendDialogAsync
- QueryHistory → QueryAsync

**File:** SessionLogWorkflowMockValidationTests.cs

### 2. Turn Lifecycle Guards (12 tests)
Tests that enforce turn lifecycle rules:
- No duplicate turns
- No concurrent turns
- Proper status transitions
- Immutability of completed/failed turns
- Session required before turns
- Active turn required for modifications

**Files:** 
- SessionLogWorkflowTests.cs (FakeSessionLogState validation)
- SessionLogWorkflowMockValidationTests.cs (Guard tests)
- SessionLogWorkflowIntegration2Tests.cs (State transition validation)

### 3. Canonical Identifiers (8 tests)
Tests identifier format validation:
- Session ID: `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`
- Request ID: `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`
- Valid format acceptance
- Invalid format rejection

**File:** SessionLogWorkflowTests.cs, SessionLogWorkflowMockValidationTests.cs

### 4. Session State Management (10 tests)
Tests session metadata tracking:
- Session properties
- Active turn tracking
- Turn count increments
- Timestamp updates
- Status transitions

**Files:** SessionLogWorkflowTests.cs, SessionLogWorkflowMockValidationTests.cs, SessionLogWorkflowIntegration2Tests.cs

### 5. Error Handling (15 tests)
Tests proper exception handling:
- No session errors
- No active turn errors
- Duplicate errors
- Immutability errors
- Invalid parameter errors

**Files:** SessionLogWorkflowTests.cs, SessionLogWorkflowIntegration2Tests.cs

### 6. Stub Client Response Validation (9 tests)
Tests that stub client returns proper DTOs:
- SubmitAsync response structure
- QueryAsync response structure
- AppendDialogAsync response structure
- Parameter handling

**Files:** SessionLogWorkflowMockValidationTests.cs, SessionLogWorkflowIntegration2Tests.cs

### 7. Complete Workflow Integration (10 tests)
Tests end-to-end scenarios:
- Open session → begin turn → complete
- Open session → begin turn → fail
- Multiple turns in single session
- Append dialog during turn
- Query history after completion

**File:** SessionLogWorkflowIntegration2Tests.cs

### 8. Interface and Contract Validation (10 tests)
Tests that all interfaces and constants are properly defined:
- ISessionLogWorkflow methods
- ISessionLogState properties
- IDialogItem interface
- ISessionAction interface
- SessionLogErrorCodes constants
- SessionLogCommandShapes constants

**File:** Iteration2_AllTestsValidation.cs

## Running Tests

### Run All Tests
```powershell
dotnet test tests/McpServer.Repl.Core.Tests
```

### Run Specific Test File
```powershell
dotnet test --filter "FullyQualifiedName~SessionLogWorkflowTests"
dotnet test --filter "FullyQualifiedName~SessionLogWorkflowMockValidationTests"
dotnet test --filter "FullyQualifiedName~SessionLogWorkflowIntegration2Tests"
```

### Run Specific Test Category
```powershell
# Run all workflow routing tests
dotnet test --filter "Name~WorkflowRouting"

# Run all turn lifecycle guard tests
dotnet test --filter "Name~TurnLifecycleGuard"

# Run all error handling tests
dotnet test --filter "Name~ErrorHandling"
```

### Run Single Test
```powershell
dotnet test --filter "Name=CompleteWorkflow_OpenSessionBeginTurnComplete_Success"
```

## Mock Components

### FakeSessionLogState

**Purpose:** In-memory implementation of ISessionLogState for testing turn lifecycle.

**Usage:**
```csharp
var fakeState = new FakeSessionLogState();

// Open session
fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

// Begin turn
fakeState.BeginTurn("req-20260304T113901Z-task-001");

// Update turn
fakeState.UpdateTurn();

// Complete turn
fakeState.CompleteTurn();

// Check state
Assert.Equal(1, fakeState.TurnCount);
```

**Validation Rules:**
- ✅ Session must exist before turn operations
- ✅ No duplicate turn request IDs
- ✅ No concurrent turns
- ✅ Proper status transitions
- ✅ Completed/failed turns are immutable

### StubSessionLogClient

**Purpose:** Stub responses for SessionLogClient without HTTP calls.

**Usage:**
```csharp
var stubClient = new StubSessionLogClient();

// Submit session log
var result = await stubClient.SubmitAsync(sessionLog);

// Query history
var queryResult = await stubClient.QueryAsync(agent: "Copilot");

// Append dialog
var dialogResult = await stubClient.AppendDialogAsync(
    "Copilot", "session-1", "req-1", dialogItems);
```

**Features:**
- Returns proper DTO structures
- Tracks internal state
- Validates parameters
- Fast, deterministic

### FakeYamlSerializer

**Purpose:** Test implementation of IYamlSerializer for YAML validation.

**Usage:**
```csharp
var serializer = new FakeYamlSerializer();

// Serialize
var yaml = serializer.Serialize(envelope);

// Deserialize
var envelope = serializer.Deserialize(yaml);

// Try deserialize
var success = serializer.TryDeserialize(yaml, out var envelope);
```

## Common Test Patterns

### Pattern 1: Complete Workflow Test
```csharp
[Fact]
public async Task CompleteWorkflow_Test()
{
    // Arrange
    var stubClient = new StubSessionLogClient();
    var fakeState = new FakeSessionLogState();
    
    // Act - Open session
    await stubClient.SubmitAsync(sessionLog);
    fakeState.OpenSession("Copilot", "session-id", "Title", "model");
    
    // Act - Begin turn
    fakeState.BeginTurn("req-id");
    
    // Act - Update and complete
    fakeState.UpdateTurn();
    fakeState.CompleteTurn();
    
    // Assert
    Assert.Equal(1, fakeState.TurnCount);
    Assert.Null(fakeState.CurrentTurnRequestId);
}
```

### Pattern 2: Error Validation Test
```csharp
[Fact]
public void ErrorHandling_Test()
{
    // Arrange
    var fakeState = new FakeSessionLogState();
    fakeState.OpenSession("Copilot", "session-id", "Title", "model");
    fakeState.BeginTurn("req-id");
    fakeState.CompleteTurn();
    
    // Act & Assert
    var exception = Assert.Throws<InvalidOperationException>(
        () => fakeState.UpdateTurn());
    
    Assert.Contains("No active turn", exception.Message);
}
```

### Pattern 3: Stub Client Validation Test
```csharp
[Fact]
public async Task StubClient_Test()
{
    // Arrange
    var stubClient = new StubSessionLogClient();
    
    // Act
    var result = await stubClient.SubmitAsync(sessionLog);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("session-id", result.SessionId);
}
```

## Test Data Patterns

### Valid Session IDs
- `Copilot-20260304T113901Z-feature`
- `Cline-20260304T120000Z-bugfix-auth`
- `Cursor-20260304T150000Z-refactor-session`
- `MyAgent-20260304T113901Z-test`

### Valid Request IDs
- `req-20260304T113901Z-task-001`
- `req-20260304T120000Z-feature-add`
- `req-20260304T150000Z-bugfix`
- `req-20260304T113901Z-multi-word-task`

### Invalid Session IDs
- `copilot-20260304T113901Z-feature` (lowercase prefix)
- `Copilot-20260304-feature` (missing time)
- `Copilot-20260304T113901Z` (missing suffix)
- `req-20260304T113901Z-feature` (wrong prefix)

### Invalid Request IDs
- `request-20260304T113901Z-task` (wrong prefix)
- `req-20260304-task` (missing time)
- `req-invalid-timestamp-task` (invalid timestamp)
- `req-20260304T113901Z` (missing suffix)

## Debugging Tests

### View Test Output
```powershell
dotnet test --logger "console;verbosity=detailed"
```

### Debug Single Test
1. Open test file in VS Code
2. Set breakpoint in test method
3. Click "Debug Test" in CodeLens
4. Step through test execution

### Common Issues

**Issue:** Tests fail with NullReferenceException
- **Fix:** Ensure mock objects are properly initialized

**Issue:** Tests fail with "No session is active"
- **Fix:** Call `OpenSession()` before turn operations

**Issue:** Tests fail with "Turn already exists"
- **Fix:** Use unique request IDs for each turn

**Issue:** Tests fail with "No active turn"
- **Fix:** Call `BeginTurn()` before update/complete/fail operations

## Documentation

- **ITERATION2_FINAL_SUMMARY.md** - Comprehensive summary of iteration 2 completion
- **ITERATION2_MOCK_IMPLEMENTATION_COMPLETE.md** - Detailed implementation notes
- **ITERATION2_TEST_SUMMARY.md** - Original test plan and coverage
- **ITERATION2_README.md** - This file

## Next Steps

### For Test Development (Current Phase)
- ✅ All iteration 2 tests implemented
- ✅ Mock infrastructure complete
- ✅ Validation tests passing

### For Implementation (Iteration 3)
- ⏳ Implement real SessionLogWorkflow
- ⏳ Integrate with SessionLogClient
- ⏳ Add persistent session state
- ⏳ Run tests against real implementation
- ⏳ Fix any failing tests

### For Integration (Iteration 4)
- ⏳ Test with real MCP server
- ⏳ Test with real file system
- ⏳ Performance testing
- ⏳ End-to-end validation

## Contact

For questions or issues with tests:
1. Check this README first
2. Review test summaries in ITERATION2_FINAL_SUMMARY.md
3. Review interface documentation in src/McpServer.Repl.Core/
4. Check test implementation in respective test files

## License

Part of the MCP Server REPL project. See root LICENSE file.
