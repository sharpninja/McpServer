using System.Text;
using Xunit;

namespace McpServer.Workspace.Validation;

/// <summary>
/// Shared fixture that provides an HttpClient configured to hit the live MCP Server
/// on port 7147, plus helper methods for Base64URL key encoding.
/// </summary>
public sealed class WorkspaceEndpointFixture : IDisposable
{
    /// <summary>Base URL of the running MCP Server.</summary>
    public const string BaseUrl = "http://localhost:7147";

    /// <summary>Route prefix for workspace endpoints.</summary>
    public const string WorkspaceRoute = "/mcpserver/workspace";

    /// <summary>Pre-configured HTTP client targeting the live service.</summary>
    public HttpClient Client { get; }

    /// <summary>Optional API key. Set via MCPSERVER_APIKEY environment variable.</summary>
    public string? ApiKey { get; }

    /// <summary>Initializes a new instance.</summary>
    public WorkspaceEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        ApiKey = Environment.GetEnvironmentVariable("MCPSERVER_APIKEY");
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            Client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }
    }

    /// <summary>Encode a workspace path to a Base64URL key for use in route segments.</summary>
    public static string EncodeKey(string path)
    {
        var bytes = Encoding.UTF8.GetBytes(path.Trim());
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Generate a unique test workspace path that won't collide with real data.</summary>
    public static string GenerateTestWorkspacePath()
    {
        return $@"C:\Temp\McpAuditTest_{Guid.NewGuid():N}";
    }

    /// <summary>Disposes resources.</summary>
    public void Dispose() => Client.Dispose();
}

/// <summary>xUnit collection definition so all workspace tests share the same fixture.</summary>
[CollectionDefinition("WorkspaceEndpoint")]
public sealed class WorkspaceEndpointCollection : ICollectionFixture<WorkspaceEndpointFixture>;
