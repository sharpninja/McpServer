using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;

/// <summary>
/// Main Nuke build orchestration entry point.
/// </summary>
partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Solution(SuppressBuildProjectCheck = true)]
    readonly Solution Solution;

    /// <summary>Root directory of the repository.</summary>
    public AbsolutePath SourceDirectory => RootDirectory / "src";

    /// <summary>Test projects directory.</summary>
    public AbsolutePath TestsDirectory => RootDirectory / "tests";

    /// <summary>Build artifacts output directory.</summary>
    public AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    /// <summary>Local NuGet packages output directory.</summary>
    public AbsolutePath LocalPackagesDirectory => RootDirectory / "local-packages";
}
