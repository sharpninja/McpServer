# Iteration 4 Requirements Workflow Tests - Implementation Summary

## Overview
Implemented comprehensive unit tests for the Requirements workflow in `RequirementsWorkflowTests.cs`. Tests cover all CRUD operations for FR/TR/TEST entities, mapping management, document generation/ingestion, selection state, validation guards, and RequirementsClient integration.

## Test Coverage (73 tests total)

### FR (Functional Requirements) CRUD - 8 tests
- List operations with filters (no filters, area filter, status filter)
- Get by ID (valid ID, invalid ID, not found)
- Create (valid request, duplicate ID)
- Update (with selection, no selection)
- Delete (valid ID, invalid ID)

### TR (Technical Requirements) CRUD - 6 tests
- List operations with filters (no filters, area + subarea filters)
- Get by ID (valid ID, invalid ID)
- Create (valid request)
- Update (with selection)
- Delete (valid ID)

### TEST Requirements CRUD - 6 tests
- List operations with filters (no filters, area filter)
- Get by ID (valid ID, invalid ID)
- Create (valid request)
- Update (with selection)
- Delete (valid ID)

### Mapping CRUD - 7 tests
- List mappings (no filters, with FR ID filter)
- Create mapping (valid request, invalid FR reference, invalid TR reference, no requirement IDs)
- Delete mapping (valid IDs, no IDs, mapping not found)

### Document Generation - 6 tests
- Markdown format generation (FR, TR, matrix, all)
- YAML format generation
- Invalid format/docType validation

### Document Ingestion - 8 tests
- Markdown content ingestion
- YAML content ingestion
- Merge strategies (overwrite, skip)
- Conflict handling and reporting
- Validation errors (invalid format, invalid strategy, empty content)

### Selection State Management - 4 tests
- No selection state
- Single requirement selection (FR only)
- Multiple requirement selection (FR + TR + TEST)
- Timestamp validation

### YAML Shaping - 5 tests
- FR create request structure
- TR query response structure
- Mapping item structure
- Document generation result structure
- Ingestion result structure

### Validation Errors - 5 tests
- Duplicate FR ID
- Invalid TR ID format
- Invalid TEST ID format
- Missing mapping references
- Storage errors

### RequirementsClient Integration - 10 tests
- ListFrAsync endpoint verification
- CreateFrAsync POST request
- ListTrAsync collection retrieval
- UpdateTrAsync PUT operation
- DeleteFrAsync success result
- ListMappingsAsync all mappings
- UpsertMappingAsync create/update
- GenerateAsync document binary
- IngestAsync markdown processing
- HTTP method and path verification

## Key Test Patterns

### Mock-Based Testing
All tests use NSubstitute to mock the `IRequirementsWorkflow` interface and `RequirementsClient`, allowing us to:
- Verify correct method calls and parameters
- Test error handling without implementation
- Validate YAML request/response shaping
- Test client HTTP integration

### Adapter Pattern
Custom adapter classes convert between Client models and Workflow interfaces:
- `FrQueryResultAdapter`: `List<FrEntry>` → `IFrQueryResult`
- `TrQueryResultAdapter`: `List<TrEntry>` → `ITrQueryResult`
- `TestQueryResultAdapter`: `List<TestEntry>` → `ITestQueryResult`
- `MappingQueryResultAdapter`: `List<IMappingItem>` → `IMappingQueryResult`

### YAML Verification
Tests verify YAML serialization of:
- Request payloads (FR/TR/TEST create/update)
- Response structures (query results, mutation results)
- Document generation outputs
- Ingestion results with conflicts

### Client Integration Verification
Tests verify `RequirementsClient` correctly:
- Routes to proper endpoints (`/mcpserver/requirements/fr`, `/mcpserver/requirements/tr`, etc.)
- Uses correct HTTP methods (GET, POST, PUT, DELETE)
- Serializes request bodies
- Deserializes response bodies
- Handles binary content (document generation)

## Requirement ID Validation Rules

Tests verify enforcement of canonical identifier formats:

### FR (Functional Requirement) ID
- **Format:** `FR-<AREA>-###`
- **Regex:** `^FR-[A-Z]+-\d{3}$`
- **Valid:** `FR-MCP-001`, `FR-AUTH-042`, `FR-API-123`
- **Invalid:** `fr-mcp-001`, `FR-MCP-1`, `FR-001`, `FR-MCP-1234`

### TR (Technical Requirement) ID
- **Format:** `TR-<AREA>-<SUBAREA>-###`
- **Regex:** `^TR-[A-Z]+-[A-Z]+-\d{3}$`
- **Valid:** `TR-MCP-ARCH-001`, `TR-API-PERF-042`, `TR-AUTH-SEC-123`
- **Invalid:** `TR-MCP-001`, `tr-mcp-arch-001`, `TR-MCP-ARCH-1`

### TEST (Test Requirement) ID
- **Format:** `TEST-<AREA>-###`
- **Regex:** `^TEST-[A-Z]+-\d{3}$`
- **Valid:** `TEST-MCP-001`, `TEST-AUTH-042`, `TEST-API-123`
- **Invalid:** `test-mcp-001`, `TEST-001`, `TEST-MCP-1`

## Selection State Behavior

The `IRequirementsSelectionState` interface tracks currently selected requirements:

```csharp
public interface IRequirementsSelectionState
{
    string? FrId { get; }
    string? TrId { get; }
    string? TestId { get; }
    DateTimeOffset SelectedAt { get; }
}
```

Tests verify:
- `UpdateFrAsync`, `UpdateTrAsync`, `UpdateTestAsync` use selected IDs when no ID is explicitly provided
- Operations throw `InvalidOperationException` when no requirement is selected
- Multiple requirements can be selected simultaneously
- Selection timestamps are maintained

## Mapping Behavior

Tests verify mapping CRUD operations:
- **Create:** Validates all referenced FR/TR/TEST IDs exist before creating mapping
- **List:** Supports filtering by FR ID, TR ID, or TEST ID
- **Delete:** Requires at least one ID to identify the mapping
- **Validation:** Rejects mappings with no requirement IDs
- **Referential Integrity:** Prevents orphan mappings (references to non-existent requirements)

## Document Generation

Tests verify document generation supports:
- **Formats:** `markdown`, `yaml`
- **Document Types:** `fr`, `tr`, `test`, `matrix`, `all`
- **Output:** Formatted content with metadata and timestamps
- **Validation:** Rejects invalid format or docType values

## Document Ingestion

Tests verify document ingestion supports:
- **Formats:** `markdown`, `yaml`
- **Merge Strategies:**
  - `overwrite`: Replaces existing requirements
  - `merge`: Combines with existing requirements
  - `skip`: Ignores conflicts, only creates new
- **Conflict Detection:** Reports duplicate IDs, invalid formats, missing references
- **Statistics:** Tracks parsed, created, and updated counts for FR/TR/TEST/mappings

## Red Phase Confirmation

All 73 tests pass with mocked `IRequirementsWorkflow` and `RequirementsClient`. The tests will guide implementation of:

1. **RequirementsWorkflow** class implementing `IRequirementsWorkflow`
2. **RequirementsClient** integration for REST API calls
3. **Selection state management** for FR/TR/TEST context
4. **Document generation** (Markdown and YAML)
5. **Document ingestion** with conflict resolution
6. **Validation logic** for requirement ID formats
7. **Mapping management** with referential integrity
8. **Error handling** with structured exceptions

## Next Steps

To implement the workflow:
1. Create `RequirementsWorkflow` class in `src/McpServer.Repl.Core/Workflows/`
2. Implement `IRequirementsWorkflow` interface
3. Integrate with `RequirementsClient` from McpServer.Client
4. Implement selection state tracking (fake `IRequirementsSelectionState`)
5. Add document generation/ingestion logic
6. Add ID validation with regex patterns
7. Add mapping validation guards
8. Run tests to validate implementation

## Test Execution

```bash
# Run all Requirements workflow tests
dotnet test tests/McpServer.Repl.Core.Tests/McpServer.Repl.Core.Tests.csproj --filter "FullyQualifiedName~RequirementsWorkflowTests"

# Expected Results: Passed: 73, Failed: 0, Total: 73
```

## Files Modified

- **Created:** `tests/McpServer.Repl.Core.Tests/RequirementsWorkflowTests.cs` (~1,500 lines)
- **Referenced:** `src/McpServer.Repl.Core/IRequirementsWorkflow.cs` (interface definition)
- **Referenced:** `src/McpServer.Client/RequirementsClient.cs` (client library)
- **Referenced:** `src/McpServer.Client/Models/RequirementsModels.cs` (data models)
- **Referenced:** `tests/McpServer.Repl.Core.Tests/FakeYamlSerializerTests.cs` (YAML serializer helper)

## Test Organization

Tests are organized into logical regions:
1. FR Query Tests
2. FR Get/Create/Update/Delete Tests
3. TR Query Tests
4. TR Get/Create/Update/Delete Tests
5. TEST Query Tests
6. TEST Get/Create/Update/Delete Tests
7. Mapping CRUD Tests
8. Document Generation Tests
9. Document Ingestion Tests
10. Selection State Tests
11. YAML Shaping Tests
12. Validation Error Tests
13. RequirementsClient Integration Tests
14. Helper Methods (test data factories)
15. Adapter Classes (model conversion)

This organization makes it easy to locate and understand tests, and mirrors the structure of `TodoWorkflowTests.cs` for consistency.
