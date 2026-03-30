using System.Globalization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Repl.IntegrationTests;

public sealed class Iteration4IntegrationTests : IDisposable
{
    private readonly ReplChildProcessHelper _replProcess;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public Iteration4IntegrationTests()
    {
        _replProcess = new ReplChildProcessHelper();
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    [Fact]
    public async Task RequirementsWorkflow_CreateFr_GetFr_DeleteFr_Succeeds()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var frId = "FR-TEST-001";

        var createEnvelope = YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
            GenerateRequestId("create-fr"),
            frId,
            "Test Functional Requirement",
            "This is a test functional requirement for integration testing",
            "high",
            "TEST",
            "Integration test notes");

        await SendCommandAndWaitAsync(createEnvelope);

        var createResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(createResponse);

        var getEnvelope = YamlEnvelopeBuilder.CreateRequirementsGetFrRequest(
            GenerateRequestId("get-fr"),
            frId);

        await SendCommandAndWaitAsync(getEnvelope);

        var getResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(getResponse);

        var deleteEnvelope = YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(
            GenerateRequestId("delete-fr"),
            frId);

        await SendCommandAndWaitAsync(deleteEnvelope);

        var deleteResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(deleteResponse);
    }

    [Fact]
    public async Task RequirementsWorkflow_CreateTr_GetTr_DeleteTr_Succeeds()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var trId = "TR-TEST-INTEG-001";

        var createEnvelope = YamlEnvelopeBuilder.CreateRequirementsCreateTrRequest(
            GenerateRequestId("create-tr"),
            trId,
            "Test Technical Requirement",
            "This is a test technical requirement for integration testing",
            "high",
            "TEST",
            "INTEG",
            "Integration test notes");

        await SendCommandAndWaitAsync(createEnvelope);

        var createResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(createResponse);

        var getEnvelope = YamlEnvelopeBuilder.CreateRequirementsGetTrRequest(
            GenerateRequestId("get-tr"),
            trId);

        await SendCommandAndWaitAsync(getEnvelope);

        var getResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(getResponse);

        var deleteEnvelope = YamlEnvelopeBuilder.CreateRequirementsDeleteTrRequest(
            GenerateRequestId("delete-tr"),
            trId);

        await SendCommandAndWaitAsync(deleteEnvelope);

        var deleteResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(deleteResponse);
    }

    [Fact]
    public async Task RequirementsWorkflow_CreateTest_GetTest_DeleteTest_Succeeds()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var testId = "TEST-INT-001";

        var createEnvelope = YamlEnvelopeBuilder.CreateRequirementsCreateTestRequest(
            GenerateRequestId("create-test"),
            testId,
            "Integration Test Requirement",
            "This is a test requirement for integration testing",
            "high",
            "INT",
            "integration",
            "Integration test notes");

        await SendCommandAndWaitAsync(createEnvelope);

        var createResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(createResponse);

        var getEnvelope = YamlEnvelopeBuilder.CreateRequirementsGetTestRequest(
            GenerateRequestId("get-test"),
            testId);

        await SendCommandAndWaitAsync(getEnvelope);

        var getResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(getResponse);

        var deleteEnvelope = YamlEnvelopeBuilder.CreateRequirementsDeleteTestRequest(
            GenerateRequestId("delete-test"),
            testId);

        await SendCommandAndWaitAsync(deleteEnvelope);

        var deleteResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(deleteResponse);
    }

    [Fact]
    public async Task RequirementsWorkflow_ListFr_ReturnsItems()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-1"),
                "FR-LIST-001",
                "List Test FR 1",
                "First functional requirement for list testing",
                "high",
                "LIST"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-2"),
                "FR-LIST-002",
                "List Test FR 2",
                "Second functional requirement for list testing",
                "medium",
                "LIST"));

        var listEnvelope = YamlEnvelopeBuilder.CreateRequirementsListFrRequest(
            GenerateRequestId("list-fr"),
            area: "LIST");

        await SendCommandAndWaitAsync(listEnvelope);

        var listResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(listResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup-1"), "FR-LIST-001"));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup-2"), "FR-LIST-002"));
    }

    [Fact]
    public async Task RequirementsWorkflow_ListTr_ReturnsItems()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTrRequest(
                GenerateRequestId("create-1"),
                "TR-LIST-ARCH-001",
                "List Test TR 1",
                "First technical requirement for list testing",
                "high",
                "LIST",
                "ARCH"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTrRequest(
                GenerateRequestId("create-2"),
                "TR-LIST-ARCH-002",
                "List Test TR 2",
                "Second technical requirement for list testing",
                "medium",
                "LIST",
                "ARCH"));

        var listEnvelope = YamlEnvelopeBuilder.CreateRequirementsListTrRequest(
            GenerateRequestId("list-tr"),
            area: "LIST",
            subarea: "ARCH");

        await SendCommandAndWaitAsync(listEnvelope);

        var listResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(listResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTrRequest(GenerateRequestId("cleanup-1"), "TR-LIST-ARCH-001"));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTrRequest(GenerateRequestId("cleanup-2"), "TR-LIST-ARCH-002"));
    }

    [Fact]
    public async Task RequirementsWorkflow_ListTest_ReturnsItems()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTestRequest(
                GenerateRequestId("create-1"),
                "TEST-LST-001",
                "List Test Requirement 1",
                "First test requirement for list testing",
                "high",
                "LST",
                "unit"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTestRequest(
                GenerateRequestId("create-2"),
                "TEST-LST-002",
                "List Test Requirement 2",
                "Second test requirement for list testing",
                "medium",
                "LST",
                "integration"));

        var listEnvelope = YamlEnvelopeBuilder.CreateRequirementsListTestRequest(
            GenerateRequestId("list-test"),
            area: "LST");

        await SendCommandAndWaitAsync(listEnvelope);

        var listResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(listResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTestRequest(GenerateRequestId("cleanup-1"), "TEST-LST-001"));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTestRequest(GenerateRequestId("cleanup-2"), "TEST-LST-002"));
    }

    [Fact]
    public async Task RequirementsWorkflow_UpdateFr_ModifiesRequirement()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var frId = "FR-UPD-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create"),
                frId,
                "Update Test FR",
                "Original description",
                "medium",
                "UPD"));

        var updateEnvelope = YamlEnvelopeBuilder.CreateRequirementsUpdateFrRequest(
            GenerateRequestId("update"),
            id: frId,
            title: "Updated FR Title",
            status: "in_progress",
            priority: "high",
            notes: "Updated with new notes");

        await SendCommandAndWaitAsync(updateEnvelope);

        var updateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(updateResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup"), frId));
    }

    [Fact]
    public async Task RequirementsWorkflow_UpdateTr_ModifiesRequirement()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var trId = "TR-UPD-TEST-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTrRequest(
                GenerateRequestId("create"),
                trId,
                "Update Test TR",
                "Original description",
                "medium",
                "UPD",
                "TEST"));

        var updateEnvelope = YamlEnvelopeBuilder.CreateRequirementsUpdateTrRequest(
            GenerateRequestId("update"),
            id: trId,
            title: "Updated TR Title",
            status: "completed",
            notes: "Completed and tested");

        await SendCommandAndWaitAsync(updateEnvelope);

        var updateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(updateResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTrRequest(GenerateRequestId("cleanup"), trId));
    }

    [Fact]
    public async Task RequirementsWorkflow_UpdateTest_ModifiesRequirement()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var testId = "TEST-UPD-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTestRequest(
                GenerateRequestId("create"),
                testId,
                "Update Test Requirement",
                "Original description",
                "medium",
                "UPD",
                "unit"));

        var updateEnvelope = YamlEnvelopeBuilder.CreateRequirementsUpdateTestRequest(
            GenerateRequestId("update"),
            id: testId,
            title: "Updated Test Title",
            status: "in_progress",
            notes: "Test in progress");

        await SendCommandAndWaitAsync(updateEnvelope);

        var updateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(updateResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTestRequest(GenerateRequestId("cleanup"), testId));
    }

    [Fact]
    public async Task RequirementsWorkflow_CreateMapping_ListMapping_DeleteMapping_Succeeds()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var frId = "FR-MAP-001";
        var trId = "TR-MAP-TEST-001";
        var testId = "TEST-MAP-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-fr"),
                frId,
                "Mapping Test FR",
                "FR for mapping test",
                "high",
                "MAP"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTrRequest(
                GenerateRequestId("create-tr"),
                trId,
                "Mapping Test TR",
                "TR for mapping test",
                "high",
                "MAP",
                "TEST"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTestRequest(
                GenerateRequestId("create-test"),
                testId,
                "Mapping Test Requirement",
                "Test requirement for mapping test",
                "high",
                "MAP",
                "integration"));

        var createMappingEnvelope = YamlEnvelopeBuilder.CreateRequirementsCreateMappingRequest(
            GenerateRequestId("create-mapping"),
            frId,
            trId,
            testId,
            "Complete traceability mapping");

        await SendCommandAndWaitAsync(createMappingEnvelope);

        var createMappingResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(createMappingResponse);

        var listMappingEnvelope = YamlEnvelopeBuilder.CreateRequirementsListMappingsRequest(
            GenerateRequestId("list-mapping"),
            frId: frId);

        await SendCommandAndWaitAsync(listMappingEnvelope);

        var listMappingResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(listMappingResponse);

        var deleteMappingEnvelope = YamlEnvelopeBuilder.CreateRequirementsDeleteMappingRequest(
            GenerateRequestId("delete-mapping"),
            frId,
            trId,
            testId);

        await SendCommandAndWaitAsync(deleteMappingEnvelope);

        var deleteMappingResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(deleteMappingResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTestRequest(GenerateRequestId("cleanup-test"), testId));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTrRequest(GenerateRequestId("cleanup-tr"), trId));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup-fr"), frId));
    }

    [Fact]
    public async Task RequirementsWorkflow_GenerateDocument_Markdown_ReturnsFormattedDocument()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var frId = "FR-GEN-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-fr"),
                frId,
                "Generate Document Test",
                "FR for document generation test",
                "high",
                "GEN"));

        var generateEnvelope = YamlEnvelopeBuilder.CreateRequirementsGenerateDocumentRequest(
            GenerateRequestId("generate-md"),
            "markdown",
            "fr");

        await SendCommandAndWaitAsync(generateEnvelope);

        var generateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(generateResponse);

        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(generateResponse!);
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("type") || response.ContainsKey("Type"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup"), frId));
    }

    [Fact]
    public async Task RequirementsWorkflow_GenerateDocument_Yaml_ReturnsYamlDocument()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var trId = "TR-GEN-YAML-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTrRequest(
                GenerateRequestId("create-tr"),
                trId,
                "YAML Generation Test",
                "TR for YAML document generation test",
                "medium",
                "GEN",
                "YAML"));

        var generateEnvelope = YamlEnvelopeBuilder.CreateRequirementsGenerateDocumentRequest(
            GenerateRequestId("generate-yaml"),
            "yaml",
            "tr");

        await SendCommandAndWaitAsync(generateEnvelope);

        var generateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(generateResponse);

        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(generateResponse!);
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("type") || response.ContainsKey("Type"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTrRequest(GenerateRequestId("cleanup"), trId));
    }

    [Fact]
    public async Task RequirementsWorkflow_GenerateDocument_Matrix_ReturnsTraceabilityMatrix()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var generateEnvelope = YamlEnvelopeBuilder.CreateRequirementsGenerateDocumentRequest(
            GenerateRequestId("generate-matrix"),
            "markdown",
            "matrix");

        await SendCommandAndWaitAsync(generateEnvelope);

        var generateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(generateResponse);

        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(generateResponse!);
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("type") || response.ContainsKey("Type"));
    }

    [Fact]
    public async Task RequirementsWorkflow_IngestDocument_Markdown_ParsesAndCreatesRequirements()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var markdownContent = @"# Functional Requirements
## FR-ING-001: Ingest Test Requirement
System must support ingesting requirements from Markdown
Priority: high
Status: pending
Area: ING";

        var ingestEnvelope = YamlEnvelopeBuilder.CreateRequirementsIngestDocumentRequest(
            GenerateRequestId("ingest-md"),
            markdownContent,
            "markdown",
            "merge");

        await SendCommandAndWaitAsync(ingestEnvelope);

        var ingestResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(ingestResponse);

        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(ingestResponse!);
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("type") || response.ContainsKey("Type"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup"), "FR-ING-001"));
    }

    [Fact]
    public async Task RequirementsWorkflow_IngestDocument_Yaml_ParsesAndCreatesRequirements()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var yamlContent = @"requirements:
  - id: FR-INGY-001
    title: YAML Ingest Test
    description: System must support ingesting requirements from YAML
    priority: medium
    area: INGY
    status: pending";

        var ingestEnvelope = YamlEnvelopeBuilder.CreateRequirementsIngestDocumentRequest(
            GenerateRequestId("ingest-yaml"),
            yamlContent,
            "yaml",
            "skip");

        await SendCommandAndWaitAsync(ingestEnvelope);

        var ingestResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(ingestResponse);

        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(ingestResponse!);
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("type") || response.ContainsKey("Type"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup"), "FR-INGY-001"));
    }

    [Fact]
    public async Task RequirementsWorkflow_CurrentSelection_ReturnsSelectionState()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var currentSelectionEnvelope = YamlEnvelopeBuilder.CreateRequirementsCurrentSelectionRequest(
            GenerateRequestId("current-selection"));

        await SendCommandAndWaitAsync(currentSelectionEnvelope);

        var selectionResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(selectionResponse);

        var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(selectionResponse!);
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("type") || response.ContainsKey("Type"));
    }

    [Fact]
    public async Task RequirementsWorkflow_InvalidFrId_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidId = "invalid-fr-id";

        var createEnvelope = YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
            GenerateRequestId("create-invalid"),
            invalidId,
            "Invalid ID Test",
            "Testing invalid FR ID format",
            "medium",
            "TEST");

        await SendCommandAndWaitAsync(createEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_InvalidTrId_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidId = "TR-NO-SUBAREA";

        var createEnvelope = YamlEnvelopeBuilder.CreateRequirementsCreateTrRequest(
            GenerateRequestId("create-invalid"),
            invalidId,
            "Invalid TR ID Test",
            "Testing invalid TR ID format",
            "medium",
            "TEST",
            "SUBAREA");

        await SendCommandAndWaitAsync(createEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_InvalidTestId_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidId = "TESTINVALID001";

        var createEnvelope = YamlEnvelopeBuilder.CreateRequirementsCreateTestRequest(
            GenerateRequestId("create-invalid"),
            invalidId,
            "Invalid TEST ID Test",
            "Testing invalid TEST ID format",
            "medium",
            "TST",
            "unit");

        await SendCommandAndWaitAsync(createEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_GetNonExistentFr_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var nonExistentId = "FR-XXX-999";

        var getEnvelope = YamlEnvelopeBuilder.CreateRequirementsGetFrRequest(
            GenerateRequestId("get-nonexistent"),
            nonExistentId);

        await SendCommandAndWaitAsync(getEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_GetNonExistentTr_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var nonExistentId = "TR-XXX-YYY-999";

        var getEnvelope = YamlEnvelopeBuilder.CreateRequirementsGetTrRequest(
            GenerateRequestId("get-nonexistent"),
            nonExistentId);

        await SendCommandAndWaitAsync(getEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_GetNonExistentTest_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var nonExistentId = "TEST-XXX-999";

        var getEnvelope = YamlEnvelopeBuilder.CreateRequirementsGetTestRequest(
            GenerateRequestId("get-nonexistent"),
            nonExistentId);

        await SendCommandAndWaitAsync(getEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_CreateDuplicateFr_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var frId = "FR-DUP-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-1"),
                frId,
                "Duplicate Test FR",
                "First creation",
                "medium",
                "DUP"));

        var duplicateEnvelope = YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
            GenerateRequestId("create-duplicate"),
            frId,
            "Duplicate Test FR 2",
            "Attempting duplicate creation",
            "medium",
            "DUP");

        await SendCommandAndWaitAsync(duplicateEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup"), frId));
    }

    [Fact]
    public async Task RequirementsWorkflow_CreateMappingWithNonExistentRequirements_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var createMappingEnvelope = YamlEnvelopeBuilder.CreateRequirementsCreateMappingRequest(
            GenerateRequestId("create-invalid-mapping"),
            "FR-FAKE-001",
            "TR-FAKE-TEST-001",
            "TEST-FAKE-001",
            "Invalid mapping");

        await SendCommandAndWaitAsync(createMappingEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_DeleteNonExistentMapping_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var deleteMappingEnvelope = YamlEnvelopeBuilder.CreateRequirementsDeleteMappingRequest(
            GenerateRequestId("delete-nonexistent"),
            "FR-FAKE-001",
            "TR-FAKE-TEST-001",
            "TEST-FAKE-001");

        await SendCommandAndWaitAsync(deleteMappingEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_IngestInvalidMarkdown_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidMarkdown = "This is not a valid requirement document format";

        var ingestEnvelope = YamlEnvelopeBuilder.CreateRequirementsIngestDocumentRequest(
            GenerateRequestId("ingest-invalid"),
            invalidMarkdown,
            "markdown",
            "merge");

        await SendCommandAndWaitAsync(ingestEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_IngestInvalidYaml_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidYaml = "not: [valid: yaml: structure";

        var ingestEnvelope = YamlEnvelopeBuilder.CreateRequirementsIngestDocumentRequest(
            GenerateRequestId("ingest-invalid-yaml"),
            invalidYaml,
            "yaml",
            "merge");

        await SendCommandAndWaitAsync(ingestEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task RequirementsWorkflow_CompleteWorkflow_FrTrTestMappingGenerate()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var frId = "FR-WFL-001";
        var trId = "TR-WFL-FULL-001";
        var testId = "TEST-WFL-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-fr"),
                frId,
                "Complete Workflow FR",
                "Functional requirement for complete workflow test",
                "critical",
                "WFL",
                "Complete workflow test"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTrRequest(
                GenerateRequestId("create-tr"),
                trId,
                "Complete Workflow TR",
                "Technical requirement for complete workflow test",
                "high",
                "WFL",
                "FULL",
                "Architecture notes"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTestRequest(
                GenerateRequestId("create-test"),
                testId,
                "Complete Workflow Test",
                "Test requirement for complete workflow",
                "high",
                "WFL",
                "e2e",
                "End-to-end test"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateMappingRequest(
                GenerateRequestId("create-mapping"),
                frId,
                trId,
                testId,
                "Complete traceability"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsGenerateDocumentRequest(
                GenerateRequestId("generate-all"),
                "yaml",
                "all"));

        var generateResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(generateResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsListMappingsRequest(
                GenerateRequestId("list-mappings"),
                frId: frId));

        var listResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(listResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsUpdateFrRequest(
                GenerateRequestId("update-fr"),
                id: frId,
                status: "completed",
                notes: "Workflow complete"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteMappingRequest(
                GenerateRequestId("delete-mapping"),
                frId,
                trId,
                testId));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTestRequest(GenerateRequestId("cleanup-test"), testId));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTrRequest(GenerateRequestId("cleanup-tr"), trId));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup-fr"), frId));
    }

    [Fact]
    public async Task RequirementsWorkflow_ListWithFilters_ReturnsFilteredResults()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-1"),
                "FR-FLT-001",
                "High Priority FR",
                "High priority functional requirement",
                "high",
                "FLT"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-2"),
                "FR-FLT-002",
                "Low Priority FR",
                "Low priority functional requirement",
                "low",
                "FLT"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsUpdateFrRequest(
                GenerateRequestId("update-1"),
                id: "FR-FLT-001",
                status: "completed"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsListFrRequest(
                GenerateRequestId("list-completed"),
                area: "FLT",
                status: "completed"));

        var completedResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(completedResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsListFrRequest(
                GenerateRequestId("list-pending"),
                area: "FLT",
                status: "pending"));

        var pendingResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(pendingResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup-1"), "FR-FLT-001"));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup-2"), "FR-FLT-002"));
    }

    [Fact]
    public async Task RequirementsWorkflow_MappingListFilters_ReturnCorrectResults()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var frId1 = "FR-MFL-001";
        var frId2 = "FR-MFL-002";
        var trId = "TR-MFL-TEST-001";
        var testId = "TEST-MFL-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-fr1"),
                frId1,
                "Mapping Filter FR 1",
                "First FR for mapping filter test",
                "high",
                "MFL"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-fr2"),
                frId2,
                "Mapping Filter FR 2",
                "Second FR for mapping filter test",
                "high",
                "MFL"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTrRequest(
                GenerateRequestId("create-tr"),
                trId,
                "Mapping Filter TR",
                "TR for mapping filter test",
                "high",
                "MFL",
                "TEST"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateTestRequest(
                GenerateRequestId("create-test"),
                testId,
                "Mapping Filter Test",
                "Test for mapping filter test",
                "high",
                "MFL",
                "unit"));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateMappingRequest(
                GenerateRequestId("create-map1"),
                frId1,
                trId,
                testId));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateMappingRequest(
                GenerateRequestId("create-map2"),
                frId2,
                trId,
                null));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsListMappingsRequest(
                GenerateRequestId("list-fr1"),
                frId: frId1));

        var fr1Response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(fr1Response);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsListMappingsRequest(
                GenerateRequestId("list-tr"),
                trId: trId));

        var trResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(trResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteMappingRequest(
                GenerateRequestId("delete-map1"),
                frId1,
                trId,
                testId));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteMappingRequest(
                GenerateRequestId("delete-map2"),
                frId2,
                trId,
                null));

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTestRequest(GenerateRequestId("cleanup-test"), testId));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteTrRequest(GenerateRequestId("cleanup-tr"), trId));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup-fr1"), frId1));
        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup-fr2"), frId2));
    }

    [Fact]
    public async Task RequirementsWorkflow_IngestMergeStrategy_OverwriteExisting()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var frId = "FR-MRG-001";

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCreateFrRequest(
                GenerateRequestId("create-original"),
                frId,
                "Original Title",
                "Original description",
                "low",
                "MRG",
                "Original notes"));

        var yamlContent = $@"requirements:
  - id: {frId}
    title: Updated Title
    description: Updated description via ingest
    priority: critical
    area: MRG
    status: in_progress
    notes: Updated via ingest";

        var ingestEnvelope = YamlEnvelopeBuilder.CreateRequirementsIngestDocumentRequest(
            GenerateRequestId("ingest-overwrite"),
            yamlContent,
            "yaml",
            "overwrite");

        await SendCommandAndWaitAsync(ingestEnvelope);

        var ingestResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(ingestResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsGetFrRequest(
                GenerateRequestId("get-updated"),
                frId));

        var getResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(getResponse);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsDeleteFrRequest(GenerateRequestId("cleanup"), frId));
    }

    [Fact]
    public async Task RequirementsWorkflow_GenerateAllDocTypes_ValidatesOutput()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var docTypes = new[] { "fr", "tr", "test", "matrix", "all" };

        foreach (var docType in docTypes)
        {
            var generateEnvelope = YamlEnvelopeBuilder.CreateRequirementsGenerateDocumentRequest(
                GenerateRequestId($"generate-{docType}"),
                "markdown",
                docType);

            await SendCommandAndWaitAsync(generateEnvelope);

            var response = _replProcess.StdoutLines.LastOrDefault();
            Assert.NotNull(response);

            var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
            Assert.NotNull(responseDict);
        }
    }

    [Fact]
    public async Task RequirementsWorkflow_GenerateBothFormats_ValidatesOutput()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var markdownEnvelope = YamlEnvelopeBuilder.CreateRequirementsGenerateDocumentRequest(
            GenerateRequestId("generate-md"),
            "markdown",
            "fr");

        await SendCommandAndWaitAsync(markdownEnvelope);

        var mdResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(mdResponse);

        var yamlEnvelope = YamlEnvelopeBuilder.CreateRequirementsGenerateDocumentRequest(
            GenerateRequestId("generate-yaml"),
            "yaml",
            "fr");

        await SendCommandAndWaitAsync(yamlEnvelope);

        var yamlResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(yamlResponse);
    }

    [Fact]
    public async Task RequirementsWorkflow_SelectionStatePersistence_VerifyAcrossCommands()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCurrentSelectionRequest(
                GenerateRequestId("check-initial")));

        var initialSelection = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(initialSelection);

        await Task.Delay(200);

        await SendCommandAndWaitAsync(
            YamlEnvelopeBuilder.CreateRequirementsCurrentSelectionRequest(
                GenerateRequestId("check-again")));

        var secondSelection = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(secondSelection);
    }

    private async Task SendCommandAndWaitAsync(object envelope)
    {
        var initialCount = _replProcess.StdoutLines.Count;
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(envelope));
        await _replProcess.WaitForStdoutLineCountAsync(initialCount + 1, TimeSpan.FromSeconds(5));
        await Task.Delay(100);
    }

    private static string GenerateRequestId(string suffix)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
        return $"req-{timestamp}Z-{suffix}";
    }

    public void Dispose()
    {
        _replProcess.StopAsync().Wait(TimeSpan.FromSeconds(3));
        _replProcess.Dispose();
    }
}
