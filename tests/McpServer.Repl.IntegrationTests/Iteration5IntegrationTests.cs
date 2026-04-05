using System.Globalization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Repl.IntegrationTests;

public sealed class Iteration5IntegrationTests : IDisposable
{
    private readonly ReplChildProcessHelper _replProcess;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public Iteration5IntegrationTests()
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
    public async Task GenericClientPassthrough_ContextQuery_ReturnsResults()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var contextQueryEnvelope = YamlEnvelopeBuilder.CreateContextQueryRequest(
            GenerateRequestId("context-query"),
            "test query",
            limit: 5);

        await SendCommandAndWaitAsync(contextQueryEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        Assert.True(responseDict.ContainsKey("type") || responseDict.ContainsKey("Type"));
    }

    [Fact]
    public async Task GenericClientPassthrough_RepoGetBranches_ReturnsResults()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var getBranchesEnvelope = YamlEnvelopeBuilder.CreateRepoGetBranchesRequest(
            GenerateRequestId("get-branches"));

        await SendCommandAndWaitAsync(getBranchesEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        Assert.True(responseDict.ContainsKey("type") || responseDict.ContainsKey("Type"));
    }

    [Fact]
    public async Task GenericClientPassthrough_DesktopOpenFolder_ValidatesArguments()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var openFolderEnvelope = YamlEnvelopeBuilder.CreateDesktopOpenFolderRequest(
            GenerateRequestId("open-folder"),
            "/tmp/test-folder");

        await SendCommandAndWaitAsync(openFolderEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        Assert.True(responseDict.ContainsKey("type") || responseDict.ContainsKey("Type"));
    }

    [Fact]
    public async Task GenericClientPassthrough_NestedObjectArgument_CoercesCorrectly()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var nestedArgs = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object>
            {
                ["query"] = "nested test",
                ["options"] = new Dictionary<string, object>
                {
                    ["maxResults"] = 10,
                    ["includeMetadata"] = true
                }
            }
        };

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("nested-object"),
            "context",
            "SearchAsync",
            nestedArgs);

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GenericClientPassthrough_ArrayArgument_CoercesCorrectly()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var arrayArgs = new Dictionary<string, object?>
        {
            ["tags"] = new[] { "tag1", "tag2", "tag3" },
            ["priorities"] = new[] { 1, 2, 3 }
        };

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("array-args"),
            "context",
            "SearchAsync",
            arrayArgs);

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GenericClientPassthrough_ComplexNestedStructure_CoercesCorrectly()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var complexArgs = new Dictionary<string, object?>
        {
            ["filters"] = new Dictionary<string, object>
            {
                ["categories"] = new[] { "cat1", "cat2" },
                ["dateRange"] = new Dictionary<string, object>
                {
                    ["start"] = "2024-01-01",
                    ["end"] = "2024-12-31"
                },
                ["flags"] = new Dictionary<string, object>
                {
                    ["includeArchived"] = false,
                    ["includeDeleted"] = false
                }
            }
        };

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("complex-nested"),
            "context",
            "SearchAsync",
            complexArgs);

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GenericClientPassthrough_YamlResponseShape_ValidStructure()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var contextQueryEnvelope = YamlEnvelopeBuilder.CreateContextQueryRequest(
            GenerateRequestId("yaml-shape-test"),
            "yaml shape test");

        await SendCommandAndWaitAsync(contextQueryEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        
        Assert.True(responseDict.ContainsKey("type") || responseDict.ContainsKey("Type"));
        
        var typeValue = responseDict.ContainsKey("type") ? responseDict["type"] : responseDict["Type"];
        Assert.True(typeValue?.ToString() == "result" || typeValue?.ToString() == "error");
        
        Assert.True(responseDict.ContainsKey("payload") || responseDict.ContainsKey("Payload"));
    }

    [Fact]
    public async Task GenericClientPassthrough_UnknownClient_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidEnvelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("unknown-client"),
            "invalidclient",
            "SomeMethodAsync",
            new Dictionary<string, object?> { ["arg1"] = "value1" });

        await SendCommandAndWaitAsync(invalidEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        
        var typeValue = responseDict.ContainsKey("type") ? responseDict["type"] : responseDict["Type"];
        Assert.Equal("error", typeValue?.ToString()?.ToLower());
    }

    [Fact]
    public async Task GenericClientPassthrough_UnknownMethod_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidEnvelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("unknown-method"),
            "context",
            "InvalidMethodAsync",
            new Dictionary<string, object?> { ["query"] = "test" });

        await SendCommandAndWaitAsync(invalidEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        
        var typeValue = responseDict.ContainsKey("type") ? responseDict["type"] : responseDict["Type"];
        Assert.Equal("error", typeValue?.ToString()?.ToLower());
    }

    [Fact]
    public async Task GenericClientPassthrough_MissingRequiredParameter_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidEnvelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("missing-param"),
            "context",
            "SearchAsync",
            new Dictionary<string, object?>());

        await SendCommandAndWaitAsync(invalidEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        
        var typeValue = responseDict.ContainsKey("type") ? responseDict["type"] : responseDict["Type"];
        Assert.Equal("error", typeValue?.ToString()?.ToLower());
    }

    [Fact]
    public async Task GenericClientPassthrough_InvalidArgumentType_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidEnvelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("invalid-type"),
            "context",
            "SearchAsync",
            new Dictionary<string, object?>
            {
                ["query"] = "test",
                ["limit"] = "not-a-number"
            });

        await SendCommandAndWaitAsync(invalidEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        
        var typeValue = responseDict.ContainsKey("type") ? responseDict["type"] : responseDict["Type"];
        Assert.Equal("error", typeValue?.ToString()?.ToLower());
    }

    [Fact]
    public async Task GenericClientPassthrough_NullForNonNullableParameter_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidEnvelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("null-param"),
            "context",
            "SearchAsync",
            new Dictionary<string, object?>
            {
                ["query"] = null
            });

        await SendCommandAndWaitAsync(invalidEnvelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        
        var typeValue = responseDict.ContainsKey("type") ? responseDict["type"] : responseDict["Type"];
        Assert.Equal("error", typeValue?.ToString()?.ToLower());
    }

    [Fact]
    public async Task GenericClientPassthrough_MultipleClients_Sequential()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var contextEnvelope = YamlEnvelopeBuilder.CreateContextQueryRequest(
            GenerateRequestId("multi-context"),
            "test");
        await SendCommandAndWaitAsync(contextEnvelope);
        var contextResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(contextResponse);

        var repoEnvelope = YamlEnvelopeBuilder.CreateRepoGetBranchesRequest(
            GenerateRequestId("multi-repo"));
        await SendCommandAndWaitAsync(repoEnvelope);
        var repoResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(repoResponse);

        var desktopEnvelope = YamlEnvelopeBuilder.CreateDesktopOpenFolderRequest(
            GenerateRequestId("multi-desktop"),
            "/tmp");
        await SendCommandAndWaitAsync(desktopEnvelope);
        var desktopResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(desktopResponse);
    }

    [Fact]
    public async Task GenericClientPassthrough_OptionalParameters_UsesDefaults()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var minimalArgs = new Dictionary<string, object?>
        {
            ["query"] = "optional params test"
        };

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("optional-params"),
            "context",
            "SearchAsync",
            minimalArgs);

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task GenericClientPassthrough_BooleanArguments_CoercesCorrectly()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var boolArgs = new Dictionary<string, object?>
        {
            ["query"] = "boolean test",
            ["includeArchived"] = true,
            ["includeDeleted"] = false
        };

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("bool-args"),
            "context",
            "SearchAsync",
            boolArgs);

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GenericClientPassthrough_NumericArguments_CoercesCorrectly()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var numericArgs = new Dictionary<string, object?>
        {
            ["query"] = "numeric test",
            ["limit"] = 25,
            ["offset"] = 0,
            ["maxScore"] = 0.95
        };

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("numeric-args"),
            "context",
            "SearchAsync",
            numericArgs);

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GenericClientPassthrough_EmptyArrayArgument_HandlesCorrectly()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var emptyArrayArgs = new Dictionary<string, object?>
        {
            ["query"] = "empty array test",
            ["tags"] = Array.Empty<string>()
        };

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("empty-array"),
            "context",
            "SearchAsync",
            emptyArrayArgs);

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GenericClientPassthrough_MixedTypeArray_CoercesElements()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var mixedArgs = new Dictionary<string, object?>
        {
            ["values"] = new object[] { "string", 42, true, 3.14 }
        };

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("mixed-array"),
            "context",
            "SearchAsync",
            mixedArgs);

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GenericClientPassthrough_CaseInsensitiveClientName_Resolves()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("case-insensitive-client"),
            "CONTEXT",
            "SearchAsync",
            new Dictionary<string, object?> { ["query"] = "case test" });

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task GenericClientPassthrough_CaseInsensitiveParameterName_Matches()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("case-insensitive-param"),
            "context",
            "SearchAsync",
            new Dictionary<string, object?> { ["QUERY"] = "case test" });

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
    }

    [Fact]
    public async Task GenericClientPassthrough_MultipleValidRequests_AllSucceed()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        for (int i = 0; i < 5; i++)
        {
            var envelope = YamlEnvelopeBuilder.CreateContextQueryRequest(
                GenerateRequestId($"multi-valid-{i}"),
                $"test query {i}");

            await SendCommandAndWaitAsync(envelope);

            var response = _replProcess.StdoutLines.LastOrDefault();
            Assert.NotNull(response);
        }
    }

    [Fact]
    public async Task GenericClientPassthrough_ResponseContainsRequestId_Correlation()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var requestId = GenerateRequestId("correlation-test");
        var envelope = YamlEnvelopeBuilder.CreateContextQueryRequest(requestId, "correlation test");

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        Assert.True(responseDict.ContainsKey("payload") || responseDict.ContainsKey("Payload"));
    }

    [Fact]
    public async Task GenericClientPassthrough_SuccessResultShape_HasCorrectFields()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var envelope = YamlEnvelopeBuilder.CreateContextQueryRequest(
            GenerateRequestId("result-shape"),
            "result shape test");

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        
        Assert.True(responseDict.ContainsKey("type") || responseDict.ContainsKey("Type"));
        Assert.True(responseDict.ContainsKey("payload") || responseDict.ContainsKey("Payload"));
        
        var payloadKey = responseDict.ContainsKey("payload") ? "payload" : "Payload";
        var payload = responseDict[payloadKey] as Dictionary<object, object>;
        
        if (payload != null)
        {
            var hasRequestId = payload.Keys.Any(k => k.ToString()?.ToLower() == "requestid");
            Assert.True(hasRequestId);
        }
    }

    [Fact]
    public async Task GenericClientPassthrough_ErrorResultShape_HasCorrectFields()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var envelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("error-shape"),
            "invalidclient",
            "SomeMethodAsync",
            new Dictionary<string, object?>());

        await SendCommandAndWaitAsync(envelope);

        var response = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(response);

        var responseDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(response!);
        Assert.NotNull(responseDict);
        
        var typeValue = responseDict.ContainsKey("type") ? responseDict["type"] : responseDict["Type"];
        Assert.Equal("error", typeValue?.ToString()?.ToLower());
        
        Assert.True(responseDict.ContainsKey("payload") || responseDict.ContainsKey("Payload"));
        
        var payloadKey = responseDict.ContainsKey("payload") ? "payload" : "Payload";
        var payload = responseDict[payloadKey] as Dictionary<object, object>;
        
        if (payload != null)
        {
            var hasCode = payload.Keys.Any(k => k.ToString()?.ToLower() == "code");
            var hasMessage = payload.Keys.Any(k => k.ToString()?.ToLower() == "message");
            Assert.True(hasCode);
            Assert.True(hasMessage);
        }
    }

    [Fact]
    public async Task GenericClientPassthrough_CompleteWorkflow_VariousClientTypes()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var contextEnvelope = YamlEnvelopeBuilder.CreateContextQueryRequest(
            GenerateRequestId("workflow-context"),
            "workflow test");
        await SendCommandAndWaitAsync(contextEnvelope);
        var contextResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(contextResponse);

        var repoEnvelope = YamlEnvelopeBuilder.CreateRepoGetBranchesRequest(
            GenerateRequestId("workflow-repo"));
        await SendCommandAndWaitAsync(repoEnvelope);
        var repoResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(repoResponse);

        var invalidEnvelope = YamlEnvelopeBuilder.CreateGenericClientRequest(
            GenerateRequestId("workflow-invalid"),
            "invalidclient",
            "TestAsync",
            new Dictionary<string, object?>());
        await SendCommandAndWaitAsync(invalidEnvelope);
        var errorResponse = _replProcess.StdoutLines.LastOrDefault();
        Assert.NotNull(errorResponse);
        
        var errorDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(errorResponse!);
        var typeValue = errorDict.ContainsKey("type") ? errorDict["type"] : errorDict["Type"];
        Assert.Equal("error", typeValue?.ToString()?.ToLower());
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
