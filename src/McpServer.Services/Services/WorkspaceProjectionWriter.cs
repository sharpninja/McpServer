using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-DB-001: Writes the informational appsettings workspace projection
/// after the canonical database workspace registry has been committed.
/// </summary>
public interface IWorkspaceProjectionWriter
{
    /// <summary>Writes the non-secret workspace projection to appsettings.</summary>
    Task WriteProjectionAsync(IReadOnlyList<WorkspaceConfigEntry> workspaces, CancellationToken ct);
}

/// <summary>
/// TR-MCP-DB-001: Appsettings projection writer for database-authoritative
/// workspace registrations.
/// </summary>
public sealed class WorkspaceProjectionWriter : IWorkspaceProjectionWriter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceProjectionWriter"/> class.</summary>
    public WorkspaceProjectionWriter(IConfiguration configuration, IHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    /// <inheritdoc />
    public async Task WriteProjectionAsync(IReadOnlyList<WorkspaceConfigEntry> workspaces, CancellationToken ct)
    {
        var sanitized = workspaces
            .Select(w => new WorkspaceConfigEntry
            {
                WorkspacePath = w.WorkspacePath,
                Name = w.Name,
                TodoPath = w.TodoPath,
                DataDirectory = w.DataDirectory,
                TunnelProvider = w.TunnelProvider,
                RunAs = w.RunAs,
                IsPrimary = w.IsPrimary,
                IsEnabled = w.IsEnabled,
                PromptTemplate = w.PromptTemplate,
                StatusPrompt = w.StatusPrompt,
                ImplementPrompt = w.ImplementPrompt,
                PlanPrompt = w.PlanPrompt,
                BannedLicenses = w.BannedLicenses,
                BannedCountriesOfOrigin = w.BannedCountriesOfOrigin,
                BannedOrganizations = w.BannedOrganizations,
                BannedIndividuals = w.BannedIndividuals,
                AgentPath = w.AgentPath,
                DateTimeCreated = w.DateTimeCreated,
                DateTimeModified = w.DateTimeModified,
            })
            .ToList();

        var path = ResolveAppsettingsPath();
        if (path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
        {
            await WriteAllYamlAsync(path, sanitized, ct).ConfigureAwait(false);
        }
        else
        {
            var jsonText = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            var doc = JsonNode.Parse(jsonText, new JsonNodeOptions { PropertyNameCaseInsensitive = true })!;
            var mcp = doc["Mcp"] as JsonObject ?? new JsonObject();
            mcp["Workspaces"] = JsonSerializer.SerializeToNode(sanitized, s_jsonOptions);
            doc["Mcp"] = mcp;
            await File.WriteAllTextAsync(path, doc.ToJsonString(s_jsonOptions), ct).ConfigureAwait(false);
        }

        if (_configuration is IConfigurationRoot root)
            root.Reload();
    }

    /// <summary>Resolves the appsettings file that owns the workspace projection.</summary>
    internal string ResolveAppsettingsPath()
    {
        var contentRoot = _env.ContentRootPath;
        var baseDir = AppContext.BaseDirectory;

        var yamlContentRoot = Path.Combine(contentRoot, "appsettings.yaml");
        if (File.Exists(yamlContentRoot)) return yamlContentRoot;

        var jsonContentRoot = Path.Combine(contentRoot, "appsettings.json");
        if (File.Exists(jsonContentRoot)) return jsonContentRoot;

        var yamlBaseDir = Path.Combine(baseDir, "appsettings.yaml");
        if (File.Exists(yamlBaseDir)) return yamlBaseDir;

        var jsonBaseDir = Path.Combine(baseDir, "appsettings.json");
        if (File.Exists(jsonBaseDir)) return jsonBaseDir;

        return jsonContentRoot;
    }

    private static async Task WriteAllYamlAsync(string path, List<WorkspaceConfigEntry> workspaces, CancellationToken ct)
    {
        var yamlText = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var data = deserializer.Deserialize<Dictionary<string, object>>(yamlText);
        if (!data.TryGetValue("Mcp", out var mcpObj) || mcpObj is not IDictionary<object, object> mcpDict)
        {
            data["Mcp"] = mcpDict = new Dictionary<object, object>();
        }

        mcpDict["Workspaces"] = workspaces;
        var output = serializer.Serialize(data);
        await File.WriteAllTextAsync(path, output, ct).ConfigureAwait(false);
    }
}
