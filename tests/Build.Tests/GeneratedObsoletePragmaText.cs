namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-AIUNIT-002: the exact CS0612/CS0618 pragma pair that EF Core once generated in migration files, built from
/// tokens so this source never holds the literal suppression the warning-suppression scanner looks for. Shared by link
/// between the two halves of WarningSuppressionValidationTargetTests: the infrastructure half writes it into fixtures
/// for the normalizer cases, the integration half searches the repository's generated migrations for it.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-AIUNIT-002; TR-MCP-QUALITY-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from the private fields of WarningSuppressionValidationTargetTests when that class split into its infrastructure half (Build.Tests) and its integration half (Build.IntegrationTests) in slice S4 (hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY).</remarks>
internal static class GeneratedObsoletePragmaText
{
    private const string PragmaToken = "#pragma";

    /// <summary>The generated disable line for CS0612 and CS0618.</summary>
    internal static readonly string Disable = PragmaToken + " warning disable " + "612, 618";

    /// <summary>The generated restore line for CS0612 and CS0618.</summary>
    internal static readonly string Restore = PragmaToken + " warning restore " + "612, 618";
}
