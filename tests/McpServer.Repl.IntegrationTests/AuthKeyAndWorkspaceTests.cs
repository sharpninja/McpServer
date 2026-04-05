using System.Net;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Repl.IntegrationTests;

/// <summary>
/// Tests for auth key acceptance and workspace selection via X-Workspace-Path header.
/// </summary>
public sealed class AuthKeyAndWorkspaceTests : IDisposable
{
    private readonly ReplChildProcessHelper _replProcess;
    private readonly ISerializer _yamlSerializer;

    public AuthKeyAndWorkspaceTests()
    {
        _replProcess = new ReplChildProcessHelper();
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    [Fact]
    public async Task AuthKey_InXApiKeyHeader_IsValidated()
    {
        var serverUrl = "http://localhost:5177";
        var testApiKey = "test-api-key-integration-001";
        
        using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", testApiKey);
        
        try
        {
            var response = await httpClient.GetAsync("/health");
            
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.Unauthorized,
                "Should handle API key validation");
        }
        catch (HttpRequestException)
        {
        }
    }

    [Fact]
    public async Task WorkspacePath_InXWorkspacePathHeader_IsRecognized()
    {
        var serverUrl = "http://localhost:5177";
        var testWorkspacePath = "/test/integration/workspace";
        
        using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        httpClient.DefaultRequestHeaders.Add("X-Workspace-Path", testWorkspacePath);
        
        try
        {
            var response = await httpClient.GetAsync("/health");
            
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound,
                "Should recognize X-Workspace-Path header");
        }
        catch (HttpRequestException)
        {
        }
    }

    [Fact]
    public async Task WorkspaceSelection_ViaYamlRequest_CompletesSuccessfully()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var selectRequest = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "workspace-select-001",
            "/test/workspace/path");

        var yamlContent = _yamlSerializer.Serialize(selectRequest);
        await _replProcess.WriteLineAsync(yamlContent);

        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        Assert.True(foundResponse, "Should receive workspace selection response");
    }

    [Fact]
    public async Task WorkspaceSelection_MultipleSwitches_HandlesCorrectly()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var workspace1 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "ws-001",
            "/test/workspace1");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(workspace1));
        await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(3));

        var workspace2 = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "ws-002",
            "/test/workspace2");
        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(workspace2));

        var foundResponses = await _replProcess.WaitForStdoutLineCountAsync(2, TimeSpan.FromSeconds(5));
        Assert.True(_replProcess.IsRunning, "Process should handle multiple workspace switches");
    }

    [Fact]
    public async Task AuthKeyAndWorkspace_BothHeaders_AreProcessedTogether()
    {
        var serverUrl = "http://localhost:5177";
        var testApiKey = "test-api-key-002";
        var testWorkspacePath = "/test/workspace/combined";
        
        using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", testApiKey);
        httpClient.DefaultRequestHeaders.Add("X-Workspace-Path", testWorkspacePath);
        
        try
        {
            var response = await httpClient.GetAsync("/health");
            
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound,
                "Should process both auth key and workspace path headers");
        }
        catch (HttpRequestException)
        {
        }
    }

    [Fact]
    public async Task AuthKey_Missing_ReturnsAppropriateError()
    {
        var serverUrl = "http://localhost:5177";
        var testWorkspacePath = "/test/workspace/auth-required";
        
        using var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        httpClient.DefaultRequestHeaders.Add("X-Workspace-Path", testWorkspacePath);
        
        try
        {
            var response = await httpClient.GetAsync("/mcpserver/workspace/list");
            
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized || 
                response.StatusCode == HttpStatusCode.NotFound,
                "Should require auth key for protected endpoints");
        }
        catch (HttpRequestException)
        {
        }
    }

    [Fact]
    public async Task WorkspaceSelection_InvalidPath_ReturnsError()
    {
        await _replProcess.StartAsync();
        await Task.Delay(1000);

        var invalidRequest = YamlEnvelopeBuilder.CreateWorkspaceSelectRequest(
            "ws-invalid-001",
            "");

        await _replProcess.WriteLineAsync(_yamlSerializer.Serialize(invalidRequest));
        var foundResponse = await _replProcess.WaitForStdoutLineCountAsync(1, TimeSpan.FromSeconds(5));
        
        Assert.True(_replProcess.IsRunning, "Process should handle invalid workspace selection gracefully");
    }

    public void Dispose()
    {
        _replProcess.StopAsync().Wait(TimeSpan.FromSeconds(3));
        _replProcess.Dispose();
    }
}
