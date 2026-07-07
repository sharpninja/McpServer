using System.Diagnostics;

namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-YAML-MUTATION-001: Guards YAML updates so agents deserialize,
/// mutate objects, serialize, and save instead of editing YAML as text.
/// </summary>
[Trait("Category", "Integration")]
public sealed class YamlObjectMutationTests
{
    /// <summary>
    /// TEST-MCP-YAML-MUTATION-001: Verifies the object-first YAML rule is
    /// published in repo, marker-template, context, and tracked skill guidance.
    /// </summary>
    [Fact]
    public async Task YamlMutationGuidance_IsPublishedToAgentSurfaces()
    {
        var requiredText = new[]
        {
            "deserialize the complete document into an object",
            "mutate the object",
            "serialize",
            "plugins/core/lib-ps/yaml-object-mutation.ps1",
        };

        var files = new List<string>
        {
            "AGENTS.md",
            Path.Combine("docs", "context", "yaml-object-mutation.md"),
            Path.Combine("templates", "prompt-templates.yaml"),
        };

        files.AddRange(Directory.GetFiles(Path.Combine(FindRepositoryRoot(), "skills"), "SKILL.md", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path)));

        foreach (var relativePath in files)
        {
            var text = await ReadRepositoryTextAsync(relativePath).ConfigureAwait(true);
            foreach (var value in requiredText)
            {
                Assert.Contains(value, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// TEST-MCP-YAML-MUTATION-002: Verifies the plugin helper can set nested
    /// YAML values by round-tripping through serializer commands.
    /// </summary>
    [Fact]
    public async Task YamlMutationHelper_RoundTripsNestedKeysBySerializer()
    {
        var root = FindRepositoryRoot();
        var tempDirectory = Path.Combine(Path.GetTempPath(), "mcp-yaml-object-mutation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var yamlPath = Path.Combine(tempDirectory, "appsettings.yaml");
        var helperPath = Path.Combine(root, "plugins", "core", "lib-ps", "yaml-object-mutation.ps1");

        var escapedHelperPath = EscapePowerShellSingleQuotedString(helperPath);
        var escapedYamlPath = EscapePowerShellSingleQuotedString(yamlPath);
        var command = string.Join(
            Environment.NewLine,
            $". '{escapedHelperPath}'",
            $"Set-McpYamlObjectValue -Path '{escapedYamlPath}' -KeyPath Triage,AgentPath -Value 'codex' -Create | Out-Null",
            $"Set-McpYamlObjectValue -Path '{escapedYamlPath}' -KeyPath Triage,QuietPeriodMinutes -Value 15 | Out-Null",
            $"$document = ConvertFrom-Yaml -Yaml ([System.IO.File]::ReadAllText('{escapedYamlPath}')) -Ordered",
            "if ($document['Triage']['AgentPath'] -ne 'codex') { throw 'AgentPath was not preserved.' }",
            "if ($document['Triage']['QuietPeriodMinutes'] -ne 15) { throw 'QuietPeriodMinutes was not preserved.' }");

        var result = await RunPowerShellAsync(command).ConfigureAwait(true);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(yamlPath), "The helper did not create the YAML file.");
        var yamlText = await File.ReadAllTextAsync(yamlPath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("Triage:", yamlText, StringComparison.Ordinal);
        Assert.Contains("AgentPath: codex", yamlText, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-YAML-MUTATION-003: Verifies plugin sync builds
    /// CORE-MANIFEST.yaml from an object and serializer instead of line strings.
    /// </summary>
    [Fact]
    public async Task PluginCoreSyncManifest_UsesYamlSerializerInsteadOfLineAssembly()
    {
        var text = await ReadRepositoryTextAsync(Path.Combine("plugins", "core", "sync", "sync-plugin-core.ps1"))
            .ConfigureAwait(true);

        Assert.Contains("ConvertFrom-Yaml", text, StringComparison.Ordinal);
        Assert.Contains("ConvertTo-Yaml", text, StringComparison.Ordinal);
        Assert.Contains("[ordered]@", text, StringComparison.Ordinal);
        Assert.DoesNotContain("$lines.Add", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Get-Content -LiteralPath $manifest", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-YAML-MUTATION-004: Verifies final-response complete-turn params
    /// are serialized from an object instead of hand-written YAML text.
    /// </summary>
    [Fact]
    public async Task FinalResponse_UsesYamlSerializerForCompleteTurnParams()
    {
        var text = await ReadRepositoryTextAsync(Path.Combine("plugins", "core", "lib-ps", "final-response.ps1"))
            .ConfigureAwait(true);

        Assert.Contains("ConvertTo-Yaml", text, StringComparison.Ordinal);
        Assert.Contains("[ordered]@{ response = $Response }", text, StringComparison.Ordinal);
        Assert.DoesNotContain("response: |", text, StringComparison.Ordinal);
        Assert.DoesNotContain("$indented", text, StringComparison.Ordinal);
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunPowerShellAsync(string command)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        process.StartInfo.ArgumentList.Add("-NoLogo");
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-Command");
        process.StartInfo.ArgumentList.Add(command);

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken: TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken: TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        return (process.ExitCode, await stdoutTask.ConfigureAwait(true), await stderrTask.ConfigureAwait(true));
    }

    private static string EscapePowerShellSingleQuotedString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static async Task<string> ReadRepositoryTextAsync(string relativePath)
    {
        var path = Path.Combine(FindRepositoryRoot(), relativePath);
        return await File.ReadAllTextAsync(path, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing McpServer.sln.");
    }
}
