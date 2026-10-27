// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("Explicit native JSON catalog containing pinned staged runtime/module files and original per-package license provenance. No download fallback.")]
    readonly string ReplBundleCatalog = string.Empty;

    [Parameter("Runtime identifiers to publish. All six are required for P5; Windows P1 may request its exact native platform.")]
    readonly string[] ReplBundleRids = ["linux-x64", "linux-arm64", "osx-x64", "osx-arm64", "win-x64", "win-arm64"];

    /// <summary>
    /// FR-QB-005/TR-QB-RUNTIME-001: Publish self-contained REPL executables and
    /// package explicit compatible offline PowerShell/modules with original licenses.
    /// PackReplTool remains the separate NuGet tool target.
    /// </summary>
    public Target PublishReplBundles => _ => _
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(ReplBundleCatalog) || !File.Exists(ReplBundleCatalog))
                throw new InvalidOperationException("Pinned offline runtime/module/provenance catalog is required; no automatic installation.");
            using var catalog = JsonDocument.Parse(File.ReadAllText(ReplBundleCatalog));
            var platforms = catalog.RootElement.GetProperty("rids");
            var allowed = new HashSet<string>(["linux-x64", "linux-arm64", "osx-x64", "osx-arm64", "win-x64", "win-arm64"], StringComparer.Ordinal);
            if (ReplBundleRids.Length == 0 || ReplBundleRids.Distinct(StringComparer.Ordinal).Count() != ReplBundleRids.Length)
                throw new InvalidOperationException("A nonempty unique platform set is required.");
            foreach (var rid in ReplBundleRids)
            {
                if (!allowed.Contains(rid) || !platforms.TryGetProperty(rid, out var entry))
                    throw new InvalidOperationException("Required exact platform catalog missing: " + rid);
                var stage = Path.GetFullPath(entry.GetProperty("sourceDirectory").GetString()
                    ?? throw new InvalidOperationException("Explicit private staging root required."));
                var privateRoot = Path.Combine(RootDirectory, ".mcpServer") + Path.DirectorySeparatorChar;
                if (!stage.StartsWith(privateRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Bundle staging must stay in this checkout's private artifact root.");
                var spec = entry.GetProperty("spec");
                if (spec.GetProperty("rid").GetString() != rid)
                    throw new InvalidOperationException("Platform inventory mismatch.");
                var version = spec.GetProperty("version").GetString()
                    ?? throw new InvalidOperationException("Pinned version required.");
                DotNetPublish(settings => settings
                    .SetProject(SourceDirectory / "QBrainAi.Repl.Host" / "QBrainAi.Repl.Host.csproj")
                    .SetConfiguration(Configuration)
                    .SetRuntime(rid)
                    .SetSelfContained(true)
                    .SetProperty("PackAsTool", "false")
                    .SetProperty("Version", version)
                    .SetOutput(Path.Combine(stage, "repl")));
                var extension = rid.StartsWith("win-", StringComparison.Ordinal) ? ".exe" : string.Empty;
                var originalAppHost = Path.Combine(stage, "repl", "QBrainAi.Repl.Host" + extension);
                var bundledAppHost = Path.Combine(stage, "repl", "qbrain-ai-repl" + extension);
                if (!File.Exists(originalAppHost))
                    throw new InvalidOperationException("Original published REPL apphost is missing.");
                if (File.Exists(bundledAppHost))
                {
                    if (!File.ReadAllBytes(originalAppHost).AsSpan().SequenceEqual(File.ReadAllBytes(bundledAppHost)))
                        throw new InvalidOperationException("Existing apphost alias differs");
                }
                else File.Copy(originalAppHost, bundledAppHost);
                Console.WriteLine($"[QB-BUNDLE-APPHOST] {originalAppHost} -> {bundledAppHost}");
            }
        });

    /// <summary>TR-QB-RUNTIME-001: Package every requested platform only after exact offline provenance is supplied.</summary>
    public Target BuildReplBundles => _ => _
        .DependsOn(PublishReplBundles)
        .Executes(() =>
        {
            foreach (var rid in ReplBundleRids) PublishOfflineReplArchive(rid);
        });

    private void PublishOfflineReplArchive(string rid)
    {
                var start = new ProcessStartInfo(OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh")
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    WorkingDirectory = RootDirectory,
                };
                foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-File",
                             Path.Combine(RootDirectory, "scripts", "Build-ReplRuntimeBundle.ps1"),
                             "-CatalogPath", Path.GetFullPath(ReplBundleCatalog), "-RuntimeIdentifier", rid })
                    start.ArgumentList.Add(argument);
                using var process = Process.Start(start) ?? throw new InvalidOperationException("Bundle publisher did not start.");
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                Console.Out.Write(stdout.GetAwaiter().GetResult());
                Console.Error.Write(stderr.GetAwaiter().GetResult());
                if (process.ExitCode != 0) throw new InvalidOperationException("Exact offline bundle publication failed: " + rid);
    }
}