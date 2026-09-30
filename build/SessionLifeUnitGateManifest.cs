using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// FR-MCP-107 / TR-MCP-PLAN-001: Captures the pre-run source and tool manifest the unit gate validates.
/// </summary>
internal sealed partial class SessionLifeUnitGateValidator
{
    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Writes <c>source-manifest.json</c> from the canonical source policy and current tool bytes.
    /// </summary>
    /// <param name="runId">The gate run id.</param>
    /// <param name="manifestPath">The absolute manifest path.</param>
    internal void WriteSourceManifest(string runId, string manifestPath)
    {
        var policy = new JsonObject
        {
            ["mode"] = "git-tracked-plus-relevant-untracked",
            ["roots"] = new JsonArray("build", "src", "tests", "plugins/core/test-fixtures/pester"),
            ["exclusions"] = new JsonArray(
                "TestResults/**",
                "docs/receipts/sessionlife-completion/**/test-results/**",
                "**/bin/**",
                "**/obj/**"),
        };
        var skeleton = new JsonObject
        {
            ["sourcePolicy"] = policy.DeepClone(),
        };
        var sources = new JsonArray();
        foreach (var relative in ListIncludedSources(skeleton))
        {
            var fullPath = ResolveInsideRepository(relative)
                ?? throw new InvalidOperationException($"Session-life source path escaped the repository: {relative}");
            var bytes = File.ReadAllBytes(fullPath);
            sources.Add(new JsonObject
            {
                ["path"] = relative,
                ["length"] = bytes.LongLength,
                ["sha256"] = Convert.ToHexString(SHA256.HashData(bytes)),
                ["provenance"] = "tracked-or-relevant-untracked",
            });
        }

        var tools = new JsonArray();
        foreach (var name in new[] { "dotnet", "pwsh" }.OrderBy(tool => tool, StringComparer.Ordinal))
        {
            tools.Add(new JsonObject
            {
                ["name"] = name,
                ["observedValue"] = ObserveTool(name),
            });
        }

        var document = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["runId"] = runId,
            ["capturedAtUtc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["sourcePolicy"] = policy,
            ["sources"] = sources,
            ["tools"] = tools,
        };
        var directory = Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(
            manifestPath,
            document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    }

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Lists the repository-relative sources included by a manifest policy.
    /// </summary>
    /// <param name="manifest">A document that contains <c>sourcePolicy</c>.</param>
    /// <returns>The ordered included source paths.</returns>
    internal IReadOnlyList<string> ListIncludedSources(JsonObject manifest) =>
        EnumeratePolicySources(manifest).OrderBy(path => path, StringComparer.Ordinal).ToArray();

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Returns <c>path|version|hash</c> for a tool resolved from PATH.
    /// </summary>
    /// <param name="name">The tool name, <c>dotnet</c> or <c>pwsh</c>.</param>
    /// <returns>The current tool observation.</returns>
    internal static string ObserveTool(string name)
    {
        var path = ResolveExecutable(name);
        var version = QueryVersion(name, path);
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        return $"{path}|{version}|{hash}";
    }

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Resolves an executable from PATH without searching the repository.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <returns>The absolute executable path.</returns>
    private static string ResolveExecutable(string name)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var candidate in new[] { name, name + ".exe" })
            {
                var fullPath = Path.Combine(directory, candidate);
                if (File.Exists(fullPath))
                    return Path.GetFullPath(fullPath);
            }
        }

        throw new InvalidOperationException($"Session-life gate could not resolve tool '{name}' on PATH.");
    }

    /// <summary>
    /// FR-MCP-107 / TR-MCP-PLAN-001: Reads one version line from the resolved tool.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="path">The absolute executable path.</param>
    /// <returns>The trimmed version line.</returns>
    private static string QueryVersion(string name, string path)
    {
        var start = new ProcessStartInfo
        {
            FileName = path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        if (string.Equals(name, "pwsh", StringComparison.Ordinal))
        {
            start.ArgumentList.Add("-NoLogo");
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            start.ArgumentList.Add("-Command");
            start.ArgumentList.Add("$PSVersionTable.PSVersion.ToString()");
        }
        else
        {
            start.ArgumentList.Add("--version");
        }

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"Session-life gate could not start '{name}'.");
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        var line = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;
        if (process.ExitCode != 0 || line.Length == 0)
        {
            throw new InvalidOperationException($"Session-life gate could not read the version of '{name}'.");
        }

        return line;
    }
}
