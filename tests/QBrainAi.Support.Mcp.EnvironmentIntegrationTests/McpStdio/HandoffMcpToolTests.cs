using System.Reflection;
using System.Text.Json;
using QBrainAi.Cqrs.Mvvm;
using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Services.AgentHelp;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;
using NSubstitute;
using QBrainAi.Tests.Shared;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.McpStdio;

/// <summary>
/// TEST-HANDOFF-006: native handoff MCP tools and public-surface inventory.
/// </summary>
/// <remarks>2026-11-07, TEST-HANDOFF-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (McpStdio/HandoffMcpToolTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases and the helpers only they use (FindRepoRoot and FindPrimaryRepoRoot) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class HandoffMcpToolTestsBase in McpStdio/HandoffMcpToolTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class HandoffMcpToolTests : HandoffMcpToolTestsBase, IDisposable
{
    /// <summary>TEST-HANDOFF-006: native tools, Director aliases, and plugin skill exist.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void PublicSurfaces_ExposeIngestGetAndApprove()
    {
        var toolNames = typeof(FwhMcpTools)
            .GetMethods()
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("handoff_ingest", toolNames);
        Assert.Contains("handoff_get", toolNames);
        Assert.Contains("handoff_approve", toolNames);

        var aliases = typeof(HandoffIngestDirectorCommand).Assembly
            .GetTypes()
            .Select(type => type.GetCustomAttribute<ViewModelCommandAttribute>()?.Alias)
            .Where(alias => alias is not null)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("handoff-ingest", aliases);
        Assert.Contains("handoff-get", aliases);
        Assert.Contains("handoff-approve", aliases);

        var executor = Substitute.For<IHandoffDirectorExecutor>();
        Assert.NotNull(new HandoffIngestDirectorCommand(executor).PrimaryCommand);
        Assert.NotNull(new HandoffGetDirectorCommand(executor).PrimaryCommand);
        Assert.NotNull(new HandoffApproveDirectorCommand(executor).PrimaryCommand);

        var skill = Path.Combine(FindRepoRoot(), "plugins", "core", "skills", "handoff", "SKILL.md");
        Assert.True(File.Exists(skill), skill);
        var skillText = File.ReadAllText(skill);
        Assert.Contains("workflow.handoff.ingest", skillText, StringComparison.Ordinal);
    }

    /// <summary>P2-6: plugin-sync artifact matches the canonical core handoff skill bytes.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void PluginSync_HandoffSkill_MatchesCoreArtifact()
    {
        var root = FindRepoRoot();
        var primary = FindPrimaryRepoRoot(root);
        var core = Path.Combine(root, "plugins", "core", "skills", "handoff", "SKILL.md");
        var grok = Path.Combine(Directory.GetParent(primary)!.FullName, "mcpserver-grok-plugin", "skills", "handoff", "SKILL.md");
        Assert.True(File.Exists(core), core);
        Assert.True(File.Exists(grok), grok);
        Assert.Equal(
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(core))),
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(grok))));
    }

    private static string FindRepoRoot() => TestRepositoryRoot.Resolve();

    private static string FindPrimaryRepoRoot(string repoRoot)
    {
        var gitFile = Path.Combine(repoRoot, ".git");
        if (!File.Exists(gitFile))
            return repoRoot;

        var gitDirLine = File.ReadLines(gitFile).FirstOrDefault(line => line.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase));
        if (gitDirLine is null)
            return repoRoot;

        var gitDir = gitDirLine["gitdir:".Length..].Trim();
        if (!Path.IsPathRooted(gitDir))
            gitDir = Path.GetFullPath(Path.Combine(repoRoot, gitDir));

        var main = Directory.GetParent(gitDir)?.Parent?.Parent;
        if (main is not null && File.Exists(Path.Combine(main.FullName, "QBrainAi.sln")))
            return main.FullName;

        return repoRoot;
    }
}
