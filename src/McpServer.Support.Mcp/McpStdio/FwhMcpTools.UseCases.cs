// TR-MCP-USECASE-005 / FR-MCP-USECASE-001..010: Use case MCP tools partial of FwhMcpTools.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases;
using McpServer.Support.Mcp.UseCases.Commands;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.UseCases.Queries;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    /// <summary>FR-MCP-USECASE-001: List workspace use cases.</summary>
    [McpServerTool(Name = "usecase_list"), Description("List use cases in the workspace. Optional title filter.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseList(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional title contains filter")] string? title = null,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.QueryAsync(
                new ListUseCasesQuery(workspacePath, title),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-001: Get a use case aggregate by id.</summary>
    [McpServerTool(Name = "usecase_get"), Description("Get a use case by id including actors, flows, steps, and FR links.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseGet(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Use case id")] long useCaseId,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.QueryAsync(
                new GetUseCaseQuery(workspacePath, useCaseId),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-001: Create a use case.</summary>
    [McpServerTool(Name = "usecase_create"), Description("Create a use case. Optional frId creates a Realizes link; createBasicFlow seeds a Main Basic flow.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseCreate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Use case title")] string title,
        [Description("Optional brief description")] string? briefDescription = null,
        [Description("Optional precondition")] string? precondition = null,
        [Description("Optional postcondition")] string? postcondition = null,
        [Description("Optional scope")] string? scope = null,
        [Description("Priority (default 0)")] int priority = 0,
        [Description("Optional FR id to link (string)")] string? frId = null,
        [Description("Link type when frId is set (default Realizes)")] string? linkType = null,
        [Description("When true, create an empty Basic Main flow")] bool createBasicFlow = false,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.SendAsync(
                new CreateUseCaseCommand(
                    workspacePath,
                    new CreateUseCaseRequest
                    {
                        Title = title,
                        BriefDescription = briefDescription,
                        Precondition = precondition,
                        Postcondition = postcondition,
                        Scope = scope,
                        Priority = priority,
                        FrId = frId,
                        LinkType = linkType,
                        CreateBasicFlow = createBasicFlow,
                    }),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-001: Update use case header fields.</summary>
    [McpServerTool(Name = "usecase_update"), Description("Update use case header fields (title, brief, pre/postcondition, scope, priority).")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseUpdate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Use case id")] long useCaseId,
        [Description("Optional new title")] string? title = null,
        [Description("Optional brief description")] string? briefDescription = null,
        [Description("Optional precondition")] string? precondition = null,
        [Description("Optional postcondition")] string? postcondition = null,
        [Description("Optional scope")] string? scope = null,
        [Description("Optional priority")] int? priority = null,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.SendAsync(
                new UpdateUseCaseCommand(
                    workspacePath,
                    useCaseId,
                    new UpdateUseCaseRequest
                    {
                        Title = title,
                        BriefDescription = briefDescription,
                        Precondition = precondition,
                        Postcondition = postcondition,
                        Scope = scope,
                        Priority = priority,
                    }),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-001: Soft-delete a use case.</summary>
    [McpServerTool(Name = "usecase_delete"), Description("Soft-delete a use case and its durable child rows.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseDelete(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Use case id")] long useCaseId,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.SendAsync(
                new DeleteUseCaseCommand(workspacePath, useCaseId),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-003: Link a use case to a functional requirement.</summary>
    [McpServerTool(Name = "usecase_link_fr"), Description("Link a use case to a functional requirement (default LinkType Realizes).")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseLinkFr(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Use case id")] long useCaseId,
        [Description("Functional requirement id (string)")] string frId,
        [Description("Link type (default Realizes)")] string? linkType = null,
        [Description("Link order")] int linkOrder = 0,
        [Description("Optional notes")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.SendAsync(
                new LinkUseCaseToFrCommand(workspacePath, useCaseId, frId, linkType, linkOrder, notes),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-004: Create a use case from a functional requirement.</summary>
    [McpServerTool(Name = "usecase_from_fr"), Description("Create a shell use case from an FR with an automatic Realizes link.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseFromFr(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Functional requirement id (string)")] string frId,
        [Description("Optional title override")] string? title = null,
        [Description("Optional brief description override")] string? briefDescription = null,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            // Title/briefDescription overrides are reserved for a future command expansion;
            // Phase 1 CreateUseCaseFromFrCommand uses FR title/body only.
            _ = title;
            _ = briefDescription;
            var result = await _dispatcher.SendAsync(
                new CreateUseCaseFromFrCommand(workspacePath, frId),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-005: Generate Mermaid diagram for a use case.</summary>
    [McpServerTool(Name = "usecase_diagram"), Description("Generate a use case diagram (format: mermaid or plantuml).")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseDiagram(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Use case id")] long useCaseId,
        [Description("Diagram format (mermaid or plantuml)")] string format = "mermaid",
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.QueryAsync(
                new GetUseCaseDiagramQuery(workspacePath, useCaseId, format),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-006: Report UC↔FR Realizes coverage gaps.</summary>
    [McpServerTool(Name = "usecase_coverage"), Description("Report use cases and FRs missing Realizes links in the workspace.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseCoverage(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.QueryAsync(
                new GetUseCaseFrCoverageQuery(workspacePath),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-008: Set approval status (Draft/Submitted/Approved/Rejected).</summary>
    [McpServerTool(Name = "usecase_set_approval"), Description("Set use case approval status. Approving increments versionNumber.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseSetApproval(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Use case id")] long useCaseId,
        [Description("Status: Draft, Submitted, Approved, or Rejected")] string status,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.SendAsync(
                new SetUseCaseApprovalStatusCommand(workspacePath, useCaseId, status),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-009: Set product membership key.</summary>
    [McpServerTool(Name = "usecase_set_product"), Description("Set or clear product membership key for multi-workspace sharing hooks.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseSetProduct(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Use case id")] long useCaseId,
        [Description("Product key; omit or empty to clear")] string? productKey = null,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.SendAsync(
                new SetUseCaseProductKeyCommand(workspacePath, useCaseId, productKey),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-USECASE-009: List use cases by product key.</summary>
    [McpServerTool(Name = "usecase_list_by_product"), Description("List use cases sharing a product key.")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> UseCaseListByProduct(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Product key")] string productKey,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
            return JsonSerializer.Serialize(new { error = "CQRS dispatcher is not registered." });

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _dispatcher.QueryAsync(
                new ListUseCasesByProductQuery(workspacePath, productKey),
                cancellationToken).ConfigureAwait(false);
            return SerializeResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    private static string SerializeResult<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            var error = result.Error ?? "Use case operation failed.";
            if (error.StartsWith(UseCaseResultCodes.NotFound, StringComparison.Ordinal) ||
                error.StartsWith(UseCaseResultCodes.Validation, StringComparison.Ordinal) ||
                error.StartsWith(UseCaseResultCodes.Conflict, StringComparison.Ordinal))
            {
                var prefix = error.StartsWith(UseCaseResultCodes.NotFound, StringComparison.Ordinal)
                    ? UseCaseResultCodes.NotFound
                    : error.StartsWith(UseCaseResultCodes.Validation, StringComparison.Ordinal)
                        ? UseCaseResultCodes.Validation
                        : UseCaseResultCodes.Conflict;
                error = error[prefix.Length..].TrimStart();
            }

            return JsonSerializer.Serialize(new { error }, s_camelCaseOptions);
        }

        return JsonSerializer.Serialize(result.Value, s_camelCaseOptions);
    }
}
