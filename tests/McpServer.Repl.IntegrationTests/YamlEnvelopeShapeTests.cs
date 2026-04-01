using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Repl.IntegrationTests;

/// <summary>
/// Tests to validate YAML envelope parsing and shape correctness.
/// </summary>
public sealed class YamlEnvelopeShapeTests
{
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public YamlEnvelopeShapeTests()
    {
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    [Fact]
    public void HelloEnvelope_SerializesAndDeserializes_Correctly()
    {
        var helloEnvelope = YamlEnvelopeBuilder.CreateHelloEnvelope(
            protocolVersion: "1.0",
            capabilities: new[] { "auth", "workspace-multi" },
            metadata: new Dictionary<string, string>
            {
                ["clientName"] = "test-client",
                ["clientVersion"] = "1.0.0"
            });

        var yaml = _yamlSerializer.Serialize(helloEnvelope);
        Assert.Contains("type: hello", yaml);
        Assert.Contains("protocolVersion: \"1.0\"", yaml);
        Assert.Contains("auth", yaml);
        Assert.Contains("workspace-multi", yaml);

        var deserialized = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
        Assert.NotNull(deserialized);
        Assert.True(deserialized.ContainsKey("type") || deserialized.ContainsKey("Type"));
    }

    [Fact]
    public void RequestEnvelope_SerializesAndDeserializes_Correctly()
    {
        var requestEnvelope = YamlEnvelopeBuilder.CreateRequestEnvelope(
            requestId: "req-001",
            method: "workspace.select",
            parameters: new { workspacePath = "/test/workspace" });

        var yaml = _yamlSerializer.Serialize(requestEnvelope);
        Assert.Contains("type: request", yaml);
        Assert.Contains("requestId: req-001", yaml);
        Assert.Contains("method: workspace.select", yaml);
        Assert.Contains("workspacePath: /test/workspace", yaml);

        var deserialized = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
        Assert.NotNull(deserialized);
    }

    [Fact]
    public void ResultEnvelope_SerializesAndDeserializes_Correctly()
    {
        var resultEnvelope = YamlEnvelopeBuilder.CreateResultEnvelope(
            requestId: "req-001",
            result: new { success = true, data = "test-data" });

        var yaml = _yamlSerializer.Serialize(resultEnvelope);
        Assert.Contains("type: result", yaml);
        Assert.Contains("requestId: req-001", yaml);
        Assert.Contains("success: true", yaml);

        var deserialized = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
        Assert.NotNull(deserialized);
    }

    [Fact]
    public void ErrorEnvelope_SerializesAndDeserializes_Correctly()
    {
        var errorEnvelope = YamlEnvelopeBuilder.CreateErrorEnvelope(
            requestId: "req-001",
            code: "invalid_workspace",
            message: "Workspace not found",
            details: new { attemptedPath = "/invalid/path" });

        var yaml = _yamlSerializer.Serialize(errorEnvelope);
        Assert.Contains("type: error", yaml);
        Assert.Contains("requestId: req-001", yaml);
        Assert.Contains("code: invalid_workspace", yaml);
        Assert.Contains("message: Workspace not found", yaml);

        var deserialized = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
        Assert.NotNull(deserialized);
    }

    [Fact]
    public void EventEnvelope_SerializesAndDeserializes_Correctly()
    {
        var eventEnvelope = YamlEnvelopeBuilder.CreateEventEnvelope(
            eventName: "workspace.changed",
            data: new { oldWorkspace = "/old", newWorkspace = "/new" });

        var yaml = _yamlSerializer.Serialize(eventEnvelope);
        Assert.Contains("type: event", yaml);
        Assert.Contains("event: workspace.changed", yaml);
        Assert.Contains("oldWorkspace: /old", yaml);

        var deserialized = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
        Assert.NotNull(deserialized);
    }

    [Fact]
    public void TrustBootstrapRequest_HasCorrectShape()
    {
        var bootstrapRequest = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            requestId: "trust-001",
            workspacePath: "/test/workspace",
            nonce: "challenge-123",
            signature: "signature-xyz");

        var yaml = _yamlSerializer.Serialize(bootstrapRequest);
        Assert.Contains("type: request", yaml);
        Assert.Contains("method: trust.bootstrap", yaml);
        Assert.Contains("workspacePath: /test/workspace", yaml);
        Assert.Contains("nonce: challenge-123", yaml);
        Assert.Contains("signature: signature-xyz", yaml);
    }

    [Fact]
    public void WorkspaceSelectRequest_HasCorrectShape()
    {
        var selectRequest = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            requestId: "ws-001",
            workspacePath: "/test/workspace");

        var yaml = _yamlSerializer.Serialize(selectRequest);
        Assert.Contains("type: request", yaml);
        Assert.Contains("method: workspace.select", yaml);
        Assert.Contains("requestId: ws-001", yaml);
        Assert.Contains("workspacePath: /test/workspace", yaml);
    }

    [Fact]
    public void NonceRequest_HasCorrectShape()
    {
        var nonceRequest = YamlEnvelopeBuilder.CreateNonceRequest(
            requestId: "nonce-001",
            workspacePath: "/test/workspace");

        var yaml = _yamlSerializer.Serialize(nonceRequest);
        Assert.Contains("type: request", yaml);
        Assert.Contains("method: trust.getNonce", yaml);
        Assert.Contains("requestId: nonce-001", yaml);
        Assert.Contains("workspacePath: /test/workspace", yaml);
    }

    [Fact]
    public void AllEnvelopeTypes_HaveTypeDiscriminator()
    {
        var envelopes = new[]
        {
            YamlEnvelopeBuilder.CreateHelloEnvelope(),
            YamlEnvelopeBuilder.CreateRequestEnvelope("req-001", "test.method"),
            YamlEnvelopeBuilder.CreateResultEnvelope("req-001"),
            YamlEnvelopeBuilder.CreateErrorEnvelope("req-001", "test_code", "test message"),
            YamlEnvelopeBuilder.CreateEventEnvelope("test.event")
        };

        foreach (var envelope in envelopes)
        {
            var yaml = _yamlSerializer.Serialize(envelope);
            Assert.Contains("type:", yaml);
            
            var deserialized = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            Assert.NotNull(deserialized);
            Assert.True(
                deserialized.ContainsKey("type") || deserialized.ContainsKey("Type"),
                "Envelope must have a type discriminator");
        }
    }

    [Fact]
    public void EnvelopePayload_IsProperlyNested()
    {
        var requestEnvelope = YamlEnvelopeBuilder.CreateRequestEnvelope(
            "req-001",
            "test.method",
            new { testParam = "testValue" });

        var yaml = _yamlSerializer.Serialize(requestEnvelope);
        var deserialized = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);

        Assert.NotNull(deserialized);
        Assert.True(
            deserialized.ContainsKey("payload") || deserialized.ContainsKey("Payload"),
            "Envelope must have a payload field");
    }

    [Fact]
    public void YamlSerialization_RoundTrip_PreservesData()
    {
        var originalEnvelope = YamlEnvelopeBuilder.CreateRequestEnvelope(
            "req-roundtrip-001",
            "test.method",
            new { key1 = "value1", key2 = 42 });

        var yaml = _yamlSerializer.Serialize(originalEnvelope);
        var deserialized = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
        var reserialized = _yamlSerializer.Serialize(deserialized);

        Assert.Contains("req-roundtrip-001", reserialized);
        Assert.Contains("test.method", reserialized);
    }
}
