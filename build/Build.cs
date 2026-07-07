using System.Text;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Serilog;

/// <summary>
/// Main Nuke build orchestration entry point.
/// </summary>
partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Solution(SuppressBuildProjectCheck = true)]
    readonly Solution Solution = null!;

    /// <summary>Root directory of the repository.</summary>
    public AbsolutePath SourceDirectory => RootDirectory / "src";

    /// <summary>Test projects directory.</summary>
    public AbsolutePath TestsDirectory => RootDirectory / "tests";

    /// <summary>Build artifacts output directory.</summary>
    public AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    /// <summary>Local NuGet packages output directory.</summary>
    public AbsolutePath LocalPackagesDirectory => RootDirectory / "local-packages";

    /// <summary>
    /// Writes the combined markdown (prompt + results/findings) for an aiUnit review to docs/reviews,
    /// similar to the logic in PlanTransactionReviewTests. Looks for the latest runlog in the given artifact subdir.
    /// </summary>
    public void WriteAiUnitReviewMarkdown(string reviewType, string artifactSubDir, string reviewNameForLog)
    {
        var root = RootDirectory;
        var artifactDir = root / "artifacts" / artifactSubDir;
        if (!Directory.Exists(artifactDir))
        {
            Log.Warning("No aiunit artifacts dir for {ReviewType} at {Dir}", reviewType, artifactDir);
            return;
        }

        var latest = Directory.GetFiles(artifactDir, $"aiunit-review-{reviewType}-*.json")
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (latest == null)
        {
            Log.Warning("No aiunit-review-{ReviewType}*.json found in {Dir}", reviewType, artifactDir);
            return;
        }

        var resultJson = File.ReadAllText(latest);
        using var document = JsonDocument.Parse(resultJson);
        var rootEl = document.RootElement;

        string prompt = rootEl.TryGetProperty("prompt", out var p) ? p.GetString() ?? "" : "";
        string findingsJson = rootEl.TryGetProperty("findings", out var f) ? f.GetRawText() : resultJson;

        var reviewsDir = root / "docs" / "reviews";
        Directory.CreateDirectory(reviewsDir);

        var baseName = Path.GetFileNameWithoutExtension(latest);
        var mdPath = reviewsDir / (baseName + ".md");

        var markdown = new StringBuilder()
            .Append("# aiUnit Review: ").AppendLine(reviewType)
            .AppendLine()
            .Append("- Run-log: `").Append(Path.GetFileName(latest)).AppendLine("`")
            .Append("- Source: `").Append(latest).AppendLine("`")
            .AppendLine()
            .AppendLine("## Prompt")
            .AppendLine()
            .AppendLine("```text")
            .AppendLine(prompt.TrimEnd())
            .AppendLine("```")
            .AppendLine()
            .AppendLine("## Response")
            .AppendLine()
            .AppendLine("```json")
            .AppendLine(findingsJson.TrimEnd())
            .AppendLine("```")
            .ToString();

        File.WriteAllText(mdPath, markdown);
        Log.Information("Wrote combined aiUnit {ReviewName} markdown: {Path}", reviewNameForLog, mdPath);
    }

    /// <summary>
    /// Writes the markdown directly from prompt and response data (used when the review is triggered inside the target).
    /// </summary>
    public void WriteAiUnitReviewMarkdownFromData(string reviewType, string prompt, string responseJson, string runLogPath)
    {
        var reviewsDir = RootDirectory / "docs" / "reviews";
        Directory.CreateDirectory(reviewsDir);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmss.fffZ");
        var baseName = $"aiunit-review-{reviewType}-{timestamp}";
        var mdPath = reviewsDir / (baseName + ".md");

        var markdown = new StringBuilder()
            .Append("# aiUnit Review: ").AppendLine(reviewType)
            .AppendLine()
            .Append("- Run-log: `").Append(Path.GetFileName(runLogPath)).AppendLine("`")
            .Append("- Source: `").Append(runLogPath).AppendLine("`")
            .AppendLine()
            .AppendLine("## Prompt")
            .AppendLine()
            .AppendLine("```text")
            .AppendLine(prompt.TrimEnd())
            .AppendLine("```")
            .AppendLine()
            .AppendLine("## Response")
            .AppendLine()
            .AppendLine("```json")
            .AppendLine(responseJson.TrimEnd())
            .AppendLine("```")
            .ToString();

        File.WriteAllText(mdPath, markdown);
        Log.Information("Wrote combined aiUnit {ReviewType} markdown from triggered review: {Path}", reviewType, mdPath);
    }

}
