using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for workspace management endpoints (<c>/mcpserver/workspace</c>). Provides full
/// lifecycle operations: list, get, create, update, delete, start/stop Kestrel hosts,
/// query process status, and manage the global marker prompt template.
/// </summary>
/// <seealso cref="McpServerClient.Workspace"/>
public sealed class WorkspaceClient : McpClientBase
{
    /// <inheritdoc />
    public WorkspaceClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal WorkspaceClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>List all registered workspaces.</summary>
    public async Task<WorkspaceListResult> ListAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<WorkspaceListResult>("mcpserver/workspace", cancellationToken);
    }

    /// <summary>Get a workspace by its Base64URL-encoded path key.</summary>
    public async Task<WorkspaceDto> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        return await GetAsync<WorkspaceDto>($"mcpserver/workspace/{Uri.EscapeDataString(key)}", cancellationToken);
    }

    /// <summary>Register a new workspace.</summary>
    public async Task<WorkspaceMutationResult> CreateAsync(WorkspaceCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<WorkspaceMutationResult>("mcpserver/workspace", request, cancellationToken);
    }

    /// <summary>Update a workspace.</summary>
    public async Task<WorkspaceMutationResult> UpdateAsync(string key, WorkspaceUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<WorkspaceMutationResult>($"mcpserver/workspace/{Uri.EscapeDataString(key)}", request, cancellationToken);
    }

    /// <summary>Apply a natural-language workspace policy directive.</summary>
    public async Task<WorkspacePolicyApplyResult> ApplyPolicyAsync(WorkspacePolicyApplyRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<WorkspacePolicyApplyResult>("mcpserver/workspace/policy", request, cancellationToken);
    }

    /// <summary>Delete a workspace.</summary>
    public async Task<WorkspaceMutationResult> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<WorkspaceMutationResult>($"mcpserver/workspace/{Uri.EscapeDataString(key)}", cancellationToken);
    }

    /// <summary>Initialize workspace directory scaffold.</summary>
    public async Task<WorkspaceInitResult> InitAsync(string key, CancellationToken cancellationToken = default)
    {
        return await PostAsync<WorkspaceInitResult>($"mcpserver/workspace/{Uri.EscapeDataString(key)}/init", null, cancellationToken);
    }

    /// <summary>Start a workspace Kestrel host.</summary>
    public async Task<WorkspaceProcessStatus> StartAsync(string key, CancellationToken cancellationToken = default)
    {
        return await PostAsync<WorkspaceProcessStatus>($"mcpserver/workspace/{Uri.EscapeDataString(key)}/start", null, cancellationToken);
    }

    /// <summary>Stop a workspace Kestrel host.</summary>
    public async Task<WorkspaceProcessStatus> StopAsync(string key, CancellationToken cancellationToken = default)
    {
        return await PostAsync<WorkspaceProcessStatus>($"mcpserver/workspace/{Uri.EscapeDataString(key)}/stop", null, cancellationToken);
    }

    /// <summary>Get workspace process status.</summary>
    public async Task<WorkspaceProcessStatus> GetStatusAsync(string key, CancellationToken cancellationToken = default)
    {
        return await GetAsync<WorkspaceProcessStatus>($"mcpserver/workspace/{Uri.EscapeDataString(key)}/status", cancellationToken);
    }

    /// <summary>Get the global marker prompt template. Only available on the primary workspace.</summary>
    public async Task<GlobalPromptResult> GetGlobalPromptAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GlobalPromptResult>("mcpserver/workspace/prompt", cancellationToken);
    }

    /// <summary>Update the global marker prompt template. Only available on the primary workspace.</summary>
    public async Task<GlobalPromptResult> UpdateGlobalPromptAsync(GlobalPromptUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<GlobalPromptResult>("mcpserver/workspace/prompt", request, cancellationToken);
    }

    /// <summary>Regenerates marker files for all currently running workspaces.</summary>
    public async Task<MarkerRegenerationResult> RegenerateMarkersAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<MarkerRegenerationResult>("mcpserver/workspace/markers/regenerate", null, cancellationToken);
    }

    /// <summary>Gets the current requirement scope layer for the active workspace.</summary>
    public async Task<WorkspaceCurrentRequirementLayer> GetCurrentRequirementLayerAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<WorkspaceCurrentRequirementLayer>("mcpserver/workspace/current-requirement-layer", cancellationToken);
    }

    /// <summary>Sets the current requirement scope layer for the active workspace.</summary>
    public async Task<WorkspaceCurrentRequirementLayer> SetCurrentRequirementLayerAsync(WorkspaceCurrentRequirementLayerUpdate request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<WorkspaceCurrentRequirementLayer>("mcpserver/workspace/current-requirement-layer", request, cancellationToken);
    }
}
