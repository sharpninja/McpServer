namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-AIUNIT-002: Verifies the warning suppression scanner inventory used by
/// TR-MCP-QUALITY-001 before warning remediation removes or approves suppressions.
/// </summary>
public sealed class WarningSuppressionScannerTests
{
    /// <summary>
    /// TEST-MCP-AIUNIT-002: Creates a synthetic repository with each supported suppression
    /// mechanism so the scanner proves it can report the current warning bypass surface.
    /// </summary>
    [Fact]
    public void Scan_FixtureRepository_ReportsAllSuppressionMechanisms()
    {
        var root = CreateTempRoot();
        try
        {
            WriteFile(root, "src/App/App.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <NoWarn>$(NoWarn);CA1002, CS9996</NoWarn>
                    <WarningsNotAsErrors>CA9991;CA9992</WarningsNotAsErrors>
                    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
                    <SuppressTrimAnalysisWarnings>true</SuppressTrimAnalysisWarnings>
                    <ErrorOnDuplicatePublishOutputFiles>false</ErrorOnDuplicatePublishOutputFiles>
                  </PropertyGroup>
                </Project>
                """);
            WriteFile(root, "src/App/Example.cs", """
                using System.Diagnostics.CodeAnalysis;

                #pragma warning disable 612, CA9995
                [SuppressMessage("Usage", "CA1819:Properties should not return arrays", Justification = "Fixture")]
                internal sealed class Example
                {
                }
                """);
            WriteFile(root, ".editorconfig", """
                root = true

                [*.cs]
                dotnet_diagnostic.CA9993.severity = none
                dotnet_diagnostic.CA9994.severity = silent
                """);

            var occurrences = WarningSuppressionScanner.Scan(root);

            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.NoWarn && occurrence.DiagnosticId == "CA1002");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.NoWarn && occurrence.DiagnosticId == "CS9996");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.WarningsNotAsErrors && occurrence.DiagnosticId == "CA9991");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.TreatWarningsAsErrorsFalse && occurrence.DiagnosticId == "TreatWarningsAsErrors");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.SuppressTrimAnalysisWarnings && occurrence.DiagnosticId == "SuppressTrimAnalysisWarnings");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.ErrorOnDuplicatePublishOutputFiles && occurrence.DiagnosticId == "ErrorOnDuplicatePublishOutputFiles");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.PragmaWarningDisable && occurrence.DiagnosticId == "CS0612");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.PragmaWarningDisable && occurrence.DiagnosticId == "CA9995");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.SuppressMessage && occurrence.DiagnosticId == "CA1819");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.EditorConfigSeverity && occurrence.DiagnosticId == "CA9993");
            Assert.Contains(occurrences, occurrence => occurrence.Mechanism == WarningSuppressionMechanism.EditorConfigSeverity && occurrence.DiagnosticId == "CA9994");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Creates included and excluded repository paths so build output,
    /// generated source, scanner output folders, and warning-governance fixture files are ignored.
    /// </summary>
    [Fact]
    public void Scan_RepositoryBoundaries_ExcludesBuildOutputGeneratedCodeAndGovernanceFixtures()
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
            WriteFile(root, "bin/Debug/Skipped.csproj", "<Project><PropertyGroup><NoWarn>CA9999</NoWarn></PropertyGroup></Project>");
            WriteFile(root, "obj/Debug/Skipped.csproj", "<Project><PropertyGroup><NoWarn>CA9998</NoWarn></PropertyGroup></Project>");
            WriteFile(root, ".git/Skipped.csproj", "<Project><PropertyGroup><NoWarn>CA9997</NoWarn></PropertyGroup></Project>");
            WriteFile(root, "node_modules/pkg/Skipped.csproj", "<Project><PropertyGroup><NoWarn>CA9996</NoWarn></PropertyGroup></Project>");
            WriteFile(root, "artifacts/Skipped.csproj", "<Project><PropertyGroup><NoWarn>CA9995</NoWarn></PropertyGroup></Project>");
            WriteFile(root, "artifacts/warnings/generated.csproj", "<Project><PropertyGroup><NoWarn>CA9994</NoWarn></PropertyGroup></Project>");
            WriteFile(root, "TestResults/Skipped.csproj", "<Project><PropertyGroup><NoWarn>CA9993</NoWarn></PropertyGroup></Project>");
            WriteFile(root, "src/Migrations/20260328000000_Create.Designer.cs", """
                // <auto-generated />
                #pragma warning disable 612, 618
                """);
            WriteFile(root, "src/Generated/Contract.g.cs", "#pragma warning disable CA1819");
            WriteFile(root, "tests/Build.Tests/WarningSuppressionScannerTests.cs", "<Project><PropertyGroup><NoWarn>CA9990</NoWarn></PropertyGroup></Project>");

            var occurrences = WarningSuppressionScanner.Scan(root);
            var diagnostics = occurrences.Select(occurrence => occurrence.DiagnosticId).ToArray();

            Assert.Contains("CA1002", diagnostics);
            Assert.Contains("CA1819", diagnostics);
            Assert.DoesNotContain("CS0612", diagnostics);
            Assert.DoesNotContain("CS0618", diagnostics);
            Assert.DoesNotContain("CA9990", diagnostics);
            Assert.DoesNotContain("CA9999", diagnostics);
            Assert.DoesNotContain("CA9998", diagnostics);
            Assert.DoesNotContain("CA9997", diagnostics);
            Assert.DoesNotContain("CA9996", diagnostics);
            Assert.DoesNotContain("CA9995", diagnostics);
            Assert.DoesNotContain("CA9994", diagnostics);
            Assert.DoesNotContain("CA9993", diagnostics);
            Assert.Equal(occurrences.OrderBy(occurrence => occurrence.RelativePath, StringComparer.OrdinalIgnoreCase).ThenBy(occurrence => occurrence.LineNumber).ToArray(), occurrences);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcpserver-warning-scanner-test-{Guid.NewGuid():N}");
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
