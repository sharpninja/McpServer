using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Storage.Entities;

namespace QBrainAi.Support.Mcp.Tests.Requirements;

/// <summary>
/// FR-MCP-040 ac-1 (PR 87 slice P): binds the unchanged <see cref="RequirementStoredTimestampContractTests"/> rows and
/// assertions, mock-gated in ug-r9-mockgate-p-1, to the production reader and to the service's own FR, TR and TEST row
/// mappers and mapping builder, over entities that carry the rows' stored texts. No store, clock or environment is read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1: binds the mock-gated rows to the production reader, row mappers and mapping builder (binding ug-r9-binding-p-1).</description></item>
/// </list>
/// </remarks>
public sealed class RequirementStoredTimestampBindingTests
{
    /// <summary>The production reader and earliest-link reader satisfy every row.</summary>
    [Fact]
    public void ProductionReader_SatisfiesMockGatedRows() =>
        RequirementStoredTimestampContractTests.AssertReaders(RequirementStoredTimestamp.Read, links => RequirementStoredTimestamp.Earliest(links));

    /// <summary>The FR, TR and TEST row mappers report the stored creation and update times, and the mapping builder the
    /// earliest stored link time, for every row.</summary>
    [Fact]
    public void ServiceMappers_SatisfyMockGatedRows()
    {
        RequirementStoredTimestampContractTests.AssertReaders(
            stored => RequirementsDatabaseDocumentService.MapFr(Row("fr", "FR-X-001", stored, "")).CreatedAtUtc,
            Mapping);
        RequirementStoredTimestampContractTests.AssertReaders(
            stored => RequirementsDatabaseDocumentService.MapFr(Row("fr", "FR-X-001", "", stored)).UpdatedAtUtc,
            Mapping);
        RequirementStoredTimestampContractTests.AssertReaders(
            stored => RequirementsDatabaseDocumentService.MapTr(Row("tr", "TR-X-001", stored, "")).CreatedAtUtc,
            Mapping);
        RequirementStoredTimestampContractTests.AssertReaders(
            stored => RequirementsDatabaseDocumentService.MapTest(Row("test", "TEST-X-001", "", stored)).UpdatedAtUtc,
            Mapping);
    }

    private static DateTimeOffset? Mapping(IReadOnlyList<string> links) =>
        RequirementsDatabaseDocumentService.BuildMapping(
            "FR-X-001",
            links.Select((created, index) => new RequirementTraceabilityLinkEntity
            {
                WorkspaceId = "workspace",
                FrId = "FR-X-001",
                TargetKind = index % 2 == 0 ? "tr" : "test",
                TargetId = "X-" + index,
                CreatedAtUtc = created,
            }).ToArray(),
            "workspace").CreatedAtUtc;

    private static RequirementEntity Row(string kind, string id, string created, string updated) => new()
    {
        WorkspaceId = "workspace",
        Kind = kind,
        Id = id,
        Title = "title",
        Body = "body",
        CreatedAtUtc = created,
        UpdatedAtUtc = updated,
    };
}
