using System.Runtime.Versioning;
using static NukeBuild.Tests.ServiceUpdateTestSupport;
using Fixture = NukeBuild.Tests.ServiceUpdateFixture;

namespace NukeBuild.IntegrationTests;

/// <summary>TEST-MCP-SERVICEUPDATE-001: temporary fixtures exercise deployment without a real service.</summary>
/// <remarks>
/// Requirement coverage: TR-MCP-NUKE-015 AC-004 (the service-update evidence): these six Platform=Linux methods (ten
/// cases) need a real Linux host (GNU tar, Unix file modes, symbolic links) and run in the Linux integration lane
/// (TR-MCP-NUKE-019 AC-2). AC-005 and TR-MCP-SERVICEUPDATE-001 per-os-tests, the same criterion before its move to
/// TR-MCP-NUKE-015 (every infrastructure case runs and passes on Linux and on Windows with no skip), are proved by the
/// infrastructure half of this class in Build.Tests.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-NUKE-015 AC-004 and AC-005, TR-MCP-SERVICEUPDATE-001 per-os-tests: cites the criteria it proves, so the criterion text no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TR-MCP-NUKE-015 AC-004, AC-005; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of ServiceUpdateTests, split from its infrastructure cases in Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"). The six Platform=Linux methods (ten cases) moved unchanged; the helpers and the fixture both halves use are the linked ServiceUpdateTestSupport and ServiceUpdateFixture, reached through a using-static directive and a Fixture alias, not copies; only the namespace changed.</description></item>
/// <item><term>2026-11-07</term><description>TR-MCP-NUKE-015 AC-004, AC-005; TR-MCP-SERVICEUPDATE-001 per-os-tests: the coverage remark now says that these Platform=Linux cases run in the Linux integration lane and that the per-OS criterion is proved by the infrastructure half, instead of the copied text that described every case as run directly as infrastructure on both hosts; documentation only (ClaudeCode review of slice S4).</description></item>
/// </list>
/// </remarks>
public sealed class ServiceUpdateTests
{
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
}
