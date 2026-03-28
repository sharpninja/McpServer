using McpServer.Support.Mcp.Services;
using Xunit;

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
                serverStartedAtUtc: serverStartedAtUtc);

            var markerPath = Path.Combine(tempDir, MarkerFileService.MarkerFileName);
            var yaml = await File.ReadAllTextAsync(markerPath);

            Assert.Contains(globalPrompt, yaml);
            Assert.Contains("markerWrittenAtUtc:", yaml);
            Assert.Contains($"serverStartedAtUtc: {serverStartedAtUtc:o}", yaml);
            Assert.Contains("serverStartupUtc: /server-startup-utc", yaml);
            Assert.Contains("markerFileTimestamp: /marker-file-timestamp?repoPath={workspacePath}", yaml);
            Assert.Contains("desktop: /mcpserver/desktop", yaml);
            Assert.Contains("signature:", yaml);
            Assert.Contains("trust_bootstrap:", yaml);
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
    /// contains both the marker file entry and the local <c>.mcpServer/</c>
    /// state directory entry without duplicating either on repeated writes.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-018.
    /// Test data: temp workspace directory with a pre-existing <c>.gitignore</c>
    /// containing a stable baseline entry.
    /// This data is used to validate idempotent ignore-file updates performed by
    /// marker writing during workspace startup.
    /// </remarks>
    [Fact]
    public async Task WriteMarkerAsync_AddsMarkerAndMcpServerEntriesToGitIgnoreWithoutDuplicates()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-gitignore-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var gitignorePath = Path.Combine(tempDir, ".gitignore");
            await File.WriteAllTextAsync(gitignorePath, "bin/" + Environment.NewLine);

            await MarkerFileService.WriteMarkerAsync(
                workspacePath: tempDir,
                port: 7147,
                workspaceName: "test",
                globalPromptTemplate: "Prompt");

            await MarkerFileService.WriteMarkerAsync(
                workspacePath: tempDir,
                port: 7147,
                workspaceName: "test",
                globalPromptTemplate: "Prompt");

            var gitignoreLines = await File.ReadAllLinesAsync(gitignorePath);

            Assert.Equal(1, gitignoreLines.Count(line => line == "AGENTS-README-FIRST.yaml"));
            Assert.Equal(1, gitignoreLines.Count(line => line == ".mcpServer/"));
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
}
