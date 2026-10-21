using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Production binding of the directory-walk cases to the two user-profile scans: marker agent-plugin version
/// resolution and the session-log agent-history scan each keep working when a directory under the profile is an
/// untrusted mount point (its listing throws) or a link, and neither scan fails.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-WS-005 / AC-TR-MCP-WS-005-001 and TR-MCP-SESSIONLOG-006 /
/// AC-TR-MCP-SESSIONLOG-006-009 (operator 2026-10-20: "Fix scans in PR 87 (Recommended)"; triage report
/// b0e41c36). Test data: a temporary profile the test creates, listed through
/// <see cref="DirectoryTreeWalker.ListFileSystem"/>, with the failure conditions scripted on top: one directory's
/// listing throws the untrusted-mount-point <see cref="IOException"/> seen on PAYTON-LEGION2 as kingd, and one
/// directory is reported as a link. Seam controls run the same profile with the link flag removed and prove the
/// result changes, so the expected values come from the link and error handling rather than from the layout. The
/// version-file cases (operator 2026-10-20: "Fold R3-P3 in first") read the same profile through
/// <see cref="MarkerFileService.ReadExistingFileText"/> with named <c>.version</c> files scripted to throw the
/// sharing-violation <see cref="IOException"/> of a locked file; their seam control reads every file normally. The
/// linked-root cases (hostile validation round 5 finding R5-F1; operator 2026-10-21: "Fix both, strengthen ACs")
/// report a cache or history root, or a folder between the profile and it, as a link and prove it contributes
/// nothing, while a profile reported as a link by its own parent is still scanned.
/// </remarks>
public sealed class UserProfileScanBindingTests : IDisposable
{
    private const string UntrustedMountPointMessage =
        "The path cannot be traversed because it contains an untrusted mount point.";

    private const string PluginName = "mcpserver-scan-binding-plugin";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"mcp-scan-binding-{Guid.NewGuid():N}");

    /// <summary>Creates the temporary root that holds the profile and the workspace.</summary>
    public UserProfileScanBindingTests() => Directory.CreateDirectory(_root);

    /// <summary>Deletes the temporary root.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private string Profile => Path.Combine(_root, "profile");

    private string Workspace => Path.Combine(_root, "workspace");

    private static bool SamePath(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.Ordinal);

    /// <summary>
    /// A walker over the real temporary profile in which <paramref name="unlistable"/> throws the untrusted-mount-point
    /// error and <paramref name="link"/> (when given) is reported as a directory link.
    /// </summary>
    private static DirectoryTreeWalker ScriptedWalker(string unlistable, string? link) =>
        new(directory =>
        {
            if (SamePath(directory, unlistable))
                throw new IOException(UntrustedMountPointMessage);
            return DirectoryTreeWalker.ListFileSystem(directory)
                .Select(entry => link is not null && SamePath(entry.Path, link) ? entry with { IsLink = true } : entry)
                .ToList();
        });

    private (string Unlistable, string Linked) CreatePluginCacheProfile()
    {
        var cache = Path.Combine(Profile, ".claude", "plugins", "cache");
        var reachable = Path.Combine(cache, PluginName, "7.7.7");
        var linked = Path.Combine(cache, PluginName, "9.9.9");
        var unlistable = Path.Combine(cache, "caveman");
        Directory.CreateDirectory(reachable);
        Directory.CreateDirectory(linked);
        Directory.CreateDirectory(Path.Combine(unlistable, "compress", "scripts"));
        Directory.CreateDirectory(Workspace);
        File.WriteAllText(Path.Combine(reachable, ".version"), "7.7.7");
        File.WriteAllText(Path.Combine(linked, ".version"), "9.9.9");
        return (unlistable, linked);
    }

    /// <summary>
    /// Test-owned resolution sources: no plugin-root variable, the production sibling rule applied to the temporary
    /// workspace (a folder that is never created), and the temporary profile.
    /// </summary>
    private AgentPluginVersionSources Sources => new(_ => null, MarkerFileService.SiblingPluginRoot, Profile);

    private string Resolve(DirectoryTreeWalker walker) =>
        MarkerFileService.ResolveAgentPluginVersion(
            Workspace,
            PluginName,
            "CLAUDE_PLUGIN_ROOT",
            Sources,
            walker);

    /// <summary>
    /// AC-TR-MCP-WS-005-001: with an untrusted mount point and a linked higher version in the Claude plugin cache,
    /// resolution does not throw and returns the reachable version; with the link flag removed (seam control) the
    /// higher version wins, so the link is what was skipped.
    /// </summary>
    [Fact]
    public void ResolveAgentPluginVersion_UnlistableAndLinkedCacheDirectories_ResolveTheReachableVersion()
    {
        var (unlistable, linked) = CreatePluginCacheProfile();

        Assert.Equal("7.7.7", Resolve(ScriptedWalker(unlistable, linked)));
        Assert.Equal("9.9.9", Resolve(ScriptedWalker(unlistable, link: null)));
    }

    /// <summary>
    /// AC-TR-MCP-WS-005-001: when the plugin cache root itself cannot be listed, resolution does not throw and
    /// falls back to the synced plugin version.
    /// </summary>
    [Fact]
    public void ResolveAgentPluginVersion_CacheRootUnlistable_FallsBackToTheSyncedVersion()
    {
        CreatePluginCacheProfile();
        var cacheRoot = Path.Combine(Profile, ".claude", "plugins", "cache");

        Assert.Equal(MarkerFileService.SyncedAgentPluginVersion, Resolve(ScriptedWalker(cacheRoot, link: null)));
    }

    /// <summary>
    /// The file system read, except that every path in <paramref name="unreadable"/> throws the sharing-violation
    /// <see cref="IOException"/> a locked file raises.
    /// </summary>
    private static Func<string, string?> ScriptedReader(params string[] unreadable) =>
        path => unreadable.Any(locked => SamePath(path, locked))
            ? throw new IOException("The process cannot access the file because it is being used by another process.")
            : MarkerFileService.ReadExistingFileText(path);

    private string ResolveWithReader(Func<string, string?> readText) =>
        MarkerFileService.ResolveAgentPluginVersion(
            Workspace,
            PluginName,
            "CLAUDE_PLUGIN_ROOT",
            Sources,
            DirectoryTreeWalker.FileSystem,
            readText);

    /// <summary>
    /// AC-TR-MCP-WS-005-001: when the higher cached version's <c>.version</c> cannot be read, resolution does not
    /// throw and returns the next readable version; with the plain file read (seam control) the higher version wins,
    /// so the unreadable file is what was passed over.
    /// </summary>
    [Fact]
    public void ResolveAgentPluginVersion_UnreadableVersionFile_ResolvesTheNextReadableVersion()
    {
        var (_, higher) = CreatePluginCacheProfile();

        Assert.Equal("7.7.7", ResolveWithReader(ScriptedReader(Path.Combine(higher, ".version"))));
        Assert.Equal("9.9.9", ResolveWithReader(MarkerFileService.ReadExistingFileText));
    }

    /// <summary>
    /// AC-TR-MCP-WS-005-001: when every cached version file cannot be read, resolution does not throw and falls back
    /// to the synced plugin version.
    /// </summary>
    [Fact]
    public void ResolveAgentPluginVersion_EveryVersionFileUnreadable_FallsBackToTheSyncedVersion()
    {
        var (_, higher) = CreatePluginCacheProfile();
        var lower = Path.Combine(Path.GetDirectoryName(higher)!, "7.7.7");

        Assert.Equal(
            MarkerFileService.SyncedAgentPluginVersion,
            ResolveWithReader(ScriptedReader(Path.Combine(higher, ".version"), Path.Combine(lower, ".version"))));
    }

    private (string Unlistable, string Linked) CreateHistoryProfile()
    {
        var claude = Path.Combine(Profile, ".claude");
        var history = Path.Combine(claude, "projects", "agent-scanbinding");
        var unlistable = Path.Combine(claude, "plugins", "marketplaces", "caveman");
        var linked = Path.Combine(claude, "plugins", "cache", "linked");
        Directory.CreateDirectory(history);
        Directory.CreateDirectory(Path.Combine(unlistable, "compress", "scripts"));
        Directory.CreateDirectory(Path.Combine(linked, "agent-scanbinding"));
        File.WriteAllText(Path.Combine(history, "chat.jsonl"), "working PLAN-SCANBINDING-001");
        File.WriteAllText(Path.Combine(linked, "agent-scanbinding", "chat.jsonl"), "working PLAN-LINKEDCOPY-001");
        return (unlistable, linked);
    }

    /// <summary>
    /// AC-TR-MCP-SESSIONLOG-006-009: with an untrusted mount point and a linked history folder under ~/.claude, the
    /// extractor does not throw and takes the TODO id from the reachable history only; with the link flag removed
    /// (seam control) the linked copy is read too and the tied ids yield None.
    /// </summary>
    [Fact]
    public void Extract_UnlistableAndLinkedHistoryDirectories_ReadTheReachableHistory()
    {
        var (unlistable, linked) = CreateHistoryProfile();

        var skipped = new SessionLogTurnContextExtractor(ScriptedWalker(unlistable, linked))
            .Extract(new SessionLogTurnEntity(), workspacePath: null, userProfilePath: Profile, agentSessionId: "agent-scanbinding");
        var followed = new SessionLogTurnContextExtractor(ScriptedWalker(unlistable, link: null))
            .Extract(new SessionLogTurnEntity(), workspacePath: null, userProfilePath: Profile, agentSessionId: "agent-scanbinding");

        Assert.Equal("PLAN-SCANBINDING-001", skipped.TodoId);
        Assert.Equal(SessionLogTurnContextValidator.NoneSentinel, followed.TodoId);
    }

    /// <summary>
    /// The cache layouts of AC-TR-MCP-WS-005-001, each with the folder (relative to the profile) a case reports as a
    /// link: the cache root itself or one of the folders between the profile and it (hostile validation round 5
    /// finding R5-F1).
    /// </summary>
    public static TheoryData<string, string> LinkedCacheComponents => new()
    {
        { "codex", ".codex" },
        { "codex", ".codex/plugins" },
        { "codex", ".codex/plugins/cache" },
        { "codex", ".codex/plugins/cache/" + PluginName },
        { "claude", ".claude" },
        { "claude", ".claude/plugins" },
        { "claude", ".claude/plugins/cache" },
        { "grok", ".grok" },
        { "grok", ".grok/installed-plugins" },
    };

    /// <summary>A path the scripted walker treats as unlistable that no case creates.</summary>
    private string NoUnlistableFolder => Path.Combine(_root, "no-such-folder");

    private void CreateSingleCacheProfile(string kind)
    {
        var versionRoot = kind switch
        {
            "codex" => Path.Combine(Profile, ".codex", "plugins", "cache", PluginName, "9.9.9"),
            "claude" => Path.Combine(Profile, ".claude", "plugins", "cache", PluginName, "9.9.9"),
            "grok" => Path.Combine(Profile, ".grok", "installed-plugins", PluginName, "9.9.9"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown cache layout."),
        };
        Directory.CreateDirectory(versionRoot);
        Directory.CreateDirectory(Workspace);
        File.WriteAllText(Path.Combine(versionRoot, ".version"), "9.9.9");
    }

    /// <summary>
    /// AC-TR-MCP-WS-005-001: with the only cached version under a cache root that is a link, or under a folder
    /// between the profile and that root that is a link, resolution does not throw and falls back to the synced
    /// plugin version; with the link flag removed (seam control) the cached version wins, so the link at or above
    /// the root is what was skipped.
    /// </summary>
    /// <param name="kind">Cache layout: codex, claude or grok.</param>
    /// <param name="linkedRelative">The folder, relative to the profile, reported as a link.</param>
    [Theory]
    [MemberData(nameof(LinkedCacheComponents))]
    public void ResolveAgentPluginVersion_LinkedCacheRootOrAncestor_FallsBackToTheSyncedVersion(string kind, string linkedRelative)
    {
        CreateSingleCacheProfile(kind);
        var linked = Path.Combine(Profile, linkedRelative.Replace('/', Path.DirectorySeparatorChar));

        Assert.Equal(MarkerFileService.SyncedAgentPluginVersion, Resolve(ScriptedWalker(NoUnlistableFolder, linked)));
        Assert.Equal("9.9.9", Resolve(ScriptedWalker(NoUnlistableFolder, link: null)));
    }

    /// <summary>
    /// AC-TR-MCP-WS-005-001: a user profile that is itself a link is still scanned, so the cached version resolves
    /// with the profile reported as a link by its parent listing, which the scan never reads.
    /// </summary>
    [Fact]
    public void ResolveAgentPluginVersion_LinkedProfile_IsStillScanned()
    {
        CreateSingleCacheProfile("claude");

        Assert.Equal("9.9.9", Resolve(ScriptedWalker(NoUnlistableFolder, Profile)));
    }

    /// <summary>The four agent-history roots of AC-TR-MCP-SESSIONLOG-006-009.</summary>
    public static TheoryData<string> HistoryRoots => new() { ".grok", ".claude", ".codex", ".cursor" };

    /// <summary>
    /// AC-TR-MCP-SESSIONLOG-006-009: when a history root is a link, the extractor does not throw and the turn keeps
    /// None; with the link flag removed (seam control) the history under it yields the TODO id, and a home that is
    /// itself a link is still scanned (hostile validation round 5 finding R5-F1).
    /// </summary>
    /// <param name="rootName">The history root under the profile.</param>
    [Theory]
    [MemberData(nameof(HistoryRoots))]
    public void Extract_LinkedHistoryRoot_KeepsNone(string rootName)
    {
        var history = Path.Combine(Profile, rootName, "projects", "agent-linkedroot");
        Directory.CreateDirectory(history);
        File.WriteAllText(Path.Combine(history, "chat.jsonl"), "working PLAN-LINKEDROOT-001");

        var linkedRoot = new SessionLogTurnContextExtractor(ScriptedWalker(NoUnlistableFolder, Path.Combine(Profile, rootName)))
            .Extract(new SessionLogTurnEntity(), workspacePath: null, userProfilePath: Profile, agentSessionId: "agent-linkedroot");
        var plain = new SessionLogTurnContextExtractor(ScriptedWalker(NoUnlistableFolder, link: null))
            .Extract(new SessionLogTurnEntity(), workspacePath: null, userProfilePath: Profile, agentSessionId: "agent-linkedroot");
        var linkedHome = new SessionLogTurnContextExtractor(ScriptedWalker(NoUnlistableFolder, Profile))
            .Extract(new SessionLogTurnEntity(), workspacePath: null, userProfilePath: Profile, agentSessionId: "agent-linkedroot");

        Assert.Equal(SessionLogTurnContextValidator.NoneSentinel, linkedRoot.TodoId);
        Assert.Equal("PLAN-LINKEDROOT-001", plain.TodoId);
        Assert.Equal("PLAN-LINKEDROOT-001", linkedHome.TodoId);
    }

    /// <summary>
    /// AC-TR-MCP-SESSIONLOG-006-009: when ~/.claude itself cannot be listed, the extractor does not throw and the
    /// turn keeps None.
    /// </summary>
    [Fact]
    public void Extract_HistoryRootUnlistable_KeepsNone()
    {
        CreateHistoryProfile();

        var result = new SessionLogTurnContextExtractor(ScriptedWalker(Path.Combine(Profile, ".claude"), link: null))
            .Extract(new SessionLogTurnEntity(), workspacePath: null, userProfilePath: Profile, agentSessionId: "agent-scanbinding");

        Assert.Equal(SessionLogTurnContextValidator.NoneSentinel, result.TodoId);
        Assert.Equal(SessionLogTurnContextValidator.NoneSentinel, result.PlanFile);
    }
}
