using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace QBrainAi.Support.Mcp.Options;

/// <summary>
/// TR-MCP-QBRAIN-005: Projects QBrainAi and Mcp keys onto Mcp using layer precedence.
/// Command line wins over environment, which wins over file QBrainAi, which wins over file Mcp.
/// Values are computed from the live provider list so reload and later test overrides stay current.
/// </summary>
internal sealed class CanonicalSectionProjectionSource : IConfigurationSource
{
    private readonly IConfigurationRoot _root;

    /// <summary>Creates a source bound to the configuration root that owns the other providers.</summary>
    public CanonicalSectionProjectionSource(IConfigurationRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new CanonicalSectionProjectionProvider(_root);
}

/// <summary>
/// TR-MCP-QBRAIN-005: Last-added view of the effective Mcp section.
/// Within one layer, QBrainAi beats Mcp. A higher layer of either prefix beats a lower layer.
/// </summary>
internal sealed class CanonicalSectionProjectionProvider : ConfigurationProvider, IDisposable
{
    private const string CanonicalPrefix = "QBrainAi:";
    private const string LegacyPrefix = "Mcp:";
    private readonly IConfigurationRoot _root;
    private readonly object _gate = new();
    private readonly List<IDisposable> _subscriptions = [];
    private readonly List<IChangeToken> _watchedTokens = [];
    private Dictionary<string, string?>? _cache;
    private int _watchedProviderCount;
    private bool _subscribed;

    /// <summary>Creates a provider that reads sibling sources from <paramref name="root"/>.</summary>
    public CanonicalSectionProjectionProvider(IConfigurationRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }

    /// <inheritdoc />
    public override void Load()
    {
        EnsureSubscriptions();
    }

    /// <inheritdoc />
    public override bool TryGet(string key, out string? value)
    {
        return CurrentMap().TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        var prefix = parentPath is null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;
        return CurrentMap()
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(pair => Segment(pair.Key, prefix))
            .Concat(earlierKeys)
            .OrderBy(key => key, ConfigurationKeyComparer.Instance);
    }

    /// <summary>Reads one projected Mcp suffix, ignoring values contributed by raw QBrainAi providers.</summary>
    public bool TryGetProjected(string suffix, out string? value)
    {
        ArgumentNullException.ThrowIfNull(suffix);
        return CurrentMap().TryGetValue(LegacyPrefix + suffix, out value);
    }

    /// <summary>Returns the effective product section (Mcp prefix removed) from the current projection.</summary>
    public IConfiguration CreateProductConfiguration()
    {
        var product = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in CurrentMap())
        {
            if (pair.Key.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase))
                product[pair.Key[LegacyPrefix.Length..]] = pair.Value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(product).Build();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
            _subscriptions.Clear();
            _subscribed = false;
        }
    }

    private Dictionary<string, string?> CurrentMap()
    {
        lock (_gate)
        {
            return CurrentMapCore();
        }
    }

    private Dictionary<string, string?> CurrentMapCore()
    {
        var providerCount = 0;
        foreach (var provider in _root.Providers)
            providerCount++;
        if (_cache is not null
            && providerCount == _watchedProviderCount
            && _watchedTokens.TrueForAll(token => !token.HasChanged))
            return _cache;

        var providers = new List<FlattenedProvider>();
        var ordered = _root.Providers.ToList();
        var selfIndex = -1;
        for (var index = 0; index < ordered.Count; index++)
        {
            if (ReferenceEquals(ordered[index], this))
            {
                selfIndex = index;
                break;
            }
        }

        var seen = new HashSet<IConfigurationProvider>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < ordered.Count; index++)
        {
            if (index == selfIndex)
                continue;
            foreach (var provider in Flatten(ordered[index], seen))
            {
                providers.Add(new FlattenedProvider(provider, Classify(provider), selfIndex >= 0 && index > selfIndex, index));
            }
        }

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in BuildBatches(providers))
            Overlay(result, batch);

        _watchedTokens.Clear();
        foreach (var provider in _root.Providers)
        {
            if (!ReferenceEquals(provider, this))
                _watchedTokens.Add(provider.GetReloadToken());
        }

        _watchedProviderCount = providerCount;
        _cache = result;
        return result;
    }

    private void EnsureSubscriptions()
    {
        lock (_gate)
        {
            if (_subscribed)
                return;
            _subscribed = true;
            foreach (var provider in _root.Providers)
            {
                if (ReferenceEquals(provider, this) || provider is CanonicalSectionProjectionProvider)
                    continue;
                var captured = provider;
                _subscriptions.Add(ChangeToken.OnChange(captured.GetReloadToken, OnReload));
            }
        }
    }

    private static IEnumerable<ProviderBatch> BuildBatches(List<FlattenedProvider> providers)
    {
        foreach (var layer in new[] { SourceLayer.File, SourceLayer.Memory, SourceLayer.Environment, SourceLayer.CommandLine })
        {
            var before = providers.Where(item => !item.AfterProjection && item.Layer == layer).Select(item => item.Provider).ToList();
            if (before.Count > 0)
                yield return new ProviderBatch(before, CanonicalBeatsLegacy: true, ReplaceWorkspaces: layer == SourceLayer.File);
        }

        var afterGroups = providers
            .Where(item => item.AfterProjection)
            .GroupBy(item => item.Order)
            .OrderBy(group => group.Key);
        foreach (var group in afterGroups)
        {
            yield return new ProviderBatch(group.Select(item => item.Provider).ToList(), CanonicalBeatsLegacy: true, ReplaceWorkspaces: false);
        }
    }

    private static void Overlay(Dictionary<string, string?> result, ProviderBatch batch)
    {
        var canonical = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var legacy = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in batch.Providers)
        {
            foreach (var path in EnumeratePaths(provider))
            {
                if (!provider.TryGet(path, out var value))
                    continue;
                if (!TrySplit(path, out var isCanonical, out var suffix))
                    continue;
                (isCanonical ? canonical : legacy)[suffix] = value;
            }
        }

        var canonicalWorkspaces = canonical.Where(pair => IsWorkspaceSuffix(pair.Key)).ToList();
        var legacyWorkspaces = legacy.Where(pair => IsWorkspaceSuffix(pair.Key)).ToList();
        if (batch.ReplaceWorkspaces && canonicalWorkspaces.Count > 0)
        {
            RemoveWorkspaceKeys(result);
            foreach (var pair in canonicalWorkspaces)
                result[LegacyPrefix + pair.Key] = pair.Value;
        }
        else if (canonicalWorkspaces.Count > 0 || legacyWorkspaces.Count > 0)
        {
            var owned = ArrayParents(canonicalWorkspaces.Select(pair => pair.Key));
            foreach (var pair in legacyWorkspaces)
            {
                if (batch.CanonicalBeatsLegacy && IsUnderOwnedArray(pair.Key, owned))
                    continue;
                result[LegacyPrefix + pair.Key] = pair.Value;
            }

            if (batch.CanonicalBeatsLegacy)
            {
                foreach (var pair in canonicalWorkspaces)
                    result[LegacyPrefix + pair.Key] = pair.Value;
            }
        }

        foreach (var pair in legacy)
        {
            if (IsWorkspaceSuffix(pair.Key))
                continue;
            if (batch.CanonicalBeatsLegacy && canonical.ContainsKey(pair.Key))
                continue;
            result[LegacyPrefix + pair.Key] = pair.Value;
        }

        if (batch.CanonicalBeatsLegacy)
        {
            foreach (var pair in canonical)
            {
                if (IsWorkspaceSuffix(pair.Key))
                    continue;
                result[LegacyPrefix + pair.Key] = pair.Value;
            }
        }
    }

    private static IEnumerable<IConfigurationProvider> Flatten(IConfigurationProvider provider, HashSet<IConfigurationProvider> seen)
    {
        if (!seen.Add(provider) || provider is CanonicalSectionProjectionProvider)
            yield break;

        var type = provider.GetType();
        if (type.Name.Contains("Chained", StringComparison.Ordinal))
        {
            var configuration = type.GetProperty("Configuration")?.GetValue(provider) as IConfigurationRoot;
            if (configuration is not null)
            {
                foreach (var inner in configuration.Providers)
                {
                    foreach (var flattened in Flatten(inner, seen))
                        yield return flattened;
                }

                yield break;
            }
        }

        yield return provider;
    }

    private static SourceLayer Classify(IConfigurationProvider provider)
    {
        var name = provider.GetType().Name;
        if (name.Contains("CommandLine", StringComparison.Ordinal))
            return SourceLayer.CommandLine;
        if (name.Contains("Environment", StringComparison.Ordinal))
            return SourceLayer.Environment;
        if (name.Contains("Memory", StringComparison.Ordinal))
            return SourceLayer.Memory;
        return SourceLayer.File;
    }

    private static IEnumerable<string> EnumeratePaths(IConfigurationProvider provider)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Collect(provider, parent: null, keys);
        return keys;
    }

    private static void Collect(IConfigurationProvider provider, string? parent, ISet<string> keys)
    {
        foreach (var child in provider.GetChildKeys(Enumerable.Empty<string>(), parent))
        {
            var path = string.IsNullOrEmpty(parent) ? child : parent + ConfigurationPath.KeyDelimiter + child;
            if (!keys.Add(path))
                continue;
            Collect(provider, path, keys);
        }
    }

    private static bool TrySplit(string path, out bool canonical, out string suffix)
    {
        if (path.StartsWith(CanonicalPrefix, StringComparison.OrdinalIgnoreCase))
        {
            canonical = true;
            suffix = path[CanonicalPrefix.Length..];
            return suffix.Length > 0;
        }

        if (path.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            canonical = false;
            suffix = path[LegacyPrefix.Length..];
            return suffix.Length > 0;
        }

        canonical = false;
        suffix = string.Empty;
        return false;
    }

    private static bool IsWorkspaceSuffix(string suffix)
        => suffix.Equals("Workspaces", StringComparison.OrdinalIgnoreCase)
           || suffix.StartsWith("Workspaces:", StringComparison.OrdinalIgnoreCase);

    private static void RemoveWorkspaceKeys(Dictionary<string, string?> result)
    {
        foreach (var key in result.Keys.Where(IsWorkspaceResultKey).ToList())
            result.Remove(key);
    }

    private static bool IsWorkspaceResultKey(string key)
        => key.Equals("Mcp:Workspaces", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Mcp:Workspaces:", StringComparison.OrdinalIgnoreCase);

    private static HashSet<string> ArrayParents(IEnumerable<string> suffixes)
    {
        var parents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var suffix in suffixes)
        {
            var parts = suffix.Split(':');
            for (var index = 0; index < parts.Length; index++)
            {
                if (!int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out _))
                    continue;
                parents.Add(string.Join(':', parts.Take(index)));
            }
        }

        return parents;
    }

    private static bool IsUnderOwnedArray(string suffix, HashSet<string> parents)
    {
        var parts = suffix.Split(':');
        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out _))
                continue;
            if (parents.Contains(string.Join(':', parts.Take(index))))
                return true;
        }

        return false;
    }

    private static string Segment(string key, string prefix)
    {
        var index = key.IndexOf(ConfigurationPath.KeyDelimiter, prefix.Length, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? key[prefix.Length..] : key[prefix.Length..index];
    }

    private enum SourceLayer
    {
        File = 1,
        Memory = 2,
        Environment = 3,
        CommandLine = 4,
    }

    private readonly record struct FlattenedProvider(IConfigurationProvider Provider, SourceLayer Layer, bool AfterProjection, int Order);

    private readonly record struct ProviderBatch(IReadOnlyList<IConfigurationProvider> Providers, bool CanonicalBeatsLegacy, bool ReplaceWorkspaces);
}
