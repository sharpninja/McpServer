namespace NukeBuild.IntegrationTests;

/// <summary>
/// TEST-MCP-AIUNIT-002: Verifies the warning suppression approval register validator
/// required by TR-MCP-QUALITY-001 before approved suppressions can remain in source.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-AIUNIT-002; TR-MCP-QUALITY-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from its four infrastructure cases in Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"). ApprovalRegister_ContainsOnlyScopedCurrentApprovalsAndMatchesCurrentOccurrences and FindRepositoryRoot, which only it uses, moved unchanged; the halves share no private helper; only the namespace changed.</remarks>
public sealed class WarningSuppressionApprovalValidatorTests
{
    /// <summary>
    /// TEST-MCP-AIUNIT-002: Loads the repository approval register to prove current
    /// warning suppression approvals are scoped and match the live scanner inventory.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void ApprovalRegister_ContainsOnlyScopedCurrentApprovalsAndMatchesCurrentOccurrences()
    {
        var repositoryRoot = FindRepositoryRoot();
        var approvalPath = Path.Combine(repositoryRoot, "config", "warning-suppression-approvals.json");
        var allowedDiagnostics = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CA1416",
            "CA1819",
            "CA1848",
            "CA2227",
            "CS8602",
        };
        var approvals = WarningSuppressionApprovalLoader.Load(approvalPath);
        var occurrences = WarningSuppressionScanner.Scan(repositoryRoot);
        var errors = WarningSuppressionApprovalValidator.Validate(approvals, occurrences);

        Assert.NotEmpty(approvals);
        Assert.All(approvals, approval => Assert.True(approval.DiagnosticId is not null && allowedDiagnostics.Contains(approval.DiagnosticId)));
        Assert.DoesNotContain(approvals, approval => approval.Scope?.Contains('*', StringComparison.Ordinal) == true);
        Assert.DoesNotContain(approvals, approval => approval.Scope?.Contains('?', StringComparison.Ordinal) == true);
        Assert.Empty(errors);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
