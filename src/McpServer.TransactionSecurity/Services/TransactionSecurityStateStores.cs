using System.Collections.Concurrent;
using McpServer.TransactionSecurity.Models;
using Microsoft.EntityFrameworkCore;

namespace McpServer.TransactionSecurity.Services;

internal interface IKeyServerStateStore : IDisposable
{
    Task SavePartyAsync(
        PartyRegistrationResponse party,
        IReadOnlyCollection<PartyKeyDescriptor> keys,
        CancellationToken cancellationToken);

    Task<KeyServerPartyState?> GetPartyAsync(string partyId, CancellationToken cancellationToken);

    Task<PartyKeyDescriptor?> GetPartyKeyAsync(string partyId, string keyId, CancellationToken cancellationToken);

    Task SaveManifestAsync(TransactionManifestTraceRecord manifest, CancellationToken cancellationToken);

    Task<TransactionManifestTraceRecord?> GetManifestAsync(string transactionId, CancellationToken cancellationToken);

    Task<TransactionFailureReason> TryReserveManifestReplayAsync(
        string scope,
        string pairKey,
        long sequence,
        string nonceKey,
        string transactionId,
        CancellationToken cancellationToken);

    Task RecordAuditAsync(
        string eventName,
        string? transactionId,
        TransactionFailureReason reason,
        string? details,
        CancellationToken cancellationToken);
}

internal interface ISubscriberStateStore : IDisposable
{
    Task<SubscriberTransactionState?> GetTransactionAsync(string transactionId, CancellationToken cancellationToken);

    Task<bool> TryAddTransactionAsync(SubscriberTransactionState transaction, CancellationToken cancellationToken);

    Task<SubscriberTransactionState> AddOrKeepAbortAsync(
        SubscriberTransactionState transaction,
        CancellationToken cancellationToken);

    Task<long?> GetLastSequenceAsync(string pairKey, CancellationToken cancellationToken);

    Task SetLastSequenceAsync(string pairKey, long sequence, CancellationToken cancellationToken);

    Task<bool> TryAddNonceAsync(string nonceKey, string transactionId, CancellationToken cancellationToken);

    Task RecordAuditAsync(
        string eventName,
        string? transactionId,
        TransactionFailureReason reason,
        string? details,
        CancellationToken cancellationToken);
}

internal sealed record KeyServerPartyState(
    PartyRegistrationResponse Party,
    IReadOnlyList<PartyKeyDescriptor> Keys);

internal sealed record SubscriberTransactionState(
    string TransactionId,
    string Status,
    TransactionFailureReason Reason,
    string ManifestHashSha256,
    string EncryptedBodySha256,
    string? DiffgramId,
    DateTimeOffset? CommittedAtUtc,
    DateTimeOffset? AbortedAtUtc);

internal sealed class InMemoryKeyServerStateStore : IKeyServerStateStore
{
    private readonly ConcurrentDictionary<string, KeyServerPartyState> _parties = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TransactionManifestTraceRecord> _manifests = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _lastSequences = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _nonces = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<TransactionAuditState> _audit = new();
    private readonly object _replayGate = new();

    public Task SavePartyAsync(
        PartyRegistrationResponse party,
        IReadOnlyCollection<PartyKeyDescriptor> keys,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _parties.AddOrUpdate(
            party.PartyId,
            _ => new KeyServerPartyState(Clone(party), keys.Select(Clone).ToArray()),
            (_, existing) => new KeyServerPartyState(Clone(party), MergeKeys(existing.Keys, keys)));
        return Task.CompletedTask;
    }

    public Task<KeyServerPartyState?> GetPartyAsync(string partyId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _parties.TryGetValue(partyId, out var party)
                ? new KeyServerPartyState(Clone(party.Party), party.Keys.Select(Clone).ToArray())
                : null);
    }

    public Task<PartyKeyDescriptor?> GetPartyKeyAsync(
        string partyId,
        string keyId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _parties.TryGetValue(partyId, out var party)
                ? party.Keys.FirstOrDefault(key => string.Equals(key.KeyId, keyId, StringComparison.OrdinalIgnoreCase)) is { } key
                    ? Clone(key)
                    : null
                : null);
    }

    public Task SaveManifestAsync(
        TransactionManifestTraceRecord manifest,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _manifests[manifest.TransactionId] = Clone(manifest);
        return Task.CompletedTask;
    }

    public Task<TransactionManifestTraceRecord?> GetManifestAsync(
        string transactionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _manifests.TryGetValue(transactionId, out var manifest)
                ? Clone(manifest)
                : null);
    }

    public Task<TransactionFailureReason> TryReserveManifestReplayAsync(
        string scope,
        string pairKey,
        long sequence,
        string nonceKey,
        string transactionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scopedPairKey = BuildScopedReplayKey(scope, pairKey);
        var scopedNonceKey = BuildScopedReplayKey(scope, nonceKey);
        lock (_replayGate)
        {
            if (_nonces.ContainsKey(scopedNonceKey))
                return Task.FromResult(TransactionFailureReason.ReplayNonce);
            if (_lastSequences.TryGetValue(scopedPairKey, out var lastSequence) && sequence <= lastSequence)
                return Task.FromResult(TransactionFailureReason.StaleSequence);

            _nonces[scopedNonceKey] = transactionId;
            _lastSequences[scopedPairKey] = sequence;
            return Task.FromResult(TransactionFailureReason.None);
        }
    }

    public Task RecordAuditAsync(
        string eventName,
        string? transactionId,
        TransactionFailureReason reason,
        string? details,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _audit.Enqueue(new TransactionAuditState(eventName, transactionId, reason, details, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    private static PartyRegistrationResponse Clone(PartyRegistrationResponse response)
        => new()
        {
            PartyId = response.PartyId,
            Role = response.Role,
            ActiveSigningKeyId = response.ActiveSigningKeyId,
            ActiveEncryptionKeyId = response.ActiveEncryptionKeyId,
            Status = response.Status,
            CreatedAtUtc = response.CreatedAtUtc,
            UpdatedAtUtc = response.UpdatedAtUtc,
        };

    private static PartyKeyDescriptor Clone(PartyKeyDescriptor descriptor)
        => new()
        {
            PartyId = descriptor.PartyId,
            KeyId = descriptor.KeyId,
            Purpose = descriptor.Purpose,
            Algorithm = descriptor.Algorithm,
            PublicKeyPem = descriptor.PublicKeyPem,
            Status = descriptor.Status,
            CreatedAtUtc = descriptor.CreatedAtUtc,
            ExpiresAtUtc = descriptor.ExpiresAtUtc,
        };

    private static TransactionManifestTraceRecord Clone(TransactionManifestTraceRecord manifest)
        => new()
        {
            TransactionId = manifest.TransactionId,
            TurnId = manifest.TurnId,
            PublisherPartyId = manifest.PublisherPartyId,
            PublisherSigningKeyId = manifest.PublisherSigningKeyId,
            SubscriberPartyId = manifest.SubscriberPartyId,
            SubscriberEncryptionKeyId = manifest.SubscriberEncryptionKeyId,
            Sequence = manifest.Sequence,
            Nonce = manifest.Nonce,
            IssuedAtUtc = manifest.IssuedAtUtc,
            ExpiresAtUtc = manifest.ExpiresAtUtc,
            DiffgramSha256 = manifest.DiffgramSha256,
            EncryptedBodySha256 = manifest.EncryptedBodySha256,
            SignatureAlgorithm = manifest.SignatureAlgorithm,
            EncryptionAlgorithm = manifest.EncryptionAlgorithm,
            CanonicalizationProfile = manifest.CanonicalizationProfile,
            SignatureKeyId = manifest.SignatureKeyId,
            SignatureValue = manifest.SignatureValue,
            SignedAtUtc = manifest.SignedAtUtc,
            ManifestHashSha256 = manifest.ManifestHashSha256,
            Status = manifest.Status,
            CreatedAtUtc = manifest.CreatedAtUtc,
        };

    private static IReadOnlyList<PartyKeyDescriptor> MergeKeys(
        IReadOnlyList<PartyKeyDescriptor> existingKeys,
        IReadOnlyCollection<PartyKeyDescriptor> incomingKeys)
    {
        var merged = existingKeys.Select(Clone).ToList();
        foreach (var incomingKey in incomingKeys)
        {
            var existingIndex = merged.FindIndex(
                key => string.Equals(key.KeyId, incomingKey.KeyId, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
                merged[existingIndex] = Clone(incomingKey);
            else
                merged.Add(Clone(incomingKey));
        }

        return merged;
    }

    private static string BuildScopedReplayKey(string scope, string value)
        => $"{scope.Trim()}\n{value}";
}

internal sealed class InMemorySubscriberStateStore : ISubscriberStateStore
{
    private readonly ConcurrentDictionary<string, SubscriberTransactionState> _transactions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _lastSequences = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _nonces = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<TransactionAuditState> _audit = new();

    public Task<SubscriberTransactionState?> GetTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_transactions.TryGetValue(transactionId, out var transaction) ? transaction : null);
    }

    public Task<bool> TryAddTransactionAsync(
        SubscriberTransactionState transaction,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_transactions.TryAdd(transaction.TransactionId, transaction));
    }

    public Task<SubscriberTransactionState> AddOrKeepAbortAsync(
        SubscriberTransactionState transaction,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = _transactions.AddOrUpdate(
            transaction.TransactionId,
            transaction,
            (_, existing) => string.Equals(existing.Status, "committed", StringComparison.OrdinalIgnoreCase)
                ? existing
                : transaction);
        return Task.FromResult(current);
    }

    public Task<long?> GetLastSequenceAsync(string pairKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_lastSequences.TryGetValue(pairKey, out var sequence) ? sequence : (long?)null);
    }

    public Task SetLastSequenceAsync(string pairKey, long sequence, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _lastSequences.AddOrUpdate(pairKey, sequence, (_, current) => Math.Max(current, sequence));
        return Task.CompletedTask;
    }

    public Task<bool> TryAddNonceAsync(
        string nonceKey,
        string transactionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_nonces.TryAdd(nonceKey, transactionId));
    }

    public Task RecordAuditAsync(
        string eventName,
        string? transactionId,
        TransactionFailureReason reason,
        string? details,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _audit.Enqueue(new TransactionAuditState(eventName, transactionId, reason, details, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}

internal sealed class SqliteTransactionSecurityStateStore : IKeyServerStateStore, ISubscriberStateStore
{
    private readonly DbContextOptions<TransactionSecurityDbContext> _options;

    public SqliteTransactionSecurityStateStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        _options = new DbContextOptionsBuilder<TransactionSecurityDbContext>()
            .UseSqlite($"Data Source={fullPath}")
            .Options;

        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public async Task SavePartyAsync(
        PartyRegistrationResponse party,
        IReadOnlyCollection<PartyKeyDescriptor> keys,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        var existing = await db.KeyServerParties
            .Include(entity => entity.Keys)
            .SingleOrDefaultAsync(entity => entity.PartyId == party.PartyId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            existing = new KeyServerPartyEntity { PartyId = party.PartyId };
            db.KeyServerParties.Add(existing);
        }

        existing.Role = party.Role;
        existing.ActiveSigningKeyId = party.ActiveSigningKeyId;
        existing.ActiveEncryptionKeyId = party.ActiveEncryptionKeyId;
        existing.Status = party.Status;
        existing.CreatedAtUtc = party.CreatedAtUtc;
        existing.UpdatedAtUtc = party.UpdatedAtUtc;

        foreach (var key in keys)
        {
            var existingKey = existing.Keys.SingleOrDefault(
                current => string.Equals(current.KeyId, key.KeyId, StringComparison.OrdinalIgnoreCase));
            if (existingKey is null)
                db.KeyServerPartyKeys.Add(ToEntity(key));
            else
                UpdateEntity(existingKey, key);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeyServerPartyState?> GetPartyAsync(
        string partyId,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        var entity = await db.KeyServerParties
            .AsNoTracking()
            .Include(party => party.Keys)
            .SingleOrDefaultAsync(party => party.PartyId == partyId, cancellationToken)
            .ConfigureAwait(false);
        return entity is null
            ? null
            : new KeyServerPartyState(ToModel(entity), entity.Keys.Select(ToModel).ToArray());
    }

    public async Task<PartyKeyDescriptor?> GetPartyKeyAsync(
        string partyId,
        string keyId,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        var entity = await db.KeyServerPartyKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(
                key => key.PartyId == partyId && key.KeyId == keyId,
                cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : ToModel(entity);
    }

    public async Task SaveManifestAsync(
        TransactionManifestTraceRecord manifest,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        var existing = await db.KeyServerManifests
            .SingleOrDefaultAsync(entity => entity.TransactionId == manifest.TransactionId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
            db.KeyServerManifests.Add(ToEntity(manifest));
        else
            UpdateEntity(existing, manifest);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TransactionManifestTraceRecord?> GetManifestAsync(
        string transactionId,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        var entity = await db.KeyServerManifests
            .AsNoTracking()
            .SingleOrDefaultAsync(manifest => manifest.TransactionId == transactionId, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<TransactionFailureReason> TryReserveManifestReplayAsync(
        string scope,
        string pairKey,
        long sequence,
        string nonceKey,
        string transactionId,
        CancellationToken cancellationToken)
    {
        var scopedPairKey = BuildScopedReplayKey(scope, pairKey);
        var scopedNonceKey = BuildScopedReplayKey(scope, nonceKey);
        await using var db = CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var nonceExists = await db.KeyServerNonces
            .AsNoTracking()
            .AnyAsync(existing => existing.NonceKey == scopedNonceKey, cancellationToken)
            .ConfigureAwait(false);
        if (nonceExists)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TransactionFailureReason.ReplayNonce;
        }

        var sequenceEntity = await db.KeyServerSequences
            .SingleOrDefaultAsync(existing => existing.PairKey == scopedPairKey, cancellationToken)
            .ConfigureAwait(false);
        if (sequenceEntity is not null && sequence <= sequenceEntity.LastSequence)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TransactionFailureReason.StaleSequence;
        }

        db.KeyServerNonces.Add(new KeyServerNonceEntity
        {
            NonceKey = scopedNonceKey,
            TransactionId = transactionId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        if (sequenceEntity is null)
        {
            db.KeyServerSequences.Add(new KeyServerSequenceEntity
            {
                PairKey = scopedPairKey,
                LastSequence = sequence,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            sequenceEntity.LastSequence = sequence;
            sequenceEntity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return TransactionFailureReason.None;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TransactionFailureReason.ReplayNonce;
        }
    }

    public async Task<SubscriberTransactionState?> GetTransactionAsync(
        string transactionId,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        var entity = await db.SubscriberTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(transaction => transaction.TransactionId == transactionId, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : ToState(entity);
    }

    public async Task<bool> TryAddTransactionAsync(
        SubscriberTransactionState transaction,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        db.SubscriberTransactions.Add(ToEntity(transaction));
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task<SubscriberTransactionState> AddOrKeepAbortAsync(
        SubscriberTransactionState transaction,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        var existing = await db.SubscriberTransactions
            .SingleOrDefaultAsync(entity => entity.TransactionId == transaction.TransactionId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            db.SubscriberTransactions.Add(ToEntity(transaction));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return transaction;
        }

        if (string.Equals(existing.Status, "committed", StringComparison.OrdinalIgnoreCase))
            return ToState(existing);

        UpdateEntity(existing, transaction);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return transaction;
    }

    public async Task<long?> GetLastSequenceAsync(
        string pairKey,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        var entity = await db.SubscriberSequences
            .AsNoTracking()
            .SingleOrDefaultAsync(sequence => sequence.PairKey == pairKey, cancellationToken)
            .ConfigureAwait(false);
        return entity?.LastSequence;
    }

    public async Task SetLastSequenceAsync(
        string pairKey,
        long sequence,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        var entity = await db.SubscriberSequences
            .SingleOrDefaultAsync(existing => existing.PairKey == pairKey, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
            db.SubscriberSequences.Add(new SubscriberSequenceEntity { PairKey = pairKey, LastSequence = sequence });
        else
            entity.LastSequence = Math.Max(entity.LastSequence, sequence);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryAddNonceAsync(
        string nonceKey,
        string transactionId,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        db.SubscriberNonces.Add(new SubscriberNonceEntity
        {
            NonceKey = nonceKey,
            TransactionId = transactionId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task RecordAuditAsync(
        string eventName,
        string? transactionId,
        TransactionFailureReason reason,
        string? details,
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();
        db.TransactionAuditEvents.Add(new TransactionAuditEntity
        {
            EventName = eventName,
            TransactionId = transactionId,
            Reason = reason,
            Details = details,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
    }

    private TransactionSecurityDbContext CreateContext()
        => new(_options);

    private static PartyRegistrationResponse ToModel(KeyServerPartyEntity entity)
        => new()
        {
            PartyId = entity.PartyId,
            Role = entity.Role,
            ActiveSigningKeyId = entity.ActiveSigningKeyId,
            ActiveEncryptionKeyId = entity.ActiveEncryptionKeyId,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
        };

    private static PartyKeyDescriptor ToModel(KeyServerPartyKeyEntity entity)
        => new()
        {
            PartyId = entity.PartyId,
            KeyId = entity.KeyId,
            Purpose = entity.Purpose,
            Algorithm = entity.Algorithm,
            PublicKeyPem = entity.PublicKeyPem,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
        };

    private static KeyServerPartyKeyEntity ToEntity(PartyKeyDescriptor key)
        => new()
        {
            PartyId = key.PartyId,
            KeyId = key.KeyId,
            Purpose = key.Purpose,
            Algorithm = key.Algorithm,
            PublicKeyPem = key.PublicKeyPem,
            Status = key.Status,
            CreatedAtUtc = key.CreatedAtUtc,
            ExpiresAtUtc = key.ExpiresAtUtc,
        };

    private static void UpdateEntity(KeyServerPartyKeyEntity entity, PartyKeyDescriptor key)
    {
        entity.Purpose = key.Purpose;
        entity.Algorithm = key.Algorithm;
        entity.PublicKeyPem = key.PublicKeyPem;
        entity.Status = key.Status;
        entity.CreatedAtUtc = key.CreatedAtUtc;
        entity.ExpiresAtUtc = key.ExpiresAtUtc;
    }

    private static TransactionManifestTraceRecord ToModel(KeyServerManifestEntity entity)
        => new()
        {
            TransactionId = entity.TransactionId,
            TurnId = entity.TurnId,
            PublisherPartyId = entity.PublisherPartyId,
            PublisherSigningKeyId = entity.PublisherSigningKeyId,
            SubscriberPartyId = entity.SubscriberPartyId,
            SubscriberEncryptionKeyId = entity.SubscriberEncryptionKeyId,
            Sequence = entity.Sequence,
            Nonce = entity.Nonce,
            IssuedAtUtc = entity.IssuedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            DiffgramSha256 = entity.DiffgramSha256,
            EncryptedBodySha256 = entity.EncryptedBodySha256,
            SignatureAlgorithm = entity.SignatureAlgorithm,
            EncryptionAlgorithm = entity.EncryptionAlgorithm,
            CanonicalizationProfile = entity.CanonicalizationProfile,
            SignatureKeyId = entity.SignatureKeyId,
            SignatureValue = entity.SignatureValue,
            SignedAtUtc = entity.SignedAtUtc,
            ManifestHashSha256 = entity.ManifestHashSha256,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
        };

    private static KeyServerManifestEntity ToEntity(TransactionManifestTraceRecord manifest)
        => new()
        {
            TransactionId = manifest.TransactionId,
            TurnId = manifest.TurnId,
            PublisherPartyId = manifest.PublisherPartyId,
            PublisherSigningKeyId = manifest.PublisherSigningKeyId,
            SubscriberPartyId = manifest.SubscriberPartyId,
            SubscriberEncryptionKeyId = manifest.SubscriberEncryptionKeyId,
            Sequence = manifest.Sequence,
            Nonce = manifest.Nonce,
            IssuedAtUtc = manifest.IssuedAtUtc,
            ExpiresAtUtc = manifest.ExpiresAtUtc,
            DiffgramSha256 = manifest.DiffgramSha256,
            EncryptedBodySha256 = manifest.EncryptedBodySha256,
            SignatureAlgorithm = manifest.SignatureAlgorithm,
            EncryptionAlgorithm = manifest.EncryptionAlgorithm,
            CanonicalizationProfile = manifest.CanonicalizationProfile,
            SignatureKeyId = manifest.SignatureKeyId,
            SignatureValue = manifest.SignatureValue,
            SignedAtUtc = manifest.SignedAtUtc,
            ManifestHashSha256 = manifest.ManifestHashSha256,
            Status = manifest.Status,
            CreatedAtUtc = manifest.CreatedAtUtc,
        };

    private static void UpdateEntity(KeyServerManifestEntity entity, TransactionManifestTraceRecord manifest)
    {
        entity.TurnId = manifest.TurnId;
        entity.PublisherPartyId = manifest.PublisherPartyId;
        entity.PublisherSigningKeyId = manifest.PublisherSigningKeyId;
        entity.SubscriberPartyId = manifest.SubscriberPartyId;
        entity.SubscriberEncryptionKeyId = manifest.SubscriberEncryptionKeyId;
        entity.Sequence = manifest.Sequence;
        entity.Nonce = manifest.Nonce;
        entity.IssuedAtUtc = manifest.IssuedAtUtc;
        entity.ExpiresAtUtc = manifest.ExpiresAtUtc;
        entity.DiffgramSha256 = manifest.DiffgramSha256;
        entity.EncryptedBodySha256 = manifest.EncryptedBodySha256;
        entity.SignatureAlgorithm = manifest.SignatureAlgorithm;
        entity.EncryptionAlgorithm = manifest.EncryptionAlgorithm;
        entity.CanonicalizationProfile = manifest.CanonicalizationProfile;
        entity.SignatureKeyId = manifest.SignatureKeyId;
        entity.SignatureValue = manifest.SignatureValue;
        entity.SignedAtUtc = manifest.SignedAtUtc;
        entity.ManifestHashSha256 = manifest.ManifestHashSha256;
        entity.Status = manifest.Status;
        entity.CreatedAtUtc = manifest.CreatedAtUtc;
    }

    private static SubscriberTransactionState ToState(SubscriberTransactionEntity entity)
        => new(
            entity.TransactionId,
            entity.Status,
            entity.Reason,
            entity.ManifestHashSha256,
            entity.EncryptedBodySha256,
            entity.DiffgramId,
            entity.CommittedAtUtc,
            entity.AbortedAtUtc);

    private static SubscriberTransactionEntity ToEntity(SubscriberTransactionState transaction)
        => new()
        {
            TransactionId = transaction.TransactionId,
            Status = transaction.Status,
            Reason = transaction.Reason,
            ManifestHashSha256 = transaction.ManifestHashSha256,
            EncryptedBodySha256 = transaction.EncryptedBodySha256,
            DiffgramId = transaction.DiffgramId,
            CommittedAtUtc = transaction.CommittedAtUtc,
            AbortedAtUtc = transaction.AbortedAtUtc,
        };

    private static void UpdateEntity(
        SubscriberTransactionEntity entity,
        SubscriberTransactionState transaction)
    {
        entity.Status = transaction.Status;
        entity.Reason = transaction.Reason;
        entity.ManifestHashSha256 = transaction.ManifestHashSha256;
        entity.EncryptedBodySha256 = transaction.EncryptedBodySha256;
        entity.DiffgramId = transaction.DiffgramId;
        entity.CommittedAtUtc = transaction.CommittedAtUtc;
        entity.AbortedAtUtc = transaction.AbortedAtUtc;
    }

    private static string BuildScopedReplayKey(string scope, string value)
        => $"{scope.Trim()}\n{value}";
}

internal sealed record TransactionAuditState(
    string EventName,
    string? TransactionId,
    TransactionFailureReason Reason,
    string? Details,
    DateTimeOffset CreatedAtUtc);

internal sealed class TransactionSecurityDbContext : DbContext
{
    public TransactionSecurityDbContext(DbContextOptions<TransactionSecurityDbContext> options)
        : base(options)
    {
    }

    public DbSet<KeyServerPartyEntity> KeyServerParties => Set<KeyServerPartyEntity>();

    public DbSet<KeyServerPartyKeyEntity> KeyServerPartyKeys => Set<KeyServerPartyKeyEntity>();

    public DbSet<KeyServerSequenceEntity> KeyServerSequences => Set<KeyServerSequenceEntity>();

    public DbSet<KeyServerNonceEntity> KeyServerNonces => Set<KeyServerNonceEntity>();

    public DbSet<KeyServerManifestEntity> KeyServerManifests => Set<KeyServerManifestEntity>();

    public DbSet<SubscriberTransactionEntity> SubscriberTransactions => Set<SubscriberTransactionEntity>();

    public DbSet<SubscriberSequenceEntity> SubscriberSequences => Set<SubscriberSequenceEntity>();

    public DbSet<SubscriberNonceEntity> SubscriberNonces => Set<SubscriberNonceEntity>();

    public DbSet<TransactionAuditEntity> TransactionAuditEvents => Set<TransactionAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KeyServerPartyEntity>(entity =>
        {
            entity.ToTable("TransactionKeyServerParties");
            entity.HasKey(party => party.PartyId);
            entity.Property(party => party.PartyId).HasMaxLength(200);
            entity.Property(party => party.Role).HasMaxLength(100);
            entity.Property(party => party.ActiveSigningKeyId).HasMaxLength(200);
            entity.Property(party => party.ActiveEncryptionKeyId).HasMaxLength(200);
            entity.Property(party => party.Status).HasMaxLength(50);
            entity.HasMany(party => party.Keys)
                .WithOne()
                .HasForeignKey(key => key.PartyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KeyServerPartyKeyEntity>(entity =>
        {
            entity.ToTable("TransactionKeyServerPartyKeys");
            entity.HasKey(key => new { key.PartyId, key.KeyId });
            entity.Property(key => key.PartyId).HasMaxLength(200);
            entity.Property(key => key.KeyId).HasMaxLength(200);
            entity.Property(key => key.Purpose).HasMaxLength(50);
            entity.Property(key => key.Algorithm).HasMaxLength(100);
            entity.Property(key => key.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<KeyServerSequenceEntity>(entity =>
        {
            entity.ToTable("TransactionKeyServerSequences");
            entity.HasKey(sequence => sequence.PairKey);
        });

        modelBuilder.Entity<KeyServerNonceEntity>(entity =>
        {
            entity.ToTable("TransactionKeyServerNonces");
            entity.HasKey(nonce => nonce.NonceKey);
            entity.Property(nonce => nonce.TransactionId).HasMaxLength(200);
        });

        modelBuilder.Entity<KeyServerManifestEntity>(entity =>
        {
            entity.ToTable("TransactionKeyServerManifests");
            entity.HasKey(manifest => manifest.TransactionId);
            entity.Property(manifest => manifest.TransactionId).HasMaxLength(200);
            entity.Property(manifest => manifest.TurnId).HasMaxLength(200);
            entity.Property(manifest => manifest.PublisherPartyId).HasMaxLength(200);
            entity.Property(manifest => manifest.PublisherSigningKeyId).HasMaxLength(200);
            entity.Property(manifest => manifest.SubscriberPartyId).HasMaxLength(200);
            entity.Property(manifest => manifest.SubscriberEncryptionKeyId).HasMaxLength(200);
            entity.Property(manifest => manifest.DiffgramSha256).HasMaxLength(64);
            entity.Property(manifest => manifest.EncryptedBodySha256).HasMaxLength(64);
            entity.Property(manifest => manifest.SignatureAlgorithm).HasMaxLength(100);
            entity.Property(manifest => manifest.EncryptionAlgorithm).HasMaxLength(120);
            entity.Property(manifest => manifest.CanonicalizationProfile).HasMaxLength(120);
            entity.Property(manifest => manifest.SignatureKeyId).HasMaxLength(200);
            entity.Property(manifest => manifest.ManifestHashSha256).HasMaxLength(64);
            entity.Property(manifest => manifest.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<SubscriberTransactionEntity>(entity =>
        {
            entity.ToTable("TransactionSubscriberTransactions");
            entity.HasKey(transaction => transaction.TransactionId);
            entity.Property(transaction => transaction.TransactionId).HasMaxLength(200);
            entity.Property(transaction => transaction.Status).HasMaxLength(50);
            entity.Property(transaction => transaction.ManifestHashSha256).HasMaxLength(64);
            entity.Property(transaction => transaction.EncryptedBodySha256).HasMaxLength(64);
            entity.Property(transaction => transaction.DiffgramId).HasMaxLength(240);
        });

        modelBuilder.Entity<SubscriberSequenceEntity>(entity =>
        {
            entity.ToTable("TransactionSubscriberSequences");
            entity.HasKey(sequence => sequence.PairKey);
        });

        modelBuilder.Entity<SubscriberNonceEntity>(entity =>
        {
            entity.ToTable("TransactionSubscriberNonces");
            entity.HasKey(nonce => nonce.NonceKey);
            entity.Property(nonce => nonce.TransactionId).HasMaxLength(200);
        });

        modelBuilder.Entity<TransactionAuditEntity>(entity =>
        {
            entity.ToTable("TransactionAuditEvents");
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.EventName).HasMaxLength(120);
            entity.Property(audit => audit.TransactionId).HasMaxLength(200);
        });
    }
}

internal sealed class KeyServerPartyEntity
{
    public string PartyId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? ActiveSigningKeyId { get; set; }

    public string? ActiveEncryptionKeyId { get; set; }

    public string Status { get; set; } = "active";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public List<KeyServerPartyKeyEntity> Keys { get; } = [];
}

internal sealed class KeyServerPartyKeyEntity
{
    public string PartyId { get; set; } = string.Empty;

    public string KeyId { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public string Algorithm { get; set; } = string.Empty;

    public string PublicKeyPem { get; set; } = string.Empty;

    public string Status { get; set; } = "active";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }
}

internal sealed class KeyServerSequenceEntity
{
    public string PairKey { get; set; } = string.Empty;

    public long LastSequence { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class KeyServerNonceEntity
{
    public string NonceKey { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class KeyServerManifestEntity
{
    public string TransactionId { get; set; } = string.Empty;

    public string? TurnId { get; set; }

    public string PublisherPartyId { get; set; } = string.Empty;

    public string? PublisherSigningKeyId { get; set; }

    public string SubscriberPartyId { get; set; } = string.Empty;

    public string? SubscriberEncryptionKeyId { get; set; }

    public long Sequence { get; set; }

    public string Nonce { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string DiffgramSha256 { get; set; } = string.Empty;

    public string EncryptedBodySha256 { get; set; } = string.Empty;

    public string SignatureAlgorithm { get; set; } = string.Empty;

    public string EncryptionAlgorithm { get; set; } = string.Empty;

    public string CanonicalizationProfile { get; set; } = string.Empty;

    public string SignatureKeyId { get; set; } = string.Empty;

    public string SignatureValue { get; set; } = string.Empty;

    public DateTimeOffset SignedAtUtc { get; set; }

    public string ManifestHashSha256 { get; set; } = string.Empty;

    public string Status { get; set; } = "signed";

    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class SubscriberTransactionEntity
{
    public string TransactionId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public TransactionFailureReason Reason { get; set; }

    public string ManifestHashSha256 { get; set; } = string.Empty;

    public string EncryptedBodySha256 { get; set; } = string.Empty;

    public string? DiffgramId { get; set; }

    public DateTimeOffset? CommittedAtUtc { get; set; }

    public DateTimeOffset? AbortedAtUtc { get; set; }
}

internal sealed class SubscriberSequenceEntity
{
    public string PairKey { get; set; } = string.Empty;

    public long LastSequence { get; set; }
}

internal sealed class SubscriberNonceEntity
{
    public string NonceKey { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class TransactionAuditEntity
{
    public long Id { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string? TransactionId { get; set; }

    public TransactionFailureReason Reason { get; set; }

    public string? Details { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
