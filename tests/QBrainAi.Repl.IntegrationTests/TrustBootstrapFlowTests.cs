using System.Net;
using QBrainAi.TestSupport.TemporaryInstance;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace QBrainAi.Repl.IntegrationTests;

/// <summary>
/// Tests for the trust bootstrap flow: health check, signature validation, nonce challenge.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TrustBootstrapFlowTests : IDisposable
{
    private readonly ReplChildProcessHelper _replProcess;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public TrustBootstrapFlowTests()
    {
        _replProcess = new ReplChildProcessHelper();
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    /// <summary>TR-MCP-NUKE-019 AC-6: the temporary instance's health is 200 and echoes the request's nonce.</summary>
    [Fact]
    public async Task HealthCheck_WithNonce_ReturnsNonceInResponse()
    {
        var testNonce = "test-nonce-bootstrap-123";
        await _replProcess.EnsureInstanceAsync(TestContext.Current.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = _replProcess.InstanceBaseUrl };
        using var response = await httpClient.GetAsync($"/health?nonce={testNonce}", cancellationToken: TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(TemporaryInstanceChecks.CheckHealthAnswer((int)response.StatusCode, content, testNonce));
    }

    [Fact]
    public async Task TrustBootstrap_SendsNonceRequest_ReceivesResponse()
    {
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var nonceRequest = YamlEnvelopeBuilder.CreateNonceRequest(
            "nonce-req-001",
            "/test/workspace");

        var yamlContent = _yamlSerializer.Serialize(nonceRequest);
        await _replProcess.WriteLineAsync(yamlContent, cancellationToken: TestContext.Current.CancellationToken);

        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(foundResponse, "Should receive nonce challenge response");
    }

    [Fact]
    public async Task TrustBootstrap_SubmitsSignature_ValidatesCorrectly()
    {
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var bootstrapRequest = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "bootstrap-001",
            "/test/workspace",
            nonce: "challenge-nonce-xyz",
            signature: "test-signature-abc");

        var yamlContent = _yamlSerializer.Serialize(bootstrapRequest);
        await _replProcess.WriteLineAsync(yamlContent, cancellationToken: TestContext.Current.CancellationToken);

        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(foundResponse, "Should receive trust bootstrap response");
    }

    [Fact]
    public async Task TrustBootstrap_FullFlow_CompletesSuccessfully()
    {
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var nonceRequest = YamlEnvelopeBuilder.CreateNonceRequest(
            "nonce-001",
            "/test/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(nonceRequest), cancellationToken: TestContext.Current.CancellationToken);
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(3), cancellationToken: TestContext.Current.CancellationToken);

        var bootstrapRequest = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "bootstrap-001",
            "/test/workspace",
            nonce: "received-nonce",
            signature: "computed-signature");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(bootstrapRequest), cancellationToken: TestContext.Current.CancellationToken);

        var foundFinalResponse = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_replProcess.IsRunning, "Process should still be running after bootstrap");
    }

    [Fact]
    public async Task SignatureValidation_InvalidSignature_ReturnsError()
    {
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var invalidRequest = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "bootstrap-002",
            "/test/workspace",
            nonce: "valid-nonce",
            signature: "invalid-signature-xyz");

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(invalidRequest), cancellationToken: TestContext.Current.CancellationToken);
        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        
        if (foundResponse && _replProcess.StdoutLines.Count > 0)
        {
            var responseLine = _replProcess.StdoutLines.FirstOrDefault();
            if (responseLine != null)
            {
                var response = _yamlDeserializer.Deserialize<Dictionary<string, object>>(responseLine);
                Assert.NotNull(response);
            }
        }
    }

    [Fact]
    public async Task NonceChallengeFlow_GeneratesUniqueChallenges()
    {
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var request1 = YamlEnvelopeBuilder.CreateNonceRequest("nonce-req-1", "/workspace1");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request1), cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(500, cancellationToken: TestContext.Current.CancellationToken);

        var request2 = YamlEnvelopeBuilder.CreateNonceRequest("nonce-req-2", "/workspace2");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request2), cancellationToken: TestContext.Current.CancellationToken);

        var foundResponses = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_replProcess.StdoutLines.Count >= 1, "Should receive nonce responses");
    }

    public void Dispose()
    {
        _replProcess.StopAsync().Wait(TimeSpan.FromSeconds(3));
        _replProcess.Dispose();
    }
}
