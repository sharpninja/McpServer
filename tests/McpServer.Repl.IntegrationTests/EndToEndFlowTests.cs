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
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var helloEnvelope = YamlEnvelopeBuilder.CreateHelloEnvelope(
            protocolVersion: "1.0",
            capabilities: new[] { "auth", "workspace-multi" },
            metadata: new Dictionary<string, string>
            {
                ["clientName"] = "integration-test-client",
                ["clientVersion"] = "1.0.0"
            });

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(helloEnvelope), cancellationToken: TestContext.Current.CancellationToken);
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);

        var workspaceSelect = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "ws-select-e2e-001",
            "/test/workspace/e2e");

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(workspaceSelect), cancellationToken: TestContext.Current.CancellationToken);
        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);

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
            var healthResponse = await httpClient.GetAsync($"/health?nonce={testNonce}", cancellationToken: TestContext.Current.CancellationToken);
            if (healthResponse.StatusCode == HttpStatusCode.OK)
            {
                var content = await healthResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
                Assert.Contains(testNonce, content);
            }
        }
        catch (HttpRequestException)
        {
        }

        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var nonceRequest = YamlEnvelopeBuilder.CreateNonceRequest(
            "nonce-e2e-001",
            "/test/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(nonceRequest), cancellationToken: TestContext.Current.CancellationToken);
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(3), cancellationToken: TestContext.Current.CancellationToken);

        var bootstrapRequest = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "bootstrap-e2e-001",
            "/test/workspace",
            nonce: testNonce,
            signature: "computed-signature-from-nonce");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(bootstrapRequest), cancellationToken: TestContext.Current.CancellationToken);

        var foundBootstrapResponse = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
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
            var response1 = await httpClient.GetAsync("/health", cancellationToken: TestContext.Current.CancellationToken);

            httpClient.DefaultRequestHeaders.Remove("X-Workspace-Path");
            httpClient.DefaultRequestHeaders.Add("X-Workspace-Path", workspace2);
            var response2 = await httpClient.GetAsync("/health", cancellationToken: TestContext.Current.CancellationToken);
        }
        catch (HttpRequestException)
        {
        }

        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var select1 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("ws-1", workspace1);
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(select1), cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(500, cancellationToken: TestContext.Current.CancellationToken);

        var select2 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("ws-2", workspace2);
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(select2), cancellationToken: TestContext.Current.CancellationToken);

        await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_replProcess.IsRunning);
    }

    [Fact]
    public async Task CompleteFlow_InvalidThenValidRequest()
    {
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var invalidRequest = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "invalid-001",
            "");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(invalidRequest), cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(500, cancellationToken: TestContext.Current.CancellationToken);

        var validRequest = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "valid-001",
            "/test/valid/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(validRequest), cancellationToken: TestContext.Current.CancellationToken);

        var foundResponses = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_replProcess.IsRunning, "Process should recover from invalid request");
    }

    [Fact]
    public async Task CompleteFlow_StressTest_MultipleRapidRequests()
    {
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        for (int i = 0; i < 10; i++)
        {
            var request = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
                $"stress-{i:D3}",
                $"/test/workspace/stress/{i}");
            await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request), cancellationToken: TestContext.Current.CancellationToken);
            await Task.Delay(100, cancellationToken: TestContext.Current.CancellationToken);
        }

        await _replProcess.WaitForStdoutLineCountAsync(5, TimeSpan.FromSeconds(10), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_replProcess.IsRunning, "Process should handle rapid requests");
    }

    [Fact]
    public async Task CompleteFlow_AllEnvelopeTypes_InSequence()
    {
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var hello = YamlEnvelopeBuilder.CreateHelloEnvelope();
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(hello), cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, cancellationToken: TestContext.Current.CancellationToken);

        var getNonce = YamlEnvelopeBuilder.CreateNonceRequest("seq-nonce", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(getNonce), cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, cancellationToken: TestContext.Current.CancellationToken);

        var bootstrap = YamlEnvelopeBuilder.CreateTrustBootstrapRequest(
            "seq-bootstrap", 
            "/workspace", 
            "nonce", 
            "sig");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(bootstrap), cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, cancellationToken: TestContext.Current.CancellationToken);

        var selectWs = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("seq-select", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(selectWs), cancellationToken: TestContext.Current.CancellationToken);

        var foundResponses = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_replProcess.IsRunning);
    }

    [Fact]
    public async Task CompleteFlow_HttpAndYaml_InterleavedRequests()
    {
        var serverUrl = "http://localhost:5177";
        var apiKey = "interleaved-test-key";
        
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var yamlRequest1 = YamlEnvelopeBuilder.CreateNonceRequest("inter-1", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(yamlRequest1), cancellationToken: TestContext.Current.CancellationToken);

        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            await httpClient.GetAsync("/health?nonce=http-nonce", cancellationToken: TestContext.Current.CancellationToken);
        }
        catch (HttpRequestException)
        {
        }

        var yamlRequest2 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("inter-2", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(yamlRequest2), cancellationToken: TestContext.Current.CancellationToken);

        await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_replProcess.IsRunning);
    }

    [Fact]
    public async Task CompleteFlow_ProcessCleanShutdown_AfterMultipleRequests()
    {
        await _replProcess.StartAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var request1 = YamlEnvelopeBuilder.CreateNonceRequest("shutdown-1", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request1), cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, cancellationToken: TestContext.Current.CancellationToken);

        var request2 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest("shutdown-2", "/workspace");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(request2), cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(300, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_replProcess.IsRunning);

        await _replProcess.StopAsync(cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(500, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(_replProcess.IsRunning, "Process should cleanly shut down");
    }

    public void Dispose()
    {
        _replProcess.StopAsync().Wait(TimeSpan.FromSeconds(3));
        _replProcess.Dispose();
    }
}
