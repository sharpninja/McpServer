using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Products.Queries;

/// <summary>FR-MCP-PRODUCT-001: List products visible to the caller (owner or member).</summary>
/// <param name="WorkspacePath">Caller workspace path.</param>
public sealed record ListProductsQuery(string WorkspacePath)
    : IQuery<IReadOnlyList<ProductDto>>;

/// <summary>FR-MCP-PRODUCT-001: Handles <see cref="ListProductsQuery"/>.</summary>
public sealed class ListProductsQueryHandler(McpDbContext db)
    : IQueryHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProductDto>>> HandleAsync(ListProductsQuery query, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var caller = ProductCqrsHelpers.ResolveCaller(query.WorkspacePath);
            var products = await db.Products
                .Include(p => p.Memberships)
                .Where(p => p.OwnerWorkspaceId == caller || p.Memberships.Any(m => m.WorkspaceId == caller))
                .OrderBy(p => p.Key)
                .ToListAsync(context.CancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<ProductDto> dtos = products.Select(p => ProductCqrsHelpers.ToDto(db, p)).ToArray();
            return Result<IReadOnlyList<ProductDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ProductDto>>.Failure(ex.Message, ex);
        }
    }
}
