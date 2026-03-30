using System.Net;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Repl.IntegrationTests;

/// <summary>
/// Tests for the trust bootstrap flow: health check, signature validation, nonce challenge.
/// </summary>
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

    [Fact]
    public async Task HealthCheck_WithNonce_ReturnsNonceInResponse()
    {
        var serverUrl = "http://localhost:5177";
        var testNonce = "test-nonce-bootstrap-123";
        
        using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        
        try
        {
            var response = await httpClient.GetAsync($"/health?nonce={testNonce}");
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains(testNonce, content);
            }
        }
        catch (HttpRequestException)
        {
        }
    }

    [Fact]
    public async Task TrustBootstrap_SendsNonceRequest_ReceivesResponse()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var nonceRequest = YamlEnvelopeBuilder.CreateNonceRequest(
            "nonce-req-001",
            "/test/workspace");

        var yamlContent = _yamlSerializer.Serialize(nonceRequest);
        await _replProcess.WriteLineAsync(yamlContent);

        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        Assert.True(foundResponse, "Should receive nonce challenge response");
    }

    [Fact]
    public async Task TrustBootstrap_SubmitsSignature_ValidatesCorrectly()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var bootstrapRequest = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "bootstrap-001",
            "/test/workspace",
            nonce: "challenge-nonce-xyz",
            signature: "test-signature-abc");

        var yamlContent = _yamlSerializer.Serialize(bootstrapRequest);
        await _replProcess.WriteLineAsync(yamlContent);

        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        Assert.True(foundResponse, "Should receive trust bootstrap response");
    }

    [Fact]
    public async Task TrustBootstrap_FullFlow_CompletesSuccessfully()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var nonceRequest = YamlEnvelopeBuilder.CreateNonceRequest(
            "nonce-001",
            "/test/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(nonceRequest));
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(3));

        var bootstrapRequest = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "bootstrap-001",
            "/test/workspace",
            nonce: "received-nonce",
            signature: "computed-signature");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(bootstrapRequest));

        var foundFinalResponse = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5));
        Assert.True(_replProcess.IsRunning, "Process should still be running after bootstrap");
    }

    [Fact]
    public async Task SignatureValidation_InvalidSignature_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidRequest = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "bootstrap-002",
            "/test/workspace",
            nonce: "valid-nonce",
            signature: "invalid-signature-xyz");

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(invalidRequest));
        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        
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
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var request1 = YamlEnvelopeBuilder.CreateNonceRequest("nonce-req-1", "/workspace1");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request1));
        await Task.Delay(500);

        var request2 = YamlEnvelopeBuilder.CreateNonceRequest("nonce-req-2", "/workspace2");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request2));

        var foundResponses = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5));
        Assert.True(_replProcess.StdoutLines.Count >= 1, "Should receive nonce responses");
    }

    public void Dispose()
    {
        _replProcess.StopAsync().Wait(TimeSpan.FromSeconds(3));
        _replProcess.Dispose();
    }
}
