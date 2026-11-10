using System.Diagnostics;
using System.Reflection;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-USECASE-018 AC-006: the per-test state and helpers that both halves of TunnelProviderTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from TunnelProviderTests (_processRunner, _processEnvironment, .ctor and CreateOptions) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class TunnelProviderTestsBase
{
    internal readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    internal readonly IProcessEnvironmentService _processEnvironment = Substitute.For<IProcessEnvironmentService>();

    /// <summary>
    /// TR-MCP-TUN-004: makes <see cref="IProcessEnvironmentService.ResolveExecutable"/> echo the requested
    /// file name so the existing stubs keyed on the bare <c>ngrok</c> command keep matching.
    /// </summary>
    protected TunnelProviderTestsBase()
    {
        _processEnvironment
            .ResolveExecutable(Arg.Any<ProcessStartInfo>(), Arg.Any<string>())
            .Returns(ci => ci.ArgAt<string>(1));
    }

    internal static Microsoft.Extensions.Options.IOptions<TunnelOptions> CreateOptions(Action<TunnelOptions>? configure = null)
    {
        var opts = new TunnelOptions { Port = 7147 };
        configure?.Invoke(opts);
        return Microsoft.Extensions.Options.Options.Create(opts);
    }
}
