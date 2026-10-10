using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    /// <summary>Restore NuGet packages for the solution and for every build project outside it (TR-MCP-NUKE-002 AC-003).</summary>
    public Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution));

            // Compile builds the whole shared project set with --no-restore, and some of it (the src/Compatibility
            // aliases) is not in the solution, so restore those projects one by one.
            var solutionProjects = Solution.GetAllProjects("*")
                .Select(project => Path.GetFullPath(project.Path))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var projectFile in GetBuildProjectFiles(SourceDirectory, TestsDirectory)
                         .Where(path => !solutionProjects.Contains(Path.GetFullPath(path))))
            {
                DotNetRestore(_ => _
                    .SetProjectFile(projectFile));
            }
        });
}
