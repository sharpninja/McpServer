using QBrainAi.Support.Mcp.Services;
using Xunit;
using QBrainAi.Support.Mcp.Storage.Services;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Unit tests for <see cref="WorkspaceContext"/>.</summary>
public sealed class WorkspaceContextTests
{
    [Fact]
    public void DefaultStateAllPropertiesNull()
    {
        var ctx = new WorkspaceContext();
        Assert.Null(ctx.WorkspacePath);
        Assert.Null(ctx.WorkspaceName);
        Assert.Null(ctx.DataDirectory);
        Assert.Null(ctx.TodoFilePath);
        Assert.Null(ctx.SessionsPath);
        Assert.Null(ctx.ExternalDocsPath);
    }

    [Fact]
    public void IsResolvedReturnsFalseWhenPathNull()
    {
        var ctx = new WorkspaceContext();
        Assert.False(ctx.IsResolved);
    }

    [Fact]
    public void IsResolvedReturnsTrueWhenPathSet()
    {
        var ctx = new WorkspaceContext { WorkspacePath = @"C:\projects\test" };
        Assert.True(ctx.IsResolved);
    }

    [Fact]
    public void IsDefaultKeyDefaultsFalse()
    {
        var ctx = new WorkspaceContext();
        Assert.False(ctx.IsDefaultKey);
    }

    [Fact]
    public void SetWorkspaceAllPropertiesPopulated()
    {
        var ctx = new WorkspaceContext
        {
            WorkspacePath = @"C:\projects\test",
            WorkspaceName = "Test",
            DataDirectory = @"C:\data\test",
            TodoFilePath = "docs/todo.yaml",
            SessionsPath = @"C:\projects\test\docs\sessions",
            ExternalDocsPath = @"C:\projects\test\docs\external",
            IsDefaultKey = true,
        };

        Assert.Equal(@"C:\projects\test", ctx.WorkspacePath);
        Assert.Equal("Test", ctx.WorkspaceName);
        Assert.Equal(@"C:\data\test", ctx.DataDirectory);
        Assert.Equal("docs/todo.yaml", ctx.TodoFilePath);
        Assert.Equal(@"C:\projects\test\docs\sessions", ctx.SessionsPath);
        Assert.Equal(@"C:\projects\test\docs\external", ctx.ExternalDocsPath);
        Assert.True(ctx.IsDefaultKey);
        Assert.True(ctx.IsResolved);
    }
}
