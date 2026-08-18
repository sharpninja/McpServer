using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Products;

/// <summary>
/// TR-MCP-PRODUCT-AUTH-001 / TR-MCP-PRODUCT-MODEL-001: Private helpers used only by product CQRS handlers.
/// </summary>
internal static class ProductCqrsHelpers
{
    /// <summary>Canonical product key format.</summary>
    public static readonly Regex ProductKeyRegex = new(
        @"^PROD-[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Owner membership role.</summary>
    public const string RoleOwner = "Owner";

    /// <summary>Ordinary membership role.</summary>
    public const string RoleMember = "Member";

    /// <summary>Validates a product key or returns a 400 error.</summary>
    public static string? ValidateKey(string? key, out string normalized)
    {
        normalized = (key ?? string.Empty).Trim();
        if (!ProductKeyRegex.IsMatch(normalized))
            return ProductResultCodes.BadRequestMsg("Product key must match PROD-* (example PROD-MCPSERVER).");

        return null;
    }

    /// <summary>Normalizes a required display name.</summary>
    public static string? ValidateName(string? name, out string normalized)
    {
        normalized = (name ?? string.Empty).Trim();
        if (normalized.Length == 0)
            return ProductResultCodes.BadRequestMsg("Product name is required.");

        return null;
    }

    /// <summary>Resolves the caller workspace id from the command path.</summary>
    public static string ResolveCaller(string? workspacePath)
    {
        var resolved = (workspacePath ?? string.Empty).Trim();
        if (resolved.Length == 0)
            throw new InvalidOperationException("Workspace path is required for product operations.");

        return resolved;
    }

    /// <summary>Requires a registered, enabled, non-deleted workspace.</summary>
    public static async Task<string?> RequireRegisteredEnabledWorkspaceAsync(
        McpDbContext db,
        string workspaceId,
        CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.WorkspaceId == workspaceId, cancellationToken)
            .ConfigureAwait(false);

        if (workspace is null || workspace.IsDeleted || !workspace.IsEnabled)
            return ProductResultCodes.BadRequestMsg("Workspace must be a registered enabled workspace.");

        return null;
    }

    /// <summary>Loads a non-deleted product by key, including memberships.</summary>
    public static Task<ProductEntity?> LoadProductByKeyAsync(
        McpDbContext db,
        string key,
        CancellationToken cancellationToken)
    {
        return db.Products
            .Include(p => p.Memberships)
            .FirstOrDefaultAsync(p => p.Key == key, cancellationToken);
    }

    /// <summary>True when a tracked entity has the soft-delete shadow flag set.</summary>
    public static bool IsSoftDeleted(McpDbContext db, object entity)
    {
        var entry = db.Entry(entity);
        if (entry.Metadata.FindProperty("IsDeleted") is null)
            return false;

        return entry.Property("IsDeleted").CurrentValue is true;
    }

    /// <summary>True when the caller is an active (non-deleted) member or owner.</summary>
    public static bool IsActiveMember(McpDbContext db, ProductEntity product, string workspaceId)
    {
        return product.Memberships.Any(m =>
            string.Equals(m.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase)
            && !IsSoftDeleted(db, m));
    }

    /// <summary>True when the caller owns the product.</summary>
    public static bool IsOwner(ProductEntity product, string workspaceId)
        => string.Equals(product.OwnerWorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Visibility/auth for a read: missing or non-member is 404 (do not leak existence).
    /// </summary>
    public static string? RequireVisible(McpDbContext db, ProductEntity? product, string caller)
    {
        if (product is null || IsSoftDeleted(db, product) || !IsActiveMember(db, product, caller))
            return ProductResultCodes.NotFoundMsg("Product was not found.");

        return null;
    }

    /// <summary>
    /// Visibility/auth for an owner mutation. Outsiders get 404; members who are not owners get 403.
    /// </summary>
    public static string? RequireOwner(McpDbContext db, ProductEntity? product, string caller)
    {
        if (product is null || IsSoftDeleted(db, product))
            return ProductResultCodes.NotFoundMsg("Product was not found.");

        if (IsOwner(product, caller))
            return null;

        if (IsActiveMember(db, product, caller))
            return ProductResultCodes.ForbiddenMsg("Only the owner may change this product.");

        return ProductResultCodes.NotFoundMsg("Product was not found.");
    }

    /// <summary>Maps a product aggregate to the public DTO.</summary>
    public static ProductDto ToDto(McpDbContext db, ProductEntity product)
    {
        var members = product.Memberships
            .Where(m => !IsSoftDeleted(db, m))
            .Select(m => m.WorkspaceId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!members.Contains(product.OwnerWorkspaceId, StringComparer.OrdinalIgnoreCase))
            members = [product.OwnerWorkspaceId, .. members];

        return new ProductDto
        {
            Key = product.Key,
            Name = product.Name,
            Description = product.Description,
            OwnerWorkspaceId = product.OwnerWorkspaceId,
            MemberWorkspaceIds = members,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc,
        };
    }

    /// <summary>Clears soft-delete shadow properties so a membership can be restored.</summary>
    public static void ClearSoftDelete(McpDbContext db, object entity)
    {
        var entry = db.Entry(entity);
        if (entry.Metadata.FindProperty("IsDeleted") is null)
            return;

        entry.State = EntityState.Modified;
        entry.Property("IsDeleted").CurrentValue = false;
        entry.Property("DeletedAtUtc").CurrentValue = null;
        entry.Property("DeletedBy").CurrentValue = null;
        entry.Property("DeleteReason").CurrentValue = null;
    }
}
