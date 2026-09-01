using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Products.Queries;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

/// <summary>TR-MCP-PRODUCT-API-001: MCP tools that dispatch product CQRS handlers.</summary>
public sealed partial class FwhMcpTools
{
    /// <summary>FR-MCP-PRODUCT-001: Create a product.</summary>
    [McpServerTool(Name = "product_create"), Description("Create a product owned by the caller workspace.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ProductCreate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Product key such as PROD-MCPSERVER")] string key,
        [Description("Display name")] string name,
        [Description("Optional description")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _dispatcher.SendAsync(
            new CreateProductCommand(workspacePath, new CreateProductRequest
            {
                Key = key,
                Name = name,
                Description = description,
            }),
            cancellationToken).ConfigureAwait(false);
        return SerializeResult(result);
    }

    /// <summary>FR-MCP-PRODUCT-001: List visible products.</summary>
    [McpServerTool(Name = "product_list"), Description("List products the caller owns or belongs to.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ProductList(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _dispatcher.QueryAsync(new ListProductsQuery(workspacePath), cancellationToken).ConfigureAwait(false);
        return SerializeResult(result);
    }

    /// <summary>FR-MCP-PRODUCT-001: Get one product.</summary>
    [McpServerTool(Name = "product_get"), Description("Get one product visible to the caller.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ProductGet(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Product key")] string key,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _dispatcher.QueryAsync(new GetProductQuery(workspacePath, key), cancellationToken).ConfigureAwait(false);
        return SerializeResult(result);
    }

    /// <summary>FR-MCP-PRODUCT-001: Update a product.</summary>
    [McpServerTool(Name = "product_update"), Description("Owner updates product name or description.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ProductUpdate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Product key")] string key,
        [Description("Display name")] string name,
        [Description("Optional description")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _dispatcher.SendAsync(
            new UpdateProductCommand(workspacePath, key, new UpdateProductRequest { Name = name, Description = description }),
            cancellationToken).ConfigureAwait(false);
        return SerializeResult(result);
    }

    /// <summary>FR-MCP-PRODUCT-001: Soft-delete a product.</summary>
    [McpServerTool(Name = "product_delete"), Description("Owner soft-deletes a product.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ProductDelete(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Product key")] string key,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _dispatcher.SendAsync(
            new DeleteProductCommand(workspacePath, key),
            cancellationToken).ConfigureAwait(false);
        return SerializeResult(result);
    }

    /// <summary>FR-MCP-PRODUCT-002: List members.</summary>
    [McpServerTool(Name = "product_list_members"), Description("List members of a product.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ProductListMembers(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Product key")] string key,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _dispatcher.QueryAsync(
            new ListProductMembersQuery(workspacePath, key),
            cancellationToken).ConfigureAwait(false);
        return SerializeResult(result);
    }

    /// <summary>FR-MCP-PRODUCT-002: Add a member.</summary>
    [McpServerTool(Name = "product_add_member"), Description("Owner adds a registered workspace as a product member.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ProductAddMember(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Product key")] string key,
        [Description("Member workspace id")] string memberWorkspaceId,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _dispatcher.SendAsync(
            new AddProductMemberCommand(workspacePath, key, memberWorkspaceId),
            cancellationToken).ConfigureAwait(false);
        return SerializeResult(result);
    }

    /// <summary>FR-MCP-PRODUCT-002: Remove a member or self-leave.</summary>
    [McpServerTool(Name = "product_remove_member"), Description("Owner removes a member, or a member leaves itself.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ProductRemoveMember(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Product key")] string key,
        [Description("Member workspace id to remove")] string memberWorkspaceId,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _dispatcher.SendAsync(
            new RemoveProductMemberCommand(workspacePath, key, memberWorkspaceId),
            cancellationToken).ConfigureAwait(false);
        return SerializeResult(result);
    }
}
