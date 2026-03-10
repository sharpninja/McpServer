using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// TEST-MCP-089: Verifies that <see cref="DesktopClient"/> posts structured desktop-launch
/// requests to the authenticated MCP Server HTTP endpoint and deserializes the typed launch
/// result returned by the server.
/// The tests use <see cref="MockHttpHandler"/> so request paths and serialized JSON can be
/// inspected deterministically without launching real local programs.
/// </summary>
public sealed class DesktopClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    /// <summary>
    /// TEST-MCP-089: Verifies that <see cref="DesktopClient.LaunchAsync"/> targets
    /// <c>/mcpserver/desktop/launch</c>, preserves the structured launch payload, and returns
    /// the typed launch result.
    /// The test uses representative executable, environment-variable, and wait-for-exit data so
    /// hosted-agent desktop-launch calls can rely on the same client contract.
    /// </summary>
    [Fact]
    public async Task LaunchAsync_PostsStructuredDesktopLaunchRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"processId":4242,"exitCode":0}""");
        using var http = new HttpClient(handler);
        var client = new DesktopClient(http, DefaultOptions);

        var result = await client.LaunchAsync(
            new DesktopLaunchRequest
            {
                ExecutablePath = @"C:\Windows\System32\cmd.exe",
                Arguments = "/c exit 0",
                WorkingDirectory = @"C:\Windows\System32",
                EnvironmentVariables = new Dictionary<string, string> { ["TEST_ENV"] = "true" },
                CreateNoWindow = true,
                WindowStyle = "Hidden",
                WaitForExit = true,
                TimeoutMs = 5000
            });

        Assert.True(result.Success);
        Assert.Equal(4242, result.ProcessId);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/mcpserver/desktop/launch", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"executablePath\":\"C:\\\\Windows\\\\System32\\\\cmd.exe\"", handler.LastRequestBody!);
        Assert.Contains("\"TEST_ENV\":\"true\"", handler.LastRequestBody!);
        Assert.Contains("\"windowStyle\":\"Hidden\"", handler.LastRequestBody!);
        Assert.Contains("\"waitForExit\":true", handler.LastRequestBody!);
    }
}
