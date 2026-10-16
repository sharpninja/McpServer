using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.Services;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// Probe over the workspace discriminator of a database context: what the context reports and what
/// it persists. Lets the same contract run against scripted data and a real <see cref="McpDbContext"/>.
/// </summary>
public interface IWorkspaceDiscriminatorProbe
{
    /// <summary>Discriminator currently applied to queries and stamping.</summary>
    string CurrentWorkspaceId { get; }

    /// <summary>Overrides the discriminator.</summary>
    void Override(string workspaceId);

    /// <summary>Pushes a discriminator; disposing restores the previous one.</summary>
    IDisposable Push(string workspaceId);

    /// <summary>Saves a TODO row (explicit WorkspaceId, or empty to stamp) and returns the stored WorkspaceId.</summary>
    Task<string> SaveTodoAsync(string id, string explicitWorkspaceId);

    /// <summary>Registers a workspace row whose id and path are set to <paramref name="workspacePath"/> verbatim.</summary>
    Task RegisterWorkspaceAsync(string workspacePath);

    /// <summary>True when a workspace row matches <paramref name="workspaceId"/> by id (query filters ignored).</summary>
    Task<bool> WorkspaceExistsByIdAsync(string workspaceId);

    /// <summary>True when a workspace row matches <paramref name="workspacePath"/> by path (query filters ignored).</summary>
    Task<bool> WorkspaceExistsByPathAsync(string workspacePath);

    /// <summary>Counts TODO rows whose WorkspaceId equals <paramref name="workspaceId"/> (query filters ignored).</summary>
    Task<int> CountTodosAsync(string workspaceId);

    /// <summary>Saves a TODO row with an explicit WorkspaceId and returns the tracked instance's WorkspaceId.</summary>
    Task<string> SaveTodoTrackedAsync(string id, string explicitWorkspaceId);
}

/// <summary>
/// TEST-MCP-PATHNORM-001 / FR-MCP-PATHNORM-001 (plan P1.4a): the database boundary canonicalizes
/// path-shaped workspace discriminators to the lexical canonical form (forward slashes) on every
/// entry point (constructor, override, push, explicit and stamped WorkspaceId on save). Opaque ids
/// (hub hash ids, relative values, tagged keys) and the empty global id pass through unchanged.
/// BDP v4 (plan section 2a): MockData fixture then Real fixture.
/// </summary>
public abstract class WorkspaceDiscriminatorContractTests
{
    private const string HubHashId = "5D7B660CC7E8F984913D8CEBCFEB84C8408F2F3D896728AA3E5AAB525E6C2BB3";

    /// <summary>The canonicalization function under test.</summary>
    protected abstract string Canonicalize(string? workspaceId);

    /// <summary>A discriminator probe whose context was constructed with <paramref name="contextWorkspacePath"/>.</summary>
    protected abstract IWorkspaceDiscriminatorProbe CreateProbe(string? contextWorkspacePath);

    /// <summary>Path-shaped values become canonical; everything else is unchanged.</summary>
    [Theory]
    [InlineData(@"Q:\ws\a", "Q:/ws/a")]
    [InlineData("Q:/ws/a/", "Q:/ws/a")]
    [InlineData(@"q:\WS\A", "q:/WS/A")]
    [InlineData(@"\\srv\share\x", "//srv/share/x")]
    [InlineData("/home/x/./y/", "/home/x/y")]
    [InlineData(HubHashId, HubHashId)]
    [InlineData("relative/x", "relative/x")]
    [InlineData(@"windows:WS:\TEST", @"windows:WS:\TEST")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Canonicalize_PathShapedOnly(string? input, string expected)
    {
        Assert.Equal(expected, Canonicalize(input));
    }

    /// <summary>Constructor, override and push all canonicalize; disposing a push restores.</summary>
    [Fact]
    public void Discriminator_EntryPoints_Canonicalize()
    {
        var probe = CreateProbe(@"Q:\ws\a");
        Assert.Equal("Q:/ws/a", probe.CurrentWorkspaceId);

        probe.Override(@"Q:\ws\b");
        Assert.Equal("Q:/ws/b", probe.CurrentWorkspaceId);

        using (probe.Push(@"Q:\ws\c"))
            Assert.Equal("Q:/ws/c", probe.CurrentWorkspaceId);

        Assert.Equal("Q:/ws/b", probe.CurrentWorkspaceId);
    }

    /// <summary>Opaque discriminators (hub hash ids, the global empty id) are not altered.</summary>
    [Theory]
    [InlineData(HubHashId)]
    [InlineData("")]
    public void Discriminator_OpaqueIds_Unchanged(string opaque)
    {
        var probe = CreateProbe(null);

        probe.Override(opaque);

        Assert.Equal(opaque, probe.CurrentWorkspaceId);
    }

    /// <summary>Saved rows carry the canonical WorkspaceId whether stamped or set explicitly.</summary>
    [Fact]
    public async Task Save_StampedAndExplicitIds_AreCanonical()
    {
        var probe = CreateProbe(@"Q:\ws\a");

        Assert.Equal("Q:/ws/a", await probe.SaveTodoAsync("DISC-STAMP-001", string.Empty));
        Assert.Equal("Q:/ws/explicit", await probe.SaveTodoAsync("DISC-EXPL-001", @"Q:\ws\explicit"));
    }

    /// <summary>
    /// Predicates that compare a caller-supplied path with a stored workspace key (id or path) match
    /// the canonical row whichever separator the caller used, and do not match other workspaces.
    /// </summary>
    [Fact]
    public async Task Predicates_RawOrCanonicalPath_MatchCanonicalRows()
    {
        var probe = CreateProbe(@"Q:\ws\a");
        await probe.RegisterWorkspaceAsync(@"Q:\ws\reg");
        await probe.SaveTodoAsync("DISC-PRED-001", string.Empty);

        Assert.True(await probe.WorkspaceExistsByIdAsync(@"Q:\ws\reg"));
        Assert.True(await probe.WorkspaceExistsByIdAsync("Q:/ws/reg"));
        Assert.True(await probe.WorkspaceExistsByPathAsync(@"Q:\ws\reg"));
        Assert.True(await probe.WorkspaceExistsByPathAsync("Q:/ws/reg"));
        Assert.False(await probe.WorkspaceExistsByIdAsync(@"Q:\ws\other"));
        Assert.Equal(1, await probe.CountTodosAsync(@"Q:\ws\a"));
        Assert.Equal(1, await probe.CountTodosAsync("Q:/ws/a"));
    }

    /// <summary>
    /// After save, the tracked instance carries the same canonical WorkspaceId as the stored row, so
    /// later tracking queries that return that instance do not expose the caller's raw spelling.
    /// </summary>
    [Fact]
    public async Task Save_TrackedInstance_CarriesCanonicalId()
    {
        var probe = CreateProbe(@"Q:\ws\a");

        Assert.Equal("Q:/ws/explicit", await probe.SaveTodoTrackedAsync("DISC-TRACK-001", @"Q:\ws\explicit"));
    }
}

/// <summary>BDP v4 step 2: discriminator contract against scripted data.</summary>
public sealed class WorkspaceDiscriminatorMockDataTests : WorkspaceDiscriminatorContractTests
{
    private static readonly Dictionary<string, string> Canonical = new(StringComparer.Ordinal)
    {
        [@"Q:\ws\a"] = "Q:/ws/a",
        ["Q:/ws/a/"] = "Q:/ws/a",
        [@"q:\WS\A"] = "q:/WS/A",
        [@"\\srv\share\x"] = "//srv/share/x",
        ["/home/x/./y/"] = "/home/x/y",
        ["5D7B660CC7E8F984913D8CEBCFEB84C8408F2F3D896728AA3E5AAB525E6C2BB3"] = "5D7B660CC7E8F984913D8CEBCFEB84C8408F2F3D896728AA3E5AAB525E6C2BB3",
        ["relative/x"] = "relative/x",
        [@"windows:WS:\TEST"] = @"windows:WS:\TEST",
        [""] = "",
        ["Q:/ws/a"] = "Q:/ws/a",
        [@"Q:\ws\b"] = "Q:/ws/b",
        [@"Q:\ws\c"] = "Q:/ws/c",
        [@"Q:\ws\explicit"] = "Q:/ws/explicit",
        [@"Q:\ws\reg"] = "Q:/ws/reg",
        ["Q:/ws/reg"] = "Q:/ws/reg",
        [@"Q:\ws\other"] = "Q:/ws/other",
    };

    /// <inheritdoc />
    protected override string Canonicalize(string? workspaceId) => Lookup(workspaceId);

    /// <inheritdoc />
    protected override IWorkspaceDiscriminatorProbe CreateProbe(string? contextWorkspacePath) =>
        new ScriptedProbe(Lookup(contextWorkspacePath));

    private static string Lookup(string? value) =>
        Canonical.TryGetValue(value ?? string.Empty, out var canonical)
            ? canonical
            : throw new InvalidOperationException($"No scripted discriminator data for '{value}'.");

    private sealed class ScriptedProbe(string initial) : IWorkspaceDiscriminatorProbe
    {
        private readonly HashSet<string> _workspaces = new(StringComparer.Ordinal);
        private readonly List<string> _todoWorkspaceIds = [];

        public string CurrentWorkspaceId { get; private set; } = initial;

        public void Override(string workspaceId) => CurrentWorkspaceId = Lookup(workspaceId);

        public IDisposable Push(string workspaceId)
        {
            var previous = CurrentWorkspaceId;
            CurrentWorkspaceId = Lookup(workspaceId);
            return new Restore(() => CurrentWorkspaceId = previous);
        }

        public Task<string> SaveTodoAsync(string id, string explicitWorkspaceId)
        {
            var stored = explicitWorkspaceId.Length == 0 ? CurrentWorkspaceId : Lookup(explicitWorkspaceId);
            _todoWorkspaceIds.Add(stored);
            return Task.FromResult(stored);
        }

        public Task RegisterWorkspaceAsync(string workspacePath)
        {
            _workspaces.Add(Lookup(workspacePath));
            return Task.CompletedTask;
        }

        public Task<bool> WorkspaceExistsByIdAsync(string workspaceId) =>
            Task.FromResult(_workspaces.Contains(Lookup(workspaceId)));

        public Task<bool> WorkspaceExistsByPathAsync(string workspacePath) =>
            Task.FromResult(_workspaces.Contains(Lookup(workspacePath)));

        public Task<int> CountTodosAsync(string workspaceId)
        {
            var key = Lookup(workspaceId);
            return Task.FromResult(_todoWorkspaceIds.Count(stored => stored == key));
        }

        public Task<string> SaveTodoTrackedAsync(string id, string explicitWorkspaceId) =>
            SaveTodoAsync(id, explicitWorkspaceId);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
