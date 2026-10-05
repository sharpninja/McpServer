using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>TR-MCP-SERVICEUPDATE-001: native command result; output is never included in failure messages.</summary>
internal sealed record ServiceCommandResult(int ExitCode, string StandardOutput);

/// <summary>Explicit update paths; the lock-root override exists for isolated tests only.</summary>
internal sealed record LinuxServiceUpdateOptions(string ServiceName, string InstallPath, string PublishPath,
    string BackupRoot, string RuntimeIdentifier, string LockRoot);

/// <summary>Non-secret recovery archive receipt and verified running PID.</summary>
internal sealed record LinuxServiceUpdateResult(string ArchivePath, string ArchiveSha256, int MainPid);

/// <summary>Systemd environment-file path and missing-file policy.</summary>
internal sealed record ServiceEnvironmentFile(string Path, bool Optional);

/// <summary>TR-MCP-SERVICEUPDATE-001: updates an existing systemd unit with retained live-state recovery.</summary>
internal sealed class LinuxServiceHelper(Func<string, IReadOnlyList<string>, ServiceCommandResult> run)
{
    private const string AppHost = "QBrainAi.Support.Mcp";
    private const string LegacyAppHost = "McpServer.Support.Mcp";
    private const UnixFileMode PrivateDirectory = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode PrivateFile = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private const string UnitProperties = "LoadState,Type,FragmentPath,DropInPaths,EnvironmentFiles,Environment,PassEnvironment,WorkingDirectory,ExecStart,MainPID,ActiveState,RootDirectory,RootImage,BindPaths,BindReadOnlyPaths,TemporaryFileSystem,PrivateTmp,MountImages,ExtensionImages,ExtensionDirectories";

    /// <summary>Production command runner drains both streams, uses argument lists and enforces a bounded timeout.</summary>
    public static ServiceCommandResult RunCommand(string command, IReadOnlyList<string> arguments)
    {
        var info = new ProcessStartInfo(command)
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start deployment command.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(15 * 60 * 1000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Deployment command exceeded its time limit.");
        }
        if (!Task.WaitAll([stdout, stderr], TimeSpan.FromSeconds(30)))
            throw new TimeoutException("Deployment command output did not close.");
        return new(process.ExitCode, stdout.Result);
    }

    /// <summary>Validates, stops, archives, replaces, restores and verifies exactly the selected service.</summary>
    public LinuxServiceUpdateResult Update(LinuxServiceUpdateOptions options, Action copyRuntimeConfig, Action verifyHealth)
    {
        if (Command("id", "-u").Trim() != "0")
            throw new InvalidOperationException("Linux UpdateService requires root elevation.");
        if (!Regex.IsMatch(options.ServiceName, @"^[A-Za-z0-9_@.-]+\.service$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Specify one .service unit name.");
        var install = SafePath(options.InstallPath);
        var stage = SafePath(options.PublishPath);
        var backupRoot = SafePath(options.BackupRoot);
        var lockRoot = SafePath(options.LockRoot);
        RejectOverlap(install, stage);
        RejectOverlap(backupRoot, install);
        RejectOverlap(backupRoot, stage);
        RejectOverlap(lockRoot, install);
        RejectOverlap(lockRoot, stage);
        RejectOverlap(lockRoot, backupRoot);
        MakePrivateDirectory(lockRoot);
        // Unit aliases share an installation and must therefore share the same update lock.
        var lockPath = Path.Combine(lockRoot, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(install))) + ".lock");
        EnsureNoLinks(lockPath);
        using var serviceLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        SetPrivateFile(lockPath);
        if (!Command("tar", "--version").Contains("GNU tar", StringComparison.Ordinal))
            throw new InvalidOperationException("GNU tar is required to preserve service metadata.");
        ValidateStage(stage, options.RuntimeIdentifier);
        var unit = GetUnit(options.ServiceName);
        ValidateUnit(unit, install);
        ValidateConfiguration(unit, install);
        var preserved = WindowsServiceHelper.GetPreservedStatePaths(install).ToList();
        preserved.Add(RequiredUnitFile(Value(unit, "FragmentPath")));
        preserved.AddRange(ParseWords(Value(unit, "DropInPaths")).Select(RequiredUnitFile));
        foreach (var file in ParseEnvironmentFiles(Value(unit, "EnvironmentFiles")))
        {
            var path = SafePath(file.Path);
            if (File.Exists(path)) preserved.Add(path);
            else if (!file.Optional) throw new FileNotFoundException("Required service environment file is missing.");
        }
        foreach (var path in preserved)
        {
            SafePath(path);
            RejectOverlap(path, backupRoot);
            RejectOverlap(path, stage);
            RejectOverlap(path, lockRoot);
            if (Within(install, path))
                throw new InvalidOperationException("A preservation root must not contain the application installation.");
        }
        preserved = preserved.Distinct(StringComparer.Ordinal)
            .Where(path => !preserved.Any(parent => parent != path && Within(path, parent))).ToList();
        MakePrivateDirectory(backupRoot);
        var archiveDirectory = Path.Combine(backupRoot, DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N"));
        MakePrivateDirectory(archiveDirectory);
        var archive = Path.Combine(archiveDirectory, "preserved-state.tar");
        string? hash = null;
        Stop(options.ServiceName);
        try
        {
            using (File.Create(archive)) { }
            SetPrivateFile(archive);
            Command("tar", new[] { "--create", "--file", archive, "--acls", "--xattrs", "--numeric-owner", "--directory", Path.GetPathRoot(install)!, "--" }
                .Concat(preserved.Select(path => Path.GetRelativePath(Path.GetPathRoot(install)!, path))).ToArray());
            Command("tar", "--list", "--file", archive);
            hash = Hash(archive);
            ReplaceFiles(stage, install, preserved);
            EnsureNoLinks(Path.Combine(install, "config", "brain-slots", "quad-brain-slot-assignments.yaml"));
            copyRuntimeConfig();
            Restore(archive, hash, preserved, install);
            RetargetLegacyExecStart(unit, install);
            Command("systemctl", "daemon-reload");
            Command("systemctl", "start", options.ServiceName);
            var pid = WaitForServiceProcess(options.ServiceName, Path.Combine(install, AppHost));
            verifyHealth();
            return new(archive, hash, pid);
        }
        catch (Exception failure)
        {
            if (hash is not null)
            {
                try { Stop(options.ServiceName); Restore(archive, hash, preserved, install); }
                catch (Exception recovery)
                {
                    throw new AggregateException($"Update and live-state restoration failed. Retained archive: {archive}", failure, recovery);
                }
            }
            throw new InvalidOperationException($"Service update failed; recovery evidence retained at {archiveDirectory}. No binary rollback was performed.", failure);
        }
    }

    /// <summary>TR-MCP-SERVICEUPDATE-001: systemd simple units report start before exec; bound retries until the active main process resolves to the deployed apphost.</summary>
    private int WaitForServiceProcess(string service, string executable)
    {
        const int attempts = 100;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var state = GetUnit(service);
            var activeState = Value(state, "ActiveState");
            if (activeState is "failed" or "inactive")
                throw new InvalidOperationException("Updated service stopped before its application process became ready.");
            if (activeState == "active" && int.TryParse(Value(state, "MainPID"), out var pid) && pid > 0)
            {
                var process = run("readlink", ["-f", $"/proc/{pid}/exe"]);
                if (process.ExitCode == 0 && process.StandardOutput.Trim() == executable)
                    return pid;
            }
            if (attempt < attempts - 1) Thread.Sleep(100);
        }
        throw new InvalidOperationException("Updated service did not become active with the expected executable within the startup retry limit.");
    }
    /// <summary>Gets only required systemd properties; sensitive values remain local.</summary>
    private Dictionary<string, string> GetUnit(string name) => Command("systemctl", "show", name, "--no-pager", "--property=" + UnitProperties)
        .Split('\n').Where(line => line.Contains('='))
        .Select(line => line.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1].TrimEnd('\r'), StringComparer.Ordinal);

    /// <summary>Reads an optional property without leaking contents in errors.</summary>
    private static string Value(IReadOnlyDictionary<string, string> unit, string key) => unit.GetValueOrDefault(key, "");

    /// <summary>Checks installed unit identity before any service mutation.</summary>
    private static void ValidateUnit(IReadOnlyDictionary<string, string> unit, string install)
    {
        if (Value(unit, "LoadState") != "loaded" || Value(unit, "Type") is not ("simple" or "exec" or "notify"))
            throw new InvalidOperationException("An existing loaded simple, exec or notify service is required.");
        if (new[] { "RootDirectory", "RootImage", "BindPaths", "BindReadOnlyPaths", "TemporaryFileSystem", "MountImages", "ExtensionImages", "ExtensionDirectories" }
            .Any(property => !string.IsNullOrWhiteSpace(Value(unit, property))) || Value(unit, "PrivateTmp") is not ("" or "no" or "false"))
            throw new InvalidOperationException("Services with filesystem namespace remapping are unsupported.");
        if (Decode(Value(unit, "WorkingDirectory")) != install)
            throw new InvalidOperationException("Service WorkingDirectory must match the selected install path.");
        var start = Value(unit, "ExecStart");
        var executable = Regex.Match(start, @"^\{ path=(.*?) ; argv\[\]=(.*?) ; ignore_errors=", RegexOptions.CultureInvariant);
        if (!executable.Success || start.Count(c => c == '{') != 1 || !IsAcceptedAppHost(install, Decode(executable.Groups[1].Value)))
            throw new InvalidOperationException("Service ExecStart must directly run the selected application host.");
        ValidateArguments(ParseWords(executable.Groups[2].Value));
    }

    /// <summary>Accepts the current host and the 1.x McpServer host under the selected install directory.</summary>
    private static bool IsAcceptedAppHost(string install, string executable)
        => string.Equals(executable, Path.Combine(install, AppHost), StringComparison.Ordinal)
           || string.Equals(executable, Path.Combine(install, LegacyAppHost), StringComparison.Ordinal);

    /// <summary>
    /// TR-MCP-QBRAIN-006: After files are replaced, a preserved unit that still names McpServer.Support.Mcp
    /// is retargeted at QBrainAi.Support.Mcp before daemon-reload. The old binary is no longer on disk.
    /// </summary>
    private void RetargetLegacyExecStart(IReadOnlyDictionary<string, string> unit, string install)
    {
        var start = Value(unit, "ExecStart");
        var executable = Regex.Match(start, @"^\{ path=(.*?) ; argv\[\]=", RegexOptions.CultureInvariant);
        if (!executable.Success)
            return;

        var legacyPath = Path.Combine(install, LegacyAppHost);
        if (!string.Equals(Decode(executable.Groups[1].Value), legacyPath, StringComparison.Ordinal))
            return;

        var fragment = RequiredUnitFile(Value(unit, "FragmentPath"));
        var text = File.ReadAllText(fragment);
        var updated = text.Replace(LegacyAppHost, AppHost, StringComparison.Ordinal);
        if (updated == text)
            throw new InvalidOperationException("Legacy ExecStart could not be retargeted to the deployed application host.");

        File.WriteAllText(fragment, updated);
    }

    /// <summary>Fails closed when configuration precedence could point preservation at the wrong live data.</summary>
    private void ValidateConfiguration(IReadOnlyDictionary<string, string> unit, string install)
    {
        RequireRegularFile(Path.Combine(install, "appsettings.yaml"));
        if (Directory.EnumerateFiles(install, "appsettings*").Any(path => Path.GetFileName(path) != "appsettings.yaml" &&
            (path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))))
            throw new InvalidOperationException("Configuration overlays are not supported by this updater.");
        ValidateEnvironment(ParseWords(Value(unit, "Environment")));
        ValidateEnvironment(ParseWords(Value(unit, "PassEnvironment")));
        foreach (var file in ParseEnvironmentFiles(Value(unit, "EnvironmentFiles")))
        {
            var path = SafePath(file.Path);
            if (File.Exists(path)) { RequireRegularFile(path); ValidateEnvironment(File.ReadAllLines(path)); }
            else if (!file.Optional) throw new FileNotFoundException("Required service environment file is missing.");
        }
        if (int.TryParse(Value(unit, "MainPID"), out var pid) && pid > 0 && OperatingSystem.IsLinux())
        {
            ValidateEnvironment(File.ReadAllText($"/proc/{pid}/environ").Split('\0'));
            ValidateArguments(File.ReadAllText($"/proc/{pid}/cmdline").Split('\0'));
        }
    }

    /// <summary>Detects data/instance/content-root overrides without exposing secret values.</summary>
    private static bool OverridesData(string name)
    {
        name = name.Trim().Trim('"', '\'').TrimStart('-').Replace("__", ":", StringComparison.Ordinal).ToUpperInvariant();
        return name is "DATAFOLDER" or "MCP:DATADIRECTORY" or "MCP_INSTANCE" or "INSTANCE" or "CONTENTROOT" or "ASPNETCORE_CONTENTROOT" or "DOTNET_CONTENTROOT"
            || name.StartsWith("MCP:INSTANCES:", StringComparison.Ordinal);
    }

    /// <summary>Checks environment names only; values are neither logged nor returned.</summary>
    private static void ValidateEnvironment(IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            var line = value.Trim();
            if (line.StartsWith('#') || line.StartsWith(';') || line.Length == 0) continue;
            if (OverridesData(line.Split('=', 2)[0]))
                throw new InvalidOperationException("Data, instance or content-root environment overrides are unsupported.");
        }
    }

    /// <summary>Checks command-line configuration keys including key=value syntax.</summary>
    private static void ValidateArguments(IEnumerable<string> arguments)
    {
        if (arguments.Any(arg => OverridesData(arg.Split('=', 2)[0].TrimStart('/'))))
            throw new InvalidOperationException("Data, instance or content-root arguments are unsupported.");
    }

    /// <summary>Tokenizes systemd's quoted/escaped property words without shell expansion.</summary>
    internal static IReadOnlyList<string> ParseWords(string value)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        char quote = '\0';
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '\\' && i + 1 < value.Length)
            {
                if (value[i + 1] == 'x' && i + 3 < value.Length && byte.TryParse(value.AsSpan(i + 2, 2), System.Globalization.NumberStyles.HexNumber, null, out var code))
                { word.Append((char)code); i += 3; }
                else if (value[i + 1] is '\\' or '"' or '\'' or ' ') word.Append(value[++i]);
                else word.Append(c);
            }
            else if (quote != '\0') { if (c == quote) quote = '\0'; else word.Append(c); }
            else if (c is '"' or '\'') quote = c;
            else if (char.IsWhiteSpace(c)) { if (word.Length > 0) { words.Add(word.ToString()); word.Clear(); } }
            else word.Append(c);
        }
        if (quote != '\0') throw new InvalidDataException("Malformed systemd property quoting.");
        if (word.Length > 0) words.Add(word.ToString());
        return words;
    }

    /// <summary>Decodes one systemd path, retaining embedded spaces.</summary>
    private static string Decode(string value) => string.Join(' ', ParseWords(value));

    /// <summary>Parses systemd EnvironmentFiles paths and optionality, rejecting unrecognized syntax.</summary>
    internal static IReadOnlyList<ServiceEnvironmentFile> ParseEnvironmentFiles(string value)
    {
        var files = new List<ServiceEnvironmentFile>();
        while (!string.IsNullOrWhiteSpace(value))
        {
            var match = Regex.Match(value, @"^\s*(.*?) \(ignore_errors=(yes|no)\)(?:\s+|$)", RegexOptions.CultureInvariant);
            if (!match.Success) throw new InvalidDataException("Unrecognized EnvironmentFiles property.");
            files.Add(new(Decode(match.Groups[1].Value), match.Groups[2].Value == "yes"));
            value = value[match.Length..];
        }
        return files;
    }

    /// <summary>Checks the apphost format, architecture and executable mode, then rejects links in the stage.</summary>
    private void ValidateStage(string stage, string rid)
    {
        if (rid is not ("linux-x64" or "linux-arm64")) throw new PlatformNotSupportedException("Unsupported Linux RID.");
        InspectTree(stage);
        var host = Path.Combine(stage, AppHost);
        RequireRegularFile(host);
        using var stream = File.OpenRead(host);
        Span<byte> header = stackalloc byte[64];
        if (stream.Read(header) != header.Length || !header[..4].SequenceEqual(new byte[] { 127, 69, 76, 70 }) || header[4] != 2 || header[5] != 1 ||
            System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(header[18..]) != (rid == "linux-x64" ? 62 : 183))
            throw new InvalidDataException("Publish apphost is not an ELF executable for the selected architecture.");
        if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(host) & UnixFileMode.UserExecute) == 0)
            throw new InvalidDataException("Publish apphost is not executable.");
    }

    /// <summary>Rejects any stage reparse point before recursive traversal.</summary>
    private void InspectTree(string directory)
    {
        EnsureNoLinks(directory);
        foreach (var path in Directory.EnumerateFileSystemEntries(directory))
        {
            EnsureNoLinks(path);
            if (Directory.Exists(path)) InspectTree(path);
            else RequireRegularFile(path);
        }
    }

    /// <summary>Rejects pipes, devices and sockets before any unbounded filesystem read can occur.</summary>
    private void RequireRegularFile(string path)
    {
        EnsureNoLinks(path);
        var value = Command("stat", "-c", "%f", "--", path).Trim();
        if (!uint.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var mode) ||
            (mode & 0xf000) != 0x8000)
            throw new InvalidDataException("Deployment input must be an ordinary file.");
    }

    /// <summary>Normalizes absolute non-root paths and validates every existing ancestor.</summary>
    private static string SafePath(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new InvalidDataException("Deployment paths must be absolute.");
        path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (path == Path.GetPathRoot(path)) throw new InvalidDataException("A filesystem root is not a safe deployment path.");
        EnsureNoLinks(path);
        return path;
    }

    /// <summary>Rejects symlink roots and ancestors including dangling links.</summary>
    private static void EnsureNoLinks(string path)
    {
        for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Deployment roots and ancestors must not be symbolic links.");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }

    /// <summary>Ordinal containment including the path itself.</summary>
    private static bool Within(string path, string root) => path == root || path.StartsWith(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    /// <summary>Rejects overlap in either direction.</summary>
    private static void RejectOverlap(string first, string second)
    {
        if (Within(first, second) || Within(second, first)) throw new InvalidDataException("Deployment and preservation paths overlap unsafely.");
    }

    /// <summary>Requires a regular existing systemd configuration path.</summary>
    private string RequiredUnitFile(string value)
    {
        var path = SafePath(Decode(value));
        if (!File.Exists(path)) throw new FileNotFoundException("Required systemd configuration file is missing.");
        RequireRegularFile(path);
        return path;
    }

    /// <summary>Creates private recovery/lock directories without permitting symlink traversal.</summary>
    private void MakePrivateDirectory(string path)
    {
        EnsureNoLinks(path);
        if (Directory.Exists(path) && Command("stat", "-c", "%u", "--", path).Trim() != "0")
            throw new InvalidDataException("Private deployment directories must be owned by root.");
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(path);
        else { Directory.CreateDirectory(path, PrivateDirectory); File.SetUnixFileMode(path, PrivateDirectory); }
        if (Command("stat", "-c", "%u", "--", path).Trim() != "0")
            throw new InvalidDataException("Private deployment directories must be owned by root.");
    }

    /// <summary>Protects archive and lock files on Unix.</summary>
    private static void SetPrivateFile(string path)
    {
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, PrivateFile);
    }

    /// <summary>Stops exactly one unit and verifies that systemd reports it inactive.</summary>
    private void Stop(string service)
    {
        Command("systemctl", "stop", service);
        var state = run("systemctl", ["is-active", service]);
        if (state.StandardOutput.Trim() is not ("inactive" or "failed"))
            throw new InvalidOperationException("Selected service did not stop.");
    }

    /// <summary>Hashes archive bytes without loading live data into memory.</summary>
    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    /// <summary>Verifies archive identity and safe destinations before restoring live state and metadata.</summary>
    private void Restore(string archive, string hash, IEnumerable<string> preserved, string install)
    {
        EnsureNoLinks(archive);
        if (Hash(archive) != hash) throw new InvalidDataException("Preservation archive checksum changed.");
        foreach (var path in preserved) EnsureNoLinks(path);
        Command("tar", "--extract", "--file", archive, "--directory", Path.GetPathRoot(install)!,
            "--acls", "--xattrs", "--numeric-owner", "--same-owner", "--same-permissions");
    }

    /// <summary>Removes stale app files without following links; protected live roots are left in place.</summary>
    private static void ReplaceFiles(string stage, string install, IReadOnlyList<string> preserved)
    {
        bool Protected(string path) => preserved.Any(root => Within(path, root));
        void Clean(string directory)
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                if (Protected(path)) continue;
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) { File.Delete(path); continue; }
                if (Directory.Exists(path))
                {
                    Clean(path);
                    if (!Directory.EnumerateFileSystemEntries(path).Any()) Directory.Delete(path);
                }
                else File.Delete(path);
            }
        }
        void Copy(string source, string destination)
        {
            EnsureNoLinks(source); EnsureNoLinks(destination);
            Directory.CreateDirectory(destination);
            foreach (var path in Directory.EnumerateFileSystemEntries(source))
            {
                var target = Path.Combine(destination, Path.GetFileName(path));
                if (Protected(target)) continue;
                EnsureNoLinks(path); EnsureNoLinks(target);
                if (Directory.Exists(path)) Copy(path, target);
                else
                {
                    File.Copy(path, target, true);
                    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(target, File.GetUnixFileMode(path));
                }
            }
        }
        EnsureNoLinks(install); Clean(install); Copy(stage, install);
    }

    /// <summary>Executes a native operation, retaining sensitive command output only in local memory.</summary>
    private string Command(string command, params string[] arguments)
    {
        var result = run(command, arguments);
        if (result.ExitCode != 0) throw new InvalidOperationException($"Deployment command {command} failed with exit code {result.ExitCode}.");
        return result.StandardOutput;
    }
}
