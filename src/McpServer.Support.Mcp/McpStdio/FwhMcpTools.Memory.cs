using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using McpServer.Support.Mcp.Services;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

/// <summary>FR-MCP-MEMORY-017 / TR-MCP-MEMORY-API-002: Additive memory MCP verbs.</summary>
public sealed partial class FwhMcpTools
{
    /// <summary>FR-MCP-MEMORY-010: Remember a durable multi-layer memory.</summary>
    [McpServerTool(Name = "memory_remember"), Description(MemorySurfaceCatalog.RememberDescription)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> MemoryRemember(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Memory content")] string content,
        [Description("Optional title")] string? title = null,
        [Description("Optional summary")] string? summary = null,
        [Description("Memory type token")] string? type = null,
        [Description("Optional comma-separated tags")] string? tags = null,
        [Description("Optional confidence in [0,1]")] double? confidence = null,
        [Description("Optional provenance kind")] string? sourceKind = null,
        [Description("Optional provenance reference")] string? sourceRef = null,
        [Description("Memory scope: Global or Workspace")] string? scope = null,
        [Description("Optional explicit memory id")] string? id = null,
        [Description("Optional updater identity")] string? updatedBy = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseMemoryScope(scope, out var parsedScope, out var error))
                return SerializeJson(MemoryErrorEnvelope.Create(McpErrorClassifier.ValidationError, error ?? "Invalid scope.", 400));

            var request = new MemoryRememberRequest
            {
                Id = id,
                Title = title,
                Summary = summary,
                Content = content,
                Type = type,
                Tags = ParseTagList(tags),
                Confidence = confidence,
                SourceKind = sourceKind,
                SourceRef = sourceRef,
                Scope = parsedScope,
                UpdatedBy = updatedBy,
            };

            if (_memoryMutations is not null)
            {
                var gated = await _memoryMutations.RememberAsync(request, cancellationToken).ConfigureAwait(false);
                return SerializeJson(gated);
            }

            return await DispatchMemoryAsync(
                new RememberMemoryCommand(_workspaceContext.WorkspacePath ?? workspacePath, request),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-MEMORY-011: Recall memories by meaning or keyword.</summary>
    [McpServerTool(Name = "memory_recall"), Description(MemorySurfaceCatalog.RecallDescription)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> MemoryRecall(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Recall query text")] string query,
        [Description("Optional minimum score in [0,1]")] double? minScore = null,
        [Description("Optional result cap")] int? topN = null,
        [Description("Optional comma-separated tags")] string? tags = null,
        [Description("Optional type filter")] string? type = null,
        [Description("Optional scope filter: Global or Workspace")] string? scope = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!TryParseMemoryScope(scope, out var parsedScope, out var error))
                return SerializeJson(MemoryErrorEnvelope.Create(McpErrorClassifier.ValidationError, error ?? "Invalid scope.", 400));

            var dispatched = await RequireMemoryDispatcher().QueryAsync(
                new RecallMemoryQuery(
                    _workspaceContext.WorkspacePath ?? workspacePath,
                    query,
                    minScore,
                    topN,
                    ParseTagList(tags),
                    type,
                    parsedScope),
                cancellationToken).ConfigureAwait(false);
            return dispatched.IsSuccess && dispatched.Value is not null
                ? SerializeJson(dispatched.Value)
                : SerializeJson(MemoryErrorEnvelope.Create(McpErrorClassifier.InternalError, dispatched.Error ?? "Recall failed.", 500));
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-MEMORY-012: Explore a memory neighborhood.</summary>
    [McpServerTool(Name = "memory_explore"), Description(MemorySurfaceCatalog.ExploreDescription)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> MemoryExplore(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional MEMORY-* seed")] string? seedId = null,
        [Description("Optional query seed")] string? query = null,
        [Description("Optional hop depth")] int? depth = null,
        [Description("Optional neighbor cap")] int? maxNeighbors = null,
        [Description("Optional Hebbian override")] bool? hebbianEnabled = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var dispatched = await RequireMemoryDispatcher().QueryAsync(
                new ExploreMemoryQuery(
                    _workspaceContext.WorkspacePath ?? workspacePath,
                    seedId,
                    query,
                    depth,
                    maxNeighbors,
                    hebbianEnabled),
                cancellationToken).ConfigureAwait(false);
            return dispatched.IsSuccess && dispatched.Value is not null
                ? SerializeJson(dispatched.Value)
                : SerializeJson(MemoryErrorEnvelope.Create(McpErrorClassifier.InternalError, dispatched.Error ?? "Explore failed.", 500));
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-MEMORY-013: Plan or apply consolidate/sleep merge.</summary>
    [McpServerTool(Name = "memory_consolidate"), Description(MemorySurfaceCatalog.ConsolidateDescription)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> MemoryConsolidate(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("When false, apply writes. Default is dry-run.")] bool? dryRun = null,
        [Description("Optional similarity threshold in [0,1]")] double? similarityThreshold = null,
        [Description("When true, merged-away rows may be hard-deleted")] bool? allowHardDelete = null,
        [Description("Optional client run id")] string? runId = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var request = new MemoryConsolidateRequest
            {
                DryRun = dryRun,
                SimilarityThreshold = similarityThreshold,
                AllowHardDelete = allowHardDelete,
                RunId = runId,
            };

            if (_memoryMutations is not null && request.DryRun == false)
            {
                var gated = await _memoryMutations.ConsolidateAsync(request, cancellationToken).ConfigureAwait(false);
                return SerializeJson(gated);
            }

            return await DispatchMemoryAsync(
                new ConsolidateMemoryCommand(_workspaceContext.WorkspacePath ?? workspacePath, request),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-MEMORY-015: Promote a session-log or context source into memory.</summary>
    [McpServerTool(Name = "memory_promote"), Description(MemorySurfaceCatalog.PromoteDescription)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> MemoryPromote(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Source kind: sessionlog or context")] string sourceKind,
        [Description("Source reference")] string sourceRef,
        [Description("Optional content override")] string? content = null,
        [Description("Optional summary override")] string? summary = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            var request = new MemoryPromoteRequest
            {
                SourceKind = sourceKind,
                SourceRef = sourceRef,
                Content = content,
                Summary = summary,
            };

            if (_memoryMutations is not null)
            {
                var gated = await _memoryMutations.PromoteAsync(request, cancellationToken).ConfigureAwait(false);
                return SerializeJson(gated);
            }

            return await DispatchMemoryAsync(
                new PromoteMemoryCommand(_workspaceContext.WorkspacePath ?? workspacePath, request),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    /// <summary>FR-MCP-MEMORY-014: Revert a memory to a prior snapshot.</summary>
    [McpServerTool(Name = "memory_revert"), Description(MemorySurfaceCatalog.RevertDescription)]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> MemoryRevert(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Memory id")] string id,
        [Description("Snapshot number to restore")] int versionNumber,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (_memoryMutations is not null)
            {
                var gated = await _memoryMutations.RevertAsync(id, versionNumber, cancellationToken).ConfigureAwait(false);
                return SerializeJson(gated);
            }

            return await DispatchMemoryAsync(
                new RevertMemoryCommand(_workspaceContext.WorkspacePath ?? workspacePath, id, versionNumber),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return McpToolErrors.Serialize(ex);
        }
    }

    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    private async Task<string> DispatchMemoryAsync<T>(McpServer.Cqrs.ICommand<T> command, CancellationToken cancellationToken)
    {
        var dispatched = await RequireMemoryDispatcher().SendAsync(command, cancellationToken).ConfigureAwait(false);
        return dispatched.IsSuccess && dispatched.Value is not null
            ? SerializeJson(dispatched.Value)
            : SerializeJson(MemoryErrorEnvelope.Create(McpErrorClassifier.InternalError, dispatched.Error ?? "Memory command failed.", 500));
    }

    private McpServer.Cqrs.IDispatcher RequireMemoryDispatcher()
        => _dispatcher ?? throw new InvalidOperationException("CQRS dispatcher is not registered for memory surfaces.");

    private static IReadOnlyList<string>? ParseTagList(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return null;

        return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
