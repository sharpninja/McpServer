using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-129, FR-MCP-130, TR-MCP-QUAD-002, and TR-MCP-QUAD-003: Gated live brain-slot invocation service.
/// </summary>
public sealed class BrainSlotInvocationService : IBrainSlotInvocationService
{
    private readonly McpDbContext _db;
    private readonly IBrainSlotRegistryService _registry;
    private readonly IBrainSlotCredentialResolver _credentialResolver;
    private readonly IBrainSlotChatClientFactory _chatClientFactory;
    private readonly IBrainSlotContextAdmissionService _contextAdmission;
    private readonly ITurnTransactionCoordinator? _transactionCoordinator;
    private readonly IKeyServerPartyRegistry _partyRegistry;
    private readonly IOptionsMonitor<BrainSlotOptions> _brainSlotOptions;
    private readonly IOptionsMonitor<TurnTransactionOptions> _transactionOptions;
    private readonly ILogger<BrainSlotInvocationService> _logger;

    /// <summary>Initializes a new instance of the <see cref="BrainSlotInvocationService"/> class.</summary>
    public BrainSlotInvocationService(
        McpDbContext db,
        IBrainSlotRegistryService registry,
        IBrainSlotCredentialResolver credentialResolver,
        IBrainSlotChatClientFactory chatClientFactory,
        IBrainSlotContextAdmissionService contextAdmission,
        IKeyServerPartyRegistry partyRegistry,
        IOptionsMonitor<BrainSlotOptions> brainSlotOptions,
        IOptionsMonitor<TurnTransactionOptions> transactionOptions,
        ILogger<BrainSlotInvocationService> logger,
        ITurnTransactionCoordinator? transactionCoordinator = null)
    {
        _db = db;
        _registry = registry;
        _credentialResolver = credentialResolver;
        _chatClientFactory = chatClientFactory;
        _contextAdmission = contextAdmission;
        _partyRegistry = partyRegistry;
        _brainSlotOptions = brainSlotOptions;
        _transactionOptions = transactionOptions;
        _logger = logger;
        _transactionCoordinator = transactionCoordinator;
    }

    /// <inheritdoc />
    public async Task<BrainSlotInvokeResponse> InvokeAsync(string slotId, BrainSlotInvokeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var started = DateTimeOffset.UtcNow;
        var normalizedSlotId = string.IsNullOrWhiteSpace(slotId) ? string.Empty : slotId.Trim();
        var invocation = new BrainSlotInvocationEntity
        {
            SlotId = normalizedSlotId,
            Status = "attempted",
            Reason = BrainSlotReasonCodes.None,
            TurnId = request.TurnId,
            StartedAtUtc = started,
            AdmitToGraphRag = request.AdmitToGraphRag,
            MetadataJson = JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string, string>()),
        };

        var slot = await _registry.GetEntityAsync(normalizedSlotId, cancellationToken).ConfigureAwait(false);
        if (slot is null)
            return await RejectAsync(invocation, BrainSlotReasonCodes.SlotNotFound, started, "slot not found", cancellationToken).ConfigureAwait(false);

        PopulateInvocationFromSlot(invocation, slot);
        _db.BrainSlotInvocations.Add(invocation);
        AddAudit("invoke_attempt", slot, invocation, null);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!_brainSlotOptions.CurrentValue.ExecutionEnabled)
            return await RejectAsync(invocation, BrainSlotReasonCodes.ExecutionDisabled, started, "execution disabled", cancellationToken).ConfigureAwait(false);
        if (!slot.Enabled)
            return await RejectAsync(invocation, BrainSlotReasonCodes.SlotDisabled, started, "slot disabled", cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(request.Input))
            return await RejectAsync(invocation, BrainSlotReasonCodes.ValidationFailed, started, "input is required", cancellationToken).ConfigureAwait(false);
        if (request.AdmitToGraphRag && !string.Equals(slot.Role, BrainSlotRoles.CuriosityEngine, StringComparison.Ordinal))
            return await RejectAsync(invocation, BrainSlotReasonCodes.DeferredFeatureDisabled, started, "only CuriosityEngine may request GraphRAG admission", cancellationToken).ConfigureAwait(false);
        if (!TransactionsAreRequired())
            return await RejectAsync(invocation, BrainSlotReasonCodes.TransactionsRequired, started, "required turn transactions are disabled", cancellationToken).ConfigureAwait(false);

        try
        {
            BrainSlotValidation.ValidateEndpoint(slot.ProviderKind, slot.Endpoint, _brainSlotOptions);
        }
        catch (BrainSlotValidationException ex)
        {
            return await RejectAsync(invocation, ex.Reason, started, ex.Message, cancellationToken).ConfigureAwait(false);
        }

        if (!await PartyMappingIsActiveAsync(slot, cancellationToken).ConfigureAwait(false))
            return await RejectAsync(invocation, BrainSlotReasonCodes.PartyMappingInvalid, started, "trusted party signing key is missing or disabled", cancellationToken).ConfigureAwait(false);

        var credential = await _credentialResolver.ResolveAsync(slot.CredentialReference, cancellationToken).ConfigureAwait(false);
        if (credential is null)
            return await RejectAsync(invocation, BrainSlotReasonCodes.CredentialUnavailable, started, "credential reference could not be resolved", cancellationToken).ConfigureAwait(false);

        var promptHash = HashHex(request.Input);
        invocation.PromptSha256 = promptHash;
        string output;
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(slot.TimeoutSeconds <= 0 ? _brainSlotOptions.CurrentValue.DefaultTimeoutSeconds : slot.TimeoutSeconds));
            var client = _chatClientFactory.Create(slot, credential);
            output = await client.CompleteAsync(slot, request.Input, request.Temperature, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await RejectAsync(invocation, BrainSlotReasonCodes.ProviderFailed, started, "provider timeout", cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Brain slot provider failed for {SlotId}", slot.SlotId);
            return await RejectAsync(invocation, BrainSlotReasonCodes.ProviderFailed, started, "provider failed", cancellationToken).ConfigureAwait(false);
        }

        var outputHash = HashHex(output);
        invocation.OutputSha256 = outputHash;
        var operationBodyJson = JsonSerializer.Serialize(new
        {
            slotId = slot.SlotId,
            slot.Role,
            slot.ProviderKind,
            slot.ModelId,
            promptSha256 = promptHash,
            outputSha256 = outputHash,
            admissionTarget = request.AdmitToGraphRag ? "GraphRAG" : "none",
            temperature = request.Temperature,
            metadata = request.Metadata,
            startedAtUtc = started,
            modelCompletedAtUtc = DateTimeOffset.UtcNow,
        });

        var transaction = await _transactionCoordinator!.ExecuteAsync(
            new TurnTransactionRequest
            {
                TransactionId = $"brain-slot-{Guid.NewGuid():N}",
                TurnId = request.TurnId,
                OperationName = "brain-slot.invoke",
                OperationBodyJson = operationBodyJson,
                PublisherPartyId = slot.PartyId,
                Sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Mutating = true,
            },
            _ => Task.FromResult(new TurnMutationResult
            {
                Success = true,
                ResultJson = JsonSerializer.Serialize(new
                {
                    slot.SlotId,
                    slot.Role,
                    slot.ModelId,
                    promptSha256 = promptHash,
                    outputSha256 = outputHash,
                }),
            }),
            cancellationToken).ConfigureAwait(false);

        if (!string.Equals(transaction.Status, "committed", StringComparison.OrdinalIgnoreCase))
        {
            invocation.TransactionId = transaction.TransactionId;
            return await RejectAsync(invocation, BrainSlotReasonCodes.CommitFailed, started, transaction.Message ?? transaction.Status, cancellationToken).ConfigureAwait(false);
        }

        invocation.Status = "committed";
        invocation.Reason = BrainSlotReasonCodes.None;
        invocation.TransactionId = transaction.TransactionId;
        invocation.DiffgramId = transaction.DiffgramId;
        invocation.CompletedAtUtc = DateTimeOffset.UtcNow;
        AddAudit("invoke_committed", slot, invocation, new { transaction.TransactionId, transaction.DiffgramId });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (request.AdmitToGraphRag && string.Equals(slot.Role, BrainSlotRoles.CuriosityEngine, StringComparison.Ordinal))
        {
            try
            {
                await _contextAdmission.AdmitAsync(slot, output, transaction.TransactionId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Committed brain-slot output admission failed for {SlotId}", slot.SlotId);
            }
        }

        return new BrainSlotInvokeResponse
        {
            Status = "committed",
            Reason = BrainSlotReasonCodes.None,
            SlotId = slot.SlotId,
            Role = slot.Role,
            TransactionId = transaction.TransactionId,
            DiffgramId = transaction.DiffgramId,
            ModelId = slot.ModelId,
            Output = output,
            StartedAtUtc = started,
            CompletedAtUtc = invocation.CompletedAtUtc ?? DateTimeOffset.UtcNow,
        };
    }

    private bool TransactionsAreRequired()
    {
        var transactionOptions = _transactionOptions.CurrentValue;
        return _transactionCoordinator is not null
            && transactionOptions.Enabled
            && transactionOptions.RequiredForMutations
            && !_transactionCoordinator.GetStatus().Degraded;
    }

    private async Task<bool> PartyMappingIsActiveAsync(BrainSlotDefinitionEntity slot, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slot.PartyId))
            return false;
        var key = await _partyRegistry.GetPartyKeyAsync(slot.PartyId, BrainSlotValidation.SigningKeyId(slot.PartyId), cancellationToken)
            .ConfigureAwait(false);
        return key is not null
            && string.Equals(key.Status, "active", StringComparison.OrdinalIgnoreCase)
            && string.Equals(key.Purpose, "signing", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<BrainSlotInvokeResponse> RejectAsync(
        BrainSlotInvocationEntity invocation,
        string reason,
        DateTimeOffset started,
        string? details,
        CancellationToken cancellationToken)
    {
        invocation.Status = "rejected";
        invocation.Reason = reason;
        invocation.CompletedAtUtc = DateTimeOffset.UtcNow;
        if (_db.Entry(invocation).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
        {
            if (!string.IsNullOrWhiteSpace(invocation.Role))
                _db.BrainSlotInvocations.Add(invocation);
        }
        else
        {
            _db.BrainSlotInvocations.Update(invocation);
        }

        _db.DataAuditLogs.Add(new DataAuditLogEntity
        {
            WorkspaceId = _db.CurrentWorkspaceId,
            EntityKind = "BrainSlotInvocation",
            EntityKey = invocation.InvocationId,
            Action = "invoke_rejected",
            SourceType = nameof(BrainSlotInvocationService),
            Actor = "system",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { reason, details }),
        });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new BrainSlotInvokeResponse
        {
            Status = "rejected",
            Reason = reason,
            SlotId = invocation.SlotId,
            Role = invocation.Role,
            TransactionId = invocation.TransactionId,
            DiffgramId = invocation.DiffgramId,
            ModelId = string.IsNullOrWhiteSpace(invocation.ModelId) ? null : invocation.ModelId,
            Output = null,
            StartedAtUtc = started,
            CompletedAtUtc = invocation.CompletedAtUtc ?? DateTimeOffset.UtcNow,
        };
    }

    private void AddAudit(string action, BrainSlotDefinitionEntity slot, BrainSlotInvocationEntity invocation, object? metadata)
    {
        _db.DataAuditLogs.Add(new DataAuditLogEntity
        {
            WorkspaceId = _db.CurrentWorkspaceId,
            EntityKind = "BrainSlotInvocation",
            EntityKey = invocation.InvocationId,
            Action = action,
            SourceType = nameof(BrainSlotInvocationService),
            Actor = "system",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new
            {
                slot.SlotId,
                slot.Role,
                slot.ProviderKind,
                slot.ModelId,
                invocation.TurnId,
                invocation.AdmitToGraphRag,
                metadata,
            }),
        });
    }

    private static void PopulateInvocationFromSlot(BrainSlotInvocationEntity invocation, BrainSlotDefinitionEntity slot)
    {
        invocation.SlotId = slot.SlotId;
        invocation.Role = slot.Role;
        invocation.ProviderKind = slot.ProviderKind;
        invocation.ModelId = slot.ModelId;
    }

    private static string HashHex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
