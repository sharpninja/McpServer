using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MEMORY-JOBS-002 / TEST-MCP-MEMORY-013: CQRS command for a scheduled consolidate tick.
/// S4 Red ships the port only; no handler is registered until S4 Green.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="DryRun">Optional dry-run override. Null uses default true.</param>
/// <param name="ForceOverlap">When true, simulates an overlapping tick for lock tests.</param>
public sealed record RunConsolidateJobCommand(
    string WorkspacePath,
    bool? DryRun = null,
    bool ForceOverlap = false) : ICommand<MemoryConsolidateJobResult>;
