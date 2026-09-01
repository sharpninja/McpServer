using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-PRODUCT-003 / FR-MCP-PRODUCT-001: In-process HTTP launch for product create and local effective.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProductsLaunchTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Starts an isolated in-process host.</summary>
    public ProductsLaunchTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<WorkspaceTokenService>().GetToken(_factory.WorkspacePath)
                    ?? throw new InvalidOperationException("Workspace API key was not generated.");
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", token);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    /// <summary>POST /mcpserver/products returns key and ownerWorkspaceId; effective local is a sane envelope.</summary>
    [Fact]
    public async Task PostProduct_AndGetEffectiveLocal_Succeeds()
    {
        var create = await _client.PostAsJsonAsync(
            "/mcpserver/products",
            new { key = "PROD-MCPSERVER", name = "McpServer" },
            TestContext.Current.CancellationToken);
        var body = await create.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(create.IsSuccessStatusCode, body);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("PROD-MCPSERVER", doc.RootElement.GetProperty("key").GetString());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("ownerWorkspaceId").GetString()));

        var effective = await _client.GetAsync(
            "/mcpserver/requirements/effective?productScope=local",
            TestContext.Current.CancellationToken);
        var effectiveBody = await effective.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, effective.StatusCode);
        using var effectiveDoc = JsonDocument.Parse(effectiveBody);
        Assert.True(
            effectiveDoc.RootElement.TryGetProperty("functional", out _)
            || effectiveDoc.RootElement.TryGetProperty("Functional", out _));

        WriteLaunchReceipt(
            Environment.GetEnvironmentVariable("MCP_PRODUCTS_LAUNCH_RECEIPT"),
            body,
            effectiveBody);
    }

    /// <summary>Second independent host launch for verification step 6.</summary>
    [Fact]
    public async Task PostProduct_AndGetEffectiveLocal_SecondLaunch_Succeeds()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var token = factory.Services.GetRequiredService<WorkspaceTokenService>().GetToken(factory.WorkspacePath)
                    ?? throw new InvalidOperationException("Workspace API key was not generated.");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", token);

        var create = await client.PostAsJsonAsync(
            "/mcpserver/products",
            new { key = "PROD-MCPSERVER", name = "McpServer" },
            TestContext.Current.CancellationToken);
        var body = await create.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(create.IsSuccessStatusCode, body);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("PROD-MCPSERVER", doc.RootElement.GetProperty("key").GetString());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("ownerWorkspaceId").GetString()));

        var effective = await client.GetAsync(
            "/mcpserver/requirements/effective?productScope=local",
            TestContext.Current.CancellationToken);
        var effectiveBody = await effective.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, effective.StatusCode);
        using var effectiveDoc = JsonDocument.Parse(effectiveBody);
        Assert.True(
            effectiveDoc.RootElement.TryGetProperty("functional", out _)
            || effectiveDoc.RootElement.TryGetProperty("Functional", out _));

        WriteLaunchReceipt(
            Environment.GetEnvironmentVariable("MCP_PRODUCTS_LAUNCH_RECEIPT_2"),
            body,
            effectiveBody);
    }

    private static void WriteLaunchReceipt(string? path, string createBody, string effectiveBody)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "POST /mcpserver/products" + Environment.NewLine + createBody + Environment.NewLine
            + "GET /mcpserver/requirements/effective?productScope=local" + Environment.NewLine + effectiveBody + Environment.NewLine);
    }
}
