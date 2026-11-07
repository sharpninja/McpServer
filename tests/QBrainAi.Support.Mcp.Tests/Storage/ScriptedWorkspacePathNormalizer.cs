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
        new(Posix, @"C:\Users\fixture-user", Win, "C:/Users/fixture-user", "fixture-user", false),
        new(Posix, "C:/Users/fixture-user", Win, "C:/Users/fixture-user", "fixture-user", false),
        new(Posix, @"C:\Users/fixture-user\", Win, "C:/Users/fixture-user", "fixture-user", false),
        new(Posix, "C:/Users/fixture-user/", Win, "C:/Users/fixture-user", "fixture-user", false),
        new(Posix, @"\\srv\share\x", Win, "//srv/share/x", "x", false),
        new(Posix, "//srv/share/x", Win, "//srv/share/x", "x", false),
        new(Posix, @"\\srv\share\repo", Win, "//srv/share/repo", "repo", false),
        new(Posix, @"C:\Users/fixture-user\\repo/.\y\..\x", Win, "C:/Users/fixture-user/repo/x", "x", false),
        new(Posix, "/fixture/workspaces/McpServer", Posix, "/fixture/workspaces/McpServer", "McpServer", true),
        new(Posix, "relative/x", Posix, "/opt/mcpserver/app/relative/x", "x", true),
        new(Posix, "relative", Posix, "/opt/mcpserver/app/relative", "relative", true),
        new(Posix, "/home/x", Posix, "/home/x", "x", true),
        new(Posix, @"C:\", Win, "C:/", "C:/", false),
        new(Posix, "/", Posix, "/", "/", true),
        // Consumer fixtures (TriageService).
        new(Posix, "workspaces/rel", Posix, "/opt/mcpserver/app/workspaces/rel", "rel", true),
        new(Posix, "/fixture/workspaces/QBrainAi", Posix, "/fixture/workspaces/QBrainAi", "QBrainAi", true),
        // Consumer fixtures (WorkspaceService, HandoffWorkspacePaths, attribution validator, WorkspaceContext).
        new(Posix, @"C:\Users\fixture-user\repo", Win, "C:/Users/fixture-user/repo", "repo", false),
        new(Posix, "ws", Posix, "/opt/mcpserver/app/ws", "ws", true),
        // Domain-site fixtures (R4: todo resolver/factory, EF todo, execution, agent pool).
        new(Posix, "C:/Users/fixture-user/repo", Win, "C:/Users/fixture-user/repo", "repo", false),
        new(Posix, "/fixture/workspaces/McpServer/", Posix, "/fixture/workspaces/McpServer", "McpServer", true),
        // Domain-site fixtures (P1.4d: tool registry, agent workspaces).
        new(Posix, @"C:\Users\fixture-user\repo\", Win, "C:/Users/fixture-user/repo", "repo", false),

        // Simulated Windows host (cwd C:\svc).
        new(Win, "/fixture/workspaces/McpServer", Posix, "/fixture/workspaces/McpServer", "McpServer", false),
        new(Win, "/home/x", Posix, "/home/x", "x", false),
        new(Win, @"C:\Users\fixture-user", Win, "C:/Users/fixture-user", "fixture-user", true),
        new(Win, "C:/Users/fixture-user/", Win, "C:/Users/fixture-user", "fixture-user", true),
        new(Win, "relative", Win, "C:/svc/relative", "relative", true),
        new(Win, @"\\srv\share\repo", Win, "//srv/share/repo", "repo", true),
        new(Win, "//srv/share/x", Win, "//srv/share/x", "x", true),
        // Consumer fixtures (TriageService).
        new(Win, "/fixture/workspaces/ExampleWorkspace", Posix, "/fixture/workspaces/ExampleWorkspace", "ExampleWorkspace", false),
        // Consumer fixtures (WorkspaceService, WorkspaceTokenService, RepoFileService).
        new(Win, "/fixture/workspaces/ExampleWorkspace/", Posix, "/fixture/workspaces/ExampleWorkspace", "ExampleWorkspace", false),
        new(Win, "/fixture/workspaces/./ExampleWorkspace", Posix, "/fixture/workspaces/ExampleWorkspace", "ExampleWorkspace", false),
        new(Win, "/__mcp_unit_test__fed_init", Posix, "/__mcp_unit_test__fed_init", "__mcp_unit_test__fed_init", false),
        new(Win, "/fixture/workspaces/McpServer/", Posix, "/fixture/workspaces/McpServer", "McpServer", false),
        new(Win, "/__mcp_unit_test__repo", Posix, "/__mcp_unit_test__repo", "__mcp_unit_test__repo", false),
        // Domain-site fixtures (R4: factory default root, hostile review, requirements, token store).
        new(Win, ".", Win, "C:/svc", "svc", true),
        new(Win, "/fixture/workspaces/ExampleWorkspaceOther", Posix, "/fixture/workspaces/ExampleWorkspaceOther", "ExampleWorkspaceOther", false),
    ];

    private static readonly CombineRow[] CombineRows =
    [
        new(@"C:\Users\fixture-user", ["docs", "sessions"], "C:/Users/fixture-user/docs/sessions"),
        new("/fixture/workspaces/McpServer", ["docs", "sessions"], "/fixture/workspaces/McpServer/docs/sessions"),
        new(@"C:\Users\fixture-user", ["a", "..", "b"], "C:/Users/fixture-user/b"),
        new(@"C:\Users\fixture-user", ["/srv/other"], "/srv/other"),

        // Consumer fixtures: WorkspaceContext / middleware derived paths.
        new("/fixture/workspaces/ExampleWorkspace", ["docs", "sessions"], "/fixture/workspaces/ExampleWorkspace/docs/sessions"),
        new("/fixture/workspaces/ExampleWorkspace", ["docs", "external"], "/fixture/workspaces/ExampleWorkspace/docs/external"),
        new(@"C:\Users\fixture-user\repo", ["docs", "sessions"], "C:/Users/fixture-user/repo/docs/sessions"),
        new(@"C:\Users\fixture-user\repo", ["docs", "external"], "C:/Users/fixture-user/repo/docs/external"),

        // Consumer fixtures: attribution validator (POSIX root on a Windows host).
        new("/fixture/workspaces/ExampleWorkspace", ["docs/readme.md"], "/fixture/workspaces/ExampleWorkspace/docs/readme.md"),
        new("/fixture/workspaces/ExampleWorkspace", ["src/../src/b.cs"], "/fixture/workspaces/ExampleWorkspace/src/b.cs"),
        new("/fixture/workspaces/ExampleWorkspace", ["/fixture/workspaces/ExampleWorkspace/src/a.cs"], "/fixture/workspaces/ExampleWorkspace/src/a.cs"),
        new("/fixture/workspaces/ExampleWorkspace", [@"C:\fixture\workspaces\ExampleWorkspace\src\a.cs"], "C:/fixture/workspaces/ExampleWorkspace/src/a.cs"),
        new("/fixture/workspaces/ExampleWorkspace", ["/fixture/workspaces/ExampleWorkspaceOther/a.cs"], "/fixture/workspaces/ExampleWorkspaceOther/a.cs"),

        // Consumer fixtures: attribution validator (Windows root on a Linux hub).
        new("C:/Users/fixture-user/repo", ["docs/readme.md"], "C:/Users/fixture-user/repo/docs/readme.md"),
        new("C:/Users/fixture-user/repo", ["src/../src/b.cs"], "C:/Users/fixture-user/repo/src/b.cs"),
        new("C:/Users/fixture-user/repo", ["C:/Users/fixture-user/repo/src/a.cs"], "C:/Users/fixture-user/repo/src/a.cs"),
        new("C:/Users/fixture-user/repo", [@"/opt/mcpserver/app/C:\Users\fixture-user\repo/src/a.cs"], @"/opt/mcpserver/app/C:\Users\fixture-user\repo/src/a.cs"),
        new("C:/Users/fixture-user/repo", [@"C:\Users\fixture-user\repoOther\a.cs"], "C:/Users/fixture-user/repoOther/a.cs"),

        // Domain-site fixtures (P1.4d: WorkspaceService relative RepoRoot under the content root).
        new(@"C:\svc", ["repo"], "C:/svc/repo"),
        new(@"C:\svc", [@"C:\Users\fixture-user\repo\"], "C:/Users/fixture-user/repo"),

        // Root-relative Windows segments (PR #81 P12, PR #82 review): keep the drive or the UNC share.
        new("C:/content", [@"\repo"], "C:/repo"),
        new(@"C:\content\deep", [@"\repo", "sub"], "C:/repo/sub"),
        new(@"\\srv\share\content", [@"\repo"], "//srv/share/repo"),
        new("//srv/share/content", [@"\repo", "sub"], "//srv/share/repo/sub"),
        new(@"\\srv\share", [@"\repo"], "//srv/share/repo"),
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
