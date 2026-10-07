using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Unit tests for <see cref="WorkspaceContext"/>.</summary>
public sealed class WorkspaceContextTests
{
    [Fact]
    public void DefaultState_AllPropertiesNull()
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
    public void IsResolved_ReturnsFalse_WhenPathNull()
    {
        var ctx = new WorkspaceContext();
        Assert.False(ctx.IsResolved);
    }

    [Fact]
    public void IsResolved_ReturnsTrue_WhenPathSet()
    {
        var ctx = new WorkspaceContext { WorkspacePath = @"C:\projects\test" };
        Assert.True(ctx.IsResolved);
    }

    [Fact]
    public void IsDefaultKey_DefaultsFalse()
    {
        var ctx = new WorkspaceContext();
        Assert.False(ctx.IsDefaultKey);
    }

    [Fact]
    public void SetWorkspace_AllPropertiesPopulated()
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

    /// <summary>
    /// TEST-MCP-FED-PATH-001: derived paths for a foreign-platform workspace use the workspace's
    /// own separators (no "/home/x\docs\sessions" on Windows, no "C:\x/docs/sessions" on Linux).
    /// </summary>
    [Fact]
    public void SetDerivedPaths_ForeignPlatformWorkspace_UsesWorkspaceSyntax()
    {
        var foreign = OperatingSystem.IsWindows() ? "/home/sharpninja/github/RideAudit" : @"C:\Users\kingd\repo";
        var ctx = new WorkspaceContext();

        ctx.SetDerivedPaths(foreign);

        var normalizer = QBrainAi.Client.WorkspacePathNormalizer.Process;
        Assert.Equal(normalizer.Combine(foreign, "docs", "sessions"), ctx.SessionsPath);
        Assert.Equal(normalizer.Combine(foreign, "docs", "external"), ctx.ExternalDocsPath);
        Assert.False(ctx.SessionsPath!.Contains('/') && ctx.SessionsPath.Contains('\\'), ctx.SessionsPath);
    }

    /// <summary>TEST-MCP-FED-PATH-001: host-native workspaces keep their existing derived paths.</summary>
    [Fact]
    public void SetDerivedPaths_NativeWorkspace_MatchesNormalizedCombine()
    {
        var native = Path.Combine(Path.GetTempPath(), "derived-native");
        var ctx = new WorkspaceContext();

        ctx.SetDerivedPaths(native);

        Assert.Equal(
            QBrainAi.Client.WorkspacePathNormalizer.Process.Normalize(Path.Combine(native, "docs", "sessions")),
            ctx.SessionsPath);
    }
}
