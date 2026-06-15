using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// FR-MCP-129: Client for external brain-slot registry and invocation endpoints.
/// </summary>
/// <seealso cref="McpServerClient.BrainSlots"/>
public sealed class BrainSlotClient : McpClientBase
{
    /// <inheritdoc />
    public BrainSlotClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal BrainSlotClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Lists visible brain-slot definitions.</summary>
    public Task<IReadOnlyList<BrainSlotDto>> ListAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<BrainSlotDto>>("mcpserver/brain-slots", cancellationToken);

    /// <summary>Gets a brain-slot definition by slot id.</summary>
    public Task<BrainSlotDto> GetAsync(string slotId, CancellationToken cancellationToken = default)
        => GetAsync<BrainSlotDto>($"mcpserver/brain-slots/{Uri.EscapeDataString(slotId)}", cancellationToken);

    /// <summary>Creates or updates a brain-slot definition.</summary>
    public Task<BrainSlotDto> UpsertAsync(string slotId, UpsertBrainSlotRequest request, CancellationToken cancellationToken = default)
        => PutAsync<BrainSlotDto>($"mcpserver/brain-slots/{Uri.EscapeDataString(slotId)}", request, cancellationToken);

    /// <summary>Soft-deletes and disables a brain-slot definition.</summary>
    public Task<BrainSlotDto> DeleteAsync(string slotId, CancellationToken cancellationToken = default)
        => DeleteAsync<BrainSlotDto>($"mcpserver/brain-slots/{Uri.EscapeDataString(slotId)}", cancellationToken);

    /// <summary>Enables a brain-slot definition.</summary>
    public Task<BrainSlotDto> EnableAsync(string slotId, bool replaceExisting = false, CancellationToken cancellationToken = default)
        => PostAsync<BrainSlotDto>($"mcpserver/brain-slots/{Uri.EscapeDataString(slotId)}/enable?replaceExisting={replaceExisting.ToString().ToLowerInvariant()}", null, cancellationToken);

    /// <summary>Disables a brain-slot definition.</summary>
    public Task<BrainSlotDto> DisableAsync(string slotId, CancellationToken cancellationToken = default)
        => PostAsync<BrainSlotDto>($"mcpserver/brain-slots/{Uri.EscapeDataString(slotId)}/disable", null, cancellationToken);

    /// <summary>Gets per-role brain-slot readiness status.</summary>
    public Task<BrainSlotStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        => GetAsync<BrainSlotStatusResponse>("mcpserver/brain-slots/status", cancellationToken);

    /// <summary>Invokes a configured brain slot.</summary>
    public Task<BrainSlotInvokeResponse> InvokeAsync(string slotId, BrainSlotInvokeRequest request, CancellationToken cancellationToken = default)
        => PostAsync<BrainSlotInvokeResponse>($"mcpserver/brain-slots/{Uri.EscapeDataString(slotId)}/invoke", request, cancellationToken);
}
