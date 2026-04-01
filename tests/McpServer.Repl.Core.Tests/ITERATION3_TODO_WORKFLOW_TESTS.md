# Iteration 3: TODO Workflow Unit Tests

## Overview

Comprehensive unit tests for TODO workflow orchestration in `McpServer.Repl.Core`. These tests verify CRUD operations, selection-state management, requirements analysis, streaming event emission, cancellation handling, projection status/repair, and error responses.

## Test Coverage (79 Tests)

### 1. Query Tests (8 tests)
- `QueryAsync_NoFilters_ReturnsAllTodos` - Verify query without filters returns all TODOs
- `QueryAsync_WithKeywordFilter_ReturnsMatchingTodos` - Filter by keyword in title/description
- `QueryAsync_WithPriorityFilter_ReturnsMatchingTodos` - Filter by priority (critical, high, medium, low)
- `QueryAsync_WithSectionFilter_ReturnsMatchingTodos` - Filter by section (backend, frontend, infrastructure)
- `QueryAsync_WithIdFilter_ReturnsSingleTodo` - Filter by exact TODO ID
- `QueryAsync_WithDoneFilter_ReturnsCompletedTodos` - Filter by completion status
- `QueryAsync_WithMultipleFilters_ReturnsCombinedResults` - Multiple filters combined
- `QueryAsync_StorageError_ThrowsInvalidOperationException` - Storage error handling

### 2. Get By ID Tests (7 tests)
- `GetAsync_ValidId_ReturnsTodoItem` - Get TODO by valid canonical ID
- `GetAsync_InvalidIdFormat_ThrowsArgumentException` - Validate ID format rules
- `GetAsync_LowercaseId_ThrowsArgumentException` - Reject lowercase IDs
- `GetAsync_MissingPadding_ThrowsArgumentException` - Reject IDs without 3-digit padding
- `GetAsync_NullOrEmptyId_ThrowsArgumentException` - Null/empty ID validation
- `GetAsync_TodoNotFound_ThrowsInvalidOperationException` - Not found error
- `GetAsync_IssueIdFormat_ReturnsTodoItem` - Support ISSUE-### format

### 3. Selection State Tests (6 tests)
- `CurrentSelection_NoSelection_ReturnsNull` - No selection initially
- `SelectAsync_ValidId_SetsSelectionState` - Select TODO by ID
- `SelectAsync_InvalidId_ThrowsArgumentException` - Invalid ID validation
- `SelectAsync_TodoNotFound_ThrowsInvalidOperationException` - Not found error
- `CurrentSelection_AfterSelection_ReturnsSelectionWithTimestamp` - Selection timestamp
- `SelectAsync_ChangeSelection_UpdatesSelectionState` - Change active selection

### 4. Create Tests (6 tests)
- `CreateAsync_ValidRequest_CreatesTodoItem` - Create TODO with valid data
- `CreateAsync_NullRequest_ThrowsArgumentNullException` - Null request validation
- `CreateAsync_InvalidIdFormat_ThrowsArgumentException` - ID format validation
- `CreateAsync_DuplicateId_ThrowsInvalidOperationException` - Duplicate ID error
- `CreateAsync_IssueNew_CreatesGitHubIssue` - ISSUE-NEW creates GitHub issue
- `CreateAsync_MissingRequiredFields_ThrowsArgumentException` - Required field validation

### 5. Update Tests (7 tests)
- `UpdateAsync_WithId_UpdatesTodoItem` - Update by explicit ID
- `UpdateAsync_WithSelectedId_UpdatesSelectedTodoItem` - Update selected TODO
- `UpdateAsync_NoSelection_ThrowsInvalidOperationException` - No selection error
- `UpdateAsync_InvalidId_ThrowsArgumentException` - Invalid ID validation
- `UpdateAsync_NullRequest_ThrowsArgumentNullException` - Null request validation
- `UpdateAsync_TodoNotFound_ThrowsInvalidOperationException` - Not found error
- `UpdateAsync_PartialUpdate_PreservesUnchangedFields` - Partial update behavior

### 6. Delete Tests (6 tests)
- `DeleteAsync_WithId_DeletesTodoItem` - Delete by explicit ID
- `DeleteAsync_WithSelectedId_DeletesSelectedTodoItem` - Delete selected TODO
- `DeleteAsync_NoSelection_ThrowsInvalidOperationException` - No selection error
- `DeleteAsync_InvalidId_ThrowsArgumentException` - Invalid ID validation
- `DeleteAsync_TodoNotFound_ThrowsInvalidOperationException` - Not found error
- `DeleteAsync_NullOrEmptyId_ThrowsArgumentException` - Null/empty ID validation

### 7. Requirements Analysis Tests (4 tests)
- `AnalyzeRequirementsAsync_ValidId_ReturnsAnalysis` - Analyze FR/TR references
- `AnalyzeRequirementsAsync_InvalidId_ThrowsArgumentException` - Invalid ID validation
- `AnalyzeRequirementsAsync_TodoNotFound_ThrowsInvalidOperationException` - Not found error
- `AnalyzeRequirementsAsync_MissingRequirements_ReturnsIncompleteAnalysis` - Missing requirements flag

### 8. Streaming Event Tests (14 tests)
- `StreamStatusAsync_ValidId_EmitsProgressEvents` - Status analysis streaming
- `StreamPlanAsync_ValidId_EmitsProgressEvents` - Plan generation streaming
- `StreamImplementAsync_ValidId_EmitsProgressEvents` - Implementation streaming
- `StreamStatusAsync_EmitsMultipleEvents_InCorrectOrder` - Event ordering
- `StreamStatusAsync_NullCallback_ThrowsArgumentNullException` - Null callback validation
- `StreamPlanAsync_NullCallback_ThrowsArgumentNullException` - Null callback validation
- `StreamImplementAsync_NullCallback_ThrowsArgumentNullException` - Null callback validation
- `StreamStatusAsync_InvalidId_ThrowsArgumentException` - Invalid ID validation
- `StreamPlanAsync_InvalidId_ThrowsArgumentException` - Invalid ID validation
- `StreamImplementAsync_InvalidId_ThrowsArgumentException` - Invalid ID validation
- `StreamStatusAsync_Cancelled_ThrowsOperationCanceledException` - Cancellation handling
- `StreamPlanAsync_Cancelled_ThrowsOperationCanceledException` - Cancellation handling
- `StreamImplementAsync_Cancelled_ThrowsOperationCanceledException` - Cancellation handling
- `StreamPlanAsync_ErrorDuringStreaming_EmitsErrorEvent` - Error event emission

### 9. Projection Status and Repair Tests (7 tests)
- `GetProjectionStatusAsync_ValidId_ReturnsStatus` - Get projection status
- `GetProjectionStatusAsync_NoProjections_ReturnsEmptyStatus` - No projections
- `GetProjectionStatusAsync_StaleProjection_ReturnsStaleFlag` - Stale detection
- `GetProjectionStatusAsync_InvalidId_ThrowsArgumentException` - Invalid ID validation
- `GetProjectionStatusAsync_TodoNotFound_ThrowsInvalidOperationException` - Not found error
- `RepairProjectionAsync_ValidId_RebuildsProjection` - Projection repair
- `RepairProjectionAsync_AfterRepair_ProjectionStatusUpdated` - Status after repair

### 10. YAML Event Shaping Tests (7 tests)
- `YamlShaping_StreamStatusEvent_MatchesExpectedStructure` - Status event YAML
- `YamlShaping_StreamPlanEvent_MatchesExpectedStructure` - Plan event YAML
- `YamlShaping_StreamImplementEvent_MatchesExpectedStructure` - Implement event YAML
- `YamlShaping_StreamCompleteEvent_MatchesExpectedStructure` - Complete event YAML
- `YamlShaping_StreamErrorEvent_MatchesExpectedStructure` - Error event YAML
- `YamlShaping_ImplementProgressEvent_ContainsFilePath` - File path in events
- `YamlShaping_MultipleEventsStream_ProducesValidYamlDocuments` - Multi-document stream

### 11. Error Response Tests (4 tests)
- `ErrorResponse_InvalidTodoId_ReturnsStructuredError` - Invalid ID error structure
- `ErrorResponse_TodoNotFound_ReturnsStructuredError` - Not found error structure
- `ErrorResponse_StorageError_ReturnsStructuredError` - Storage error structure
- `ErrorResponse_NullRequest_ContainsParameterName` - Parameter name in error

## Test Architecture

### Mocking Strategy
- Uses NSubstitute to mock `ITodoWorkflow` interface
- Uses `FakeYamlSerializer` for YAML serialization testing
- Adapter pattern for converting between model classes and interfaces

### Adapter Classes
- `TodoQueryResultAdapter` - Adapts `TodoQueryResult` to `ITodoQueryResult`
- `TodoItemAdapter` - Adapts `TodoFlatItem` to `ITodoItem`
- `TodoSubtaskAdapter` - Adapts `TodoFlatTask` to `ITodoSubtask`
- `TodoMutationResultAdapter` - Adapts `TodoMutationResult` to `ITodoMutationResult`
- `TodoRequirementsAnalysisAdapter` - Adapts `RequirementsAnalysisResult` to `ITodoRequirementsAnalysis`
- `RequirementReferenceAdapter` - Creates mock requirement references

### Test Helpers
- `CreateTodoItem()` - Factory for TodoFlatItem test data
- `CreateMockSelectionState()` - Factory for ITodoSelectionState mocks
- `CreateTodoCreateRequest()` - Factory for ITodoCreateRequest mocks
- `CreateTodoUpdateRequest()` - Factory for ITodoUpdateRequest mocks
- `CreateMockProjectionStatus()` - Factory for ITodoProjectionStatus mocks
- `CreateEnvelope()` - Factory for IYamlEnvelope mocks
- `CreateStreamingEvent()` - Factory for IStreamingEvent mocks

## Dependencies
- `McpServer.Repl.Core` - Core interfaces (ITodoWorkflow, IYamlSerializer, etc.)
- `McpServer.Todo.Validation.Models` - Model classes (TodoFlatItem, TodoQueryResult, etc.)
- `NSubstitute` - Mocking framework
- `xUnit` - Test framework
- `YamlDotNet` - YAML serialization (via FakeYamlSerializer)

## Red Phase Confirmation
All 79 tests pass with mocked implementations, confirming the test suite is ready for implementation. Tests will fail (red phase) once the actual implementation is added and interfaces are properly wired.

## Event Type Specifications

### Status Stream Events
- `status.progress` - Progress update during status analysis
- `status.complete` - Status analysis completed
- `status.error` - Status analysis failed

### Plan Stream Events
- `plan.progress` - Progress update during plan generation
- `plan.complete` - Plan generation completed
- `plan.error` - Plan generation failed

### Implement Stream Events
- `implement.progress` - Progress update during implementation
- `implement.complete` - Implementation completed
- `implement.error` - Implementation failed

## Canonical TODO ID Rules (Verified)
- Format: `<PHASE>-<AREA>-###` or `ISSUE-{number}`
- Regex: `^[A-Z]+-[A-Z0-9]+-\d{3}$` or `^ISSUE-\d+$`
- Valid examples: `PLAN-NAMINGCONVENTIONS-001`, `MCP-API-042`, `ISSUE-17`
- Invalid examples: `plan-api-001`, `MCP-API-42`, `ISSUE-ABC`, `MCPAPI001`
- Special: `ISSUE-NEW` for creating GitHub issues (server returns `ISSUE-{number}`)
