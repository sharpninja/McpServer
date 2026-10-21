using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.Tools.Git;
using Serilog;

partial class Build
{
    /// <summary>TR-QB-TRUST-001: select one or more supported REPL RIDs; defaults to all six.</summary>
    [Parameter("REPL bundle runtimes (default: linux-x64 linux-arm64 osx-x64 osx-arm64 win-x64 win-arm64)")]
    readonly string[] ReplRuntimeIdentifiers = ReplBundleBuilder.SupportedRuntimes.ToArray();

    /// <summary>Publish self-contained, hashed REPL distributions without installing or deploying them.</summary>
    public Target BuildReplBundles => _ => _
        .Executes(() =>
        {
            var version = ResolveNuGetPackageVersion(PackageVersion, RootDirectory / "GitVersion.yml");
            var commit = GitTasks.Git("rev-parse HEAD", workingDirectory: RootDirectory, logOutput: false)
                .Select(line => line.Text).Single().Trim();
            var dirty = GitTasks.Git("status --porcelain", workingDirectory: RootDirectory, logOutput: false).Count > 0;
            var project = SourceDirectory / "QBrainAi.Repl.Host" / "QBrainAi.Repl.Host.csproj";
            var manifest = ReplBundleBuilder.Build(ArtifactsDirectory / "repl-bundles", RootDirectory / "LICENSE",
                version, commit, dirty, ReplRuntimeIdentifiers, request =>
                {
                    var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, WorkingDirectory = RootDirectory };
                    foreach (var argument in new[]
                    {
                        "publish", project.ToString(), "--configuration", Configuration, "--runtime", request.RuntimeIdentifier,
                        "--self-contained", "true", "--output", request.OutputDirectory, "--artifacts-path", request.IntermediateDirectory,
                        "-p:PackAsTool=false", "-p:QBrainReplBundle=true", "-p:UseAppHost=true", "-p:PublishSingleFile=false",
                        "-p:PublishTrimmed=false", "-p:Version=" + version, "-p:PackageVersion=" + version, "-p:InformationalVersion=" + version,
                    }) start.ArgumentList.Add(argument);
                    using var publish = Process.Start(start) ?? throw new InvalidOperationException("Could not launch REPL publish.");
                    publish.WaitForExit();
                    if (publish.ExitCode != 0) throw new InvalidOperationException($"REPL publish failed for {request.RuntimeIdentifier} (exit {publish.ExitCode}).");
                    CopyReplRuntimeNotices(request);
                    if (request.RuntimeIdentifier == RuntimeInformation.RuntimeIdentifier) VerifyNativeReplBundle(request);
                });
            Log.Information("REPL bundle manifest: {Manifest}. Cross-platform execution must be validated on each target host.", manifest);
        });

    private static void CopyReplRuntimeNotices(ReplBundlePublishRequest request)
    {
        using var configuration = JsonDocument.Parse(File.ReadAllText(Path.Combine(request.OutputDirectory, "qbrain-ai-repl.runtimeconfig.json")));
        var framework = configuration.RootElement.GetProperty("runtimeOptions").GetProperty("includedFrameworks")
            .EnumerateArray().Single(item => item.GetProperty("name").GetString() == "Microsoft.NETCore.App");
        var runtimeVersion = framework.GetProperty("version").GetString();
        if (!Version.TryParse(runtimeVersion, out _)) throw new InvalidDataException("Invalid bundled .NET runtime version.");
        var assets = Path.Combine(request.IntermediateDirectory, "obj", "QBrainAi.Repl.Host", "project.assets.json");
        using var restore = JsonDocument.Parse(File.ReadAllText(assets));
        var runtimePackage = restore.RootElement.GetProperty("packageFolders").EnumerateObject()
            .Where(folder => Path.IsPathFullyQualified(folder.Name))
            .Select(folder => Path.Combine(folder.Name, "microsoft.netcore.app.runtime." + request.RuntimeIdentifier, runtimeVersion!))
            .FirstOrDefault(folder => File.Exists(Path.Combine(folder, "LICENSE.TXT")) && File.Exists(Path.Combine(folder, "THIRD-PARTY-NOTICES.TXT")))
            ?? throw new InvalidDataException("Resolved .NET runtime license and third-party notices were not found.");
        File.Copy(Path.Combine(runtimePackage, "LICENSE.TXT"), Path.Combine(request.OutputDirectory, "LICENSE.NET.txt"));
        File.Copy(Path.Combine(runtimePackage, "THIRD-PARTY-NOTICES.TXT"), Path.Combine(request.OutputDirectory, "THIRD-PARTY-NOTICES.NET.txt"));
    }

    private static void VerifyNativeReplBundle(ReplBundlePublishRequest request)
    {
        var executable = Path.Combine(request.OutputDirectory, OperatingSystem.IsWindows() ? "qbrain-ai-repl.exe" : "qbrain-ai-repl");
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, WorkingDirectory = request.OutputDirectory,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        start.ArgumentList.Add("--version");
        // Prove the native apphost can start with no discoverable installed SDK
        // or shared runtime. This does not claim marker/server integration.
        start.Environment["PATH"] = request.OutputDirectory;
        foreach (var key in new[] { "DOTNET_ROOT", "DOTNET_ROOT_X64", "DOTNET_ROOT_ARM64", "DOTNET_ROOT(x86)" })
            start.Environment[key] = Path.Combine(request.IntermediateDirectory, "no-shared-runtime");
        start.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not launch native REPL bundle.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Native REPL bundle version smoke exceeded 30 seconds.");
        }
        var version = stdout.GetAwaiter().GetResult().Trim();
        var error = stderr.GetAwaiter().GetResult();
        if (process.ExitCode != 0 || (version != request.Version && !version.StartsWith(request.Version + "+", StringComparison.Ordinal)))
            throw new InvalidDataException($"Native REPL bundle version smoke failed (exit {process.ExitCode}): {version} {error}");
        Log.Information("Native REPL smoke passed for {Runtime}: {Version}", request.RuntimeIdentifier, version);
    }
}
