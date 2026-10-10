using System.Text.Json;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.AI;
using McpServer.Support.Mcp.Storage.Entities;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-LLMSTRATEGY-001: Per-role completion strategies, shared turn-context identity, and HTTP flattening.
/// </summary>
public sealed class BrainSlotLlmStrategyTests
{
    [Fact]
    public void CreateStrategy_TwoRoles_ResolvesTwoDifferentStrategyTypes()
    {
        var factory = new BrainSlotChatClientFactory();
        var creativity = Slot(BrainSlotRoles.Creativity, "OpenAICompatible", "http://localhost:11434/v1");
        var logic = Slot(BrainSlotRoles.Logic, "OpenAI", "https://api.openai.com/v1");

        var creativityStrategy = factory.CreateStrategy(creativity, "test-key");
        var logicStrategy = factory.CreateStrategy(logic, "test-key");

        Assert.NotNull(creativityStrategy);
        Assert.NotNull(logicStrategy);
        Assert.NotEqual(creativityStrategy.GetType(), logicStrategy.GetType());
    }

    [Fact]
    public async Task CompletionStrategy_ReceivesSharedTurnContextByReference()
    {
        var context = new BrainSlotTurnContext
        {
            OriginalInput = "shared-original",
            SessionId = "sess-1",
            TurnId = "turn-1",
            TransactionId = "txn-1",
        };
        context.SetCommittedEvidence(BrainSlotRoles.Creativity, "draft");
        var first = new RecordingCompletionStrategy();
        var second = new RecordingCompletionStrategy();
        var slot = Slot(BrainSlotRoles.Creativity, "OpenAICompatible", "http://localhost:11434/v1");

        await first.CompleteAsync(slot, "role-prompt-a", context, temperature: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await second.CompleteAsync(slot, "role-prompt-b", context, temperature: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Same(context, first.ReceivedContext);
        Assert.Same(context, second.ReceivedContext);
        Assert.Same(first.ReceivedContext, second.ReceivedContext);
    }

    [Fact]
    public void BuildOpenAiCompatibleRequestJson_WhenTurnContextSupplied_IncludesOriginalInputAndTurnIds()
    {
        var slot = Slot(BrainSlotRoles.Logic, "OpenAICompatible", "http://localhost:11434/v1");
        var context = new BrainSlotTurnContext
        {
            OriginalInput = "original-user-input",
            SessionId = "sess-ctx",
            TurnId = "turn-ctx",
            TransactionId = "txn-ctx",
        };
        context.SetCommittedEvidence(BrainSlotRoles.Creativity, "draft-a");

        using var document = JsonDocument.Parse(BrainSlotChatClientFactory.BuildOpenAiCompatibleRequestJson(slot, "role-prompt-logic", context, 0.2));
        var messages = document.RootElement.GetProperty("messages").EnumerateArray().Select(static m => m.GetProperty("content").GetString() ?? string.Empty).ToArray();
        var joined = string.Join('\n', messages);

        Assert.Contains("role-prompt-logic", joined, StringComparison.Ordinal);
        Assert.Contains("original-user-input", joined, StringComparison.Ordinal);
        Assert.Contains("sessionId=sess-ctx", joined, StringComparison.Ordinal);
        Assert.Contains("turnId=turn-ctx", joined, StringComparison.Ordinal);
        Assert.Contains("transactionId=txn-ctx", joined, StringComparison.Ordinal);
        Assert.Contains("evidence.Creativity=draft-a", joined, StringComparison.Ordinal);

        var chatMessages = BrainSlotChatClientFactory.BuildOpenAiMessages(slot, "role-prompt-logic", context);
        var joinedMessages = string.Join('\n', chatMessages.Select(static m => m.Text));
        Assert.Contains("role-prompt-logic", joinedMessages, StringComparison.Ordinal);
        Assert.Contains("original-user-input", joinedMessages, StringComparison.Ordinal);
        Assert.Contains("sessionId=sess-ctx", joinedMessages, StringComparison.Ordinal);
        Assert.Contains("evidence.Creativity=draft-a", joinedMessages, StringComparison.Ordinal);
        var options = BrainSlotChatClientFactory.BuildOpenAiOptions(slot, 0.2);
        Assert.Equal(0.2f, options.Temperature);
    }

    [Fact]
    public void Factory_CreateStrategy_ExistsWithoutNewAgentSdk()
    {
        Assert.NotNull(typeof(BrainSlotChatClientFactory).GetMethod("CreateStrategy"));
        Assert.NotNull(typeof(IBrainSlotCompletionStrategy));
        var factory = new BrainSlotChatClientFactory();
        Assert.NotNull(factory);
        var csproj = File.ReadAllText(Path.Combine(RepoRoot(), "src", "McpServer.Support.Mcp", "McpServer.Support.Mcp.csproj"));
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
            @"..\McpServer.ServiceDefaults\McpServer.ServiceDefaults.csproj",
            @"..\McpServer.TransactionSecurity\McpServer.TransactionSecurity.csproj",
            @"..\McpServer.Common.AgentCli\McpServer.Common.AgentCli.csproj",
            @"..\McpServer.Storage\McpServer.Storage.csproj",
            @"..\McpServer.Storage.SqliteMigrations\McpServer.Storage.SqliteMigrations.csproj",
            @"..\McpServer.Storage.PostgreSqlMigrations\McpServer.Storage.PostgreSqlMigrations.csproj",
            @"..\McpServer.Storage.SqlServerMigrations\McpServer.Storage.SqlServerMigrations.csproj",
            @"..\McpServer.Services\McpServer.Services.csproj",
            @"..\McpServer.SessionLog.Transcripts\McpServer.SessionLog.Transcripts.csproj",
            @"..\McpServer.GraphRag\McpServer.GraphRag.csproj",
        ];
        Assert.Equal(expectedPackages, packages);
        Assert.Equal(expectedProjects, projects);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "McpServer.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static BrainSlotDefinitionEntity Slot(string role, string providerKind, string endpoint)
        => new()
        {
            WorkspaceId = string.Empty,
            SlotId = role.ToLowerInvariant() + "-main",
            Role = role,
            ProviderKind = providerKind,
            ModelId = "model",
            Endpoint = endpoint,
            CredentialReference = "env:TEST_KEY",
            PartyId = "brain-slot:" + role.ToLowerInvariant(),
            Enabled = true,
            TimeoutSeconds = 30,
            MaxOutputTokens = 1024,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

    private sealed class RecordingCompletionStrategy : IBrainSlotCompletionStrategy
    {
        public BrainSlotTurnContext? ReceivedContext { get; private set; }

        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            BrainSlotTurnContext context,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            ReceivedContext = context;
            return Task.FromResult("ok");
        }
    }
}
