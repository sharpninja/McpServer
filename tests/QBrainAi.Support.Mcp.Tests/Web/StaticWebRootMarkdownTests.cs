using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using QBrainAi.Support.Mcp.Tests.Memory;
using QBrainAi.Support.Mcp.Web;
using Xunit;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Web;

/// <summary>
/// TR-MCP-USECASE-007 AC-1 / TEST-MCP-USECASE-008: the web root never hands out the content of a Markdown file. Folder
/// README.md files under wwwroot document the folder for maintainers; they stay out of build and publish output, and the
/// static file middleware does not serve a Markdown file even when a server runs from the source tree, where wwwroot is
/// read from disk. UI assets (.html, .js, .css) are still served. Each middleware case runs the real
/// <see cref="StaticFileMiddleware"/> over a temporary web root, so no case depends on the machine or the content root.
/// </summary>
public sealed class StaticWebRootMarkdownTests
{
    /// <summary>Markdown files in the temporary web root; none may be served. notes.MD checks the case of the extension.</summary>
    private static readonly string[] MarkdownFiles = ["README.md", "memory/README.md", "usecases/README.md", "notes.MD"];

    /// <summary>UI assets in the temporary web root; each must be served.</summary>
    private static readonly string[] UiAssetFiles = ["index.html", "usecases/app.js", "memory/site.css"];

    /// <summary>Named faults the contract must reject.</summary>
    public static TheoryData<string> Faults => new()
    {
        "DefaultOptionsServeMarkdown",
        "UnknownFileTypesServed",
        "UiAssetTypeDropped",
    };

    /// <summary>
    /// Mock gate: the scripted reference options (default content types without .md) satisfy the contract: no Markdown
    /// file is served and every UI asset is.
    /// </summary>
    [Fact]
    public async Task ScriptedReference_SatisfiesTheContract()
    {
        await AssertServesUiAssetsButNoMarkdownAsync(ScriptedReferenceOptions).ConfigureAwait(true);
    }

    /// <summary>
    /// Negative controls: each named fault (the default options, which map .md; serving unknown file types; dropping
    /// a UI asset type) is rejected by the contract inside this passing test.
    /// </summary>
    /// <param name="fault">The named fault.</param>
    [Theory]
    [MemberData(nameof(Faults))]
    public async Task WrongOptions_AreRejectedByTheContract(string fault)
    {
        await Assert.ThrowsAnyAsync<XunitException>(() => AssertServesUiAssetsButNoMarkdownAsync(() => FaultOptions(fault))).ConfigureAwait(true);
    }

    /// <summary>
    /// Mock gate for the wiring check: a Program.cs text that passes the policy to UseStaticFiles is accepted, and texts
    /// that call UseStaticFiles bare or with other options are rejected.
    /// </summary>
    [Fact]
    public void WiringCheck_AcceptsThePolicyCallAndRejectsOthers()
    {
        Assert.True(WiresStaticWebRootPolicy("app.UseDefaultFiles();\napp.UseStaticFiles(StaticWebRoot.CreateOptions());\n"));
        Assert.False(WiresStaticWebRootPolicy("app.UseDefaultFiles();\napp.UseStaticFiles();\n"));
        Assert.False(WiresStaticWebRootPolicy("app.UseStaticFiles(new StaticFileOptions());\n"));
        Assert.False(WiresStaticWebRootPolicy("app.UseStaticFiles(StaticWebRoot.CreateOptions());\napp.UseStaticFiles();\n"));
    }

    /// <summary>
    /// Mock gate for the output check: a project text that removes wwwroot README.md files from Content is accepted, and
    /// texts without the removal, or removing a different pattern, are rejected.
    /// </summary>
    [Fact]
    public void OutputCheck_AcceptsTheReadmeRemovalAndRejectsOthers()
    {
        Assert.True(ExcludesWwwrootReadmes("<ItemGroup>\n  <Content Remove=\"wwwroot\\**\\README.md\" />\n</ItemGroup>"));
        Assert.False(ExcludesWwwrootReadmes("<ItemGroup>\n  <Content Include=\"templates\\**\\*.yaml\" />\n</ItemGroup>"));
        Assert.False(ExcludesWwwrootReadmes("<ItemGroup>\n  <Content Remove=\"wwwroot\\README.md\" />\n</ItemGroup>"));
        Assert.False(ExcludesWwwrootReadmes("<!-- <Content Remove=\"wwwroot\\**\\README.md\" /> -->"));
    }

    /// <summary>
    /// Binding: the host project keeps wwwroot README.md files out of Content, so they are not static web assets and
    /// are not copied to build or publish output.
    /// </summary>
    [Fact]
    public void HostProject_ExcludesWwwrootReadmesFromContent()
    {
        Assert.True(ExcludesWwwrootReadmes(MemoryS6Catalog.ReadHostCsproj()), "QBrainAi.Support.Mcp.csproj must remove wwwroot\\**\\README.md from Content.");
    }

    /// <summary>
    /// Binding: the production options (<see cref="StaticWebRoot.CreateOptions"/>) satisfy the same contract as the
    /// scripted reference: no Markdown file is served and every UI asset is.
    /// </summary>
    [Fact]
    public async Task ProductionOptions_SatisfyTheContract()
    {
        await AssertServesUiAssetsButNoMarkdownAsync(StaticWebRoot.CreateOptions).ConfigureAwait(true);
    }

    /// <summary>Binding: the host's Program.cs passes the production options to every UseStaticFiles call.</summary>
    [Fact]
    public void HostProgram_PassesThePolicyToUseStaticFiles()
    {
        Assert.True(WiresStaticWebRootPolicy(MemoryS6Catalog.ReadProgramSource()), "Program.cs must call UseStaticFiles(StaticWebRoot.CreateOptions()).");
    }

    /// <summary>Scripted reference: default content types with .md removed, unknown types not served.</summary>
    private static StaticFileOptions ScriptedReferenceOptions()
    {
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings.Remove(".md");
        return new StaticFileOptions { ContentTypeProvider = contentTypes };
    }

    /// <summary>Builds the options for one named fault.</summary>
    private static StaticFileOptions FaultOptions(string fault)
    {
        switch (fault)
        {
            case "DefaultOptionsServeMarkdown":
                return new StaticFileOptions();
            case "UnknownFileTypesServed":
                var unknown = ScriptedReferenceOptions();
                unknown.ServeUnknownFileTypes = true;
                return unknown;
            case "UiAssetTypeDropped":
                var dropped = new FileExtensionContentTypeProvider();
                dropped.Mappings.Remove(".md");
                dropped.Mappings.Remove(".js");
                return new StaticFileOptions { ContentTypeProvider = dropped };
            default:
                throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown fault.");
        }
    }

    /// <summary>The contract: over a temporary web root, no Markdown file is served and every UI asset is.</summary>
    private static async Task AssertServesUiAssetsButNoMarkdownAsync(Func<StaticFileOptions> createOptions)
    {
        using var root = new TemporaryWebRoot([.. MarkdownFiles, .. UiAssetFiles]);
        foreach (var file in MarkdownFiles)
        {
            var served = await ServeAsync(createOptions(), root.Path, file).ConfigureAwait(true);
            Assert.False(served.Served, $"Markdown file {file} must not be served (status {served.StatusCode}).");
        }

        foreach (var file in UiAssetFiles)
        {
            var served = await ServeAsync(createOptions(), root.Path, file).ConfigureAwait(true);
            Assert.True(served.Served, $"UI asset {file} must be served (status {served.StatusCode}).");
            Assert.Equal(TemporaryWebRoot.ContentOf(file), served.Body);
        }
    }

    /// <summary>Runs one GET through a real StaticFileMiddleware rooted at the folder.</summary>
    private static async Task<(bool Served, int StatusCode, string Body)> ServeAsync(StaticFileOptions options, string webRoot, string file)
    {
        options.FileProvider = new PhysicalFileProvider(webRoot);
        var nextCalled = false;
        var middleware = new StaticFileMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new StubWebHostEnvironment(webRoot),
            Microsoft.Extensions.Options.Options.Create(options),
            NullLoggerFactory.Instance);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/" + file;
        using var body = new MemoryStream();
        context.Response.Body = body;
        await middleware.Invoke(context).ConfigureAwait(true);
        var text = System.Text.Encoding.UTF8.GetString(body.ToArray());
        return (!nextCalled && context.Response.StatusCode == StatusCodes.Status200OK && text.Length > 0, context.Response.StatusCode, text);
    }

    /// <summary>True when the text calls UseStaticFiles only with StaticWebRoot.CreateOptions().</summary>
    private static bool WiresStaticWebRootPolicy(string programSource)
    {
        var calls = System.Text.RegularExpressions.Regex.Matches(programSource, @"\.UseStaticFiles\((?<args>[^;]*)\);");
        return calls.Count > 0 && calls.All(call => call.Groups["args"].Value.Trim() == "StaticWebRoot.CreateOptions()");
    }

    /// <summary>True when the project text removes wwwroot\**\README.md from Content outside a comment.</summary>
    private static bool ExcludesWwwrootReadmes(string projectText)
    {
        var withoutComments = System.Text.RegularExpressions.Regex.Replace(projectText, "<!--.*?-->", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);
        return withoutComments.Contains("<Content Remove=\"wwwroot\\**\\README.md\" />", StringComparison.Ordinal);
    }

    /// <summary>A temporary web root holding the named files, each with distinct text; deleted on dispose.</summary>
    private sealed class TemporaryWebRoot : IDisposable
    {
        public TemporaryWebRoot(IEnumerable<string> files)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "static-web-root-" + Guid.NewGuid().ToString("N"));
            foreach (var file in files)
            {
                var full = System.IO.Path.Combine(Path, file);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
                File.WriteAllText(full, ContentOf(file));
            }
        }

        public string Path { get; }

        public static string ContentOf(string file) => "content of " + file;

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }

    /// <summary>Minimal host environment whose web root is the temporary folder.</summary>
    private sealed class StubWebHostEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;

        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(webRoot);

        public string ApplicationName { get; set; } = "StaticWebRootMarkdownTests";

        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(webRoot);

        public string ContentRootPath { get; set; } = webRoot;

        public string EnvironmentName { get; set; } = "Development";
    }
}
