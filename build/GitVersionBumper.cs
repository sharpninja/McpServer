using System.Text.RegularExpressions;

/// <summary>
/// Parses and increments the patch component of the next-version field in GitVersion.yml.
/// </summary>
static partial class GitVersionBumper
{
    [GeneratedRegex(@"(?m)^(next-version:\s*)(\d+)\.(\d+)\.(\d+)")]
    private static partial Regex NextVersionRegex();

    /// <summary>
    /// Parses the next-version from GitVersion.yml content.
    /// </summary>
    public static (int Major, int Minor, int Patch)? ParseVersion(string content)
    {
        var match = NextVersionRegex().Match(content);
        if (!match.Success)
            return null;

        return (
            int.Parse(match.Groups[2].Value),
            int.Parse(match.Groups[3].Value),
            int.Parse(match.Groups[4].Value));
    }

    /// <summary>
    /// Bumps the patch version in GitVersion.yml content and returns the updated content
    /// along with old and new version strings.
    /// </summary>
    public static (string NewContent, string OldVersion, string NewVersion)? BumpPatch(string content)
    {
        var version = ParseVersion(content);
        if (version is null)
            return null;

        var (major, minor, patch) = version.Value;
        var oldVersion = $"{major}.{minor}.{patch}";
        var newVersion = $"{major}.{minor}.{patch + 1}";

        var newContent = NextVersionRegex().Replace(content, $"${{1}}{newVersion}");
        return (newContent, oldVersion, newVersion);
    }
}
