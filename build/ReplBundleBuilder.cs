using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>TR-QB-TRUST-001: isolated self-contained REPL publish request.</summary>
internal sealed record ReplBundlePublishRequest(string RuntimeIdentifier, string OutputDirectory, string IntermediateDirectory, string Version);

/// <summary>TR-QB-TRUST-001: create verified, independently versioned REPL distribution bundles.</summary>
internal static class ReplBundleBuilder
{
    /// <summary>Every platform required by the cline-qb distribution contract.</summary>
    internal static IReadOnlyList<string> SupportedRuntimes { get; } = Array.AsReadOnly(new[]
    {
        "linux-x64", "linux-arm64", "osx-x64", "osx-arm64", "win-x64", "win-arm64",
    });

    /// <summary>Publish selected runtimes and return a manifest only after every bundle validates.</summary>
    internal static string Build(string outputRoot, string licensePath, string version, string sourceCommit,
        bool sourceDirty, IReadOnlyList<string> runtimes, Action<ReplBundlePublishRequest> publish)
    {
        ArgumentNullException.ThrowIfNull(publish);
        if (!Path.IsPathFullyQualified(outputRoot)) throw new ArgumentException("Bundle output must be absolute.", nameof(outputRoot));
        if (runtimes.Count == 0 || runtimes.Distinct(StringComparer.Ordinal).Count() != runtimes.Count ||
            runtimes.Any(runtime => !SupportedRuntimes.Contains(runtime, StringComparer.Ordinal)))
            throw new ArgumentException("Select distinct runtimes from the supported six-platform inventory.", nameof(runtimes));
        if (!Regex.IsMatch(version, @"^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)*$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Bundle version must be a semantic version.", nameof(version));
        if (!Regex.IsMatch(sourceCommit, @"^(?:[a-fA-F0-9]{40}|[a-fA-F0-9]{64})$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Bundle provenance requires a full Git commit.", nameof(sourceCommit));
        if (!File.Exists(licensePath)) throw new FileNotFoundException("REPL source license is required.", licensePath);

        // Each attempt has a separate directory. Failure cannot overwrite an
        // earlier successful snapshot, and no manifest represents partial work.
        var runRoot = Path.Combine(outputRoot, DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(runRoot);
        var bundles = new List<BundleArtifact>();
        foreach (var runtime in runtimes)
        {
            var output = Path.Combine(runRoot, runtime);
            publish(new(runtime, output, Path.Combine(runRoot, "intermediate", runtime), version));
            var files = EnumerateSafeFiles(output);
            var executable = runtime.StartsWith("win-", StringComparison.Ordinal) ? "qbrain-ai-repl.exe" : "qbrain-ai-repl";
            ValidateSelfContained(output, runtime, executable);
            File.Copy(licensePath, Path.Combine(output, "LICENSE.QBrainAI.txt"), overwrite: false);
            files.Add(Path.Combine(output, "LICENSE.QBrainAI.txt"));
            var archiveName = $"qbrain-ai-repl-{version}-{runtime}.tar.gz";
            var archivePath = Path.Combine(runRoot, archiveName);
            var inventory = WriteArchive(archivePath, output, executable, files);
            bundles.Add(new(runtime, executable, archiveName, HashFile(archivePath), inventory));
        }

        var manifest = new
        {
            schemaVersion = 1, artifact = "qbrain-ai-repl", version, sourceCommit, sourceDirty,
            projectLicenseFile = "LICENSE.QBrainAI.txt", createdAtUtc = DateTimeOffset.UtcNow, bundles,
        };
        var temporary = Path.Combine(runRoot, "manifest.json.tmp");
        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, manifest, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            stream.Flush(flushToDisk: true);
        }
        var path = Path.Combine(runRoot, "manifest.json");
        File.Move(temporary, path);
        return path;
    }

    private static List<string> EnumerateSafeFiles(string directory)
    {
        if (!Directory.Exists(directory)) throw new InvalidDataException("Publisher produced no output directory.");
        var files = new List<string>();
        var directories = new Stack<string>();
        directories.Push(directory);
        while (directories.TryPop(out var current))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Bundle output cannot contain symbolic links.");
            foreach (var entry in Directory.EnumerateFileSystemEntries(current))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Bundle output cannot contain symbolic links.");
                if ((attributes & FileAttributes.Directory) != 0) directories.Push(entry);
                else files.Add(entry);
            }
        }
        return files;
    }

    private static void ValidateSelfContained(string output, string runtime, string executable)
    {
        var windows = runtime.StartsWith("win-", StringComparison.Ordinal);
        var extension = windows ? ".dll" : runtime.StartsWith("osx-", StringComparison.Ordinal) ? ".dylib" : ".so";
        var prefix = windows ? "" : "lib";
        var required = new[] { executable, "qbrain-ai-repl.dll", "qbrain-ai-repl.deps.json", "qbrain-ai-repl.runtimeconfig.json",
            "System.Private.CoreLib.dll", prefix + "hostfxr" + extension, prefix + "hostpolicy" + extension, prefix + "coreclr" + extension };
        foreach (var name in required)
        {
            var file = new FileInfo(Path.Combine(output, name));
            if (!file.Exists || file.Length == 0) throw new InvalidDataException($"Self-contained {runtime} output lacks {name}.");
        }
        try
        {
            using var configuration = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "qbrain-ai-repl.runtimeconfig.json")));
            var options = configuration.RootElement.GetProperty("runtimeOptions");
            if (options.TryGetProperty("framework", out _) || options.TryGetProperty("frameworks", out _) ||
                !options.TryGetProperty("includedFrameworks", out var frameworks) || frameworks.ValueKind != JsonValueKind.Array ||
                !frameworks.EnumerateArray().Any(item => item.GetProperty("name").GetString() == "Microsoft.NETCore.App"))
                throw new InvalidDataException("REPL runtime configuration must declare its included .NET runtime.");
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidDataException("REPL runtime configuration is invalid.", error);
        }
    }

    private static List<BundleFile> WriteArchive(string archive, string root, string executable, IEnumerable<string> files)
    {
        var inventory = new List<BundleFile>();
        using var output = new FileStream(archive, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        using (var writer = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: true))
        {
            foreach (var file in files.OrderBy(file => Path.GetRelativePath(root, file), StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
                // Hash the same snapshot of bytes placed in the archive.
                var bytes = File.ReadAllBytes(file);
                using var data = new MemoryStream(bytes, writable: false);
                var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
                if (relative == executable) mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, relative)
                {
                    DataStream = data, Mode = mode, ModificationTime = DateTimeOffset.UnixEpoch,
                });
                inventory.Add(new(relative, bytes.LongLength, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()));
            }
        }
        output.Flush(flushToDisk: true);
        return inventory;
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record BundleFile(string Path, long Length, string Sha256);
    private sealed record BundleArtifact(string RuntimeIdentifier, string Executable, string Archive, string Sha256, List<BundleFile> Files);
}
