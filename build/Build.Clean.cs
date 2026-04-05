using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Clean build outputs and artifacts.</summary>
    public Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();

            DotNetClean(_ => _
                .SetProject(Solution)
                .SetConfiguration(Configuration));
        });
}
