using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-138, TEST-MCP-139, TEST-MCP-140: Red contract tests for DB-FK-001
/// database-authoritative workspaces, workspace foreign keys, soft deletes, audit
/// ledger coverage, TODO requirement links, and requirement traceability FKs.
/// </summary>
public sealed class DbFkContractTests
{
    /// <summary>
    /// TEST-MCP-138: The EF model must contain a canonical <c>Workspaces</c>
    /// entity because configured appsettings workspaces are only a projection.
    /// </summary>
    [Fact]
    public void DBFK_WorkspacesEntity_IsCanonicalModelRoot()
    {
        using var db = CreateContext();

        var workspaceEntity = FindEntity(db.Model, "WorkspaceEntity", "Workspaces");

        Assert.NotNull(workspaceEntity);
        Assert.Contains(
            workspaceEntity!.GetProperties(),
            p => p.Name is "WorkspaceId" or "WorkspacePath");
    }

    /// <summary>
    /// TEST-MCP-138: Every persistent entity that owns a <c>WorkspaceId</c>
    /// property must have a required FK to canonical <c>Workspaces</c> with
    /// non-cascading delete behavior.
    /// </summary>
    [Fact]
    public void DBFK_WorkspaceIdEntities_HaveRequiredWorkspacesForeignKeys()
    {
        using var db = CreateContext();

        var failures = db.Model.GetEntityTypes()
            .Where(e => e.FindProperty("WorkspaceId") is not null)
            .Where(e => !IsWorkspacesPrincipal(e))
            .Select(e => new
            {
                Entity = e,
                ForeignKey = e.GetForeignKeys().SingleOrDefault(fk =>
                    fk.Properties.Any(p => p.Name == "WorkspaceId")
                    && IsWorkspacesPrincipal(fk.PrincipalEntityType)),
            })
            .Where(x => x.ForeignKey is null
                || !x.ForeignKey.IsRequired
                || x.ForeignKey.DeleteBehavior is not (DeleteBehavior.Restrict or DeleteBehavior.NoAction))
            .Select(x => x.ForeignKey is null
                ? $"{x.Entity.ClrType.Name}: missing Workspaces FK"
                : $"{x.Entity.ClrType.Name}: FK required={x.ForeignKey.IsRequired}, delete={x.ForeignKey.DeleteBehavior}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            failures.Length == 0,
            "WorkspaceId FK contract failures: " + string.Join("; ", failures));
    }

    /// <summary>
    /// TEST-MCP-139: Durable domain tables must expose common soft-delete metadata
    /// so delete operations preserve rows for recovery and audit.
    /// </summary>
    [Fact]
    public void DBFK_DurableEntities_ExposeSoftDeleteMetadata()
    {
        using var db = CreateContext();

        var failures = DurableEntityTypes(db.Model)
            .Where(e => e.FindProperty("IsDeleted") is null
                || e.FindProperty("DeletedAtUtc") is null
                || e.FindProperty("DeletedBy") is null)
            .Select(e => $"{e.ClrType.Name}: missing IsDeleted, DeletedAtUtc, or DeletedBy")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            failures.Length == 0,
            "Soft-delete contract failures: " + string.Join("; ", failures));
    }

    /// <summary>
    /// TEST-MCP-139: Durable MCP state must not use database cascade deletes.
    /// Explicit soft-delete propagation is required when child rows should be
    /// hidden with a parent.
    /// </summary>
    [Fact]
    public void DBFK_DurableRelationships_DoNotCascadeDelete()
    {
        using var db = CreateContext();

        var failures = db.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => IsDurableEntity(fk.DeclaringEntityType) && IsDurableEntity(fk.PrincipalEntityType))
            .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade)
            .Select(fk => $"{fk.DeclaringEntityType.ClrType.Name} -> {fk.PrincipalEntityType.ClrType.Name}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            failures.Length == 0,
            "Durable cascade delete contract failures: " + string.Join("; ", failures));
    }

    /// <summary>
    /// TEST-MCP-139: The generic append-only audit ledger must exist and capture
    /// enough metadata for every mutable database entity mutation.
    /// </summary>
    [Fact]
    public void DBFK_GenericAuditLedger_ExistsWithRequiredColumns()
    {
        using var db = CreateContext();

        var auditEntity = FindEntity(db.Model, "DataAuditLogEntity", "DataAuditLogs");

        Assert.NotNull(auditEntity);

        var required = new[]
        {
            "AuditId",
            "WorkspaceId",
            "EntityKind",
            "EntityKey",
            "Action",
            "Actor",
            "SourceType",
            "RequestId",
            "CorrelationId",
            "FederationOperationId",
            "OccurredAtUtc",
            "PreviousSnapshotJson",
            "CurrentSnapshotJson",
            "DiffJson",
            "MetadataJson",
        };

        var missing = required
            .Where(name => auditEntity!.FindProperty(name) is null)
            .ToArray();

        Assert.Empty(missing);
    }

    /// <summary>
    /// TEST-MCP-140: TODO requirement assignments must be relational link rows with
    /// FKs to the durable TODO anchor and the requirements table; JSON fields are
    /// only compatibility projections.
    /// </summary>
    [Fact]
    public void DBFK_TodoRequirementLinks_AreRelationalAndForeignKeyed()
    {
        using var db = CreateContext();

        var linkEntity = FindEntity(db.Model, "TodoRequirementLinkEntity", "TodoRequirementLinks");

        Assert.NotNull(linkEntity);
        Assert.Contains(linkEntity!.GetForeignKeys(), fk => IsTodoPrincipal(fk.PrincipalEntityType));
        Assert.Contains(linkEntity.GetForeignKeys(), fk => IsRequirementPrincipal(fk.PrincipalEntityType));
    }

    /// <summary>
    /// TEST-MCP-140: Requirement traceability links must reject orphan FR, TR, and
    /// TEST references by using FKs to the requirements table.
    /// </summary>
    [Fact]
    public void DBFK_RequirementTraceabilityLinks_AreForeignKeyedToRequirements()
    {
        using var db = CreateContext();

        var linkEntity = FindEntity(db.Model, "RequirementTraceabilityLinkEntity", "RequirementTraceabilityLinks");

        Assert.NotNull(linkEntity);

        var requirementFks = linkEntity!.GetForeignKeys()
            .Where(fk => IsRequirementPrincipal(fk.PrincipalEntityType))
            .ToArray();

        Assert.True(
            requirementFks.Length >= 2,
            $"RequirementTraceabilityLinkEntity must have source and target requirement FKs; found {requirementFks.Length}.");
    }

    /// <summary>
    /// TEST-MCP-138: Federation workspace rows must preserve proxy/global IDs
    /// while also linking to the canonical workspace registry.
    /// </summary>
    [Fact]
    public void DBFK_FederationWorkspaces_LinkToCanonicalWorkspaces()
    {
        using var db = CreateContext();

        var federationEntity = FindEntity(db.Model, "FederationWorkspaceEntity", "FederationWorkspaces");

        Assert.NotNull(federationEntity);
        Assert.Contains(federationEntity!.GetProperties(), p => p.Name == "CanonicalWorkspaceId");

        var workspaceFks = federationEntity.GetForeignKeys()
            .Where(fk => IsWorkspacesPrincipal(fk.PrincipalEntityType))
            .ToArray();

        Assert.Contains(workspaceFks, fk =>
            fk.Properties.Any(p => p.Name == "CanonicalWorkspaceId")
            && fk.DeleteBehavior is DeleteBehavior.Restrict or DeleteBehavior.NoAction);
    }

    private static McpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"dbfk-contract-{Guid.NewGuid():N}")
            .Options;

        return new McpDbContext(options);
    }

    private static IEntityType? FindEntity(IModel model, string clrTypeName, string tableName)
    {
        return model.GetEntityTypes().SingleOrDefault(e =>
            string.Equals(e.ClrType.Name, clrTypeName, StringComparison.Ordinal)
            || string.Equals(e.GetTableName(), tableName, StringComparison.Ordinal));
    }

    private static IEnumerable<IEntityType> DurableEntityTypes(IModel model)
    {
        return model.GetEntityTypes()
            .Where(e => e.ClrType.Name != "DataAuditLogEntity")
            .Where(e => e.GetTableName() is not null)
            .Where(e => e.ClrType.Name.EndsWith("Entity", StringComparison.Ordinal));
    }

    private static bool IsDurableEntity(IEntityType entityType)
    {
        return entityType.ClrType.Name != "DataAuditLogEntity"
            && entityType.GetTableName() is not null
            && entityType.ClrType.Name.EndsWith("Entity", StringComparison.Ordinal);
    }

    private static bool IsWorkspacesPrincipal(IEntityType entityType)
    {
        return entityType.ClrType.Name == "WorkspaceEntity"
            || entityType.GetTableName() == "Workspaces";
    }

    private static bool IsTodoPrincipal(IEntityType entityType)
    {
        return entityType.ClrType.Name is "TodoRecordEntity" or "TodoItemEntity"
            || entityType.GetTableName() is "TodoRecords" or "TodoItems";
    }

    private static bool IsRequirementPrincipal(IEntityType entityType)
    {
        return entityType.ClrType.Name == "RequirementEntity"
            || entityType.GetTableName() == "Requirements";
    }
}
