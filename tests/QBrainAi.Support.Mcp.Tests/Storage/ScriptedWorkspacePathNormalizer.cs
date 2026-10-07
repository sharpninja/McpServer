using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// Scripted <see cref="IWorkspacePathNormalizer"/> for contract validation. Every answer comes from
/// the literal tables below; inputs that are not scripted throw so data gaps cannot pass silently.
/// For rows marked host-native it calls <see cref="IWorkspaceHostEnvironment.NormalizeNativePath"/>
/// once per <see cref="Normalize"/> so host-interaction assertions are exercised.
/// </summary>
internal sealed class ScriptedWorkspacePathNormalizer : IWorkspacePathNormalizer
{
    private const WorkspacePathPlatform Win = WorkspacePathPlatform.Windows;
    private const WorkspacePathPlatform Posix = WorkspacePathPlatform.CaseSensitive;

    private static readonly Row[] Rows =
    [
        // Simulated Linux hub (cwd /opt/mcpserver/app).
        new(Posix, @"C:\Users\kingd", Win, @"C:\Users\kingd", "kingd", false),
        new(Posix, "C:/Users/kingd", Win, @"C:\Users\kingd", "kingd", false),
        new(Posix, @"C:\Users/kingd\", Win, @"C:\Users\kingd", "kingd", false),
        new(Posix, "C:/Users/kingd/", Win, @"C:\Users\kingd", "kingd", false),
        new(Posix, @"\\srv\share\x", Win, @"\\srv\share\x", "x", false),
        new(Posix, "//srv/share/x", Win, @"\\srv\share\x", "x", false),
        new(Posix, @"\\srv\share\repo", Win, @"\\srv\share\repo", "repo", false),
        new(Posix, @"C:\Users/kingd\\repo/.\y\..\x", Win, @"C:\Users\kingd\repo\x", "x", false),
        new(Posix, "/home/sharpninja/github/McpServer", Posix, "/home/sharpninja/github/McpServer", "McpServer", true),
        new(Posix, "relative/x", Posix, "/opt/mcpserver/app/relative/x", "x", true),
        new(Posix, "relative", Posix, "/opt/mcpserver/app/relative", "relative", true),
        new(Posix, "/home/x", Posix, "/home/x", "x", true),
        new(Posix, @"C:\", Win, @"C:\", @"C:\", false),
        new(Posix, "/", Posix, "/", "/", true),
        // Consumer fixtures (TriageService).
        new(Posix, "workspaces/rel", Posix, "/opt/mcpserver/app/workspaces/rel", "rel", true),
        new(Posix, "/home/sharpninja/github/QBrainAi", Posix, "/home/sharpninja/github/QBrainAi", "QBrainAi", true),
        // Consumer fixtures (WorkspaceService, HandoffWorkspacePaths, attribution validator, WorkspaceContext).
        new(Posix, @"C:\Users\kingd\repo", Win, @"C:\Users\kingd\repo", "repo", false),
        new(Posix, "ws", Posix, "/opt/mcpserver/app/ws", "ws", true),
        // Domain-site fixtures (R4: todo resolver/factory, EF todo, execution, agent pool).
        new(Posix, "C:/Users/kingd/repo", Win, @"C:\Users\kingd\repo", "repo", false),
        new(Posix, "/home/sharpninja/github/McpServer/", Posix, "/home/sharpninja/github/McpServer", "McpServer", true),

        // Simulated Windows host (cwd C:\svc).
        new(Win, "/home/sharpninja/github/McpServer", Posix, "/home/sharpninja/github/McpServer", "McpServer", false),
        new(Win, "/home/x", Posix, "/home/x", "x", false),
        new(Win, @"C:\Users\kingd", Win, @"C:\Users\kingd", "kingd", true),
        new(Win, "C:/Users/kingd/", Win, @"C:\Users\kingd", "kingd", true),
        new(Win, "relative", Win, @"C:\svc\relative", "relative", true),
        new(Win, @"\\srv\share\repo", Win, @"\\srv\share\repo", "repo", true),
        new(Win, "//srv/share/x", Win, @"\\srv\share\x", "x", true),
        // Consumer fixtures (TriageService).
        new(Win, "/home/sharpninja/github/RideAudit", Posix, "/home/sharpninja/github/RideAudit", "RideAudit", false),
        // Consumer fixtures (WorkspaceService, WorkspaceTokenService, RepoFileService).
        new(Win, "/home/sharpninja/github/RideAudit/", Posix, "/home/sharpninja/github/RideAudit", "RideAudit", false),
        new(Win, "/home/sharpninja/github/./RideAudit", Posix, "/home/sharpninja/github/RideAudit", "RideAudit", false),
        new(Win, "/__mcp_unit_test__fed_init", Posix, "/__mcp_unit_test__fed_init", "__mcp_unit_test__fed_init", false),
        new(Win, "/home/sharpninja/github/McpServer/", Posix, "/home/sharpninja/github/McpServer", "McpServer", false),
        new(Win, "/__mcp_unit_test__repo", Posix, "/__mcp_unit_test__repo", "__mcp_unit_test__repo", false),
        // Domain-site fixtures (R4: factory default root, hostile review, requirements, token store).
        new(Win, ".", Win, @"C:\svc", "svc", true),
        new(Win, "/home/sharpninja/github/RideAuditOther", Posix, "/home/sharpninja/github/RideAuditOther", "RideAuditOther", false),
    ];

    private static readonly CombineRow[] CombineRows =
    [
        new(@"C:\Users\kingd", ["docs", "sessions"], @"C:\Users\kingd\docs\sessions"),
        new("/home/sharpninja/github/McpServer", ["docs", "sessions"], "/home/sharpninja/github/McpServer/docs/sessions"),
        new(@"C:\Users\kingd", ["a", "..", "b"], @"C:\Users\kingd\b"),
        new(@"C:\Users\kingd", ["/srv/other"], "/srv/other"),

        // Consumer fixtures: WorkspaceContext / middleware derived paths.
        new("/home/sharpninja/github/RideAudit", ["docs", "sessions"], "/home/sharpninja/github/RideAudit/docs/sessions"),
        new("/home/sharpninja/github/RideAudit", ["docs", "external"], "/home/sharpninja/github/RideAudit/docs/external"),
        new(@"C:\Users\kingd\repo", ["docs", "sessions"], @"C:\Users\kingd\repo\docs\sessions"),
        new(@"C:\Users\kingd\repo", ["docs", "external"], @"C:\Users\kingd\repo\docs\external"),

        // Consumer fixtures: attribution validator (POSIX root on a Windows host).
        new("/home/sharpninja/github/RideAudit", ["docs/readme.md"], "/home/sharpninja/github/RideAudit/docs/readme.md"),
        new("/home/sharpninja/github/RideAudit", ["src/../src/b.cs"], "/home/sharpninja/github/RideAudit/src/b.cs"),
        new("/home/sharpninja/github/RideAudit", ["/home/sharpninja/github/RideAudit/src/a.cs"], "/home/sharpninja/github/RideAudit/src/a.cs"),
        new("/home/sharpninja/github/RideAudit", [@"C:\home\sharpninja\github\RideAudit\src\a.cs"], @"C:\home\sharpninja\github\RideAudit\src\a.cs"),
        new("/home/sharpninja/github/RideAudit", ["/home/sharpninja/github/RideAuditOther/a.cs"], "/home/sharpninja/github/RideAuditOther/a.cs"),

        // Consumer fixtures: attribution validator (Windows root on a Linux hub).
        new(@"C:\Users\kingd\repo", ["docs/readme.md"], @"C:\Users\kingd\repo\docs\readme.md"),
        new(@"C:\Users\kingd\repo", ["src/../src/b.cs"], @"C:\Users\kingd\repo\src\b.cs"),
        new(@"C:\Users\kingd\repo", ["C:/Users/kingd/repo/src/a.cs"], @"C:\Users\kingd\repo\src\a.cs"),
        new(@"C:\Users\kingd\repo", [@"/opt/mcpserver/app/C:\Users\kingd\repo/src/a.cs"], @"/opt/mcpserver/app/C:\Users\kingd\repo/src/a.cs"),
        new(@"C:\Users\kingd\repo", [@"C:\Users\kingd\repoOther\a.cs"], @"C:\Users\kingd\repoOther\a.cs"),
    ];

    private readonly IWorkspaceHostEnvironment _host;

    public ScriptedWorkspacePathNormalizer(IWorkspaceHostEnvironment host) => _host = host;

    public WorkspacePathPlatform DetectPlatform(string workspacePath) => Find(workspacePath).Platform;

    public string Normalize(string workspacePath)
    {
        var row = Find(workspacePath);
        if (row.HostNative)
            _host.NormalizeNativePath(row.Input);
        return row.Normalized;
    }

    public string GetLeafName(string workspacePath) => Find(workspacePath).Leaf;

    public bool IsHostNative(string workspacePath) => Find(workspacePath).HostNative;

    public string Combine(string rootPath, params string[] segments) =>
        CombineRows.FirstOrDefault(row => row.Root == rootPath && row.Segments.SequenceEqual(segments))?.Result
        ?? throw new InvalidOperationException($"No scripted Combine data for '{rootPath}' + [{string.Join(", ", segments)}].");

    private Row Find(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        var input = workspacePath.Trim();
        var platform = _host.Platform;
        return Rows.FirstOrDefault(row => row.Host == platform && row.Input == input)
            ?? throw new InvalidOperationException($"No scripted normalizer data for host {platform}, input '{input}'.");
    }

    private sealed record Row(
        WorkspacePathPlatform Host,
        string Input,
        WorkspacePathPlatform Platform,
        string Normalized,
        string Leaf,
        bool HostNative);

    private sealed record CombineRow(string Root, string[] Segments, string Result);
}
