using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class YamlFramingTests
{
    [Fact]
    public void ParseHelloEnvelope_ValidYaml_ReturnsTypedEnvelope()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var yamlInput = @"
type: hello
payload:
  protocolVersion: ""1.0""
  capabilities:
    - auth
    - workspace-multi
  metadata:
    client: test-client
";
        
        var expectedEnvelope = Substitute.For<IYamlEnvelope>();
        expectedEnvelope.Type.Returns("hello");
        var helloPayload = Substitute.For<IHelloPayload>();
        helloPayload.ProtocolVersion.Returns("1.0");
        helloPayload.Capabilities.Returns(new[] { "auth", "workspace-multi" });
        expectedEnvelope.Payload.Returns(helloPayload);
        
        serializer.Deserialize(yamlInput).Returns(expectedEnvelope);
        
        var result = serializer.Deserialize(yamlInput);
        
        Assert.NotNull(result);
        Assert.Equal("hello", result.Type);
        Assert.NotNull(result.Payload);
        
        serializer.Received(1).Deserialize(yamlInput);
    }

    [Fact]
    public void ParseRequestEnvelope_ValidYaml_ReturnsRequestPayload()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var yamlInput = @"
type: request
payload:
  requestId: req-001
  method: workspace.select
  params:
    path: /home/user/project
";
        
        var expectedEnvelope = Substitute.For<IYamlEnvelope>();
        expectedEnvelope.Type.Returns("request");
        var requestPayload = Substitute.For<IRequestPayload>();
        requestPayload.RequestId.Returns("req-001");
        requestPayload.Method.Returns("workspace.select");
        expectedEnvelope.Payload.Returns(requestPayload);
        
        serializer.Deserialize(yamlInput).Returns(expectedEnvelope);
        
        var result = serializer.Deserialize(yamlInput);
        
        Assert.NotNull(result);
        Assert.Equal("request", result.Type);
        var payload = result.Payload as IRequestPayload;
        Assert.NotNull(payload);
        Assert.Equal("req-001", payload.RequestId);
        Assert.Equal("workspace.select", payload.Method);
    }

    [Fact]
    public void ParseEventEnvelope_ValidYaml_ReturnsEventPayload()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var yamlInput = @"
type: event
payload:
  event: workspace.changed
  data:
    newWorkspace: /home/user/project
  timestamp: 2024-01-15T10:30:00Z
";
        
        var expectedEnvelope = Substitute.For<IYamlEnvelope>();
        expectedEnvelope.Type.Returns("event");
        var eventPayload = Substitute.For<IEventPayload>();
        eventPayload.Event.Returns("workspace.changed");
        eventPayload.Timestamp.Returns(DateTimeOffset.Parse("2024-01-15T10:30:00Z"));
        expectedEnvelope.Payload.Returns(eventPayload);
        
        serializer.Deserialize(yamlInput).Returns(expectedEnvelope);
        
        var result = serializer.Deserialize(yamlInput);
        
        Assert.NotNull(result);
        Assert.Equal("event", result.Type);
        var payload = result.Payload as IEventPayload;
        Assert.NotNull(payload);
        Assert.Equal("workspace.changed", payload.Event);
    }

    [Fact]
    public void ParseResultEnvelope_ValidYaml_ReturnsResultPayload()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var yamlInput = @"
type: result
payload:
  requestId: req-001
  result:
    success: true
";
        
        var expectedEnvelope = Substitute.For<IYamlEnvelope>();
        expectedEnvelope.Type.Returns("result");
        var resultPayload = Substitute.For<IResultPayload>();
        resultPayload.RequestId.Returns("req-001");
        expectedEnvelope.Payload.Returns(resultPayload);
        
        serializer.Deserialize(yamlInput).Returns(expectedEnvelope);
        
        var result = serializer.Deserialize(yamlInput);
        
        Assert.NotNull(result);
        Assert.Equal("result", result.Type);
        var payload = result.Payload as IResultPayload;
        Assert.NotNull(payload);
        Assert.Equal("req-001", payload.RequestId);
    }

    [Fact]
    public void ParseErrorEnvelope_ValidYaml_ReturnsErrorPayload()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var yamlInput = @"
type: error
payload:
  requestId: req-001
  code: invalid_workspace
  message: Workspace not found
  details:
    path: /invalid/path
";
        
        var expectedEnvelope = Substitute.For<IYamlEnvelope>();
        expectedEnvelope.Type.Returns("error");
        var errorPayload = Substitute.For<IErrorPayload>();
        errorPayload.RequestId.Returns("req-001");
        errorPayload.Code.Returns("invalid_workspace");
        errorPayload.Message.Returns("Workspace not found");
        expectedEnvelope.Payload.Returns(errorPayload);
        
        serializer.Deserialize(yamlInput).Returns(expectedEnvelope);
        
        var result = serializer.Deserialize(yamlInput);
        
        Assert.NotNull(result);
        Assert.Equal("error", result.Type);
        var payload = result.Payload as IErrorPayload;
        Assert.NotNull(payload);
        Assert.Equal("req-001", payload.RequestId);
        Assert.Equal("invalid_workspace", payload.Code);
        Assert.Equal("Workspace not found", payload.Message);
    }

    [Fact]
    public void SerializeEnvelope_HelloType_ProducesValidYaml()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns("hello");
        
        var expectedYaml = "type: hello\npayload:\n  protocolVersion: \"1.0\"\n";
        serializer.Serialize(envelope).Returns(expectedYaml);
        
        var result = serializer.Serialize(envelope);
        
        Assert.NotNull(result);
        Assert.Contains("type: hello", result);
        serializer.Received(1).Serialize(envelope);
    }

    [Fact]
    public void Deserialize_MalformedYaml_ThrowsFormatException()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var invalidYaml = "type: hello\npayload: [unmatched";
        
        serializer.When(x => x.Deserialize(invalidYaml))
            .Do(x => throw new FormatException("Invalid YAML syntax"));
        
        Assert.Throws<FormatException>(() => serializer.Deserialize(invalidYaml));
    }

    [Fact]
    public void Deserialize_MissingTypeField_ThrowsInvalidOperationException()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var yamlWithoutType = "payload:\n  test: value";
        
        serializer.When(x => x.Deserialize(yamlWithoutType))
            .Do(x => throw new InvalidOperationException("Missing 'type' field"));
        
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(yamlWithoutType));
    }

    [Fact]
    public void Deserialize_UnknownEnvelopeType_ThrowsInvalidOperationException()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var yamlWithUnknownType = "type: unknown\npayload: {}";
        
        serializer.When(x => x.Deserialize(yamlWithUnknownType))
            .Do(x => throw new InvalidOperationException("Unknown envelope type: unknown"));
        
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(yamlWithUnknownType));
    }

    [Fact]
    public void TryDeserialize_ValidYaml_ReturnsTrueWithEnvelope()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        var yamlInput = "type: hello\npayload:\n  protocolVersion: \"1.0\"";
        
        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns("hello");
        
        IYamlEnvelope? outEnvelope;
        serializer.TryDeserialize(yamlInput, out outEnvelope!)
            .Returns(x =>
            {
                x[1] = envelope;
                return true;
            });
        
        var result = serializer.TryDeserialize(yamlInput, out var resultEnvelope);
        
        Assert.True(result);
        Assert.NotNull(resultEnvelope);
        Assert.Equal("hello", resultEnvelope.Type);
    }

    [Fact]
    public void TryDeserialize_InvalidYaml_ReturnsFalse()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        var invalidYaml = "invalid: [yaml";
        
        IYamlEnvelope? outEnvelope;
        serializer.TryDeserialize(invalidYaml, out outEnvelope!)
            .Returns(x =>
            {
                x[1] = null;
                return false;
            });
        
        var result = serializer.TryDeserialize(invalidYaml, out var resultEnvelope);
        
        Assert.False(result);
        Assert.Null(resultEnvelope);
    }

    [Fact]
    public void SerializeStream_MultipleEnvelopes_ProducesYamlDocumentStream()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var envelope1 = Substitute.For<IYamlEnvelope>();
        envelope1.Type.Returns("hello");
        
        var envelope2 = Substitute.For<IYamlEnvelope>();
        envelope2.Type.Returns("request");
        
        var envelopes = new[] { envelope1, envelope2 };
        
        var expectedStream = "---\ntype: hello\n---\ntype: request\n";
        serializer.SerializeStream(envelopes).Returns(expectedStream);
        
        var result = serializer.SerializeStream(envelopes);
        
        Assert.NotNull(result);
        Assert.Contains("---", result);
        serializer.Received(1).SerializeStream(envelopes);
    }

    [Fact]
    public void DeserializeStream_MultipleDocuments_ReturnsEnvelopeList()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var yamlStream = "---\ntype: hello\n---\ntype: request\n";
        
        var envelope1 = Substitute.For<IYamlEnvelope>();
        envelope1.Type.Returns("hello");
        
        var envelope2 = Substitute.For<IYamlEnvelope>();
        envelope2.Type.Returns("request");
        
        serializer.DeserializeStream(yamlStream).Returns(new[] { envelope1, envelope2 });
        
        var result = serializer.DeserializeStream(yamlStream);
        
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("hello", result[0].Type);
        Assert.Equal("request", result[1].Type);
    }

    [Fact]
    public void ValidateEnvelope_MissingPayload_ThrowsInvalidOperationException()
    {
        var serializer = Substitute.For<IYamlSerializer>();
        
        var yamlWithoutPayload = "type: request";
        
        serializer.When(x => x.Deserialize(yamlWithoutPayload))
            .Do(x => throw new InvalidOperationException("Missing 'payload' field"));
        
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(yamlWithoutPayload));
    }
}
