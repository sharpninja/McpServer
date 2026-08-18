using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.Products.Queries;

/// <summary>FR-MCP-PRODUCT-005: Synthesized product-requirement context chunks.</summary>
public sealed class ProductRequirementChunkDto
{
    /// <summary>Origin workspace that owns the requirement row.</summary>
    public string OriginWorkspaceId { get; set; } = string.Empty;

    /// <summary>Requirement id.</summary>
    public string RequirementId { get; set; } = string.Empty;

    /// <summary>Requirement body text tagged with origin.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Always <c>product-requirements</c>.</summary>
    public string SourceType { get; set; } = "product-requirements";
}

/// <summary>
/// FR-MCP-PRODUCT-005 / TR-MCP-PRODUCT-CTX-001: Context chunks from product-visible requirements.
/// </summary>
/// <param name="WorkspacePath">Caller workspace path.</param>
/// <param name="Query">Optional text filter.</param>
/// <param name="SourceType">Optional source filter; <c>product-requirements</c> returns only requirement chunks.</param>
public sealed record GetProductRequirementContextQuery(
    string WorkspacePath,
    string? Query,
    string? SourceType)
    : IQuery<IReadOnlyList<ProductRequirementChunkDto>>;

/// <summary>FR-MCP-PRODUCT-005: Handles <see cref="GetProductRequirementContextQuery"/>.</summary>
public sealed class GetProductRequirementContextQueryHandler(McpDbContext db)
    : IQueryHandler<GetProductRequirementContextQuery, IReadOnlyList<ProductRequirementChunkDto>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductRequirementChunkDto>>> HandleAsync(
        GetProductRequirementContextQuery query,
        CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var caller = ProductCqrsHelpers.ResolveCaller(query.WorkspacePath);
            var source = string.IsNullOrWhiteSpace(query.SourceType)
                ? "product-requirements"
                : query.SourceType.Trim();
            if (!source.Equals("product-requirements", StringComparison.OrdinalIgnoreCase))
                return Result<IReadOnlyList<ProductRequirementChunkDto>>.Success([]);

            var effective = await ProductShareHelper
                .GetEffectiveAsync(db, caller, layerKey: null, productScope: "product", context.CancellationToken)
                .ConfigureAwait(false);

            var filter = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim();
            var chunks = new List<ProductRequirementChunkDto>();
            foreach (var fr in effective.Functional)
                AddChunk(chunks, fr.WorkspaceId, fr.Id, fr.Title, fr.Body, filter);
            foreach (var tr in effective.Technical)
                AddChunk(chunks, tr.WorkspaceId, tr.Id, tr.Title, tr.Body, filter);
            foreach (var test in effective.Testing)
                AddChunk(chunks, test.WorkspaceId, test.Id, test.Title, test.Condition, filter);

            return Result<IReadOnlyList<ProductRequirementChunkDto>>.Success(chunks);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ProductRequirementChunkDto>>.Failure(ex.Message, ex);
        }
    }

    private static void AddChunk(
        List<ProductRequirementChunkDto> chunks,
        string originWorkspaceId,
        string id,
        string title,
        string body,
        string? filter)
    {
        var content = $"[originWorkspaceId={originWorkspaceId}] {id} {title}: {body}";
        if (filter is not null
            && content.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return;
        }

        chunks.Add(new ProductRequirementChunkDto
        {
            OriginWorkspaceId = originWorkspaceId,
            RequirementId = id,
            Content = content,
            SourceType = "product-requirements",
        });
    }
}
