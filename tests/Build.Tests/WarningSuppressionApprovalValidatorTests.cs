using System.Text.Json;

namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-AIUNIT-002: Verifies the warning suppression approval register validator
/// required by TR-MCP-QUALITY-001 before approved suppressions can remain in source.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-AIUNIT-002; TR-MCP-QUALITY-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the infrastructure half of this class; its Category=Integration case ApprovalRegister_ContainsOnlyScopedCurrentApprovalsAndMatchesCurrentOccurrences and FindRepositoryRoot, which only that case used, moved unchanged to the class of the same name in the integration-owned Build.IntegrationTests, so this infrastructure-owned project holds no integration case (slice S4, hostile validation finding R14-F4); the four remaining cases are unchanged.</remarks>
public sealed class WarningSuppressionApprovalValidatorTests
{
    /// <summary>
    /// TEST-MCP-AIUNIT-002: Uses an empty approval object to prove every required
    /// approval contract field is validated independently.
    /// </summary>
    [Fact]
    public void Validate_MissingRequiredFields_ReportsEachMissingContractField()
    {
        WarningSuppressionApproval[] approvals = [new()];
        WarningSuppressionOccurrence[] occurrences = [];

        var errors = WarningSuppressionApprovalValidator.Validate(approvals, occurrences);
        var codes = errors.Select(error => error.Code).ToArray();

        Assert.Contains("missing_diagnostic", codes);
        Assert.Contains("missing_scope", codes);
        Assert.Contains("missing_mechanism", codes);
        Assert.Contains("missing_justification", codes);
        Assert.Contains("missing_owner", codes);
        Assert.Contains("missing_permanence", codes);
        Assert.Contains("missing_review_condition", codes);
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Uses duplicate exact approvals and a wildcard scope to prove
    /// the approval register remains deny-by-default and cannot approve broad paths.
    /// </summary>
    [Fact]
    public void Validate_DuplicateAndBroadApprovals_ReportErrors()
    {
        WarningSuppressionApproval[] approvals =
        [
            CreateApproval("CA1002", "src/App/App.cs", WarningSuppressionMechanism.NoWarn),
            CreateApproval("CA1002", "src/App/App.cs", WarningSuppressionMechanism.NoWarn),
            CreateApproval("CA1031", "src/**/*.cs", WarningSuppressionMechanism.PragmaWarningDisable),
        ];
        WarningSuppressionOccurrence[] occurrences =
        [
            new("src/App/App.cs", 4, "CA1002", WarningSuppressionMechanism.NoWarn, "<NoWarn>CA1002</NoWarn>"),
        ];

        var errors = WarningSuppressionApprovalValidator.Validate(approvals, occurrences);
        var codes = errors.Select(error => error.Code).ToArray();

        Assert.Contains("duplicate_approval", codes);
        Assert.Contains("broad_scope", codes);
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Uses one stale approval and one changed mechanism approval
    /// to prove approvals are bound to the current source occurrence shape.
    /// </summary>
    [Fact]
    public void Validate_StaleApprovalAndChangedMechanism_ReportErrors()
    {
        WarningSuppressionApproval[] approvals =
        [
            CreateApproval("CA1002", "src/App/App.cs", WarningSuppressionMechanism.NoWarn),
            CreateApproval("CA9999", "src/App/Missing.cs", WarningSuppressionMechanism.SuppressMessage),
        ];
        WarningSuppressionOccurrence[] occurrences =
        [
            new("src/App/App.cs", 7, "CA1002", WarningSuppressionMechanism.PragmaWarningDisable, "#pragma warning disable CA1002"),
        ];

        var errors = WarningSuppressionApprovalValidator.Validate(approvals, occurrences);
        var codes = errors.Select(error => error.Code).ToArray();

        Assert.Contains("changed_mechanism", codes);
        Assert.Contains("stale_approval", codes);
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Uses a temporary approval register and scanner inventory
    /// to prove the inventory writer emits machine-readable JSON without changing approvals.
    /// </summary>
    [Fact]
    public void LoadValidateAndWriteInventory_PreservesApprovalSourceAndWritesJson()
    {
        var root = CreateTempRoot();
        try
        {
            var approvalPath = Path.Combine(root, "config", "warning-suppression-approvals.json");
            Directory.CreateDirectory(Path.GetDirectoryName(approvalPath)!);
            var approvalJson = """
                [
                  {
                    "diagnosticId": "CA1002",
                    "scope": "src/App/App.cs",
                    "mechanism": "NoWarn",
                    "justification": "Fixture approval",
                    "owner": "Quality",
                    "permanence": "conditional",
                    "reviewCondition": "Remove after API migration"
                  }
                ]
                """;
            File.WriteAllText(approvalPath, approvalJson);
            var originalApprovalJson = File.ReadAllText(approvalPath);
            var occurrences = new[]
            {
                new WarningSuppressionOccurrence("src/App/App.cs", 3, "CA1002", WarningSuppressionMechanism.NoWarn, "<NoWarn>CA1002</NoWarn>"),
            };

            var approvals = WarningSuppressionApprovalLoader.Load(approvalPath);
            var errors = WarningSuppressionApprovalValidator.Validate(approvals, occurrences);
            var inventoryPath = WarningSuppressionApprovalValidator.WriteInventory(
                Path.Combine(root, "artifacts", "warnings"),
                occurrences,
                approvals,
                errors);

            Assert.Empty(errors);
            Assert.Equal(originalApprovalJson, File.ReadAllText(approvalPath));
            Assert.True(File.Exists(inventoryPath));
            using var document = JsonDocument.Parse(File.ReadAllText(inventoryPath));
            Assert.True(document.RootElement.TryGetProperty("occurrences", out var inventoryOccurrences));
            Assert.True(document.RootElement.TryGetProperty("approvals", out var inventoryApprovals));
            Assert.Equal("CA1002", inventoryOccurrences[0].GetProperty("diagnosticId").GetString());
            Assert.Equal("CA1002", inventoryApprovals[0].GetProperty("diagnosticId").GetString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static WarningSuppressionApproval CreateApproval(
        string diagnosticId,
        string scope,
        WarningSuppressionMechanism mechanism)
    {
        return new WarningSuppressionApproval
        {
            DiagnosticId = diagnosticId,
            Scope = scope,
            Mechanism = mechanism.ToString(),
            Justification = "Fixture justification",
            Owner = "Quality",
            Permanence = "conditional",
            ReviewCondition = "Remove when fixture is remediated",
        };
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcpserver-warning-approval-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
