// TR-MCP-REPL-005 / Phase 1d: Requirements management MCP tools partial of FwhMcpTools.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Products.Queries;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    // ── GROUP A2: Requirements management tools ──────────────────────────

    /// <summary>REQ-MGMT-001: List requirements entries by type (fr/tr/test/mapping/all).</summary>
    [McpServerTool(Name = "requirements_list"), Description("List requirements entries. type = fr|tr|test|mapping|all (default all).")]
    public async Task<string> RequirementsList(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Entry type: fr, tr, test, mapping, or all")] string? type = "all",
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsEntityType(type, out var entityType))
                return JsonSerializer.Serialize(new { error = "Unsupported type. Expected fr|tr|test|mapping|all." });

            return entityType switch
            {
                RequirementsEntityType.Functional => JsonSerializer.Serialize(new { type = "fr", items = await _requirementsDocumentService.GetAllFrAsync(cancellationToken).ConfigureAwait(false) }),
                RequirementsEntityType.Technical => JsonSerializer.Serialize(new { type = "tr", items = await _requirementsDocumentService.GetAllTrAsync(cancellationToken).ConfigureAwait(false) }),
                RequirementsEntityType.Testing => JsonSerializer.Serialize(new { type = "test", items = await _requirementsDocumentService.GetAllTestAsync(cancellationToken).ConfigureAwait(false) }),
                RequirementsEntityType.Mapping => JsonSerializer.Serialize(new { type = "mapping", items = await _requirementsDocumentService.GetAllMappingsAsync(cancellationToken).ConfigureAwait(false) }),
                RequirementsEntityType.All => JsonSerializer.Serialize(new
                {
                    functional = await _requirementsDocumentService.GetAllFrAsync(cancellationToken).ConfigureAwait(false),
                    technical = await _requirementsDocumentService.GetAllTrAsync(cancellationToken).ConfigureAwait(false),
                    testing = await _requirementsDocumentService.GetAllTestAsync(cancellationToken).ConfigureAwait(false),
                    mapping = await _requirementsDocumentService.GetAllMappingsAsync(cancellationToken).ConfigureAwait(false)
                }),
                _ => JsonSerializer.Serialize(new { error = "Unsupported type." })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-PRODUCT-003 / TR-MCP-PRODUCT-API-001: Effective requirements with productScope.</summary>
    [McpServerTool(Name = "requirements_effective"), Description("Get effective requirements. Optional layerKey and productScope=local|product (default product).")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> RequirementsEffective(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional layer preview key")] string? layerKey = null,
        [Description("product (default) or local")] string? productScope = "product",
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _dispatcher.QueryAsync(
            new GetProductEffectiveRequirementsQuery(
                workspacePath,
                layerKey,
                string.IsNullOrWhiteSpace(productScope) ? "product" : productScope),
            cancellationToken).ConfigureAwait(false);
        return SerializeResult(result);
    }

    /// <summary>REQ-MGMT-001: Generate requirements documents as Markdown or workspace files.</summary>
    [McpServerTool(Name = "requirements_generate"), Description("Generate requirements documents. doc = functional|technical|testing|mapping|matrix|all (default all). format = markdown|wiki. doc=all writes files to the workspace and returns export metadata.")]
    public async Task<string> RequirementsGenerate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Document selector: functional, technical, testing, mapping, matrix, or all")] string? doc = "all",
        [Description("Output format: markdown or wiki")] string? format = "markdown",
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsDocType(doc, out var docType))
                return JsonSerializer.Serialize(new { error = "Unsupported doc. Expected functional|technical|testing|mapping|matrix|all." });

            var normalizedFormat = (format ?? "markdown").Trim().ToLowerInvariant();
            if (normalizedFormat == "wiki")
            {
                if (docType != RequirementsDocType.All)
                    return JsonSerializer.Serialize(new { error = "Wiki generation requires doc=all." });

                var export = await _requirementsDocumentService.GenerateWikiAsync(
                    Path.Combine(workspacePath, "docs", "Project", "wiki"),
                    ct: cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(export, s_camelCaseOptions);
            }

            if (normalizedFormat is not "markdown" and not "yaml")
                return JsonSerializer.Serialize(new { error = "Unsupported format. Expected markdown|yaml|wiki." });

            if (docType == RequirementsDocType.All)
            {
                var export = await _requirementsDocumentService.GenerateAllAsync(
                    Path.Combine(workspacePath, "docs", "Project"),
                    ct: cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(export, s_camelCaseOptions);
            }

            var result = await _requirementsDocumentService.GenerateDocumentAsync(docType, cancellationToken).ConfigureAwait(false);
            return result.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>REQ-MGMT-001: Create a requirement or mapping row.</summary>
    [McpServerTool(Name = "requirements_create"), Description("Create a requirement entry. type = fr|tr|test|mapping. For mapping, body is comma-separated TR ids and testIds is comma-separated TEST ids.")]
    public async Task<string> RequirementsCreate(
        [Description("Entry type: fr, tr, test, or mapping")] string type,
        [Description("Entry id (FR/TR/TEST id or FR id for mapping rows)")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Title (required for fr; optional for tr; ignored for test/mapping)")] string? title = null,
        [Description("Body text (required for fr/tr/test; for mapping use comma-separated TR ids)")] string? body = null,
        [Description("Comma-separated TEST ids for mapping rows")] string? testIds = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsEntityType(type, out var entityType) || entityType == RequirementsEntityType.All)
                return JsonSerializer.Serialize(new { error = "Unsupported type. Expected fr|tr|test|mapping." });

            switch (entityType)
            {
                case RequirementsEntityType.Functional:
                {
                    var entry = new FrEntry(id, title ?? string.Empty, body ?? string.Empty);
                    await _requirementsDocumentService.AddFrAsync(entry, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = entry });
                }
                case RequirementsEntityType.Technical:
                {
                    var entry = new TrEntry(id, title ?? string.Empty, body ?? string.Empty);
                    await _requirementsDocumentService.AddTrAsync(entry, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = entry });
                }
                case RequirementsEntityType.Testing:
                {
                    var condition = string.IsNullOrWhiteSpace(body) ? (title ?? string.Empty) : body;
                    var entry = new TestEntry(id, condition);
                    await _requirementsDocumentService.AddTestAsync(entry, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = entry });
                }
                case RequirementsEntityType.Mapping:
                {
                    var mapping = new FrTrMapping(id, ParseMappingIds(body), ParseMappingIds(testIds));
                    await _requirementsDocumentService.UpsertMappingAsync(mapping, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = mapping });
                }
                default:
                    return JsonSerializer.Serialize(new { error = "Unsupported type." });
            }
        }
        catch (RequirementsRepositoryException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>REQ-MGMT-001: Update a requirement or mapping row. Omitted fields remain unchanged.</summary>
    [McpServerTool(Name = "requirements_update"), Description("Update a requirement entry. type = fr|tr|test|mapping. Omitted title/body values keep the current value.")]
    public async Task<string> RequirementsUpdate(
        [Description("Entry type: fr, tr, test, or mapping")] string type,
        [Description("Entry id (FR/TR/TEST id or FR id for mapping rows)")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Updated title (fr/tr only)")] string? title = null,
        [Description("Updated body text or mapping TR id list")] string? body = null,
        [Description("Updated comma-separated TEST ids for mapping rows")] string? testIds = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsEntityType(type, out var entityType) || entityType == RequirementsEntityType.All)
                return JsonSerializer.Serialize(new { error = "Unsupported type. Expected fr|tr|test|mapping." });

            switch (entityType)
            {
                case RequirementsEntityType.Functional:
                {
                    var existing = await _requirementsDocumentService.GetFrAsync(id, cancellationToken).ConfigureAwait(false);
                    if (existing is null) return JsonSerializer.Serialize(new { error = $"FR '{id}' not found." });
                    var updated = existing with
                    {
                        Title = title ?? existing.Title,
                        Body = body ?? existing.Body
                    };
                    await _requirementsDocumentService.UpdateFrAsync(updated, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = updated });
                }
                case RequirementsEntityType.Technical:
                {
                    var existing = await _requirementsDocumentService.GetTrAsync(id, cancellationToken).ConfigureAwait(false);
                    if (existing is null) return JsonSerializer.Serialize(new { error = $"TR '{id}' not found." });
                    var updated = existing with
                    {
                        Title = title ?? existing.Title,
                        Body = body ?? existing.Body
                    };
                    await _requirementsDocumentService.UpdateTrAsync(updated, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = updated });
                }
                case RequirementsEntityType.Testing:
                {
                    var existing = await _requirementsDocumentService.GetTestAsync(id, cancellationToken).ConfigureAwait(false);
                    if (existing is null) return JsonSerializer.Serialize(new { error = $"TEST '{id}' not found." });
                    var updated = existing with
                    {
                        Condition = body ?? title ?? existing.Condition
                    };
                    await _requirementsDocumentService.UpdateTestAsync(updated, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = updated });
                }
                case RequirementsEntityType.Mapping:
                {
                    var existing = await _requirementsDocumentService.GetMappingAsync(id, cancellationToken).ConfigureAwait(false);
                    var trIds = body is null && existing is not null
                        ? existing.TrIds
                        : ParseMappingIds(body);
                    var targetTestIds = testIds is null && existing is not null
                        ? existing.TestIds
                        : ParseMappingIds(testIds);
                    var updated = new FrTrMapping(id, trIds, targetTestIds);
                    await _requirementsDocumentService.UpsertMappingAsync(updated, cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Serialize(new { success = true, item = updated });
                }
                default:
                    return JsonSerializer.Serialize(new { error = "Unsupported type." });
            }
        }
        catch (RequirementsRepositoryException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>REQ-MGMT-001: Delete a requirement or mapping row by id.</summary>
    [McpServerTool(Name = "requirements_delete"), Description("Delete a requirement entry. type = fr|tr|test|mapping.")]
    public async Task<string> RequirementsDelete(
        [Description("Entry type: fr, tr, test, or mapping")] string type,
        [Description("Entry id (FR/TR/TEST id or FR id for mapping rows)")] string id,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseRequirementsEntityType(type, out var entityType) || entityType == RequirementsEntityType.All)
                return JsonSerializer.Serialize(new { error = "Unsupported type. Expected fr|tr|test|mapping." });

            switch (entityType)
            {
                case RequirementsEntityType.Functional:
                    await _requirementsDocumentService.DeleteFrAsync(id, cancellationToken).ConfigureAwait(false);
                    break;
                case RequirementsEntityType.Technical:
                    await _requirementsDocumentService.DeleteTrAsync(id, cancellationToken).ConfigureAwait(false);
                    break;
                case RequirementsEntityType.Testing:
                    await _requirementsDocumentService.DeleteTestAsync(id, cancellationToken).ConfigureAwait(false);
                    break;
                case RequirementsEntityType.Mapping:
                    await _requirementsDocumentService.DeleteMappingAsync(id, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    return JsonSerializer.Serialize(new { error = "Unsupported type." });
            }

            return JsonSerializer.Serialize(new { success = true });
        }
        catch (RequirementsRepositoryException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    private enum RequirementsEntityType
    {
        Functional,
        Technical,
        Testing,
        Mapping,
        All
    }

    private static bool TryParseRequirementsDocType(string? raw, out RequirementsDocType docType)
    {
        switch ((raw ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "functional":
            case "fr":
                docType = RequirementsDocType.Functional;
                return true;
            case "technical":
            case "tr":
                docType = RequirementsDocType.Technical;
                return true;
            case "testing":
            case "test":
                docType = RequirementsDocType.Testing;
                return true;
            case "mapping":
                docType = RequirementsDocType.Mapping;
                return true;
            case "matrix":
                docType = RequirementsDocType.Matrix;
                return true;
            case "all":
                docType = RequirementsDocType.All;
                return true;
            default:
                docType = default;
                return false;
        }
    }

    private static bool TryParseRequirementsEntityType(string? raw, out RequirementsEntityType entityType)
    {
        switch ((raw ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "functional":
            case "fr":
                entityType = RequirementsEntityType.Functional;
                return true;
            case "technical":
            case "tr":
                entityType = RequirementsEntityType.Technical;
                return true;
            case "testing":
            case "test":
                entityType = RequirementsEntityType.Testing;
                return true;
            case "mapping":
                entityType = RequirementsEntityType.Mapping;
                return true;
            case "all":
                entityType = RequirementsEntityType.All;
                return true;
            default:
                entityType = default;
                return false;
        }
    }

    private static IReadOnlyList<string> ParseMappingIds(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Array.Empty<string>();

        return body
            .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
