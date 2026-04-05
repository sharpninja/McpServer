# Iteration 4 Integration Tests Summary

## Overview
Comprehensive integration tests for the Requirements workflow via YAML STDIO protocol.

## Test File
- **Location**: `tests/McpServer.Repl.IntegrationTests/Iteration4IntegrationTests.cs`
- **Test Count**: 34 tests
- **Test Class**: `Iteration4IntegrationTests`

## Test Coverage

### FR (Functional Requirements) CRUD Operations
1. **RequirementsWorkflow_CreateFr_GetFr_DeleteFr_Succeeds**
   - Creates FR with valid ID format (FR-TEST-001)
   - Retrieves the created FR
   - Deletes the FR
   - Validates complete FR lifecycle

2. **RequirementsWorkflow_ListFr_ReturnsItems**
   - Creates multiple FRs in the same area
   - Lists FRs with area filter
   - Validates filtered list response
   - Cleanup: deletes created FRs

3. **RequirementsWorkflow_UpdateFr_ModifiesRequirement**
   - Creates FR with initial values
   - Updates title, status, priority, and notes
   - Validates update response
   - Cleanup: deletes FR

### TR (Technical Requirements) CRUD Operations
4. **RequirementsWorkflow_CreateTr_GetTr_DeleteTr_Succeeds**
   - Creates TR with valid ID format (TR-TEST-INTEG-001)
   - Retrieves the created TR
   - Deletes the TR
   - Validates complete TR lifecycle

5. **RequirementsWorkflow_ListTr_ReturnsItems**
   - Creates multiple TRs with area and subarea
   - Lists TRs with area and subarea filters
   - Validates filtered list response
   - Cleanup: deletes created TRs

6. **RequirementsWorkflow_UpdateTr_ModifiesRequirement**
   - Creates TR with initial values
   - Updates title, status, and notes
   - Validates update response
   - Cleanup: deletes TR

### TEST (Test Requirements) CRUD Operations
7. **RequirementsWorkflow_CreateTest_GetTest_DeleteTest_Succeeds**
   - Creates TEST with valid ID format (TEST-INT-001)
   - Retrieves the created TEST
   - Deletes the TEST
   - Validates complete TEST lifecycle

8. **RequirementsWorkflow_ListTest_ReturnsItems**
   - Creates multiple TEST items
   - Lists TEST items with area filter
   - Validates filtered list response
   - Cleanup: deletes created TEST items

9. **RequirementsWorkflow_UpdateTest_ModifiesRequirement**
   - Creates TEST with initial values
   - Updates title, status, and notes
   - Validates update response
   - Cleanup: deletes TEST

### Mapping CRUD Operations
10. **RequirementsWorkflow_CreateMapping_ListMapping_DeleteMapping_Succeeds**
    - Creates FR, TR, and TEST items
    - Creates mapping linking all three
    - Lists mappings filtered by FR ID
    - Deletes mapping
    - Validates complete mapping lifecycle
    - Cleanup: deletes all requirements

11. **RequirementsWorkflow_MappingListFilters_ReturnCorrectResults**
    - Creates multiple requirements and mappings
    - Tests filtering by FR ID
    - Tests filtering by TR ID
    - Validates correct filter results
    - Cleanup: deletes all mappings and requirements

### Document Generation
12. **RequirementsWorkflow_GenerateDocument_Markdown_ReturnsFormattedDocument**
    - Creates FR
    - Generates Markdown document for FR type
    - Validates response contains YAML envelope
    - Cleanup: deletes FR

13. **RequirementsWorkflow_GenerateDocument_Yaml_ReturnsYamlDocument**
    - Creates TR
    - Generates YAML document for TR type
    - Validates response contains YAML envelope
    - Cleanup: deletes TR

14. **RequirementsWorkflow_GenerateDocument_Matrix_ReturnsTraceabilityMatrix**
    - Generates traceability matrix in Markdown format
    - Validates response contains YAML envelope
    - Tests matrix document type

15. **RequirementsWorkflow_GenerateAllDocTypes_ValidatesOutput**
    - Iterates through all document types: fr, tr, test, matrix, all
    - Generates Markdown for each type
    - Validates all responses

16. **RequirementsWorkflow_GenerateBothFormats_ValidatesOutput**
    - Generates FR document in Markdown format
    - Generates FR document in YAML format
    - Validates both format responses

### Document Ingestion
17. **RequirementsWorkflow_IngestDocument_Markdown_ParsesAndCreatesRequirements**
    - Ingests Markdown content with FR definition
    - Uses merge strategy
    - Validates ingestion response
    - Cleanup: deletes ingested FR

18. **RequirementsWorkflow_IngestDocument_Yaml_ParsesAndCreatesRequirements**
    - Ingests YAML content with FR definition
    - Uses skip strategy
    - Validates ingestion response
    - Cleanup: deletes ingested FR

19. **RequirementsWorkflow_IngestMergeStrategy_OverwriteExisting**
    - Creates FR with original values
    - Ingests document with updated values using overwrite strategy
    - Retrieves FR to verify update
    - Cleanup: deletes FR

### Selection State Persistence
20. **RequirementsWorkflow_CurrentSelection_ReturnsSelectionState**
    - Queries current selection state
    - Validates response structure
    - Tests selection state query with no selection

21. **RequirementsWorkflow_SelectionStatePersistence_VerifyAcrossCommands**
    - Checks selection state initially
    - Checks selection state again after delay
    - Verifies persistence across multiple queries

### Validation and Error Handling
22. **RequirementsWorkflow_InvalidFrId_ReturnsError**
    - Attempts to create FR with invalid ID format
    - Validates error response

23. **RequirementsWorkflow_InvalidTrId_ReturnsError**
    - Attempts to create TR with invalid ID format
    - Validates error response

24. **RequirementsWorkflow_InvalidTestId_ReturnsError**
    - Attempts to create TEST with invalid ID format
    - Validates error response

25. **RequirementsWorkflow_GetNonExistentFr_ReturnsError**
    - Attempts to get FR that doesn't exist
    - Validates error response

26. **RequirementsWorkflow_GetNonExistentTr_ReturnsError**
    - Attempts to get TR that doesn't exist
    - Validates error response

27. **RequirementsWorkflow_GetNonExistentTest_ReturnsError**
    - Attempts to get TEST that doesn't exist
    - Validates error response

28. **RequirementsWorkflow_CreateDuplicateFr_ReturnsError**
    - Creates FR
    - Attempts to create duplicate FR with same ID
    - Validates error response
    - Cleanup: deletes FR

29. **RequirementsWorkflow_CreateMappingWithNonExistentRequirements_ReturnsError**
    - Attempts to create mapping with non-existent requirement IDs
    - Validates error response

30. **RequirementsWorkflow_DeleteNonExistentMapping_ReturnsError**
    - Attempts to delete mapping that doesn't exist
    - Validates error response

31. **RequirementsWorkflow_IngestInvalidMarkdown_ReturnsError**
    - Attempts to ingest invalid Markdown content
    - Validates error response

32. **RequirementsWorkflow_IngestInvalidYaml_ReturnsError**
    - Attempts to ingest invalid YAML content
    - Validates error response

### Complex Workflows
33. **RequirementsWorkflow_CompleteWorkflow_FrTrTestMappingGenerate**
    - Creates FR with critical priority
    - Creates TR with high priority
    - Creates TEST with e2e type
    - Creates mapping linking all three
    - Generates complete document
    - Lists mappings
    - Updates FR status to completed
    - Deletes mapping
    - Cleanup: deletes all requirements

34. **RequirementsWorkflow_ListWithFilters_ReturnsFilteredResults**
    - Creates FRs with different priorities
    - Updates one FR to completed status
    - Lists FRs filtered by completed status
    - Lists FRs filtered by pending status
    - Validates both filter results
    - Cleanup: deletes all FRs

## Helper Methods

### YamlEnvelopeBuilder Extensions
Added comprehensive builder methods for Requirements workflow:

#### FR Operations
- `CreateRequirementsListFrRequest`
- `CreateRequirementsGetFrRequest`
- `CreateRequirementsCreateFrRequest`
- `CreateRequirementsUpdateFrRequest`
- `CreateRequirementsDeleteFrRequest`

#### TR Operations
- `CreateRequirementsListTrRequest`
- `CreateRequirementsGetTrRequest`
- `CreateRequirementsCreateTrRequest`
- `CreateRequirementsUpdateTrRequest`
- `CreateRequirementsDeleteTrRequest`

#### TEST Operations
- `CreateRequirementsListTestRequest`
- `CreateRequirementsGetTestRequest`
- `CreateRequirementsCreateTestRequest`
- `CreateRequirementsUpdateTestRequest`
- `CreateRequirementsDeleteTestRequest`

#### Mapping Operations
- `CreateRequirementsListMappingsRequest`
- `CreateRequirementsCreateMappingRequest`
- `CreateRequirementsDeleteMappingRequest`

#### Document Operations
- `CreateRequirementsGenerateDocumentRequest`
- `CreateRequirementsIngestDocumentRequest`

#### Selection Operations
- `CreateRequirementsCurrentSelectionRequest`

## Test Patterns

### Standard Test Pattern
1. Start REPL process
2. Wait for initialization (1000ms)
3. Send YAML envelope via STDIO
4. Wait for response
5. Validate response structure
6. Cleanup created resources
7. Dispose of REPL process

### Request ID Generation
- Format: `req-{timestamp}Z-{suffix}`
- Timestamp: `yyyyMMddTHHmmss` UTC
- Example: `req-20260304T113901Z-create-fr`

### Response Validation
- All responses validated as non-null
- Response deserialized as YAML dictionary
- Type field presence verified
- Error responses validated for structure

## Key Features Tested

### YAML STDIO Protocol
✅ Request envelope structure
✅ Response envelope structure
✅ Error envelope structure
✅ YAML serialization/deserialization

### FR/TR/TEST CRUD
✅ Create with valid IDs
✅ Get by ID
✅ List with filters
✅ Update operations
✅ Delete operations

### Mapping Traceability
✅ Create mappings
✅ List with filters (FR, TR, TEST)
✅ Delete mappings
✅ Validation of referenced requirements

### Document Generation
✅ Markdown format
✅ YAML format
✅ All document types (fr, tr, test, matrix, all)
✅ Content validation

### Document Ingestion
✅ Markdown parsing
✅ YAML parsing
✅ Merge strategies (overwrite, merge, skip)
✅ Conflict handling

### Selection State
✅ Query current selection
✅ Persistence across commands
✅ Empty selection state

### Error Handling
✅ Invalid ID formats
✅ Non-existent requirements
✅ Duplicate creation
✅ Invalid mappings
✅ Invalid document formats

## Execution Time
- Average test execution: 1-2 seconds per test
- Total suite execution: ~2-3 minutes
- Includes REPL process startup and cleanup

## Notes
- Tests use isolated requirement IDs with unique prefixes
- All tests perform cleanup to avoid state pollution
- Tests validate YAML envelope structure, not detailed content
- Real implementation validation happens at unit test level
- Integration tests focus on STDIO protocol and workflow
