using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-FED-PATH-001: session-log attribution checks containment by the workspace root's own
/// path syntax. A hub validating a turn for a foreign-platform workspace must not re-root either
/// side on its own drive or working directory.
/// </summary>
public sealed class SessionLogWorkspaceAttributionValidatorCrossPlatformTests
{
    private static readonly string ForeignRoot = OperatingSystem.IsWindows()
        ? "/home/sharpninja/github/RideAudit"
        : @"C:\Users\kingd\repo";

    /// <summary>
    /// FR-MCP-FED-PATH-001: a host-native path that only "matches" a foreign root after the host
    /// re-roots the root (Windows: F:\home\..., Linux: /cwd/C:\...) is outside the workspace.
    /// </summary>
    [Fact]
    public void ValidatePaths_HostRewriteOfForeignRoot_IsOutside()
    {
        var hostRewrite = OperatingSystem.IsWindows()
            ? Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory)!, "home", "sharpninja", "github", "RideAudit", "src", "a.cs")
            : Path.Combine(Environment.CurrentDirectory, @"C:\Users\kingd\repo", "src", "a.cs");

        Assert.Throws<ArgumentException>(() =>
            SessionLogWorkspaceAttributionValidator.ValidatePaths([hostRewrite], null, ForeignRoot, "filesModified"));
    }

    /// <summary>FR-MCP-FED-PATH-001: relative and absolute paths under a foreign root are inside.</summary>
    [Fact]
    public void ValidatePaths_PathsUnderForeignRoot_AreInside()
    {
        var absolute = OperatingSystem.IsWindows()
            ? "/home/sharpninja/github/RideAudit/src/a.cs"
            : "C:/Users/kingd/repo/src/a.cs";

        SessionLogWorkspaceAttributionValidator.ValidatePaths(
            ["docs/readme.md", "src/../src/b.cs", absolute],
            null,
            ForeignRoot,
            "filesModified");
    }

    /// <summary>FR-MCP-FED-PATH-001: a sibling of a foreign root is outside.</summary>
    [Fact]
    public void ValidatePaths_SiblingOfForeignRoot_IsOutside()
    {
        var sibling = OperatingSystem.IsWindows()
            ? "/home/sharpninja/github/RideAuditOther/a.cs"
            : @"C:\Users\kingd\repoOther\a.cs";

        Assert.Throws<ArgumentException>(() =>
            SessionLogWorkspaceAttributionValidator.ValidatePaths([sibling], null, ForeignRoot, "filesModified"));
    }
}
