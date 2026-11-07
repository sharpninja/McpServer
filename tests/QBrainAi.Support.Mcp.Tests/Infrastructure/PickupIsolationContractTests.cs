using QBrainAi.Client;
using QBrainAi.Support.Mcp.Services;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Infrastructure;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-8, TR-MCP-USECASE-018 AC-005/AC-006, FR-MCP-018 ac-1:
/// explicit-input contracts for uncached repository lookup, blank SQL overrides and canonical scan reachability.
/// Fixed virtual roots, listings and strings provide all data; no environment, clock or filesystem is read.
/// The same assertions bind scripted controls first and the implementation after the mock gate.
/// </summary>
public abstract class PickupIsolationContractTests
{
    /// <summary>Uncached lookup through explicit path and repository predicates.</summary>
    protected abstract string Locate(string? configured, string? buildRoot, string?[] starts,
        Func<string, string> normalize, Func<string, bool> isRoot, Func<string, string?> parent);

    /// <summary>SQL override selection over an explicit value.</summary>
    protected abstract string ResolveSql(string? value);

    /// <summary>Reachability through explicit listings and path-platform policy.</summary>
    private protected abstract bool Reach(string boundary, string root, WorkspacePathPlatform platform,
        Func<string, IReadOnlyList<DirectoryListingEntry>> list);

    /// <summary>Null, empty and whitespace overrides use the pinned 120-second LocalDB default.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void UnsetSqlOverride_UsesDefault(string? value) => AssertSqlDefault(ResolveSql(value));

    /// <summary>An explicit connection string, including surrounding spaces, remains byte-for-byte unchanged.</summary>
    [Fact]
    public void ExplicitSqlOverride_IsPreserved()
    {
        const string value = " Server=database.example;Database=master;Encrypt=False ";
        Assert.Equal(value, ResolveSql(value));
    }

    /// <summary>Configured roots win, are normalized, and are re-evaluated on every call.</summary>
    [Fact]
    public void RepositoryLookup_ConfiguredPriorityAndNoCache()
    {
        Assert.Equal("/first", InvokeLocate("/first/.", "/build", ["/start"]));
        Assert.Equal("/second", InvokeLocate("/second", "/build", ["/start"]));
        Assert.Equal("/build", InvokeLocate(" \t", "/build", ["/start"]));
    }

    /// <summary>An invalid configured or metadata root fails closed instead of choosing another checkout.</summary>
    [Fact]
    public void RepositoryLookup_InvalidExplicitRoot_DoesNotFallBack()
    {
        Assert.Throws<InvalidOperationException>(() => InvokeLocate("/invalid", "/build", ["/first"]));
        Assert.Throws<InvalidOperationException>(() => InvokeLocate(null, "/invalid", ["/first"]));
    }

    /// <summary>Blank starts are ignored, ancestors are searched in start order, and absence fails explicitly.</summary>
    [Fact]
    public void RepositoryLookup_AncestorFallbackAndAbsence()
    {
        Assert.Equal("/second", InvokeLocate(null, null, [null, " ", "/missing/child", "/second/child"]));
        Assert.Throws<InvalidOperationException>(() => InvokeLocate(null, null, ["/missing/child"]));
    }

    private string InvokeLocate(string? configured, string? buildRoot, string?[] starts) =>
        Locate(configured, buildRoot, starts,
            value => value.EndsWith("/.", StringComparison.Ordinal) ? value[..^2] : value,
            value => value is "/first" or "/second" or "/build",
            value => value.LastIndexOf('/') > 0 ? value[..value.LastIndexOf('/')] : null);

    /// <summary>Equivalent Windows separators, dot segments and case still reach the owned scan root.</summary>
    [Fact]
    public void Reachability_CanonicalWindowsSpellings()
    {
        var listed = new List<string>();
        Assert.True(Reach(@"C:\Owned\Profile\.", "c:/owned/profile/plugins/cache/", WorkspacePathPlatform.Windows,
            path =>
            {
                listed.Add(path);
                if (string.Equals(path, "C:/Owned/Profile", StringComparison.OrdinalIgnoreCase))
                    return [new(@"C:\Owned\Profile\plugins\.", true, false)];
                Assert.Equal("C:/Owned/Profile/plugins", path, ignoreCase: true);
                return [new("c:/owned/profile/plugins/cache/", true, false)];
            }));
        Assert.Equal(2, listed.Count);
    }

    /// <summary>Canonicalization preserves refusal of links and sibling prefixes, and trusts the boundary itself.</summary>
    [Fact]
    public void Reachability_CanonicalizationDoesNotBypassContainment()
    {
        Assert.True(Reach("/owned/profile/.", "/owned/profile/", WorkspacePathPlatform.CaseSensitive,
            _ => throw new InvalidOperationException("Boundary equality must not list anything.")));
        Assert.False(Reach("/owned/profile", "/owned/profile/plugins/cache", WorkspacePathPlatform.CaseSensitive,
            _ => [new("/owned/profile/plugins/.", true, true)]));
        Assert.False(Reach("/owned/profile", "/owned/profile-other/cache", WorkspacePathPlatform.CaseSensitive,
            _ => [new("/owned/profile/plugins", true, false)]));
        Assert.False(Reach("/owned/profile", "/owned/profile/Plugins", WorkspacePathPlatform.CaseSensitive,
            _ => [new("/owned/profile/plugins", true, false)]));
    }

    /// <summary>Pinned default assertions shared with deliberately wrong defaults.</summary>
    internal static void AssertSqlDefault(string value)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(value);
        Assert.Equal(@"(localdb)\MSSQLLocalDB", builder.DataSource);
        Assert.True(builder.IntegratedSecurity);
        Assert.True(builder.TrustServerCertificate);
        Assert.Equal(120, builder.ConnectTimeout);
        Assert.Empty(builder.InitialCatalog);
    }
}

/// <summary>Mock gate: independent explicit reference adapters plus assertion-failure negative controls.</summary>
public sealed class PickupIsolationMockTests : PickupIsolationContractTests
{
    private readonly string? _fault;

    /// <summary>Constructs the correct reference adapter used for the mock gate.</summary>
    public PickupIsolationMockTests() { }

    private PickupIsolationMockTests(string fault) => _fault = fault;

    /// <inheritdoc />
    protected override string ResolveSql(string? value) => _fault == "blank-passthrough" && value is not null ? value : string.IsNullOrWhiteSpace(value)
        ? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=120"
        : value;

    /// <inheritdoc />
    protected override string Locate(string? configured, string? buildRoot, string?[] starts,
        Func<string, string> normalize, Func<string, bool> isRoot, Func<string, string?> parent)
    {
        if (_fault == "cached-root") return "/first";
        if (_fault == "invalid-fallback") return "/build";
        foreach (var candidate in new[] { configured, buildRoot })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            var root = normalize(candidate);
            return isRoot(root) ? root : throw new InvalidOperationException("Explicit root rejected.");
        }
        foreach (var start in starts.Where(start => !string.IsNullOrWhiteSpace(start)))
        {
            var current = normalize(start!);
            while (current is not null)
            {
                if (isRoot(current)) return current;
                current = parent(current);
            }
        }
        throw new InvalidOperationException("No repository.");
    }

    /// <inheritdoc />
    private protected override bool Reach(string boundary, string root, WorkspacePathPlatform platform,
        Func<string, IReadOnlyList<DirectoryListingEntry>> list)
    {
        if (_fault == "raw-path") return false;
        if (_fault == "unchecked-root") return true;
        // Fixed contract-derived answers; exercise the prescribed listing interactions, not production normalization.
        if (platform == WorkspacePathPlatform.Windows)
        {
            var top = Assert.Single(list("C:/Owned/Profile"));
            var child = Assert.Single(list("C:/Owned/Profile/plugins"));
            return !top.IsLink && !child.IsLink;
        }
        if (boundary == "/owned/profile/.") return true;
        return false;
    }

    /// <summary>Blank passthrough, cached roots, fallback after invalid override and raw path equality are rejected.</summary>
    [Fact]
    public void WrongAdapters_AreRejectedByTheContractAssertions()
    {
        foreach (var blank in new[] { "", " \t" })
            Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
                new PickupIsolationMockTests("blank-passthrough").UnsetSqlOverride_UsesDefault(blank)));
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
            new PickupIsolationMockTests("cached-root").RepositoryLookup_ConfiguredPriorityAndNoCache()));
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
            new PickupIsolationMockTests("invalid-fallback").RepositoryLookup_InvalidExplicitRoot_DoesNotFallBack()));
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
            new PickupIsolationMockTests("raw-path").Reachability_CanonicalWindowsSpellings()));
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() =>
            new PickupIsolationMockTests("unchecked-root").Reachability_CanonicalizationDoesNotBypassContainment()));
    }
}
