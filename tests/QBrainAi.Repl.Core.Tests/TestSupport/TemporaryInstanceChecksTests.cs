using QBrainAi.TestSupport.TemporaryInstance;
using Xunit;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// Tests the two checks an integration case uses to prove it runs on a temporary instance it owns: the REPL host's
/// start arguments name a workspace and a marker inside the instance's own root, and the instance's health answer is
/// 200 and echoes the request's nonce.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-NUKE-019 AC-6 (every integration case MigrationIntegrationTests runs uses a temporary
/// instance it owns, never the live server, its marker or its API key, and asserts on that instance's answers instead
/// of catching a failure and passing; hostile validation round 8b, policy 7.9; the live-marker defect is
/// triage-report-81596036a11249d3b79afac1b53d53d9). Test data: scripted argument lists and health answers with the
/// verdict each must get, as literal strings with both separator styles, run against checks passed in, so the
/// contract is proved on scripted reference checks with negative controls before the shared checks exist; no test
/// touches the file system, the network or the process environment.
/// </remarks>
public sealed class TemporaryInstanceChecksTests
{
    private const string OwnedRoot = "/scratch/owned";
    private const string WindowsOwnedRoot = @"C:\scratch\owned";
    private const string Nonce = "nonce-r8b-1";

    /// <summary>
    /// The start-argument contract rows: a row name, the REPL host's arguments, the owned root, and whether the check
    /// must accept them. Only a workspace and a marker strictly inside the owned root are accepted.
    /// </summary>
    internal static readonly (string Name, string[] Arguments, string Root, bool Accepted)[] StartRows =
    [
        ("owned layout", Start("/scratch/owned/workspace", "/scratch/owned/workspace/AGENTS-README-FIRST.yaml"), OwnedRoot, true),
        ("owned layout with Windows separators", Start(@"C:\scratch\owned\workspace", @"C:\scratch\owned\workspace\AGENTS-README-FIRST.yaml"), WindowsOwnedRoot, true),
        ("the root checkout's marker", Start("/scratch/owned/workspace", "/home/user/github/McpServer/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("a marker in another temporary root", Start("/scratch/owned/workspace", "/scratch/other/workspace/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("a sibling root sharing the prefix", Start("/scratch/owned/workspace", "/scratch/owned2/workspace/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("a parent-segment escape", Start("/scratch/owned/workspace", "/scratch/owned/../live/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("the owned root itself as the marker", Start("/scratch/owned/workspace", "/scratch/owned"), OwnedRoot, false),
        ("no marker argument", ["host.dll", "--agent-stdio", "--workspace-path", "/scratch/owned/workspace"], OwnedRoot, false),
        ("a marker argument without a value", ["host.dll", "--agent-stdio", "--workspace-path", "/scratch/owned/workspace", "--marker-file"], OwnedRoot, false),
        ("a workspace outside the root", Start("/home/user/github/McpServer", "/scratch/owned/workspace/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("no workspace argument", ["host.dll", "--agent-stdio", "--marker-file", "/scratch/owned/workspace/AGENTS-README-FIRST.yaml"], OwnedRoot, false),
    ];

    /// <summary>
    /// The health-answer contract rows: a row name, the status code, the body, and whether the check must accept the
    /// answer for <see cref="Nonce"/>. Only a 200 answer whose body holds the nonce is accepted.
    /// </summary>
    internal static readonly (string Name, int Status, string? Body, bool Accepted)[] HealthRows =
    [
        ("200 echoing the nonce", 200, "Healthy " + Nonce, true),
        ("503 echoing the nonce", 503, "Unhealthy " + Nonce, false),
        ("200 without the nonce", 200, "Healthy", false),
        ("200 with an empty body", 200, string.Empty, false),
        ("200 with no body", 200, null, false),
        ("200 echoing a different nonce", 200, "Healthy nonce-r8b-2", false),
    ];

    /// <summary>
    /// Returns the names of the start-argument rows on which <paramref name="check"/> gives the wrong verdict (a
    /// violation message for an accepted row, or null for a rejected row).
    /// </summary>
    /// <param name="check">The start-argument check under test: the arguments and the owned root to a violation or null.</param>
    internal static List<string> StartContractFailures(Func<IReadOnlyList<string>, string, string?> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return StartRows
            .Where(row => (check(row.Arguments, row.Root) is null) != row.Accepted)
            .Select(row => row.Name)
            .ToList();
    }

    /// <summary>Returns the names of the health rows on which <paramref name="check"/> gives the wrong verdict.</summary>
    /// <param name="check">The health check under test: the status, the body and the nonce to a violation or null.</param>
    internal static List<string> HealthContractFailures(Func<int, string?, string, string?> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return HealthRows
            .Where(row => (check(row.Status, row.Body, Nonce) is null) != row.Accepted)
            .Select(row => row.Name)
            .ToList();
    }

    /// <summary>
    /// Binding: the shared checks, <see cref="TemporaryInstanceChecks.CheckReplStartArguments"/> and
    /// <see cref="TemporaryInstanceChecks.CheckHealthAnswer"/>, satisfy the same rows the scripted reference passed and
    /// the wrong checks failed.
    /// </summary>
    [Fact]
    public void SharedChecks_SatisfyTheContract()
    {
        Assert.Empty(StartContractFailures(TemporaryInstanceChecks.CheckReplStartArguments));
        Assert.Empty(HealthContractFailures(TemporaryInstanceChecks.CheckHealthAnswer));
    }

    /// <summary>Mock gate positive control: the scripted reference checks satisfy every row of both contracts.</summary>
    [Fact]
    public void ScriptedReferenceChecks_SatisfyTheContract()
    {
        Assert.Empty(StartContractFailures(ScriptedStart));
        Assert.Empty(HealthContractFailures(ScriptedHealth));
    }

    /// <summary>
    /// Mock gate negative controls, each rejected inside a passing harness on the row named: a check that accepts
    /// anything, one that checks only the workspace, one that compares a string prefix without a segment boundary, one
    /// that does not refuse a parent segment, and one that accepts a missing marker.
    /// </summary>
    /// <param name="wrong">The name of the wrong start-argument check.</param>
    /// <param name="failingRow">The contract row that must reject it.</param>
    [Theory]
    [InlineData("accepts anything", "the root checkout's marker")]
    [InlineData("workspace only", "the root checkout's marker")]
    [InlineData("string prefix", "a sibling root sharing the prefix")]
    [InlineData("no parent-segment refusal", "a parent-segment escape")]
    [InlineData("missing marker allowed", "no marker argument")]
    public void WrongStartChecks_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, StartContractFailures(WrongStartCheck(wrong)));

    /// <summary>
    /// Mock gate negative controls on the health contract: a check that reads only the status and one that reads only
    /// the body, each rejected on the row named.
    /// </summary>
    /// <param name="wrong">The name of the wrong health check.</param>
    /// <param name="failingRow">The contract row that must reject it.</param>
    [Theory]
    [InlineData("status only", "200 without the nonce")]
    [InlineData("body only", "503 echoing the nonce")]
    public void WrongHealthChecks_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, HealthContractFailures(WrongHealthCheck(wrong)));

    private static string[] Start(string workspace, string marker) =>
        ["host.dll", "--agent-stdio", "--agent", "default", "--workspace-path", workspace, "--marker-file", marker];

    private static string? ScriptedStart(IReadOnlyList<string> arguments, string root)
    {
        var workspace = ValueAfter(arguments, "--workspace-path");
        var marker = ValueAfter(arguments, "--marker-file");
        if (workspace is null)
            return "The REPL host was started without --workspace-path.";
        if (marker is null)
            return "The REPL host was started without --marker-file.";
        if (!ScriptedInside(workspace, root))
            return $"The workspace '{workspace}' is outside the owned root '{root}'.";
        return ScriptedInside(marker, root) ? null : $"The marker '{marker}' is outside the owned root '{root}'.";
    }

    private static string? ScriptedHealth(int status, string? body, string nonce) =>
        status == 200 && !string.IsNullOrEmpty(nonce) && body is not null && body.Contains(nonce, StringComparison.Ordinal)
            ? null
            : $"The health answer was {status} and did not echo the nonce '{nonce}'.";

    private static bool ScriptedInside(string path, string root)
    {
        var pathSegments = Segments(path);
        var rootSegments = Segments(root);
        if (pathSegments.Any(segment => segment is "." or ".."))
            return false;
        return pathSegments.Length > rootSegments.Length
            && rootSegments.SequenceEqual(pathSegments.Take(rootSegments.Length), StringComparer.Ordinal);
    }

    private static string[] Segments(string path) =>
        path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

    private static string? ValueAfter(IReadOnlyList<string> arguments, string name)
    {
        for (var index = 0; index < arguments.Count - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                return arguments[index + 1];
        }

        return null;
    }

    private static Func<IReadOnlyList<string>, string, string?> WrongStartCheck(string wrong) => wrong switch
    {
        "accepts anything" => (_, _) => null,
        "workspace only" => (arguments, root) =>
            ValueAfter(arguments, "--workspace-path") is { } workspace && ScriptedInside(workspace, root) ? null : "workspace outside",
        "string prefix" => (arguments, root) =>
            ValueAfter(arguments, "--workspace-path") is { } workspace
            && ValueAfter(arguments, "--marker-file") is { } marker
            && workspace.StartsWith(root, StringComparison.Ordinal)
            && marker.StartsWith(root, StringComparison.Ordinal)
            && marker.Length > root.Length
            && !marker.Contains("..", StringComparison.Ordinal)
                ? null
                : "outside",
        "no parent-segment refusal" => (arguments, root) =>
            ValueAfter(arguments, "--workspace-path") is { } workspace
            && ValueAfter(arguments, "--marker-file") is { } marker
            && SegmentPrefix(workspace, root)
            && SegmentPrefix(marker, root)
                ? null
                : "outside",
        "missing marker allowed" => (arguments, root) =>
            ValueAfter(arguments, "--marker-file") is null ? null : ScriptedStart(arguments, root),
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong start check."),
    };

    private static Func<int, string?, string, string?> WrongHealthCheck(string wrong) => wrong switch
    {
        "status only" => (status, _, _) => status == 200 ? null : "not 200",
        "body only" => (_, body, nonce) => body is not null && body.Contains(nonce, StringComparison.Ordinal) ? null : "no nonce",
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong health check."),
    };

    private static bool SegmentPrefix(string path, string root)
    {
        var pathSegments = Segments(path);
        var rootSegments = Segments(root);
        return pathSegments.Length > rootSegments.Length
            && rootSegments.SequenceEqual(pathSegments.Take(rootSegments.Length), StringComparer.Ordinal);
    }
}
