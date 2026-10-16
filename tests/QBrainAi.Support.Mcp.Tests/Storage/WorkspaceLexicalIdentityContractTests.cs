using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PATHNORM-001 / FR-MCP-PATHNORM-001 (plan P1.4a/P1.4b): lexical workspace identity
/// persists Windows paths with forward slashes, keeps the UNC leading "//", strips Win32 device
/// prefixes, keeps the windows:/case-sensitive: tag, and compares/contains by that form.
/// BDP v4 (plan section 2a): <see cref="WorkspaceLexicalIdentityMockDataTests"/> proves these tests
/// against a literal data table before production code; <see cref="WorkspaceLexicalIdentityRealTests"/>
/// binds them to <see cref="WorkspaceIdentityPath"/>.
/// </summary>
public abstract class WorkspaceLexicalIdentityContractTests
{
    /// <summary>Lexical normalization under an explicit platform.</summary>
    protected abstract string NormalizeLexical(string path, WorkspacePathPlatform platform);

    /// <summary>Durable storage key selected from path syntax.</summary>
    protected abstract string StorageKey(string path);

    /// <summary>Lexical equivalence of two paths.</summary>
    protected abstract bool AreEquivalent(string left, string right);

    /// <summary>Lexical containment of a candidate beneath a root.</summary>
    protected abstract bool IsWithinRoot(string root, string candidate);

    /// <summary>FR-MCP-PATHNORM-001-AC1: drive paths persist with forward slashes, case preserved.</summary>
    [Theory]
    [InlineData(@"C:\Users\kingd", "C:/Users/kingd")]
    [InlineData("C:/Users/kingd", "C:/Users/kingd")]
    [InlineData(@"C:\Users/kingd\", "C:/Users/kingd")]
    [InlineData(@"c:\users\KINGD", "c:/users/KINGD")]
    [InlineData(@"C:\a/b\..\.\c", "C:/a/c")]
    [InlineData(@"C:\", "C:/")]
    [InlineData("C:/", "C:/")]
    public void DrivePath_NormalizesToForwardSlashes(string input, string expected)
    {
        Assert.Equal(expected, NormalizeLexical(input, WorkspacePathPlatform.Windows));
    }

    /// <summary>FR-MCP-PATHNORM-001-AC2: both UNC spellings normalize to one form keeping "//".</summary>
    [Theory]
    [InlineData(@"\\srv\share\x", "//srv/share/x")]
    [InlineData("//srv/share/x", "//srv/share/x")]
    [InlineData(@"\\srv\share", "//srv/share/")]
    [InlineData(@"\\srv\share\a\..\x", "//srv/share/x")]
    public void UncPath_NormalizesToDoubleSlashForm(string input, string expected)
    {
        Assert.Equal(expected, NormalizeLexical(input, WorkspacePathPlatform.Windows));
    }

    /// <summary>TR-MCP-PATHNORM-001: Win32 device prefixes are stripped to the drive or UNC form.</summary>
    [Theory]
    [InlineData(@"\\?\C:\x", "C:/x")]
    [InlineData(@"\\?\UNC\srv\share\x", "//srv/share/x")]
    public void DevicePrefix_IsStripped(string input, string expected)
    {
        Assert.Equal(expected, NormalizeLexical(input, WorkspacePathPlatform.Windows));
    }

    /// <summary>FR-MCP-PATHNORM-001: POSIX paths are unchanged by the separator rule.</summary>
    [Theory]
    [InlineData("/home/x", "/home/x")]
    [InlineData("/home/./x/", "/home/x")]
    public void PosixPath_IsUnchanged(string input, string expected)
    {
        Assert.Equal(expected, NormalizeLexical(input, WorkspacePathPlatform.CaseSensitive));
    }

    /// <summary>
    /// FR-MCP-PATHNORM-001 (plan 4c.2, "opaque identifiers unchanged"): Windows-platform values with no
    /// drive, UNC or root (opaque identifiers, relative values) keep their spelling; only rooted paths
    /// switch to forward slashes.
    /// </summary>
    [Theory]
    [InlineData(@"ws:\test", @"ws:\test")]
    [InlineData(@"relative\x", @"relative\x")]
    public void UnrootedWindowsValue_KeepsSpelling(string input, string expected)
    {
        Assert.Equal(expected, NormalizeLexical(input, WorkspacePathPlatform.Windows));
    }

    /// <summary>FR-MCP-PATHNORM-001-AC1/AC2: one storage key per identity, forward slashes, tag kept.</summary>
    [Theory]
    [InlineData(@"C:\Users\kingd", "windows:C:/USERS/KINGD")]
    [InlineData("C:/Users/kingd", "windows:C:/USERS/KINGD")]
    [InlineData(@"c:\users\KINGD", "windows:C:/USERS/KINGD")]
    [InlineData(@"\\srv\share\x", "windows://SRV/SHARE/X")]
    [InlineData("//srv/share/x", "windows://SRV/SHARE/X")]
    [InlineData("/home/x", "case-sensitive:/home/x")]
    public void StorageKey_UsesForwardSlashesAndPlatformTag(string input, string expected)
    {
        Assert.Equal(expected, StorageKey(input));
    }

    /// <summary>FR-MCP-PATHNORM-001-AC1: separator spelling and Windows case do not change identity.</summary>
    [Theory]
    [InlineData(@"C:\Users\kingd", "c:/users/KINGD", true)]
    [InlineData(@"\\srv\share\x", "//SRV/share/x", true)]
    [InlineData("/home/x", "/home/X", false)]
    [InlineData(@"C:\Users\kingd", "/Users/kingd", false)]
    public void AreEquivalent_IgnoresSeparatorSpelling(string left, string right, bool expected)
    {
        Assert.Equal(expected, AreEquivalent(left, right));
    }

    /// <summary>TR-MCP-PATHNORM-001: containment uses the forward-slash form for Windows paths.</summary>
    [Theory]
    [InlineData(@"C:\Users\kingd\repo", "C:/Users/kingd/repo/src/a.cs", true)]
    [InlineData("C:/Users/kingd/repo", @"C:\Users\kingd\repo", true)]
    [InlineData(@"C:\Users\kingd\repo", @"C:\Users\kingd\repoOther\a.cs", false)]
    [InlineData("//srv/share/x", @"\\srv\share\x\y", true)]
    [InlineData("/home/x", "/home/x/y", true)]
    [InlineData("/home/x", "/home/xy", false)]
    public void IsWithinRoot_UsesForwardSlashForm(string root, string candidate, bool expected)
    {
        Assert.Equal(expected, IsWithinRoot(root, candidate));
    }
}

/// <summary>BDP v4 step 2: lexical identity contract proven against a literal data table.</summary>
public sealed class WorkspaceLexicalIdentityMockDataTests : WorkspaceLexicalIdentityContractTests
{
    private static readonly Dictionary<(string, WorkspacePathPlatform), string> Lexical = new()
    {
        [(@"C:\Users\kingd", WorkspacePathPlatform.Windows)] = "C:/Users/kingd",
        [("C:/Users/kingd", WorkspacePathPlatform.Windows)] = "C:/Users/kingd",
        [(@"C:\Users/kingd\", WorkspacePathPlatform.Windows)] = "C:/Users/kingd",
        [(@"c:\users\KINGD", WorkspacePathPlatform.Windows)] = "c:/users/KINGD",
        [(@"C:\a/b\..\.\c", WorkspacePathPlatform.Windows)] = "C:/a/c",
        [(@"C:\", WorkspacePathPlatform.Windows)] = "C:/",
        [("C:/", WorkspacePathPlatform.Windows)] = "C:/",
        [(@"\\srv\share\x", WorkspacePathPlatform.Windows)] = "//srv/share/x",
        [("//srv/share/x", WorkspacePathPlatform.Windows)] = "//srv/share/x",
        [(@"\\srv\share", WorkspacePathPlatform.Windows)] = "//srv/share/",
        [(@"\\srv\share\a\..\x", WorkspacePathPlatform.Windows)] = "//srv/share/x",
        [(@"\\?\C:\x", WorkspacePathPlatform.Windows)] = "C:/x",
        [(@"\\?\UNC\srv\share\x", WorkspacePathPlatform.Windows)] = "//srv/share/x",
        [(@"ws:\test", WorkspacePathPlatform.Windows)] = @"ws:\test",
        [(@"relative\x", WorkspacePathPlatform.Windows)] = @"relative\x",
        [("/home/x", WorkspacePathPlatform.CaseSensitive)] = "/home/x",
        [("/home/./x/", WorkspacePathPlatform.CaseSensitive)] = "/home/x",
    };

    private static readonly Dictionary<string, string> Keys = new()
    {
        [@"C:\Users\kingd"] = "windows:C:/USERS/KINGD",
        ["C:/Users/kingd"] = "windows:C:/USERS/KINGD",
        ["c:/users/KINGD"] = "windows:C:/USERS/KINGD",
        [@"c:\users\KINGD"] = "windows:C:/USERS/KINGD",
        [@"\\srv\share\x"] = "windows://SRV/SHARE/X",
        ["//srv/share/x"] = "windows://SRV/SHARE/X",
        ["//SRV/share/x"] = "windows://SRV/SHARE/X",
        ["/home/x"] = "case-sensitive:/home/x",
        ["/home/X"] = "case-sensitive:/home/X",
        ["/Users/kingd"] = "case-sensitive:/Users/kingd",
    };

    private static readonly Dictionary<(string, string), bool> Containment = new()
    {
        [(@"C:\Users\kingd\repo", "C:/Users/kingd/repo/src/a.cs")] = true,
        [("C:/Users/kingd/repo", @"C:\Users\kingd\repo")] = true,
        [(@"C:\Users\kingd\repo", @"C:\Users\kingd\repoOther\a.cs")] = false,
        [("//srv/share/x", @"\\srv\share\x\y")] = true,
        [("/home/x", "/home/x/y")] = true,
        [("/home/x", "/home/xy")] = false,
    };

    /// <inheritdoc />
    protected override string NormalizeLexical(string path, WorkspacePathPlatform platform) =>
        Lexical.TryGetValue((path, platform), out var value)
            ? value
            : throw new InvalidOperationException($"No scripted lexical data for '{path}' ({platform}).");

    /// <inheritdoc />
    protected override string StorageKey(string path) =>
        Keys.TryGetValue(path, out var value)
            ? value
            : throw new InvalidOperationException($"No scripted storage key for '{path}'.");

    /// <inheritdoc />
    protected override bool AreEquivalent(string left, string right) =>
        string.Equals(StorageKey(left), StorageKey(right), StringComparison.Ordinal);

    /// <inheritdoc />
    protected override bool IsWithinRoot(string root, string candidate) =>
        Containment.TryGetValue((root, candidate), out var value)
            ? value
            : throw new InvalidOperationException($"No scripted containment data for '{root}' / '{candidate}'.");
}

/// <summary>BDP v4 step 3: lexical identity contract against <see cref="WorkspaceIdentityPath"/>.</summary>
public sealed class WorkspaceLexicalIdentityRealTests : WorkspaceLexicalIdentityContractTests
{
    /// <inheritdoc />
    protected override string NormalizeLexical(string path, WorkspacePathPlatform platform) =>
        WorkspaceIdentityPath.NormalizeLexicalPath(path, platform);

    /// <inheritdoc />
    protected override string StorageKey(string path) => WorkspaceIdentityPath.GetStorageKey(path);

    /// <inheritdoc />
    protected override bool AreEquivalent(string left, string right) =>
        WorkspaceIdentityPath.AreEquivalent(left, right);

    /// <inheritdoc />
    protected override bool IsWithinRoot(string root, string candidate) =>
        WorkspaceIdentityPath.IsWithinRoot(root, candidate);
}
