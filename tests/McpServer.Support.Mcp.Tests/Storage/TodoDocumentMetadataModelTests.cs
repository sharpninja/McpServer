using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-TODO-007 regression guard: <see cref="TodoDocumentMetadataEntity.SingletonId"/>
/// is a fixed sentinel (always <c>1</c>) that every provider must treat as
/// caller-assigned, never auto-generated. SQLite silently accepts explicit values
/// for <c>AUTOINCREMENT</c> columns, so a test that only runs against SQLite cannot
/// catch the SQL Server <c>IDENTITY_INSERT OFF</c> rejection (error 544). This test
/// inspects the EF model directly instead of relying on runtime behavior.
/// </summary>
public sealed class TodoDocumentMetadataModelTests
{
    [Fact]
    public void SingletonId_IsConfiguredAsValueGeneratedNever()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"todo-meta-model-{Guid.NewGuid():N}")
            .Options;

        using var ctx = new McpDbContext(options);
        var entity = ctx.Model.FindEntityType(typeof(TodoDocumentMetadataEntity));
        Assert.NotNull(entity);

        var singletonId = entity!.FindProperty(nameof(TodoDocumentMetadataEntity.SingletonId));
        Assert.NotNull(singletonId);

        Assert.Equal(
            ValueGenerated.Never,
            singletonId!.ValueGenerated);
    }
}
