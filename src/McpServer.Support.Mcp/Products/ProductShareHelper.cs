using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Products;

/// <summary>
/// TR-MCP-PRODUCT-SHARE-001: Handler-owned share query. Unions local effective rows with
/// sibling member workspaces. No public service facade.
/// </summary>
internal static class ProductShareHelper
{
    private const string FrKind = "fr";
    private const string TrKind = "tr";
    private const string TestKind = "test";

    /// <summary>Builds the effective set for the caller using <paramref name="productScope"/>.</summary>
    public static async Task<EffectiveRequirementsResult> GetEffectiveAsync(
        McpDbContext db,
        string callerWorkspaceId,
        string? layerKey,
        string productScope,
        CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.WorkspaceId == callerWorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        var resolvedLayerKey = string.IsNullOrWhiteSpace(layerKey)
            ? workspace?.CurrentRequirementLayerKey ?? RequirementScopeLayerDefaults.DefaultLayerKey
            : layerKey.Trim();

        var localLayers = await LoadLayersAsync(db, callerWorkspaceId, cancellationToken).ConfigureAwait(false);
        var currentLayer = localLayers.FirstOrDefault(l =>
            string.Equals(l.Key, resolvedLayerKey, StringComparison.OrdinalIgnoreCase))
            ?? localLayers.OrderBy(l => l.Order).FirstOrDefault()
            ?? new RequirementScopeLayerEntry(resolvedLayerKey, 1, resolvedLayerKey, WorkspaceId: callerWorkspaceId);

        var functional = new List<FrEntry>();
        var technical = new List<TrEntry>();
        var testing = new List<TestEntry>();
        var mappings = new List<FrTrMapping>();

        await AddWorkspaceEffectiveAsync(
                db,
                callerWorkspaceId,
                resolvedLayerKey,
                localLayers,
                functional,
                technical,
                testing,
                mappings,
                cancellationToken)
            .ConfigureAwait(false);

        var productKeys = new List<string>();
        var scope = productScope.Trim();
        if (scope.Equals("product", StringComparison.OrdinalIgnoreCase))
        {
            var memberships = await db.ProductWorkspaceMemberships
                .Include(m => m.Product)
                .ThenInclude(p => p!.Memberships)
                .Where(m => m.WorkspaceId == callerWorkspaceId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var products = memberships
                .Where(m => m.Product is not null
                    && !ProductCqrsHelpers.IsSoftDeleted(db, m)
                    && !ProductCqrsHelpers.IsSoftDeleted(db, m.Product))
                .Select(m => m.Product!)
                .DistinctBy(p => p.ProductId)
                .ToArray();

            productKeys.AddRange(products.Select(p => p.Key).Distinct(StringComparer.OrdinalIgnoreCase));

            var siblingIds = products
                .SelectMany(p => p.Memberships)
                .Where(m => !ProductCqrsHelpers.IsSoftDeleted(db, m)
                    && !string.Equals(m.WorkspaceId, callerWorkspaceId, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.WorkspaceId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var sibling in siblingIds)
            {
                var originLayers = await LoadLayersAsync(db, sibling, cancellationToken).ConfigureAwait(false);
                if (!originLayers.Any(l => string.Equals(l.Key, resolvedLayerKey, StringComparison.OrdinalIgnoreCase)))
                    continue;

                await AddWorkspaceEffectiveAsync(
                        db,
                        sibling,
                        resolvedLayerKey,
                        originLayers,
                        functional,
                        technical,
                        testing,
                        mappings,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return new EffectiveRequirementsResult(
            currentLayer,
            functional,
            technical,
            testing,
            mappings,
            productKeys.Count == 0 ? null : productKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static async Task<IReadOnlyList<RequirementScopeLayerEntry>> LoadLayersAsync(
        McpDbContext db,
        string workspaceId,
        CancellationToken cancellationToken)
    {
        using (db.PushWorkspaceId(workspaceId))
        {
            var rows = await db.RequirementScopeLayers
                .AsNoTracking()
                .OrderBy(l => l.Order)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return rows.Select(l => new RequirementScopeLayerEntry(
                    l.Key,
                    l.Order,
                    l.Name,
                    l.Description,
                    l.ScopeEndLayerKey,
                    l.WorkspaceId,
                    l.CreatedAtUtc,
                    l.UpdatedAtUtc))
                .ToArray();
        }
    }

    private static async Task AddWorkspaceEffectiveAsync(
        McpDbContext db,
        string workspaceId,
        string layerKey,
        IReadOnlyList<RequirementScopeLayerEntry> layers,
        List<FrEntry> functional,
        List<TrEntry> technical,
        List<TestEntry> testing,
        List<FrTrMapping> mappings,
        CancellationToken cancellationToken)
    {
        var layerOrders = layers.ToDictionary(x => x.Key, x => x.Order, StringComparer.OrdinalIgnoreCase);
        var layerEndOrders = layers.ToDictionary(
            x => x.Key,
            x => string.IsNullOrWhiteSpace(x.ScopeEndLayerKey) ? (int?)null : layerOrders[x.ScopeEndLayerKey!],
            StringComparer.OrdinalIgnoreCase);
        if (!layerOrders.TryGetValue(layerKey, out var currentOrder))
            return;

        List<RequirementEntity> rows;
        List<RequirementTraceabilityLinkEntity> links;
        List<RequirementAcceptanceCriterionEntity> criteria;
        using (db.PushWorkspaceId(workspaceId))
        {
            rows = await db.Requirements.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            links = await db.RequirementTraceabilityLinks.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            var requirementIds = rows.Select(r => r.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            criteria = requirementIds.Count == 0
                ? []
                : await db.RequirementAcceptanceCriteria
                    .AsNoTracking()
                    .Where(c => c.WorkspaceId == workspaceId && requirementIds.Contains(c.RequirementId))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
        }

        var criteriaLookup = criteria.ToLookup(c => (c.WorkspaceId, c.RequirementKind, c.RequirementId));
        foreach (var row in rows)
        {
            row.AcceptanceCriteria = criteriaLookup[(row.WorkspaceId, row.Kind, row.Id)]
                .OrderBy(c => c.Ordinal)
                .ToList();
        }

        var effectiveRows = rows
            .Where(row => IsRequirementEffective(row, currentOrder, layerOrders, layerEndOrders))
            .ToList();
        var effectiveKeys = effectiveRows
            .Select(row => $"{row.Kind}\0{row.Id}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        functional.AddRange(effectiveRows.Where(x => x.Kind == FrKind).Select(MapFr));
        technical.AddRange(effectiveRows.Where(x => x.Kind == TrKind).Select(MapTr));
        testing.AddRange(effectiveRows.Where(x => x.Kind == TestKind).Select(MapTest));
        mappings.AddRange(links
            .Where(link =>
                effectiveKeys.Contains($"{FrKind}\0{link.FrId}")
                && effectiveKeys.Contains($"{link.TargetKind}\0{link.TargetId}"))
            .GroupBy(link => link.FrId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new FrTrMapping(
                group.Key,
                group.Where(link => link.TargetKind == TrKind).Select(link => link.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                group.Where(link => link.TargetKind == TestKind).Select(link => link.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                workspaceId)));
    }

    private static bool IsRequirementEffective(
        RequirementEntity row,
        int currentOrder,
        IReadOnlyDictionary<string, int> layerOrders,
        IReadOnlyDictionary<string, int?> layerEndOrders)
    {
        if (!layerOrders.TryGetValue(row.ScopeStartLayerKey, out var startOrder))
            return false;
        if (startOrder > currentOrder)
            return false;

        int? effectiveEnd = null;
        if (!string.IsNullOrWhiteSpace(row.ScopeEndLayerKey))
        {
            if (!layerOrders.TryGetValue(row.ScopeEndLayerKey, out var requirementEndOrder))
                return false;
            effectiveEnd = requirementEndOrder;
        }

        if (layerEndOrders.TryGetValue(row.ScopeStartLayerKey, out var layerEndOrder) && layerEndOrder.HasValue)
            effectiveEnd = effectiveEnd.HasValue ? Math.Min(effectiveEnd.Value, layerEndOrder.Value) : layerEndOrder.Value;

        return !effectiveEnd.HasValue || effectiveEnd.Value >= currentOrder;
    }

    private static FrEntry MapFr(RequirementEntity x) =>
        new(x.Id, x.Title, x.Body, x.WorkspaceId, x.Priority, x.Status, x.Notes, ToCriterionModels(x.AcceptanceCriteria), x.ScopeStartLayerKey, x.ScopeEndLayerKey);

    private static TrEntry MapTr(RequirementEntity x) =>
        new(x.Id, x.Title, x.Body, x.WorkspaceId, x.Priority, x.Status, x.Notes, ToCriterionModels(x.AcceptanceCriteria), x.ScopeStartLayerKey, x.ScopeEndLayerKey);

    private static TestEntry MapTest(RequirementEntity x) =>
        new(x.Id, x.Body, x.WorkspaceId, x.Title, x.Priority, x.Status, x.Notes, ToCriterionModels(x.AcceptanceCriteria), x.ScopeStartLayerKey, x.ScopeEndLayerKey);

    private static IReadOnlyList<AcceptanceCriterion> ToCriterionModels(IEnumerable<RequirementAcceptanceCriterionEntity> rows) =>
        rows.OrderBy(r => r.Ordinal)
            .Select(r => new AcceptanceCriterion
            {
                Id = r.CriterionId,
                Text = r.Text,
                IsSatisfied = r.IsSatisfied,
                Evidence = r.Evidence,
            })
            .ToList();
}
