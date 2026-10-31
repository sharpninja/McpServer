using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Tests how marker agent-plugin version resolution reads one candidate root's version files (<c>.version</c>,
/// then the plugin manifests and <c>package.json</c>, in that order): a file that exists but cannot be read (an IO
/// or access error) yields no version, so the read goes on to the next file, and any other error propagates.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-WS-005 / FR-MCP-018 ac-1 (operator 2026-10-20: "Fold R3-P3 in first", whose
/// option text adds "a version file that cannot be read never fails the marker" to the criterion with a scripted
/// test and negative controls, a mock gate, then the fix in MarkerFileService). Test data: scripted version files
/// (<see cref="ScriptedVersionFiles"/>) under a relative root, whose reads return fixed text, report the file as
/// absent, or throw the exceptions a locked, denied or vanished file raises; no file system is read. Expected
/// versions are derived from the criterion and from the reading order the resolution already had: the first file
/// with a non-empty version wins, an unreadable file counts as one without a version, and malformed JSON still
/// counts as no version. Negative controls run deliberately wrong reads (the previous read, which let read errors
/// propagate; one that stops at the first unreadable file; one that hides every error) over the same files and
/// prove the version assertion rejects each.
/// </remarks>
public sealed class PluginVersionFileReadTests
{
    private const string Root = "plugin-root";

    private const string LockedMessage =
        "The process cannot access the file because it is being used by another process.";

    /// <summary>A version read under test: reads the version files of <c>root</c> through <c>readText</c>.</summary>
    private delegate string? ReadFunction(string root, Func<string, string?> readText);

    /// <summary>The version files a root may hold, in the order the resolution reads them.</summary>
    private static readonly string[] VersionFileOrder =
    [
        ".version",
        "plugin.json",
        Path.Combine(".codex-plugin", "plugin.json"),
        Path.Combine(".claude-plugin", "plugin.json"),
        Path.Combine(".grok-plugin", "plugin.json"),
        "package.json",
    ];

    private static string FileAt(string relative) => Path.Combine(Root, relative);

    /// <summary>
    /// Scripted cases: the version files and the version the criterion requires. Each case builds fresh files so
    /// the read record starts empty.
    /// </summary>
    private static readonly Dictionary<string, (Func<ScriptedVersionFiles> Files, string? Version)> Cases =
        new(StringComparer.Ordinal)
        {
            ["version-file"] = (
                () => new ScriptedVersionFiles()
                    .Holds(FileAt(".version"), "3.1.0\n"),
                "3.1.0"),
            ["version-file-locked-manifest-next"] = (
                () => new ScriptedVersionFiles()
                    .Throws(FileAt(".version"), () => new IOException(LockedMessage))
                    .Holds(FileAt("plugin.json"), """{"version":"3.2.0"}"""),
                "3.2.0"),
            ["version-file-denied-nothing-else"] = (
                () => new ScriptedVersionFiles()
                    .Throws(FileAt(".version"), () => new UnauthorizedAccessException("Access to the path is denied.")),
                null),
            ["manifest-locked-next-manifest"] = (
                () => new ScriptedVersionFiles()
                    .Throws(FileAt("plugin.json"), () => new IOException(LockedMessage))
                    .Holds(FileAt(Path.Combine(".claude-plugin", "plugin.json")), """{"version":"3.3.0"}"""),
                "3.3.0"),
            ["every-file-unreadable"] = (
                () => new ScriptedVersionFiles()
                    .Throws(FileAt(".version"), () => new IOException(LockedMessage))
                    .Throws(FileAt("plugin.json"), () => new UnauthorizedAccessException("Access to the path is denied."))
                    .Throws(FileAt("package.json"), () => new IOException(LockedMessage)),
                null),
            ["version-file-vanished"] = (
                () => new ScriptedVersionFiles()
                    .Throws(FileAt(".version"), () => new FileNotFoundException("Could not find file.", FileAt(".version")))
                    .Holds(FileAt("package.json"), """{"version":"3.4.0"}"""),
                "3.4.0"),
            ["empty-and-malformed-fall-through"] = (
                () => new ScriptedVersionFiles()
                    .Holds(FileAt(".version"), "   ")
                    .Holds(FileAt("plugin.json"), "not json")
                    .Holds(FileAt("package.json"), """{"version":"3.5.0"}"""),
                "3.5.0"),
            ["no-version-files"] = (
                () => new ScriptedVersionFiles(),
                null),
        };

    /// <summary>The scripted case names, for the theories.</summary>
    public static TheoryData<string> CaseNames => new(Cases.Keys.Order(StringComparer.Ordinal));

    /// <summary>Asserts one read result against the version a case requires.</summary>
    private static void AssertVersion(string? expected, string? actual) => Assert.Equal(expected, actual);

    /// <summary>Runs one read over a fresh copy of a case's files and asserts its result.</summary>
    private static void RunAndAssert(string name, ReadFunction read)
    {
        var (filesFactory, version) = Cases[name];
        var files = filesFactory();
        AssertVersion(version, read(Root, files.Read));
    }

    /// <summary>Mock gate positive control: each case's required version passes the version assertion.</summary>
    /// <param name="name">Scripted case name.</param>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ScriptedCase_RequiredVersion_PassesTheVersionAssertion(string name) =>
        AssertVersion(Cases[name].Version, Cases[name].Version);

    /// <summary>The theories cover every scripted case.</summary>
    [Fact]
    public void ScriptedTheory_CoversEveryCase() => Assert.Equal(8, Cases.Count);

    /// <summary>
    /// The scripted files are themselves correct: a read records the path, returns the scripted text, throws the
    /// scripted exception, and reports an unscripted file as absent (null), the way a file that does not exist is
    /// reported.
    /// </summary>
    [Fact]
    public void ScriptedFiles_RecordReadsAndReportUnscriptedFilesAbsent()
    {
        var files = new ScriptedVersionFiles()
            .Holds(FileAt(".version"), "1.0.0")
            .Throws(FileAt("plugin.json"), () => new IOException(LockedMessage));

        Assert.Equal("1.0.0", files.Read(FileAt(".version")));
        Assert.Equal(LockedMessage, Assert.Throws<IOException>(() => files.Read(FileAt("plugin.json"))).Message);
        Assert.Null(files.Read(FileAt("package.json")));
        Assert.Equal(new[] { FileAt(".version"), FileAt("plugin.json"), FileAt("package.json") }, files.Reads);
    }

    /// <summary>
    /// Mock gate negative controls: each wrong read is rejected by the version assertion on the case that exposes
    /// it, inside a passing harness. The previous read (read errors propagate) fails on every case with an
    /// unreadable file; a read that stops at the first unreadable file loses the readable file after it.
    /// </summary>
    [Fact]
    public void WrongReadNegativeControls_AreRejectedInsidePassingHarness()
    {
        foreach (var name in new[] { "version-file-locked-manifest-next", "version-file-denied-nothing-else", "manifest-locked-next-manifest", "every-file-unreadable", "version-file-vanished" })
            Assert.NotNull(Record.Exception(() => RunAndAssert(name, PreviousRead)));

        foreach (var name in new[] { "version-file-locked-manifest-next", "manifest-locked-next-manifest", "version-file-vanished" })
            Assert.NotNull(Record.Exception(() => RunAndAssert(name, StopsAtFirstUnreadableRead)));

        // Each wrong read is wrong only where a case exposes it: on the cases without an unreadable file, every one
        // of them yields the required version, so the rejections above come from the behavior under test and not
        // from an assertion that rejects everything.
        foreach (var read in new ReadFunction[] { PreviousRead, StopsAtFirstUnreadableRead, HidesEveryErrorRead })
        {
            foreach (var name in new[] { "version-file", "empty-and-malformed-fall-through", "no-version-files" })
                Assert.Null(Record.Exception(() => RunAndAssert(name, read)));
        }
    }

    /// <summary>
    /// Mock gate negative control for the error filter: a scripted read error that is neither an IO nor an access
    /// error escapes the scripted files, and a read that hides every error swallows it; the production binding
    /// requires the read to let it propagate.
    /// </summary>
    [Fact]
    public void UnexpectedReadError_EscapesTheScriptedFiles()
    {
        var files = new ScriptedVersionFiles()
            .Throws(FileAt(".version"), () => new InvalidOperationException("not an IO error"))
            .Holds(FileAt("package.json"), """{"version":"3.6.0"}""");

        Assert.Throws<InvalidOperationException>(() => PreviousRead(Root, files.Read));
        Assert.Equal("3.6.0", HidesEveryErrorRead(Root, files.Read));
    }

    /// <summary>
    /// Production binding of the scripted cases: <see cref="MarkerFileService.TryReadAgentPluginVersion"/> over each
    /// case's files yields the required version through the same assertion.
    /// </summary>
    /// <param name="name">Scripted case name.</param>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ScriptedCase_ProductionRead_YieldsTheRequiredVersion(string name) =>
        RunAndAssert(name, MarkerFileService.TryReadAgentPluginVersion);

    /// <summary>
    /// Production binding of the error filter: a read error that is neither an IO nor an access error propagates out
    /// of <see cref="MarkerFileService.TryReadAgentPluginVersion"/> instead of being treated as a missing version.
    /// </summary>
    [Fact]
    public void ProductionRead_UnexpectedReadError_Propagates()
    {
        var files = new ScriptedVersionFiles()
            .Throws(FileAt(".version"), () => new InvalidOperationException("not an IO error"))
            .Holds(FileAt("package.json"), """{"version":"3.6.0"}""");

        Assert.Throws<InvalidOperationException>(() => MarkerFileService.TryReadAgentPluginVersion(Root, files.Read));
    }

    // Wrong reads used only as negative controls.

    private static string? PreviousRead(string root, Func<string, string?> readText) =>
        ConfigurableRead(root, readText, tolerateReadErrors: false, stopAtFirstUnreadable: false, hideEveryError: false);

    private static string? StopsAtFirstUnreadableRead(string root, Func<string, string?> readText) =>
        ConfigurableRead(root, readText, tolerateReadErrors: true, stopAtFirstUnreadable: true, hideEveryError: false);

    private static string? HidesEveryErrorRead(string root, Func<string, string?> readText) =>
        ConfigurableRead(root, readText, tolerateReadErrors: true, stopAtFirstUnreadable: false, hideEveryError: true);

    private static string? ConfigurableRead(
        string root,
        Func<string, string?> readText,
        bool tolerateReadErrors,
        bool stopAtFirstUnreadable,
        bool hideEveryError)
    {
        foreach (var relative in VersionFileOrder)
        {
            string? text;
            try
            {
                text = readText(Path.Combine(root, relative));
            }
            catch (Exception ex) when (hideEveryError || (tolerateReadErrors && ex is IOException or UnauthorizedAccessException))
            {
                if (stopAtFirstUnreadable)
                    return null;
                continue;
            }

            if (text is null)
                continue;
            var version = relative == ".version" ? text.Trim() : VersionFromJson(text);
            if (!string.IsNullOrWhiteSpace(version))
                return version;
        }

        return null;
    }

    private static string? VersionFromJson(string text)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(text);
            return document.RootElement.TryGetProperty("version", out var version)
                && version.ValueKind == System.Text.Json.JsonValueKind.String
                ? version.GetString()
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Scripted version files: each path returns fixed text or throws a fixed exception, every read is recorded in
    /// order, and an unscripted path reads as absent (null).
    /// </summary>
    private sealed class ScriptedVersionFiles
    {
        private readonly Dictionary<string, Func<string>> _files = new(StringComparer.Ordinal);

        /// <summary>Paths read so far, in order.</summary>
        public List<string> Reads { get; } = [];

        /// <summary>Scripts <paramref name="path"/> to hold <paramref name="text"/>.</summary>
        public ScriptedVersionFiles Holds(string path, string text)
        {
            _files[path] = () => text;
            return this;
        }

        /// <summary>Scripts <paramref name="path"/> to throw the exception <paramref name="error"/> creates.</summary>
        public ScriptedVersionFiles Throws(string path, Func<Exception> error)
        {
            _files[path] = () => throw error();
            return this;
        }

        /// <summary>Reads one file as scripted; null when the path is not scripted (the file does not exist).</summary>
        public string? Read(string path)
        {
            Reads.Add(path);
            return _files.TryGetValue(path, out var read) ? read() : null;
        }
    }
}
