using System.Diagnostics;
using System.Reflection;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-USECASE-018 AC-006: the per-test state and helpers that both halves of FrpTunnelProviderTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from FrpTunnelProviderTests (_processRunner, CreateSut and CreateOptions) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class FrpTunnelProviderTestsBase
{
    internal readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();

    internal FrpTunnelProvider CreateSut(Action<TunnelOptions>? configure = null)
        => new(CreateOptions(configure), _processRunner, NullLogger<FrpTunnelProvider>.Instance);

    internal static IOptions<TunnelOptions> CreateOptions(Action<TunnelOptions>? configure = null)
    {
        var options = new TunnelOptions { Port = 7147 };
        configure?.Invoke(options);
        return Microsoft.Extensions.Options.Options.Create(options);
    }
}
