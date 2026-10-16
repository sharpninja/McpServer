using NSubstitute;
using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001: NSubstitute <see cref="IWorkspaceHostEnvironment"/> doubles so any test
/// runner can simulate a Linux hub or a Windows host, and the matching normalizers for
/// BDP v4 MockData (scripted) and Real (production) fixtures.
/// </summary>
internal static class SimulatedWorkspaceHosts
{
    /// <summary>Working directory of the simulated Linux hub.</summary>
    public const string LinuxWorkingDirectory = "/opt/mcpserver/app";

    /// <summary>Working directory of the simulated Windows host.</summary>
    public const string WindowsWorkingDirectory = @"C:\svc";

    /// <summary>Simulated Linux hub: POSIX host, working directory /opt/mcpserver/app.</summary>
    public static IWorkspaceHostEnvironment Linux()
    {
        var host = Substitute.For<IWorkspaceHostEnvironment>();
        host.Platform.Returns(WorkspacePathPlatform.CaseSensitive);
        host.CurrentDirectory.Returns(LinuxWorkingDirectory);
        host.NormalizeNativePath(Arg.Any<string>()).Returns(call =>
        {
            var path = call.Arg<string>()!;
            var full = path.StartsWith('/') ? path : LinuxWorkingDirectory + "/" + path;
            return WorkspaceIdentityPath.NormalizeLexicalPath(full, WorkspacePathPlatform.CaseSensitive);
        });
        return host;
    }

    /// <summary>Simulated Windows host: working directory C:\svc.</summary>
    public static IWorkspaceHostEnvironment Windows()
    {
        var host = Substitute.For<IWorkspaceHostEnvironment>();
        host.Platform.Returns(WorkspacePathPlatform.Windows);
        host.CurrentDirectory.Returns(WindowsWorkingDirectory);
        host.NormalizeNativePath(Arg.Any<string>()).Returns(call =>
        {
            var path = call.Arg<string>()!;
            var full = path.Length >= 2 && path[1] == ':' ? path : WindowsWorkingDirectory + @"\" + path;
            return WorkspaceIdentityPath.NormalizeLexicalPath(full, WorkspacePathPlatform.Windows);
        });
        return host;
    }

    /// <summary>Simulated host for a platform.</summary>
    public static IWorkspaceHostEnvironment For(WorkspacePathPlatform platform) =>
        platform == WorkspacePathPlatform.Windows ? Windows() : Linux();

    /// <summary>BDP v4 MockData fixture: scripted normalizer over a simulated host.</summary>
    public static IWorkspacePathNormalizer Scripted(WorkspacePathPlatform hostPlatform) =>
        new ScriptedWorkspacePathNormalizer(For(hostPlatform));

    /// <summary>BDP v4 Real fixture: production normalizer over a simulated host.</summary>
    public static IWorkspacePathNormalizer Real(WorkspacePathPlatform hostPlatform) =>
        new WorkspacePathNormalizer(For(hostPlatform));
}
