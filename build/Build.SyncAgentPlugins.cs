using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Serilog;

partial class Build
{
    [Parameter("Directory containing mcpserver-*-plugin repositories; defaults to the repository parent directory")]
    readonly string? AgentPluginParent = null;

    private static readonly JsonSerializerOptions s_pluginJsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string[] s_knownAgentPluginRepositories =
    [
        "mcpserver-claude-code-plugin",
        "mcpserver-claude-cowork-plugin",
        "mcpserver-cline-plugin",
        "mcpserver-cline-v2-plugin",
        "mcpserver-codex-plugin",
        "mcpserver-copilot-plugin",
        "mcpserver-grok-plugin",
        "mcpserver-opencode-plugin"
    ];

    /// <summary>Sync the canonical plugin core into sibling agent plugins and normalize plugin package versions.</summary>
    public Target SyncAgentPlugins => _ => _
        .Executes(() =>
        {
            var syncScript = RootDirectory / "plugins" / "core" / "sync" / "sync-plugin-core.ps1";
            if (!File.Exists(syncScript.ToString()))
                throw new FileNotFoundException("Plugin core sync script was not found.", syncScript.ToString());

            var wrapperScript = RootDirectory / "plugins" / "core" / "hooks-templates" / "generate-wrappers.ps1";
            if (!File.Exists(wrapperScript.ToString()))
                throw new FileNotFoundException("Plugin wrapper generator was not found.", wrapperScript.ToString());

            var stagedRoot = RootDirectory / "plugins" / "core" / ".staged-plugin";
            Directory.CreateDirectory(stagedRoot.ToString());
            SyncPluginCorePackage(RootDirectory, stagedRoot, syncScript, wrapperScript, "claude-code");

            var pluginRoots = DiscoverAgentPluginRoots(RootDirectory, AgentPluginParent);
            if (pluginRoots.Count == 0)
            {
                Log.Warning("No known agent plugin repositories were found under {PluginParent}. Synced only the workspace staged plugin package.", ResolveAgentPluginParentDirectory(RootDirectory, AgentPluginParent));
                return;
            }

            foreach (var pluginRoot in pluginRoots)
            {
                SyncPluginCorePackage(RootDirectory, pluginRoot, syncScript, wrapperScript, ResolvePluginHostName(pluginRoot));
            }

            RefreshNodePluginCoreVendorPackages(RootDirectory, pluginRoots);

            var nextVersion = ResolveNextMinorPluginVersion(pluginRoots);
            var updates = PlanPluginVersionUpdates(pluginRoots, nextVersion);
            foreach (var update in updates)
            {
                File.WriteAllText(update.Path, update.UpdatedContent);
                Log.Information("Updated plugin version in {Path} to {Version}", update.Path, nextVersion);
            }

            RefreshKnownPluginCaches(pluginRoots, nextVersion);
        });

    /// <summary>Validate that generated plugin runtime packages are PowerShell-only.</summary>
    public Target ValidatePluginPowerShellOnly => _ => _
        .Executes(() =>
        {
            var roots = DiscoverAgentPluginRoots(RootDirectory, AgentPluginParent)
                .Concat([RootDirectory / "plugins" / "core" / ".staged-plugin"])
                .Where(path => Directory.Exists(path.ToString()))
                .ToArray();

            foreach (var root in roots)
                ValidatePluginPowerShellOnlyPackage(root);

            Log.Information("PowerShell-only plugin validation passed for {Count} package roots.", roots.Length);
        });

    /// <summary>Plan a common minor version bump from the highest version found in plugin manifests and package files.</summary>
    internal static string ResolveNextMinorPluginVersion(IReadOnlyList<AbsolutePath> pluginRoots)
    {
        ArgumentNullException.ThrowIfNull(pluginRoots);

        PluginSemanticVersion? highest = null;
        foreach (var path in EnumerateVersionedFiles(pluginRoots))
        {
            foreach (var version in ReadVersionValues(path))
            {
                if (!TryParseSemanticVersion(version, out var parsed))
                    continue;

                if (highest is null || parsed.CompareTo(highest.Value) > 0)
                    highest = parsed;
            }
        }

        if (highest is null)
            throw new InvalidOperationException("Could not find a semantic version in plugin manifests or package files.");

        return $"{highest.Value.Major}.{highest.Value.Minor + 1}.0";
    }

    /// <summary>Create deterministic JSON rewrites for plugin manifests and root package files.</summary>
    internal static IReadOnlyList<PluginVersionUpdate> PlanPluginVersionUpdates(
        IReadOnlyList<AbsolutePath> pluginRoots,
        string version)
    {
        ArgumentNullException.ThrowIfNull(pluginRoots);
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Plugin version is required.", nameof(version));

        var updates = new List<PluginVersionUpdate>();
        foreach (var path in EnumerateVersionedFiles(pluginRoots))
        {
            var originalContent = File.ReadAllText(path);
            var updatedContent = RewriteVersionedFile(path, originalContent, version.Trim());
            if (!string.Equals(originalContent, updatedContent, StringComparison.Ordinal))
            {
                updates.Add(new PluginVersionUpdate(
                    NormalizePath(path),
                    originalContent,
                    updatedContent));
            }
        }

        return updates
            .OrderBy(update => update.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>A planned plugin version file rewrite.</summary>
    internal sealed record PluginVersionUpdate(string Path, string OriginalContent, string UpdatedContent);

    internal static IReadOnlyList<AbsolutePath> DiscoverAgentPluginRoots(AbsolutePath rootDirectory, string? pluginParent = null)
    {
        var parentDirectory = ResolveAgentPluginParentDirectory(rootDirectory, pluginParent);
        if (string.IsNullOrWhiteSpace(parentDirectory))
            return [];

        return s_knownAgentPluginRepositories
            .Select(name => (AbsolutePath)Path.Combine(parentDirectory, name))
            .Where(path => Directory.Exists(path.ToString()))
            .OrderBy(path => path.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string? ResolveAgentPluginParentDirectory(AbsolutePath rootDirectory, string? pluginParent = null)
    {
        if (!string.IsNullOrWhiteSpace(pluginParent))
            return Path.GetFullPath(pluginParent);

        return Directory.GetParent(rootDirectory.ToString())?.FullName;
    }

    private static string? ResolvePluginHostName(AbsolutePath pluginRoot)
    {
        var repoName = Path.GetFileName(pluginRoot.ToString().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return repoName switch
        {
            "mcpserver-claude-code-plugin" => "claude-code",
            "mcpserver-claude-cowork-plugin" => "claude-cowork",
            "mcpserver-codex-plugin" => "codex",
            "mcpserver-copilot-plugin" => "copilot",
            "mcpserver-grok-plugin" => "grok",
            _ => null
        };
    }

    private static void SyncPluginCorePackage(
        AbsolutePath rootDirectory,
        AbsolutePath pluginRoot,
        AbsolutePath syncScript,
        AbsolutePath wrapperScript,
        string? hostName)
    {
        Log.Information("Syncing plugin core into {PluginRoot}", pluginRoot);
        ProcessTasks.StartProcess(
                "pwsh.exe",
                $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{syncScript}\" -PluginRoot \"{pluginRoot}\"")
            .AssertZeroExitCode();

        if (!string.IsNullOrWhiteSpace(hostName))
        {
            Log.Information("Generating {HostName} PowerShell wrappers in {PluginRoot}", hostName, pluginRoot);
            ProcessTasks.StartProcess(
                    "pwsh.exe",
                    $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{wrapperScript}\" -HostName \"{hostName}\" -PluginRoot \"{pluginRoot}\"")
                .AssertZeroExitCode();
        }
        else
        {
            Log.Information("No hook wrappers are generated for {PluginRoot}", pluginRoot);
        }

        RunPluginCoreIntegrityCheck(rootDirectory, pluginRoot);
        ValidatePluginPowerShellOnlyPackage(pluginRoot);
    }

    private static IEnumerable<string> EnumerateVersionedFiles(IReadOnlyList<AbsolutePath> pluginRoots)
    {
        foreach (var root in pluginRoots)
        {
            var rootPath = root.ToString();
            if (!Directory.Exists(rootPath))
                continue;

            var rootVersionFile = Path.Combine(rootPath, ".version");
            if (File.Exists(rootVersionFile))
                yield return rootVersionFile;

            foreach (var file in Directory.EnumerateFiles(rootPath, "plugin.json", SearchOption.AllDirectories)
                         .Concat(Directory.EnumerateFiles(rootPath, "package.json", SearchOption.AllDirectories))
                         .Concat(Directory.EnumerateFiles(rootPath, "package-lock.json", SearchOption.AllDirectories))
                         .Where(IsVersionPlanCandidate)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }

    private static bool IsVersionPlanCandidate(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(static segment =>
                segment.Equals(".git", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("cache", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var fileName = Path.GetFileName(path);
        if (fileName.Equals("plugin.json", StringComparison.OrdinalIgnoreCase))
            return true;

        return (fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("package-lock.json", StringComparison.OrdinalIgnoreCase))
            && Path.GetDirectoryName(path)?.EndsWith("-plugin", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static IEnumerable<string> ReadVersionValues(string path)
    {
        if (Path.GetFileName(path).Equals(".version", StringComparison.OrdinalIgnoreCase))
        {
            var rootVersion = File.ReadAllText(path).Trim();
            if (!string.IsNullOrWhiteSpace(rootVersion))
                yield return rootVersion;
            yield break;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            yield break;
        }

        if (root is not JsonObject obj)
            yield break;

        var version = obj["version"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(version))
            yield return version;

        if (Path.GetFileName(path).Equals("package-lock.json", StringComparison.OrdinalIgnoreCase)
            && obj["packages"]?[""] is JsonObject rootPackage)
        {
            var packageVersion = rootPackage["version"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(packageVersion))
                yield return packageVersion;
        }
    }

    private static string RewriteVersionedFile(string path, string originalContent, string version)
    {
        if (Path.GetFileName(path).Equals(".version", StringComparison.OrdinalIgnoreCase))
            return version + "\n";

        return RewriteVersionedJson(path, originalContent, version);
    }

    private static string RewriteVersionedJson(string path, string originalContent, string version)
    {
        JsonObject root;
        try
        {
            root = JsonNode.Parse(originalContent) as JsonObject
                ?? throw new InvalidOperationException($"Expected a JSON object in {path}.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON in plugin version file '{path}': {ex.Message}", ex);
        }

        root["version"] = version;

        if (Path.GetFileName(path).Equals("package-lock.json", StringComparison.OrdinalIgnoreCase)
            && root["packages"]?[""] is JsonObject rootPackage)
        {
            rootPackage["version"] = version;
        }

        var json = root.ToJsonString(s_pluginJsonOptions)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
        return json + "\n";
    }

    private static bool TryParseSemanticVersion(string? value, out PluginSemanticVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var core = value.Split(['-', '+'], 2, StringSplitOptions.TrimEntries)[0];
        var parts = core.Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || parts.Length > 3)
            return false;

        if (!int.TryParse(parts[0], out var major)
            || !int.TryParse(parts[1], out var minor)
            || (parts.Length == 3 && !int.TryParse(parts[2], out _)))
        {
            return false;
        }

        var patch = parts.Length == 3 ? int.Parse(parts[2]) : 0;
        version = new PluginSemanticVersion(major, minor, patch);
        return true;
    }

    private static void RunPluginCoreIntegrityCheck(AbsolutePath rootDirectory, AbsolutePath pluginRoot)
    {
        if (!Directory.Exists(pluginRoot.ToString()))
            return;

        var script = rootDirectory / "plugins" / "core" / "sync" / "check-core-integrity.ps1";
        if (!File.Exists(script.ToString()))
        {
            Log.Warning("Plugin core integrity script was not found at {Script}; skipping.", script);
            return;
        }

        ProcessTasks.StartProcess(
                "pwsh.exe",
                $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{script}\" -PluginRoot \"{pluginRoot}\"")
            .AssertZeroExitCode();
    }

    private static void ValidatePluginPowerShellOnlyPackage(AbsolutePath pluginRoot)
    {
        if (!Directory.Exists(pluginRoot.ToString()))
            return;

        var forbidden = Directory.EnumerateFiles(pluginRoot.ToString(), "*", SearchOption.AllDirectories)
            .Where(path => !IsUnderIgnoredDirectory(path))
            .Select(path => new
            {
                FullPath = path,
                Relative = Path.GetRelativePath(pluginRoot.ToString(), path).Replace('\\', '/')
            })
            .Where(file => !file.Relative.Split('/').Any(static segment => segment.Equals("tests", StringComparison.OrdinalIgnoreCase)))
            .Where(file => file.Relative.StartsWith("lib/", StringComparison.OrdinalIgnoreCase)
                || file.Relative.StartsWith("hooks/", StringComparison.OrdinalIgnoreCase)
                || file.Relative.StartsWith("skills/", StringComparison.OrdinalIgnoreCase)
                || file.Relative.Equals("plugin.json", StringComparison.OrdinalIgnoreCase)
                || file.Relative.Equals("CORE-MANIFEST.yaml", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Relative.Equals("bootstrap/install-powershell.sh", StringComparison.OrdinalIgnoreCase))
            .Where(file => HasForbiddenPluginRuntimeReference(file.Relative, file.FullPath))
            .Select(file => file.Relative)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (forbidden.Length > 0)
            throw new InvalidOperationException($"Plugin package {pluginRoot} contains forbidden runtime files or references: {string.Join(", ", forbidden.Take(20))}");

        ValidateRequirementsSkillLayerGuidance(pluginRoot.ToString());
    }

    internal static void ValidateRequirementsSkillLayerGuidance(string pluginRoot)
    {
        var skillPath = Path.Combine(pluginRoot, "skills", "requirements", "SKILL.md");
        if (!File.Exists(skillPath))
            return;

        var content = File.ReadAllText(skillPath);
        var requiredFragments = new[]
        {
            "## Requirement Scope Layers",
            "workflow.requirements.listLayers",
            "workflow.requirements.createLayer",
            "workflow.requirements.updateLayer",
            "workflow.requirements.effective",
            "req_list_layers",
            "req_create_layer",
            "req_update_layer",
            "req_effective",
            "client.Requirements.ListRequirementLayersAsync",
            "client.Requirements.CreateRequirementLayerAsync",
            "client.Requirements.UpdateRequirementLayerAsync",
            "client.Requirements.GetEffectiveRequirementsAsync",
            "key",
            "order",
            "name",
            "description",
            "scopeStartLayerKey",
            "scopeEndLayerKey",
            "layerKey"
        };

        var missing = requiredFragments
            .Where(fragment => !content.Contains(fragment, StringComparison.Ordinal))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Plugin package {pluginRoot} requirements skill is missing requirement layer guidance: {string.Join(", ", missing)}");
        }
    }

    private static bool HasForbiddenPluginRuntimeReference(string relative, string fullPath)
    {
        if (relative.Contains("/lib-sh/", StringComparison.OrdinalIgnoreCase)
            || relative.Contains("/lib-node/", StringComparison.OrdinalIgnoreCase)
            || relative.EndsWith(".sh", StringComparison.OrdinalIgnoreCase)
            || relative.EndsWith(".bash", StringComparison.OrdinalIgnoreCase)
            || relative.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(fullPath);
        if (!new[] { ".json", ".md", ".ps1", ".psm1", ".psd1", ".yaml", ".yml", ".txt" }
            .Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var content = File.ReadAllText(fullPath);
        return Regex.IsMatch(
            content,
            @"(\bbash\b|\blib-sh\b|\blib-node\b|\bnode\s|\bnode\.exe\b|repl-daemon\.js|complete-turn-to-recovery\.js|\.sh\b|\.bash\b|repl-invoke\.sh|mcpserver-repl --agent-stdio|repl_invoke)",
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// TR-MCP-SYNC-001: version-less stable vendor file name, so consumer package.json references
    /// never break on a version bump and a hard-coded version can never drift from package.json again.
    /// </summary>
    const string NodeCoreStableVendorFileName = "sharpninja-mcpserver-plugin-core.tgz";

    private static void RefreshNodePluginCoreVendorPackages(
        AbsolutePath rootDirectory,
        IReadOnlyList<AbsolutePath> pluginRoots)
    {
        // TR-MCP-SYNC-001: discover consumers by ANY tarball variant so first-run migration finds
        // the legacy version-suffixed files as well as the stable name.
        var vendorDirectories = pluginRoots
            .Select(root => root / "vendor")
            .Where(dir => Directory.Exists(dir.ToString())
                && Directory.GetFiles(dir.ToString(), "sharpninja-mcpserver-plugin-core*.tgz").Length > 0)
            .ToArray();
        if (vendorDirectories.Length == 0)
            return;

        var nodeCoreRoot = rootDirectory / "plugins" / "core" / "lib-node";
        if (!Directory.Exists(nodeCoreRoot.ToString()))
            throw new DirectoryNotFoundException($"Node plugin core source was not found at {nodeCoreRoot}.");

        var manifestVersion = ReadNodeCorePackageVersion(nodeCoreRoot);

        Log.Information("Building Node plugin core package for {Count} plugin vendor target(s)", vendorDirectories.Length);
        ProcessTasks.StartProcess("npm", "run build", workingDirectory: nodeCoreRoot.ToString())
            .AssertZeroExitCode();

        var pack = ProcessTasks.StartProcess(
            "npm",
            "pack --silent",
            workingDirectory: nodeCoreRoot.ToString(),
            logOutput: false);
        pack.WaitForExit();
        pack.AssertZeroExitCode();

        var packedFileName = pack.Output
            .Select(static output => output.Text.Trim())
            .LastOrDefault(static text => text.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(packedFileName))
            throw new FileNotFoundException("npm pack did not report a packed Node plugin core tarball name.");

        // TR-MCP-SYNC-001: npm pack derives the tarball name from package.json, so a mismatch here
        // means the pack ran against a different manifest than the one this sync read. Fail loudly.
        var expectedPackedFileName = $"sharpninja-mcpserver-plugin-core-{manifestVersion}.tgz";
        if (!string.Equals(packedFileName, expectedPackedFileName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"npm pack produced '{packedFileName}' but plugins/core/lib-node/package.json declares version {manifestVersion} (expected '{expectedPackedFileName}').");
        }

        var packedPath = nodeCoreRoot / packedFileName;
        if (!File.Exists(packedPath.ToString()))
            throw new FileNotFoundException("npm pack did not produce the expected Node plugin core package.", packedPath.ToString());

        foreach (var vendorDirectory in vendorDirectories)
        {
            var stableTarget = vendorDirectory / NodeCoreStableVendorFileName;
            File.Copy(packedPath.ToString(), stableTarget.ToString(), overwrite: true);

            // Remove superseded variants (the legacy versioned names) so exactly one tarball remains.
            foreach (var variant in Directory.GetFiles(vendorDirectory.ToString(), "sharpninja-mcpserver-plugin-core*.tgz"))
            {
                if (!string.Equals(Path.GetFileName(variant), NodeCoreStableVendorFileName, StringComparison.OrdinalIgnoreCase))
                    File.Delete(variant);
            }

            RewriteVendorDependencyReference(vendorDirectory.Parent);
            Log.Information("Refreshed Node plugin core vendor package {Target} (content {Version})", stableTarget, manifestVersion);
        }

        File.Delete(packedPath.ToString());
    }

    /// <summary>Reads the version declared by plugins/core/lib-node/package.json.</summary>
    /// <param name="nodeCoreRoot">Node plugin core source directory.</param>
    /// <returns>The manifest version string.</returns>
    private static string ReadNodeCorePackageVersion(AbsolutePath nodeCoreRoot)
    {
        var manifestPath = nodeCoreRoot / "package.json";
        using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifestPath.ToString()));
        var version = manifest.RootElement.TryGetProperty("version", out var value) ? value.GetString() : null;
        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException($"package.json at {manifestPath} declares no version.");
        return version;
    }

    /// <summary>
    /// TR-MCP-SYNC-001: rewrites a consumer's package.json dependency reference from any versioned
    /// vendor tarball path to the stable version-less name, preserving the file's formatting via a
    /// targeted in-place replacement.
    /// </summary>
    /// <param name="pluginRoot">Consumer plugin repository root containing package.json.</param>
    private static void RewriteVendorDependencyReference(AbsolutePath pluginRoot)
    {
        var packageJsonPath = pluginRoot / "package.json";
        if (!File.Exists(packageJsonPath.ToString()))
            return;

        var content = File.ReadAllText(packageJsonPath.ToString());
        var rewritten = Regex.Replace(
            content,
            @"file:vendor/sharpninja-mcpserver-plugin-core[^""]*\.tgz",
            $"file:vendor/{NodeCoreStableVendorFileName}");
        if (!string.Equals(content, rewritten, StringComparison.Ordinal))
        {
            File.WriteAllText(packageJsonPath.ToString(), rewritten);
            Log.Information("Rewrote Node core vendor dependency reference in {PackageJson}", packageJsonPath);
        }
    }

    private static void RefreshKnownPluginCaches(IReadOnlyList<AbsolutePath> pluginRoots, string version)
    {
        foreach (var pluginRoot in pluginRoots)
        {
            foreach (var cacheRoot in ResolvePluginCacheRoots(pluginRoot, version))
            {
                ReplacePluginCache(pluginRoot.ToString(), cacheRoot);
                Log.Information("Refreshed plugin cache {CacheRoot}", cacheRoot);
            }
        }
    }

    internal static void ReplacePluginCache(string sourceRoot, string cacheRoot)
    {
        if (Directory.Exists(cacheRoot))
        {
            var parent = Path.GetDirectoryName(cacheRoot)
                ?? throw new InvalidOperationException($"Unable to resolve parent directory for plugin cache '{cacheRoot}'.");
            Directory.CreateDirectory(parent);

            var oldCacheRoot = Path.Combine(
                parent,
                $"{Path.GetFileName(cacheRoot)}.deleting-{DateTime.UtcNow:yyyyMMddHHmmssfff}");

            Directory.Move(cacheRoot, oldCacheRoot);
            if (!TryDeletePluginCacheDirectory(oldCacheRoot, out var cleanupError))
                Log.Warning(cleanupError, "Could not remove old plugin cache {CacheRoot}; continuing after replacement.", oldCacheRoot);
        }

        CopyDirectory(sourceRoot, cacheRoot);
    }

    internal static bool TryDeletePluginCacheDirectory(string cacheRoot, out Exception? error)
    {
        error = null;
        if (!Directory.Exists(cacheRoot))
            return true;

        try
        {
            var options = new EnumerationOptions
            {
                AttributesToSkip = 0,
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            };

            foreach (var path in Directory.EnumerateFileSystemEntries(cacheRoot, "*", options))
                File.SetAttributes(path, FileAttributes.Normal);

            File.SetAttributes(cacheRoot, FileAttributes.Normal);
            Directory.Delete(cacheRoot, recursive: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = ex;
            return false;
        }
    }

    private static IEnumerable<string> ResolvePluginCacheRoots(AbsolutePath pluginRoot, string version)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
            return [];

        return ResolvePluginCacheRoots(pluginRoot, version, home);
    }

    internal static IReadOnlyList<string> ResolvePluginCacheRoots(
        AbsolutePath pluginRoot,
        string version,
        string homeDirectory)
    {
        if (string.IsNullOrWhiteSpace(homeDirectory))
            return [];

        var roots = new List<string>();
        var repoName = Path.GetFileName(pluginRoot.ToString().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (repoName.Equals("mcpserver-codex-plugin", StringComparison.OrdinalIgnoreCase))
        {
            roots.Add(Path.Combine(homeDirectory, ".codex", "plugins", "cache", "mcpserver-codex-plugin", "mcpserver", version));
            roots.Add(Path.Combine(homeDirectory, ".codex", "plugins", "cache", "mcpserver-local", "mcpserver", version));
        }

        if (repoName.Equals("mcpserver-claude-code-plugin", StringComparison.OrdinalIgnoreCase))
            roots.Add(Path.Combine(homeDirectory, ".claude", "plugins", "cache", "mcpserver-local", "mcpserver", version));

        if (repoName.Equals("mcpserver-claude-cowork-plugin", StringComparison.OrdinalIgnoreCase))
            roots.Add(Path.Combine(homeDirectory, ".claude", "plugins", "cache", "mcpserver-cowork", "mcpserver-cowork", version));

        if (repoName.Equals("mcpserver-cline-plugin", StringComparison.OrdinalIgnoreCase)
            || repoName.Equals("mcpserver-cline-v2-plugin", StringComparison.OrdinalIgnoreCase))
        {
            var clineLocalRoot = Path.Combine(homeDirectory, ".cline", "plugins", "_installed", "local");
            if (Directory.Exists(clineLocalRoot))
            {
                roots.AddRange(Directory.EnumerateDirectories(clineLocalRoot, repoName + "-*", SearchOption.TopDirectoryOnly)
                    .Select(path => Path.Combine(path, "package")));
            }
        }

        if (repoName.Equals("mcpserver-grok-plugin", StringComparison.OrdinalIgnoreCase))
        {
            var grokInstalledRoot = Path.Combine(homeDirectory, ".grok", "installed-plugins");
            if (Directory.Exists(grokInstalledRoot))
            {
                roots.AddRange(Directory.EnumerateDirectories(grokInstalledRoot, "*mcpserver-grok-plugin*", SearchOption.TopDirectoryOnly));
            }
        }

        return roots
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);

        foreach (var directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories)
                     .Where(path => !IsUnderIgnoredDirectory(path)))
        {
            Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
                     .Where(path => !IsUnderIgnoredDirectory(path)))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, file);
            var destination = Path.Combine(destinationRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static bool IsUnderIgnoredDirectory(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(static segment =>
            segment.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("tests", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("test-fixtures", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("lib-sh", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("lib-node", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private readonly record struct PluginSemanticVersion(int Major, int Minor, int Patch)
        : IComparable<PluginSemanticVersion>
    {
        public int CompareTo(PluginSemanticVersion other)
        {
            var major = Major.CompareTo(other.Major);
            if (major != 0)
                return major;

            var minor = Minor.CompareTo(other.Minor);
            if (minor != 0)
                return minor;

            return Patch.CompareTo(other.Patch);
        }
    }
}
