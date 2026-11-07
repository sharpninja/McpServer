using Xunit.Sdk;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 (second criterion: "Generated test artifacts are written only to test-owned disposable output
/// locations, not to repository fixture or documentation paths"), BDPv4 mock gate: a test's artifact folder is accepted
/// only when it is an absolute path with no '.' or '..' segment that lies outside the checkout (its root, the POSIX
/// root, a drive letter or a UNC server and share written with either separator, is part of its identity; then path
/// segments split on either separator are compared, UNC and drive-letter paths ignoring case and POSIX paths
/// ordinally); the checkout itself, any folder inside it, a case or separator alias of either on Windows, a relative
/// path and a dot segment are refused, and within one path syntax a path under another root, server, share or drive
/// is another location. Across path syntaxes (a POSIX path against a Windows drive or UNC path) the folder is refused,
/// and so are a Windows device path ("\\?\", "\\.\", "\??\", either separator) and a Windows segment that ends in a
/// dot or a space or holds a colon, a UNC server and share name included, because each can name a location inside the
/// checkout. Fixed rows and an independent scripted reference check, plus the stated rule over the scripted path
/// identity as a second positive control; wrong checks (accepts anything, refuses anything, plain string prefix,
/// ignores dot segments, accepts the checkout root, accepts a relative folder, one separator only, ordinal only,
/// ignores case everywhere, ignores the root, UNC compared ordinally, accepts device paths, compares across path
/// syntaxes, ignores Windows segment aliases, Windows segment rules on POSIX paths, ignores UNC server and share
/// aliases) are rejected by the same assertions inside this passing harness. Strings only: no folder is created or
/// read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, generated artifacts in test-owned locations): added the output-folder contract, because two memory-bench stub cases wrote into the checkout's docs/benchmarks/results and rewrote two tracked files on every lane run (operator 2026-11-02: "Fix in scope"; triage-report-32e6bc07).</description></item>
/// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, appended "compared ... under the path's own syntax"): added four case rows and the wrong checks "ordinal only" (the earlier shared check) and "ignores case everywhere", because hostile validation finding R9-F3 showed a Windows case alias of the checkout was accepted; documented every helper (MEMORY-FACT-008; finding R9-F8) (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
/// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, appended "A path's root is part of its identity"): added rows for a forward-slash, mixed-separator and backslash UNC alias of the checkout (refused), a distinct UNC share, a single-rooted path under a UNC checkout and a UNC path under a POSIX checkout (accepted as other locations), and the wrong checks "ignores the root" (the round 10 shared check) and "UNC compared ordinally", because hostile validation round 10 finding R10-F2 showed forward-slash UNC aliases were compared ordinally and the root was flattened (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 (second criterion): documentation only, the summary now states the root rule and lists all eleven wrong checks (stale summary found before round 11).</description></item>
/// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, appended "That holds only within one path syntax"): the rows for a single-rooted path under a UNC checkout and for two leading separators under a POSIX checkout now expect a refusal; added rows for a POSIX folder under a drive checkout (refused), a UNC folder under a drive checkout (another location), device path, device namespace, forward-slash device, device UNC and NT object aliases (refused), a device path on another drive (refused), trailing-dot, trailing-space and stream-syntax aliases (refused) and a POSIX name ending in a dot (accepted); the stated rule as a second positive control; and the wrong checks "accepts device paths", "compares across path syntaxes", "ignores Windows segment aliases" (each the round 11 shared check's behavior) and "Windows segment rules on POSIX paths", because the hostile validation round 11 that stopped without a verdict found a device path alias of a folder inside the checkout accepted, and the sibling sweep found the cross-syntax and segment aliases (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// <item><term>2026-11-03</term><description>Correction of the entry above (holistic read before hostile validation round 12, finding B3): "don't gate on me between rounds" covers fixing findings after a DISAGREE verdict, and round 11 stopped with none; the change was kept by the operator's choice "Mark ready, then HV (Recommended)" (2026-11-03 13:11). That entry stays as written.</description></item>
/// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, appended "The UNC server and share names are held to the same segment rule"): added rows for a trailing-dot UNC server alias and a trailing-space or trailing-dot UNC share alias of the checkout, a share name holding a colon, and a trailing-dot server outside the checkout (each refused); the stated rule now applies the segment rule to the server and share names; added the wrong check "ignores UNC server and share aliases" (the slice X shared check's behavior); documented <see cref="StatedSyntaxRule_SatisfiesEveryRow"/> with its requirement and change log (finding C9), because the holistic read before hostile validation round 12 (finding D3) found a trailing-dot server alias of the checkout accepted (operator 2026-11-03: "Strengthen + fix (Recommended)").</description></item>
/// </list>
/// </remarks>
public sealed class TestOutputFolderContractTests
{
    /// <summary>A checkout root, a candidate artifact folder and whether the folder is acceptable.</summary>
    /// <param name="Name">Row name.</param>
    /// <param name="Checkout">The checkout root.</param>
    /// <param name="Folder">The candidate artifact folder.</param>
    /// <param name="Accepted">Whether the folder is a test-owned location outside the checkout.</param>
    internal sealed record OutputCase(string Name, string Checkout, string Folder, bool Accepted);

    /// <summary>The fixed rows.</summary>
    internal static readonly OutputCase[] Cases =
    [
        new("temporary folder", "/work/repo", "/tmp/memory-bench-stub-0123", true),
        new("sibling sharing the prefix", "/work/repo", "/work/repo-out/stub", true),
        new("checkout documentation folder", "/work/repo", "/work/repo/docs/benchmarks/results", false),
        new("checkout root itself", "/work/repo", "/work/repo", false),
        new("checkout root with trailing separator", "/work/repo", "/work/repo/", false),
        new("dot segment inside the checkout", "/work/repo", "/work/repo/./docs/out", false),
        new("parent segment", "/work/repo", "/work/repo/../tmp/out", false),
        new("parent segment escaping into the checkout", "/work/repo", "/work/other/../repo/docs", false),
        new("dot segment outside the checkout", "/work/repo", "/tmp/./out", false),
        new("relative folder", "/work/repo", "docs/benchmarks/results", false),
        new("Windows folder inside the checkout, other separator", "C:/GitHub/repo", @"C:\GitHub\repo\docs\out", false),
        new("Windows temporary folder", "C:/GitHub/repo", @"D:\Temp\memory-bench-stub-0123", true),
        new("Windows drive letter case alias inside the checkout", "C:/GitHub/repo", "c:/GitHub/repo/docs/out", false),
        new("Windows folder case alias inside the checkout", "C:/GitHub/repo", @"C:\github\REPO\docs\out", false),
        new("Windows checkout root in another case", "C:/GitHub/repo", "c:/github/repo", false),
        new("POSIX folder differing only in case is a distinct sibling", "/work/repo", "/work/Repo/out", true),
        new("Windows UNC case alias inside the checkout", "//Server/Share/Repo", "//server/share/repo/docs/out", false),
        new("Windows UNC mixed-separator alias inside the checkout", "//Server/Share/Repo", @"\\server\share/Repo\docs", false),
        new("Windows UNC checkout root in backslashes", "//Server/Share/Repo", @"\\Server\Share\Repo", false),
        new("a distinct UNC share is another location", "//Server/Share/Repo", "//Server/Other/Repo/out", true),
        new("a single-rooted path under a UNC checkout is another path syntax", "//server/share/repo", "/server/share/repo/docs/out", false),
        new("two leading separators under a POSIX checkout are another path syntax", "/server/share/repo", "//server/share/repo/docs/out", false),
        new("a POSIX folder under a drive-letter checkout is another path syntax", "C:/GitHub/repo", "/GitHub/repo/docs/out", false),
        new("a UNC folder under a drive-letter checkout is another location", "C:/GitHub/repo", "//Server/Share/out", true),
        new("a device path alias inside the checkout", "C:/GitHub/repo", @"\\?\C:\GitHub\repo\docs\out", false),
        new("a device namespace alias inside the checkout", "C:/GitHub/repo", @"\\.\C:\GitHub\repo\docs\out", false),
        new("a forward-slash device alias inside the checkout", "C:/GitHub/repo", "//?/C:/GitHub/repo/docs/out", false),
        new("a device UNC alias inside a UNC checkout", "//Server/Share/Repo", @"\\?\UNC\Server\Share\Repo\docs", false),
        new("an NT object path inside the checkout", "C:/GitHub/repo", @"\??\C:\GitHub\repo\docs", false),
        new("a device path on another drive is still refused", "C:/GitHub/repo", @"\\?\D:\Temp\out", false),
        new("a trailing-dot alias of a checkout segment", "C:/GitHub/repo", @"C:\GitHub\repo.\docs\out", false),
        new("a trailing-space alias of a checkout segment", "C:/GitHub/repo", @"C:\GitHub\repo \docs\out", false),
        new("a stream-syntax alias of the checkout", "C:/GitHub/repo", @"C:\GitHub\repo::$INDEX_ALLOCATION\docs", false),
        new("a trailing-dot alias of the UNC server", "//Server/Share/Repo", "//Server./Share/Repo/docs", false),
        new("a trailing-space alias of the UNC share", "//Server/Share/Repo", @"\\Server\Share \Repo\docs", false),
        new("a trailing-dot alias of the UNC share", "//Server/Share/Repo", "//Server/Share./Repo/docs", false),
        new("a UNC share name holding a colon", "//Server/Share/Repo", "//Server/Share:x/Repo/docs", false),
        new("a trailing-dot UNC server outside the checkout is still refused", "C:/GitHub/repo", "//Other./Share/out", false),
        new("a POSIX segment ending in a dot is an ordinary name", "/work/repo", "/tmp/out./stub", true),
    ];

    /// <summary>The scripted reference check satisfies every row.</summary>
    [Fact]
    public void ScriptedCheck_SatisfiesEveryRow() => AssertCheck(ScriptedCheck);

    /// <summary>Each wrong check fails the same assertions.</summary>
    /// <param name="fault">The deliberate fault.</param>
    [Theory]
    [InlineData("accepts anything")]
    [InlineData("refuses anything")]
    [InlineData("string prefix")]
    [InlineData("ignores dot segments")]
    [InlineData("accepts the checkout root")]
    [InlineData("accepts a relative folder")]
    [InlineData("one separator only")]
    [InlineData("ordinal only")]
    [InlineData("ignores case everywhere")]
    [InlineData("ignores the root")]
    [InlineData("UNC compared ordinally")]
    [InlineData("accepts device paths")]
    [InlineData("compares across path syntaxes")]
    [InlineData("ignores Windows segment aliases")]
    [InlineData("Windows segment rules on POSIX paths")]
    [InlineData("ignores UNC server and share aliases")]
    public void WrongChecks_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertCheck((folder, checkout) => fault switch
        {
            "accepts anything" => null,
            "refuses anything" => Refusal(folder, checkout),
            "string prefix" => folder.Replace('\\', '/').StartsWith(checkout.Replace('\\', '/'), StringComparison.Ordinal) ? Refusal(folder, checkout) : null,
            "ignores dot segments" => IsAbsolute(folder) && IsInsideOrEqual(Segments(folder), Segments(checkout)) ? Refusal(folder, checkout) : IsAbsolute(folder) ? null : Refusal(folder, checkout),
            "accepts the checkout root" => Segments(folder).Length == Segments(checkout).Length ? null : ScriptedCheck(folder, checkout),
            "accepts a relative folder" => IsAbsolute(folder) ? ScriptedCheck(folder, checkout) : null,
            "one separator only" => !folder.Contains('/', StringComparison.Ordinal) && IsAbsolute(folder) ? null : ScriptedCheck(folder, checkout),
            "ordinal only" => SegmentCheck(folder, checkout, StringComparer.Ordinal),
            "ignores case everywhere" => SegmentCheck(folder, checkout, StringComparer.OrdinalIgnoreCase),
            "ignores the root" => RootRuleCheck(folder, checkout, flattenRoot: true, ordinalUnc: false),
            "UNC compared ordinally" => RootRuleCheck(folder, checkout, flattenRoot: false, ordinalUnc: true),
            "accepts device paths" => SyntaxRuleCheck(folder, checkout, SyntaxFlaw.AcceptsDevicePaths),
            "compares across path syntaxes" => SyntaxRuleCheck(folder, checkout, SyntaxFlaw.AcrossSyntaxes),
            "ignores Windows segment aliases" => SyntaxRuleCheck(folder, checkout, SyntaxFlaw.IgnoresWindowsAliases),
            "Windows segment rules on POSIX paths" => SyntaxRuleCheck(folder, checkout, SyntaxFlaw.WindowsAliasesEverywhere),
            "ignores UNC server and share aliases" => SyntaxRuleCheck(folder, checkout, SyntaxFlaw.IgnoresUncRootAliases),
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        }));

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion, appended "That holds only within one path syntax" and "The
    /// UNC server and share names are held to the same segment rule"): positive control for the rule stated in the
    /// criterion: the syntax rule with no flaw, built on the scripted path identity, gives every row its stated verdict,
    /// so the rows and the rule agree.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 generated-artifact criterion (appended "That holds only within one path syntax"): added as a second positive control for the findings of the hostile validation round 11 that stopped without a verdict.</description></item>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 generated-artifact criterion (appended "The UNC server and share names are held to the same segment rule"): the rule it runs now covers the server and share names; requirement and change log added, because the holistic read before hostile validation round 12 found it had neither (findings D3 and C9; operator 2026-11-03: "Strengthen + fix (Recommended)").</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public void StatedSyntaxRule_SatisfiesEveryRow() => AssertCheck((folder, checkout) => SyntaxRuleCheck(folder, checkout, SyntaxFlaw.None));

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion, appended "That holds only within one path syntax"): the
    /// one flaw a <see cref="SyntaxRuleCheck"/> variant carries.
    /// </summary>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: added for the findings of the hostile validation round 11 that stopped without a verdict.</remarks>
    internal enum SyntaxFlaw
    {
        /// <summary>No flaw: the rule as the criterion states it.</summary>
        None,

        /// <summary>Accepts a Windows device path (the round 11 shared check read "\\?\C:\" as a UNC share named "?/C:").</summary>
        AcceptsDevicePaths,

        /// <summary>Accepts a folder in the other path syntax as another location (the round 11 shared check).</summary>
        AcrossSyntaxes,

        /// <summary>Accepts a Windows segment that ends in a dot or a space or holds a colon (the round 11 shared check).</summary>
        IgnoresWindowsAliases,

        /// <summary>Applies the Windows segment rule to POSIX paths too, which refuses ordinary POSIX names.</summary>
        WindowsAliasesEverywhere,

        /// <summary>
        /// Applies the Windows segment rule to the segments only, not to a UNC server or share name (the slice X shared
        /// check; 2026-11-03, holistic read before hostile validation round 12, finding D3).
        /// </summary>
        IgnoresUncRootAliases,
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion, appended "That holds only within one path syntax"): the
    /// output rule over the scripted path identity, with at most one flaw. A folder is refused when it is not absolute,
    /// has a '.' or '..' segment, is a Windows device path (two separators then '?' or '.' then a separator, or a
    /// separator, "??" and a separator), is in the other path syntax than the checkout (POSIX against Windows, where a
    /// drive letter and a UNC share are both Windows), has a Windows segment that ends in a dot or a space or holds a
    /// colon (a UNC server and share name included), or is the checkout or lies inside it.
    /// </summary>
    /// <param name="folder">The candidate artifact folder.</param>
    /// <param name="checkout">The checkout root.</param>
    /// <param name="flaw">The one flaw, or <see cref="SyntaxFlaw.None"/>.</param>
    /// <returns>Null when the folder is acceptable under the rule; otherwise the refusal.</returns>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 generated-artifact criterion: added for the findings of the hostile validation round 11 that stopped without a verdict (a device path alias, a folder in the other path syntax and a Windows segment alias of a folder inside the checkout were accepted).</description></item>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 generated-artifact criterion (appended "The UNC server and share names are held to the same segment rule"): the segment rule also covers a UNC server and share name, and the flaw <see cref="SyntaxFlaw.IgnoresUncRootAliases"/> leaves them out, because the holistic read before hostile validation round 12 (finding D3) found a trailing-dot server alias of the checkout accepted (operator 2026-11-03: "Strengthen + fix (Recommended)").</description></item>
    /// </list>
    /// </remarks>
    private static string? SyntaxRuleCheck(string folder, string checkout, SyntaxFlaw flaw)
    {
        var path = ScriptedPathIdentity.Parse(folder);
        var root = ScriptedPathIdentity.Parse(checkout);
        var windowsFolder = path.Kind is ScriptedPathIdentity.RootKind.Drive or ScriptedPathIdentity.RootKind.Unc;
        var windowsCheckout = root.Kind is ScriptedPathIdentity.RootKind.Drive or ScriptedPathIdentity.RootKind.Unc;
        var aliasRuleApplies = flaw != SyntaxFlaw.IgnoresWindowsAliases && (windowsFolder || flaw == SyntaxFlaw.WindowsAliasesEverywhere);
        string[] aliasNames = path.Kind == ScriptedPathIdentity.RootKind.Unc && flaw != SyntaxFlaw.IgnoresUncRootAliases
            ? [.. path.Root.Split('/'), .. path.Segments]
            : path.Segments;
        var refused = path.Kind == ScriptedPathIdentity.RootKind.None
            || path.Segments.Any(segment => segment is "." or "..")
            || (flaw != SyntaxFlaw.AcceptsDevicePaths && IsDevicePath(folder))
            || (flaw != SyntaxFlaw.AcrossSyntaxes && windowsFolder != windowsCheckout)
            || (aliasRuleApplies && aliasNames.Any(IsWindowsSegmentAlias))
            || ScriptedPathIdentity.Inside(folder, checkout)
            || ScriptedPathIdentity.Same(folder, checkout);
        return refused ? Refusal(folder, checkout) : null;
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion): whether a path is a Windows device path: two separators of
    /// either kind, then '?' or '.', then a separator ("\\?\", "\\.\", "//?/"), or a separator, "??" and a separator
    /// ("\??\").
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>True for a device path.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: added for the findings of the hostile validation round 11 that stopped without a verdict.</remarks>
    private static bool IsDevicePath(string path)
    {
        static bool Separator(char value) => value is '/' or '\\';
        return path.Length >= 4
            && ((Separator(path[0]) && Separator(path[1]) && path[2] is '?' or '.' && Separator(path[3]))
                || (Separator(path[0]) && path[1] == '?' && path[2] == '?' && Separator(path[3])));
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion): whether a Windows path segment is one Windows resolves to
    /// another name (it ends in a dot or a space) or reads as a stream (it holds a colon).
    /// </summary>
    /// <param name="segment">The segment.</param>
    /// <returns>True for such a segment.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: added for the findings of the hostile validation round 11 that stopped without a verdict.</remarks>
    private static bool IsWindowsSegmentAlias(string segment) =>
        segment.EndsWith('.') || segment.EndsWith(' ') || segment.Contains(':', StringComparison.Ordinal);

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion), for the wrong checks "ignores the root" (the round 10 shared
    /// check, which flattened the root into the segments) and "UNC compared ordinally": an output check over the scripted
    /// path identity with one flaw.
    /// </summary>
    /// <param name="folder">The candidate artifact folder.</param>
    /// <param name="checkout">The checkout root.</param>
    /// <param name="flattenRoot">Ignore root kind and identity.</param>
    /// <param name="ordinalUnc">Compare UNC paths ordinally.</param>
    /// <returns>Null when the folder is acceptable under the flawed rule; otherwise the refusal.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 generated-artifact criterion (appended "A path's root is part of its identity"): added for hostile validation round 10 finding R10-F2.</remarks>
    private static string? RootRuleCheck(string folder, string checkout, bool flattenRoot, bool ordinalUnc) =>
        ScriptedPathIdentity.IsAbsolute(folder)
        && !ScriptedPathIdentity.Parse(folder).Segments.Any(segment => segment is "." or "..")
        && !ScriptedPathIdentity.Inside(folder, checkout, flattenRoot, ordinalUnc)
        && !ScriptedPathIdentity.Same(folder, checkout, flattenRoot, ordinalUnc)
            ? null
            : Refusal(folder, checkout);

    /// <summary>Asserts a check against every row: null for an acceptable folder, a message naming the folder otherwise.</summary>
    /// <param name="check">The check under test: folder and checkout root in, null or a refusal out.</param>
    internal static void AssertCheck(Func<string, string, string?> check)
    {
        foreach (var row in Cases)
        {
            var verdict = check(row.Folder, row.Checkout);
            if (row.Accepted)
                Assert.True(verdict is null, $"{row.Name}: refused '{verdict}'");
            else
                Assert.True(verdict is not null && verdict.Contains(row.Folder, StringComparison.Ordinal), $"{row.Name}: '{verdict}'");
        }
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion) scripted reference: the verdict each row states, looked up
    /// from the rows, so the reference carries no path logic of its own.
    /// </summary>
    /// <param name="folder">The candidate artifact folder.</param>
    /// <param name="checkout">The checkout root.</param>
    /// <returns>Null for an acceptable folder; otherwise the refusal naming the folder.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static string? ScriptedCheck(string folder, string checkout) =>
        Cases.Single(row => row.Folder == folder && row.Checkout == checkout).Accepted ? null : Refusal(folder, checkout);

    /// <summary>FR-MCP-TESTRESOURCE-001 (generated-artifact criterion): the refusal text, which names the folder.</summary>
    /// <param name="folder">The refused folder.</param>
    /// <param name="checkout">The checkout root.</param>
    /// <returns>The refusal.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static string Refusal(string folder, string checkout) =>
        $"The test artifact folder '{folder}' is not a test-owned location outside the checkout '{checkout}'.";

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion), for the wrong checks: whether a path's segments start with
    /// the root's segments, compared ordinally.
    /// </summary>
    /// <param name="path">The path's segments.</param>
    /// <param name="root">The root's segments.</param>
    /// <returns>True when the path is the root or lies inside it.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static bool IsInsideOrEqual(string[] path, string[] root) =>
        path.Length >= root.Length && root.Select((segment, index) => segment == path[index]).All(same => same);

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion), for the wrong checks: whether a path is absolute (a POSIX
    /// root or a drive letter with a separator).
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>True for an absolute path.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static bool IsAbsolute(string path) =>
        path.StartsWith('/') || (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] is '/' or '\\');

    /// <summary>FR-MCP-TESTRESOURCE-001 (generated-artifact criterion), for the wrong checks: a path's segments, either separator.</summary>
    /// <param name="path">The path.</param>
    /// <returns>The non-empty segments.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static string[] Segments(string path) => path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion), for the wrong checks "ordinal only" and "ignores case
    /// everywhere": a segment check that uses one comparer for every path, whatever its syntax.
    /// </summary>
    /// <param name="folder">The candidate artifact folder.</param>
    /// <param name="checkout">The checkout root.</param>
    /// <param name="comparer">The one comparer used for every segment.</param>
    /// <returns>Null when the folder is absolute, has no dot segment and is outside the checkout under that comparer.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: added for hostile validation finding R9-F3 (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</remarks>
    private static string? SegmentCheck(string folder, string checkout, StringComparer comparer)
    {
        var path = Segments(folder);
        var root = Segments(checkout);
        var inside = path.Length >= root.Length && root.Select((segment, index) => comparer.Equals(segment, path[index])).All(same => same);
        return IsAbsolute(folder) && !path.Any(segment => segment is "." or "..") && !inside ? null : Refusal(folder, checkout);
    }
}
