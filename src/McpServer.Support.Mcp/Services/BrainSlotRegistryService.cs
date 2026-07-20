using System.Text.Json;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-129 and TR-MCP-QUAD-001: EF-backed durable brain-slot registry.
/// </summary>
public sealed class BrainSlotRegistryService : IBrainSlotRegistryService
{
    private static readonly string[] SoftDeleteFilter = ["SoftDelete"];
    private readonly McpDbContext _db;
    private readonly IKeyServerPartyRegistry _partyRegistry;
    private readonly IBrainSlotCredentialResolver _credentialResolver;
    private readonly IOptionsMonitor<BrainSlotOptions> _options;
    private readonly ILogger<BrainSlotRegistryService> _logger;
    private readonly BrainSlotPartyKeyReconciler _partyKeyReconciler;

    /// <summary>Initializes a new instance of the <see cref="BrainSlotRegistryService"/> class.</summary>
    public BrainSlotRegistryService(
        McpDbContext db,
        IKeyServerPartyRegistry partyRegistry,
        IBrainSlotCredentialResolver credentialResolver,
        IOptionsMonitor<BrainSlotOptions> options,
        ILogger<BrainSlotRegistryService> logger)
    {
        _db = db;
        _partyRegistry = partyRegistry;
        _credentialResolver = credentialResolver;
        _options = options;
        _logger = logger;
        _partyKeyReconciler = new BrainSlotPartyKeyReconciler(partyRegistry, logger);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BrainSlotDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.BrainSlotDefinitions
            .AsNoTracking()
            .OrderBy(slot => slot.Role)
            .ThenBy(slot => slot.SlotId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToDto).ToArray();
    }

    /// <inheritdoc />
    public async Task<BrainSlotDto?> GetAsync(string slotId, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityAsync(slotId, cancellationToken).ConfigureAwait(false);
        return entity is null ? null : ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<BrainSlotDefinitionEntity?> GetEntityAsync(string slotId, CancellationToken cancellationToken = default)
    {
        var normalizedSlotId = NormalizeSlotId(slotId);
        if (normalizedSlotId is null)
            return null;

        return await _db.BrainSlotDefinitions
            .FirstOrDefaultAsync(slot => slot.SlotId == normalizedSlotId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BrainSlotDefinitionEntity?> GetEnabledEntityForRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var normalizedRole = BrainSlotValidation.NormalizeRole(role);
        return await _db.BrainSlotDefinitions
            .FirstOrDefaultAsync(slot => slot.Role == normalizedRole && slot.Enabled, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BrainSlotDto> UpsertAsync(string slotId, UpsertBrainSlotRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedSlotId = NormalizeSlotId(slotId)
            ?? throw new BrainSlotValidationException("slotId is required.");
        var now = DateTimeOffset.UtcNow;
        var role = BrainSlotValidation.NormalizeRole(request.Role);
        var providerKind = BrainSlotValidation.NormalizeProviderKind(request.ProviderKind);
        var timeout = NormalizeTimeout(request.TimeoutSeconds);
        var partyId = NormalizePartyId(request.PartyId, role);

        ValidateRequired(request.ModelId, nameof(request.ModelId));
        ValidateCredentialReference(request.CredentialReference);
        BrainSlotValidation.ValidateEndpoint(providerKind, request.Endpoint, _options);

        var entity = await _db.BrainSlotDefinitions
            .IgnoreQueryFilters(SoftDeleteFilter)
            .FirstOrDefaultAsync(slot => slot.SlotId == normalizedSlotId, cancellationToken)
            .ConfigureAwait(false);

        var action = entity is null ? "create" : "update";
        if (entity is null)
        {
            entity = new BrainSlotDefinitionEntity
            {
                SlotId = normalizedSlotId,
                CreatedAtUtc = now,
            };
            _db.BrainSlotDefinitions.Add(entity);
        }
        else
        {
            RestoreSoftDeleted(entity);
        }

        if (request.Enabled)
            await EnforceEnabledRoleUniquenessAsync(normalizedSlotId, role, request.ReplaceExisting, cancellationToken).ConfigureAwait(false);

        entity.Role = role;
        entity.DisplayName = NormalizeOptional(request.DisplayName);
        entity.ProviderKind = providerKind;
        entity.ModelId = request.ModelId.Trim();
        entity.Endpoint = NormalizeOptional(request.Endpoint);
        entity.CredentialReference = request.CredentialReference.Trim();
        entity.PartyId = partyId;
        entity.Enabled = request.Enabled;
        entity.TimeoutSeconds = timeout;
        entity.MaxOutputTokens = request.MaxOutputTokens <= 0 ? 1024 : request.MaxOutputTokens;
        entity.SystemPrompt = NormalizeOptional(request.SystemPrompt);
        if (entity.WeightVersion == 0 && entity.WeightUpdatedAtUtc is null)
            entity.OrchestrationWeight = NormalizeWeight(request.OrchestrationWeight);
        entity.UpdatedAtUtc = now;

        await RegisterPartyAsync(entity, cancellationToken).ConfigureAwait(false);
        AddAudit(action, entity, null);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Brain slot {Action}: {SlotId} ({Role})", action, entity.SlotId, entity.Role);
        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<BrainSlotDto> DeleteAsync(string slotId, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(slotId, cancellationToken).ConfigureAwait(false);
        entity.Enabled = false;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var dto = ToDto(entity);
        AddAudit("delete", entity, null);
        _db.BrainSlotDefinitions.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return dto;
    }

    /// <inheritdoc />
    public async Task<BrainSlotDto> EnableAsync(string slotId, bool replaceExisting, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(slotId, cancellationToken).ConfigureAwait(false);
        await EnforceEnabledRoleUniquenessAsync(entity.SlotId, entity.Role, replaceExisting, cancellationToken).ConfigureAwait(false);
        entity.Enabled = true;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await RegisterPartyAsync(entity, cancellationToken).ConfigureAwait(false);
        AddAudit("enable", entity, new { replaceExisting });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<BrainSlotDto> DisableAsync(string slotId, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredEntityAsync(slotId, cancellationToken).ConfigureAwait(false);
        entity.Enabled = false;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await RegisterPartyAsync(entity, cancellationToken).ConfigureAwait(false);
        AddAudit("disable", entity, null);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<BrainSlotStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.BrainSlotDefinitions
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var readiness = new Dictionary<string, bool>(StringComparer.Ordinal);
        var missing = new List<string>();
        var disabled = new List<string>();
        var errors = new List<string>();

        foreach (var role in BrainSlotRoles.All)
        {
            var roleRows = rows.Where(slot => string.Equals(slot.Role, role, StringComparison.OrdinalIgnoreCase)).ToArray();
            var enabled = roleRows.Where(slot => slot.Enabled).ToArray();
            if (roleRows.Length == 0)
            {
                missing.Add(role);
                readiness[role] = false;
                continue;
            }

            if (enabled.Length == 0)
            {
                disabled.Add(role);
                readiness[role] = false;
                continue;
            }

            if (enabled.Length > 1)
                errors.Add($"{role}: multiple enabled slots are visible.");

            var slot = enabled[0];
            var valid = await ValidateReadinessAsync(slot, errors, cancellationToken).ConfigureAwait(false);
            readiness[role] = valid && enabled.Length == 1;
        }

        return new BrainSlotStatusResponse
        {
            RoleReadiness = readiness,
            MissingRoles = missing,
            DisabledRoles = disabled,
            ValidationErrors = errors,
            QuadReady = BrainSlotRoles.All.All(role => readiness.TryGetValue(role, out var ready) && ready) && errors.Count == 0,
        };
    }

    private async Task<BrainSlotDefinitionEntity> GetRequiredEntityAsync(string slotId, CancellationToken cancellationToken)
    {
        var entity = await GetEntityAsync(slotId, cancellationToken).ConfigureAwait(false);
        return entity ?? throw new BrainSlotNotFoundException(slotId);
    }

    private async Task EnforceEnabledRoleUniquenessAsync(string slotId, string role, bool replaceExisting, CancellationToken cancellationToken)
    {
        var existing = await _db.BrainSlotDefinitions
            .Where(slot => slot.Enabled && slot.Role == role && slot.SlotId != slotId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing.Count == 0)
            return;

        if (!replaceExisting)
            throw new BrainSlotConflictException($"An enabled brain slot already exists for role '{role}'. Set replaceExisting=true to replace it.");

        foreach (var slot in existing)
        {
            slot.Enabled = false;
            slot.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await RegisterPartyAsync(slot, cancellationToken).ConfigureAwait(false);
            AddAudit("replace", slot, new { replacementSlotId = slotId });
        }
    }

    private async Task RegisterPartyAsync(BrainSlotDefinitionEntity entity, CancellationToken cancellationToken)
    {
        await _partyRegistry.RegisterPartyAsync(
            new PartyRegistrationRequest
            {
                PartyId = entity.PartyId,
                Role = BrainSlotValidation.DefaultPartyId(entity.Role),
                Status = entity.Enabled ? "active" : "disabled",
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> ValidateReadinessAsync(BrainSlotDefinitionEntity slot, List<string> errors, CancellationToken cancellationToken)
    {
        var valid = true;
        void Fail(string message)
        {
            errors.Add($"{slot.Role}/{slot.SlotId}: {message}");
            valid = false;
        }

        try { BrainSlotValidation.NormalizeProviderKind(slot.ProviderKind); } catch (BrainSlotValidationException ex) { Fail(ex.Message); }
        if (string.IsNullOrWhiteSpace(slot.ModelId)) Fail("modelId is required.");
        if (!_credentialResolver.IsSupportedReference(slot.CredentialReference)) Fail("credentialReference must use env:, config:, or file:.");
        if (string.IsNullOrWhiteSpace(slot.PartyId)) Fail("partyId is required.");
        try { BrainSlotValidation.ValidateEndpoint(slot.ProviderKind, slot.Endpoint, _options); } catch (BrainSlotValidationException ex) { Fail(ex.Message); }
        if (!string.IsNullOrWhiteSpace(slot.PartyId))
        {
            var signingKeyId = BrainSlotValidation.SigningKeyId(slot.PartyId);
            var key = await _partyRegistry.GetPartyKeyAsync(slot.PartyId, signingKeyId, cancellationToken)
                .ConfigureAwait(false);

            // TR-MCP-SEC-006: a renamed party (Creativity/Logic) may still have its signing key under the legacy
            // hemisphere party id. Copy it forward once, then re-read; the legacy rows stay untouched.
            if (key is null
                && await _partyKeyReconciler.TryAdoptLegacySigningKeyAsync(slot.PartyId, slot.Role, cancellationToken).ConfigureAwait(false))
            {
                key = await _partyRegistry.GetPartyKeyAsync(slot.PartyId, signingKeyId, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (key is null
                || !string.Equals(key.Status, "active", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(key.Purpose, "signing", StringComparison.OrdinalIgnoreCase))
            {
                Fail("trusted party signing key is missing or disabled.");
            }
        }

        return valid;
    }

    private int NormalizeTimeout(int timeoutSeconds)
    {
        var value = timeoutSeconds <= 0 ? _options.CurrentValue.DefaultTimeoutSeconds : timeoutSeconds;
        if (value <= 0)
            value = 30;
        if (value > _options.CurrentValue.MaxTimeoutSeconds)
            throw new BrainSlotValidationException($"timeoutSeconds must be <= {_options.CurrentValue.MaxTimeoutSeconds}.");
        return value;
    }

    private static double NormalizeWeight(double weight)
        => double.IsNaN(weight) || double.IsInfinity(weight) || weight <= 0 ? 1.0 : weight;

    private void ValidateCredentialReference(string reference)
    {
        if (!_credentialResolver.IsSupportedReference(reference))
            throw new BrainSlotValidationException("credentialReference must use env:, config:, or file:.");
    }

    private static void ValidateRequired(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BrainSlotValidationException($"{name} is required.");
    }

    private static string NormalizePartyId(string? partyId, string role)
        => string.IsNullOrWhiteSpace(partyId) ? BrainSlotValidation.DefaultPartyId(role) : partyId.Trim();

    private static string? NormalizeSlotId(string? slotId)
        => string.IsNullOrWhiteSpace(slotId) ? null : slotId.Trim();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void RestoreSoftDeleted(BrainSlotDefinitionEntity entity)
    {
        var entry = _db.Entry(entity);
        entry.Property("IsDeleted").CurrentValue = false;
        entry.Property("DeletedAtUtc").CurrentValue = null;
        entry.Property("DeletedBy").CurrentValue = null;
        entry.Property("DeleteReason").CurrentValue = null;
    }

    private void AddAudit(string action, BrainSlotDefinitionEntity entity, object? metadata)
    {
        _db.DataAuditLogs.Add(new DataAuditLogEntity
        {
            WorkspaceId = _db.CurrentWorkspaceId,
            EntityKind = "BrainSlotDefinition",
            EntityKey = entity.SlotId,
            Action = action,
            SourceType = nameof(BrainSlotRegistryService),
            Actor = "system",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            CurrentSnapshotJson = JsonSerializer.Serialize(ToDto(entity)),
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
        });
    }

    /// <summary>Maps a storage row to a public DTO.</summary>
    public static BrainSlotDto ToDto(BrainSlotDefinitionEntity entity)
        => new()
        {
            SlotId = entity.SlotId,
            Role = entity.Role,
            DisplayName = entity.DisplayName,
            ProviderKind = entity.ProviderKind,
            ModelId = entity.ModelId,
            Endpoint = entity.Endpoint,
            CredentialReference = entity.CredentialReference,
            PartyId = entity.PartyId,
            Enabled = entity.Enabled,
            TimeoutSeconds = entity.TimeoutSeconds,
            MaxOutputTokens = entity.MaxOutputTokens,
            SystemPrompt = entity.SystemPrompt,
            OrchestrationWeight = entity.OrchestrationWeight <= 0 ? 1.0 : entity.OrchestrationWeight,
            WeightVersion = entity.WeightVersion,
            WeightUpdatedAtUtc = entity.WeightUpdatedAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };
}
