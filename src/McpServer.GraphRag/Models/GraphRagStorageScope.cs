namespace McpServer.Support.Mcp.Models;

/// <summary>
/// TR-MCP-GRAPHRAG-GLOBAL-001: Identifies whether GraphRAG artifacts are workspace-local or host-global.
/// </summary>
public enum GraphRagStorageScope
{
    /// <summary>Per-workspace GraphRAG store under the active workspace root.</summary>
    Workspace = 0,

    /// <summary>Host-global canonical corpus shared across all workspaces.</summary>
    Global = 1,
}