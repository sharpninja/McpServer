using System.Text.Json;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-134, FR-MCP-135, TR-MCP-QUAD-005, TR-MCP-QUAD-006, and TR-MCP-QUAD-007:
/// executes full Quad-Brain orchestration, AoT reconciliation, and durable role-weight updates.
/// </summary>
public sealed class QuadBrainOrchestrationService : IQuadBrainOrchestrationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly McpDbContext _db;
    private readonly IBrainSlotRegistryService _registry;
    private readonly IBrainSlotInvocationService _invocation;
    private readonly ITurnTransactionCoordinator? _transactionCoordinator;
    private readonly IOptionsMonitor<TurnTransactionOptions> _transactionOptions;
    private readonly ILogger<QuadBrainOrchestrationService> _logger;
    private readonly IBrainInteractionSessionLogger? _brainLogger;

    /// <summary>Initializes a new instance of the <see cref="QuadBrainOrchestrationService"/> class.</summary>
    public QuadBrainOrchestrationService(
        McpDbContext db,
        IBrainSlotRegistryService registry,
        IBrainSlotInvocationService invocation,
        IOptionsMonitor<TurnTransactionOptions> transactionOptions,
        ILogger<QuadBrainOrchestrationService> logger,
        ITurnTransactionCoordinator? transactionCoordinator = null,
        IBrainInteractionSessionLogger? brainLogger = null)
    {
        _db = db;
        _registry = registry;
        _invocation = invocation;
        _transactionOptions = transactionOptions;
        _logger = logger;
        _transactionCoordinator = transactionCoordinator;
        _brainLogger = brainLogger;
    }

    /// <inheritdoc />
    public async Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
        QuadBrainOrchestrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var started = DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(request.Input))
            return RejectOrchestration(BrainSlotReasonCodes.ValidationFailed, started, []);

        var status = await _registry.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        if (!status.QuadReady)
            return RejectOrchestration(BrainSlotReasonCodes.QuadNotReady, started, []);

        var slots = await GetEnabledSlotsAsync(cancellationToken).ConfigureAwait(false);
        if (slots.Count != BrainSlotRoles.All.Count)
            return RejectOrchestration(BrainSlotReasonCodes.QuadNotReady, started, []);

        var roleResults = new List<QuadBrainRoleResult>();
        var leftSlot = slots[BrainSlotRoles.LeftHemisphere];
        var rightSlot = slots[BrainSlotRoles.RightHemisphere];
        var leftTask = InvokeRoleAsync(
                leftSlot,
                BuildRolePrompt(BrainSlotRoles.LeftHemisphere, request.Input, slots[BrainSlotRoles.LeftHemisphere]),
                request,
                admitToGraphRag: false,
                cancellationToken);
        var rightTask = InvokeRoleAsync(
                rightSlot,
                BuildRolePrompt(BrainSlotRoles.RightHemisphere, request.Input, slots[BrainSlotRoles.RightHemisphere]),
                request,
                admitToGraphRag: false,
                cancellationToken);
        await Task.WhenAll(leftTask, rightTask).ConfigureAwait(false);

        var left = await leftTask.ConfigureAwait(false);
        var right = await rightTask.ConfigureAwait(false);
        roleResults.Add(ToRoleResult(left, leftSlot));
        roleResults.Add(ToRoleResult(right, rightSlot));
        if (!IsTransactionCommitted(left))
            return RejectOrchestration(left.Reason, started, roleResults);
        if (!IsTransactionCommitted(right))
            return RejectOrchestration(right.Reason, started, roleResults);

        if (!HasValidOutput(left) && !HasValidOutput(right))
        {
            var curiosity = await InvokeRoleAsync(
                    slots[BrainSlotRoles.CuriosityEngine],
                    BuildCuriosityEscalationPrompt(request.Input, left, right, slots[BrainSlotRoles.CuriosityEngine]),
                    request,
                    request.AdmitCuriosityToGraphRag,
                    cancellationToken)
                .ConfigureAwait(false);
            roleResults.Add(ToRoleResult(curiosity, slots[BrainSlotRoles.CuriosityEngine]));
            return RejectOrchestration(
                IsTransactionCommitted(curiosity) ? BrainSlotReasonCodes.OrchestrationFailed : curiosity.Reason,
                started,
                roleResults);
        }

        var reconciliation = await ExecuteAotReconciliationAsync(new AotReconciliationRequest
        {
            Input = request.Input,
            LeftOutput = left.Output ?? string.Empty,
            RightOutput = right.Output ?? string.Empty,
            TurnId = request.TurnId,
            Metadata = AddMetadata(request.Metadata, "quadOperation", "full-orchestration"),
        }, cancellationToken).ConfigureAwait(false);

        roleResults.Add(ToRoleResult(reconciliation, slots[BrainSlotRoles.ArbiterOfTruth]));

        if (!IsCommitted(reconciliation))
            return RejectOrchestration(reconciliation.Reason, started, roleResults);
        if (IsAotSemanticRejection(reconciliation.Output))
        {
            var voteLeftTask = InvokeRoleAsync(
                    leftSlot,
                    BuildVotingPrompt(BrainSlotRoles.LeftHemisphere, request.Input, left.Output, right.Output, reconciliation.Output, leftSlot),
                    request,
                    admitToGraphRag: false,
                    cancellationToken);
            var voteRightTask = InvokeRoleAsync(
                    rightSlot,
                    BuildVotingPrompt(BrainSlotRoles.RightHemisphere, request.Input, left.Output, right.Output, reconciliation.Output, rightSlot),
                    request,
                    admitToGraphRag: false,
                    cancellationToken);
            await Task.WhenAll(voteLeftTask, voteRightTask).ConfigureAwait(false);

            var voteLeft = await voteLeftTask.ConfigureAwait(false);
            var voteRight = await voteRightTask.ConfigureAwait(false);
            roleResults.Add(ToRoleResult(voteLeft, leftSlot));
            roleResults.Add(ToRoleResult(voteRight, rightSlot));
            if (!HasValidOutput(voteLeft))
                return RejectOrchestration(voteLeft.Reason, started, roleResults);
            if (!HasValidOutput(voteRight))
                return RejectOrchestration(voteRight.Reason, started, roleResults);

            reconciliation = await ExecuteAotReconciliationAsync(new AotReconciliationRequest
            {
                Input = request.Input,
                LeftOutput = voteLeft.Output ?? string.Empty,
                RightOutput = voteRight.Output ?? string.Empty,
                TurnId = request.TurnId,
                Metadata = AddMetadata(request.Metadata, "quadOperation", "voting-reconciliation"),
            }, cancellationToken).ConfigureAwait(false);
            roleResults.Add(ToRoleResult(reconciliation, slots[BrainSlotRoles.ArbiterOfTruth]));
            if (!IsCommitted(reconciliation) || IsAotSemanticRejection(reconciliation.Output))
                return RejectOrchestration(reconciliation.Reason, started, roleResults);
        }

        QuadBrainWeightUpdateResponse? weightUpdate = null;
        if (request.WeightUpdate is not null)
        {
            request.WeightUpdate.TurnId ??= request.TurnId;
            weightUpdate = await ExecuteWeightUpdateAsync(request.WeightUpdate, cancellationToken).ConfigureAwait(false);
        }

        return new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Reason = BrainSlotReasonCodes.None,
            Output = reconciliation.Output,
            TransactionId = reconciliation.TransactionId,
            DiffgramId = reconciliation.DiffgramId,
            RoleResults = roleResults,
            WeightUpdate = weightUpdate,
            StartedAtUtc = started,
            CompletedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <inheritdoc />
    public async Task<AotReconciliationResponse> ExecuteAotReconciliationAsync(
        AotReconciliationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var started = DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(request.Input)
            || (string.IsNullOrWhiteSpace(request.LeftOutput) && string.IsNullOrWhiteSpace(request.RightOutput)))
        {
            return RejectAot(BrainSlotReasonCodes.ValidationFailed, started);
        }

        var arbiter = await _registry.GetEnabledEntityForRoleAsync(BrainSlotRoles.ArbiterOfTruth, cancellationToken)
            .ConfigureAwait(false);
        if (arbiter is null)
            return RejectAot(BrainSlotReasonCodes.QuadNotReady, started);

        var prompt = BuildAotPrompt(request, arbiter);
        var response = await _invocation.InvokeAsync(arbiter.SlotId, new BrainSlotInvokeRequest
        {
            Input = prompt,
            TurnId = request.TurnId,
            AdmitToGraphRag = false,
            Temperature = RoleTemperature(arbiter.Role),
            Metadata = AddMetadata(request.Metadata, "quadOperation", "aot-reconciliation"),
        }, cancellationToken).ConfigureAwait(false);

        // FR-MCP-QBEXEC-003: log the Arbiter-of-Truth reconciliation prompt + output in full (best-effort).
        await LogBrainInteractionAsync(request.Metadata, request.TurnId, BrainSlotRoles.ArbiterOfTruth, prompt, response.Output, cancellationToken)
            .ConfigureAwait(false);

        return new AotReconciliationResponse
        {
            Status = response.Status,
            Reason = response.Reason,
            SlotId = response.SlotId,
            ModelId = response.ModelId,
            TransactionId = response.TransactionId,
            DiffgramId = response.DiffgramId,
            Output = response.Output,
            StartedAtUtc = started,
            CompletedAtUtc = response.CompletedAtUtc,
        };
    }

    /// <inheritdoc />
    public async Task<QuadBrainWeightUpdateResponse> ExecuteWeightUpdateAsync(
        QuadBrainWeightUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var started = DateTimeOffset.UtcNow;
        if (!request.AotApproved || !request.AdminApproved || !request.SafetyGatesPassed || string.IsNullOrWhiteSpace(request.ReasonText))
            return RejectWeight(BrainSlotReasonCodes.WeightUpdateRejected, started, null, null);
        if (!TransactionsAreRequired())
            return RejectWeight(BrainSlotReasonCodes.TransactionsRequired, started, null, null);

        Dictionary<string, double> normalizedWeights;
        Dictionary<string, int> normalizedVersions;
        try
        {
            normalizedWeights = NormalizeWeights(request.RoleWeights);
            normalizedVersions = NormalizeVersions(request.ExpectedVersions);
        }
        catch (BrainSlotValidationException ex)
        {
            return RejectWeight(ex.Reason, started, null, null);
        }

        if (normalizedWeights.Count == 0)
            return RejectWeight(BrainSlotReasonCodes.WeightUpdateRejected, started, null, null);

        var slots = await LoadTrackedWeightSlotsAsync(normalizedWeights.Keys, cancellationToken).ConfigureAwait(false);
        if (slots.Count != normalizedWeights.Count)
            return RejectWeight(BrainSlotReasonCodes.QuadNotReady, started, null, null);

        foreach (var slot in slots)
        {
            if (normalizedVersions.TryGetValue(slot.Role, out var expected) && expected != slot.WeightVersion)
                return RejectWeight(BrainSlotReasonCodes.WeightVersionConflict, started, null, null);
        }

        IReadOnlyList<QuadBrainWeightSnapshot> appliedSnapshots = [];
        var operationBody = new
        {
            roleWeights = normalizedWeights,
            expectedVersions = normalizedVersions,
            request.ReasonText,
            request.ProposedBy,
            request.AotApproved,
            request.AdminApproved,
            request.SafetyGatesPassed,
            metadata = request.Metadata,
            startedAtUtc = started,
        };
        var transaction = await _transactionCoordinator!.ExecuteAsync(
            new TurnTransactionRequest
            {
                TransactionId = $"brain-slot-weight-{Guid.NewGuid():N}",
                TurnId = request.TurnId,
                OperationName = "brain-slot.weight-update",
                OperationBodyJson = JsonSerializer.Serialize(operationBody, JsonOptions),
                PublisherPartyId = BrainSlotValidation.DefaultPartyId(BrainSlotRoles.ArbiterOfTruth),
                Sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Mutating = true,
            },
            async ct =>
            {
                appliedSnapshots = await ApplyWeightUpdateAsync(slots, normalizedWeights, request, ct).ConfigureAwait(false);
                return new TurnMutationResult
                {
                    Success = true,
                    ResultJson = JsonSerializer.Serialize(appliedSnapshots, JsonOptions),
                    RollbackAsync = rollbackToken => RollbackWeightUpdateAsync(appliedSnapshots, rollbackToken),
                };
            },
            cancellationToken).ConfigureAwait(false);

        if (!string.Equals(transaction.Status, "committed", StringComparison.OrdinalIgnoreCase))
            return RejectWeight(BrainSlotReasonCodes.CommitFailed, started, transaction.TransactionId, transaction.DiffgramId);

        return new QuadBrainWeightUpdateResponse
        {
            Status = "committed",
            Reason = BrainSlotReasonCodes.None,
            TransactionId = transaction.TransactionId,
            DiffgramId = transaction.DiffgramId,
            Snapshots = appliedSnapshots,
            StartedAtUtc = started,
            CompletedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private async Task<IReadOnlyDictionary<string, BrainSlotDefinitionEntity>> GetEnabledSlotsAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, BrainSlotDefinitionEntity>(StringComparer.Ordinal);
        foreach (var role in BrainSlotRoles.All)
        {
            var slot = await _registry.GetEnabledEntityForRoleAsync(role, cancellationToken).ConfigureAwait(false);
            if (slot is not null)
                result[role] = slot;
        }

        return result;
    }

    private async Task<BrainSlotInvokeResponse> InvokeRoleAsync(
        BrainSlotDefinitionEntity slot,
        string prompt,
        QuadBrainOrchestrationRequest request,
        bool admitToGraphRag,
        CancellationToken cancellationToken)
    {
        var response = await _invocation.InvokeAsync(slot.SlotId, new BrainSlotInvokeRequest
        {
            Input = prompt,
            TurnId = request.TurnId,
            AdmitToGraphRag = admitToGraphRag,
            Temperature = RoleTemperature(slot.Role),
            Metadata = AddMetadata(request.Metadata, "quadRole", slot.Role),
        }, cancellationToken).ConfigureAwait(false);

        // FR-MCP-QBEXEC-003: log the full prompt + output of this brain interaction (best-effort, secret-redacted).
        await LogBrainInteractionAsync(request.Metadata, request.TurnId, slot.Role, prompt, response.Output, cancellationToken)
            .ConfigureAwait(false);
        return response;
    }

    /// <summary>FR-MCP-QBEXEC-003: best-effort full-text logging of a brain interaction to the session log.</summary>
    private Task LogBrainInteractionAsync(
        IReadOnlyDictionary<string, string>? metadata,
        string? turnId,
        string role,
        string prompt,
        string? output,
        CancellationToken cancellationToken)
    {
        if (_brainLogger is null)
            return Task.CompletedTask;

        var sessionId = MetadataValue(metadata, "sessionId");
        var sourceType = MetadataValue(metadata, "sourceType") ?? "QBAgent";
        return _brainLogger.LogInteractionAsync(sourceType, sessionId, turnId, role, prompt, output, cancellationToken);
    }

    private static string? MetadataValue(IReadOnlyDictionary<string, string>? metadata, string key)
        => metadata is not null && metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private static string BuildRolePrompt(string role, string input, BrainSlotDefinitionEntity slot)
        => $"""
           Quad-Brain role: {role}
           Agent description: {RoleDescription(role)}
           Orchestration weight: {NormalizeWeightForPrompt(slot.OrchestrationWeight)}

           Original input:
           {input}

           Produce the role-specific analysis for this turn. Return only the analysis needed by ArbiterOfTruth.
           """;

    private static string BuildCuriosityEscalationPrompt(
        string input,
        BrainSlotInvokeResponse left,
        BrainSlotInvokeResponse right,
        BrainSlotDefinitionEntity slot)
        => $"""
           Quad-Brain role: {BrainSlotRoles.CuriosityEngine}
           Agent description: {RoleDescription(BrainSlotRoles.CuriosityEngine)}
           Orchestration weight: {NormalizeWeightForPrompt(slot.OrchestrationWeight)}

           Original input:
           {input}

           LeftHemisphere produced no valid committed user response.
           Left status: {left.Status}
           Left output:
           {left.Output}

           RightHemisphere produced no valid committed user response.
           Right status: {right.Status}
           Right output:
           {right.Output}

           Evaluate whether the user appears frustrated by previous responses and identify any deeper research or context that should be admitted for a later turn. Do not answer the user directly.
           """;

    private static string BuildAotPrompt(AotReconciliationRequest request, BrainSlotDefinitionEntity arbiter)
    {
        var curiositySection = string.IsNullOrWhiteSpace(request.CuriosityOutput)
            ? string.Empty
            : $"""

              Additional CuriosityEngine research/context:
              {request.CuriosityOutput}
              """;
        return $"""
           Quad-Brain role: {BrainSlotRoles.ArbiterOfTruth}
           Agent description: {RoleDescription(BrainSlotRoles.ArbiterOfTruth)}
           Orchestration weight: {NormalizeWeightForPrompt(arbiter.OrchestrationWeight)}

           Original input:
           {request.Input}

           LeftHemisphere committed analysis:
           {request.LeftOutput}

           RightHemisphere committed analysis:
           {request.RightOutput}
           {curiositySection}

           Reconcile the evidence, enforce the user's directive, identify any material uncertainty, and return the final decision. You may return the left response, the right response, or a combined response. If neither hemisphere response is valid enough to answer the user, begin the output with "REJECT:" so the runtime can start the voting/reconciliation mechanism.
           """;
    }

    private static string BuildVotingPrompt(
        string role,
        string input,
        string? leftOutput,
        string? rightOutput,
        string? arbiterRejection,
        BrainSlotDefinitionEntity slot)
        => $"""
           Quad-Brain role: {role}
           Agent description: {RoleDescription(role)}
           Orchestration weight: {NormalizeWeightForPrompt(slot.OrchestrationWeight)}

           Voting/reconciliation round.

           Original input:
           {input}

           Initial LeftHemisphere response:
           {leftOutput}

           Initial RightHemisphere response:
           {rightOutput}

           ArbiterOfTruth rejection:
           {arbiterRejection}

           Vote on whether the final answer should use Left, Right, a combined answer, or remain rejected. Return only revised evidence for ArbiterOfTruth; do not answer the user directly.
           """;

    private static QuadBrainRoleResult ToRoleResult(BrainSlotInvokeResponse response, BrainSlotDefinitionEntity slot)
        => new()
        {
            Role = response.Role,
            SlotId = response.SlotId,
            Status = response.Status,
            Reason = response.Reason,
            ModelId = response.ModelId,
            TransactionId = response.TransactionId,
            DiffgramId = response.DiffgramId,
            Output = response.Output,
            OrchestrationWeight = slot.OrchestrationWeight <= 0 ? 1.0 : slot.OrchestrationWeight,
            WeightVersion = slot.WeightVersion,
        };

    private static QuadBrainRoleResult ToRoleResult(AotReconciliationResponse response, BrainSlotDefinitionEntity slot)
        => new()
        {
            Role = BrainSlotRoles.ArbiterOfTruth,
            SlotId = response.SlotId,
            Status = response.Status,
            Reason = response.Reason,
            ModelId = response.ModelId,
            TransactionId = response.TransactionId,
            DiffgramId = response.DiffgramId,
            Output = response.Output,
            OrchestrationWeight = slot.OrchestrationWeight <= 0 ? 1.0 : slot.OrchestrationWeight,
            WeightVersion = slot.WeightVersion,
        };

    private async Task<IReadOnlyList<BrainSlotDefinitionEntity>> LoadTrackedWeightSlotsAsync(
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var normalized = roles.ToArray();
        return await _db.BrainSlotDefinitions
            .Where(slot => slot.Enabled && normalized.Contains(slot.Role))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<QuadBrainWeightSnapshot>> ApplyWeightUpdateAsync(
        IReadOnlyList<BrainSlotDefinitionEntity> slots,
        IReadOnlyDictionary<string, double> normalizedWeights,
        QuadBrainWeightUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var snapshots = new List<QuadBrainWeightSnapshot>();
        foreach (var slot in slots.OrderBy(item => item.Role, StringComparer.Ordinal))
        {
            var previousWeight = slot.OrchestrationWeight <= 0 ? 1.0 : slot.OrchestrationWeight;
            var previousVersion = slot.WeightVersion;
            var newWeight = normalizedWeights[slot.Role];
            slot.OrchestrationWeight = newWeight;
            slot.WeightVersion += 1;
            slot.WeightUpdatedAtUtc = now;
            slot.UpdatedAtUtc = now;

            var snapshot = new QuadBrainWeightSnapshot
            {
                Role = slot.Role,
                SlotId = slot.SlotId,
                PreviousWeight = previousWeight,
                NewWeight = newWeight,
                PreviousVersion = previousVersion,
                NewVersion = slot.WeightVersion,
            };
            snapshots.Add(snapshot);
            AddWeightAudit(slot, request, snapshot);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return snapshots;
    }

    private async Task RollbackWeightUpdateAsync(
        IReadOnlyList<QuadBrainWeightSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        foreach (var snapshot in snapshots)
        {
            var slot = await _db.BrainSlotDefinitions
                .FirstOrDefaultAsync(item => item.SlotId == snapshot.SlotId && item.Role == snapshot.Role, cancellationToken)
                .ConfigureAwait(false);
            if (slot is null)
                continue;

            slot.OrchestrationWeight = snapshot.PreviousWeight;
            slot.WeightVersion = snapshot.PreviousVersion;
            slot.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _db.DataAuditLogs.Add(new DataAuditLogEntity
            {
                WorkspaceId = _db.CurrentWorkspaceId,
                EntityKind = "BrainSlotDefinition",
                EntityKey = slot.SlotId,
                Action = "weight_update_rollback",
                SourceType = nameof(QuadBrainOrchestrationService),
                Actor = "system",
                OccurredAtUtc = DateTimeOffset.UtcNow,
                MetadataJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Rolled back {Count} Quad-Brain weight updates.", snapshots.Count);
    }

    private void AddWeightAudit(
        BrainSlotDefinitionEntity slot,
        QuadBrainWeightUpdateRequest request,
        QuadBrainWeightSnapshot snapshot)
    {
        _db.DataAuditLogs.Add(new DataAuditLogEntity
        {
            WorkspaceId = _db.CurrentWorkspaceId,
            EntityKind = "BrainSlotDefinition",
            EntityKey = slot.SlotId,
            Action = "weight_update",
            SourceType = nameof(QuadBrainOrchestrationService),
            Actor = string.IsNullOrWhiteSpace(request.ProposedBy) ? "system" : request.ProposedBy!.Trim(),
            RequestId = request.TurnId,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            PreviousSnapshotJson = JsonSerializer.Serialize(new
            {
                slot.SlotId,
                slot.Role,
                orchestrationWeight = snapshot.PreviousWeight,
                weightVersion = snapshot.PreviousVersion,
            }, JsonOptions),
            CurrentSnapshotJson = JsonSerializer.Serialize(BrainSlotRegistryService.ToDto(slot), JsonOptions),
            MetadataJson = JsonSerializer.Serialize(new
            {
                request.ReasonText,
                request.AotApproved,
                request.AdminApproved,
                request.SafetyGatesPassed,
                request.Metadata,
                snapshot,
            }, JsonOptions),
        });
    }

    private bool TransactionsAreRequired()
    {
        var transactionOptions = _transactionOptions.CurrentValue;
        return _transactionCoordinator is not null
            && transactionOptions.Enabled
            && transactionOptions.RequiredForMutations
            && !_transactionCoordinator.GetStatus().Degraded;
    }

    private static Dictionary<string, double> NormalizeWeights(IReadOnlyDictionary<string, double>? weights)
    {
        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var pair in weights ?? new Dictionary<string, double>())
        {
            var role = BrainSlotValidation.NormalizeRole(pair.Key);
            var weight = pair.Value;
            if (double.IsNaN(weight) || double.IsInfinity(weight) || weight <= 0 || weight > 100)
                throw new BrainSlotValidationException("roleWeights must be finite values greater than 0 and less than or equal to 100.", BrainSlotReasonCodes.WeightUpdateRejected);
            if (result.ContainsKey(role))
                throw new BrainSlotValidationException("roleWeights contains duplicate roles.", BrainSlotReasonCodes.WeightUpdateRejected);
            result[role] = weight;
        }

        return result;
    }

    private static Dictionary<string, int> NormalizeVersions(IReadOnlyDictionary<string, int>? versions)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in versions ?? new Dictionary<string, int>())
        {
            var role = BrainSlotValidation.NormalizeRole(pair.Key);
            result[role] = pair.Value;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string> AddMetadata(
        IReadOnlyDictionary<string, string>? metadata,
        string key,
        string value)
    {
        var result = new Dictionary<string, string>(metadata ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        result[key] = value;
        return result;
    }

    private static bool IsTransactionCommitted(BrainSlotInvokeResponse response)
        => string.Equals(response.Status, "committed", StringComparison.OrdinalIgnoreCase);

    private static bool HasValidOutput(BrainSlotInvokeResponse response)
        => IsTransactionCommitted(response)
           && !string.IsNullOrWhiteSpace(response.Output);

    private static bool IsCommitted(AotReconciliationResponse response)
        => string.Equals(response.Status, "committed", StringComparison.OrdinalIgnoreCase)
           && !string.IsNullOrWhiteSpace(response.Output);

    private static bool IsAotSemanticRejection(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return false;

        var trimmed = output.TrimStart();
        if (trimmed.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!trimmed.StartsWith('{'))
            return false;

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            return JsonPropertyEquals(document.RootElement, "decision", "reject")
                   || JsonPropertyEquals(document.RootElement, "decision", "rejected")
                   || JsonPropertyEquals(document.RootElement, "status", "reject")
                   || JsonPropertyEquals(document.RootElement, "status", "rejected");
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool JsonPropertyEquals(JsonElement element, string propertyName, string expected)
        => element.TryGetProperty(propertyName, out var property)
           && property.ValueKind == JsonValueKind.String
           && string.Equals(property.GetString(), expected, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeWeightForPrompt(double value)
        => (value <= 0 ? 1.0 : value).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

    private static double? RoleTemperature(string role)
        => string.Equals(role, BrainSlotRoles.RightHemisphere, StringComparison.Ordinal)
            ? 0.0
            : null;

    private static string RoleDescription(string role)
        => role switch
        {
            BrainSlotRoles.LeftHemisphere => "Creative left-brain analyst; emphasize creativity, generative options, and novel solution paths.",
            BrainSlotRoles.RightHemisphere => "Absolute accuracy right-brain analyst; emphasize exactness, verification, and factual correctness.",
            BrainSlotRoles.CuriosityEngine => "Curious researcher; investigate frustration signals and deeper context without directly answering the user.",
            BrainSlotRoles.ArbiterOfTruth => "Arbiter of truth for code tasks and enforcer of rules for all tasks.",
            _ => "Quad-Brain specialist.",
        };

    private static QuadBrainOrchestrationResponse RejectOrchestration(
        string reason,
        DateTimeOffset started,
        IReadOnlyList<QuadBrainRoleResult> roleResults)
        => new()
        {
            Status = "rejected",
            Reason = string.IsNullOrWhiteSpace(reason) ? BrainSlotReasonCodes.OrchestrationFailed : reason,
            RoleResults = roleResults,
            StartedAtUtc = started,
            CompletedAtUtc = DateTimeOffset.UtcNow,
        };

    private static AotReconciliationResponse RejectAot(string reason, DateTimeOffset started)
        => new()
        {
            Status = "rejected",
            Reason = reason,
            SlotId = BrainSlotRoles.ArbiterOfTruth,
            StartedAtUtc = started,
            CompletedAtUtc = DateTimeOffset.UtcNow,
        };

    private static QuadBrainWeightUpdateResponse RejectWeight(
        string reason,
        DateTimeOffset started,
        string? transactionId,
        string? diffgramId)
        => new()
        {
            Status = "rejected",
            Reason = reason,
            TransactionId = transactionId,
            DiffgramId = diffgramId,
            StartedAtUtc = started,
            CompletedAtUtc = DateTimeOffset.UtcNow,
        };
}
