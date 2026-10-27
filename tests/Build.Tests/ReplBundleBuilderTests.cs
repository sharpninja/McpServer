using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace NukeBuild.Tests;

/// <summary>TEST-QB-003/055: bundle contract tests use fake publish output, never claim native runtime execution.</summary>
public sealed class ReplBundleBuilderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "qb-repl-bundles-" + Guid.NewGuid().ToString("N"));
    private string Output => Path.Combine(root, "artifacts");
    private string License => Path.Combine(root, "LICENSE");

    /// <summary>Create an isolated source-license fixture for each test.</summary>
    public ReplBundleBuilderTests()
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(License, "Project license fixture: preserve these bytes without guessing a license identifier.");
    }

    /// <summary>Remove only this test's isolated files.</summary>
    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>Every supported RID retains complete files, source provenance, licenses and verifiable archive bytes.</summary>
    [Theory]
    [InlineData("linux-x64")]
    [InlineData("linux-arm64")]
    [InlineData("osx-x64")]
    [InlineData("osx-arm64")]
    [InlineData("win-x64")]
    [InlineData("win-arm64")]
    public void Bundle_CanBeExtractedAndVerified(string runtime)
    {
        var calls = new List<ReplBundlePublishRequest>();
        var manifestPath = Build([runtime], request => { calls.Add(request); PublishFixture(request); });
        Assert.Single(calls);
        Assert.Equal(runtime, calls[0].RuntimeIdentifier);
        Assert.NotEqual(calls[0].OutputDirectory, calls[0].IntermediateDirectory);
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var value = manifest.RootElement;
        Assert.Equal("1.2.3", value.GetProperty("version").GetString());
        Assert.Equal(new string('a', 40), value.GetProperty("sourceCommit").GetString());
        Assert.True(value.GetProperty("sourceDirty").GetBoolean());
        Assert.Equal("LICENSE.QBrainAI.txt", value.GetProperty("projectLicenseFile").GetString());
        Assert.False(value.TryGetProperty("license", out _));
        var bundle = Assert.Single(value.GetProperty("bundles").EnumerateArray());
        var archivePath = Path.Combine(Path.GetDirectoryName(manifestPath)!, bundle.GetProperty("archive").GetString()!);
        Assert.Equal(Hash(archivePath), bundle.GetProperty("sha256").GetString());
        var extracted = Path.Combine(root, "extracted");
        Directory.CreateDirectory(extracted);
        using (var input = File.OpenRead(archivePath))
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            TarFile.ExtractToDirectory(gzip, extracted, overwriteFiles: false);
        Assert.Equal(File.ReadAllText(License), File.ReadAllText(Path.Combine(extracted, "LICENSE.QBrainAI.txt")));
        foreach (var file in bundle.GetProperty("files").EnumerateArray())
        {
            var path = Path.Combine(extracted, file.GetProperty("path").GetString()!);
            Assert.Equal(Hash(path), file.GetProperty("sha256").GetString());
            Assert.Equal(new FileInfo(path).Length, file.GetProperty("length").GetInt64());
        }
        Assert.Equal("nested supporting asset", File.ReadAllText(Path.Combine(extracted, "assets", "support.txt")));
        if (!OperatingSystem.IsWindows() && !runtime.StartsWith("win-", StringComparison.Ordinal))
        {
            var mode = File.GetUnixFileMode(Path.Combine(extracted, "qbrain-ai-repl"));
            Assert.NotEqual((UnixFileMode)0, mode & UnixFileMode.UserExecute);
        }
    }

    /// <summary>The distribution inventory covers exactly the six platforms promised by the QB plan.</summary>
    [Fact]
    public void SupportedRuntimes_AreTheRequiredSix()
    {
        Assert.Equal(new[] { "linux-x64", "linux-arm64", "osx-x64", "osx-arm64", "win-x64", "win-arm64" },
            ReplBundleBuilder.SupportedRuntimes);
    }

    /// <summary>The supported PowerShell build entry point must expose the bundle target to callers.</summary>
    [Fact]
    public void Nuke_ExposesBuildReplBundlesTarget() => Assert.NotNull(typeof(Build).GetProperty("BuildReplBundles"));

    /// <summary>Unsupported or duplicate runtime selection cannot launch a publisher or create artifact files.</summary>
    [Theory]
    [InlineData("../linux-x64")]
    [InlineData("linux-musl-x64")]
    [InlineData("win-x86")]
    public void InvalidRuntime_RefusedBeforePublish(string runtime)
    {
        var calls = 0;
        Assert.Throws<ArgumentException>(() => Build([runtime], _ => calls++));
        Assert.Equal(0, calls);
        Assert.False(Directory.Exists(Output));
    }

    /// <summary>Duplicate RID selection is rejected instead of silently replacing one result.</summary>
    [Fact]
    public void DuplicateRuntime_RefusedBeforePublish() =>
        Assert.Throws<ArgumentException>(() => Build(["linux-x64", "linux-x64"], _ => throw new Exception("Publisher must not run")));

    /// <summary>Framework-dependent output or a missing native runtime cannot masquerade as a self-contained bundle.</summary>
    [Theory]
    [InlineData("apphost")]
    [InlineData("runtime")]
    [InlineData("framework")]
    public void IncompletePublish_NeverProducesManifest(string damage)
    {
        Assert.Throws<InvalidDataException>(() => Build(["linux-x64"], request =>
        {
            PublishFixture(request);
            if (damage == "apphost") File.Delete(Path.Combine(request.OutputDirectory, "qbrain-ai-repl"));
            if (damage == "runtime") File.Delete(Path.Combine(request.OutputDirectory, "libcoreclr.so"));
            if (damage == "framework") File.WriteAllText(Path.Combine(request.OutputDirectory, "qbrain-ai-repl.runtimeconfig.json"),
                "{\"runtimeOptions\":{\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}");
        }));
        Assert.Empty(Directory.GetFiles(Output, "manifest.json", SearchOption.AllDirectories));
    }

    /// <summary>One failed platform leaves no success manifest and preserves the earlier independent artifact snapshot.</summary>
    [Fact]
    public void FailedLaterPublish_PreservesEarlierSnapshot()
    {
        var previous = Build(["linux-x64"], PublishFixture);
        var previousBytes = File.ReadAllBytes(previous);
        var calls = 0;
        Assert.Throws<IOException>(() => Build(["linux-x64", "win-x64"], request =>
        {
            if (++calls == 2) throw new IOException("Fixture publish failed");
            PublishFixture(request);
        }));
        Assert.Equal(previousBytes, File.ReadAllBytes(previous));
        Assert.Equal(new[] { previous }, Directory.GetFiles(Output, "manifest.json", SearchOption.AllDirectories));
    }

    private string Build(string[] runtimes, Action<ReplBundlePublishRequest> publish) =>
        ReplBundleBuilder.Build(Output, License, "1.2.3", new string('a', 40), true, runtimes, publish);

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void PublishFixture(ReplBundlePublishRequest request)
    {
        var windows = request.RuntimeIdentifier.StartsWith("win-", StringComparison.Ordinal);
        var extension = windows ? ".dll" : request.RuntimeIdentifier.StartsWith("osx-", StringComparison.Ordinal) ? ".dylib" : ".so";
        Directory.CreateDirectory(request.OutputDirectory);
        var names = new[] { windows ? "qbrain-ai-repl.exe" : "qbrain-ai-repl", "qbrain-ai-repl.dll", "qbrain-ai-repl.deps.json",
            "System.Private.CoreLib.dll", (windows ? "" : "lib") + "hostfxr" + extension,
            (windows ? "" : "lib") + "hostpolicy" + extension, (windows ? "" : "lib") + "coreclr" + extension };
        foreach (var name in names) File.WriteAllText(Path.Combine(request.OutputDirectory, name), "Mock publish fixture only");
        File.WriteAllText(Path.Combine(request.OutputDirectory, "qbrain-ai-repl.runtimeconfig.json"),
            "{\"runtimeOptions\":{\"includedFrameworks\":[{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}]}}");
        Directory.CreateDirectory(Path.Combine(request.OutputDirectory, "assets"));
        File.WriteAllText(Path.Combine(request.OutputDirectory, "assets", "support.txt"), "nested supporting asset");
    }
}
