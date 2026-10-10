using System.Net.Http;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>FR-MCP-HYGIENE-005: Typed client for read-only workspace validation.</summary>
public sealed class WorkspaceValidationClient : McpClientBase
{
    /// <inheritdoc />
    public WorkspaceValidationClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options)
    {
    }

    internal WorkspaceValidationClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>FR-MCP-HYGIENE-001: POST /qbrainai/workspace-validation/validate</summary>
    public Task<WorkspaceValidationResult> ValidateAsync(
        WorkspaceValidationRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<WorkspaceValidationResult>("qbrainai/workspace-validation/validate", request, cancellationToken);
}
