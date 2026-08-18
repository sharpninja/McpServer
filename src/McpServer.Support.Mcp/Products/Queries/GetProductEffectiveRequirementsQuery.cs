using McpServer.Cqrs;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.Products.Queries;

/// <summary>
/// FR-MCP-PRODUCT-003 / TR-MCP-PRODUCT-SHARE-001: Effective requirements with optional product union.
/// </summary>
/// <param name="WorkspacePath">Caller workspace path.</param>
/// <param name="LayerKey">Optional layer preview; empty uses the caller workspace current layer.</param>
/// <param name="ProductScope"><c>product</c> (default) or <c>local</c>.</param>
public sealed record GetProductEffectiveRequirementsQuery(
    string WorkspacePath,
    string? LayerKey,
    string ProductScope)
    : IQuery<EffectiveRequirementsResult>;

/// <summary>FR-MCP-PRODUCT-003: Handles <see cref="GetProductEffectiveRequirementsQuery"/>.</summary>
public sealed class GetProductEffectiveRequirementsQueryHandler(McpDbContext db)
    : IQueryHandler<GetProductEffectiveRequirementsQuery, EffectiveRequirementsResult>
{
    /// <inheritdoc />
    public async Task<Result<EffectiveRequirementsResult>> HandleAsync(
        GetProductEffectiveRequirementsQuery query,
        CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var caller = ProductCqrsHelpers.ResolveCaller(query.WorkspacePath);
            var scope = string.IsNullOrWhiteSpace(query.ProductScope) ? "product" : query.ProductScope.Trim();
            if (!scope.Equals("product", StringComparison.OrdinalIgnoreCase)
                && !scope.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                return Result<EffectiveRequirementsResult>.Failure(
                    ProductResultCodes.BadRequestMsg("productScope must be product or local."));
            }

            var value = await ProductShareHelper
                .GetEffectiveAsync(db, caller, query.LayerKey, scope, context.CancellationToken)
                .ConfigureAwait(false);
            return Result<EffectiveRequirementsResult>.Success(value);
        }
        catch (Exception ex)
        {
            return Result<EffectiveRequirementsResult>.Failure(ex.Message, ex);
        }
    }
}
