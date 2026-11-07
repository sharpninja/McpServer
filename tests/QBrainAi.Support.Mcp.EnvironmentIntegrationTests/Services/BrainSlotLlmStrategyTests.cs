using System.Text.Json;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.AI;
using QBrainAi.Support.Mcp.Storage.Entities;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-LLMSTRATEGY-001: Per-role completion strategies, shared turn-context identity, and HTTP flattening.
/// </summary>
/// <remarks>
/// Requirement coverage named by requirement evidence: FR-MCP-LLMSTRATEGY-001 (its first criterion, which carries no
/// criterion id, through CreateStrategy_TwoRoles_ResolvesTwoDifferentStrategyTypes).
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>FR-MCP-LLMSTRATEGY-001 (criterion without an id): cites the criterion whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TEST-MCP-LLMSTRATEGY-001; FR-MCP-LLMSTRATEGY-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/BrainSlotLlmStrategyTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (RepoRoot) moved unchanged with the class attributes; only the namespace changed.</description></item>
/// </list>
/// </remarks>
public sealed class BrainSlotLlmStrategyTests
{
    /// <remarks>Integration: reads the live server project file to check its package and project references (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Factory_CreateStrategy_ExistsWithoutNewAgentSdk()
    {
        Assert.NotNull(typeof(BrainSlotChatClientFactory).GetMethod("CreateStrategy"));
        Assert.NotNull(typeof(IBrainSlotCompletionStrategy));
        var factory = new BrainSlotChatClientFactory();
        Assert.NotNull(factory);
        var csproj = File.ReadAllText(Path.Combine(RepoRoot(), "src", "QBrainAi.Support.Mcp", "QBrainAi.Support.Mcp.csproj"));
        var packages = System.Text.RegularExpressions.Regex.Matches(csproj, @"<PackageReference Include=""([^""]+)""")
            .Select(static m => m.Groups[1].Value)
            .ToArray();
        var projects = System.Text.RegularExpressions.Regex.Matches(csproj, @"<ProjectReference Include=""([^""]+)""")
            .Select(static m => m.Groups[1].Value)
            .ToArray();
        string[] expectedPackages =
        [
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore.Authentication.JwtBearer", "Microsoft.Extensions.AI.OpenAI", "OpenAI",
            "Microsoft.AspNetCore.Identity.EntityFrameworkCore", "Duende.IdentityServer", "Duende.IdentityServer.AspNetIdentity",
            "Duende.IdentityServer.EntityFramework", "Microsoft.Extensions.Hosting.WindowsServices", "Microsoft.EntityFrameworkCore.Sqlite",
            "Microsoft.EntityFrameworkCore.SqlServer", "Npgsql.EntityFrameworkCore.PostgreSQL", "Microsoft.EntityFrameworkCore.InMemory",
            "Microsoft.EntityFrameworkCore.Design", "Microsoft.EntityFrameworkCore.Analyzers", "Microsoft.CodeAnalysis.Common",
            "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.CSharp.Workspaces", "Microsoft.CodeAnalysis.Workspaces.Common",
            "Microsoft.CodeAnalysis.Workspaces.MSBuild", "Microsoft.Build.Framework", "ModelContextProtocol", "ModelContextProtocol.AspNetCore",
            "Serilog.AspNetCore", "Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Http", "Swashbuckle.AspNetCore", "YamlDotNet",
            "NetEscapades.Configuration.Yaml", "Microsoft.ML.OnnxRuntime", "HNSWIndex", "Handlebars.Net", "QRCoder",
        ];
        string[] expectedProjects =
        [
            @"..\QBrainAi.ServiceDefaults\QBrainAi.ServiceDefaults.csproj",
            @"..\QBrainAi.TransactionSecurity\QBrainAi.TransactionSecurity.csproj",
            @"..\QBrainAi.Common.AgentCli\QBrainAi.Common.AgentCli.csproj",
            @"..\QBrainAi.Storage\QBrainAi.Storage.csproj",
            @"..\QBrainAi.Storage.SqliteMigrations\QBrainAi.Storage.SqliteMigrations.csproj",
            @"..\QBrainAi.Storage.PostgreSqlMigrations\QBrainAi.Storage.PostgreSqlMigrations.csproj",
            @"..\QBrainAi.Storage.SqlServerMigrations\QBrainAi.Storage.SqlServerMigrations.csproj",
            @"..\QBrainAi.Services\QBrainAi.Services.csproj",
            @"..\QBrainAi.SessionLog.Transcripts\QBrainAi.SessionLog.Transcripts.csproj",
            @"..\QBrainAi.GraphRag\QBrainAi.GraphRag.csproj",
        ];
        Assert.Equal(expectedPackages, packages);
        Assert.Equal(expectedProjects, projects);
    }

    private static string RepoRoot() => QBrainAi.Tests.Shared.TestRepositoryRoot.Resolve();
}
