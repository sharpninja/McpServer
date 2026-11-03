using Xunit.Sdk;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 (second criterion: "Generated test artifacts are written only to test-owned disposable output
/// locations, not to repository fixture or documentation paths"), BDPv4 mock gate: a test's artifact folder is accepted
/// only when it is an absolute path with no '.' or '..' segment that lies outside the checkout (compared by path segment
/// with either separator, under the path's own syntax: a Windows-form path ignores case, drive letter included, and a
/// POSIX path compares ordinally); the checkout itself, any folder inside it, a case alias of either on Windows, a
/// relative path and a dot segment are refused. Fixed rows and an independent scripted reference check; wrong checks
/// (accepts anything, refuses anything, plain string prefix, ignores dot segments, accepts the checkout root, accepts a
/// relative folder, one separator only, ordinal only, ignores case everywhere) are rejected by the same assertions inside
/// this passing harness. Strings only: no folder is created or read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, generated artifacts in test-owned locations): added the output-folder contract, because two memory-bench stub cases wrote into the checkout's docs/benchmarks/results and rewrote two tracked files on every lane run (operator 2026-11-02: "Fix in scope"; triage-report-32e6bc07).</description></item>
/// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, appended "compared ... under the path's own syntax"): added four case rows and the wrong checks "ordinal only" (the earlier shared check) and "ignores case everywhere", because hostile validation finding R9-F3 showed a Windows case alias of the checkout was accepted; documented every helper (MEMORY-FACT-008; finding R9-F8) (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
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
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        }));

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
