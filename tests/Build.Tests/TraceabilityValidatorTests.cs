namespace NukeBuild.Tests;

/// <summary>
/// TEST-NUKE-004: Verifies TraceabilityValidator correctly extracts requirement IDs
/// from markdown documents and validates coverage across mapping and matrix files.
/// </summary>
public sealed class TraceabilityValidatorTests
{
    [Fact]
    public void GetIdsFromHeadings_ExtractsFrIds()
    {
        string[] lines = ["# Header", "## FR-MCP-001 Some Feature", "## FR-MCP-002 Another Feature", "text"];
        var ids = TraceabilityValidator.GetIdsFromHeadings(lines,
            new System.Text.RegularExpressions.Regex(@"^##\s+(FR-[A-Z0-9-]+-\d{3})\b"));
        Assert.Equal(2, ids.Count);
        Assert.Equal("FR-MCP-001", ids[0]);
        Assert.Equal("FR-MCP-002", ids[1]);
    }

    [Fact]
    public void GetTestIds_ExtractsTestIds()
    {
        string[] lines = ["TEST-MCP-001 is a test", "and TEST-MCP-002 too", "no test here"];
        var ids = TraceabilityValidator.GetTestIds(lines);
        Assert.Equal(2, ids.Count);
        Assert.Contains("TEST-MCP-001", ids);
        Assert.Contains("TEST-MCP-002", ids);
    }

    [Fact]
    public void GetTestIds_ExtractsSuffixedAndNamedTestIds()
    {
        string[] lines =
        [
            "- TEST-SUPPORT-010A-1: workspace stamping coverage",
            "- TEST-MCP-REQACPLUGIN-BASH: plugin acceptance criteria coverage",
        ];

        var ids = TraceabilityValidator.GetTestIds(lines);

        Assert.Contains("TEST-SUPPORT-010A-1", ids);
        Assert.Contains("TEST-MCP-REQACPLUGIN-BASH", ids);
    }

    [Fact]
    public void GetMappingFrIds_ExtractsFrIdsFromTable()
    {
        string[] lines = ["| FR-MCP-001 | TR-MCP-ARCH-001 |", "| FR-MCP-002 | TR-MCP-API-001 |", "| header |"];
        var ids = TraceabilityValidator.GetMappingFrIds(lines);
        Assert.Equal(2, ids.Count);
        Assert.Equal("FR-MCP-001", ids[0]);
    }

    [Fact]
    public void GetMappingFrIds_ExtractsLetterSuffixedFrIdsFromTable()
    {
        string[] lines = ["| FR-SUPPORT-010A | TR-MCP-MT-003A | TEST-SUPPORT-010A-1 |"];

        var ids = TraceabilityValidator.GetMappingFrIds(lines);

        Assert.Single(ids);
        Assert.Equal("FR-SUPPORT-010A", ids[0]);
    }

    [Fact]
    public void ExpandRangeToken_SingleId_ReturnsSelf()
    {
        var result = TraceabilityValidator.ExpandRangeToken("FR-MCP-001").ToList();
        Assert.Single(result);
        Assert.Equal("FR-MCP-001", result[0]);
    }

    [Fact]
    public void ExpandRangeToken_Range_ExpandsCorrectly()
    {
        var result = TraceabilityValidator.ExpandRangeToken("FR-MCP-001-003").ToList();
        Assert.Equal(3, result.Count);
        Assert.Equal("FR-MCP-001", result[0]);
        Assert.Equal("FR-MCP-002", result[1]);
        Assert.Equal("FR-MCP-003", result[2]);
    }

    [Fact]
    public void GetMatrixIds_ExpandsRanges()
    {
        string[] lines = ["| FR-MCP-001-003 | Planned |", "| TR-MCP-ARCH-001 | Done |"];
        var ids = TraceabilityValidator.GetMatrixIds(lines);
        Assert.Contains("FR-MCP-001", ids);
        Assert.Contains("FR-MCP-002", ids);
        Assert.Contains("FR-MCP-003", ids);
        Assert.Contains("TR-MCP-ARCH-001", ids);
    }

    [Fact]
    public void GetMatrixIds_ExpandsEnDashRanges()
    {
        string[] lines = ["| TR-MCP-DATA-001–003 | Complete | Storage and indexing |"];

        var ids = TraceabilityValidator.GetMatrixIds(lines);

        Assert.Contains("TR-MCP-DATA-001", ids);
        Assert.Contains("TR-MCP-DATA-002", ids);
        Assert.Contains("TR-MCP-DATA-003", ids);
    }

    [Fact]
    public void GetMatrixIds_PreservesLiteralRangeLikeIds()
    {
        string[] lines = ["| TR-MCP-AGENT-PARITY-020-027 | Tracked | Technical-Requirements.md |"];

        var ids = TraceabilityValidator.GetMatrixIds(lines);

        Assert.Contains("TR-MCP-AGENT-PARITY-020-027", ids);
        Assert.Contains("TR-MCP-AGENT-PARITY-020", ids);
        Assert.Contains("TR-MCP-AGENT-PARITY-027", ids);
    }

    [Fact]
    public void Validate_AllPresent_ReturnsNoMissing()
    {
        string[] fr = ["## FR-MCP-001 Feature"];
        string[] tr = ["## TR-MCP-ARCH-001 Arch"];
        string[] test = ["TEST-MCP-001 test"];
        string[] mapping = ["| FR-MCP-001 | TR-MCP-ARCH-001 |"];
        string[] matrix = ["| FR-MCP-001 | Done |", "| TR-MCP-ARCH-001 | Done |", "| TEST-MCP-001 | Done |"];

        var result = TraceabilityValidator.Validate(fr, tr, test, mapping, matrix);
        Assert.Empty(result.MissingFrInMapping);
        Assert.Empty(result.MissingFrInMatrix);
        Assert.Empty(result.MissingTrInMatrix);
        Assert.Empty(result.MissingTestInMatrix);
    }

    [Fact]
    public void Validate_LetterSuffixedRequirements_AreCovered()
    {
        string[] fr = ["## FR-SUPPORT-010A SessionLog Workspace Stamping"];
        string[] tr = ["## TR-MCP-MT-003A"];
        string[] test = ["- TEST-SUPPORT-010A-1: workspace stamping coverage"];
        string[] mapping = ["| FR-SUPPORT-010A | TR-MCP-MT-003A | TEST-SUPPORT-010A-1 |"];
        string[] matrix =
        [
            "| FR-SUPPORT-010A | Tracked | Functional-Requirements.md |",
            "| TR-MCP-MT-003A | Tracked | Technical-Requirements.md |",
            "| TEST-SUPPORT-010A-1 | Tracked | Testing-Requirements.md |",
        ];

        var result = TraceabilityValidator.Validate(fr, tr, test, mapping, matrix);

        Assert.Empty(result.MissingFrInMapping);
        Assert.Empty(result.MissingFrInMatrix);
        Assert.Empty(result.MissingTrInMatrix);
        Assert.Empty(result.MissingTestInMatrix);
    }

    [Fact]
    public void Validate_LetterSuffixedRequirementsMissingTraceability_AreReported()
    {
        string[] fr = ["## FR-SUPPORT-010A SessionLog Workspace Stamping"];
        string[] tr = ["## TR-MCP-MT-003A"];
        string[] test = ["- TEST-SUPPORT-010A-1: workspace stamping coverage"];
        string[] mapping = [];
        string[] matrix = [];

        var result = TraceabilityValidator.Validate(fr, tr, test, mapping, matrix);

        Assert.Contains("FR-SUPPORT-010A", result.MissingFrInMapping);
        Assert.Contains("FR-SUPPORT-010A", result.MissingFrInMatrix);
        Assert.Contains("TR-MCP-MT-003A", result.MissingTrInMatrix);
        Assert.Contains("TEST-SUPPORT-010A-1", result.MissingTestInMatrix);
    }

    [Fact]
    public void Validate_MissingFrInMapping_ReportsCorrectly()
    {
        string[] fr = ["## FR-MCP-001 Feature", "## FR-MCP-002 Feature2"];
        string[] tr = [];
        string[] test = [];
        string[] mapping = ["| FR-MCP-001 | TR-MCP-ARCH-001 |"];
        string[] matrix = ["| FR-MCP-001 | Done |", "| FR-MCP-002 | Done |"];

        var result = TraceabilityValidator.Validate(fr, tr, test, mapping, matrix);
        Assert.Single(result.MissingFrInMapping);
        Assert.Equal("FR-MCP-002", result.MissingFrInMapping[0]);
    }

    [Fact]
    public void ValidationResult_HasFrErrors_TrueWhenMissing()
    {
        var result = new TraceabilityValidator.ValidationResult
        {
            MissingFrInMapping = ["FR-MCP-001"],
        };
        Assert.True(result.HasFrErrors);
    }
}
