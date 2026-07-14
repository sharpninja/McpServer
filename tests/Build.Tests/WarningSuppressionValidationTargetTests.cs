namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-AIUNIT-002: Verifies the ValidateWarningSuppressions Nuke target contract
/// required by TR-MCP-QUALITY-001 warning governance.
/// </summary>
public sealed class WarningSuppressionValidationTargetTests
{
    private const string PragmaToken = "#pragma";

    private static readonly string GeneratedObsoleteDisablePragma = PragmaToken + " warning disable " + "612, 618";

    private static readonly string GeneratedObsoleteRestorePragma = PragmaToken + " warning restore " + "612, 618";

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Uses reflection to prove the build exposes a dedicated
    /// validation target for warning suppression governance.
    /// </summary>
    [Fact]
    public void Build_HasValidateWarningSuppressionsTarget()
    {
        var prop = typeof(Build).GetProperty("ValidateWarningSuppressions");

        Assert.NotNull(prop);
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Uses an unapproved NoWarn occurrence to prove repository
    /// validation returns a concise file and line diagnostic before the build target fails.
    /// </summary>
    [Fact]
    public void ValidateRepository_UnapprovedOccurrence_ReturnsFileLineDiagnostic()
    {
        var root = CreateTempRoot();
        try
        {
            WriteFile(root, "src/App/App.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <NoWarn>CA1002</NoWarn>
                  </PropertyGroup>
                </Project>
                """);
            WriteFile(root, "config/warning-suppression-approvals.json", "[]");

            var errors = WarningSuppressionApprovalValidator.ValidateRepository(
                root,
                Path.Combine(root, "config", "warning-suppression-approvals.json"),
                Path.Combine(root, "artifacts", "warnings"));

            var error = Assert.Single(errors);
            Assert.Equal("unapproved_occurrence", error.Code);
            Assert.Equal("CA1002", error.DiagnosticId);
            Assert.Equal("src/App/App.csproj", error.Scope);
            Assert.Equal(3, error.LineNumber);
            Assert.Contains("src/App/App.csproj:3", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Uses invalid approval JSON to prove parse failures are
    /// reported as validation errors with the approval path instead of being hidden.
    /// </summary>
    [Fact]
    public void ValidateRepository_InvalidApprovalJson_ReturnsParseError()
    {
        var root = CreateTempRoot();
        try
        {
            WriteFile(root, "config/warning-suppression-approvals.json", "{ invalid json");

            var errors = WarningSuppressionApprovalValidator.ValidateRepository(
                root,
                Path.Combine(root, "config", "warning-suppression-approvals.json"),
                Path.Combine(root, "artifacts", "warnings"));

            var error = Assert.Single(errors);
            Assert.Equal("approval_parse_error", error.Code);
            Assert.Equal("config/warning-suppression-approvals.json", error.Scope);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: W9 regression coverage proves CA1031 broad-catch
    /// suppressions are removed from the live repository suppression inventory.
    /// </summary>
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
    [Fact]
    public void Scan_CurrentRepository_HasOnlySerializerCompatibilityCA2227Suppressions()
    {
        var root = FindRepositoryRoot();
        string[] allowedScopes =
        [
            "src/McpServer.Services/Models/TodoModels.cs",
            "src/McpServer.Services/Models/UnifiedSessionLogDto.cs",
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
    [Fact]
    public void Scan_CurrentRepository_HasNoGeneratedMigrationObsoleteWarningPragmas()
    {
        var root = FindRepositoryRoot();
        string[] migrationRoots =
        [
            Path.Combine(root, "src", "McpServer.Storage", "Migrations"),
            Path.Combine(root, "src", "McpServer.Storage.SqliteMigrations"),
            Path.Combine(root, "src", "McpServer.Storage.SqlServerMigrations"),
            Path.Combine(root, "src", "McpServer.Storage.PostgreSqlMigrations"),
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

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcpserver-warning-target-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void WriteFile(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
