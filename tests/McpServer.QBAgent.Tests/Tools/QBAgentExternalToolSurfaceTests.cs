using McpServer.Client;
using McpServer.QBAgent.Tools;
using McpServer.Support.Mcp.Services;

namespace McpServer.QBAgent.Tests.Tools;

/// <summary>
/// TEST-MCP-QBTOOLS-001: Verifies the QBAgent external tool surface exposes the file/powershell/bash/git
/// primitives by name and that they are NOT mcp_-prefixed, so the QuadBrain interceptor treats them as external
/// tools the agent executes (FR-MCP-QBTOOLS-001/007, TR-MCP-QBTOOLS-000).
/// </summary>
public sealed class QBAgentExternalToolSurfaceTests
{
    private static QBAgentToolSet BuildSurface()
    {
        using var http = new HttpClient { BaseAddress = new Uri("http://localhost:7147") };
        var client = new McpServerClient(http, new McpServerClientOptions { ApiKey = "test", WorkspacePath = "F:/work/repo" });
        var pwsh = new FakePowerShellSessionManager(new McpServer.McpAgent.PowerShellSessions.PowerShellSessionCommandResult());
        var runner = new FakeProcessRunner(new ProcessRunResult(0, "ok", null));
        return QBAgentExternalToolSurface.Create(client, pwsh, runner, "F:/work/repo", allowGitPush: false);
    }

    /// <summary>All seven external primitives are present by name.</summary>
    [Fact]
    public void Create_IncludesAllExternalPrimitives()
    {
        using var set = BuildSurface();
        var names = set.Tools.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("read_file", names);
        Assert.Contains("write_file", names);
        Assert.Contains("list_files", names);
        Assert.Contains("edit_file", names);
        Assert.Contains("run_powershell", names);
        Assert.Contains("run_bash", names);
        Assert.Contains("git", names);
    }

    /// <summary>No external tool carries the mcp_ prefix (which would route it server-side and strip it).</summary>
    [Fact]
    public void Create_NoToolIsMcpPrefixed()
    {
        using var set = BuildSurface();

        Assert.DoesNotContain(set.Tools, t => t.Name.StartsWith("mcp_", StringComparison.Ordinal));
    }

    /// <summary>Disposing the tool set is idempotent and releases the PowerShell tool without throwing.</summary>
    [Fact]
    public void Dispose_IsIdempotent()
    {
        var set = BuildSurface();

        set.Dispose();
        set.Dispose();
    }
}
