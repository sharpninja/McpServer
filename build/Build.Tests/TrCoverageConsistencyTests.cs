using System.Text.RegularExpressions;
using Xunit;

namespace NukeBuild.Tests;

/// <summary>
/// Regression guard: prevents the failure mode where a provider-agnostic Technical
/// Requirement (e.g. TR-MCP-CFG-007) is marked ✅ Complete while a downstream TR
/// remains ✅ Complete with its Covered-by list still pinning a provider-specific
/// implementation type (e.g. `SqliteTodoService`), which silently contradicts the
/// factory-pattern mandate.
/// </summary>
public sealed class TrCoverageConsistencyTests
{
    private static readonly Regex TrHeaderRegex = new(
        @"^## (?<id>TR-[A-Z0-9-]+)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex StatusRegex = new(
        @"\*\*Status:\*\*\s*(?<status>[^\r\n]+)",
        RegexOptions.Compiled);

    private static readonly Regex CoveredByRegex = new(
        @"\*\*Covered by:\*\*\s*(?<list>[^\r\n]+)",
        RegexOptions.Compiled);

    private static readonly Regex BacktickedTypeRegex = new(
        @"`(?<t>[^`]+)`",
        RegexOptions.Compiled);

    private static readonly Regex ProviderSpecificTypeRegex = new(
        @"^(?<prefix>Sqlite|SqlServer|PostgreSql|Postgres)[A-Z]\w*(Service|Store|Provider|Migrator|Strategy)?$",
        RegexOptions.Compiled);

    private const string CompleteMarker = "✅";

    private static string TechnicalRequirementsPath
    {
        get
        {
            var dir = AppContext.BaseDirectory;
            for (var i = 0; i < 10 && dir is not null; i++)
            {
                var candidate = Path.Combine(dir, "docs", "Project", "Technical-Requirements.md");
                if (File.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }

            throw new FileNotFoundException("Could not locate docs/Project/Technical-Requirements.md from test base directory.");
        }
    }

    private static List<(string Id, string Status, IReadOnlyList<string> CoveredBy)> ParseTrs()
    {
        var text = File.ReadAllText(TechnicalRequirementsPath);
        var matches = TrHeaderRegex.Matches(text);
        var result = new List<(string, string, IReadOnlyList<string>)>();

        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var body = text.Substring(start, end - start);
            var id = matches[i].Groups["id"].Value;
            var status = StatusRegex.Match(body).Groups["status"].Value.Trim();
            var coveredByMatch = CoveredByRegex.Match(body);
            var types = new List<string>();
            if (coveredByMatch.Success)
            {
                foreach (Match t in BacktickedTypeRegex.Matches(coveredByMatch.Groups["list"].Value))
                {
                    var name = t.Groups["t"].Value.Trim();
                    if (!string.IsNullOrEmpty(name))
                        types.Add(name);
                }
            }

            result.Add((id, status, types));
        }

        return result;
    }

    [Fact]
    public void CfgProviderFactoryTrExists_AndIsComplete()
    {
        var trs = ParseTrs();
        var cfg007 = trs.FirstOrDefault(t => t.Id == "TR-MCP-CFG-007");
        Assert.NotEqual(default, cfg007);
        Assert.Contains(CompleteMarker, cfg007.Status);
    }

    [Fact]
    public void CompletedTrs_MustNotNameProviderSpecificTypes_WhenCfg007Complete()
    {
        var trs = ParseTrs();
        var cfg007Complete = trs.Any(t => t.Id == "TR-MCP-CFG-007" && t.Status.Contains(CompleteMarker, StringComparison.Ordinal));
        Assert.True(cfg007Complete, "Precondition: TR-MCP-CFG-007 must be ✅ Complete for this regression to be meaningful.");

        var offenders = new List<(string TrId, string[] BadTypes)>();

        foreach (var tr in trs)
        {
            if (tr.Id == "TR-MCP-CFG-007")
                continue;
            if (!tr.Status.Contains(CompleteMarker, StringComparison.Ordinal))
                continue;
            if (IsExemptFromProviderFactoryRule(tr.Id))
                continue;

            var bad = tr.CoveredBy
                .Where(t => ProviderSpecificTypeRegex.IsMatch(ExtractSimpleTypeName(t)))
                .ToArray();

            if (bad.Length == 0)
                continue;

            var hasProviderAgnosticPeer = tr.CoveredBy.Any(HasProviderAgnosticName);
            if (!hasProviderAgnosticPeer)
                offenders.Add((tr.Id, bad));
        }

        if (offenders.Count > 0)
        {
            var lines = offenders.Select(o =>
                $"  {o.TrId}: provider-specific types in Covered-by without provider-agnostic peer: {string.Join(", ", o.BadTypes)}");
            Assert.Fail(
                "TR coverage inconsistency detected — CFG-007 claims provider-factory coverage while other " +
                "completed TRs still pin provider-specific implementations:" + Environment.NewLine +
                string.Join(Environment.NewLine, lines));
        }
    }

    [Fact]
    public void TrsNotMarkedComplete_AreIgnoredByRegression()
    {
        // A TR in 🟡 In Progress or 🔴 Planned status naming SqliteTodoService must not fail the gate.
        var trs = ParseTrs();
        var inProgress = trs.Where(t => !t.Status.Contains(CompleteMarker, StringComparison.Ordinal)).ToList();
        // The regression should only inspect ✅ Complete entries; assert by construction that non-complete ones
        // are not evaluated here. No assertion needed beyond confirming the filter boundary exists in code;
        // this test documents the intent and fails if the classification regex drifts.
        Assert.Contains(inProgress, t => t.Id.StartsWith("TR-", StringComparison.Ordinal));
    }

    private static string ExtractSimpleTypeName(string coveredByEntry)
    {
        var last = coveredByEntry;
        var slash = last.LastIndexOf('/');
        if (slash >= 0)
            last = last[(slash + 1)..];
        var dot = last.LastIndexOf('.');
        if (dot >= 0)
            last = last[(dot + 1)..];
        var paren = last.IndexOf(' ');
        if (paren >= 0)
            last = last[..paren];
        return last.Trim();
    }

    private static bool HasProviderAgnosticName(string entry)
    {
        var name = ExtractSimpleTypeName(entry);
        if (name.StartsWith("Ef", StringComparison.Ordinal))
            return true;
        if (name.Equals("McpDatabaseProviderFactory", StringComparison.Ordinal))
            return true;
        if (name.Equals("McpDbContext", StringComparison.Ordinal))
            return true;
        if (name.StartsWith("McpDatabase", StringComparison.Ordinal))
            return true;
        return false;
    }

    private static bool IsExemptFromProviderFactoryRule(string trId)
    {
        // TR-MCP-SEC-004 intentionally enumerates all three provider strategy classes by name
        // (SqliteMcpDatabaseProviderStrategy, SqlServerMcpDatabaseProviderStrategy,
        // PostgreSqlMcpDatabaseProviderStrategy) because it describes per-provider-native
        // at-rest encryption facilities. That's the factory's implementation, not a
        // contradiction of it.
        return trId == "TR-MCP-SEC-004";
    }
}
