using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.TestContracts;

namespace QBrainAi.Support.Mcp.IntegrationTests.Controllers;

/// <summary>Exercises the server's restricted default-key registration over its real HTTP pipeline.</summary>
/// <remarks>2026-11-26: FR-MCP-009, TR-MCP-WS-004, FR-MCP-132 ac-1. Verify the
/// actual issued key, both routes and the absence of denied effects in an isolated host.</remarks>
[Trait("Category", "Integration")]
public sealed class DefaultKeyRegistrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>Uses the existing isolated QBrain host and database fixture.</summary>
    public DefaultKeyRegistrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    /// <summary>Both supported aliases register a fresh workspace and write its marker.</summary>
    [Theory]
    [InlineData("/qbrainai/workspace", "127.0.0.1", true)]
    [InlineData("/mcpserver/workspace", "::1", false)]
    public async Task IssuedKey_RegistersWorkspaceAndWritesMarker(string route, string peer, bool enabled)
    {
        var path = NewWorkspacePath();
        var ct = TestContext.Current.CancellationToken;
        var key = await GetIssuedKeyAsync(ct);
        Assert.False(File.Exists(MarkerPath(path)));
        using var body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new
        {
            workspacePath = path,
            name = "director-registration-test",
            isEnabled = enabled
        }));
        var response = await SendAsync("POST", route, peer, key, body, ct);
        var workspace = await FindWorkspaceAsync(path, ct);
        DefaultKeyRegistrationContract.Verify(HttpStatusCode.Created, true,
            new((HttpStatusCode)response.Response.StatusCode, workspace is not null, File.Exists(MarkerPath(path))));
        Assert.NotNull(workspace);
        Assert.Equal(enabled, workspace.IsEnabled);
        Assert.Equal("director-registration-test", workspace.Name);
        Assert.False(workspace.IsPrimary);
    }

    /// <summary>Invalid credential placement, peer and payload never register the workspace.</summary>
    [Theory]
    [InlineData("192.0.2.10", "header", null, 403)]
    [InlineData(null, "header", null, 403)]
    [InlineData("127.0.0.1", "invalid", null, 401)]
    [InlineData("127.0.0.1", "query", null, 403)]
    [InlineData("127.0.0.1", "header", "isPrimary", 403)]
    [InlineData("127.0.0.1", "header", "RunAs", 403)]
    [InlineData("127.0.0.1", "header", "dumpPath", 403)]
    [InlineData("127.0.0.1", "header", "unknown", 403)]
    public async Task RestrictedGrant_DeniesRegistrationWithoutSideEffects(
        string? peer, string placement, string? forbiddenField, int status)
    {
        var path = NewWorkspacePath();
        var ct = TestContext.Current.CancellationToken;
        var key = await GetIssuedKeyAsync(ct);
        var payload = new Dictionary<string, object?> { ["workspacePath"] = path };
        if (forbiddenField is not null)
            payload[forbiddenField] = forbiddenField == "isPrimary" ? false : null;

        using var body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(payload));
        var route = placement == "query" ? $"/qbrainai/workspace?api_key={Uri.EscapeDataString(key)}" : "/qbrainai/workspace";
        var header = placement switch
        {
            "invalid" => Guid.NewGuid().ToString("N"),
            "header" => key,
            _ => null
        };
        var response = await SendAsync("POST", route, peer, header, body, ct);
        var workspace = await FindWorkspaceAsync(path, ct);
        DefaultKeyRegistrationContract.Verify((HttpStatusCode)status, false,
            new((HttpStatusCode)response.Response.StatusCode, workspace is not null, File.Exists(MarkerPath(path))));
        Assert.False(Directory.Exists(path));
    }

    /// <summary>The additional grant leaves authenticated reads and the existing write boundaries intact.</summary>
    [Theory]
    [InlineData("GET", "/qbrainai/workspace", 200)]
    [InlineData("PUT", "/qbrainai/workspace/{key}", 403)]
    [InlineData("DELETE", "/mcpserver/workspace/{key}", 403)]
    [InlineData("POST", "/qbrainai/workspace/markers/regenerate", 403)]
    [InlineData("POST", "/mcpserver/workspace/{key}/init", 403)]
    [InlineData("POST", "/qbrainai/todo", 403)]
    public async Task IssuedKey_PreservesExistingOtherPermissions(string method, string route, int status)
    {
        var ct = TestContext.Current.CancellationToken;
        var key = await GetIssuedKeyAsync(ct);
        var existingPath = _factory.WorkspacePath;
        var pathKey = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(existingPath));
        var existing = await FindWorkspaceAsync(existingPath, ct);
        Assert.NotNull(existing);
        using var body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new { name = "do-not-change" }));
        var response = await SendAsync(method, route.Replace("{key}", pathKey, StringComparison.Ordinal),
            "127.0.0.1", key, body, ct);
        Assert.Equal(status, response.Response.StatusCode);
        var after = await FindWorkspaceAsync(existingPath, ct);
        Assert.NotNull(after);
        Assert.Equal(existing.Name, after.Name);
    }

    /// <summary>Obtains the actual ephemeral key without exposing it to test output.</summary>
    private async Task<string> GetIssuedKeyAsync(CancellationToken ct)
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/api-key", UriKind.Relative), ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var key = json.RootElement.GetProperty("apiKey").GetString();
        Assert.False(string.IsNullOrWhiteSpace(key));
        return key!;
    }

    /// <summary>Sends a request through the real host with an explicit transport peer address.</summary>
    private Task<HttpContext> SendAsync(
        string method, string target, string? peer, string? key, Stream body, CancellationToken ct)
    {
        var uri = new Uri(_factory.Server.BaseAddress, target);
        return _factory.Server.SendAsync(context =>
        {
            context.Connection.RemoteIpAddress = peer is null ? null : IPAddress.Parse(peer);
            context.Request.Method = method;
            context.Request.Path = uri.AbsolutePath;
            context.Request.QueryString = new QueryString(uri.Query);
            if (method != "GET")
            {
                context.Request.Body = body;
                context.Request.ContentType = "application/json";
                context.Request.ContentLength = body.Length;
            }

            if (key is not null)
                context.Request.Headers["X-Api-Key"] = key;
        }, ct);
    }

    /// <summary>Reads workspace state through the registered service after the HTTP request.</summary>
    private async Task<WorkspaceDto?> FindWorkspaceAsync(string path, CancellationToken ct)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IWorkspaceService>().GetAsync(path, ct);
    }

    /// <summary>Returns a unique child path inside the fixture so normal fixture disposal owns cleanup.</summary>
    private string NewWorkspacePath() => Path.Combine(_factory.WorkspacePath, $"director-{Guid.NewGuid():N}");

    /// <summary>Returns the standard marker location without reading or printing credentials.</summary>
    private static string MarkerPath(string path) => Path.Combine(path, "AGENTS-README-FIRST.yaml");
}
