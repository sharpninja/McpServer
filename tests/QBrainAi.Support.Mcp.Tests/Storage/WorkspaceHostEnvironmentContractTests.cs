using NSubstitute;
using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001 / TR-MCP-FED-PATH-001: contract for the process
/// <see cref="IWorkspaceHostEnvironment"/>: it reports the running OS, the process working
/// directory, and canonical host-native normalization. BDP v4 (plan section 2a):
/// <see cref="WorkspaceHostEnvironmentMockDataTests"/> then <see cref="WorkspaceHostEnvironmentRealTests"/>.
/// Scripted Windows and Linux hosts run as unit cases using explicit expectations. Only the real host binding
/// reads the running platform and directory, so only that binding is Integration (TR-MCP-USECASE-018 AC-006).
/// </summary>
public abstract class WorkspaceHostEnvironmentContractTests
{
    /// <summary>Explicit expected platform for this adapter.</summary>
    protected abstract WorkspacePathPlatform RunningPlatform { get; }

    /// <summary>Explicit expected working directory; only the native binding reads the process.</summary>
    protected abstract string ExpectedCurrentDirectory { get; }

    /// <summary>A host-native path with a dot segment for this runner.</summary>
    protected abstract string NativeSample { get; }

    /// <summary>Expected canonical form of <see cref="NativeSample"/>.</summary>
    protected abstract string NativeSampleExpected { get; }

    /// <summary>Creates the host environment under test.</summary>
    protected abstract IWorkspaceHostEnvironment CreateHost();

    /// <summary>The host reports the platform of the running process.</summary>
    [Fact]
    public void Platform_IsRunningPlatform()
    {
        Assert.Equal(RunningPlatform, CreateHost().Platform);
    }

    /// <summary>The host reports the process working directory.</summary>
    [Fact]
    public void CurrentDirectory_IsProcessWorkingDirectory()
    {
        Assert.Equal(ExpectedCurrentDirectory, CreateHost().CurrentDirectory);
    }

    /// <summary>Host-native normalization collapses dot segments into the canonical form.</summary>
    [Fact]
    public void NormalizeNativePath_CollapsesToCanonicalForm()
    {
        Assert.Equal(NativeSampleExpected, CreateHost().NormalizeNativePath(NativeSample));
    }
}

/// <summary>BDP v4 step 2: the host contract proven against scripted data.</summary>
public sealed class WorkspaceHostEnvironmentMockDataTests : WorkspaceHostEnvironmentContractTests
{
    /// <inheritdoc />
    protected override WorkspacePathPlatform RunningPlatform => WorkspacePathPlatform.Windows;
    /// <inheritdoc />
    protected override string ExpectedCurrentDirectory => "C:/fixture/current";
    /// <inheritdoc />
    protected override string NativeSample => "C:/fixture/./repo";
    /// <inheritdoc />
    protected override string NativeSampleExpected => "C:/fixture/repo";

    /// <inheritdoc />
    protected override IWorkspaceHostEnvironment CreateHost()
    {
        var host = Substitute.For<IWorkspaceHostEnvironment>();
        host.Platform.Returns(RunningPlatform);
        host.CurrentDirectory.Returns(ExpectedCurrentDirectory);
        host.NormalizeNativePath(NativeSample).Returns(NativeSampleExpected);
        return host;
    }
}

/// <summary>Pure Linux-host scripted data preserves Linux cases without reading the machine's platform or directory.</summary>
public sealed class WorkspaceHostEnvironmentLinuxMockDataTests : WorkspaceHostEnvironmentContractTests
{
    /// <inheritdoc />
    protected override WorkspacePathPlatform RunningPlatform => WorkspacePathPlatform.CaseSensitive;
    /// <inheritdoc />
    protected override string ExpectedCurrentDirectory => "/fixture/current";
    /// <inheritdoc />
    protected override string NativeSample => "/fixture/./repo";
    /// <inheritdoc />
    protected override string NativeSampleExpected => "/fixture/repo";
    /// <inheritdoc />
    protected override IWorkspaceHostEnvironment CreateHost()
    {
        var host = Substitute.For<IWorkspaceHostEnvironment>();
        host.Platform.Returns(RunningPlatform);
        host.CurrentDirectory.Returns(ExpectedCurrentDirectory);
        host.NormalizeNativePath(NativeSample).Returns(NativeSampleExpected);
        return host;
    }
}

/// <summary>BDP v4 step 3: the host contract against <see cref="SystemWorkspaceHostEnvironment"/>.</summary>
[Trait("Category", "Integration")]
public sealed class WorkspaceHostEnvironmentRealTests : WorkspaceHostEnvironmentContractTests
{
    /// <inheritdoc />
    protected override WorkspacePathPlatform RunningPlatform => OperatingSystem.IsWindows() ? WorkspacePathPlatform.Windows : WorkspacePathPlatform.CaseSensitive;
    /// <inheritdoc />
    protected override string ExpectedCurrentDirectory => Environment.CurrentDirectory;
    /// <inheritdoc />
    protected override string NativeSample => OperatingSystem.IsWindows() ? @"C:\svc\.\repo" : "/srv/./repo";
    /// <inheritdoc />
    protected override string NativeSampleExpected => OperatingSystem.IsWindows() ? "C:/svc/repo" : "/srv/repo";

    /// <inheritdoc />
    protected override IWorkspaceHostEnvironment CreateHost() => SystemWorkspaceHostEnvironment.Instance;
}
