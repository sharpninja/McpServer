using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.Products.Commands;

/// <summary>FR-MCP-PRODUCT-001: Owner soft-deletes a product.</summary>
/// <param name="WorkspacePath">Caller workspace path (must be owner).</param>
/// <param name="ProductKey">Product key to soft-delete.</param>
public sealed record DeleteProductCommand(string WorkspacePath, string ProductKey)
    : ICommand<ProductDto>;

/// <summary>FR-MCP-PRODUCT-001: Handles <see cref="DeleteProductCommand"/>.</summary>
public sealed class DeleteProductCommandHandler(McpDbContext db) : ICommandHandler<DeleteProductCommand, ProductDto>
{
    /// <inheritdoc />
    public async Task<Result<ProductDto>> HandleAsync(DeleteProductCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var caller = ProductCqrsHelpers.ResolveCaller(command.WorkspacePath);
            var keyError = ProductCqrsHelpers.ValidateKey(command.ProductKey, out var key);
            if (keyError is not null)
                return Result<ProductDto>.Failure(keyError);

            var product = await ProductCqrsHelpers
                .LoadProductByKeyAsync(db, key, context.CancellationToken)
                .ConfigureAwait(false);
            var auth = ProductCqrsHelpers.RequireOwner(db, product, caller);
            if (auth is not null)
                return Result<ProductDto>.Failure(auth);

            var dto = ProductCqrsHelpers.ToDto(db, product!);
            foreach (var membership in product!.Memberships.ToList())
                db.ProductWorkspaceMemberships.Remove(membership);

            db.Products.Remove(product);
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<ProductDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<ProductDto>.Failure(ex.Message, ex);
        }
    }
}
