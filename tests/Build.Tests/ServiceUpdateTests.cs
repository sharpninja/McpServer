using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;

namespace NukeBuild.Tests;

/// <summary>TEST-MCP-SERVICEUPDATE-001: temporary fixtures exercise deployment without a real service.</summary>
public sealed class ServiceUpdateTests
{
    /// <summary>Reference doubles establish the required preservation order before production exists.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MockContract_RestoresBeforeStartAndRetainsBackupOnCopyFailure(bool failCopy)
    {
        var calls = new List<string>();
        Action<string> effect = name => { calls.Add(name); if (failCopy && name == "copy") throw new IOException(); };
        effect("stop");
        effect("archive");
        try { effect("copy"); effect("restore"); effect("start"); effect("health"); }
        catch (IOException) { effect("stop"); effect("restore"); }
        Assert.Equal(failCopy ? ["stop", "archive", "copy", "stop", "restore"] :
            new[] { "stop", "archive", "copy", "restore", "start", "health" }, calls);
    }
    /// <summary>Reference double proves baseline seeding and preserved-live precedence before production binding.</summary>
    [Fact]
    public void MockContract_WindowsConfigurationRequiresBaselineBeforeStopAndPreservesLive()
    {
        var root = Path.Combine(Path.GetTempPath(), "windows-service-update-mock-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source");
        var stage = Path.Combine(root, "stage");
        var install = Path.Combine(root, "install");
        var backup = Path.Combine(root, "backup");
        var baseline = Path.Combine(source, "appsettings.yaml");
        var stageConfig = Path.Combine(stage, "appsettings.yaml");
        var liveConfig = Path.Combine(install, "appsettings.yaml");
        var backupConfig = Path.Combine(backup, "appsettings.yaml");
        var events = new List<string>();

        try
        {
            foreach (var path in new[] { source, stage, install, backup })
                Directory.CreateDirectory(path);

            void ReferenceDeploy()
            {
                events.Add("validate");
                if (!File.Exists(baseline))
                    throw new FileNotFoundException("baseline configuration is required", baseline);

                File.Copy(baseline, stageConfig, true);
                events.Add("stage");
                events.Add("stop");

                if (File.Exists(liveConfig))
                {
                    File.Copy(liveConfig, backupConfig, true);
                    events.Add("backup");
                }

                File.Copy(stageConfig, liveConfig, true);
                events.Add("copy");

                if (File.Exists(backupConfig))
                {
                    File.Copy(backupConfig, liveConfig, true);
                    events.Add("restore");
                }
            }

            var missing = Assert.Throws<FileNotFoundException>(ReferenceDeploy);
            Assert.Equal(baseline, missing.FileName);
            Assert.Equal(["validate"], events);

            File.WriteAllText(baseline, "mode: baseline");
            events.Clear();
            ReferenceDeploy();
            Assert.Equal("mode: baseline", File.ReadAllText(liveConfig));
            Assert.Equal(["validate", "stage", "stop", "copy"], events);

            File.WriteAllText(liveConfig, "mode: live");
            events.Clear();
            ReferenceDeploy();
            Assert.Equal("mode: live", File.ReadAllText(liveConfig));
            Assert.Equal(["validate", "stage", "stop", "backup", "copy", "restore"], events);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }


    /// <summary>Production helper seeds a missing stage config and preserves an explicit published config.</summary>
    [Fact]
    public void WindowsConfiguration_StagesBaselineAndPreservesExplicitPublishConfig()
    {
        var root = Path.Combine(Path.GetTempPath(), "windows-service-update-production-" + Guid.NewGuid().ToString("N"));
        var stage = Path.Combine(root, "stage");
        var baseline = Path.Combine(root, "source", "appsettings.yaml");

        try
        {
            Directory.CreateDirectory(stage);
            Directory.CreateDirectory(Path.GetDirectoryName(baseline)!);
            File.WriteAllText(baseline, "mode: baseline");

            var stagedConfig = WindowsServiceHelper.EnsureBaselineConfiguration(stage, baseline);
            Assert.Equal("mode: baseline", File.ReadAllText(stagedConfig));

            File.WriteAllText(stagedConfig, "mode: explicit");
            Assert.Equal(stagedConfig, WindowsServiceHelper.EnsureBaselineConfiguration(stage, baseline));
            Assert.Equal("mode: explicit", File.ReadAllText(stagedConfig));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    /// <summary>Production helper rejects an incomplete stage before service mutation can begin.</summary>
    [Fact]
    public void WindowsConfiguration_MissingBaselineFailsWithSourcePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "windows-service-update-missing-" + Guid.NewGuid().ToString("N"));
        var stage = Path.Combine(root, "stage");
        var baseline = Path.Combine(root, "source", "appsettings.yaml");

        try
        {
            Directory.CreateDirectory(stage);
            var error = Assert.Throws<FileNotFoundException>(
                () => WindowsServiceHelper.EnsureBaselineConfiguration(stage, baseline));
            Assert.Equal(baseline, error.FileName);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }


    /// <summary>Reference double rejects a split legacy-service/canonical-directory identity before production binding.</summary>
    [Fact]
    public void MockContract_WindowsIdentityKeepsServiceAndInstallPaired()
    {
        static (string Service, string Install) Resolve(
            bool canonicalServiceExists,
            bool legacyServiceExists,
            bool canonicalInstallExists,
            bool legacyInstallExists)
        {
            if (canonicalServiceExists)
                return ("QBrainAi", @"C:\ProgramData\QBrainAi");
            if (legacyServiceExists)
                return ("McpServer", @"C:\ProgramData\McpServer");
            if (!canonicalInstallExists && legacyInstallExists)
                return ("QBrainAi", @"C:\ProgramData\McpServer");
            return ("QBrainAi", @"C:\ProgramData\QBrainAi");
        }

        var actual = Resolve(false, true, true, true);
        Assert.Equal(("McpServer", @"C:\ProgramData\McpServer"), actual);
        Assert.NotEqual(("McpServer", @"C:\ProgramData\QBrainAi"), actual);
        Assert.Equal(("QBrainAi", @"C:\ProgramData\QBrainAi"), Resolve(true, true, true, true));
    }



    /// <summary>FR-MCP-SERVICEUPDATE-001: host fixtures preserve Windows defaults and select Linux architecture.</summary>
    [Theory]
    [InlineData(true, false, Architecture.X64, "QBrainAi", "win-x64", "QBrainAi.Support.Mcp.exe")]
    [InlineData(false, true, Architecture.X64, "mcpserver.service", "linux-x64", "QBrainAi.Support.Mcp")]
    [InlineData(false, true, Architecture.Arm64, "mcpserver.service", "linux-arm64", "QBrainAi.Support.Mcp")]
    public void PlatformSelection_PreservesWindowsDefaultsAndSelectsLinuxRid(bool windows, bool linux,
        Architecture architecture, string service, string rid, string executable)
    {
        var value = Call("ServiceUpdatePlatform", "Resolve", null, windows, linux, architecture);
        Assert.Equal(service, Property(value, "DefaultServiceName"));
        Assert.Equal(rid, Property(value, "RuntimeIdentifier"));
        Assert.Equal(executable, Property(value, "ExecutableName"));
        Assert.Equal(windows ? @"C:\ProgramData\QBrainAi" : "/opt/mcpserver/app", Property(value, "DefaultInstallPath"));
    }

    /// <summary>Unsupported hosts fail before any native effect (TR-MCP-SERVICEUPDATE-001).</summary>
    [Theory]
    [InlineData(false, false, Architecture.X64)]
    [InlineData(false, true, Architecture.X86)]
    [InlineData(true, false, Architecture.X86)]
    [InlineData(true, false, Architecture.Arm)]
    public void PlatformSelection_RejectsUnsupportedHosts(bool windows, bool linux, Architecture architecture) =>
        Assert.Throws<PlatformNotSupportedException>(() => Call("ServiceUpdatePlatform", "Resolve", null, windows, linux, architecture));

    /// <summary>Unrelated Nuke targets can obtain defaults without a supported deployment host.</summary>
    [Theory]
    [InlineData(false, "QBrainAi")]
    [InlineData(true, "mcpserver.service")]
    public void ParameterDefaults_DoNotRequireSupportedDeploymentHost(bool linux, string service)
    {
        Assert.Equal(service, Property(Call("ServiceUpdatePlatform", "ParameterDefaults", null, linux), "DefaultServiceName"));
    }

    /// <summary>TR-MCP-QBRAIN-006: default Windows parameters update an existing McpServer install in place.</summary>
    [Fact]
    public void WindowsInstall_UsesLegacyServiceWhenCanonicalRegistrationIsAbsent()
    {
        var identity = Call("ServiceUpdatePlatform", "ResolveExistingWindowsInstall", null,
            "QBrainAi", @"C:\ProgramData\QBrainAi", false, true, false, true);
        Assert.Equal("McpServer", Property(identity, "ServiceName"));
        Assert.Equal(@"C:\ProgramData\McpServer", Property(identity, "InstallPath"));
        Assert.Equal("QBrainAi.Support.Mcp.exe", Property(identity, "ExecutableName"));
    }

    /// <summary>TR-MCP-QBRAIN-006: stale canonical files cannot split a live legacy service from its install.</summary>
    [Fact]
    public void WindowsInstall_LegacyServiceKeepsLegacyInstallWhenCanonicalDirectoryIsStale()
    {
        var identity = Call("ServiceUpdatePlatform", "ResolveExistingWindowsInstall", null,
            "QBrainAi", @"C:\ProgramData\QBrainAi", false, true, true, true);
        Assert.Equal("McpServer", Property(identity, "ServiceName"));
        Assert.Equal(@"C:\ProgramData\McpServer", Property(identity, "InstallPath"));
    }


    /// <summary>TR-MCP-QBRAIN-006: an explicit service name is not redirected onto the legacy registration.</summary>
    [Fact]
    public void WindowsInstall_KeepsExplicitServiceName()
    {
        var identity = Call("ServiceUpdatePlatform", "ResolveExistingWindowsInstall", null,
            "Custom", @"D:\services\custom", false, true, false, true);
        Assert.Equal("Custom", Property(identity, "ServiceName"));
        Assert.Equal(@"D:\services\custom", Property(identity, "InstallPath"));
    }

    /// <summary>TR-MCP-QBRAIN-006: a preserved Linux unit that still starts McpServer.Support.Mcp is retargeted.</summary>
    [Fact]
    public void LinuxUpdate_LegacyExecStartIsRetargeted()
    {
        using var f = new Fixture();
        var legacy = Path.Combine(f.Install, "McpServer.Support.Mcp");
        f.UnitExecutable = legacy;
        var unitPath = Path.Combine(f.Root, "example.service");
        File.WriteAllText(unitPath, "ExecStart=" + legacy + "\n");
        f.Update();
        var text = File.ReadAllText(unitPath);
        Assert.Contains("QBrainAi.Support.Mcp", text, StringComparison.Ordinal);
        Assert.DoesNotContain("McpServer.Support.Mcp", text, StringComparison.Ordinal);
        Assert.Contains("daemon-reload", f.Events);
    }

    /// <summary>TR-MCP-QBRAIN-006: a legacy ExecStart that lives only in a systemd drop-in is still retargeted.</summary>
    [Fact]
    public void LinuxUpdate_LegacyExecStartInDropInIsRetargeted()
    {
        using var f = new Fixture();
        var legacy = Path.Combine(f.Install, "McpServer.Support.Mcp");
        f.UnitExecutable = legacy;
        var unitPath = Path.Combine(f.Root, "example.service");
        File.WriteAllText(unitPath, "[Service]\nExecStart=/usr/bin/true\n");
        var dropIn = Path.Combine(f.Root, "override.conf");
        File.WriteAllText(dropIn, "[Service]\nExecStart=" + legacy + "\n");
        f.DropIns = dropIn;
        f.Update();
        var dropInText = File.ReadAllText(dropIn);
        Assert.Contains("QBrainAi.Support.Mcp", dropInText, StringComparison.Ordinal);
        Assert.DoesNotContain("McpServer.Support.Mcp", dropInText, StringComparison.Ordinal);
        Assert.DoesNotContain("QBrainAi.Support.Mcp", File.ReadAllText(unitPath), StringComparison.Ordinal);
        Assert.Contains("daemon-reload", f.Events);
    }

    /// <summary>Quoted relative YAML and legacy fallback resolve to the actual preserved data root.</summary>
    [Theory]
    [InlineData("DataFolder: '../data'\nMcp:\n  DataDirectory: ignored\n")]
    [InlineData("Mcp:\n  DataDirectory: '../data'\n")]
    [InlineData("DataFolder: ''\nMcp:\n  DataDirectory: '../data'\n")]
    [InlineData("DataFolder: null\nMcp:\n  DataDirectory: '../data'\n")]
    [InlineData("DataFolder: '  '\nMcp:\n  DataDirectory: '../data'\n")]
    public void PreservationPaths_UsesConfiguredDataFolderAndLegacyFallback(string yaml)
    {
        using var f = new Fixture(false);
        File.WriteAllText(f.Config, yaml);
        var paths = (IEnumerable<string>)Call("WindowsServiceHelper", "GetPreservedStatePaths", null, f.Install);
        Assert.Contains(f.Config, paths);
        Assert.Contains(f.Data, paths);
    }

    /// <summary>Malformed configuration must never silently select the wrong data directory.</summary>
    [Fact]
    public void PreservationPaths_MalformedYamlFailsClosed()
    {
        using var f = new Fixture(false);
        Assert.NotNull(Contract("WindowsServiceHelper").GetMethod("GetPreservedStatePaths"));
        File.WriteAllText(f.Config, "DataFolder: [unterminated");
        Assert.ThrowsAny<Exception>(() => Call("WindowsServiceHelper", "GetPreservedStatePaths", null, f.Install));
    }

    /// <summary>Legacy install-root data retains database companions and runtime folders only.</summary>
    [Fact]
    public void PreservationPaths_DefaultRootPreservesLegacyFiles()
    {
        using var f = new Fixture(false);
        File.WriteAllText(f.Config, "DataFolder: .\n");
        File.WriteAllText(Path.Combine(f.Install, "state.db-wal"), "wal");
        Directory.CreateDirectory(Path.Combine(f.Install, "templates"));
        var paths = ((IEnumerable<string>)Call("WindowsServiceHelper", "GetPreservedStatePaths", null, f.Install)).ToArray();
        Assert.Contains(Path.Combine(f.Install, "state.db-wal"), paths);
        Assert.Contains(Path.Combine(f.Install, "templates"), paths);
        Assert.DoesNotContain(f.Executable, paths);
    }

    /// <summary>Missing unit, wrong executable, overlays and link roots cannot reach stop (TR-MCP-SERVICEUPDATE-001).</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("executable")]
    [InlineData("overlay")]
    [InlineData("environment")]
    [InlineData("overlap")]
    [InlineData("working-directory")]
    [InlineData("invalid-elf")]
    [InlineData("wrong-architecture")]
    public void LinuxPreflight_RejectsUnsafeConfigurationWithoutStopping(string defect)
    {
        using var f = new Fixture();
        if (defect == "missing") f.LoadState = "not-found";
        if (defect == "executable") f.UnitExecutable = "/unexpected/server";
        if (defect == "overlay") File.WriteAllText(Path.Combine(f.Install, "appsettings.Production.yaml"), "DataFolder: /wrong");
        if (defect == "environment") File.WriteAllText(f.Env, "DataFolder=/wrong\n");
        if (defect == "symlink") { Directory.Delete(f.Data, true); Directory.CreateSymbolicLink(f.Data, f.Stage); }
        if (defect == "overlap") f.Backup = Path.Combine(f.Data, "backups");
        if (defect == "working-directory") f.WorkingDirectory = f.Data;
        if (defect == "invalid-elf") File.WriteAllText(Path.Combine(f.Stage, "QBrainAi.Support.Mcp"), "not-ELF");
        if (defect == "wrong-architecture") { var bytes = File.ReadAllBytes(Path.Combine(f.Stage, "QBrainAi.Support.Mcp")); bytes[18] = 183; File.WriteAllBytes(Path.Combine(f.Stage, "QBrainAi.Support.Mcp"), bytes); }
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.DoesNotContain("stop", f.Events);
        Assert.Equal("old", File.ReadAllText(f.Executable));
    }

    /// <summary>TEST-MCP-SERVICEUPDATE-001: systemd simple units may return before exec; transient process observations must settle before health verification.</summary>
    [Theory]
    [InlineData("/usr/lib/systemd/systemd")]
    [InlineData(null)]
    public void LinuxUpdate_WaitsForExpectedProcessAfterSystemdStart(string? firstExecutable)
    {
        using var f = new Fixture();
        f.ProcessObservations.Enqueue(firstExecutable);
        f.Update();
        Assert.Equal(2, f.ProcessChecks);
        Assert.Contains("health", f.Events);
        Assert.Equal(1, f.Events.Count(value => value == "stop"));
    }

    /// <summary>TEST-MCP-SERVICEUPDATE-001: a persistent wrong executable must fail within a bounded wait, restore live state and never pass health.</summary>
    [Fact]
    public void LinuxUpdate_PersistentWrongProcessFailsAndRestores()
    {
        using var f = new Fixture();
        f.WrongProcess = true;
        Assert.Throws<InvalidOperationException>(() => f.Update());
        Assert.InRange(f.ProcessChecks, 2, 100);
        Assert.DoesNotContain("health", f.Events);
        Assert.Equal(2, f.Events.Count(value => value == "stop"));
        Assert.Equal(f.OriginalConfig, File.ReadAllText(f.Config));
        Assert.Equal("original", File.ReadAllText(Path.Combine(f.Data, "state")));
    }
    /// <summary>Real tar with fake systemd validates archive/copy/restore/start ordering and live bytes.</summary>
    [Fact]
    public void LinuxUpdate_ArchivesBeforeReplaceRestoresBeforeStartAndChecksHealth()
    {
        using var f = new Fixture();
        File.WriteAllText(Path.Combine(f.Install, "stale.dll"), "stale");
        var result = f.Update();
        var phases = new[] { "stop", "archive", "defaults", "restore", "start", "health" };
        Assert.All(phases, phase => Assert.Contains(phase, f.Events));
        Assert.Equal(phases, f.Events.Where(phases.Contains));
        Assert.Equal(File.ReadAllBytes(Path.Combine(f.Stage, "QBrainAi.Support.Mcp")), File.ReadAllBytes(f.Executable));
        Assert.False(File.Exists(Path.Combine(f.Install, "stale.dll")));
        Assert.Equal("original", File.ReadAllText(Path.Combine(f.Data, "state")));
        Assert.Equal(f.OriginalConfig, File.ReadAllText(f.Config));
        Assert.True(File.Exists((string)Property(result, "ArchivePath")));
        Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes((string)Property(result, "ArchivePath")))), Property(result, "ArchiveSha256"));
    }

    /// <summary>Failed archive cannot replace files or start service.</summary>
    [Fact]
    public void LinuxUpdate_BackupFailureNeverCopiesOrStarts()
    {
        using var f = new Fixture { FailArchive = true };
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.DoesNotContain("defaults", f.Events);
        Assert.DoesNotContain("start", f.Events);
        Assert.Equal("old", File.ReadAllText(f.Executable));
    }

    /// <summary>Copy and health failures restore live config and retain evidence without claiming binary rollback.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LinuxUpdate_FailureRestoresAndRetainsArchive(bool failCopy)
    {
        using var f = new Fixture { FailCopy = failCopy, FailHealth = !failCopy };
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.Equal(f.OriginalConfig, File.ReadAllText(f.Config));
        Assert.Equal("original", File.ReadAllText(Path.Combine(f.Data, "state")));
        Assert.Equal("restore", f.Events.Last());
        Assert.Equal("stop", f.Events[^2]);
        Assert.NotEmpty(Directory.GetFiles(f.Backup, "*.tar", SearchOption.AllDirectories));
    }

    /// <summary>GNU tar preserves modes and symlink targets; private archive protects live secrets.</summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "Linux")]
    [SupportedOSPlatform("linux")]
    public void LinuxArchive_UsesPrivateMetadataPreservingCommands()
    {
        using var f = new Fixture { RealTar = true };
        File.SetUnixFileMode(f.Env, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.CreateSymbolicLink(Path.Combine(f.Data, "link"), "state");
        var dataFile = Path.Combine(f.Data, "state");
        Native("setfacl", "-m", "u:65534:r", dataFile);
        Native("setfattr", "-n", "user.mcp-test", "-v", "original-attribute", dataFile);
        var acl = Native("getfacl", "--omit-header", "--numeric", dataFile);
        var owner = Native("stat", "-c", "%u:%g", dataFile);
        f.OnCopy = () =>
        {
            Native("setfacl", "-b", dataFile);
            Native("setfattr", "-x", "user.mcp-test", dataFile);
            File.SetUnixFileMode(f.Env, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead);
            File.Delete(Path.Combine(f.Data, "link"));
            File.WriteAllText(Path.Combine(f.Data, "link"), "replacement");
            File.WriteAllText(Path.Combine(f.Data, "state"), "changed");
        };
        var result = f.Update();
        var archive = (string)Property(result, "ArchivePath");
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(archive));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(Path.GetDirectoryName(archive)!));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(f.Env));
        Assert.Equal("state", new FileInfo(Path.Combine(f.Data, "link")).LinkTarget);
        Assert.Equal("original", File.ReadAllText(Path.Combine(f.Data, "state")));
        Assert.Equal(acl, Native("getfacl", "--omit-header", "--numeric", dataFile));
        Assert.Equal(owner, Native("stat", "-c", "%u:%g", dataFile));
        Assert.Equal("original-attribute", Native("getfattr", "--only-values", "-n", "user.mcp-test", dataFile).TrimEnd());
        Assert.Contains("--acls", f.TarArguments);
        Assert.Contains("--xattrs", f.TarArguments);
        Assert.DoesNotContain("--dereference", f.TarArguments);
    }

    /// <summary>A stale directory link is removed without modifying its external target.</summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "Linux")]
    public void LinuxReplacement_DoesNotFollowSymlinks()
    {
        using var f = new Fixture();
        Directory.CreateSymbolicLink(Path.Combine(f.Install, "stale"), f.Data);
        f.Update();
        Assert.False(Directory.Exists(Path.Combine(f.Install, "stale")));
        Assert.Equal("original", File.ReadAllText(Path.Combine(f.Data, "state")));
    }

    /// <summary>Linux rejects linked data roots and nonexecutable stages before stopping the selected service.</summary>
    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Platform", "Linux")]
    [SupportedOSPlatform("linux")]
    [InlineData(true)]
    [InlineData(false)]
    public void LinuxPreflight_RejectsSymlinkRootAndNonexecutableStage(bool symlink)
    {
        using var f = new Fixture();
        if (symlink) { Directory.Delete(f.Data, true); Directory.CreateSymbolicLink(f.Data, f.Stage); }
        else File.SetUnixFileMode(Path.Combine(f.Stage, "QBrainAi.Support.Mcp"), UnixFileMode.UserRead);
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.DoesNotContain("stop", f.Events);
    }

    /// <summary>Concurrent invocation cannot stop or copy while the service lock is held.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LinuxUpdate_ConcurrentInvocationFailsWithoutMutation(bool alias)
    {
        using var f = new Fixture();
        f.OnCopy = () =>
        {
            f.OnCopy = null;
            if (alias) f.ServiceName = "alias.service";
            var before = f.Events.Count;
            Assert.ThrowsAny<Exception>(() => f.Update());
            Assert.DoesNotContain("stop", f.Events.Skip(before));
        };
        f.Update();
    }

    /// <summary>Zero/partial workspace validation is fatal on Linux; Windows helper compatibility remains.</summary>
    [Theory]
    [InlineData(0, 0, 0, false)]
    [InlineData(2, 1, 1, false)]
    [InlineData(2, 1, 0, true)]
    [InlineData(2, 2, 0, true)]
    public void WorkspaceHealth_UnavailableOrEmptyNeverPasses(int count, int healthy, int failed, bool valid)
    {
        Assert.NotNull(Contract("WindowsServiceHelper").GetMethod("RequireHealthyWorkspaces"));
        Action check = () => Call("WindowsServiceHelper", "RequireHealthyWorkspaces", null,
            new WindowsServiceHelper.WorkspaceHealthResult(count, healthy, failed));
        if (valid) check(); else Assert.ThrowsAny<Exception>(check);
    }

    /// <summary>The deployment manifest hashes extensionless Linux apphost rather than only Windows executables.</summary>
    [Fact]
    public void DeploymentManifest_HashesExtensionlessApphost()
    {
        using var f = new Fixture(false);
        var path = WindowsServiceHelper.WriteDeploymentManifest(f.Install, "example.service", "QBrainAi.Support.Mcp", 7147, "update");
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Contains(json.RootElement.GetProperty("executableHashes").EnumerateArray(), x =>
            x.GetProperty("name").GetString() == "QBrainAi.Support.Mcp" &&
            x.GetProperty("sha256").GetString()!.Equals(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f.Executable))), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Systemd escaped and quoted paths retain spaces, optional environment files retain their policy.</summary>
    [Fact]
    public void LinuxUnitState_ParsesDropInsAndOptionalEnvironmentFiles()
    {
        var words = (IEnumerable<string>)Call("LinuxServiceHelper", "ParseWords", null, "/etc/unit\\x20one.conf \"/etc/unit two.conf\"");
        Assert.Equal(new[] { "/etc/unit one.conf", "/etc/unit two.conf" }, words);
        var files = ((System.Collections.IEnumerable)Call("LinuxServiceHelper", "ParseEnvironmentFiles", null,
            "/etc/secret\\x20one (ignore_errors=no) /etc/optional (ignore_errors=yes)")).Cast<object>().ToArray();
        Assert.Equal("/etc/secret one", Property(files[0], "Path"));
        Assert.Equal(false, Property(files[0], "Optional"));
        Assert.Equal(true, Property(files[1], "Optional"));
    }

    /// <summary>Missing required environment fails before stop; optional missing files remain absent.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LinuxPreflight_RespectsOptionalEnvironmentFiles(bool optional)
    {
        using var f = new Fixture();
        File.Delete(f.Env);
        f.OptionalEnvironment = optional;
        if (optional) { f.Update(); Assert.False(File.Exists(f.Env)); }
        else { Assert.ThrowsAny<Exception>(() => f.Update()); Assert.DoesNotContain("stop", f.Events); }
    }

    /// <summary>Private lock and archive directories must belong to the privileged updater, not another account.</summary>
    [Fact]
    public void LinuxPreflight_RejectsUntrustedPrivateDirectoryOwner()
    {
        using var f = new Fixture { PrivateOwner = "1000" };
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.DoesNotContain("stop", f.Events);
    }

    /// <summary>Namespace remapping cannot silently change which host paths contain live service state.</summary>
    [Theory]
    [InlineData("RootDirectory")]
    [InlineData("RootImage")]
    [InlineData("BindPaths")]
    [InlineData("BindReadOnlyPaths")]
    [InlineData("TemporaryFileSystem")]
    [InlineData("PrivateTmp")]
    [InlineData("MountImages")]
    [InlineData("ExtensionImages")]
    [InlineData("ExtensionDirectories")]
    public void LinuxPreflight_RejectsRemappedServiceFilesystem(string property)
    {
        using var f = new Fixture { ExtraProperties = property + "=/different/root\n" };
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.DoesNotContain("stop", f.Events);
    }

    /// <summary>CommandLine configuration supports slash-prefixed overrides as well as double-dash options.</summary>
    [Theory]
    [InlineData("/DataFolder=/unbacked")]
    [InlineData("/Mcp:DataDirectory=/unbacked")]
    [InlineData("--DataFolder=/unbacked")]
    [InlineData("--instance another")]
    [InlineData("--contentRoot /unbacked")]
    public void LinuxPreflight_RejectsCommandLineDataOverrides(string arguments)
    {
        using var f = new Fixture { Arguments = arguments };
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.DoesNotContain("stop", f.Events);
    }

    /// <summary>A preserved runtime-config directory link must not redirect the generated-default callback.</summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "Linux")]
    public void LinuxReplacement_RejectsLinkedRuntimeConfigurationDestination()
    {
        using var f = new Fixture { RealTar = true };
        var configData = Path.Combine(f.Install, "config");
        Directory.CreateDirectory(configData);
        File.WriteAllText(f.Config, "DataFolder: '" + configData + "'\n");
        Directory.CreateSymbolicLink(Path.Combine(configData, "brain-slots"), f.Data);
        var called = false;
        f.OnCopy = () => called = true;
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.False(called);
    }

    /// <summary>Special stage files are rejected before an apphost read or any service mutation can block on a FIFO.</summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "Linux")]
    public void LinuxPreflight_RejectsSpecialStageFiles()
    {
        using var f = new Fixture { InspectRealFileKinds = true };
        Native("mkfifo", Path.Combine(f.Stage, "pipe"));
        Assert.ThrowsAny<Exception>(() => Call("LinuxServiceHelper", "ValidateStage", f.CreateHelper(), f.Stage, "linux-x64"));
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.DoesNotContain("stop", f.Events);
    }

    /// <summary>Apphost and live text inputs reject FIFOs through the regular-file guard before opening them.</summary>
    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Platform", "Linux")]
    [InlineData("apphost")]
    [InlineData("configuration")]
    [InlineData("environment")]
    [InlineData("unit")]
    public void LinuxPreflight_RejectsSpecialApphostAndTextInputs(string input)
    {
        using var f = new Fixture { InspectRealFileKinds = true };
        var path = input switch
        {
            "apphost" => Path.Combine(f.Stage, "QBrainAi.Support.Mcp"),
            "configuration" => f.Config,
            "environment" => f.Env,
            _ => Path.Combine(f.Root, "example.service")
        };
        File.Delete(path);
        Native("mkfifo", path);
        Assert.ThrowsAny<Exception>(() => Call("LinuxServiceHelper", "RequireRegularFile", f.CreateHelper(), path));
        Assert.ThrowsAny<Exception>(() => f.Update());
        Assert.DoesNotContain("stop", f.Events);
    }

    /// <summary>Real loaded drop-in bytes are preserved through the same archive as configuration.</summary>
    [Fact]
    public void LinuxUnitState_PreservesDropInWithSpaces()
    {
        using var f = new Fixture();
        var dropIn = Path.Combine(f.Root, "custom config.conf");
        File.WriteAllText(dropIn, "original-unit");
        f.DropIns = dropIn.Replace(" ", "\\x20");
        f.OnCopy = () => File.WriteAllText(dropIn, "overwritten");
        f.Update();
        Assert.Equal("original-unit", File.ReadAllText(dropIn));
    }

    /// <summary>Runs installed metadata utilities against an isolated fixture, failing on timeout or nonzero exit.</summary>
    private static string Native(string command, params string[] arguments)
    {
        var info = new ProcessStartInfo(command) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        Assert.True(process.WaitForExit(30000));
        Task.WaitAll(stdout, stderr);
        Assert.Equal(0, process.ExitCode);
        return stdout.Result;
    }

    /// <summary>Reflection keeps absent production contracts compilable for the red gate.</summary>
    private static Type Contract(string name) => typeof(WindowsServiceHelper).Assembly.GetType(name) ?? throw new InvalidOperationException("Missing production contract: " + name);
    /// <summary>Reads a returned contract record.</summary>
    private static object Property(object target, string name) => target.GetType().GetProperty(name)!.GetValue(target)!;
    /// <summary>Invokes the intended API while preserving its original exception for assertions.</summary>
    private static object Call(string type, string method, object? instance, params object[] args)
    {
        try { return (Contract(type).GetMethod(method, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing production method: " + method)).Invoke(instance, args)!; }
        catch (TargetInvocationException ex) { ExceptionDispatchInfo.Capture(ex.InnerException!).Throw(); throw; }
    }

    /// <summary>Isolated live-state fixture and command double; tar executes against temporary files only.</summary>
    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "service-update-" + Guid.NewGuid().ToString("N"));
        public string Install => Path.Combine(Root, "app");
        public string Stage => Path.Combine(Root, "stage");
        public string Data => Path.Combine(Root, "data");
        public string Config => Path.Combine(Install, "appsettings.yaml");
        public string Executable => Path.Combine(Install, "QBrainAi.Support.Mcp");
        public string Env => Path.Combine(Root, "service.env");
        public string Backup { get; set; }
        public string UnitExecutable { get; set; }
        public string WorkingDirectory { get; set; }
        public string LoadState { get; set; } = "loaded";
        public bool FailArchive { get; set; }
        public bool FailCopy { get; set; }
        public bool FailHealth { get; set; }
        public Queue<string?> ProcessObservations { get; } = new();
        public int ProcessChecks { get; private set; }
        public bool WrongProcess { get; set; }
        public bool RealTar { get; set; }
        public bool InspectRealFileKinds { get; set; }
        public bool OptionalEnvironment { get; set; }
        public string DropIns { get; set; } = "";
        public string PrivateOwner { get; set; } = "0";
        public string ServiceName { get; set; } = "example.service";
        public string ExtraProperties { get; set; } = "";
        public string Arguments { get; set; } = "--urls http://localhost:7147";
        public Action? OnCopy { get; set; }
        public string OriginalConfig => "DataFolder: '" + Data + "'\n";
        public List<string> Events { get; } = [];
        public List<string> TarArguments { get; } = [];
        private bool started;
        private readonly Dictionary<string, byte[]> snapshot = [];

        /// <summary>Builds ordinary config/data plus an ELF header matching the test RID.</summary>
        public Fixture(bool requireLinux = true)
        {
            // Fail the red gate at contract binding, never mistake a missing API for safe rejection.
            if (requireLinux) _ = Contract("LinuxServiceHelper");
            foreach (var path in new[] { Install, Stage, Data }) Directory.CreateDirectory(path);
            Backup = Path.Combine(Root, "backups"); UnitExecutable = Executable;
            WorkingDirectory = Install;
            File.WriteAllText(Config, OriginalConfig); File.WriteAllText(Executable, "old");
            File.WriteAllText(Path.Combine(Data, "state"), "original");
            File.WriteAllText(Env, "ConnectionStrings__Default=private\n");
            File.WriteAllText(Path.Combine(Root, "example.service"), "[Service]\n");
            var elf = new byte[64]; elf[0] = 127; elf[1] = 69; elf[2] = 76; elf[3] = 70; elf[4] = 2; elf[5] = 1; elf[18] = 62;
            File.WriteAllBytes(Path.Combine(Stage, "QBrainAi.Support.Mcp"), elf);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(Path.Combine(Stage, "QBrainAi.Support.Mcp"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            File.WriteAllText(Path.Combine(Stage, "appsettings.yaml"), "DataFolder: wrong\n");
        }

        /// <summary>Adapts the native-command contract without requiring unimplemented types at compile time.</summary>
        public object Update()
        {
            var helper = CreateHelper();
            var options = Activator.CreateInstance(Contract("LinuxServiceUpdateOptions"), ServiceName, Install, Stage, Backup, "linux-x64", Path.Combine(Root, "locks"))!;
            return Call("LinuxServiceHelper", "Update", helper, options, (Action)(() =>
            {
                Events.Add("defaults"); OnCopy?.Invoke();
                if (FailCopy) { File.WriteAllText(Config, "changed"); File.WriteAllText(Path.Combine(Data, "state"), "changed"); throw new IOException("injected copy failure"); }
            }), (Action)(() => { Events.Add("health"); if (FailHealth) { File.WriteAllText(Config, "changed"); File.WriteAllText(Path.Combine(Data, "state"), "changed"); throw new IOException("injected health failure"); } }));
        }

        /// <summary>Binds the command double for whole-update and isolated preflight tests.</summary>
        public object CreateHelper()
        {
            var resultType = Contract("ServiceCommandResult");
            var command = Expression.Parameter(typeof(string)); var arguments = Expression.Parameter(typeof(IReadOnlyList<string>));
            var body = Expression.Convert(Expression.Call(Expression.Constant(this), GetType().GetMethod(nameof(Run))!, command, arguments), resultType);
            var runner = Expression.Lambda(typeof(Func<,,>).MakeGenericType(typeof(string), typeof(IReadOnlyList<string>), resultType), body, command, arguments).Compile();
            return Activator.CreateInstance(Contract("LinuxServiceHelper"), runner)!;
        }

        /// <summary>Fakes systemd/id/readlink; real tar provides independent preservation evidence.</summary>
        public object Run(string command, IReadOnlyList<string> args)
        {
            object Result(int code, string output) => Activator.CreateInstance(Contract("ServiceCommandResult"), code, output)!;
            if (command == "id") return Result(0, "0");
            if (command == "stat") return Result(0, args.Contains("%f")
                ? InspectRealFileKinds ? Native("stat", args.ToArray()) : Directory.Exists(args.Last()) ? "41ed" : "81a4"
                : PrivateOwner);
            if (command == "readlink")
            {
                ProcessChecks++;
                var observation = ProcessObservations.Count > 0 ? ProcessObservations.Dequeue() : WrongProcess ? "/unexpected/process" : Executable;
                return observation is null ? Result(1, "") : Result(0, observation);
            }
            if (command == "systemctl")
            {
                if (args[0] == "show") return Result(0, $"LoadState={LoadState}\nType=simple\nFragmentPath={Path.Combine(Root, "example.service")}\nDropInPaths={DropIns}\nEnvironmentFiles={Env} (ignore_errors={(OptionalEnvironment ? "yes" : "no")})\nEnvironment=\nWorkingDirectory={WorkingDirectory}\nExecStart={{ path={UnitExecutable} ; argv[]={UnitExecutable} {Arguments} ; ignore_errors=no ; }}\nMainPID={(started ? Environment.ProcessId : 0)}\nActiveState={(started ? "active" : "inactive")}\n{ExtraProperties}");
                if (args[0] == "is-active") return Result(started ? 0 : 3, started ? "active" : "inactive");
                Events.Add(args[0]); if (args[0] == "start") started = true; if (args[0] == "stop") started = false;
                return Result(0, "");
            }
            if (command != "tar") throw new InvalidOperationException("Unexpected command: " + command);
            TarArguments.AddRange(args);
            if (args.Contains("--create")) { Events.Add("archive"); if (FailArchive) return Result(2, "injected"); }
            if (args.Contains("--extract")) Events.Add("restore");
            if (!RealTar)
            {
                if (args.Contains("--create"))
                {
                    var array = args.ToArray();
                    foreach (var relative in array.Skip(Array.IndexOf(array, "--") + 1))
                    {
                        var path = Path.GetFullPath(relative, Path.GetPathRoot(Root)!);
                        foreach (var file in Directory.Exists(path) ? Directory.GetFiles(path, "*", SearchOption.AllDirectories) : new[] { path })
                            snapshot[file] = File.ReadAllBytes(file);
                    }
                    File.WriteAllText(array[Array.IndexOf(array, "--file") + 1], "archive-double");
                }
                if (args.Contains("--extract")) foreach (var (path, bytes) in snapshot)
                { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes); }
                return Result(0, args.Contains("--version") ? "tar (GNU tar) test-double" : "");
            }
            var info = new ProcessStartInfo(command) { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in args) info.ArgumentList.Add(arg);
            using var process = Process.Start(info)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            Assert.True(process.WaitForExit(30000));
            Task.WaitAll(stdout, stderr);
            return Result(process.ExitCode, stdout.Result);
        }

        /// <summary>Removes only this test's temporary tree.</summary>
        public void Dispose() => Directory.Delete(Root, true);
    }
}
