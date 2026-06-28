using McpServer.Support.Mcp.Storage.Entities;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-QUAD-001: Durable registry operations for external brain slots.
/// </summary>
public interface IBrainSlotRegistryService
{
    /// <summary>Lists visible slots.</summary>
    Task<IReadOnlyList<BrainSlotDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets one visible slot by id.</summary>
    Task<BrainSlotDto?> GetAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a slot.</summary>
    Task<BrainSlotDto> UpsertAsync(string slotId, UpsertBrainSlotRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes and disables a slot.</summary>
    Task<BrainSlotDto> DeleteAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>Enables a slot.</summary>
    Task<BrainSlotDto> EnableAsync(string slotId, bool replaceExisting, CancellationToken cancellationToken = default);

    /// <summary>Disables a slot.</summary>
    Task<BrainSlotDto> DisableAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>Gets readiness status for all four roles.</summary>
    Task<BrainSlotStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the backing entity for internal invocation.</summary>
    Task<BrainSlotDefinitionEntity?> GetEntityAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>Gets the enabled backing entity for a role.</summary>
    Task<BrainSlotDefinitionEntity?> GetEnabledEntityForRoleAsync(string role, CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-QUAD-002: Resolves external-model credentials from safe references.
/// </summary>
public interface IBrainSlotCredentialResolver
{
    /// <summary>Resolves a credential reference to a secret value.</summary>
    Task<string?> ResolveAsync(string credentialReference, CancellationToken cancellationToken = default);

    /// <summary>Validates the credential reference shape without resolving the secret.</summary>
    bool IsSupportedReference(string credentialReference);
}

/// <summary>
/// TR-MCP-QUAD-002: Provider-specific external chat client seam.
/// </summary>
public interface IBrainSlotChatClient
{
    /// <summary>Completes one slot prompt.</summary>
    Task<string> CompleteAsync(
        BrainSlotDefinitionEntity slot,
        string input,
        double? temperature,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-QUAD-002: Creates external chat clients for configured brain slots.
/// </summary>
public interface IBrainSlotChatClientFactory
{
    /// <summary>Creates a chat client for the supplied slot and resolved credential.</summary>
    IBrainSlotChatClient Create(BrainSlotDefinitionEntity slot, string credential);
}

/// <summary>
/// FR-MCP-129 and FR-MCP-130: Executes gated brain-slot invocations.
/// </summary>
public interface IBrainSlotInvocationService
{
    /// <summary>Invokes a configured brain slot.</summary>
    Task<BrainSlotInvokeResponse> InvokeAsync(string slotId, BrainSlotInvokeRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// FR-MCP-130: Admits committed Curiosity results into context/GraphRAG.
/// </summary>
public interface IBrainSlotContextAdmissionService
{
    /// <summary>Stores committed Curiosity output in the context corpus.</summary>
    Task<string?> AdmitAsync(BrainSlotDefinitionEntity slot, string output, string transactionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// FR-MCP-134, FR-MCP-135, and TR-MCP-QUAD-005: Executes full Quad-Brain branches.
/// </summary>
public interface IQuadBrainOrchestrationService
{
    /// <summary>Executes the full four-role Quad-Brain decision loop.</summary>
    Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
        QuadBrainOrchestrationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Executes Arbiter-of-Truth reconciliation over committed role evidence.</summary>
    Task<AotReconciliationResponse> ExecuteAotReconciliationAsync(
        AotReconciliationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Executes a durable, audited role-weight update after safety gates pass.</summary>
    Task<QuadBrainWeightUpdateResponse> ExecuteWeightUpdateAsync(
        QuadBrainWeightUpdateRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-QUAD-001: Brain-slot validation failure.
/// </summary>
public sealed class BrainSlotValidationException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="BrainSlotValidationException"/> class.</summary>
    public BrainSlotValidationException(string message, string reason = BrainSlotReasonCodes.ValidationFailed)
        : base(message)
    {
        Reason = reason;
    }

    /// <summary>Structured reason code.</summary>
    public string Reason { get; }
}

/// <summary>
/// TR-MCP-QUAD-001: Brain-slot conflict failure.
/// </summary>
public sealed class BrainSlotConflictException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="BrainSlotConflictException"/> class.</summary>
    public BrainSlotConflictException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// TR-MCP-QUAD-001: Brain-slot not-found failure.
/// </summary>
public sealed class BrainSlotNotFoundException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="BrainSlotNotFoundException"/> class.</summary>
    public BrainSlotNotFoundException(string slotId)
        : base($"Brain slot '{slotId}' was not found.")
    {
        SlotId = slotId;
    }

    /// <summary>Missing slot id.</summary>
    public string SlotId { get; }
}
