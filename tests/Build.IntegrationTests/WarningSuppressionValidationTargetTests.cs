using NukeBuild.Tests;

namespace NukeBuild.IntegrationTests;

/// <summary>
/// TEST-MCP-AIUNIT-002: Verifies the ValidateWarningSuppressions Nuke target contract
/// required by TR-MCP-QUALITY-001 warning governance.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-AIUNIT-002; TR-MCP-QUALITY-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of WarningSuppressionValidationTargetTests, split from its nine infrastructure cases in Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"). The eight Scan_CurrentRepository_* cases and the helpers only they use (IsGeneratedFile, FindGeneratedObsoletePragmas, FindRepositoryRoot) moved unchanged; the generated-obsolete pragma text both halves use is the one linked GeneratedObsoletePragmaText, read by the two fields below, not a copy; only the namespace changed.</remarks>
public sealed class WarningSuppressionValidationTargetTests
{
    private static readonly string GeneratedObsoleteDisablePragma = GeneratedObsoletePragmaText.Disable;

    private static readonly string GeneratedObsoleteRestorePragma = GeneratedObsoletePragmaText.Restore;

    /// <summary>
    /// TEST-MCP-AIUNIT-002: W9 regression coverage proves CA1031 broad-catch
    /// suppressions are removed from the live repository suppression inventory.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Scan_CurrentRepository_HasNoCA1031Suppressions()
    {
        var root = FindRepositoryRoot();

        var occurrences = WarningSuppressionScanner.Scan(root)
            .Where(occurrence => string.Equals(occurrence.DiagnosticId, "CA1031", StringComparison.Ordinal))
            .Select(occurrence => $"{occurrence.RelativePath}:{occurrence.LineNumber} ({occurrence.Mechanism})")
            .ToArray();

        Assert.True(occurrences.Length == 0, $"Unexpected CA1031 suppressions: {string.Join(", ", occurrences)}");
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: W10 regression coverage proves stale URI-design
    /// suppressions are removed from the live repository suppression inventory.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Scan_CurrentRepository_HasNoCA1054OrCA1056Suppressions()
    {
        var root = FindRepositoryRoot();
        string[] diagnosticIds = ["CA1054", "CA1056"];

        var occurrences = WarningSuppressionScanner.Scan(root)
            .Where(occurrence => diagnosticIds.Contains(occurrence.DiagnosticId, StringComparer.Ordinal))
            .Select(occurrence => $"{occurrence.DiagnosticId} at {occurrence.RelativePath}:{occurrence.LineNumber} ({occurrence.Mechanism})")
            .ToArray();

        Assert.True(occurrences.Length == 0, $"Unexpected URI suppressions: {string.Join(", ", occurrences)}");
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: W11 regression coverage proves stale externally-visible
    /// type suppressions are removed from the live repository suppression inventory.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Scan_CurrentRepository_HasNoCA1515Suppressions()
    {
        var root = FindRepositoryRoot();

        var occurrences = WarningSuppressionScanner.Scan(root)
            .Where(occurrence => string.Equals(occurrence.DiagnosticId, "CA1515", StringComparison.Ordinal))
            .Select(occurrence => $"{occurrence.RelativePath}:{occurrence.LineNumber} ({occurrence.Mechanism})")
            .ToArray();

        Assert.True(occurrences.Length == 0, $"Unexpected CA1515 suppressions: {string.Join(", ", occurrences)}");
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: W12 regression coverage proves stale sync-in-async
    /// suppressions are removed from the live repository suppression inventory.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Scan_CurrentRepository_HasNoCA1849Suppressions()
    {
        var root = FindRepositoryRoot();

        var occurrences = WarningSuppressionScanner.Scan(root)
            .Where(occurrence => string.Equals(occurrence.DiagnosticId, "CA1849", StringComparison.Ordinal))
            .Select(occurrence => $"{occurrence.RelativePath}:{occurrence.LineNumber} ({occurrence.Mechanism})")
            .ToArray();

        Assert.True(occurrences.Length == 0, $"Unexpected CA1849 suppressions: {string.Join(", ", occurrences)}");
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: W13 regression coverage proves stale external-path
    /// suppressions are removed from the live repository suppression inventory.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Scan_CurrentRepository_HasNoCA3003Suppressions()
    {
        var root = FindRepositoryRoot();

        var occurrences = WarningSuppressionScanner.Scan(root)
            .Where(occurrence => string.Equals(occurrence.DiagnosticId, "CA3003", StringComparison.Ordinal))
            .Select(occurrence => $"{occurrence.RelativePath}:{occurrence.LineNumber} ({occurrence.Mechanism})")
            .ToArray();

        Assert.True(occurrences.Length == 0, $"Unexpected CA3003 suppressions: {string.Join(", ", occurrences)}");
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: W14 regression coverage proves stale missing-XML-doc
    /// suppressions are removed from the live repository suppression inventory.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Scan_CurrentRepository_HasNoCS1591Suppressions()
    {
        var root = FindRepositoryRoot();

        var occurrences = WarningSuppressionScanner.Scan(root)
            .Where(occurrence => string.Equals(occurrence.DiagnosticId, "CS1591", StringComparison.Ordinal))
            .Select(occurrence => $"{occurrence.RelativePath}:{occurrence.LineNumber} ({occurrence.Mechanism})")
            .ToArray();

        Assert.True(occurrences.Length == 0, $"Unexpected CS1591 suppressions: {string.Join(", ", occurrences)}");
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: W15 regression coverage proves CA2227 suppressions remain
    /// only on serializer DTO compatibility files that still require mutable setters.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Scan_CurrentRepository_HasOnlySerializerCompatibilityCA2227Suppressions()
    {
        var root = FindRepositoryRoot();
        string[] allowedScopes =
        [
            "src/QBrainAi.Services/Models/TodoModels.cs",
            "src/QBrainAi.Services/Models/UnifiedSessionLogDto.cs",
        ];

        var unexpectedOccurrences = WarningSuppressionScanner.Scan(root)
            .Where(occurrence => string.Equals(occurrence.DiagnosticId, "CA2227", StringComparison.Ordinal))
            .Where(occurrence => !allowedScopes.Contains(occurrence.RelativePath, StringComparer.Ordinal))
            .Select(occurrence => $"{occurrence.RelativePath}:{occurrence.LineNumber} ({occurrence.Mechanism})")
            .ToArray();

        Assert.True(unexpectedOccurrences.Length == 0, $"Unexpected CA2227 suppressions: {string.Join(", ", unexpectedOccurrences)}");
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: W18 regression coverage proves generated provider
    /// migrations do not reintroduce CS0612/CS0618 pragma pairs.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Scan_CurrentRepository_HasNoGeneratedMigrationObsoleteWarningPragmas()
    {
        var root = FindRepositoryRoot();
        string[] migrationRoots =
        [
            Path.Combine(root, "src", "QBrainAi.Storage", "Migrations"),
            Path.Combine(root, "src", "QBrainAi.Storage.SqliteMigrations"),
            Path.Combine(root, "src", "QBrainAi.Storage.SqlServerMigrations"),
            Path.Combine(root, "src", "QBrainAi.Storage.PostgreSqlMigrations"),
        ];

        var occurrences = migrationRoots
            .SelectMany(migrationRoot => Directory.GetFiles(migrationRoot, "*.cs", SearchOption.AllDirectories))
            .Where(IsGeneratedFile)
            .SelectMany(file => FindGeneratedObsoletePragmas(root, file))
            .ToArray();

        Assert.True(occurrences.Length == 0, $"Unexpected generated migration obsolete warning pragmas: {string.Join(", ", occurrences)}");
    }

    private static bool IsGeneratedFile(string file)
    {
        return File.ReadLines(file).FirstOrDefault()?.StartsWith("// <auto-generated", StringComparison.Ordinal) == true;
    }

    private static IEnumerable<string> FindGeneratedObsoletePragmas(string root, string file)
    {
        var relativePath = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
        var lineNumber = 0;
        foreach (var line in File.ReadLines(file))
        {
            lineNumber++;
            if (line.Contains(GeneratedObsoleteDisablePragma, StringComparison.Ordinal)
                || line.Contains(GeneratedObsoleteRestorePragma, StringComparison.Ordinal))
            {
                yield return $"{relativePath}:{lineNumber}";
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "config", "warning-suppression-approvals.json"))
                && Directory.Exists(Path.Combine(current.FullName, "src"))
                && Directory.Exists(Path.Combine(current.FullName, "tests")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root from the test output directory.");
    }
}
