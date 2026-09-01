using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Products.Commands;

/// <summary>FR-MCP-PRODUCT-002 / TR-MCP-PRODUCT-AUTH-001: Owner adds a member workspace.</summary>
/// <param name="WorkspacePath">Caller workspace path (must be owner).</param>
/// <param name="ProductKey">Product key.</param>
/// <param name="MemberWorkspaceId">Workspace id to add.</param>
public sealed record AddProductMemberCommand(string WorkspacePath, string ProductKey, string MemberWorkspaceId)
    : ICommand<ProductDto>;

/// <summary>FR-MCP-PRODUCT-002: Handles <see cref="AddProductMemberCommand"/>.</summary>
public sealed class AddProductMemberCommandHandler(McpDbContext db) : ICommandHandler<AddProductMemberCommand, ProductDto>
{
    /// <inheritdoc />
    public async Task<Result<ProductDto>> HandleAsync(AddProductMemberCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var caller = ProductCqrsHelpers.ResolveCaller(command.WorkspacePath);
            var keyError = ProductCqrsHelpers.ValidateKey(command.ProductKey, out var key);
            if (keyError is not null)
                return Result<ProductDto>.Failure(keyError);

            var memberId = (command.MemberWorkspaceId ?? string.Empty).Trim();
            if (memberId.Length == 0)
                return Result<ProductDto>.Failure(ProductResultCodes.BadRequestMsg("Member workspace id is required."));

            var product = await ProductCqrsHelpers
                .LoadProductByKeyAsync(db, key, context.CancellationToken)
                .ConfigureAwait(false);
            var auth = ProductCqrsHelpers.RequireOwner(db, product, caller);
            if (auth is not null)
                return Result<ProductDto>.Failure(auth);

            var workspaceError = await ProductCqrsHelpers
                .RequireRegisteredEnabledWorkspaceAsync(db, memberId, context.CancellationToken)
                .ConfigureAwait(false);
            if (workspaceError is not null)
                return Result<ProductDto>.Failure(workspaceError);

            var existing = product!.Memberships.FirstOrDefault(m =>
                string.Equals(m.WorkspaceId, memberId, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                var hidden = await db.ProductWorkspaceMemberships
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(
                        m => m.ProductId == product.ProductId && m.WorkspaceId == memberId,
                        context.CancellationToken)
                    .ConfigureAwait(false);
                if (hidden is not null)
                {
                    ProductCqrsHelpers.ClearSoftDelete(db, hidden);
                    hidden.Role = ProductCqrsHelpers.RoleMember;
                    hidden.AddedAtUtc = DateTimeOffset.UtcNow;
                    hidden.AddedBy = caller;
                    product.Memberships.Add(hidden);
                }
                else
                {
                    product.Memberships.Add(new ProductWorkspaceMembershipEntity
                    {
                        ProductId = product.ProductId,
                        WorkspaceId = memberId,
                        Role = ProductCqrsHelpers.RoleMember,
                        AddedAtUtc = DateTimeOffset.UtcNow,
                        AddedBy = caller,
                    });
                }
            }

            product.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            var reloaded = await ProductCqrsHelpers
                .LoadProductByKeyAsync(db, key, context.CancellationToken)
                .ConfigureAwait(false);
            return Result<ProductDto>.Success(ProductCqrsHelpers.ToDto(db, reloaded!));
        }
        catch (Exception ex)
        {
            return Result<ProductDto>.Failure(ex.Message, ex);
        }
    }
}
