using QBrainAi.TestSupport.TemporaryInstance;
using Xunit;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// Tests the check an in-process temporary instance makes of its real database selection before it starts: the
/// connection string its database context holds names a Data Source (keyword matched ignoring case, the last one
/// winning as in a connection string builder) that lies strictly inside the instance's owned root, compared segment by
/// segment with no '.' or '..' segment; a server connection string, a missing one, or any other selection is refused.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-NUKE-019 AC-6 as replaced on 2026-10-29 (the temporary instance's database is the one
/// inside its owned root, and a selection outside the owned root is refused before the instance starts; operator
/// 2026-10-29: "Strengthen ACs, fix both (Recommended)" and "Run the 2 classes, AC first (Recommended)"; hostile
/// validation round 8b findings R8B-F2 and R8B-F3, sibling sweep of plan section 26.5: the in-process factory that hosts
/// QuadBrainOllamaEndpointIntegrationTests). Test data: literal connection strings with both separator styles, run
/// against checks passed in, so the contract is proved on a scripted reference with negative controls before the
/// shared check exists; no test reads or writes the process environment, the file system or the network.
/// </remarks>
public sealed class OwnedDatabaseSelectionTests
{
    private const string OwnedRoot = "/scratch/owned";

    /// <summary>
    /// The contract rows: a row name, the connection string the database context holds, the owned root, and whether
    /// the check must accept the selection.
    /// </summary>
    internal static readonly (string Name, string? ConnectionString, string Root, bool Accepted)[] Rows =
    [
        ("the owned database", "Data Source=/scratch/owned/data/mcp.db", OwnedRoot, true),
        ("the owned database with Windows separators", @"Data Source=C:\scratch\owned\data\mcp.db", @"C:\scratch\owned", true),
        ("other keywords beside the owned database", "Cache=Shared;Data Source=/scratch/owned/data/mcp.db;Pooling=False", OwnedRoot, true),
        ("a keyword in another case", "data source=/scratch/owned/data/mcp.db", OwnedRoot, true),
        ("a database outside the owned root", "Data Source=/home/example/live/qbrainai.db", OwnedRoot, false),
        ("a sibling root sharing the prefix", "Data Source=/scratch/owned2/data/mcp.db", OwnedRoot, false),
        ("a parent-segment escape", "Data Source=/scratch/owned/../live/mcp.db", OwnedRoot, false),
        ("the owned root itself", "Data Source=/scratch/owned", OwnedRoot, false),
        ("a repeated Data Source keeps the last", "Data Source=/scratch/owned/data/mcp.db;Data Source=/home/example/live/qbrainai.db", OwnedRoot, false),
        ("a server connection string", "Host=live;Database=qbrainai", OwnedRoot, false),
        ("no connection string", null, OwnedRoot, false),
    ];

    /// <summary>Returns the names of the rows on which <paramref name="check"/> gives the wrong verdict.</summary>
    /// <param name="check">The check under test: the connection string and the owned root to a violation or null.</param>
    internal static List<string> ContractFailures(Func<string?, string, string?> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return Rows
            .Where(row => (check(row.ConnectionString, row.Root) is null) != row.Accepted)
            .Select(row => row.Name)
            .ToList();
    }

    /// <summary>Mock gate positive control: the scripted reference check satisfies every row.</summary>
    [Fact]
    public void ScriptedDatabaseSelectionCheck_SatisfiesTheContract() =>
        Assert.Empty(ContractFailures(ScriptedCheck));

    /// <summary>
    /// Mock gate negative controls, each rejected inside a passing harness on the row named: a check that accepts
    /// anything, one that lets the first Data Source win, one that compares a string prefix without a segment boundary,
    /// one that matches the keyword case-sensitively, and one that accepts a missing connection string.
    /// </summary>
    /// <param name="wrong">The name of the wrong check.</param>
    /// <param name="failingRow">The row that must reject it.</param>
    [Theory]
    [InlineData("accepts anything", "a database outside the owned root")]
    [InlineData("first Data Source wins", "a repeated Data Source keeps the last")]
    [InlineData("string prefix", "a sibling root sharing the prefix")]
    [InlineData("case-sensitive keyword", "a keyword in another case")]
    [InlineData("accepts a missing connection string", "no connection string")]
    public void WrongDatabaseSelectionChecks_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, ContractFailures(WrongCheck(wrong)));

    /// <summary>
    /// Binding: the shared check, <see cref="TemporaryInstanceChecks.CheckDatabaseSelection"/> (TR-MCP-NUKE-019 AC-6),
    /// satisfies the same rows the scripted reference passed and the wrong checks failed.
    /// </summary>
    [Fact]
    public void SharedDatabaseSelectionCheck_SatisfiesTheContract() =>
        Assert.Empty(ContractFailures(TemporaryInstanceChecks.CheckDatabaseSelection));

    private static string? ScriptedCheck(string? connectionString, string root) =>
        CheckWith(connectionString, root, lastWins: true, keyword: StringComparison.OrdinalIgnoreCase, segments: true, acceptMissing: false);

    private static Func<string?, string, string?> WrongCheck(string wrong) => wrong switch
    {
        "accepts anything" => (_, _) => null,
        "first Data Source wins" => (connectionString, root) =>
            CheckWith(connectionString, root, lastWins: false, keyword: StringComparison.OrdinalIgnoreCase, segments: true, acceptMissing: false),
        "string prefix" => (connectionString, root) =>
            CheckWith(connectionString, root, lastWins: true, keyword: StringComparison.OrdinalIgnoreCase, segments: false, acceptMissing: false),
        "case-sensitive keyword" => (connectionString, root) =>
            CheckWith(connectionString, root, lastWins: true, keyword: StringComparison.Ordinal, segments: true, acceptMissing: false),
        "accepts a missing connection string" => (connectionString, root) =>
            CheckWith(connectionString, root, lastWins: true, keyword: StringComparison.OrdinalIgnoreCase, segments: true, acceptMissing: true),
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong check."),
    };

    private static string? CheckWith(string? connectionString, string root, bool lastWins, StringComparison keyword, bool segments, bool acceptMissing)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return acceptMissing ? null : "The temporary instance's database context holds no connection string.";

        string? database = null;
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0 && string.Equals(part[..separator].Trim(), "Data Source", keyword) && (lastWins || database is null))
                database = part[(separator + 1)..].Trim();
        }

        var inside = database is not null
            && (segments
                ? SegmentInside(database, root)
                : database.StartsWith(root, StringComparison.Ordinal) && !database.Contains("..", StringComparison.Ordinal) && database.Length > root.Length);
        return inside ? null : $"The temporary instance's database '{database ?? connectionString}' is not inside its owned root '{root}'.";
    }

    private static bool SegmentInside(string path, string root)
    {
        var pathSegments = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        var rootSegments = root.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        return !pathSegments.Any(segment => segment is "." or "..")
            && pathSegments.Length > rootSegments.Length
            && rootSegments.SequenceEqual(pathSegments.Take(rootSegments.Length), StringComparer.Ordinal);
    }
}
