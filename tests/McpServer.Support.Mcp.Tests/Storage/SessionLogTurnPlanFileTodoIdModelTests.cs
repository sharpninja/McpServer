using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-004 / TEST-MCP-SESSIONLOG-006: entity metadata for required planFile/todoId.
/// </summary>
public sealed class SessionLogTurnPlanFileTodoIdModelTests
{
    /// <summary>AC-TR-MCP-SESSIONLOG-006-004: required strings with expected max lengths and None default.</summary>
    [Fact]
    public void SessionLogTurnEntity_PlanFileAndTodoId_RequiredWithExpectedMaxLengths()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase("model-" + Guid.NewGuid().ToString("N"))
            .Options;
        using var db = new McpDbContext(options);
        var entity = db.Model.FindEntityType(typeof(SessionLogTurnEntity));
        Assert.NotNull(entity);
        var plan = entity!.FindProperty(nameof(SessionLogTurnEntity.PlanFile));
        var todo = entity.FindProperty(nameof(SessionLogTurnEntity.TodoId));
        Assert.NotNull(plan);
        Assert.NotNull(todo);
        Assert.False(plan!.IsNullable);
        Assert.False(todo!.IsNullable);
        Assert.Equal(2048, plan.GetMaxLength());
        Assert.Equal(128, todo.GetMaxLength());
        Assert.Equal("None", new SessionLogTurnEntity().PlanFile);
        Assert.Equal("None", new SessionLogTurnEntity().TodoId);
    }
}
