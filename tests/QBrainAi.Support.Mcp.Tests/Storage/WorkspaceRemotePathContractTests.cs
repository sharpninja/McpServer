using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>BDP v4 step 2: the remote-path contract against scripted outcomes.</summary>
/// <remarks>2026-11-07, TEST-MCP-PATHNORM-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: unchanged; the contract it derives from moved into Storage/WorkspaceRemotePathContract.cs beside this file, which the integration project links, and the Windows filesystem fixture moved to the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4).</remarks>
public sealed class WorkspaceRemotePathMockDataTests : WorkspaceRemotePathContractTests
{
    private static readonly Dictionary<string, string?> Outcomes = new(StringComparer.Ordinal)
    {
        [@"\\192.0.2.1\pathnorm-share\child"] = null,
        ["//192.0.2.1/pathnorm-share/child"] = null,
        [@"\\localhost\C$\Windows"] = "C:/Windows",
        ["//localhost/C$/Windows"] = "C:/Windows",
    };

    /// <inheritdoc />
    protected override Task<string> ResolveWindowsAsync(string path)
    {
        if (!Outcomes.TryGetValue(path, out var resolved))
            throw new InvalidOperationException($"No scripted resolution for '{path}'.");

        return resolved is null
            ? Task.FromException<string>(new NotSupportedException($"Remote path '{path}' is not supported."))
            : Task.FromResult(resolved);
    }
}
