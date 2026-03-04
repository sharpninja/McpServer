using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Notification publish verification tests for extended services.</summary>
public sealed class EventPublishingServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public EventPublishingServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"mcp-evt-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
        catch
        {
            // Best-effort temp cleanup.
        }
    }

    [Fact]
    public async Task ToolRegistryService_Create_PublishesToolRegistryCreated()
    {
        var db = CreateDbContext();
        var eventBus = Substitute.For<IChangeEventBus>();
        var sut = new ToolRegistryService(db, NullLogger<ToolRegistryService>.Instance, eventBus);

        var result = await sut.CreateAsync(new ToolCreateRequest(
            Name: "test-tool",
            Description: "test description",
            Tags: ["test"])).ConfigureAwait(true);

        Assert.True(result.Success);
        await eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e =>
                e.Category == ChangeEventCategories.ToolRegistry &&
                e.Action == ChangeEventActions.Created),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task WorkspaceService_Create_PublishesWorkspaceCreated()
    {
        var eventBus = Substitute.For<IChangeEventBus>();
        var appsettingsPath = Path.Combine(_tempRoot, "appsettings.json");
        await File.WriteAllTextAsync(appsettingsPath, """
            {
              "Mcp": {
                "Workspaces": []
              }
            }
            """).ConfigureAwait(true);

        var config = new ConfigurationBuilder()
            .SetBasePath(_tempRoot)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
        var env = Substitute.For<IHostEnvironment>();
        env.ContentRootPath.Returns(_tempRoot);

        var sut = new WorkspaceService(config, env, NullLogger<WorkspaceService>.Instance, eventBus);
        var workspacePath = Path.Combine(_tempRoot, "workspace-one");

        var result = await sut.CreateAsync(new WorkspaceCreateRequest
        {
            WorkspacePath = workspacePath,
            Name = "workspace-one",
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        await eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e =>
                e.Category == ChangeEventCategories.Workspace &&
                e.Action == ChangeEventActions.Created &&
                e.EntityId == Path.GetFullPath(workspacePath)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task AgentService_UpsertDefinition_PublishesCreatedThenUpdated()
    {
        var db = CreateDbContext();
        var eventBus = Substitute.For<IChangeEventBus>();
        var sut = new AgentService(db, NullLogger<AgentService>.Instance, eventBus);

        var create = new AgentDefinitionRequest
        {
            Id = "test-agent",
            DisplayName = "Test Agent",
            DefaultLaunchCommand = "agent run",
            DefaultInstructionFile = ".cursor/rules/AGENTS.md",
            DefaultModels = ["gpt-5.3-codex"],
            DefaultBranchStrategy = "feature/test",
            DefaultSeedPrompt = "seed",
        };
        var createResult = await sut.UpsertDefinitionAsync(create).ConfigureAwait(true);
        Assert.True(createResult.Success);

        var update = create with { DisplayName = "Test Agent Updated" };
        var updateResult = await sut.UpsertDefinitionAsync(update).ConfigureAwait(true);
        Assert.True(updateResult.Success);

        await eventBus.Received().PublishAsync(
            Arg.Is<ChangeEvent>(e =>
                e.Category == ChangeEventCategories.Agent &&
                e.Action == ChangeEventActions.Created &&
                e.EntityId == "test-agent"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await eventBus.Received().PublishAsync(
            Arg.Is<ChangeEvent>(e =>
                e.Category == ChangeEventCategories.Agent &&
                e.Action == ChangeEventActions.Updated &&
                e.EntityId == "test-agent"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static McpDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"event-publish-tests-{Guid.NewGuid():N}")
            .Options;
        var db = new McpDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
