using System;
using System.Linq;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-DB-IX-001 (Slice 1 of the 4NF/FK/index refactor): the model must define
/// indexes covering the server-side query predicates surfaced by the query audit, so
/// hot filters/sorts do not fall back to table scans. Cross-referenced against the
/// existing index set - only genuinely-absent indexes are asserted here.
/// </summary>
public sealed class QueryIndexCoverageTests : IDisposable
{
    private readonly McpDbContext _ctx;

    public QueryIndexCoverageTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"ix-cov-{Guid.NewGuid()}")
            .Options;
        _ctx = new McpDbContext(options);
    }

    public void Dispose() => _ctx.Dispose();

    private void AssertIndex<TEntity>(params string[] properties)
    {
        var entityType = _ctx.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entityType);
        var has = entityType!.GetIndexes()
            .Any(ix => ix.Properties.Select(p => p.Name).SequenceEqual(properties));
        Assert.True(has, $"{typeof(TEntity).Name} is missing an index on ({string.Join(", ", properties)}).");
    }

    [Fact]
    public void Model_defines_slice1_query_indexes()
    {
        // SessionLogService.QueryAsync sort/date predicates + ordered child fetch
        AssertIndex<SessionLogTurnEntity>(nameof(SessionLogTurnEntity.Timestamp));
        AssertIndex<SessionLogActionEntity>(
            nameof(SessionLogActionEntity.SessionLogTurnId),
            nameof(SessionLogActionEntity.Order));

        // TodoItem ordering + MAX(order) aggregates
        AssertIndex<TodoItemEntity>(nameof(TodoItemEntity.SectionOrder));
        AssertIndex<TodoItemEntity>(nameof(TodoItemEntity.ItemOrder));

        // Document upsert lookup + ordered chunk fetch
        AssertIndex<ContextDocumentEntity>(
            nameof(ContextDocumentEntity.SourceType),
            nameof(ContextDocumentEntity.SourceKey));
        AssertIndex<ContextChunkEntity>(
            nameof(ContextChunkEntity.DocumentId),
            nameof(ContextChunkEntity.ChunkIndex));

        // Graph recency ordering
        AssertIndex<GraphEntityEntity>(nameof(GraphEntityEntity.CreatedAtUtc));
        AssertIndex<GraphRelationshipEntity>(nameof(GraphRelationshipEntity.CreatedAtUtc));

        // Tool bucket filter + agent listing sort
        AssertIndex<ToolDefinitionEntity>(nameof(ToolDefinitionEntity.BucketName));
        AssertIndex<AgentDefinitionEntity>(nameof(AgentDefinitionEntity.DisplayName));

        // Agent event tail read (filter WorkspacePath+AgentId, order Timestamp)
        AssertIndex<AgentEventLogEntity>(
            nameof(AgentEventLogEntity.WorkspacePath),
            nameof(AgentEventLogEntity.AgentId),
            nameof(AgentEventLogEntity.Timestamp));

        // Federation replay drain + outbox acknowledgement scan
        AssertIndex<FederationOperationEntity>(
            nameof(FederationOperationEntity.ProxyId),
            nameof(FederationOperationEntity.Status),
            nameof(FederationOperationEntity.AttemptCount),
            nameof(FederationOperationEntity.CreatedAtUtc));
        AssertIndex<FederationOutboxEntity>(
            nameof(FederationOutboxEntity.ProxyId),
            nameof(FederationOutboxEntity.AcknowledgedAtUtc));
    }
}
