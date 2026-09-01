using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Client;

/// <summary>
/// TR-MCP-PRODUCT-API-001: Typed client for <c>/mcpserver/products</c>.
/// </summary>
/// <seealso cref="McpServerClient.Products"/>
public sealed class ProductClient : McpClientBase
{
    /// <inheritdoc />
    public ProductClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
    }

    internal ProductClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>Creates a product owned by the caller workspace.</summary>
    public Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
        => PostAsync<ProductDto>("mcpserver/products", request, cancellationToken);

    /// <summary>Lists products visible to the caller.</summary>
    public Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<ProductDto>>("mcpserver/products", cancellationToken);

    /// <summary>Gets one product by key.</summary>
    public Task<ProductDto> GetAsync(string key, CancellationToken cancellationToken = default)
        => GetAsync<ProductDto>($"mcpserver/products/{Uri.EscapeDataString(key)}", cancellationToken);

    /// <summary>Owner updates name or description.</summary>
    public Task<ProductDto> UpdateAsync(string key, UpdateProductRequest request, CancellationToken cancellationToken = default)
        => PatchAsync<ProductDto>($"mcpserver/products/{Uri.EscapeDataString(key)}", request, cancellationToken);

    /// <summary>Owner soft-deletes a product.</summary>
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => SendForStatusAsync(HttpMethod.Delete, $"mcpserver/products/{Uri.EscapeDataString(key)}", null, cancellationToken);

    /// <summary>Lists members of a product.</summary>
    public Task<ProductDto> ListMembersAsync(string key, CancellationToken cancellationToken = default)
        => GetAsync<ProductDto>($"mcpserver/products/{Uri.EscapeDataString(key)}/members", cancellationToken);

    /// <summary>Owner adds a member workspace.</summary>
    public Task<ProductDto> AddMemberAsync(string key, string workspaceId, CancellationToken cancellationToken = default)
        => PutAsync<ProductDto>(
            $"mcpserver/products/{Uri.EscapeDataString(key)}/members/{Uri.EscapeDataString(workspaceId)}",
            new { },
            cancellationToken);

    /// <summary>Owner removes a member, or a member leaves. Deserializes the DELETE body (self-leave is 404 on a later GET).</summary>
    public Task<ProductDto> RemoveMemberAsync(string key, string workspaceId, CancellationToken cancellationToken = default)
        => DeleteAsync<ProductDto>(
            $"mcpserver/products/{Uri.EscapeDataString(key)}/members/{Uri.EscapeDataString(workspaceId)}",
            cancellationToken);
}

/// <summary>Client-side product DTO.</summary>
public sealed class ProductDto
{
    /// <summary>Canonical product key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Owner workspace id.</summary>
    public string OwnerWorkspaceId { get; set; } = string.Empty;

    /// <summary>Member workspace ids including the owner.</summary>
    public IReadOnlyList<string> MemberWorkspaceIds { get; set; } = [];
}

/// <summary>Client-side create-product request.</summary>
public sealed class CreateProductRequest
{
    /// <summary>Requested key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
}

/// <summary>Client-side update-product request.</summary>
public sealed class UpdateProductRequest
{
    /// <summary>Replacement display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional replacement description.</summary>
    public string? Description { get; set; }
}
