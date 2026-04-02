using Nuke.Common;
using Nuke.Common.Tooling;
using Serilog;

partial class Build
{
    /// <summary>Increment the patch version in GitVersion.yml.</summary>
    public Target BumpVersion => _ => _
        .Executes(() =>
        {
            var gitVersionPath = RootDirectory / "GitVersion.yml";
            var content = File.ReadAllText(gitVersionPath);

            var result = GitVersionBumper.BumpPatch(content)
                ?? throw new InvalidOperationException("Could not parse next-version from GitVersion.yml.");

            File.WriteAllText(gitVersionPath, result.NewContent);
            Log.Information("Bumped GitVersion: {Old} → {New}", result.OldVersion, result.NewVersion);

            ProcessTasks.StartProcess("git", $"-C \"{RootDirectory}\" add GitVersion.yml")
                .AssertZeroExitCode();
        });
}
