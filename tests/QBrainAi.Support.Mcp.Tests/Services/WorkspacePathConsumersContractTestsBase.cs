using QBrainAi.Client;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Middleware;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-FED-PATH-001: the host constant and the normalizer hook that both contracts named WorkspacePathConsumersContractTests use (the unit contract in QBrainAi.Support.Mcp.Tests, the integration contract in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from WorkspacePathConsumersContractTests (WindowsHost and Normalizer) when that contract split into a unit contract and an integration contract (slice S8, hostile validation finding R14-F4; ClaudeCode decision on the S7 leftover), so both contracts use one definition instead of a copy (DRY); only the accessibility of WindowsHost changed, from private to internal.</remarks>
public abstract class WorkspacePathConsumersContractTestsBase
{
    internal const WorkspacePathPlatform WindowsHost = WorkspacePathPlatform.Windows;

    /// <summary>Normalizer for a simulated host of the given platform.</summary>
    protected abstract IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform);
}
