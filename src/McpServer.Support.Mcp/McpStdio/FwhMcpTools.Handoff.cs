using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Services;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    /// <summary>FR-HANDOFF-007: Ingest a handoff document through the shared service.</summary>
    [McpServerTool(Name = "handoff_ingest"), Description("Ingest a workspace-scoped handoff document from a path, content, or MCP artifact and return a TODO draft or created TODO.")]
    public async Task<string> HandoffIngest(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Source kind: Path, Content, or Artifact.")] string sourceKind,
        [Description("Workspace-contained path when sourceKind is Path.")] string? path = null,
        [Description("Caller-supplied content when sourceKind is Content.")] string? content = null,
        [Description("MCP artifact id when sourceKind is Artifact.")] string? artifactId = null,
        [Description("Mode: DraftOnly, RequireReview, or CreateWhenConfident. Defaults to DraftOnly.")] string? mode = null,
        [Description("When true, skip deterministic replay.")] bool force = false,
        [Description("Optional pooled agent name.")] string? agentName = null,
        [Description("Optional prompt template identifier.")] string? promptTemplateId = null,
        CancellationToken cancellationToken = default)
    {
        if (_handoffIngestionService is null)
            return JsonSerializer.Serialize(new { error = "Handoff service is not registered." }, s_camelCaseOptions);

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        if (!Enum.TryParse<HandoffSourceKind>(sourceKind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
            return JsonSerializer.Serialize(new HandoffIngestionResult { Success = false, Error = "sourceKind must be Path, Content, or Artifact.", ErrorCode = HandoffErrorCodes.InvalidMode }, s_camelCaseOptions);
        var parsedMode = HandoffIngestionMode.DraftOnly;
        if (!string.IsNullOrWhiteSpace(mode))
        {
            if (!Enum.TryParse(mode, ignoreCase: true, out parsedMode) || !Enum.IsDefined(parsedMode))
                return JsonSerializer.Serialize(new HandoffIngestionResult { Success = false, Error = "mode must be DraftOnly, RequireReview, or CreateWhenConfident.", ErrorCode = "invalid_mode" }, s_camelCaseOptions);
        }

        var request = new HandoffIngestionRequest
        {
            SourceKind = kind,
            Path = path,
            Content = content,
            ArtifactId = artifactId,
            Mode = parsedMode,
            Force = force,
            AgentName = agentName,
            PromptTemplateId = promptTemplateId,
        };
        var result = await _handoffIngestionService.IngestAsync(request, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_camelCaseOptions);
    }

    /// <summary>FR-HANDOFF-007: Inspect a persisted handoff run.</summary>
    [McpServerTool(Name = "handoff_get"), Description("Get a persisted handoff ingestion run by run id.")]
    public async Task<string> HandoffGet(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Handoff run id.")] string runId,
        CancellationToken cancellationToken = default)
    {
        if (_handoffIngestionService is null)
            return JsonSerializer.Serialize(new { error = "Handoff service is not registered." }, s_camelCaseOptions);

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _handoffIngestionService.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_camelCaseOptions);
    }

    /// <summary>FR-HANDOFF-007: Approve or reject a stored handoff run.</summary>
    [McpServerTool(Name = "handoff_approve"), Description("Approve or reject a stored handoff run. Approval revalidates the draft before TODO creation.")]
    public async Task<string> HandoffApprove(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Handoff run id.")] string runId,
        [Description("True to approve and create the TODO.")] bool approved,
        [Description("Optional reviewer identity.")] string? reviewer = null,
        [Description("Optional review notes.")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (_handoffIngestionService is null)
            return JsonSerializer.Serialize(new { error = "Handoff service is not registered." }, s_camelCaseOptions);

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _handoffIngestionService.ApproveAsync(
            runId,
            new HandoffApprovalRequest { Approved = approved, Reviewer = reviewer, Notes = notes },
            cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_camelCaseOptions);
    }
}
