using System.Security.Cryptography;
using McpServer.Support.Mcp.Tests.Infrastructure;

namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>
/// TEST-MCP-USECASE-020-AC008: Eighteenth-review proof that historical receipts are immutable and
/// rejection/correction state lives in the append-only evidence index.
/// </summary>
public sealed class BugTriage139EighteenthEvidenceTests
{
    /// <summary>Verifies every published historical receipt against its immutable Git-era hash.</summary>
    [Theory]
    [InlineData(
        "BUG-TRIAGE-139-ninth-remediation.md",
        "CC453A428796AE4BFB97C395502F7D082145A2DA2FEE0E9CA265EA30D10F8D7E")]
    [InlineData(
        "BUG-TRIAGE-139-tenth-remediation.md",
        "5AB2C2A2600A85DE6F7DB8A8F50E38226A0693EA89698F26FA542016795D1057")]
    [InlineData(
        "BUG-TRIAGE-139-eleventh-remediation.md",
        "5B9F17C95CBD6EF471F8DC8B9D3F59CD587DE481E452A0647CAEB08F781BF1AC")]
    [InlineData(
        "BUG-TRIAGE-139-twelfth-remediation.md",
        "3F40F3195168574A52A27030902B22F62917683ED6BB8E80415AF4B4D0622628")]
    [InlineData(
        "BUG-TRIAGE-139-fourteenth-remediation.md",
        "3B09DF63D5860DEC2A96E7589CB528586CBB6726130E1BFB5C3E356184FF5C33")]
    [InlineData(
        "BUG-TRIAGE-139-fifteenth-remediation.md",
        "04BA22D37B5F29042A45B6D9A55FCE11EE23F4C33148B19BCDCE20052000F91C")]
    [InlineData(
        "BUG-TRIAGE-139-sixteenth-remediation.md",
        "CB3D04D459ED657F7DB579F712C72552638233C9695888E2FC206C12FA828DEB")]
    public void HistoricalReceipt_MatchesImmutablePublishedHash(
        string fileName,
        string expectedSha256)
    {
        var path = Path.Combine(
            RepositoryEvidenceTestSupport.ResolveRepositoryRoot(),
            "docs",
            "receipts",
            fileName);
        var bytes = File.ReadAllBytes(path);

        Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(bytes)));
    }

    /// <summary>
    /// Verifies the exact independent rejection used as the eighteenth remediation input is durable
    /// and matches the operator-provided SHA-256.
    /// </summary>
    [Fact]
    public void EighteenthInputReview_IsDurableAndByteExact()
    {
        var path = Path.Combine(
            RepositoryEvidenceTestSupport.ResolveRepositoryRoot(),
            "docs",
            "receipts",
            "artifacts",
            "BUG-TRIAGE-139",
            "reviews",
            "eighteenth-input-independent-review-3ce5a814.txt");
        var bytes = File.ReadAllBytes(path);

        Assert.Equal(
            "B39E4AF54974655D31F31A16B461390017C39350F618FB17DB74AB92BFD2F842",
            Convert.ToHexString(SHA256.HashData(bytes)));
    }

    /// <summary>
    /// Verifies corrections, canonical TODO/triage ownership, and the fixed exact-ledger path are
    /// declared only by the current append-only index.
    /// </summary>
    [Fact]
    public void CurrentEvidenceIndex_RecordsSupersessionOwnershipAndExactLedgerPath()
    {
        var path = Path.Combine(
            RepositoryEvidenceTestSupport.ResolveRepositoryRoot(),
            "docs",
            "receipts",
            "BUG-TRIAGE-139-evidence-index.md");
        var index = File.ReadAllText(path);

        Assert.Contains("append-only evidence index", index, StringComparison.Ordinal);
        Assert.Contains("req-20260905T105643Z-aff5", index, StringComparison.Ordinal);
        Assert.Contains("req-20260905T105727Z-bef9", index, StringComparison.Ordinal);
        Assert.Contains("req-20260905T105810Z-20b4", index, StringComparison.Ordinal);
        Assert.Contains("done: false", index, StringComparison.Ordinal);
        Assert.Contains(
            @"C:\Users\kingd\AppData\Local\Temp\BUG-TRIAGE-139-eighteenth\final-verification\exact-final-head.yaml",
            index,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "final-verification/exact-3ce5a81402b4/exact-final-head.yaml",
            index,
            StringComparison.Ordinal);
    }
}
