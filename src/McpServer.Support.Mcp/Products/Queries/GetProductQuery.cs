using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.Products.Queries;

/// <summary>FR-MCP-PRODUCT-004: Get one product visible to the caller.</summary>
/// <param name="WorkspacePath">Caller workspace path.</param>
/// <param name="ProductKey">Product key.</param>
public sealed record GetProductQuery(string WorkspacePath, string ProductKey)
    : IQuery<ProductDto>;

/// <summary>FR-MCP-PRODUCT-004: Handles <see cref="GetProductQuery"/>.</summary>
public sealed class GetProductQueryHandler(McpDbContext db) : IQueryHandler<GetProductQuery, ProductDto>
{
    /// <inheritdoc />
    public async Task<Result<ProductDto>> HandleAsync(GetProductQuery query, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var caller = ProductCqrsHelpers.ResolveCaller(query.WorkspacePath);
            var keyError = ProductCqrsHelpers.ValidateKey(query.ProductKey, out var key);
            if (keyError is not null)
                return Result<ProductDto>.Failure(keyError);

            var product = await ProductCqrsHelpers
                .LoadProductByKeyAsync(db, key, context.CancellationToken)
                .ConfigureAwait(false);
            var visible = ProductCqrsHelpers.RequireVisible(db, product, caller);
            if (visible is not null)
                return Result<ProductDto>.Failure(visible);

            return Result<ProductDto>.Success(ProductCqrsHelpers.ToDto(db, product!));
        }
        catch (Exception ex)
        {
            return Result<ProductDto>.Failure(ex.Message, ex);
        }
    }
}
