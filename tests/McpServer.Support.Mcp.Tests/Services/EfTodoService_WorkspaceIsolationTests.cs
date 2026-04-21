using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TODO-008 Phase 3 acceptance: <see cref="McpServer.Support.Mcp.Services.EfTodoService"/>
/// MUST honor the active workspace resolved by <c>WorkspaceAuthMiddleware</c>
/// (TR-MCP-MT-003). Reads, updates, and deletes issued under one workspace
/// MUST NOT observe or mutate rows owned by another workspace, and the same
/// canonical TODO id MAY coexist across two workspaces under the composite
/// <c>(WorkspaceId, Id)</c> primary key.
/// </summary>
/// <remarks>
/// Byrd-process failing stubs; each test flips to green once Phase 1 (entity +
/// DbContext changes) and Phase 3 (duplicate-id scoping) land. The
/// <c>PLAN-BITNETINTEGRATION-001</c> case is the live-workspace collision
/// observed between <c>bitnet-b1.58-sharp</c> and <c>TruckMate</c>: both carry
/// a plan-TODO with that id today, so the implementation must accept them as
/// two logically independent rows keyed on <c>(WorkspaceId, Id)</c>.
/// </remarks>
public sealed class EfTodoService_WorkspaceIsolationTests
{
    /// <summary>
    /// <c>GET</c>-shape queries scoped to workspace A MUST return only A's
    /// rows even when B owns rows with overlapping or disjoint ids.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 3 pending: workspace isolation not yet wired")]
    public void QueryAsync_WorkspaceA_DoesNotReturnWorkspaceBRows()
        => Assert.Fail("TR-MCP-TODO-008 Phase 3 not implemented");

    /// <summary>
    /// <c>DELETE</c> issued under workspace A MUST NOT remove a row with the
    /// same canonical id that lives in workspace B.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 3 pending: workspace-scoped delete not yet wired")]
    public void DeleteAsync_WorkspaceA_LeavesMatchingIdInWorkspaceBIntact()
        => Assert.Fail("TR-MCP-TODO-008 Phase 3 not implemented");

    /// <summary>
    /// The duplicate-id check in <c>CreateAsync</c> MUST be scoped to the
    /// active workspace so the same id can be created in two workspaces.
    /// Empirically required: <c>PLAN-BITNETINTEGRATION-001</c> exists in both
    /// <c>bitnet-b1.58-sharp</c> and <c>TruckMate</c> YAML sources today.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 3 pending: duplicate-id check not yet scoped")]
    public void CreateAsync_SameIdInTwoWorkspaces_BothSucceed_PlanBitNetIntegrationCase()
        => Assert.Fail("TR-MCP-TODO-008 Phase 3 not implemented");

    /// <summary>
    /// Audit history lookups scoped to workspace A MUST NOT surface audit
    /// rows recorded under workspace B, even when the underlying
    /// <c>(TodoId, Version)</c> pair collides.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 3 pending: audit query filter not yet installed")]
    public void GetAuditAsync_WorkspaceA_DoesNotLeakWorkspaceBAuditRows()
        => Assert.Fail("TR-MCP-TODO-008 Phase 3 not implemented");

    /// <summary>
    /// <see cref="McpServer.Support.Mcp.Services.LegacyTodoSqliteMigrator"/>
    /// MUST stamp imported rows with the active workspace's path; rows
    /// imported without a workspace context MUST NOT show up in any scoped
    /// query.
    /// </summary>
    [Fact(Skip = "TR-MCP-TODO-008 Phase 3 pending: legacy migrator not yet stamping WorkspaceId")]
    public void LegacyMigrator_StampsWorkspaceIdOnImportedRows()
        => Assert.Fail("TR-MCP-TODO-008 Phase 3 not implemented");
}
