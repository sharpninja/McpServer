namespace NukeBuild.IntegrationTests;

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using NukeBuild.Tests;

/// <summary>
/// TEST-MCP-106: Verifies the requirements wiki publication script extracts
/// platform-specific wiki files and enriches the landing page with repository
/// user-documentation links after ZIP extraction.
/// </summary>
/// <remarks>
/// <para>2026-11-07, TEST-MCP-106; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole and unchanged from Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"); its script runs through the linked BoundedProcess; only the namespace changed.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: Build.IntegrationTests, the project the entry above names, is now NukeBuild.IntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the class and its cases are unchanged.</para>
/// </remarks>
[Trait("Category", "Integration")]
public sealed class RequirementsWikiPublishScriptTests
{
    /// <summary>
    /// TEST-MCP-106: Uses a generated requirements wiki ZIP fixture with both
    /// Azure and GitHub folders because the pipeline publishes those folders
    /// independently.
    /// </summary>
    [Fact]
    public async Task PublishRequirementsWiki_GitHubOutput_AppendsGitHubUserDocumentationLinks()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var zipPath = CreateWikiExportZip(tempRoot);
            var outputPath = Path.Combine(tempRoot, "github-output");

            var result = await RunScriptAsync(
                "-Target", "GitHub",
                "-ExportZip", zipPath,
                "-OutputPath", outputPath,
                "-UserDocsBranch", "develop").ConfigureAwait(true);

            Assert.Equal(0, result.ExitCode);
            var home = await File.ReadAllTextAsync(
                Path.Combine(outputPath, "Home.md"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Contains("[Functional Requirements](Functional-Requirements)", home);
            Assert.Contains("## User Documentation", home);
            Assert.Contains("https://github.com/sharpninja/McpServer/blob/develop/docs/USER-GUIDE.md", home);
            Assert.Contains("https://github.com/sharpninja/McpServer/blob/develop/docs/context/federation.md", home);
            Assert.Contains("https://github.com/sharpninja/McpServer/blob/develop/docs/AGENT-PLUGIN-AVAILABILITY.md", home);
        }
        finally
        {
            DeleteDirectoryQuietly(tempRoot);
        }
    }

    /// <summary>
    /// TEST-MCP-106: Uses the same generated requirements wiki ZIP fixture to
    /// prove Azure wiki publication links to Azure DevOps repository docs.
    /// </summary>
    [Fact]
    public async Task PublishRequirementsWiki_AzureOutput_AppendsAzureUserDocumentationLinks()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var zipPath = CreateWikiExportZip(tempRoot);
            var outputPath = Path.Combine(tempRoot, "azure-output");

            var result = await RunScriptAsync(
                "-Target", "Azure",
                "-ExportZip", zipPath,
                "-OutputPath", outputPath,
                "-UserDocsBranch", "main").ConfigureAwait(true);

            Assert.Equal(0, result.ExitCode);
            var home = await File.ReadAllTextAsync(
                Path.Combine(outputPath, "Home.md"),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Contains("[Technical Requirements](Technical-Requirements)", home);
            Assert.Contains("## User Documentation", home);
            Assert.Contains("https://dev.azure.com/McpServer/McpServer/_git/McpServer?path=/docs/USER-GUIDE.md&version=GBmain", home);
            Assert.Contains("https://dev.azure.com/McpServer/McpServer/_git/McpServer?path=/docs/REPL-USER-GUIDE.md&version=GBmain", home);
            Assert.Contains("https://dev.azure.com/McpServer/McpServer/_git/McpServer?path=/docs/context/federation.md&version=GBmain", home);
        }
        finally
        {
            DeleteDirectoryQuietly(tempRoot);
        }
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mcpserver-wiki-script-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateWikiExportZip(string root)
    {
        var zipPath = Path.Combine(root, "requirements-wiki-documents.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddEntry(archive, "github/Home.md", "# Requirements\n\n- [Functional Requirements](Functional-Requirements)\n");
        AddEntry(archive, "github/Functional-Requirements.md", "# Functional Requirements\n");
        AddEntry(archive, "azure/Home.md", "# Requirements\n\n- [Technical Requirements](Technical-Requirements)\n");
        AddEntry(archive, "azure/Technical-Requirements.md", "# Technical Requirements\n");
        return zipPath;
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static async Task<ProcessResult> RunScriptAsync(params string[] arguments)
    {
        var scriptPath = FindRepositoryFile("scripts", "Publish-RequirementsWiki.ps1");
        var psi = new ProcessStartInfo("pwsh.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        psi.ArgumentList.Add("-NoLogo");
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(scriptPath);
        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        var result = await BoundedProcess.RunAsync(psi, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        return new ProcessResult(result.ExitCode, result.Stdout, result.Stderr);
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{Path.Combine(segments)}'.");
    }

    private static void DeleteDirectoryQuietly(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
