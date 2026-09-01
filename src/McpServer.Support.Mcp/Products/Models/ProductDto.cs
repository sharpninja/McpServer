namespace McpServer.Support.Mcp.Products.Models;

/// <summary>FR-MCP-PRODUCT-001: Product view returned by CQRS handlers.</summary>
public sealed class ProductDto
{
    /// <summary>Canonical product key (example <c>PROD-MCPSERVER</c>).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Owner workspace id.</summary>
    public string OwnerWorkspaceId { get; set; } = string.Empty;

    /// <summary>Member workspace ids including the owner.</summary>
    public IReadOnlyList<string> MemberWorkspaceIds { get; set; } = [];

    /// <summary>UTC create timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC update timestamp.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>FR-MCP-PRODUCT-001: Create-product payload.</summary>
public sealed class CreateProductRequest
{
    /// <summary>Requested key (must match <c>PROD-*</c>).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
}

/// <summary>FR-MCP-PRODUCT-001: Owner update payload.</summary>
public sealed class UpdateProductRequest
{
    /// <summary>Replacement display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional replacement description.</summary>
    public string? Description { get; set; }
}
