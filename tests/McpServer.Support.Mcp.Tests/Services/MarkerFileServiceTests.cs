using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Tests prompt composition, template context construction, and marker-file output behavior in <see cref="MarkerFileService"/>.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-018, FR-MCP-050, TR-MCP-TPL-005.
/// Test data uses deterministic URLs, workspace DTO samples, and template strings so rendering and marker YAML output
/// can be validated without external dependencies.
/// </remarks>
public sealed class MarkerFileServiceTests
{
    private const string BaseUrl = "http://localhost:7147";
    private static readonly ISerializer s_yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer s_yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly string[] PluginVersionEnvironmentVariables =
    [
        "CODEX_PLUGIN_ROOT",
        "CLAUDE_PLUGIN_ROOT",
        "COPILOT_PLUGIN_ROOT",
        "CLINE_PLUGIN_ROOT",
        "GROK_PLUGIN_ROOT",
    ];

    private static void WithClearedPluginVersionEnvironment(Action action)
    {
        var saved = PluginVersionEnvironmentVariables.ToDictionary(
            name => name,
            name => Environment.GetEnvironmentVariable(name));

        try
        {
            foreach (var name in PluginVersionEnvironmentVariables)
                Environment.SetEnvironmentVariable(name, null);

            action();
        }
        finally
        {
            foreach (var (name, value) in saved)
                Environment.SetEnvironmentVariable(name, value);
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> with plugin-version env vars cleared and the user-profile
    /// cache scan redirected to an empty temp directory, so resolution sees only the inputs the
    /// test controls regardless of plugins installed on the machine.
    /// </summary>
    /// <param name="action">The assertion body to run hermetically.</param>
    private static void WithHermeticPluginVersionResolution(Action action)
    {
        var emptyProfile = Path.Combine(Path.GetTempPath(), $"mcp-marker-profile-{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyProfile);
        try
        {
            MarkerFileService.AgentPluginUserProfileOverride = emptyProfile;
            WithClearedPluginVersionEnvironment(action);
        }
        finally
        {
            MarkerFileService.AgentPluginUserProfileOverride = null;
            Directory.Delete(emptyProfile, recursive: true);
        }
    }

    /// <summary>
    /// Builds a minimal template context for prompt-rendering tests.
    /// </summary>
    /// <param name="baseUrl">The base URL inserted into template context values.</param>
    /// <param name="apiKey">The API key inserted into template context values.</param>
    /// <returns>A context dictionary suitable for <see cref="MarkerFileService.ResolvePrompt"/>.</returns>
    /// <remarks>
    /// Test data: static local URL and optional API key values.
    /// This helper standardizes inputs so assertions focus on specific prompt behavior under test.
    /// </remarks>
    private static Dictionary<string, object?> MakeContext(string baseUrl = BaseUrl, string? apiKey = null) =>
        MarkerFileService.BuildTemplateContext(baseUrl, apiKey, workspace: null, workspacePath: @"C:\test", workspaceName: "test");

    /// <summary>
    /// Verifies that resolving a prompt with a null global template throws <see cref="ArgumentException"/>.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: a valid context and <see langword="null"/> global template.
    /// This data is used to confirm guard-clause enforcement for required global prompt content.
    /// </remarks>
    [Fact]
    public void ResolvePrompt_NullGlobal_Throws()
    {
        Assert.Throws<ArgumentException>(() => MarkerFileService.ResolvePrompt(MakeContext(), null, null));
    }

    /// <summary>
    /// Verifies that resolving a prompt with whitespace global template content throws <see cref="ArgumentException"/>.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: whitespace-only global template text.
    /// This data is used to ensure empty marker template content is rejected the same as null input.
    /// </remarks>
    [Fact]
    public void ResolvePrompt_EmptyGlobal_Throws()
    {
        Assert.Throws<ArgumentException>(() => MarkerFileService.ResolvePrompt(MakeContext(), "   ", null));
    }

    /// <summary>
    /// Verifies handlebars substitution for a custom global template string.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: template text containing <c>{{baseUrl}}</c> and deterministic <see cref="BaseUrl"/>.
    /// This data is used to validate expected placeholder replacement in global prompt rendering.
    /// </remarks>
    [Fact]
    public void ResolvePrompt_CustomGlobal_ReplacesDefault()
    {
        var template = "Custom prompt for {{baseUrl}} with special instructions.";

        var result = MarkerFileService.ResolvePrompt(MakeContext(), template, null);

        Assert.Equal($"Custom prompt for {BaseUrl} with special instructions.", result);
    }

    /// <summary>
    /// Verifies that workspace-specific prompt content is appended after global prompt content.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-018, FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: simple global/workspace prompt strings.
    /// This data is used to confirm marker prompts preserve both global and workspace instruction blocks.
    /// </remarks>
    [Fact]
    public void ResolvePrompt_WorkspaceAppends()
    {
        var globalTemplate = "Global prompt";
        var workspaceTemplate = "This workspace uses Python. Prefer pytest for testing.";

        var result = MarkerFileService.ResolvePrompt(MakeContext(), globalTemplate, workspaceTemplate);

        // Should contain both the global prompt and the workspace prompt
        Assert.Contains(globalTemplate, result);
        Assert.Contains(workspaceTemplate, result);
        Assert.Contains("\n\n", result);
    }

    /// <summary>
    /// Verifies that workspace template content receives <c>{{baseUrl}}</c> substitution.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: workspace prompt with two <c>{{baseUrl}}</c> placeholders.
    /// This data is used to ensure consistent replacement for multiple placeholder occurrences.
    /// </remarks>
    [Fact]
    public void ResolvePrompt_WorkspaceBaseUrlSubstitution()
    {
        var globalTemplate = "Global";
        var workspaceTemplate = "Dev server at {{baseUrl}}/api. Use {{baseUrl}}/docs for API docs.";

        var result = MarkerFileService.ResolvePrompt(MakeContext(), globalTemplate, workspaceTemplate);

        Assert.Contains($"Dev server at {BaseUrl}/api", result);
        Assert.Contains($"Use {BaseUrl}/docs for API docs", result);
    }

    /// <summary>
    /// Verifies that global and workspace prompt outputs are joined with a blank-line separator.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: global/workspace strings each containing <c>{{baseUrl}}</c>.
    /// This data is used to assert deterministic prompt structure used in marker files.
    /// </remarks>
    [Fact]
    public void ResolvePrompt_BothCustom_CombinesWithNewlines()
    {
        var global = "Global: server at {{baseUrl}}";
        var workspace = "Workspace: extra config for {{baseUrl}}";

        var result = MarkerFileService.ResolvePrompt(MakeContext(), global, workspace);

        Assert.Equal($"Global: server at {BaseUrl}\n\nWorkspace: extra config for {BaseUrl}", result);
    }

    /// <summary>
    /// Verifies that whitespace-only workspace prompts are not appended to the rendered global prompt.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: valid global prompt and whitespace-only workspace prompt.
    /// This data is used to ensure marker prompt composition avoids trailing separators for empty workspace overrides.
    /// </remarks>
    [Fact]
    public void ResolvePrompt_EmptyWorkspace_NotAppended()
    {
        var global = "Global prompt";
        var result = MarkerFileService.ResolvePrompt(MakeContext(), global, "   ");

        // Should be the global prompt only, no trailing separator
        Assert.DoesNotContain("\n\n   ", result);
        Assert.Equal(global, result);
    }

    /// <summary>
    /// Verifies that template context includes workspace DTO properties when a workspace is supplied.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-018, FR-MCP-050.
    /// Test data: populated <see cref="WorkspaceDto"/> with explicit values for identity, paths, and flags.
    /// This data is used to validate serialized workspace context consumed by marker template rendering.
    /// </remarks>
    [Fact]
    public void BuildTemplateContext_WithWorkspaceDto_IncludesAllProperties()
    {
        var ws = new WorkspaceDto
        {
            Name = "MyProject",
            WorkspacePath = @"C:\projects\my",
            TodoPath = @"C:\projects\my\todo.yaml",
            DataDirectory = @"C:\data\my",
            TunnelProvider = "cloudflare",
            IsPrimary = true,
            IsEnabled = true,
            DateTimeCreated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTimeModified = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            RunAs = "admin",
            PromptTemplate = "custom template",
            StatusPrompt = string.Empty,
            ImplementPrompt = string.Empty,
            PlanPrompt = string.Empty,
        };

        var ctx = MarkerFileService.BuildTemplateContext("http://localhost:7200", "tok123", ws, ws.WorkspacePath, ws.Name);

        Assert.Equal("http://localhost:7200", ctx["baseUrl"]);
        Assert.Equal("tok123", ctx["apiKey"]);
        var wsDict = Assert.IsType<Dictionary<string, object?>>(ctx["workspace"]);
        Assert.Equal("MyProject", wsDict["Name"]);
        Assert.Equal(true, wsDict["IsPrimary"]);
        Assert.Equal("cloudflare", wsDict["TunnelProvider"]);
    }

    /// <summary>
    /// Verifies that template context falls back to provided workspace name/path when workspace DTO is null.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-018, FR-MCP-050.
    /// Test data: null workspace DTO with fallback name/path values.
    /// This data is used to ensure marker generation remains stable when workspace metadata is partially unavailable.
    /// </remarks>
    [Fact]
    public void BuildTemplateContext_NullWorkspace_UsesFallbacks()
    {
        var ctx = MarkerFileService.BuildTemplateContext("http://localhost:7147", null, null, @"C:\ws", "fallback");

        Assert.Equal(string.Empty, ctx["apiKey"]);
        var wsDict = Assert.IsType<Dictionary<string, object?>>(ctx["workspace"]);
        Assert.Equal("fallback", wsDict["Name"]);
        Assert.Equal(@"C:\ws", wsDict["WorkspacePath"]);
        Assert.Equal(false, wsDict["IsPrimary"]);
    }

    /// <summary>
    /// Verifies handlebars rendering can access workspace properties from the generated template context.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-050, TR-MCP-TPL-005.
    /// Test data: workspace DTO with <c>Name = "TestProj"</c> and global template referencing <c>{{workspace.Name}}</c>.
    /// This data is used to confirm nested context resolution during marker prompt rendering.
    /// </remarks>
    [Fact]
    public void ResolvePrompt_HandlebarsRendersWorkspaceProperties()
    {
        var ws = new WorkspaceDto
        {
            Name = "TestProj",
            WorkspacePath = @"C:\test",
            TodoPath = @"C:\test\todo.yaml",
            DataDirectory = @"C:\test",
            TunnelProvider = null,
            IsPrimary = false,
            IsEnabled = true,
            DateTimeCreated = DateTime.UtcNow,
            DateTimeModified = DateTime.UtcNow,
            RunAs = null,
            PromptTemplate = null,
            StatusPrompt = string.Empty,
            ImplementPrompt = string.Empty,
            PlanPrompt = string.Empty,
        };

        var ctx = MarkerFileService.BuildTemplateContext(BaseUrl, "mytoken", ws, ws.WorkspacePath, ws.Name);
        var global = "Global for {{workspace.Name}}";
        var result = MarkerFileService.ResolvePrompt(ctx, global, null);

        Assert.Contains("Global for TestProj", result);
    }

    /// <summary>
    /// Verifies marker YAML output includes UTC timestamps and diagnostics endpoint references.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-018.
    /// Test data: temp workspace directory, fixed UTC startup timestamp, and explicit global prompt text.
    /// This data is used to assert deterministic marker-file metadata and endpoint wiring in generated output.
    /// </remarks>
    [Fact]
    public async Task WriteMarkerAsync_EmitsUtcTimestampsAndDiagnosticsEndpoints()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var serverStartedAtUtc = new DateTimeOffset(2026, 2, 26, 8, 30, 0, TimeSpan.Zero);
            var globalPrompt = "Test Global Prompt";

            await MarkerFileService.WriteMarkerAsync(
                workspacePath: tempDir,
                port: 7147,
                workspaceName: "test",
                globalPromptTemplate: globalPrompt,
                serverStartedAtUtc: serverStartedAtUtc, ct: TestContext.Current.CancellationToken);

            var markerPath = Path.Combine(tempDir, MarkerFileService.MarkerFileName);
            var yaml = await File.ReadAllTextAsync(markerPath, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Contains(globalPrompt, yaml);
            Assert.Contains("markerWrittenAtUtc:", yaml);
            Assert.Contains($"serverStartedAtUtc: {serverStartedAtUtc:o}", yaml);
            Assert.Contains("serverStartupUtc: /server-startup-utc", yaml);
            Assert.Contains("markerFileTimestamp: /marker-file-timestamp?repoPath={workspacePath}", yaml);
            Assert.Contains("desktop: /mcpserver/desktop", yaml);
            Assert.Contains("signature:", yaml);
            Assert.Contains("trust_bootstrap:", yaml);
            Assert.Contains("agent_plugins:", yaml);
            Assert.Contains("workflow.triage.*", yaml);
            Assert.Contains("mcpserver triage tools", yaml);
            Assert.Contains("triage_*", yaml);
            Assert.Contains("policy: required", yaml);
            Assert.Contains("MCP_PLUGIN_UNAVAILABLE:Codex", yaml);
            Assert.Contains("MCP_PLUGIN_UNAVAILABLE:Claude", yaml);
            Assert.Contains("MCP_PLUGIN_UNAVAILABLE:Copilot", yaml);
            Assert.Contains("MCP_PLUGIN_UNAVAILABLE:Cline", yaml);
            Assert.Contains("verifier: workspace_api_key", yaml);
            Assert.Contains("health_nonce_parameter: nonce", yaml);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test directory.
            }
        }
    }

    /// <summary>
    /// Verifies marker-file writing ensures the workspace root <c>.gitignore</c>
    /// contains the marker file entry, local <c>.mcpServer/</c> state directory
    /// entry, and workspace <c>cache/</c> directory entry without duplicating any
    /// entry on repeated writes.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-018.
    /// Test data: temp workspace directory with a pre-existing <c>.gitignore</c>
    /// containing a stable baseline entry.
    /// This data is used to validate idempotent ignore-file updates performed by
    /// marker writing during workspace startup.
    /// </remarks>
    [Fact]
    public async Task WriteMarkerAsync_AddsMarkerMcpServerAndCacheEntriesToGitIgnoreWithoutDuplicates()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-gitignore-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var gitignorePath = Path.Combine(tempDir, ".gitignore");
            await File.WriteAllTextAsync(gitignorePath, "bin/" + Environment.NewLine, cancellationToken: TestContext.Current.CancellationToken);

            await MarkerFileService.WriteMarkerAsync(
                workspacePath: tempDir,
                port: 7147,
                workspaceName: "test",
                globalPromptTemplate: "Prompt", ct: TestContext.Current.CancellationToken);

            await MarkerFileService.WriteMarkerAsync(
                workspacePath: tempDir,
                port: 7147,
                workspaceName: "test",
                globalPromptTemplate: "Prompt", ct: TestContext.Current.CancellationToken);

            var gitignoreLines = await File.ReadAllLinesAsync(gitignorePath, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, gitignoreLines.Count(line => line == "AGENTS-README-FIRST.yaml"));
            Assert.Equal(1, gitignoreLines.Count(line => line == ".mcpServer/"));
            Assert.Equal(1, gitignoreLines.Count(line => line == "cache/"));
            Assert.Contains("bin/", gitignoreLines);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test directory.
            }
        }
    }

    /// <summary>
    /// TEST-MCP-WIKIEXPORT-002: Verifies marker writing creates a default wiki export configuration when absent.
    /// </summary>
    [Fact]
    public async Task WriteMarkerAsync_CreatesDefaultWikiYamlWhenMissing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-wiki-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            await MarkerFileService.WriteMarkerAsync(
                workspacePath: tempDir,
                port: 7147,
                workspaceName: "test",
                globalPromptTemplate: "Prompt", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

            var markerPath = Path.Combine(tempDir, MarkerFileService.MarkerFileName);
            var wikiPath = Path.Combine(tempDir, "docs", "wiki.yaml");

            Assert.True(File.Exists(markerPath));
            Assert.True(File.Exists(wikiPath));

            var config = s_yamlDeserializer.Deserialize<MarkerDefaultWikiConfig>(await File.ReadAllTextAsync(wikiPath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
            var loadedConfig = RequirementsWikiExportConfigLoader.Load(tempDir, new RequirementsOptions());

            Assert.NotNull(loadedConfig);
            Assert.Equal("mcp-wiki-export/v1", config.Schema);
            Assert.Equal("home", config.Home.Document);
            Assert.Equal(
                ["home", "functional", "technical", "testing", "mapping", "matrix"],
                config.Documents.Select(document => document.Id).ToArray());
            Assert.All(config.Documents, document => Assert.Equal(["github", "azure"], document.Platforms));
            Assert.Equal(
                ["generated:home", "generated:functional", "generated:technical", "generated:testing", "generated:mapping", "generated:matrix"],
                config.Documents.Select(document => document.Source).ToArray());
            Assert.Equal(
                ["Home.md", "Functional-Requirements.md", "Technical-Requirements.md", "Testing-Requirements.md", "TR-per-FR-Mapping.md", "Requirements-Matrix.md"],
                config.Documents.Select(document => document.Target).ToArray());

            var navigationDocumentIds = FlattenNavigationDocuments(config.Navigation).ToArray();
            Assert.Equal(config.Documents.Select(document => document.Id).Order(StringComparer.Ordinal), navigationDocumentIds.Order(StringComparer.Ordinal));
            Assert.Equal(navigationDocumentIds.Length, navigationDocumentIds.Distinct(StringComparer.Ordinal).Count());
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test directory.
            }
        }
    }

    /// <summary>
    /// TEST-MCP-WIKIEXPORT-002: Verifies marker writing preserves user-authored wiki export configuration.
    /// </summary>
    [Fact]
    public async Task WriteMarkerAsync_PreservesExistingWikiYaml()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-wiki-preserve-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempDir, "docs"));

        try
        {
            var wikiPath = Path.Combine(tempDir, "docs", "wiki.yaml");
            var existingConfig = new
            {
                schema = "custom/wiki",
                documents = new[]
                {
                    new
                    {
                        id = "custom",
                        title = "Custom",
                        source = "generated:home",
                        target = "Custom.md",
                        platforms = new[] { "github" }
                    }
                },
                navigation = new[]
                {
                    new { document = "custom" }
                }
            };
            var existingYaml = s_yamlSerializer.Serialize(existingConfig);
            await File.WriteAllTextAsync(wikiPath, existingYaml, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            await MarkerFileService.WriteMarkerAsync(
                workspacePath: tempDir,
                port: 7147,
                workspaceName: "test",
                globalPromptTemplate: "Prompt", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(existingYaml, await File.ReadAllTextAsync(wikiPath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test directory.
            }
        }
    }

    /// <summary>
    /// Verifies marker removal deletes the active marker file and leaves no tombstone copy behind.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-MARKER-004, TR-MCP-MARKER-004, TEST-MCP-MARKER-004.
    /// Test data: temp workspace directory with a single marker file containing sentinel text.
    /// This data is used to prove the marker is removed outright rather than renamed to a
    /// never-reclaimed .deleted-{timestamp} archive that retains the rotated workspace API key.
    /// TR-MCP-DB-003 governs persistent MCP domain rows, not regenerated filesystem artifacts.
    /// </remarks>
    [Fact]
    public async Task RemoveMarker_DeletesMarkerFileAndLeavesNoTombstone()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-remove-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var markerPath = Path.Combine(tempDir, MarkerFileService.MarkerFileName);
            await File.WriteAllTextAsync(markerPath, "sentinel marker content", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            MarkerFileService.RemoveMarker(tempDir);

            Assert.False(File.Exists(markerPath));
            Assert.Empty(Directory.GetFiles(tempDir, MarkerFileService.MarkerFileName + ".deleted-*"));
            Assert.Empty(Directory.GetFiles(tempDir));
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test directory.
            }
        }
    }

    /// <summary>
    /// Verifies marker removal deletes the legacy markers alongside the current one without tombstones.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-MARKER-004, TR-MCP-MARKER-004, TEST-MCP-MARKER-004.
    /// Test data: temp workspace directory containing the current marker plus legacy .mcp-server.yaml
    /// and .mcp-server.json markers, proving every removal path deletes rather than archives.
    /// </remarks>
    [Fact]
    public async Task RemoveMarker_DeletesLegacyMarkersWithoutTombstones()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-legacy-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            foreach (var name in new[] { MarkerFileService.MarkerFileName, ".mcp-server.yaml", ".mcp-server.json" })
            {
                await File.WriteAllTextAsync(
                    Path.Combine(tempDir, name),
                    "legacy marker content",
                    cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            }

            MarkerFileService.RemoveMarker(tempDir);

            Assert.Empty(Directory.GetFiles(tempDir));
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test directory.
            }
        }
    }

    /// <summary>
    /// Verifies marker removal on a workspace with no marker present is a safe no-op.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-MARKER-004, TR-MCP-MARKER-004, TEST-MCP-MARKER-004.
    /// Test data: an empty temp workspace directory, proving removal neither throws nor creates files.
    /// </remarks>
    [Fact]
    public void RemoveMarker_WhenNoMarkerPresent_CreatesNothingAndDoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-absent-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            MarkerFileService.RemoveMarker(tempDir);

            Assert.Empty(Directory.GetFiles(tempDir));
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test directory.
            }
        }
    }

    /// <summary>
    /// Verifies that marker-signature payload generation is deterministic for the same marker content.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-076, TR-MCP-SEC-003.
    /// Test data: two in-memory marker objects with identical values.
    /// This data is used to prove that signature verification can be reproduced by bootstrap clients.
    /// </remarks>
    [Fact]
    public void BuildSignaturePayload_WithSameMarkerValues_IsDeterministic()
    {
        var marker = new MarkerFile
        {
            Port = 7147,
            BaseUrl = BaseUrl,
            ApiKey = "marker-key",
            Endpoints = new MarkerEndpoints
            {
                Health = "/health",
                Swagger = "/swagger/v1/swagger.json",
                SwaggerUi = "/swagger",
                McpTransport = "/mcp-transport",
                SessionLog = "/mcpserver/sessionlog",
                SessionLogDialog = "/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog",
                ContextSearch = "/mcpserver/context/search",
                ContextPack = "/mcpserver/context/pack",
                ContextSources = "/mcpserver/context/sources",
                Todo = "/mcpserver/todo",
                Repo = "/mcpserver/repo",
                Desktop = "/mcpserver/desktop",
                GitHub = "/mcpserver/gh",
                Tools = "/mcpserver/tools",
                Workspace = "/mcpserver/workspace",
                ServerStartupUtc = "/server-startup-utc",
                MarkerFileTimestamp = "/marker-file-timestamp?repoPath={workspacePath}",
            },
            Workspace = "test",
            WorkspacePath = @"C:\test",
            Pid = 123,
            StartedAt = "2026-03-28T16:00:00.0000000Z",
            MarkerWrittenAtUtc = "2026-03-28T16:00:00.0000000Z",
            ServerStartedAtUtc = "2026-03-28T15:59:00.0000000Z",
            Signature = new MarkerSignature
            {
                Algorithm = "HMAC-SHA256",
                Canonicalization = MarkerFileService.MarkerSignatureCanonicalization,
                Verifier = MarkerFileService.MarkerSignatureVerifier,
            },
            TrustBootstrap = new MarkerTrustBootstrap(),
            Prompt = "Prompt",
        };

        var payloadA = MarkerFileService.BuildSignaturePayload(marker);
        var payloadB = MarkerFileService.BuildSignaturePayload(marker);
        var signatureA = MarkerFileService.ComputeMarkerSignature(marker);
        var signatureB = MarkerFileService.ComputeMarkerSignature(marker);

        Assert.Equal(payloadA, payloadB);
        Assert.Equal(signatureA, signatureB);
    }

    private static IEnumerable<string> FlattenNavigationDocuments(IEnumerable<MarkerDefaultWikiNavigationItem> navigation)
    {
        foreach (var item in navigation)
        {
            if (!string.IsNullOrWhiteSpace(item.Document))
                yield return item.Document;

            foreach (var child in FlattenNavigationDocuments(item.Children))
                yield return child;
        }
    }

    private sealed class MarkerDefaultWikiConfig
    {
        public string Schema { get; set; } = string.Empty;

        public MarkerDefaultWikiHome Home { get; set; } = new();

        public List<MarkerDefaultWikiDocument> Documents { get; set; } = [];

        public List<MarkerDefaultWikiNavigationItem> Navigation { get; set; } = [];
    }

    private sealed class MarkerDefaultWikiHome
    {
        public string Document { get; set; } = string.Empty;
    }

    private sealed class MarkerDefaultWikiDocument
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string Target { get; set; } = string.Empty;

        public List<string> Platforms { get; set; } = [];
    }

    private sealed class MarkerDefaultWikiNavigationItem
    {
        public string? Document { get; set; }

        public string? Title { get; set; }

        public string? Path { get; set; }

        public List<MarkerDefaultWikiNavigationItem> Children { get; set; } = [];
    }

    /// <summary>
    /// TEST-MCP-PLUGIN-PSONLY-001: marker plugin contracts publish the current synced plugin version.
    /// </summary>
    [Fact]
    public void BuildDefaultAgentPlugins_UsesCurrentSyncedPluginVersion()
    {
        WithHermeticPluginVersionResolution(() =>
        {
            var plugins = MarkerFileService.BuildDefaultAgentPlugins(@"C:\test");

            Assert.All(
                plugins.Agents,
                pair => Assert.Equal("1.26.0", pair.Value.PluginVersion));
        });
    }

    /// <summary>
    /// TEST-MCP-MARKER-REFRESH-001: installed sibling plugin versions override the synced fallback in marker contracts.
    /// </summary>
    [Fact]
    public void BuildDefaultAgentPlugins_UsesInstalledSiblingPluginVersionWhenPresent()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mcp-marker-version-{Guid.NewGuid():N}");
        var workspace = Path.Combine(root, "McpServer");
        var claudePlugin = Path.Combine(root, "mcpserver-claude-code-plugin");
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(claudePlugin);
        File.WriteAllText(Path.Combine(claudePlugin, ".version"), "9.8.7");

        try
        {
            WithHermeticPluginVersionResolution(() =>
            {
                var plugins = MarkerFileService.BuildDefaultAgentPlugins(workspace);

                Assert.Equal("9.8.7", plugins.Agents["Claude"].PluginVersion);
                Assert.Equal(MarkerFileService.SyncedAgentPluginVersion, plugins.Agents["Codex"].PluginVersion);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// TEST-MCP-MARKER-REFRESH-001: resolved plugin versions affect the agent plugin contract digest.
    /// </summary>
    [Fact]
    public void ComputeAgentPluginsDigest_ChangesWhenResolvedPluginVersionChanges()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mcp-marker-digest-{Guid.NewGuid():N}");
        var workspace = Path.Combine(root, "McpServer");
        var claudePlugin = Path.Combine(root, "mcpserver-claude-code-plugin");
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(claudePlugin);

        try
        {
            WithClearedPluginVersionEnvironment(() =>
            {
                var baseline = MarkerFileService.BuildDefaultAgentPlugins(workspace);
                var baselineDigest = MarkerFileService.ComputeAgentPluginsDigest(baseline);

                File.WriteAllText(Path.Combine(claudePlugin, ".version"), "9.8.7");
                var updated = MarkerFileService.BuildDefaultAgentPlugins(workspace);
                var updatedDigest = MarkerFileService.ComputeAgentPluginsDigest(updated);

                Assert.NotEqual(baselineDigest, updatedDigest);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Verifies plugin contract data is part of the marker signature payload.</summary>
    [Fact]
    public void BuildSignaturePayload_IncludesAgentPluginDigestWhenContractIsPresent()
    {
        var plugins = MarkerFileService.BuildDefaultAgentPlugins(@"C:\test");
        plugins.ContractDigest = MarkerFileService.ComputeAgentPluginsDigest(plugins);
        var marker = new MarkerFile
        {
            Port = 7147,
            BaseUrl = BaseUrl,
            ApiKey = "marker-key",
            Endpoints = new MarkerEndpoints { Health = "/health" },
            Workspace = "test",
            WorkspacePath = @"C:\test",
            Signature = new MarkerSignature { Canonicalization = MarkerFileService.MarkerSignatureCanonicalization },
            AgentPlugins = plugins,
        };

        var payload = MarkerFileService.BuildSignaturePayload(marker);

        Assert.Contains("agentPlugins.policy=required", payload);
        Assert.Contains("agentPlugins.contractDigest=", payload);
    }

    /// <summary>
    /// The agent-plugin contract digest used by the pinned marker-v1 byte-compatibility fixtures.
    /// </summary>
    private const string PinnedContractDigest =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    /// <summary>
    /// The 27 unconditional marker-v1 payload lines for the pinned fixture marker, in canonical order.
    /// </summary>
    private static readonly string[] PinnedPayloadLines =
    [
        "canonicalization=marker-v1",
        "port=7147",
        "baseUrl=http://localhost:7147",
        "apiKey=marker-key",
        "workspace=test",
        @"workspacePath=C:\test",
        "pid=123",
        "startedAt=2026-03-28T16:00:00.0000000Z",
        "markerWrittenAtUtc=2026-03-28T16:00:00.0000000Z",
        "serverStartedAtUtc=2026-03-28T15:59:00.0000000Z",
        "endpoints.health=/health",
        "endpoints.swagger=/swagger/v1/swagger.json",
        "endpoints.swaggerUi=/swagger",
        "endpoints.mcpTransport=/mcp-transport",
        "endpoints.sessionLog=/mcpserver/sessionlog",
        "endpoints.sessionLogDialog=/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog",
        "endpoints.contextSearch=/mcpserver/context/search",
        "endpoints.contextPack=/mcpserver/context/pack",
        "endpoints.contextSources=/mcpserver/context/sources",
        "endpoints.todo=/mcpserver/todo",
        "endpoints.repo=/mcpserver/repo",
        "endpoints.desktop=/mcpserver/desktop",
        "endpoints.gitHub=/mcpserver/gh",
        "endpoints.tools=/mcpserver/tools",
        "endpoints.workspace=/mcpserver/workspace",
        "endpoints.serverStartupUtc=/server-startup-utc",
        "endpoints.markerFileTimestamp=/marker-file-timestamp?repoPath={workspacePath}",
    ];

    /// <summary>
    /// The two conditional marker-v1 payload lines emitted only when the marker carries an agent-plugin contract.
    /// </summary>
    private static readonly string[] PinnedAgentPluginPayloadLines =
    [
        "agentPlugins.policy=required",
        "agentPlugins.contractDigest=" + PinnedContractDigest,
    ];

    /// <summary>
    /// Builds the deterministic marker used by the marker-v1 byte-compatibility and field-order tests.
    /// </summary>
    /// <param name="includeAgentPlugins">
    /// When <see langword="true"/> the marker carries an agent-plugin contract, so the conditional
    /// <c>agentPlugins.*</c> payload tail is emitted.
    /// </param>
    /// <returns>A marker whose every signed value is a fixed literal.</returns>
    /// <remarks>
    /// Test data: the same literal values pinned in <see cref="PinnedPayloadLines"/> and
    /// <see cref="PinnedAgentPluginPayloadLines"/>, so the payload text is fully deterministic and
    /// independent of the host machine, clock, and installed plugins.
    /// </remarks>
    private static MarkerFile BuildPinnedSignatureMarker(bool includeAgentPlugins) => new()
    {
        Port = 7147,
        BaseUrl = BaseUrl,
        ApiKey = "marker-key",
        Endpoints = new MarkerEndpoints
        {
            Health = "/health",
            Swagger = "/swagger/v1/swagger.json",
            SwaggerUi = "/swagger",
            McpTransport = "/mcp-transport",
            SessionLog = "/mcpserver/sessionlog",
            SessionLogDialog = "/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog",
            ContextSearch = "/mcpserver/context/search",
            ContextPack = "/mcpserver/context/pack",
            ContextSources = "/mcpserver/context/sources",
            Todo = "/mcpserver/todo",
            Repo = "/mcpserver/repo",
            Desktop = "/mcpserver/desktop",
            GitHub = "/mcpserver/gh",
            Tools = "/mcpserver/tools",
            Workspace = "/mcpserver/workspace",
            ServerStartupUtc = "/server-startup-utc",
            MarkerFileTimestamp = "/marker-file-timestamp?repoPath={workspacePath}",
        },
        Workspace = "test",
        WorkspacePath = @"C:\test",
        Pid = 123,
        StartedAt = "2026-03-28T16:00:00.0000000Z",
        MarkerWrittenAtUtc = "2026-03-28T16:00:00.0000000Z",
        ServerStartedAtUtc = "2026-03-28T15:59:00.0000000Z",
        Signature = new MarkerSignature
        {
            Algorithm = "HMAC-SHA256",
            Canonicalization = MarkerFileService.MarkerSignatureCanonicalization,
            Verifier = MarkerFileService.MarkerSignatureVerifier,
        },
        TrustBootstrap = new MarkerTrustBootstrap(),
        AgentPlugins = includeAgentPlugins
            ? new MarkerAgentPlugins { Policy = "required", ContractDigest = PinnedContractDigest }
            : null,
        Prompt = "Prompt",
    };

    /// <summary>
    /// Splits a marker-v1 payload into its <c>key=value</c> lines and returns the key of each line in order.
    /// </summary>
    /// <param name="payload">The payload text produced by <see cref="MarkerFileService.BuildSignaturePayload"/>.</param>
    /// <returns>The payload field names, in emission order.</returns>
    private static string[] ReadPayloadFieldOrder(string payload)
    {
        Assert.EndsWith("\n", payload, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', payload);
        return payload[..^1]
            .Split('\n')
            .Select(line => line[..line.IndexOf('=', StringComparison.Ordinal)])
            .ToArray();
    }

    /// <summary>
    /// Reads the documented canonical payload key order out of <c>docs/REPL-AGENT-GUIDE.md</c>.
    /// </summary>
    /// <returns>The documented field names, in documented order.</returns>
    /// <remarks>
    /// Test data: the <c>```text</c> block that follows the "The canonical key order is:" heading in
    /// the REPL agent guide, which is the prose contract external verifiers implement against.
    /// </remarks>
    private static string[] ReadDocumentedCanonicalKeyOrder()
    {
        var path = FindFileFromRepoRoot("docs", "REPL-AGENT-GUIDE.md");
        var text = File.ReadAllText(path).ReplaceLineEndings("\n");
        var headingIndex = text.IndexOf("The canonical key order is:", StringComparison.Ordinal);
        Assert.True(headingIndex >= 0, $"'The canonical key order is:' heading not found in {path}.");

        var blockStart = text.IndexOf("```text", headingIndex, StringComparison.Ordinal);
        Assert.True(blockStart >= 0, $"Canonical key order code fence not found in {path}.");
        blockStart = text.IndexOf('\n', blockStart) + 1;
        var blockEnd = text.IndexOf("```", blockStart, StringComparison.Ordinal);
        Assert.True(blockEnd > blockStart, $"Canonical key order code fence is not closed in {path}.");

        var keys = new List<string>();
        foreach (var rawLine in text[blockStart..blockEnd].Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            Assert.EndsWith(@"\n", line, StringComparison.Ordinal);
            var separator = line.IndexOf('=', StringComparison.Ordinal);
            Assert.True(separator > 0, $"Documented payload line '{line}' is not in key=value form.");
            keys.Add(line[..separator]);
        }

        return [.. keys];
    }

    /// <summary>
    /// Returns the indented lines of a top-level YAML block, starting after <paramref name="blockHeader"/>
    /// and stopping at the next non-indented line.
    /// </summary>
    /// <param name="yaml">The marker YAML text.</param>
    /// <param name="blockHeader">The top-level block header, for example <c>signature:</c>.</param>
    /// <returns>The block's raw lines, indentation preserved.</returns>
    private static string[] ReadYamlBlockLines(string yaml, string blockHeader)
    {
        var lines = yaml.ReplaceLineEndings("\n").Split('\n');
        var start = Array.FindIndex(lines, line => line.StartsWith(blockHeader, StringComparison.Ordinal));
        Assert.True(start >= 0, $"Block '{blockHeader}' not found in marker YAML.");

        var block = new List<string>();
        for (var i = start + 1; i < lines.Length && (lines[i].Length == 0 || char.IsWhiteSpace(lines[i][0])); i++)
        {
            if (lines[i].Length > 0)
                block.Add(lines[i]);
        }

        Assert.NotEmpty(block);
        return [.. block];
    }

    /// <summary>
    /// Locates a repository file by walking up from the test output directory.
    /// </summary>
    /// <param name="segments">Repo-relative path segments of the file to locate.</param>
    /// <returns>The absolute path of the located file.</returns>
    private static string FindFileFromRepoRoot(params string[] segments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, Path.Combine(segments));
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate file '{Path.Combine(segments)}' from '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Verifies the marker-v1 payload builder emits exactly the fields named by
    /// <see cref="MarkerFileService.SignaturePayloadFields"/>, in that array's order, when the marker
    /// carries an agent-plugin contract.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-140, TR-MCP-SEC-005.
    /// Test data: the pinned deterministic marker from <see cref="BuildPinnedSignatureMarker"/> with
    /// an agent-plugin contract attached.
    /// This data is used to prove the declared field list and the payload builder cannot diverge.
    /// </remarks>
    [Fact]
    public void BuildSignaturePayload_DerivesFieldOrderFromSignaturePayloadFields()
    {
        var marker = BuildPinnedSignatureMarker(includeAgentPlugins: true);

        var emitted = ReadPayloadFieldOrder(MarkerFileService.BuildSignaturePayload(marker));

        Assert.Equal(MarkerFileService.SignaturePayloadFields, emitted);
        Assert.Equal(29, MarkerFileService.SignaturePayloadFields.Length);
        Assert.Equal("canonicalization", MarkerFileService.SignaturePayloadFields[0]);
        Assert.Equal("agentPlugins.contractDigest", MarkerFileService.SignaturePayloadFields[^1]);
    }

    /// <summary>
    /// Verifies the conditional <c>agentPlugins.*</c> tail is dropped from both the emitted payload and
    /// the resolved field list when the marker carries no agent-plugin contract.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-140, TR-MCP-SEC-005.
    /// Test data: the pinned deterministic marker from <see cref="BuildPinnedSignatureMarker"/> with
    /// <c>AgentPlugins</c> left null.
    /// This data is used to prove the self-describing field list reports what was emitted for that
    /// specific marker rather than the unconditional superset.
    /// </remarks>
    [Fact]
    public void ResolveSignaturePayloadFields_WithoutAgentPlugins_DropsConditionalTail()
    {
        var marker = BuildPinnedSignatureMarker(includeAgentPlugins: false);

        var resolved = MarkerFileService.ResolveSignaturePayloadFields(marker);
        var emitted = ReadPayloadFieldOrder(MarkerFileService.BuildSignaturePayload(marker));
        string[] expectedPrefix = [.. MarkerFileService.SignaturePayloadFields.Take(27)];
        string[] resolvedArray = [.. resolved];

        Assert.Equal(27, resolved.Count);
        Assert.Equal(resolved, emitted);
        Assert.DoesNotContain("agentPlugins.policy", resolved);
        Assert.DoesNotContain("agentPlugins.contractDigest", resolved);
        Assert.Equal(expectedPrefix, resolvedArray);
    }

    /// <summary>
    /// Pins the exact marker-v1 payload bytes and HMAC-SHA256 digest for a known marker so the
    /// self-describing signature refactor cannot change what six independent external verifiers recompute.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-140, TR-MCP-SEC-005, TR-MCP-SEC-003.
    /// Test data: the pinned deterministic marker from <see cref="BuildPinnedSignatureMarker"/>; the
    /// expected digests were computed by an independent PowerShell HMAC implementation mirroring
    /// McpSession.psm1 rather than by this service.
    /// This data is used to prove byte-for-byte signature compatibility with plugin lib-ps, lib-sh,
    /// lib-node, mcp-repl-ts, mcpserver-agent-core, and MarkerFileClientOptionsResolver verifiers.
    /// </remarks>
    [Fact]
    public void BuildSignaturePayload_MatchesPinnedMarkerV1Bytes()
    {
        var withPlugins = BuildPinnedSignatureMarker(includeAgentPlugins: true);
        var withoutPlugins = BuildPinnedSignatureMarker(includeAgentPlugins: false);
        var expectedWithPlugins =
            string.Join("\n", PinnedPayloadLines.Concat(PinnedAgentPluginPayloadLines)) + "\n";
        var expectedWithoutPlugins = string.Join("\n", PinnedPayloadLines) + "\n";

        Assert.Equal(expectedWithPlugins, MarkerFileService.BuildSignaturePayload(withPlugins));
        Assert.Equal(expectedWithoutPlugins, MarkerFileService.BuildSignaturePayload(withoutPlugins));
        Assert.Equal(
            "11287AD218B134F3A141C0B9BF7F8524886F9D7A29857F0E028F6F209A78CD99",
            MarkerFileService.ComputeMarkerSignature(withPlugins));
        Assert.Equal(
            "51FA0D9777E649DACE7CA284FA3DE1EBCF6CC86F53FAF881DE688C70ECF94D2F",
            MarkerFileService.ComputeMarkerSignature(withoutPlugins));
    }

    /// <summary>
    /// Verifies the documented canonical key order in <c>docs/REPL-AGENT-GUIDE.md</c> matches
    /// <see cref="MarkerFileService.SignaturePayloadFields"/> exactly, so the prose spec external
    /// verifiers implement cannot silently drift from the code.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-140, TR-MCP-SEC-005.
    /// Test data: the canonical key order code fence in <c>docs/REPL-AGENT-GUIDE.md</c>.
    /// This data is used to bind the published verifier contract to the authoritative field array.
    /// </remarks>
    [Fact]
    public void SignaturePayloadFields_MatchesDocumentedCanonicalKeyOrder()
    {
        var documented = ReadDocumentedCanonicalKeyOrder();

        Assert.Equal(MarkerFileService.SignaturePayloadFields, documented);
    }

    /// <summary>
    /// Verifies the written marker emits a self-describing signature block whose <c>fields</c> array is
    /// exactly what the payload builder emitted for that marker, plus the payload <c>format</c> contract.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-140, TR-MCP-SEC-005.
    /// Test data: a temp workspace directory, a fixed UTC startup timestamp, and explicit global prompt text.
    /// This data is used to assert the marker on disk carries enough detail for a verifier to rebuild the
    /// signed payload without hard-coding the field list.
    /// </remarks>
    [Fact]
    public async Task WriteMarkerAsync_EmitsSelfDescribingSignatureFieldsAndFormat()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-signature-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            await MarkerFileService.WriteMarkerAsync(
                workspacePath: tempDir,
                port: 7147,
                workspaceName: "test",
                globalPromptTemplate: "Test Global Prompt",
                serverStartedAtUtc: new DateTimeOffset(2026, 2, 26, 8, 30, 0, TimeSpan.Zero),
                ct: TestContext.Current.CancellationToken);

            var markerPath = Path.Combine(tempDir, MarkerFileService.MarkerFileName);
            var yaml = await File.ReadAllTextAsync(markerPath, cancellationToken: TestContext.Current.CancellationToken);
            var document = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build()
                .Deserialize<MarkerSignatureDocument>(yaml);

            // WriteMarkerAsync always attaches an agent-plugin contract, so the conditional tail is emitted.
            string[] emittedFields = [.. document.Signature.Fields];
            Assert.Equal(MarkerFileService.SignaturePayloadFields, emittedFields);
            Assert.Equal(MarkerFileService.MarkerSignatureFormat, document.Signature.Format);
            Assert.Equal("HMAC-SHA256", document.Signature.Algorithm);
            Assert.NotEmpty(document.Signature.Value);
            Assert.Contains("fields:", yaml);
            Assert.Contains("format:", yaml);

            // The added keys must not break MarkerFileClientOptionsResolver.ParseMarker, which reads the
            // signature block line-wise as two-space "key: value" entries and skips anything else.
            var signatureBlock = ReadYamlBlockLines(yaml, "signature:");
            Assert.Contains(signatureBlock, line => line.StartsWith("  canonicalization: ", StringComparison.Ordinal));
            Assert.Contains(signatureBlock, line => line.StartsWith("  value: ", StringComparison.Ordinal));
            Assert.Contains(signatureBlock, line => line.StartsWith("  format: ", StringComparison.Ordinal));
            Assert.All(
                signatureBlock.Where(line => line.TrimStart().StartsWith('-')),
                line => Assert.DoesNotContain(':', line));
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test directory.
            }
        }
    }

    /// <summary>Deserialization shape for the <c>signature</c> block of a written marker file.</summary>
    private sealed class MarkerSignatureDocument
    {
        /// <summary>The marker signature block.</summary>
        public MarkerSignatureBlock Signature { get; set; } = new();
    }

    /// <summary>Deserialization shape for the self-describing marker signature fields.</summary>
    private sealed class MarkerSignatureBlock
    {
        /// <summary>The signature algorithm name.</summary>
        public string Algorithm { get; set; } = string.Empty;

        /// <summary>The canonicalization identifier.</summary>
        public string Canonicalization { get; set; } = string.Empty;

        /// <summary>The verifier key identifier.</summary>
        public string Verifier { get; set; } = string.Empty;

        /// <summary>The ordered payload field names emitted for this marker.</summary>
        public List<string> Fields { get; set; } = [];

        /// <summary>The payload line-format contract.</summary>
        public string Format { get; set; } = string.Empty;

        /// <summary>The hex-encoded signature digest.</summary>
        public string Value { get; set; } = string.Empty;
    }
}
