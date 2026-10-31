using QBrainAi.TestSupport.TemporaryInstance;
using Xunit;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// Tests the checks an integration case uses to prove it runs on a temporary instance it owns: the REPL host's start
/// arguments name a workspace and a marker inside the instance's own root, and the instance's health answer is 200 and
/// echoes the request's nonce. Also tests the decision to start a new instance on a new port when the instance lost
/// its port at start (TR-MCP-NUKE-019).
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-TESTRESOURCE-001 ac-4 (start arguments) and ac-12 (health answers).
/// Restart rows implement retained plan design and prove no current criterion. Historical context:
/// hostile validation round 8b, policy 7.9; the live-marker defect is triage-report-81596036a11249d3b79afac1b53d53d9.
/// Test data: scripted argument lists and health answers with the
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
        ("the root checkout's marker", Start("/scratch/owned/workspace", "/fixture/workspaces/McpServer/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("a marker in another temporary root", Start("/scratch/owned/workspace", "/scratch/other/workspace/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("a sibling root sharing the prefix", Start("/scratch/owned/workspace", "/scratch/owned2/workspace/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("a parent-segment escape", Start("/scratch/owned/workspace", "/scratch/owned/../live/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("the owned root itself as the marker", Start("/scratch/owned/workspace", "/scratch/owned"), OwnedRoot, false),
        ("no marker argument", ["host.dll", "--agent-stdio", "--workspace-path", "/scratch/owned/workspace"], OwnedRoot, false),
        ("a marker argument without a value", ["host.dll", "--agent-stdio", "--workspace-path", "/scratch/owned/workspace", "--marker-file"], OwnedRoot, false),
        ("a workspace outside the root", Start("/fixture/workspaces/McpServer", "/scratch/owned/workspace/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
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
    /// The restart contract rows (TR-MCP-NUKE-019): a row name, the starts made so far, whether the instance
    /// process has exited, its output, and whether the case must start a new instance on a new port. Only an exited
    /// instance whose output says its address was already in use, within three starts in all, is restarted. The bind
    /// failure text is the one Kestrel wrote in run ug-r8b-expanded-lane-1 (Iteration5IntegrationTests, port 49974).
    /// </summary>
    internal static readonly (string Name, int Starts, bool Exited, string Output, bool Restart)[] RestartRows =
    [
        ("first start lost its port", 1, true, BindFailure, true),
        ("second start lost its port", 2, true, BindFailure, true),
        ("third start lost its port", 3, true, BindFailure, false),
        ("only the inner exception line", 1, true, "---> System.Net.Sockets.SocketException (98): Address already in use", true),
        ("the bind failure in capitals", 1, true, "FAILED TO BIND TO ADDRESS HTTP://[::]:49974: ADDRESS ALREADY IN USE.", true),
        ("instance still running", 1, false, BindFailure, false),
        ("another start failure", 1, true, "fail: QBrainAi.Support.Mcp.Services.TodoBootstrapImporter[0] TodoBootstrapImporter failed", false),
        ("a file in use", 1, true, "The database file is in use by another process.", false),
        ("no output", 1, true, string.Empty, false),
    ];

    private const string BindFailure = "Unhandled exception. System.IO.IOException: Failed to bind to address http://[::]:49974: address already in use.";

    /// <summary>Returns the names of the restart rows on which <paramref name="check"/> gives the wrong verdict.</summary>
    /// <param name="check">The restart decision under test: the starts so far, whether the process exited and its output.</param>
    internal static List<string> RestartContractFailures(Func<int, bool, string, bool> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return RestartRows
            .Where(row => check(row.Starts, row.Exited, row.Output) != row.Restart)
            .Select(row => row.Name)
            .ToList();
    }

    /// <summary>Mock gate positive control (TR-MCP-NUKE-019): the scripted restart decision satisfies every row.</summary>
    [Fact]
    public void ScriptedRestartDecision_SatisfiesTheContract() =>
        Assert.Empty(RestartContractFailures(ScriptedRestart));

    /// <summary>
    /// Mock gate negative controls on the restart contract (TR-MCP-NUKE-019), each rejected inside a passing
    /// harness on the row named: a decision with no bound on starts, one that ignores whether the process exited, one
    /// that matches any "in use" text, one that matches case-sensitively, and one that never restarts.
    /// </summary>
    /// <param name="wrong">The name of the wrong restart decision.</param>
    /// <param name="failingRow">The contract row that must reject it.</param>
    [Theory]
    [InlineData("unbounded", "third start lost its port")]
    [InlineData("ignores exit", "instance still running")]
    [InlineData("any in use", "a file in use")]
    [InlineData("case-sensitive", "the bind failure in capitals")]
    [InlineData("never restarts", "first start lost its port")]
    public void WrongRestartDecisions_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, RestartContractFailures(WrongRestartCheck(wrong)));

    private static bool ScriptedRestart(int starts, bool exited, string output) =>
        starts < 3 && exited && output.Contains("address already in use", StringComparison.OrdinalIgnoreCase);

    private static Func<int, bool, string, bool> WrongRestartCheck(string wrong) => wrong switch
    {
        "unbounded" => (_, exited, output) => exited && output.Contains("address already in use", StringComparison.OrdinalIgnoreCase),
        "ignores exit" => (starts, _, output) => starts < 3 && output.Contains("address already in use", StringComparison.OrdinalIgnoreCase),
        "any in use" => (starts, exited, output) => starts < 3 && exited && output.Contains("in use", StringComparison.OrdinalIgnoreCase),
        "case-sensitive" => (starts, exited, output) => starts < 3 && exited && output.Contains("address already in use", StringComparison.Ordinal),
        "never restarts" => (_, _, _) => false,
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong restart decision."),
    };

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
    /// Binding: the shared checks, <see cref="TemporaryInstanceChecks.CheckReplStartArguments"/>,
    /// <see cref="TemporaryInstanceChecks.CheckHealthAnswer"/> and
    /// <see cref="TemporaryInstanceChecks.ShouldRestartOnNewPort"/> (TR-MCP-NUKE-019), satisfy the same rows the
    /// scripted references passed and the wrong checks failed. Start and health assertions prove FR-MCP-TESTRESOURCE-001 ac-4 and ac-12; restart assertions prove retained plan design, with no current criterion.
    /// </summary>
    [Fact]
    public void SharedChecks_SatisfyTheContract()
    {
        Assert.Empty(StartContractFailures(TemporaryInstanceChecks.CheckReplStartArguments));
        Assert.Empty(HealthContractFailures(TemporaryInstanceChecks.CheckHealthAnswer));
        Assert.Empty(RestartContractFailures(TemporaryInstanceChecks.ShouldRestartOnNewPort));
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4: runs every original workspace/marker case through the shared REPL consumer
    /// used by both integration starters. The consumer returns a plan only for accepted rows and propagates the
    /// shared guard's refusal for the foreign, missing and escaping inputs; this binds a consumer, not the check again.
    /// </summary>
    [Fact]
    public void TemporaryInstanceConsumer_UsesExplicitOwnedWorkspaceAndMarker() =>
        OwnedInstanceIsolationBindingTests.AssertOriginalConsumerRows();

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
