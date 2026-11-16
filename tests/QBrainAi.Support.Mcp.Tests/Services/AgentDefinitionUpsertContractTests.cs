using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-027, TR-MCP-AGENT-003, TEST-MCP-033: contracts for updating an existing
/// agent definition without orphaning its ordered default-model rows.
/// </summary>
/// <remarks>
/// Change log: 2026-11-16 - Added a provider-faithful upsert regression contract
/// after the live AgentManager update attempted to null a required model-row owner.
/// </remarks>
public sealed class AgentDefinitionUpsertContractTests
{
    private static readonly DateTime OriginalCreatedAt = new(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);

    private static readonly AgentDefinitionRequest UpdateRequest = new()
    {
        Id = "test-agent",
        DisplayName = "Test Agent",
        DefaultLaunchCommand = "codex",
        DefaultInstructionFile = "AGENTS.md",
        DefaultModels = ["gpt-6.1-sol", "gpt-6-astra"],
        DefaultBranchStrategy = "feature/test/{task}",
        DefaultSeedPrompt = "Follow the workspace instructions.",
    };

    /// <summary>
    /// AC-AGENT-UPSERT-MODEL-OWNERSHIP: the contract accepts a complete update and
    /// its negative control detects a model row whose required parent id was lost.
    /// </summary>
    [Fact]
    public void ExistingDefinitionUpdate_MockGate_PreservesMetadataAndRejectsOrphanedModels()
    {
        var correct = ApplyScriptedUpdate(UpdateRequest);

        Assert.Empty(EvaluateContract(correct));

        var orphaned = correct with
        {
            Models =
            [
                correct.Models[0] with { AgentDefinitionId = null },
                correct.Models[1],
            ],
        };

        Assert.Contains(
            "Default model at ordinal 0 lost required owner 'test-agent'.",
            EvaluateContract(orphaned));
    }

    /// <summary>
    /// AC-AGENT-UPSERT-RELATIONAL: the same update contract succeeds through a
    /// relational provider that enforces the required model-row foreign key.
    /// </summary>
    [Fact]
    public async Task ExistingDefinitionUpdate_ProductionBinding_ReplacesOwnedModelsWithoutNullForeignKeys()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new McpDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        db.AgentDefinitions.Add(new AgentDefinitionEntity
        {
            Id = UpdateRequest.Id,
            DisplayName = "Legacy Name",
            DefaultLaunchCommand = "legacy-agent",
            DefaultInstructionFile = "LEGACY.md",
            Models =
            [
                new AgentDefinitionModelEntity
                {
                    AgentDefinitionId = UpdateRequest.Id,
                    Ordinal = 0,
                    Model = "legacy-model",
                },
            ],
            DefaultBranchStrategy = "legacy/{task}",
            DefaultSeedPrompt = "Legacy prompt.",
            IsBuiltIn = true,
            CreatedAt = OriginalCreatedAt,
            ModifiedAt = OriginalCreatedAt,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        db.ChangeTracker.Clear();

        var sut = new AgentService(db, NullLogger<AgentService>.Instance);
        var result = await sut.UpsertDefinitionAsync(
            UpdateRequest,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        db.ChangeTracker.Clear();

        var stored = await db.AgentDefinitions
            .SingleAsync(x => x.Id == UpdateRequest.Id, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var storedRows = await db.AgentDefinitionModels
            .Where(x => x.AgentDefinitionId == UpdateRequest.Id)
            .OrderBy(x => x.Ordinal)
            .ToListAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var snapshot = new AgentDefinitionSnapshot(
            result.Success,
            await db.AgentDefinitions.CountAsync(TestContext.Current.CancellationToken).ConfigureAwait(true),
            stored.Id,
            stored.DisplayName,
            stored.DefaultLaunchCommand,
            stored.DefaultInstructionFile,
            stored.DefaultBranchStrategy,
            stored.DefaultSeedPrompt,
            stored.IsBuiltIn,
            stored.CreatedAt,
            storedRows.Select(x => new AgentDefinitionModelSnapshot(x.AgentDefinitionId, x.Ordinal, x.Model)).ToList());

        Assert.Empty(EvaluateContract(snapshot));
    }

    private static AgentDefinitionSnapshot ApplyScriptedUpdate(AgentDefinitionRequest request)
        => new(
            Success: true,
            DefinitionCount: 1,
            Id: request.Id,
            DisplayName: request.DisplayName,
            DefaultLaunchCommand: request.DefaultLaunchCommand,
            DefaultInstructionFile: request.DefaultInstructionFile,
            DefaultBranchStrategy: request.DefaultBranchStrategy,
            DefaultSeedPrompt: request.DefaultSeedPrompt,
            IsBuiltIn: true,
            CreatedAt: OriginalCreatedAt,
            Models: request.DefaultModels
                .Select((model, ordinal) => new AgentDefinitionModelSnapshot(request.Id, ordinal, model))
                .ToList());

    private static IReadOnlyList<string> EvaluateContract(AgentDefinitionSnapshot snapshot)
    {
        var failures = new List<string>();
        if (!snapshot.Success)
            failures.Add("Upsert did not report success.");
        if (snapshot.DefinitionCount != 1)
            failures.Add($"Expected one definition but found {snapshot.DefinitionCount}.");
        if (!string.Equals(snapshot.Id, UpdateRequest.Id, StringComparison.Ordinal))
            failures.Add($"Definition id changed to '{snapshot.Id}'.");
        if (!string.Equals(snapshot.DisplayName, UpdateRequest.DisplayName, StringComparison.Ordinal))
            failures.Add("Display name was not preserved.");
        if (!string.Equals(snapshot.DefaultLaunchCommand, UpdateRequest.DefaultLaunchCommand, StringComparison.Ordinal))
            failures.Add("Launch command was not updated to 'codex'.");
        if (!string.Equals(snapshot.DefaultInstructionFile, UpdateRequest.DefaultInstructionFile, StringComparison.Ordinal))
            failures.Add("Instruction file was not preserved.");
        if (!string.Equals(snapshot.DefaultBranchStrategy, UpdateRequest.DefaultBranchStrategy, StringComparison.Ordinal))
            failures.Add("Branch strategy was not preserved.");
        if (!string.Equals(snapshot.DefaultSeedPrompt, UpdateRequest.DefaultSeedPrompt, StringComparison.Ordinal))
            failures.Add("Seed prompt was not preserved.");
        if (!snapshot.IsBuiltIn)
            failures.Add("Built-in status was not preserved.");
        if (snapshot.CreatedAt != OriginalCreatedAt)
            failures.Add("Created timestamp was not preserved.");

        var expectedModels = UpdateRequest.DefaultModels.ToArray();
        if (snapshot.Models.Count != expectedModels.Length)
        {
            failures.Add($"Expected {expectedModels.Length} default models but found {snapshot.Models.Count}.");
            return failures;
        }

        for (var ordinal = 0; ordinal < expectedModels.Length; ordinal++)
        {
            var model = snapshot.Models[ordinal];
            if (!string.Equals(model.AgentDefinitionId, UpdateRequest.Id, StringComparison.Ordinal))
                failures.Add($"Default model at ordinal {ordinal} lost required owner '{UpdateRequest.Id}'.");
            if (model.Ordinal != ordinal)
                failures.Add($"Default model ordinal {model.Ordinal} should be {ordinal}.");
            if (!string.Equals(model.Model, expectedModels[ordinal], StringComparison.Ordinal))
                failures.Add($"Default model at ordinal {ordinal} changed from '{expectedModels[ordinal]}' to '{model.Model}'.");
        }

        return failures;
    }

    private sealed record AgentDefinitionSnapshot(
        bool Success,
        int DefinitionCount,
        string Id,
        string DisplayName,
        string DefaultLaunchCommand,
        string DefaultInstructionFile,
        string DefaultBranchStrategy,
        string DefaultSeedPrompt,
        bool IsBuiltIn,
        DateTime CreatedAt,
        IReadOnlyList<AgentDefinitionModelSnapshot> Models);

    private sealed record AgentDefinitionModelSnapshot(string? AgentDefinitionId, int Ordinal, string Model);
}
