using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-CFG-006: Resolves appsettings files, flattens the effective configuration, and patches
/// <c>appsettings.yaml</c> while reloading the active <see cref="IConfiguration"/> root.
/// </summary>
public sealed class AppSettingsFileService
{
    private static readonly IDeserializer s_deserializer = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer s_serializer = new SerializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private static readonly JsonNodeOptions s_jsonNodeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Func<string, string, CancellationToken, Task> _writeTextAsync;

    /// <summary>Initializes a new instance of the <see cref="AppSettingsFileService"/> class.</summary>
    /// <param name="configuration">The active configuration root.</param>
    /// <param name="environment">The current host environment.</param>
    public AppSettingsFileService(IConfiguration configuration, IWebHostEnvironment environment)
        : this(configuration, environment, static (path, content, ct) => File.WriteAllTextAsync(path, content, ct))
    {
    }

    internal AppSettingsFileService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        Func<string, string, CancellationToken, Task> writeTextAsync)
    {
        _configuration = configuration;
        _environment = environment;
        ArgumentNullException.ThrowIfNull(writeTextAsync);
        _writeTextAsync = writeTextAsync;
    }

    /// <summary>
    /// Returns the current effective configuration as flattened <c>section:key</c> pairs.
    /// Only non-null values are included in the result.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetConfigurationValues()
    {
        return _configuration.AsEnumerable()
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is not null)
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Last().Value!,
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves the preferred appsettings file path, preferring YAML over JSON when both are present.
    /// </summary>
    public string ResolvePreferredAppsettingsPath()
    {
        var loadedYamlPath = ResolveLoadedAppsettingsPath("appsettings.yaml");
        if (!string.IsNullOrWhiteSpace(loadedYamlPath) && File.Exists(loadedYamlPath))
            return loadedYamlPath;

        var loadedJsonPath = ResolveLoadedAppsettingsPath("appsettings.json");
        if (!string.IsNullOrWhiteSpace(loadedJsonPath) && File.Exists(loadedJsonPath))
            return loadedJsonPath;

        var contentRoot = _environment.ContentRootPath;
        var baseDir = AppContext.BaseDirectory;

        var yamlContentRoot = Path.Combine(contentRoot, "appsettings.yaml");
        if (File.Exists(yamlContentRoot))
            return yamlContentRoot;

        var yamlBaseDir = Path.Combine(baseDir, "appsettings.yaml");
        if (File.Exists(yamlBaseDir))
            return yamlBaseDir;

        var jsonContentRoot = Path.Combine(contentRoot, "appsettings.json");
        if (File.Exists(jsonContentRoot))
            return jsonContentRoot;

        var jsonBaseDir = Path.Combine(baseDir, "appsettings.json");
        if (File.Exists(jsonBaseDir))
            return jsonBaseDir;

        return yamlContentRoot;
    }

    /// <summary>
    /// Resolves the preferred <c>appsettings.yaml</c> path, falling back to the content root when the file
    /// does not yet exist.
    /// </summary>
    public string ResolveYamlAppsettingsPath()
    {
        var loadedYamlPath = ResolveLoadedAppsettingsPath("appsettings.yaml");
        if (!string.IsNullOrWhiteSpace(loadedYamlPath))
            return loadedYamlPath;

        var contentRoot = _environment.ContentRootPath;
        var baseDir = AppContext.BaseDirectory;

        var yamlContentRoot = Path.Combine(contentRoot, "appsettings.yaml");
        if (File.Exists(yamlContentRoot))
            return yamlContentRoot;

        var yamlBaseDir = Path.Combine(baseDir, "appsettings.yaml");
        if (File.Exists(yamlBaseDir))
            return yamlBaseDir;

        return yamlContentRoot;
    }

    /// <summary>
    /// Loads a YAML appsettings document into a mutable dictionary tree.
    /// </summary>
    /// <param name="path">Optional explicit YAML file path.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task<Dictionary<object, object>> LoadYamlAsync(string? path = null, CancellationToken ct = default)
    {
        var resolvedPath = path ?? ResolveYamlAppsettingsPath();
        return await LoadYamlCoreAsync(resolvedPath, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the global marker prompt template in the active appsettings file using atomic write semantics.
    /// </summary>
    /// <param name="template">The new marker prompt template identifier, or <c>null</c> to remove it.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task UpdateGlobalPromptTemplateAsync(string? template, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var resolvedPath = ResolvePreferredAppsettingsPath();
            if (resolvedPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            {
                var data = await LoadYamlCoreAsync(resolvedPath, ct).ConfigureAwait(false);
                if (!data.TryGetValue("Mcp", out var mcpObj) || mcpObj is not IDictionary<object, object> mcpDict)
                    data["Mcp"] = mcpDict = new Dictionary<object, object>();

                if (template is null)
                    mcpDict.Remove("MarkerPromptTemplate");
                else
                    mcpDict["MarkerPromptTemplate"] = template;

                await SaveYamlCoreAsync(data, resolvedPath, ct).ConfigureAwait(false);
            }
            else
            {
                var document = await LoadJsonCoreAsync(resolvedPath, ct).ConfigureAwait(false);
                var mcp = document["Mcp"] as JsonObject ?? new JsonObject();
                if (template is null)
                    mcp.Remove("MarkerPromptTemplate");
                else
                    mcp["MarkerPromptTemplate"] = template;

                document["Mcp"] = mcp;
                await SaveJsonCoreAsync(document, resolvedPath, ct).ConfigureAwait(false);
            }

            ReloadConfiguration();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task<Dictionary<object, object>> LoadYamlCoreAsync(string resolvedPath, CancellationToken ct)
    {
        if (!File.Exists(resolvedPath))
            return [];

        var yamlText = await File.ReadAllTextAsync(resolvedPath, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(yamlText))
            return [];

        return s_deserializer.Deserialize<Dictionary<object, object>>(yamlText) ?? [];
    }

    /// <summary>
    /// Saves a YAML appsettings document and reloads the active configuration root.
    /// </summary>
    /// <param name="data">The YAML document to persist.</param>
    /// <param name="path">Optional explicit YAML file path.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task SaveYamlAsync(Dictionary<object, object> data, string? path = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var resolvedPath = path ?? ResolveYamlAppsettingsPath();
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await SaveYamlCoreAsync(data, resolvedPath, ct).ConfigureAwait(false);
            ReloadConfiguration();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Applies flattened key-value patches to <c>appsettings.yaml</c>, writes the updated file, and reloads
    /// the active configuration root.
    /// </summary>
    /// <param name="updates">Flattened configuration values to set or remove.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task<IReadOnlyDictionary<string, string>> PatchYamlConfigurationAsync(
        IReadOnlyDictionary<string, string?> updates,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var resolvedPath = ResolveYamlAppsettingsPath();
            var data = await LoadYamlCoreAsync(resolvedPath, ct).ConfigureAwait(false);

            foreach (var update in updates)
                ApplyPatch(data, update.Key, update.Value);

            await SaveYamlCoreAsync(data, resolvedPath, ct).ConfigureAwait(false);
            ReloadConfiguration();
            return GetConfigurationValues();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private void ReloadConfiguration()
    {
        if (_configuration is IConfigurationRoot root)
            root.Reload();
    }

    private async Task<JsonObject> LoadJsonCoreAsync(string resolvedPath, CancellationToken ct)
    {
        if (!File.Exists(resolvedPath))
            return new JsonObject();

        var jsonText = await File.ReadAllTextAsync(resolvedPath, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(jsonText))
            return new JsonObject();

        return JsonNode.Parse(jsonText, s_jsonNodeOptions) as JsonObject ?? new JsonObject();
    }

    private async Task SaveYamlCoreAsync(Dictionary<object, object> data, string resolvedPath, CancellationToken ct)
    {
        var yamlText = s_serializer.Serialize(data);
        await WriteTextAtomicallyAsync(resolvedPath, yamlText, ct).ConfigureAwait(false);
    }

    private async Task SaveJsonCoreAsync(JsonObject data, string resolvedPath, CancellationToken ct)
    {
        var jsonText = data.ToJsonString(s_jsonOptions);
        await WriteTextAtomicallyAsync(resolvedPath, jsonText, ct).ConfigureAwait(false);
    }

    private async Task WriteTextAtomicallyAsync(string resolvedPath, string content, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(
            directory ?? AppContext.BaseDirectory,
            $".{Path.GetFileName(resolvedPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await _writeTextAsync(tempPath, content, ct).ConfigureAwait(false);
            File.Move(tempPath, resolvedPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private string? ResolveLoadedAppsettingsPath(string fileName)
    {
        if (_configuration is not IConfigurationRoot root)
            return null;

        foreach (var provider in root.Providers.OfType<FileConfigurationProvider>().Reverse())
        {
            var sourcePath = provider.Source.Path;
            if (!string.Equals(Path.GetFileName(sourcePath ?? string.Empty), fileName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(sourcePath))
                continue;

            if (Path.IsPathRooted(sourcePath))
                return Path.GetFullPath(sourcePath);

            var physicalPath = provider.Source.FileProvider?.GetFileInfo(sourcePath).PhysicalPath;
            if (!string.IsNullOrWhiteSpace(physicalPath))
                return Path.GetFullPath(physicalPath);
        }

        return null;
    }

    private static void ApplyPatch(IDictionary<object, object> root, string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var segments = key.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            throw new ArgumentException(
                "Configuration keys must contain at least one non-empty segment.",
                nameof(key));
        }

        ApplyToDictionary(root, segments, 0, value);
    }

    private static void ApplyToDictionary(IDictionary<object, object> current, string[] segments, int index, string? value)
    {
        var segment = segments[index];
        if (index == segments.Length - 1)
        {
            if (value is null)
                current.Remove(segment);
            else
                current[segment] = value;

            return;
        }

        var nextContainer = GetOrCreateChildContainer(current, segment, IsListSegment(segments[index + 1]));
        ApplyToContainer(nextContainer, segments, index + 1, value);
    }

    private static void ApplyToList(IList<object> current, string[] segments, int index, string? value)
    {
        if (!TryGetListIndex(segments[index], out var listIndex))
        {
            throw new ArgumentException(
                $"Configuration key segment '{segments[index]}' must be a non-negative list index.",
                nameof(segments));
        }

        EnsureListSize(current, listIndex + 1);
        if (index == segments.Length - 1)
        {
            current[listIndex] = value!;
            return;
        }

        var nextContainer = GetOrCreateChildContainer(current, listIndex, IsListSegment(segments[index + 1]));
        ApplyToContainer(nextContainer, segments, index + 1, value);
    }

    private static void ApplyToContainer(object container, string[] segments, int index, string? value)
    {
        switch (container)
        {
            case IDictionary<object, object> dictionary:
                ApplyToDictionary(dictionary, segments, index, value);
                return;
            case IList<object> list:
                ApplyToList(list, segments, index, value);
                return;
            default:
                throw new InvalidOperationException(
                    $"Unsupported YAML node type '{container.GetType().FullName}' while patching configuration.");
        }
    }

    private static object GetOrCreateChildContainer(
        IDictionary<object, object> current,
        string key,
        bool nextIsList)
    {
        if (current.TryGetValue(key, out var existing) && IsCompatibleContainer(existing, nextIsList))
            return existing;

        object created = nextIsList ? new List<object>() : new Dictionary<object, object>();
        current[key] = created;
        return created;
    }

    private static object GetOrCreateChildContainer(IList<object> current, int index, bool nextIsList)
    {
        var existing = current[index];
        if (IsCompatibleContainer(existing, nextIsList))
            return existing;

        object created = nextIsList ? new List<object>() : new Dictionary<object, object>();
        current[index] = created;
        return created;
    }

    private static bool IsCompatibleContainer(object? value, bool expectList)
    {
        return expectList
            ? value is IList<object>
            : value is IDictionary<object, object>;
    }

    private static bool IsListSegment(string segment)
    {
        return TryGetListIndex(segment, out _);
    }

    private static bool TryGetListIndex(string segment, out int index)
    {
        return int.TryParse(segment, out index) && index >= 0;
    }

    private static void EnsureListSize(IList<object> list, int requiredSize)
    {
        while (list.Count < requiredSize)
            list.Add(string.Empty);
    }
}
