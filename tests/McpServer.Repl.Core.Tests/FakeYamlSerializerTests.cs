using McpServer.Repl.Core;
using NSubstitute;
using YamlDotNet.Serialization;

namespace McpServer.Repl.Core.Tests;

public class FakeYamlSerializerTests
{
    private readonly FakeYamlSerializer _sut;

    public FakeYamlSerializerTests()
    {
        _sut = new FakeYamlSerializer();
    }

    [Fact]
    public void Serialize_HelloEnvelope_ProducesValidYaml()
    {
        var helloPayload = Substitute.For<IHelloPayload>();
        helloPayload.ProtocolVersion.Returns("1.0");
        helloPayload.Capabilities.Returns(new[] { "auth", "workspace-multi" });

        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns("hello");
        envelope.Payload.Returns(helloPayload);

        var yaml = _sut.Serialize(envelope);

        Assert.NotNull(yaml);
        Assert.Contains("type: hello", yaml);
    }

    [Fact]
    public void Serialize_RequestEnvelope_IncludesMethodAndParams()
    {
        var requestPayload = Substitute.For<IRequestPayload>();
        requestPayload.RequestId.Returns("req-001");
        requestPayload.Method.Returns("workspace.select");
        requestPayload.Params.Returns(new Dictionary<string, object?> 
        { 
            ["path"] = "/home/user/project" 
        });

        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns("request");
        envelope.Payload.Returns(requestPayload);

        var yaml = _sut.Serialize(envelope);

        Assert.NotNull(yaml);
        Assert.Contains("type: request", yaml);
    }

    [Fact]
    public void Deserialize_HelloYaml_ReturnsTypedEnvelope()
    {
        var yaml = @"
type: hello
payload:
  protocolVersion: ""1.0""
  capabilities:
    - auth
    - workspace-multi
";

        var envelope = _sut.Deserialize(yaml);

        Assert.NotNull(envelope);
        Assert.Equal("hello", envelope.Type);
        Assert.NotNull(envelope.Payload);
    }

    [Fact]
    public void Deserialize_RequestYaml_ReturnsTypedEnvelope()
    {
        var yaml = @"
type: request
payload:
  requestId: req-001
  method: workspace.select
  params:
    path: /home/user/project
";

        var envelope = _sut.Deserialize(yaml);

        Assert.NotNull(envelope);
        Assert.Equal("request", envelope.Type);
        Assert.NotNull(envelope.Payload);
    }

    [Fact]
    public void Deserialize_ErrorEnvelope_ParsesErrorPayload()
    {
        var yaml = @"
type: error
payload:
  requestId: req-001
  code: invalid_workspace
  message: Workspace not found
";

        var envelope = _sut.Deserialize(yaml);

        Assert.NotNull(envelope);
        Assert.Equal("error", envelope.Type);
    }

    [Fact]
    public void Deserialize_MalformedYaml_ThrowsFormatException()
    {
        var invalidYaml = "type: hello\npayload: [unmatched";

        Assert.Throws<FormatException>(() => _sut.Deserialize(invalidYaml));
    }

    [Fact]
    public void Deserialize_MissingType_ThrowsInvalidOperationException()
    {
        var yamlWithoutType = "payload:\n  test: value";

        Assert.Throws<InvalidOperationException>(() => _sut.Deserialize(yamlWithoutType));
    }

    [Fact]
    public void TryDeserialize_ValidYaml_ReturnsTrueWithEnvelope()
    {
        var yaml = "type: hello\npayload:\n  protocolVersion: \"1.0\"";

        var success = _sut.TryDeserialize(yaml, out var envelope);

        Assert.True(success);
        Assert.NotNull(envelope);
        Assert.Equal("hello", envelope.Type);
    }

    [Fact]
    public void TryDeserialize_InvalidYaml_ReturnsFalseWithNull()
    {
        var invalidYaml = "type: [invalid";

        var success = _sut.TryDeserialize(invalidYaml, out var envelope);

        Assert.False(success);
        Assert.Null(envelope);
    }

    [Fact]
    public void SerializeStream_MultipleEnvelopes_ProducesDocumentStream()
    {
        var envelope1 = Substitute.For<IYamlEnvelope>();
        envelope1.Type.Returns("hello");
        envelope1.Payload.Returns(new { protocolVersion = "1.0" });

        var envelope2 = Substitute.For<IYamlEnvelope>();
        envelope2.Type.Returns("request");
        envelope2.Payload.Returns(new { requestId = "req-001", method = "test" });

        var envelopes = new[] { envelope1, envelope2 };

        var yamlStream = _sut.SerializeStream(envelopes);

        Assert.NotNull(yamlStream);
        Assert.Contains("---", yamlStream);
        var documentCount = yamlStream.Split("---", StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.Equal(2, documentCount);
    }

    [Fact]
    public void DeserializeStream_TwoDocuments_ReturnsTwoEnvelopes()
    {
        var yamlStream = @"---
type: hello
payload:
  protocolVersion: ""1.0""
---
type: request
payload:
  requestId: req-001
  method: test
";

        var envelopes = _sut.DeserializeStream(yamlStream);

        Assert.NotNull(envelopes);
        Assert.Equal(2, envelopes.Count);
        Assert.Equal("hello", envelopes[0].Type);
        Assert.Equal("request", envelopes[1].Type);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        var originalPayload = new Dictionary<string, object?>
        {
            ["requestId"] = "req-123",
            ["method"] = "workspace.select",
            ["params"] = new Dictionary<string, object?> { ["path"] = "/test" }
        };

        var originalEnvelope = Substitute.For<IYamlEnvelope>();
        originalEnvelope.Type.Returns("request");
        originalEnvelope.Payload.Returns(originalPayload);

        var yaml = _sut.Serialize(originalEnvelope);
        var deserialized = _sut.Deserialize(yaml);

        Assert.NotNull(deserialized);
        Assert.Equal("request", deserialized.Type);
        Assert.NotNull(deserialized.Payload);
    }

    [Fact]
    public void Serialize_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Serialize(null!));
    }

    [Fact]
    public void Deserialize_NullYaml_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Deserialize(null!));
    }

    [Fact]
    public void Deserialize_EmptyYaml_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _sut.Deserialize(""));
    }

    [Fact]
    public void SerializeStream_NullEnvelopes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.SerializeStream(null!));
    }

    [Fact]
    public void DeserializeStream_NullYaml_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.DeserializeStream(null!));
    }
}

internal sealed class FakeYamlSerializer : IYamlSerializer
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public FakeYamlSerializer()
    {
        _serializer = new SerializerBuilder()
            .Build();

        _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public string Serialize(IYamlEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var obj = new
        {
            type = envelope.Type,
            payload = envelope.Payload
        };

        return _serializer.Serialize(obj);
    }

    public IYamlEnvelope Deserialize(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        try
        {
            var dict = _deserializer.Deserialize<Dictionary<string, object>>(yaml);

            if (!dict.ContainsKey("type"))
            {
                throw new InvalidOperationException("Missing 'type' field in envelope");
            }

            var envelope = Substitute.For<IYamlEnvelope>();
            envelope.Type.Returns(dict["type"]?.ToString() ?? throw new InvalidOperationException("Invalid type field"));
            envelope.Payload.Returns(dict.ContainsKey("payload") ? dict["payload"] : null);

            return envelope;
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new FormatException("Invalid YAML format", ex);
        }
    }

    public bool TryDeserialize(string yaml, out IYamlEnvelope? envelope)
    {
        try
        {
            envelope = Deserialize(yaml);
            return true;
        }
        catch
        {
            envelope = null;
            return false;
        }
    }

    public string SerializeStream(IEnumerable<IYamlEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        var documents = new List<string>();

        foreach (var envelope in envelopes)
        {
            documents.Add(Serialize(envelope));
        }

        return string.Join("---\n", documents);
    }

    public IReadOnlyList<IYamlEnvelope> DeserializeStream(string yamlStream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yamlStream);

        var documents = yamlStream.Split("---", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var envelopes = new List<IYamlEnvelope>();

        foreach (var doc in documents)
        {
            if (!string.IsNullOrWhiteSpace(doc))
            {
                envelopes.Add(Deserialize(doc));
            }
        }

        return envelopes;
    }
}
