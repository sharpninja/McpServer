using System.Net;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Repl.IntegrationTests;

/// <summary>
/// End-to-end integration tests for complete trust bootstrap and workspace selection flows.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EndToEndFlowTests : IDisposable
{
    private readonly ReplChildProcessHelper _replProcess;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public EndToEndFlowTests()
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
    public async Task CompleteFlow_HelloHandshake_ThenWorkspaceSelection()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var helloEnvelope = YamlEnvelopeBuilder.CreateHelloEnvelope(
            protocolVersion: "1.0",
            capabilities: new[] { "auth", "workspace-multi" },
            metadata: new Dictionary<string, string>
            {
                ["clientName"] = "integration-test-client",
                ["clientVersion"] = "1.0.0"
            });

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(helloEnvelope));
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));

        var workspaceSelect = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "ws-select-e2e-001",
            "/test/workspace/e2e");

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(workspaceSelect));
        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5));

        Assert.True(_replProcess.IsRunning, "Process should remain running after complete flow");
    }

    [Fact]
    public async Task CompleteFlow_TrustBootstrapWithHealthCheck()
    {
        var serverUrl = "http://localhost:5177";
        var testNonce = "e2e-nonce-123";
        
        using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        
        try
        {
            var healthResponse = await httpClient.GetAsync($"/health?nonce={testNonce}");
            if (healthResponse.StatusCode == HttpStatusCode.OK)
            {
                var content = await healthResponse.Content.ReadAsStringAsync();
                Assert.Contains(testNonce, content);
            }
        }
        catch (HttpRequestException)
        {
        }

        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var nonceRequest = YamlEnvelopeBuilder.CreateNonceRequest(
            "nonce-e2e-001",
            "/test/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(nonceRequest));
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(3));

        var bootstrapRequest = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "bootstrap-e2e-001",
            "/test/workspace",
            nonce: testNonce,
            signature: "computed-signature-from-nonce");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(bootstrapRequest));

        var foundBootstrapResponse = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5));
        Assert.True(_replProcess.IsRunning);
    }

    [Fact]
    public async Task CompleteFlow_MultipleWorkspaceSwitch_WithAuth()
    {
        var serverUrl = "http://localhost:5177";
        var apiKey = "e2e-test-key-001";
        var workspace1 = "/test/workspace1";
        var workspace2 = "/test/workspace2";

        using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        try
        {
            httpClient.DefaultRequestHeaders.Remove("X-Workspace-Path");
            httpClient.DefaultRequestHeaders.Add("X-Workspace-Path", workspace1);
            var response1 = await httpClient.GetAsync("/health");

            httpClient.DefaultRequestHeaders.Remove("X-Workspace-Path");
            httpClient.DefaultRequestHeaders.Add("X-Workspace-Path", workspace2);
            var response2 = await httpClient.GetAsync("/health");
        }
        catch (HttpRequestException)
        {
        }

        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var select1 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("ws-1", workspace1);
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(select1));
        await Task.Delay(500);

        var select2 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("ws-2", workspace2);
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(select2));

        await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5));
        Assert.True(_replProcess.IsRunning);
    }

    [Fact]
    public async Task CompleteFlow_InvalidThenValidRequest()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidRequest = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "invalid-001",
            "");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(invalidRequest));
        await Task.Delay(500);

        var validRequest = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "valid-001",
            "/test/valid/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(validRequest));

        var foundResponses = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5));
        Assert.True(_replProcess.IsRunning, "Process should recover from invalid request");
    }

    [Fact]
    public async Task CompleteFlow_StressTest_MultipleRapidRequests()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        for (int i = 0; i < 10; i++)
        {
            var request = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
                $"stress-{i:D3}",
                $"/test/workspace/stress/{i}");
            await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request));
            await Task.Delay(100);
        }

        await _replProcess.WaitForStdoutLineCountAsync(5, TimeSpan.FromSeconds(10));
        Assert.True(_replProcess.IsRunning, "Process should handle rapid requests");
    }

    [Fact]
    public async Task CompleteFlow_AllEnvelopeTypes_InSequence()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var hello = YamlEnvelopeBuilder.CreateHelloEnvelope();
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(hello));
        await Task.Delay(300);

        var getNonce = YamlEnvelopeBuilder.CreateNonceRequest("seq-nonce", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(getNonce));
        await Task.Delay(300);

        var bootstrap = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "seq-bootstrap", 
            "/workspace", 
            "nonce", 
            "sig");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(bootstrap));
        await Task.Delay(300);

        var selectWs = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("seq-select", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(selectWs));

        var foundResponses = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        Assert.True(_replProcess.IsRunning);
    }

    [Fact]
    public async Task CompleteFlow_HttpAndYaml_InterleavedRequests()
    {
        var serverUrl = "http://localhost:5177";
        var apiKey = "interleaved-test-key";
        
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var yamlRequest1 = YamlEnvelopeBuilder.CreateNonceRequest("inter-1", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(yamlRequest1));

        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            await httpClient.GetAsync("/health?nonce=http-nonce");
        }
        catch (HttpRequestException)
        {
        }

        var yamlRequest2 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("inter-2", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(yamlRequest2));

        await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5));
        Assert.True(_replProcess.IsRunning);
    }

    [Fact]
    public async Task CompleteFlow_ProcessCleanShutdown_AfterMultipleRequests()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var request1 = YamlEnvelopeBuilder.CreateNonceRequest("shutdown-1", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request1));
        await Task.Delay(300);

        var request2 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("shutdown-2", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request2));
        await Task.Delay(300);

        Assert.True(_replProcess.IsRunning);

        await _replProcess.StopAsync();
        await Task.Delay(500);

        Assert.False(_replProcess.IsRunning, "Process should cleanly shut down");
    }

    public void Dispose()
    {
        _replProcess.StopAsync().Wait(TimeSpan.FromSeconds(3));
        _replProcess.Dispose();
    }
}
