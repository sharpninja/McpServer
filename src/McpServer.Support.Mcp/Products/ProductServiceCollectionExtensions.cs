using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Products.Queries;
using McpServer.Support.Mcp.Requirements.Models;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Products;

/// <summary>TR-MCP-PRODUCT-API-001: DI registration for product CQRS handlers.</summary>
public static class ProductServiceCollectionExtensions
{
    /// <summary>
    /// Registers product CQRS handlers. Call from both HTTP host and STDIO host.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProductCqrs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<ICommandHandler<CreateProductCommand, ProductDto>, CreateProductCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateProductCommand, ProductDto>, UpdateProductCommandHandler>();
        services.AddTransient<ICommandHandler<DeleteProductCommand, ProductDto>, DeleteProductCommandHandler>();
        services.AddTransient<ICommandHandler<AddProductMemberCommand, ProductDto>, AddProductMemberCommandHandler>();
        services.AddTransient<ICommandHandler<RemoveProductMemberCommand, ProductDto>, RemoveProductMemberCommandHandler>();
        services.AddTransient<IQueryHandler<GetProductQuery, ProductDto>, GetProductQueryHandler>();
        services.AddTransient<IQueryHandler<ListProductsQuery, IReadOnlyList<ProductDto>>, ListProductsQueryHandler>();
        services.AddTransient<IQueryHandler<ListProductMembersQuery, ProductDto>, ListProductMembersQueryHandler>();
        services.AddTransient<IQueryHandler<GetProductEffectiveRequirementsQuery, EffectiveRequirementsResult>, GetProductEffectiveRequirementsQueryHandler>();
        services.AddTransient<IQueryHandler<GetProductRequirementContextQuery, IReadOnlyList<ProductRequirementChunkDto>>, GetProductRequirementContextQueryHandler>();
        return services;
    }
}
