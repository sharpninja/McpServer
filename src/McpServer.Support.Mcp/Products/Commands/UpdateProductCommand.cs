using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.Products.Commands;

/// <summary>FR-MCP-PRODUCT-001: Owner updates product name or description.</summary>
/// <param name="WorkspacePath">Caller workspace path (must be owner).</param>
/// <param name="ProductKey">Product key.</param>
/// <param name="Request">Update payload.</param>
public sealed record UpdateProductCommand(string WorkspacePath, string ProductKey, UpdateProductRequest Request)
    : ICommand<ProductDto>;

/// <summary>FR-MCP-PRODUCT-001: Handles <see cref="UpdateProductCommand"/>.</summary>
public sealed class UpdateProductCommandHandler(McpDbContext db) : ICommandHandler<UpdateProductCommand, ProductDto>
{
    /// <inheritdoc />
    public async Task<Result<ProductDto>> HandleAsync(UpdateProductCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            var caller = ProductCqrsHelpers.ResolveCaller(command.WorkspacePath);
            var keyError = ProductCqrsHelpers.ValidateKey(command.ProductKey, out var key);
            if (keyError is not null)
                return Result<ProductDto>.Failure(keyError);

            var nameError = ProductCqrsHelpers.ValidateName(command.Request.Name, out var name);
            if (nameError is not null)
                return Result<ProductDto>.Failure(nameError);

            var product = await ProductCqrsHelpers
                .LoadProductByKeyAsync(db, key, context.CancellationToken)
                .ConfigureAwait(false);
            var auth = ProductCqrsHelpers.RequireOwner(db, product, caller);
            if (auth is not null)
                return Result<ProductDto>.Failure(auth);

            product!.Name = name;
            product.Description = string.IsNullOrWhiteSpace(command.Request.Description)
                ? null
                : command.Request.Description.Trim();
            product.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<ProductDto>.Success(ProductCqrsHelpers.ToDto(db, product));
        }
        catch (Exception ex)
        {
            return Result<ProductDto>.Failure(ex.Message, ex);
        }
    }
}
