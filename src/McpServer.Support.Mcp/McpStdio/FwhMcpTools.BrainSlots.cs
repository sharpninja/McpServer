using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Services;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

/// <summary>
/// FR-MCP-129 and FR-MCP-130: MCP STDIO tools for external brain-slot registry and invocation.
/// </summary>
public sealed partial class FwhMcpTools
{
    /// <summary>Lists brain-slot definitions for a workspace.</summary>
    [McpServerTool(Name = "brain_slot_list"), Description("List external brain-slot definitions for a workspace.")]
    public async Task<string> BrainSlotList(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        if (_brainSlotRegistry is null)
            return SerializeJson(new { error = "brain slot registry service is unavailable" });

        return SerializeJson(await _brainSlotRegistry.ListAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Gets one brain-slot definition.</summary>
    [McpServerTool(Name = "brain_slot_get"), Description("Get an external brain-slot definition by slot id.")]
    public async Task<string> BrainSlotGet(
        [Description("Slot id")] string slotId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        if (_brainSlotRegistry is null)
            return SerializeJson(new { error = "brain slot registry service is unavailable" });

        var slot = await _brainSlotRegistry.GetAsync(slotId, cancellationToken).ConfigureAwait(false);
        return slot is null
            ? SerializeJson(new { error = $"Brain slot '{slotId}' not found." })
            : SerializeJson(slot);
    }

    /// <summary>Creates or updates a brain-slot definition.</summary>
    [McpServerTool(Name = "brain_slot_upsert"), Description("Create or update an external brain-slot definition.")]
    public async Task<string> BrainSlotUpsert(
        [Description("Slot id")] string slotId,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Role: LeftHemisphere, RightHemisphere, CuriosityEngine, or ArbiterOfTruth")] string role,
        [Description("Provider kind: OpenAI or OpenAICompatible")] string providerKind,
        [Description("Model id")] string modelId,
        [Description("Credential reference using env:, config:, or file:")] string credentialReference,
        [Description("Trusted party id; defaults to brain-slot role mapping when omitted")] string? partyId = null,
        [Description("Optional display name")] string? displayName = null,
        [Description("Optional endpoint URI")] string? endpoint = null,
        [Description("Whether the slot is enabled")] bool enabled = false,
        [Description("Allow replacement of an existing enabled slot for the same role")] bool replaceExisting = false,
        [Description("Timeout in seconds")] int timeoutSeconds = 30,
        [Description("Maximum output tokens")] int maxOutputTokens = 1024,
        [Description("Optional system prompt")] string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        if (_brainSlotRegistry is null)
            return SerializeJson(new { error = "brain slot registry service is unavailable" });

        try
        {
            var slot = await _brainSlotRegistry.UpsertAsync(slotId, new UpsertBrainSlotRequest
            {
                Role = role,
                ProviderKind = providerKind,
                ModelId = modelId,
                CredentialReference = credentialReference,
                PartyId = partyId ?? string.Empty,
                DisplayName = displayName,
                Endpoint = endpoint,
                Enabled = enabled,
                ReplaceExisting = replaceExisting,
                TimeoutSeconds = timeoutSeconds,
                MaxOutputTokens = maxOutputTokens,
                SystemPrompt = systemPrompt,
            }, cancellationToken).ConfigureAwait(false);
            return SerializeJson(slot);
        }
        catch (Exception ex) when (ex is BrainSlotValidationException or BrainSlotConflictException)
        {
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Soft-deletes and disables a brain-slot definition.</summary>
    [McpServerTool(Name = "brain_slot_delete"), Description("Soft-delete and disable an external brain-slot definition.")]
    public async Task<string> BrainSlotDelete(
        [Description("Slot id")] string slotId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        if (_brainSlotRegistry is null)
            return SerializeJson(new { error = "brain slot registry service is unavailable" });

        try
        {
            return SerializeJson(await _brainSlotRegistry.DeleteAsync(slotId, cancellationToken).ConfigureAwait(false));
        }
        catch (BrainSlotNotFoundException ex)
        {
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Enables a brain-slot definition.</summary>
    [McpServerTool(Name = "brain_slot_enable"), Description("Enable an external brain-slot definition.")]
    public async Task<string> BrainSlotEnable(
        [Description("Slot id")] string slotId,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Allow replacement of an existing enabled slot for the same role")] bool replaceExisting = false,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        if (_brainSlotRegistry is null)
            return SerializeJson(new { error = "brain slot registry service is unavailable" });

        try
        {
            return SerializeJson(await _brainSlotRegistry.EnableAsync(slotId, replaceExisting, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is BrainSlotNotFoundException or BrainSlotConflictException)
        {
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Disables a brain-slot definition.</summary>
    [McpServerTool(Name = "brain_slot_disable"), Description("Disable an external brain-slot definition.")]
    public async Task<string> BrainSlotDisable(
        [Description("Slot id")] string slotId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        if (_brainSlotRegistry is null)
            return SerializeJson(new { error = "brain slot registry service is unavailable" });

        try
        {
            return SerializeJson(await _brainSlotRegistry.DisableAsync(slotId, cancellationToken).ConfigureAwait(false));
        }
        catch (BrainSlotNotFoundException ex)
        {
            return SerializeJson(new { error = ex.Message });
        }
    }

    /// <summary>Gets brain-slot readiness status.</summary>
    [McpServerTool(Name = "brain_slot_status"), Description("Get external brain-slot readiness status for a workspace.")]
    public async Task<string> BrainSlotStatus(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        if (_brainSlotRegistry is null)
            return SerializeJson(new { error = "brain slot registry service is unavailable" });

        return SerializeJson(await _brainSlotRegistry.GetStatusAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Invokes a configured brain slot.</summary>
    [McpServerTool(Name = "brain_slot_invoke"), Description("Invoke an external brain slot with transaction-gated output return.")]
    public async Task<string> BrainSlotInvoke(
        [Description("Slot id")] string slotId,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Input prompt")] string input,
        [Description("Owning session-log turn id")] string? turnId = null,
        [Description("Whether committed Curiosity output should be admitted to GraphRAG/context")] bool admitToGraphRag = false,
        [Description("Optional JSON object of string metadata")] string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        if (_brainSlotInvocation is null)
            return SerializeJson(new { error = "brain slot invocation service is unavailable" });

        return SerializeJson(await _brainSlotInvocation.InvokeAsync(slotId, new BrainSlotInvokeRequest
        {
            Input = input,
            TurnId = turnId,
            AdmitToGraphRag = admitToGraphRag,
            Metadata = ParseMetadata(metadataJson),
        }, cancellationToken).ConfigureAwait(false));
    }

    private static IReadOnlyDictionary<string, string> ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return new Dictionary<string, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson) ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string> { ["metadataParseError"] = "invalid json" };
        }
    }
}
