using NSubstitute;
using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001 / TR-MCP-FED-PATH-001: contract for the process
/// <see cref="IWorkspaceHostEnvironment"/>: it reports the running OS, the process working
/// directory, and canonical host-native normalization. BDP v4 (plan section 2a):
/// <see cref="WorkspaceHostEnvironmentMockDataTests"/> then <see cref="WorkspaceHostEnvironmentRealTests"/>.
/// </summary>
public abstract class WorkspaceHostEnvironmentContractTests
{
    /// <summary>Platform the running test process is on.</summary>
    protected static WorkspacePathPlatform RunningPlatform =>
        OperatingSystem.IsWindows() ? WorkspacePathPlatform.Windows : WorkspacePathPlatform.CaseSensitive;

    /// <summary>A host-native path with a dot segment for this runner.</summary>
    protected static string NativeSample => OperatingSystem.IsWindows() ? @"C:\svc\.\repo" : "/srv/./repo";

    /// <summary>Expected canonical form of <see cref="NativeSample"/>.</summary>
    protected static string NativeSampleExpected => OperatingSystem.IsWindows() ? @"C:\svc\repo" : "/srv/repo";

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
        Assert.Equal(Environment.CurrentDirectory, CreateHost().CurrentDirectory);
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
    protected override IWorkspaceHostEnvironment CreateHost()
    {
        var host = Substitute.For<IWorkspaceHostEnvironment>();
        host.Platform.Returns(RunningPlatform);
        host.CurrentDirectory.Returns(Environment.CurrentDirectory);
        host.NormalizeNativePath(NativeSample).Returns(NativeSampleExpected);
        return host;
    }
}

/// <summary>BDP v4 step 3: the host contract against <see cref="SystemWorkspaceHostEnvironment"/>.</summary>
public sealed class WorkspaceHostEnvironmentRealTests : WorkspaceHostEnvironmentContractTests
{
    /// <inheritdoc />
    protected override IWorkspaceHostEnvironment CreateHost() => SystemWorkspaceHostEnvironment.Instance;
}
