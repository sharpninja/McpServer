namespace McpServer.QBAgent.Skills;

/// <summary>
/// FR-MCP-QBSKILLS-001 / FR-MCP-QBSKILLS-003: A registry of agentskills.io skills discovered from one or more
/// root directories (workspace skills plus any vendored roots such as dotnet/skills). Implements progressive
/// disclosure: <see cref="Discover"/> returns only name+description; <see cref="Load"/> returns the full body on
/// demand. Skills are found by recursively locating <c>SKILL.md</c> files, so both flat (<c>skills/x/SKILL.md</c>)
/// and nested (<c>plugins/p/skills/x/SKILL.md</c>) layouts are supported.
/// </summary>
public interface ISkillRegistry
{
    /// <summary>Returns the discovery list (name + description) for every valid skill across all roots.</summary>
    /// <returns>The ordered discovery list.</returns>
    IReadOnlyList<SkillSummary> Discover();

    /// <summary>Loads a skill's full manifest (including instruction body) by name.</summary>
    /// <param name="name">The skill name (case-insensitive).</param>
    /// <returns>The manifest, or <see langword="null"/> when no such skill exists.</returns>
    SkillManifest? Load(string name);
}

/// <summary>FR-MCP-QBSKILLS-001: Filesystem-backed <see cref="ISkillRegistry"/> over a fixed set of roots.</summary>
public sealed class SkillRegistry : ISkillRegistry
{
    private readonly IReadOnlyList<string> _roots;
    private readonly ISkillManifestParser _parser;
    private readonly Func<string, string> _readFile;
    private readonly object _gate = new();
    private IReadOnlyDictionary<string, SkillManifest>? _skills;

    /// <summary>Initializes a new instance of the <see cref="SkillRegistry"/> class.</summary>
    /// <param name="roots">Root directories to scan, in priority order (earlier roots win on name conflict).</param>
    /// <param name="parser">The manifest parser.</param>
    /// <param name="readFile">Optional file reader override (defaults to <see cref="File.ReadAllText(string)"/>); used for testing.</param>
    public SkillRegistry(IEnumerable<string> roots, ISkillManifestParser parser, Func<string, string>? readFile = null)
    {
        ArgumentNullException.ThrowIfNull(roots);
        _roots = roots.Where(static r => !string.IsNullOrWhiteSpace(r)).ToList();
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _readFile = readFile ?? File.ReadAllText;
    }

    /// <inheritdoc />
    public IReadOnlyList<SkillSummary> Discover()
        => EnsureScanned().Values
            .Select(static m => new SkillSummary(m.Name, m.Description))
            .OrderBy(static s => s.Name, StringComparer.Ordinal)
            .ToList();

    /// <inheritdoc />
    public SkillManifest? Load(string name)
        => !string.IsNullOrWhiteSpace(name) && EnsureScanned().TryGetValue(name.Trim(), out var manifest)
            ? manifest
            : null;

    private IReadOnlyDictionary<string, SkillManifest> EnsureScanned()
    {
        if (_skills is not null)
            return _skills;

        lock (_gate)
        {
            _skills ??= Scan();
        }

        return _skills;
    }

    private IReadOnlyDictionary<string, SkillManifest> Scan()
    {
        var map = new Dictionary<string, SkillManifest>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in _roots)
        {
            if (!Directory.Exists(root))
                continue;

            foreach (var file in Directory.EnumerateFiles(root, "SKILL.md", SearchOption.AllDirectories))
            {
                string content;
                try
                {
                    content = _readFile(file);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                if (_parser.TryParse(content, file, out var manifest, out _) && manifest is not null)
                    map.TryAdd(manifest.Name, manifest); // earlier roots win on conflict
            }
        }

        return map;
    }
}
