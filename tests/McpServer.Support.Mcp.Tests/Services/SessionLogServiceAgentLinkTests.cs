using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Focused tests for session-log agent linkage behavior added for MVP-MCP-005.
/// </summary>
public sealed class SessionLogServiceAgentLinkTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\sessionlog-agent-link";

    private readonly McpDbContext _db;
    private readonly SessionLogService _sut;

    public SessionLogServiceAgentLinkTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogAgentLinkTests_{Guid.NewGuid()}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);
        _sut = new SessionLogService(_db, NullLogger<SessionLogService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task SubmitAsync_WhenSourceTypeMatchesAgentDefinition_LinksAgentDefinitionId()
    {
        _db.AgentDefinitions.Add(new AgentDefinitionEntity
        {
            Id = "Codex",
            DisplayName = "Codex",
            DefaultLaunchCommand = "codex",
            DefaultInstructionFile = string.Empty,
            DefaultModelsJson = "[]",
            DefaultBranchStrategy = "direct",
            DefaultSeedPrompt = string.Empty,
            IsBuiltIn = true,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var dto = CreateDto("Codex", BuildSessionId("Codex", "agent-link"));
        var id = await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var stored = await _db.SessionLogs.FirstAsync(x => x.Id == id).ConfigureAwait(true);
        Assert.Equal("Codex", stored.AgentDefinitionId);
        Assert.Equal("Codex", dto.AgentDefinitionId);
    }

    [Fact]
    public async Task QueryAsync_WhenFilteringByAgentDefinitionId_ReturnsOnlyMatchingSessions()
    {
        _db.AgentDefinitions.AddRange(
            new AgentDefinitionEntity
            {
                Id = "Codex",
                DisplayName = "Codex",
                DefaultLaunchCommand = "codex",
                DefaultInstructionFile = string.Empty,
                DefaultModelsJson = "[]",
                DefaultBranchStrategy = "direct",
                DefaultSeedPrompt = string.Empty,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            },
            new AgentDefinitionEntity
            {
                Id = "Copilot",
                DisplayName = "Copilot",
                DefaultLaunchCommand = "copilot",
                DefaultInstructionFile = string.Empty,
                DefaultModelsJson = "[]",
                DefaultBranchStrategy = "direct",
                DefaultSeedPrompt = string.Empty,
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            });
        await _db.SaveChangesAsync().ConfigureAwait(true);

        await _sut.SubmitAsync(CreateDto("Codex", BuildSessionId("Codex", "query-1"))).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateDto("Copilot", BuildSessionId("Copilot", "query-2"))).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { AgentDefinitionId = "Codex" }).ConfigureAwait(true);

        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal("Codex", item.AgentDefinitionId);
        Assert.Equal("Codex", item.SourceType);
    }

    private static UnifiedSessionLogDto CreateDto(string sourceType, string sessionId)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = sourceType,
            SessionId = sessionId,
            Title = "Test Session",
            Model = "gpt-5.4",
            Started = "2026-03-10T21:00:00Z",
            LastUpdated = "2026-03-10T21:05:00Z",
            Status = "completed",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260310T210100Z-entry-001",
                    Timestamp = "2026-03-10T21:01:00Z",
                    QueryText = "Test prompt",
                    Response = "Test response",
                    Status = "completed",
                }
            ]
        };
    }

    private static string BuildSessionId(string agent, string suffix)
        => $"{agent}-20260310T210000Z-{suffix}";
}
