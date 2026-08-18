using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Products.Commands;

/// <summary>FR-MCP-PRODUCT-001 / TR-MCP-PRODUCT-API-001: Create a product owned by the caller workspace.</summary>
/// <param name="WorkspacePath">Caller workspace path.</param>
/// <param name="Request">Create payload.</param>
public sealed record CreateProductCommand(string WorkspacePath, CreateProductRequest Request)
    : ICommand<ProductDto>;

/// <summary>FR-MCP-PRODUCT-001: Handles <see cref="CreateProductCommand"/>.</summary>
public sealed class CreateProductCommandHandler(McpDbContext db) : ICommandHandler<CreateProductCommand, ProductDto>
{
    /// <inheritdoc />
    public async Task<Result<ProductDto>> HandleAsync(CreateProductCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);

        try
        {
            var caller = ProductCqrsHelpers.ResolveCaller(command.WorkspacePath);
            var keyError = ProductCqrsHelpers.ValidateKey(command.Request.Key, out var key);
            if (keyError is not null)
                return Result<ProductDto>.Failure(keyError);

            var nameError = ProductCqrsHelpers.ValidateName(command.Request.Name, out var name);
            if (nameError is not null)
                return Result<ProductDto>.Failure(nameError);

            var workspaceError = await ProductCqrsHelpers
                .RequireRegisteredEnabledWorkspaceAsync(db, caller, context.CancellationToken)
                .ConfigureAwait(false);
            if (workspaceError is not null)
                return Result<ProductDto>.Failure(workspaceError);

            var existing = await ProductCqrsHelpers
                .LoadProductByKeyAsync(db, key, context.CancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return Result<ProductDto>.Failure(ProductResultCodes.ConflictMsg("Product key already exists."));

            var now = DateTimeOffset.UtcNow;
            var product = new ProductEntity
            {
                Key = key,
                Name = name,
                Description = string.IsNullOrWhiteSpace(command.Request.Description)
                    ? null
                    : command.Request.Description.Trim(),
                OwnerWorkspaceId = caller,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            product.Memberships.Add(new ProductWorkspaceMembershipEntity
            {
                WorkspaceId = caller,
                Role = ProductCqrsHelpers.RoleOwner,
                AddedAtUtc = now,
                AddedBy = caller,
            });

            db.Products.Add(product);
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<ProductDto>.Success(ProductCqrsHelpers.ToDto(db, product));
        }
        catch (Exception ex)
        {
            return Result<ProductDto>.Failure(ex.Message, ex);
        }
    }
}
