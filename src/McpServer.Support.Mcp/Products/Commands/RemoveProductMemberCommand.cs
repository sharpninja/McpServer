using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.Products.Commands;

/// <summary>FR-MCP-PRODUCT-002: Owner removes a member, or a member leaves (self).</summary>
/// <param name="WorkspacePath">Caller workspace path.</param>
/// <param name="ProductKey">Product key.</param>
/// <param name="MemberWorkspaceId">Workspace id to remove (caller id when leaving).</param>
public sealed record RemoveProductMemberCommand(string WorkspacePath, string ProductKey, string MemberWorkspaceId)
    : ICommand<ProductDto>;

/// <summary>FR-MCP-PRODUCT-002: Handles <see cref="RemoveProductMemberCommand"/>.</summary>
public sealed class RemoveProductMemberCommandHandler(McpDbContext db)
    : ICommandHandler<RemoveProductMemberCommand, ProductDto>
{
    /// <inheritdoc />
    public async Task<Result<ProductDto>> HandleAsync(RemoveProductMemberCommand command, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var caller = ProductCqrsHelpers.ResolveCaller(command.WorkspacePath);
            var keyError = ProductCqrsHelpers.ValidateKey(command.ProductKey, out var key);
            if (keyError is not null)
                return Result<ProductDto>.Failure(keyError);

            var target = (command.MemberWorkspaceId ?? string.Empty).Trim();
            if (target.Length == 0)
                return Result<ProductDto>.Failure(ProductResultCodes.BadRequestMsg("Member workspace id is required."));

            var product = await ProductCqrsHelpers
                .LoadProductByKeyAsync(db, key, context.CancellationToken)
                .ConfigureAwait(false);
            var visible = ProductCqrsHelpers.RequireVisible(db, product, caller);
            if (visible is not null)
                return Result<ProductDto>.Failure(visible);

            var isOwner = ProductCqrsHelpers.IsOwner(product!, caller);
            var isSelfLeave = string.Equals(caller, target, StringComparison.OrdinalIgnoreCase);
            if (!isOwner && !isSelfLeave)
                return Result<ProductDto>.Failure(ProductResultCodes.ForbiddenMsg("A member may leave only itself."));

            if (isOwner && isSelfLeave)
                return Result<ProductDto>.Failure(ProductResultCodes.ForbiddenMsg("Owner cannot leave; delete the product instead."));

            var membership = product!.Memberships.FirstOrDefault(m =>
                string.Equals(m.WorkspaceId, target, StringComparison.OrdinalIgnoreCase));
            if (membership is null)
                return Result<ProductDto>.Failure(ProductResultCodes.NotFoundMsg("Membership was not found."));

            db.ProductWorkspaceMemberships.Remove(membership);
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
