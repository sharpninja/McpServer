using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for workspace management endpoints (<c>/mcp/workspace</c>). Provides full
/// lifecycle operations: list, get, create, update, delete, start/stop Kestrel hosts,
/// query process status, and manage the global marker prompt template.
/// </summary>
/// <seealso cref="McpServerClient.Workspace"/>
public sealed class WorkspaceClient : McpClientBase
{
    /// <inheritdoc />
    public WorkspaceClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    /// <summary>List all registered workspaces.</summary>
    public async Task<WorkspaceListResult> ListAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<WorkspaceListResult>("mcp/workspace", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get a workspace by its Base64URL-encoded path key.</summary>
    public async Task<WorkspaceDto> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        return await GetAsync<WorkspaceDto>($"mcp/workspace/{Uri.EscapeDataString(key)}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Register a new workspace.</summary>
    public async Task<WorkspaceMutationResult> CreateAsync(WorkspaceCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<WorkspaceMutationResult>("mcp/workspace", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Update a workspace.</summary>
    public async Task<WorkspaceMutationResult> UpdateAsync(string key, WorkspaceUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<WorkspaceMutationResult>($"mcp/workspace/{Uri.EscapeDataString(key)}", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Delete a workspace.</summary>
    public async Task<WorkspaceMutationResult> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<WorkspaceMutationResult>($"mcp/workspace/{Uri.EscapeDataString(key)}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initialize workspace directory scaffold.</summary>
    public async Task<WorkspaceInitResult> InitAsync(string key, CancellationToken cancellationToken = default)
    {
        return await PostAsync<WorkspaceInitResult>($"mcp/workspace/{Uri.EscapeDataString(key)}/init", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Start a workspace Kestrel host.</summary>
    public async Task<WorkspaceProcessStatus> StartAsync(string key, CancellationToken cancellationToken = default)
    {
        return await PostAsync<WorkspaceProcessStatus>($"mcp/workspace/{Uri.EscapeDataString(key)}/start", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stop a workspace Kestrel host.</summary>
    public async Task<WorkspaceProcessStatus> StopAsync(string key, CancellationToken cancellationToken = default)
    {
        return await PostAsync<WorkspaceProcessStatus>($"mcp/workspace/{Uri.EscapeDataString(key)}/stop", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get workspace process status.</summary>
    public async Task<WorkspaceProcessStatus> GetStatusAsync(string key, CancellationToken cancellationToken = default)
    {
        return await GetAsync<WorkspaceProcessStatus>($"mcp/workspace/{Uri.EscapeDataString(key)}/status", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get the global marker prompt template. Only available on the primary workspace.</summary>
    public async Task<GlobalPromptResult> GetGlobalPromptAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GlobalPromptResult>("mcp/workspace/prompt", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Update the global marker prompt template. Only available on the primary workspace.</summary>
    public async Task<GlobalPromptResult> UpdateGlobalPromptAsync(GlobalPromptUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<GlobalPromptResult>("mcp/workspace/prompt", request, cancellationToken).ConfigureAwait(false);
    }
}
