using System.Net.Http;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>FR-MCP-HYGIENE-005: Typed client for read-only workspace validation.</summary>
public sealed class WorkspaceValidationClient : McpClientBase
{
    /// <inheritdoc />
    public WorkspaceValidationClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
    }

    internal WorkspaceValidationClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>FR-MCP-HYGIENE-001: POST /mcpserver/workspace-validation/validate</summary>
    public Task<WorkspaceValidationResult> ValidateAsync(
        WorkspaceValidationRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<WorkspaceValidationResult>("mcpserver/workspace-validation/validate", request, cancellationToken);
}
