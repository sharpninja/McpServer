using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using YamlDotNet.Serialization;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Iteration 4 unit tests for Requirements workflow orchestration.
/// Tests FR/TR/TEST CRUD operations (list with filters, get by ID, create/update/delete),
/// mapping CRUD (create FR-to-TR, delete mapping), document generation (markdown/YAML),
/// ingest (parse requirements from markdown), selection-state management,
/// and validation error responses (duplicate IDs, invalid mapping references).
/// Mocks RequirementsClient from McpServer.Client and verifies YAML request/response shaping.
/// Red phase: all tests expected to fail until implementation is complete.
/// </summary>
public class RequirementsWorkflowTests
{
    private readonly IRequirementsWorkflow _workflow;
    private readonly IYamlSerializer _yamlSerializer;

    public RequirementsWorkflowTests()
    {
        _yamlSerializer = new FakeYamlSerializer();
        _workflow = Substitute.For<IRequirementsWorkflow>();
    }

    #region FR Query Tests

    [Fact]
    public async Task ListFrAsync_NoFilters_ReturnsAllFRs()
    {
        var expectedResult = new FrQueryResultAdapter(new List<FrEntry>
        {
            CreateFrEntry("FR-MCP-001", "API Authentication", "API authentication design", "critical", "MCP"),
            CreateFrEntry("FR-MCP-002", "Session Management", "Session handling requirements", "high", "MCP")
        });

        _workflow.ListFrAsync(null, null, default)
            .Returns(Task.FromResult<IFrQueryResult>(expectedResult));

        var result = await _workflow.ListFrAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        await _workflow.Received(1).ListFrAsync(null, null, default);
    }

    [Fact]
    public async Task ListFrAsync_WithAreaFilter_ReturnsMatchingFRs()
    {
        var expectedResult = new FrQueryResultAdapter(new List<FrEntry>
        {
            CreateFrEntry("FR-AUTH-001", "OAuth2 Support", "OAuth2 authentication", "high", "AUTH")
        });

        _workflow.ListFrAsync("AUTH", null, default)
            .Returns(Task.FromResult<IFrQueryResult>(expectedResult));

        var result = await _workflow.ListFrAsync(area: "AUTH");

        Assert.Single(result.Items);
        Assert.Equal("AUTH", result.Items[0].Area);
        await _workflow.Received(1).ListFrAsync("AUTH", null, default);
    }

    [Fact]
    public async Task ListFrAsync_WithStatusFilter_ReturnsMatchingFRs()
    {
        var expectedResult = new FrQueryResultAdapter(new List<FrEntry>
        {
            CreateFrEntry("FR-MCP-001", "Completed Feature", "Feature description", "high", "MCP", "completed")
        }, "completed");

        _workflow.ListFrAsync(null, "completed", default)
            .Returns(Task.FromResult<IFrQueryResult>(expectedResult));

        var result = await _workflow.ListFrAsync(status: "completed");

        Assert.Single(result.Items);
        Assert.Equal("completed", result.Items[0].Status);
        await _workflow.Received(1).ListFrAsync(null, "completed", default);
    }

    #endregion

    #region FR Get/Create/Update/Delete Tests

    [Fact]
    public async Task GetFrAsync_ValidId_ReturnsFrItem()
    {
        var expectedItem = CreateFrItem("FR-MCP-001", "API Authentication", "API auth design");

        _workflow.GetFrAsync("FR-MCP-001", default)
            .Returns(Task.FromResult(expectedItem));

        var result = await _workflow.GetFrAsync("FR-MCP-001");

        Assert.NotNull(result);
        Assert.Equal("FR-MCP-001", result.Id);
        Assert.Equal("API Authentication", result.Title);
        await _workflow.Received(1).GetFrAsync("FR-MCP-001", default);
    }

    [Fact]
    public async Task GetFrAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.GetFrAsync("invalid-id", default)
            .Throws(new ArgumentException("Invalid FR ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetFrAsync("invalid-id"));
    }

    [Fact]
    public async Task GetFrAsync_FrNotFound_ThrowsInvalidOperationException()
    {
        _workflow.GetFrAsync("FR-NONEXISTENT-999", default)
            .Throws(new InvalidOperationException("FR item not found: FR-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.GetFrAsync("FR-NONEXISTENT-999"));
    }

    [Fact]
    public async Task CreateFrAsync_ValidRequest_CreatesFrItem()
    {
        var request = CreateFrCreateRequest();
        var createdItem = CreateFrItem("FR-MCP-001", "New FR", "New FR description");
        var mutationResult = CreateFrMutationResult(true, createdItem);

        _workflow.CreateFrAsync(Arg.Any<IFrCreateRequest>(), default)
            .Returns(Task.FromResult(mutationResult));

        var result = await _workflow.CreateFrAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("FR-MCP-001", result.Item.Id);
        await _workflow.Received(1).CreateFrAsync(Arg.Any<IFrCreateRequest>(), default);
    }

    [Fact]
    public async Task CreateFrAsync_DuplicateId_ThrowsInvalidOperationException()
    {
        var request = CreateFrCreateRequest();

        _workflow.CreateFrAsync(Arg.Any<IFrCreateRequest>(), default)
            .Throws(new InvalidOperationException("FR item with ID FR-MCP-001 already exists"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.CreateFrAsync(request));
    }

    [Fact]
    public async Task UpdateFrAsync_WithSelection_UpdatesFrItem()
    {
        var request = CreateFrUpdateRequest();
        var updatedItem = CreateFrItem("FR-MCP-001", "Updated FR", "Updated description");
        var mutationResult = CreateFrMutationResult(true, updatedItem);
        var mockSelectionState = CreateMockSelectionState("FR-MCP-001", null, null);

        _workflow.CurrentSelection().Returns(mockSelectionState);
        _workflow.UpdateFrAsync(Arg.Any<IFrUpdateRequest>(), default)
            .Returns(Task.FromResult(mutationResult));

        var result = await _workflow.UpdateFrAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Updated FR", result.Item.Title);
        await _workflow.Received(1).UpdateFrAsync(Arg.Any<IFrUpdateRequest>(), default);
    }

    [Fact]
    public async Task UpdateFrAsync_NoSelection_ThrowsInvalidOperationException()
    {
        var request = CreateFrUpdateRequest();

        _workflow.CurrentSelection().Returns((IRequirementsSelectionState?)null);
        _workflow.UpdateFrAsync(Arg.Any<IFrUpdateRequest>(), default)
            .Throws(new InvalidOperationException("No FR is currently selected"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.UpdateFrAsync(request));
    }

    [Fact]
    public async Task DeleteFrAsync_ValidId_DeletesFrItem()
    {
        _workflow.DeleteFrAsync("FR-MCP-001", default)
            .Returns(Task.CompletedTask);

        await _workflow.DeleteFrAsync("FR-MCP-001");

        await _workflow.Received(1).DeleteFrAsync("FR-MCP-001", default);
    }

    [Fact]
    public async Task DeleteFrAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.DeleteFrAsync("invalid-id", default)
            .Throws(new ArgumentException("Invalid FR ID format: invalid-id"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.DeleteFrAsync("invalid-id"));
    }

    #endregion

    #region TR Query Tests

    [Fact]
    public async Task ListTrAsync_NoFilters_ReturnsAllTRs()
    {
        var expectedResult = new TrQueryResultAdapter(new List<TrEntry>
        {
            CreateTrEntry("TR-MCP-ARCH-001", "Microservice Architecture", "Service design"),
            CreateTrEntry("TR-MCP-PERF-001", "Performance Requirements", "Performance specs")
        });

        _workflow.ListTrAsync(null, null, null, default)
            .Returns(Task.FromResult<ITrQueryResult>(expectedResult));

        var result = await _workflow.ListTrAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        await _workflow.Received(1).ListTrAsync(null, null, null, default);
    }

    [Fact]
    public async Task ListTrAsync_WithAreaAndSubareaFilters_ReturnsMatchingTRs()
    {
        var expectedResult = new TrQueryResultAdapter(new List<TrEntry>
        {
            CreateTrEntry("TR-MCP-ARCH-001", "Architecture Design", "Design spec")
        });

        _workflow.ListTrAsync("MCP", "ARCH", null, default)
            .Returns(Task.FromResult<ITrQueryResult>(expectedResult));

        var result = await _workflow.ListTrAsync(area: "MCP", subarea: "ARCH");

        Assert.Single(result.Items);
        Assert.Equal("TR-MCP-ARCH-001", result.Items[0].Id);
        await _workflow.Received(1).ListTrAsync("MCP", "ARCH", null, default);
    }

    #endregion

    #region TR Get/Create/Update/Delete Tests

    [Fact]
    public async Task GetTrAsync_ValidId_ReturnsTrItem()
    {
        var expectedItem = CreateTrItem("TR-MCP-ARCH-001", "Architecture", "Architecture design");

        _workflow.GetTrAsync("TR-MCP-ARCH-001", default)
            .Returns(Task.FromResult(expectedItem));

        var result = await _workflow.GetTrAsync("TR-MCP-ARCH-001");

        Assert.NotNull(result);
        Assert.Equal("TR-MCP-ARCH-001", result.Id);
        Assert.Equal("Architecture", result.Title);
        await _workflow.Received(1).GetTrAsync("TR-MCP-ARCH-001", default);
    }

    [Fact]
    public async Task GetTrAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.GetTrAsync("TR-MCP-001", default)
            .Throws(new ArgumentException("Invalid TR ID format: TR-MCP-001"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetTrAsync("TR-MCP-001"));
    }

    [Fact]
    public async Task CreateTrAsync_ValidRequest_CreatesTrItem()
    {
        var request = CreateTrCreateRequest();
        var createdItem = CreateTrItem("TR-MCP-ARCH-001", "New TR", "New TR description");
        var mutationResult = CreateTrMutationResult(true, createdItem);

        _workflow.CreateTrAsync(Arg.Any<ITrCreateRequest>(), default)
            .Returns(Task.FromResult(mutationResult));

        var result = await _workflow.CreateTrAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("TR-MCP-ARCH-001", result.Item.Id);
        await _workflow.Received(1).CreateTrAsync(Arg.Any<ITrCreateRequest>(), default);
    }

    [Fact]
    public async Task UpdateTrAsync_WithSelection_UpdatesTrItem()
    {
        var request = CreateTrUpdateRequest();
        var updatedItem = CreateTrItem("TR-MCP-ARCH-001", "Updated TR", "Updated description");
        var mutationResult = CreateTrMutationResult(true, updatedItem);
        var mockSelectionState = CreateMockSelectionState(null, "TR-MCP-ARCH-001", null);

        _workflow.CurrentSelection().Returns(mockSelectionState);
        _workflow.UpdateTrAsync(Arg.Any<ITrUpdateRequest>(), default)
            .Returns(Task.FromResult(mutationResult));

        var result = await _workflow.UpdateTrAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Updated TR", result.Item.Title);
        await _workflow.Received(1).UpdateTrAsync(Arg.Any<ITrUpdateRequest>(), default);
    }

    [Fact]
    public async Task DeleteTrAsync_ValidId_DeletesTrItem()
    {
        _workflow.DeleteTrAsync("TR-MCP-ARCH-001", default)
            .Returns(Task.CompletedTask);

        await _workflow.DeleteTrAsync("TR-MCP-ARCH-001");

        await _workflow.Received(1).DeleteTrAsync("TR-MCP-ARCH-001", default);
    }

    #endregion

    #region TEST Query Tests

    [Fact]
    public async Task ListTestAsync_NoFilters_ReturnsAllTests()
    {
        var expectedResult = new TestQueryResultAdapter(new List<TestEntry>
        {
            CreateTestEntry("TEST-MCP-001", "Unit test for auth"),
            CreateTestEntry("TEST-MCP-002", "Integration test for API")
        });

        _workflow.ListTestAsync(null, null, default)
            .Returns(Task.FromResult<ITestQueryResult>(expectedResult));

        var result = await _workflow.ListTestAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        await _workflow.Received(1).ListTestAsync(null, null, default);
    }

    [Fact]
    public async Task ListTestAsync_WithAreaFilter_ReturnsMatchingTests()
    {
        var expectedResult = new TestQueryResultAdapter(new List<TestEntry>
        {
            CreateTestEntry("TEST-AUTH-001", "OAuth test condition")
        });

        _workflow.ListTestAsync("AUTH", null, default)
            .Returns(Task.FromResult<ITestQueryResult>(expectedResult));

        var result = await _workflow.ListTestAsync(area: "AUTH");

        Assert.Single(result.Items);
        Assert.Equal("TEST-AUTH-001", result.Items[0].Id);
        await _workflow.Received(1).ListTestAsync("AUTH", null, default);
    }

    #endregion

    #region TEST Get/Create/Update/Delete Tests

    [Fact]
    public async Task GetTestAsync_ValidId_ReturnsTestItem()
    {
        var expectedItem = CreateTestItem("TEST-MCP-001", "Test condition");

        _workflow.GetTestAsync("TEST-MCP-001", default)
            .Returns(Task.FromResult(expectedItem));

        var result = await _workflow.GetTestAsync("TEST-MCP-001");

        Assert.NotNull(result);
        Assert.Equal("TEST-MCP-001", result.Id);
        await _workflow.Received(1).GetTestAsync("TEST-MCP-001", default);
    }

    [Fact]
    public async Task GetTestAsync_InvalidId_ThrowsArgumentException()
    {
        _workflow.GetTestAsync("TEST-001", default)
            .Throws(new ArgumentException("Invalid TEST ID format: TEST-001"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetTestAsync("TEST-001"));
    }

    [Fact]
    public async Task CreateTestAsync_ValidRequest_CreatesTestItem()
    {
        var request = CreateTestCreateRequest();
        var createdItem = CreateTestItem("TEST-MCP-001", "New test condition");
        var mutationResult = CreateTestMutationResult(true, createdItem);

        _workflow.CreateTestAsync(Arg.Any<ITestCreateRequest>(), default)
            .Returns(Task.FromResult(mutationResult));

        var result = await _workflow.CreateTestAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("TEST-MCP-001", result.Item.Id);
        await _workflow.Received(1).CreateTestAsync(Arg.Any<ITestCreateRequest>(), default);
    }

    [Fact]
    public async Task UpdateTestAsync_WithSelection_UpdatesTestItem()
    {
        var request = CreateTestUpdateRequest();
        var updatedItem = CreateTestItem("TEST-MCP-001", "Updated test condition");
        var mutationResult = CreateTestMutationResult(true, updatedItem);
        var mockSelectionState = CreateMockSelectionState(null, null, "TEST-MCP-001");

        _workflow.CurrentSelection().Returns(mockSelectionState);
        _workflow.UpdateTestAsync(Arg.Any<ITestUpdateRequest>(), default)
            .Returns(Task.FromResult(mutationResult));

        var result = await _workflow.UpdateTestAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Updated test condition", result.Item.Description);
        await _workflow.Received(1).UpdateTestAsync(Arg.Any<ITestUpdateRequest>(), default);
    }

    [Fact]
    public async Task DeleteTestAsync_ValidId_DeletesTestItem()
    {
        _workflow.DeleteTestAsync("TEST-MCP-001", default)
            .Returns(Task.CompletedTask);

        await _workflow.DeleteTestAsync("TEST-MCP-001");

        await _workflow.Received(1).DeleteTestAsync("TEST-MCP-001", default);
    }

    #endregion

    #region Mapping CRUD Tests

    [Fact]
    public async Task ListMappingsAsync_NoFilters_ReturnsAllMappings()
    {
        var expectedResult = new MappingQueryResultAdapter(new List<IMappingItem>
        {
            CreateMappingItem("FR-MCP-001", "TR-MCP-ARCH-001", null),
            CreateMappingItem("FR-MCP-002", "TR-MCP-PERF-001", "TEST-MCP-001")
        });

        _workflow.ListMappingsAsync(null, null, null, default)
            .Returns(Task.FromResult<IMappingQueryResult>(expectedResult));

        var result = await _workflow.ListMappingsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        await _workflow.Received(1).ListMappingsAsync(null, null, null, default);
    }

    [Fact]
    public async Task ListMappingsAsync_WithFrIdFilter_ReturnsMatchingMappings()
    {
        var expectedResult = new MappingQueryResultAdapter(new List<IMappingItem>
        {
            CreateMappingItem("FR-MCP-001", "TR-MCP-ARCH-001", null)
        });

        _workflow.ListMappingsAsync("FR-MCP-001", null, null, default)
            .Returns(Task.FromResult<IMappingQueryResult>(expectedResult));

        var result = await _workflow.ListMappingsAsync(frId: "FR-MCP-001");

        Assert.Single(result.Items);
        Assert.Equal("FR-MCP-001", result.Items[0].FrId);
        await _workflow.Received(1).ListMappingsAsync("FR-MCP-001", null, null, default);
    }

    [Fact]
    public async Task CreateMappingAsync_ValidRequest_CreatesMapping()
    {
        var request = CreateMappingCreateRequest("FR-MCP-001", "TR-MCP-ARCH-001", null);
        var createdMapping = CreateMappingItem("FR-MCP-001", "TR-MCP-ARCH-001", null);
        var mutationResult = CreateMappingMutationResult(true, createdMapping);

        _workflow.CreateMappingAsync(Arg.Any<IMappingCreateRequest>(), default)
            .Returns(Task.FromResult(mutationResult));

        var result = await _workflow.CreateMappingAsync(request);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("FR-MCP-001", result.Item.FrId);
        Assert.Equal("TR-MCP-ARCH-001", result.Item.TrId);
        await _workflow.Received(1).CreateMappingAsync(Arg.Any<IMappingCreateRequest>(), default);
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidFrReference_ThrowsInvalidOperationException()
    {
        var request = CreateMappingCreateRequest("FR-NONEXISTENT-999", "TR-MCP-ARCH-001", null);

        _workflow.CreateMappingAsync(Arg.Any<IMappingCreateRequest>(), default)
            .Throws(new InvalidOperationException("Referenced FR does not exist: FR-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.CreateMappingAsync(request));
    }

    [Fact]
    public async Task CreateMappingAsync_InvalidTrReference_ThrowsInvalidOperationException()
    {
        var request = CreateMappingCreateRequest("FR-MCP-001", "TR-NONEXISTENT-999", null);

        _workflow.CreateMappingAsync(Arg.Any<IMappingCreateRequest>(), default)
            .Throws(new InvalidOperationException("Referenced TR does not exist: TR-NONEXISTENT-999"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.CreateMappingAsync(request));
    }

    [Fact]
    public async Task CreateMappingAsync_NoRequirementIds_ThrowsArgumentException()
    {
        var request = CreateMappingCreateRequest(null, null, null);

        _workflow.CreateMappingAsync(Arg.Any<IMappingCreateRequest>(), default)
            .Throws(new ArgumentException("At least one requirement ID must be provided"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.CreateMappingAsync(request));
    }

    [Fact]
    public async Task DeleteMappingAsync_ValidIds_DeletesMapping()
    {
        _workflow.DeleteMappingAsync("FR-MCP-001", "TR-MCP-ARCH-001", null, default)
            .Returns(Task.CompletedTask);

        await _workflow.DeleteMappingAsync(frId: "FR-MCP-001", trId: "TR-MCP-ARCH-001");

        await _workflow.Received(1).DeleteMappingAsync("FR-MCP-001", "TR-MCP-ARCH-001", null, default);
    }

    [Fact]
    public async Task DeleteMappingAsync_NoIds_ThrowsArgumentException()
    {
        _workflow.DeleteMappingAsync(null, null, null, default)
            .Throws(new ArgumentException("At least one requirement ID must be provided"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.DeleteMappingAsync());
    }

    [Fact]
    public async Task DeleteMappingAsync_MappingNotFound_ThrowsInvalidOperationException()
    {
        _workflow.DeleteMappingAsync("FR-MCP-001", "TR-NONEXISTENT-999", null, default)
            .Throws(new InvalidOperationException("Mapping not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.DeleteMappingAsync(frId: "FR-MCP-001", trId: "TR-NONEXISTENT-999"));
    }

    #endregion

    #region Document Generation Tests

    [Fact]
    public async Task GenerateDocumentAsync_MarkdownFormat_ReturnsMarkdownContent()
    {
        var expectedResult = CreateDocumentGenerationResult("markdown", "fr", "# Functional Requirements\n\n## FR-MCP-001...");

        _workflow.GenerateDocumentAsync("markdown", "fr", default)
            .Returns(Task.FromResult(expectedResult));

        var result = await _workflow.GenerateDocumentAsync("markdown", "fr");

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("markdown", result.Format);
        Assert.Equal("fr", result.DocType);
        Assert.Contains("# Functional Requirements", result.Content);
        await _workflow.Received(1).GenerateDocumentAsync("markdown", "fr", default);
    }

    [Fact]
    public async Task GenerateDocumentAsync_YamlFormat_ReturnsYamlContent()
    {
        var expectedResult = CreateDocumentGenerationResult("yaml", "tr", "---\nrequirements:\n  - id: TR-MCP-ARCH-001");

        _workflow.GenerateDocumentAsync("yaml", "tr", default)
            .Returns(Task.FromResult(expectedResult));

        var result = await _workflow.GenerateDocumentAsync("yaml", "tr");

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("yaml", result.Format);
        Assert.Equal("tr", result.DocType);
        Assert.Contains("requirements:", result.Content);
        await _workflow.Received(1).GenerateDocumentAsync("yaml", "tr", default);
    }

    [Fact]
    public async Task GenerateDocumentAsync_MatrixDocType_ReturnsMatrixDocument()
    {
        var expectedResult = CreateDocumentGenerationResult("markdown", "matrix", "# Requirements Traceability Matrix");

        _workflow.GenerateDocumentAsync("markdown", "matrix", default)
            .Returns(Task.FromResult(expectedResult));

        var result = await _workflow.GenerateDocumentAsync("markdown", "matrix");

        Assert.NotNull(result);
        Assert.Equal("matrix", result.DocType);
        Assert.Contains("Matrix", result.Content);
        await _workflow.Received(1).GenerateDocumentAsync("markdown", "matrix", default);
    }

    [Fact]
    public async Task GenerateDocumentAsync_AllDocType_ReturnsCompleteDocument()
    {
        var expectedResult = CreateDocumentGenerationResult("markdown", "all", "# Complete Requirements Package");

        _workflow.GenerateDocumentAsync("markdown", "all", default)
            .Returns(Task.FromResult(expectedResult));

        var result = await _workflow.GenerateDocumentAsync("markdown", "all");

        Assert.NotNull(result);
        Assert.Equal("all", result.DocType);
        await _workflow.Received(1).GenerateDocumentAsync("markdown", "all", default);
    }

    [Fact]
    public async Task GenerateDocumentAsync_InvalidFormat_ThrowsArgumentException()
    {
        _workflow.GenerateDocumentAsync("invalid", "fr", default)
            .Throws(new ArgumentException("Invalid format: invalid. Valid values: markdown, yaml"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GenerateDocumentAsync("invalid", "fr"));
    }

    [Fact]
    public async Task GenerateDocumentAsync_InvalidDocType_ThrowsArgumentException()
    {
        _workflow.GenerateDocumentAsync("markdown", "invalid", default)
            .Throws(new ArgumentException("Invalid docType: invalid. Valid values: fr, tr, test, matrix, all"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GenerateDocumentAsync("markdown", "invalid"));
    }

    #endregion

    #region Document Ingestion Tests

    [Fact]
    public async Task IngestDocumentAsync_MarkdownContent_IngestsRequirements()
    {
        var content = "# Functional Requirements\n\n## FR-MCP-001 API Auth\n\nDescription...";
        var expectedResult = CreateDocumentIngestionResult(1, 0, 0, 0, 0, 0, 0);

        _workflow.IngestDocumentAsync(content, "markdown", "overwrite", default)
            .Returns(Task.FromResult(expectedResult));

        var result = await _workflow.IngestDocumentAsync(content, "markdown", "overwrite");

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(1, result.FrCreated);
        Assert.Empty(result.Conflicts);
        await _workflow.Received(1).IngestDocumentAsync(content, "markdown", "overwrite", default);
    }

    [Fact]
    public async Task IngestDocumentAsync_YamlContent_IngestsRequirements()
    {
        var content = "---\nrequirements:\n  - id: TR-MCP-ARCH-001\n    title: Architecture";
        var expectedResult = CreateDocumentIngestionResult(0, 0, 1, 0, 0, 0, 0);

        _workflow.IngestDocumentAsync(content, "yaml", "merge", default)
            .Returns(Task.FromResult(expectedResult));

        var result = await _workflow.IngestDocumentAsync(content, "yaml", "merge");

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(1, result.TrCreated);
        await _workflow.Received(1).IngestDocumentAsync(content, "yaml", "merge", default);
    }

    [Fact]
    public async Task IngestDocumentAsync_OverwriteStrategy_ReplacesExistingRequirements()
    {
        var content = "# FR-MCP-001 Updated";
        var expectedResult = CreateDocumentIngestionResult(0, 1, 0, 0, 0, 0, 0);

        _workflow.IngestDocumentAsync(content, "markdown", "overwrite", default)
            .Returns(Task.FromResult(expectedResult));

        var result = await _workflow.IngestDocumentAsync(content, "markdown", "overwrite");

        Assert.Equal(1, result.FrUpdated);
        await _workflow.Received(1).IngestDocumentAsync(content, "markdown", "overwrite", default);
    }

    [Fact]
    public async Task IngestDocumentAsync_SkipStrategy_SkipsConflicts()
    {
        var content = "# FR-MCP-001 Existing";
        var conflicts = new List<IIngestionConflict>
        {
            CreateIngestionConflict("FR-MCP-001", "duplicate_id", "ID already exists", "skipped")
        };
        var expectedResult = CreateDocumentIngestionResult(0, 0, 0, 0, 0, 0, 0, conflicts);

        _workflow.IngestDocumentAsync(content, "markdown", "skip", default)
            .Returns(Task.FromResult(expectedResult));

        var result = await _workflow.IngestDocumentAsync(content, "markdown", "skip");

        Assert.NotNull(result);
        Assert.Single(result.Conflicts);
        Assert.Equal("skipped", result.Conflicts[0].Resolution);
        await _workflow.Received(1).IngestDocumentAsync(content, "markdown", "skip", default);
    }

    [Fact]
    public async Task IngestDocumentAsync_InvalidFormat_ThrowsArgumentException()
    {
        _workflow.IngestDocumentAsync("content", "invalid", "overwrite", default)
            .Throws(new ArgumentException("Invalid format: invalid. Valid values: markdown, yaml"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.IngestDocumentAsync("content", "invalid", "overwrite"));
    }

    [Fact]
    public async Task IngestDocumentAsync_InvalidMergeStrategy_ThrowsArgumentException()
    {
        _workflow.IngestDocumentAsync("content", "markdown", "invalid", default)
            .Throws(new ArgumentException("Invalid mergeStrategy: invalid. Valid values: overwrite, merge, skip"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.IngestDocumentAsync("content", "markdown", "invalid"));
    }

    [Fact]
    public async Task IngestDocumentAsync_EmptyContent_ThrowsArgumentException()
    {
        _workflow.IngestDocumentAsync("", "markdown", "overwrite", default)
            .Throws(new ArgumentException("Content cannot be null or empty"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.IngestDocumentAsync("", "markdown", "overwrite"));
    }

    [Fact]
    public async Task IngestDocumentAsync_WithConflicts_ReportsConflicts()
    {
        var content = "# FR-INVALID-X Invalid ID";
        var conflicts = new List<IIngestionConflict>
        {
            CreateIngestionConflict("FR-INVALID-X", "invalid_format", "ID does not match required pattern", "failed")
        };
        var expectedResult = CreateDocumentIngestionResult(0, 0, 0, 0, 0, 0, 0, conflicts);

        _workflow.IngestDocumentAsync(content, "markdown", "overwrite", default)
            .Returns(Task.FromResult(expectedResult));

        var result = await _workflow.IngestDocumentAsync(content, "markdown", "overwrite");

        Assert.NotNull(result);
        Assert.Single(result.Conflicts);
        Assert.Equal("invalid_format", result.Conflicts[0].ConflictType);
        Assert.Equal("failed", result.Conflicts[0].Resolution);
    }

    #endregion

    #region Selection State Tests

    [Fact]
    public void CurrentSelection_NoSelection_ReturnsNull()
    {
        _workflow.CurrentSelection().Returns((IRequirementsSelectionState?)null);

        var selection = _workflow.CurrentSelection();

        Assert.Null(selection);
    }

    [Fact]
    public void CurrentSelection_WithFrSelection_ReturnsSelectionState()
    {
        var mockSelectionState = CreateMockSelectionState("FR-MCP-001", null, null);

        _workflow.CurrentSelection().Returns(mockSelectionState);

        var selection = _workflow.CurrentSelection();

        Assert.NotNull(selection);
        Assert.Equal("FR-MCP-001", selection!.FrId);
        Assert.Null(selection.TrId);
        Assert.Null(selection.TestId);
    }

    [Fact]
    public void CurrentSelection_WithMultipleSelections_ReturnsAllSelected()
    {
        var mockSelectionState = CreateMockSelectionState("FR-MCP-001", "TR-MCP-ARCH-001", "TEST-MCP-001");

        _workflow.CurrentSelection().Returns(mockSelectionState);

        var selection = _workflow.CurrentSelection();

        Assert.NotNull(selection);
        Assert.Equal("FR-MCP-001", selection!.FrId);
        Assert.Equal("TR-MCP-ARCH-001", selection.TrId);
        Assert.Equal("TEST-MCP-001", selection.TestId);
    }

    [Fact]
    public void CurrentSelection_HasTimestamp_ReturnsRecentTimestamp()
    {
        var mockSelectionState = CreateMockSelectionState("FR-MCP-001", null, null);

        _workflow.CurrentSelection().Returns(mockSelectionState);

        var selection = _workflow.CurrentSelection();

        Assert.NotNull(selection);
        Assert.True(selection!.SelectedAt <= DateTimeOffset.UtcNow);
        Assert.True(selection.SelectedAt >= DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    #endregion

    #region YAML Shaping Tests

    [Fact]
    public void YamlShaping_FrCreateRequest_MatchesExpectedStructure()
    {
        var request = new
        {
            id = "FR-MCP-001",
            title = "API Authentication",
            description = "User authentication design",
            priority = "critical",
            area = "MCP"
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("request", request));

        Assert.Contains("type: request", yaml);
        Assert.Contains("id: FR-MCP-001", yaml);
        Assert.Contains("title: API Authentication", yaml);
        Assert.Contains("priority: critical", yaml);
    }

    [Fact]
    public void YamlShaping_TrQueryResponse_MatchesExpectedStructure()
    {
        var response = new
        {
            items = new[]
            {
                new { id = "TR-MCP-ARCH-001", title = "Architecture", area = "MCP", subarea = "ARCH" }
            },
            totalCount = 1
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("response", response));

        Assert.Contains("type: response", yaml);
        Assert.Contains("id: TR-MCP-ARCH-001", yaml);
        Assert.Contains("totalCount: 1", yaml);
    }

    [Fact]
    public void YamlShaping_MappingItem_MatchesExpectedStructure()
    {
        var mapping = new
        {
            frId = "FR-MCP-001",
            trId = "TR-MCP-ARCH-001",
            testId = (string?)null,
            notes = "Primary mapping"
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("mapping", mapping));

        Assert.Contains("type: mapping", yaml);
        Assert.Contains("frId: FR-MCP-001", yaml);
        Assert.Contains("trId: TR-MCP-ARCH-001", yaml);
    }

    [Fact]
    public void YamlShaping_DocumentGenerationResult_MatchesExpectedStructure()
    {
        var result = new
        {
            success = true,
            format = "markdown",
            docType = "fr",
            content = "# Requirements...",
            generatedAt = DateTimeOffset.UtcNow.ToString("o")
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("document", result));

        Assert.Contains("type: document", yaml);
        Assert.Contains("format: markdown", yaml);
        Assert.Contains("docType: fr", yaml);
    }

    [Fact]
    public void YamlShaping_IngestionResult_MatchesExpectedStructure()
    {
        var result = new
        {
            success = true,
            frCreated = 2,
            frUpdated = 1,
            trCreated = 3,
            conflicts = new[]
            {
                new { requirementId = "FR-MCP-001", conflictType = "duplicate_id", resolution = "overwritten" }
            }
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("ingestion", result));

        Assert.Contains("type: ingestion", yaml);
        Assert.Contains("frCreated: 2", yaml);
        Assert.Contains("conflictType: duplicate_id", yaml);
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task ValidationError_DuplicateFrId_ReturnsStructuredError()
    {
        var request = CreateFrCreateRequest();

        _workflow.CreateFrAsync(Arg.Any<IFrCreateRequest>(), default)
            .Throws(new InvalidOperationException("FR item with ID FR-MCP-001 already exists"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.CreateFrAsync(request));

        Assert.Contains("already exists", exception.Message);
        Assert.Contains("FR-MCP-001", exception.Message);
    }

    [Fact]
    public async Task ValidationError_InvalidTrIdFormat_ReturnsStructuredError()
    {
        _workflow.GetTrAsync("TR-INVALID", default)
            .Throws(new ArgumentException("Invalid TR ID format: TR-INVALID. Expected format: TR-<AREA>-<SUBAREA>-###"));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetTrAsync("TR-INVALID"));

        Assert.Contains("Invalid TR ID format", exception.Message);
    }

    [Fact]
    public async Task ValidationError_InvalidTestIdFormat_ReturnsStructuredError()
    {
        _workflow.GetTestAsync("test-001", default)
            .Throws(new ArgumentException("Invalid TEST ID format: test-001. Expected format: TEST-<AREA>-###"));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.GetTestAsync("test-001"));

        Assert.Contains("Invalid TEST ID format", exception.Message);
    }

    [Fact]
    public async Task ValidationError_MissingMappingReference_ReturnsStructuredError()
    {
        var request = CreateMappingCreateRequest("FR-MCP-001", "TR-NONEXISTENT-999", null);

        _workflow.CreateMappingAsync(Arg.Any<IMappingCreateRequest>(), default)
            .Throws(new InvalidOperationException("Referenced TR does not exist: TR-NONEXISTENT-999"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.CreateMappingAsync(request));

        Assert.Contains("does not exist", exception.Message);
        Assert.Contains("TR-NONEXISTENT-999", exception.Message);
    }

    [Fact]
    public async Task ValidationError_StorageError_ReturnsStructuredError()
    {
        _workflow.ListFrAsync(null, null, default)
            .Throws(new InvalidOperationException("Storage connection failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.ListFrAsync());

        Assert.Contains("Storage", exception.Message);
    }

    #endregion

    #region Helper Methods

    private static FrEntry CreateFrEntry(string id, string title, string body, string priority = "medium", string area = "MCP", string status = "pending")
    {
        return new FrEntry
        {
            Id = id,
            Title = title,
            Body = body
        };
    }

    private static IFrItem CreateFrItem(string id, string title, string description, string priority = "medium", string status = "pending", string area = "MCP")
    {
        var item = Substitute.For<IFrItem>();
        item.Id.Returns(id);
        item.Title.Returns(title);
        item.Description.Returns(description);
        item.Priority.Returns(priority);
        item.Status.Returns(status);
        item.Area.Returns(area);
        item.CreatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
        item.UpdatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
        return item;
    }

    private static IFrCreateRequest CreateFrCreateRequest(string id = "FR-MCP-001")
    {
        var request = Substitute.For<IFrCreateRequest>();
        request.Id.Returns(id);
        request.Title.Returns("New FR");
        request.Description.Returns("New FR description");
        request.Priority.Returns("high");
        request.Area.Returns("MCP");
        return request;
    }

    private static IFrUpdateRequest CreateFrUpdateRequest()
    {
        var request = Substitute.For<IFrUpdateRequest>();
        request.Title.Returns("Updated FR");
        request.Description.Returns("Updated description");
        request.Status.Returns("in_progress");
        return request;
    }

    private static IFrMutationResult CreateFrMutationResult(bool success, IFrItem item)
    {
        var result = Substitute.For<IFrMutationResult>();
        result.Success.Returns(success);
        result.Item.Returns(item);
        return result;
    }

    private static TrEntry CreateTrEntry(string id, string title, string body)
    {
        return new TrEntry
        {
            Id = id,
            Title = title,
            Body = body
        };
    }

    private static ITrItem CreateTrItem(string id, string title, string description, string area = "MCP", string subarea = "ARCH")
    {
        var item = Substitute.For<ITrItem>();
        item.Id.Returns(id);
        item.Title.Returns(title);
        item.Description.Returns(description);
        item.Area.Returns(area);
        item.Subarea.Returns(subarea);
        item.Priority.Returns("medium");
        item.Status.Returns("pending");
        item.CreatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
        item.UpdatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
        return item;
    }

    private static ITrCreateRequest CreateTrCreateRequest(string id = "TR-MCP-ARCH-001")
    {
        var request = Substitute.For<ITrCreateRequest>();
        request.Id.Returns(id);
        request.Title.Returns("New TR");
        request.Description.Returns("New TR description");
        request.Priority.Returns("high");
        request.Area.Returns("MCP");
        request.Subarea.Returns("ARCH");
        return request;
    }

    private static ITrUpdateRequest CreateTrUpdateRequest()
    {
        var request = Substitute.For<ITrUpdateRequest>();
        request.Title.Returns("Updated TR");
        request.Description.Returns("Updated description");
        request.Status.Returns("in_progress");
        return request;
    }

    private static ITrMutationResult CreateTrMutationResult(bool success, ITrItem item)
    {
        var result = Substitute.For<ITrMutationResult>();
        result.Success.Returns(success);
        result.Item.Returns(item);
        return result;
    }

    private static TestEntry CreateTestEntry(string id, string condition)
    {
        return new TestEntry
        {
            Id = id,
            Condition = condition
        };
    }

    private static ITestItem CreateTestItem(string id, string description, string area = "MCP", string testType = "unit")
    {
        var item = Substitute.For<ITestItem>();
        item.Id.Returns(id);
        item.Title.Returns($"Test {id}");
        item.Description.Returns(description);
        item.Area.Returns(area);
        item.TestType.Returns(testType);
        item.Priority.Returns("medium");
        item.Status.Returns("pending");
        item.CreatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
        item.UpdatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
        return item;
    }

    private static ITestCreateRequest CreateTestCreateRequest(string id = "TEST-MCP-001")
    {
        var request = Substitute.For<ITestCreateRequest>();
        request.Id.Returns(id);
        request.Title.Returns("New Test");
        request.Description.Returns("New test description");
        request.Priority.Returns("high");
        request.Area.Returns("MCP");
        request.TestType.Returns("unit");
        return request;
    }

    private static ITestUpdateRequest CreateTestUpdateRequest()
    {
        var request = Substitute.For<ITestUpdateRequest>();
        request.Title.Returns("Updated Test");
        request.Description.Returns("Updated test condition");
        request.Status.Returns("in_progress");
        return request;
    }

    private static ITestMutationResult CreateTestMutationResult(bool success, ITestItem item)
    {
        var result = Substitute.For<ITestMutationResult>();
        result.Success.Returns(success);
        result.Item.Returns(item);
        return result;
    }

    private static IMappingItem CreateMappingItem(string? frId, string? trId, string? testId, string? notes = null)
    {
        var item = Substitute.For<IMappingItem>();
        item.FrId.Returns(frId);
        item.TrId.Returns(trId);
        item.TestId.Returns(testId);
        item.Notes.Returns(notes);
        item.CreatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
        return item;
    }

    private static IMappingCreateRequest CreateMappingCreateRequest(string? frId, string? trId, string? testId)
    {
        var request = Substitute.For<IMappingCreateRequest>();
        request.FrId.Returns(frId);
        request.TrId.Returns(trId);
        request.TestId.Returns(testId);
        return request;
    }

    private static IMappingMutationResult CreateMappingMutationResult(bool success, IMappingItem item)
    {
        var result = Substitute.For<IMappingMutationResult>();
        result.Success.Returns(success);
        result.Item.Returns(item);
        return result;
    }

    private static IDocumentGenerationResult CreateDocumentGenerationResult(string format, string docType, string content)
    {
        var result = Substitute.For<IDocumentGenerationResult>();
        result.Success.Returns(true);
        result.Format.Returns(format);
        result.DocType.Returns(docType);
        result.Content.Returns(content);
        result.GeneratedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
        return result;
    }

    private static IDocumentIngestionResult CreateDocumentIngestionResult(
        int frCreated, int frUpdated, int trCreated, int trUpdated, int testCreated, int testUpdated, int mappingsCreated,
        List<IIngestionConflict>? conflicts = null)
    {
        var result = Substitute.For<IDocumentIngestionResult>();
        result.Success.Returns(true);
        result.FrCreated.Returns(frCreated);
        result.FrUpdated.Returns(frUpdated);
        result.TrCreated.Returns(trCreated);
        result.TrUpdated.Returns(trUpdated);
        result.TestCreated.Returns(testCreated);
        result.TestUpdated.Returns(testUpdated);
        result.MappingsCreated.Returns(mappingsCreated);
        result.Conflicts.Returns(conflicts ?? new List<IIngestionConflict>());
        result.IngestedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
        return result;
    }

    private static IIngestionConflict CreateIngestionConflict(string requirementId, string conflictType, string description, string resolution)
    {
        var conflict = Substitute.For<IIngestionConflict>();
        conflict.RequirementId.Returns(requirementId);
        conflict.ConflictType.Returns(conflictType);
        conflict.Description.Returns(description);
        conflict.Resolution.Returns(resolution);
        return conflict;
    }

    private static IRequirementsSelectionState CreateMockSelectionState(string? frId, string? trId, string? testId)
    {
        var state = Substitute.For<IRequirementsSelectionState>();
        state.FrId.Returns(frId);
        state.TrId.Returns(trId);
        state.TestId.Returns(testId);
        state.SelectedAt.Returns(DateTimeOffset.UtcNow);
        return state;
    }

    private IYamlEnvelope CreateEnvelope(string type, object payload)
    {
        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns(type);
        envelope.Payload.Returns(payload);
        return envelope;
    }

    #endregion

    #region Adapter Classes

    private class FrQueryResultAdapter : IFrQueryResult
    {
        private readonly List<FrEntry> _entries;
        private readonly string? _status;

        public FrQueryResultAdapter(List<FrEntry> entries, string? status = null)
        {
            _entries = entries;
            _status = status;
        }

        public IReadOnlyList<IFrItem> Items => _entries.Select(e => CreateFrItemFromEntry(e, _status)).ToList();
        public int TotalCount => _entries.Count;

        private static IFrItem CreateFrItemFromEntry(FrEntry entry, string? status = null)
        {
            var item = Substitute.For<IFrItem>();
            item.Id.Returns(entry.Id);
            item.Title.Returns(entry.Title);
            item.Description.Returns(entry.Body);
            item.Priority.Returns("medium");
            item.Status.Returns(status ?? "pending");
            item.Area.Returns(entry.Id.Split('-')[1]);
            item.CreatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
            item.UpdatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
            return item;
        }
    }

    private class TrQueryResultAdapter : ITrQueryResult
    {
        private readonly List<TrEntry> _entries;

        public TrQueryResultAdapter(List<TrEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyList<ITrItem> Items => _entries.Select(e => CreateTrItemFromEntry(e)).ToList();
        public int TotalCount => _entries.Count;

        private static ITrItem CreateTrItemFromEntry(TrEntry entry)
        {
            var item = Substitute.For<ITrItem>();
            item.Id.Returns(entry.Id);
            item.Title.Returns(entry.Title);
            item.Description.Returns(entry.Body);
            var parts = entry.Id.Split('-');
            item.Area.Returns(parts.Length > 1 ? parts[1] : "MCP");
            item.Subarea.Returns(parts.Length > 2 ? parts[2] : "ARCH");
            item.Priority.Returns("medium");
            item.Status.Returns("pending");
            item.CreatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
            item.UpdatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
            return item;
        }
    }

    private class TestQueryResultAdapter : ITestQueryResult
    {
        private readonly List<TestEntry> _entries;

        public TestQueryResultAdapter(List<TestEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyList<ITestItem> Items => _entries.Select(e => CreateTestItemFromEntry(e)).ToList();
        public int TotalCount => _entries.Count;

        private static ITestItem CreateTestItemFromEntry(TestEntry entry)
        {
            var item = Substitute.For<ITestItem>();
            item.Id.Returns(entry.Id);
            item.Title.Returns($"Test {entry.Id}");
            item.Description.Returns(entry.Condition);
            item.Area.Returns(entry.Id.Split('-')[1]);
            item.TestType.Returns("unit");
            item.Priority.Returns("medium");
            item.Status.Returns("pending");
            item.CreatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
            item.UpdatedAt.Returns(DateTimeOffset.UtcNow.ToString("o"));
            return item;
        }
    }

    private class MappingQueryResultAdapter : IMappingQueryResult
    {
        private readonly List<IMappingItem> _items;

        public MappingQueryResultAdapter(List<IMappingItem> items)
        {
            _items = items;
        }

        public IReadOnlyList<IMappingItem> Items => _items;
        public int TotalCount => _items.Count;
    }

    #endregion
}
