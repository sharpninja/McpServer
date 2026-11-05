using System.Text.Json;
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
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 ac-12 (strengthened: the answer's nonce field equals the whole nonce): health bodies are now JSON in the server's shape, with rows for a longer and a shorter nonce, the nonce in another field, a malformed body and a non-string nonce, and wrong checks "nonce substring" (the earlier shared check) and "any field", because hostile validation finding R9-F2 showed a substring match accepted another nonce. FR-MCP-TESTRESOURCE-001 ac-4 (strengthened: segments compared under the path's own syntax): start rows for a Windows case alias (accepted) and a POSIX case alias (refused), and wrong checks "ordinal only" and "ignores case everywhere", the sibling of finding R9-F3 (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
/// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "Strict containment in the owned root also requires the same root"): start rows for a UNC owned root (accepted, and accepted in another case with mixed separators), a single-rooted marker or workspace under a UNC root, a marker on another share, a UNC marker under a POSIX root and a marker on another drive (each refused), the wrong checks "ignores the root" (the round 10 shared check) and "UNC compared ordinally", and the scripted reference moved to ScriptedPathIdentity, because hostile validation round 10 finding R10-F2 showed the check flattened the root (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// </list>
/// </remarks>
public sealed class TemporaryInstanceChecksTests
{
    private const string OwnedRoot = "/scratch/owned";
    private const string WindowsOwnedRoot = @"C:\scratch\owned";
    /// <summary>FR-MCP-TESTRESOURCE-001 ac-4: the UNC owned root of the start rows that require the same root kind and identity.</summary>
    /// <remarks>2026-11-05, FR-MCP-TESTRESOURCE-001 ac-4: this entry documents the constant, added on 2026-11-03 with the UNC start rows (hostile validation round 10 finding R10-F2), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private const string UncOwnedRoot = "//server/share/owned";
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
        ("owned layout in another case on Windows", Start(@"c:\Scratch\owned\workspace", @"C:\scratch\OWNED\workspace\AGENTS-README-FIRST.yaml"), WindowsOwnedRoot, true),
        ("a POSIX marker differing only in case is another root", Start("/scratch/owned/workspace", "/scratch/Owned/workspace/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("owned UNC layout", Start("//server/share/owned/workspace", "//server/share/owned/workspace/AGENTS-README-FIRST.yaml"), UncOwnedRoot, true),
        ("owned UNC layout in another case and separators", Start(@"\\SERVER\Share\owned\workspace", @"//server/SHARE\Owned/workspace\AGENTS-README-FIRST.yaml"), UncOwnedRoot, true),
        ("a single-rooted marker under a UNC root is another root", Start("//server/share/owned/workspace", "/server/share/owned/workspace/AGENTS-README-FIRST.yaml"), UncOwnedRoot, false),
        ("a single-rooted workspace under a UNC root is another root", Start("/server/share/owned/workspace", "//server/share/owned/workspace/AGENTS-README-FIRST.yaml"), UncOwnedRoot, false),
        ("a marker on another UNC share", Start("//server/share/owned/workspace", "//server/other/owned/workspace/AGENTS-README-FIRST.yaml"), UncOwnedRoot, false),
        ("a UNC marker under a POSIX root is another root", Start("/scratch/owned/workspace", "//scratch/owned/workspace/AGENTS-README-FIRST.yaml"), OwnedRoot, false),
        ("a marker on another drive", Start(@"C:\scratch\owned\workspace", @"D:\scratch\owned\workspace\AGENTS-README-FIRST.yaml"), WindowsOwnedRoot, false),
    ];

    /// <summary>
    /// The health-answer contract rows: a row name, the status code, the body, and whether the check must accept the
    /// answer for <see cref="Nonce"/>. Bodies are JSON in the server's health shape (status and nonce fields). Only a 200
    /// answer whose nonce field equals the whole nonce, compared ordinally, is accepted.
    /// </summary>
    internal static readonly (string Name, int Status, string? Body, bool Accepted)[] HealthRows =
    [
        ("200 echoing the nonce", 200, HealthBody("Healthy", Nonce), true),
        ("503 echoing the nonce", 503, HealthBody("Unhealthy", Nonce), false),
        ("200 without the nonce", 200, HealthBody("Healthy", null), false),
        ("200 with an empty body", 200, string.Empty, false),
        ("200 with no body", 200, null, false),
        ("200 echoing a different nonce", 200, HealthBody("Healthy", "nonce-r8b-2"), false),
        ("200 with a longer nonce", 200, HealthBody("Healthy", Nonce + "-suffix"), false),
        ("200 with a shorter nonce", 200, HealthBody("Healthy", "nonce-r8b"), false),
        ("200 with the nonce in another field", 200, HealthBody("Healthy " + Nonce, "nonce-other"), false),
        ("200 with a body that is not JSON", 200, "Healthy " + Nonce, false),
        ("200 with a nonce field that is not text", 200, JsonSerializer.Serialize(new { status = "Healthy", nonce = 7 }), false),
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
    [InlineData("ordinal only", "owned layout in another case on Windows")]
    [InlineData("ignores case everywhere", "a POSIX marker differing only in case is another root")]
    [InlineData("ignores the root", "a single-rooted marker under a UNC root is another root")]
    [InlineData("UNC compared ordinally", "owned UNC layout in another case and separators")]
    public void WrongStartChecks_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, StartContractFailures(WrongStartCheck(wrong)));

    /// <summary>
    /// Mock gate negative controls on the health contract: a check that reads only the status, one that reads only the
    /// body, one that accepts the nonce anywhere in the body as a substring, and one that accepts the nonce in any field,
    /// each rejected on the row named.
    /// </summary>
    /// <param name="wrong">The name of the wrong health check.</param>
    /// <param name="failingRow">The contract row that must reject it.</param>
    [Theory]
    [InlineData("status only", "200 without the nonce")]
    [InlineData("body only", "503 echoing the nonce")]
    [InlineData("nonce substring", "200 with a longer nonce")]
    [InlineData("any field", "200 with the nonce in another field")]
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

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-12 scripted reference: accepts a 200 answer only when its JSON body holds a text field
    /// "nonce" equal, ordinally, to the whole nonce the request sent.
    /// </summary>
    /// <param name="status">The answer's status code.</param>
    /// <param name="body">The answer's body, or null.</param>
    /// <param name="nonce">The nonce the request sent.</param>
    /// <returns>Null for an accepted answer; otherwise the violation.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-12: the nonce field is compared exactly instead of the body as a substring (hostile validation finding R9-F2; operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</remarks>
    private static string? ScriptedHealth(int status, string? body, string nonce) =>
        status == 200 && !string.IsNullOrEmpty(nonce) && ScriptedNonceField(body) is { } echoed && string.Equals(echoed, nonce, StringComparison.Ordinal)
            ? null
            : $"The health answer was {status} and did not echo the nonce '{nonce}'.";

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-12 scripted reference: the text value of a JSON body's "nonce" field, if any.</summary>
    /// <param name="body">The body, or null.</param>
    /// <returns>The nonce text, or null when the body is missing, is not a JSON object, or has no text nonce field.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-12: added for hostile validation finding R9-F2.</remarks>
    private static string? ScriptedNonceField(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("nonce", out var field)
                && field.ValueKind == JsonValueKind.String
                    ? field.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-12: a health body in the server's shape, built from an object.</summary>
    /// <param name="status">The status field.</param>
    /// <param name="nonce">The nonce field, or null to leave it out.</param>
    /// <returns>The JSON body.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-12: added for hostile validation finding R9-F2.</remarks>
    private static string HealthBody(string status, string? nonce) =>
        nonce is null ? JsonSerializer.Serialize(new { status }) : JsonSerializer.Serialize(new { status, nonce });

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4 scripted reference: whether a path lies strictly inside a root, through
    /// <see cref="ScriptedPathIdentity.Inside"/>: the same root kind and root identity first, then by path segment with
    /// either separator and no '.' or '..' segment, UNC and drive-letter paths ignoring case and POSIX paths ordinally.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="root">The root.</param>
    /// <returns>True when the path is strictly inside the root.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-4: compares under the path's own syntax instead of ordinally everywhere (the sibling of hostile validation finding R9-F3). 2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 (appended "Strict containment in the owned root also requires the same root"): delegates to ScriptedPathIdentity, which compares root kind and root identity first (hostile validation round 10 finding R10-F2). 2026-11-03, documentation only: the summary states the delegated rule instead of the 2026-11-02 one (stale summary found before round 11).</remarks>
    private static bool ScriptedInside(string path, string root) => ScriptedPathIdentity.Inside(path, root);

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
        "ordinal only" => (arguments, root) => WrongCaseStart(arguments, root, StringComparer.Ordinal),
        "ignores case everywhere" => (arguments, root) => WrongCaseStart(arguments, root, StringComparer.OrdinalIgnoreCase),
        "ignores the root" => (arguments, root) => WrongRootStart(arguments, root, flattenRoot: true, ordinalUnc: false),
        "UNC compared ordinally" => (arguments, root) => WrongRootStart(arguments, root, flattenRoot: false, ordinalUnc: true),
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong start check."),
    };

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4, for the wrong checks "ignores the root" (the round 10 shared check, which flattened the
    /// root into the segments) and "UNC compared ordinally": a start check over the scripted path identity with one flaw.
    /// </summary>
    /// <param name="arguments">The REPL host's arguments.</param>
    /// <param name="root">The owned root.</param>
    /// <param name="flattenRoot">Ignore root kind and identity.</param>
    /// <param name="ordinalUnc">Compare UNC paths ordinally.</param>
    /// <returns>Null when the workspace and the marker are inside the root under the flawed rule.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 (appended "Strict containment in the owned root also requires the same root"): added for hostile validation round 10 finding R10-F2.</remarks>
    private static string? WrongRootStart(IReadOnlyList<string> arguments, string root, bool flattenRoot, bool ordinalUnc) =>
        ValueAfter(arguments, "--workspace-path") is { } workspace
        && ValueAfter(arguments, "--marker-file") is { } marker
        && ScriptedPathIdentity.Inside(workspace, root, flattenRoot, ordinalUnc)
        && ScriptedPathIdentity.Inside(marker, root, flattenRoot, ordinalUnc)
            ? null
            : "outside";

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4, for the wrong checks "ordinal only" and "ignores case everywhere": a start check
    /// that compares every path's segments with one comparer, whatever its syntax.
    /// </summary>
    /// <param name="arguments">The REPL host's arguments.</param>
    /// <param name="root">The owned root.</param>
    /// <param name="comparer">The one comparer used for every segment.</param>
    /// <returns>Null when the workspace and the marker are strictly inside the root under that comparer.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-4: added for the sibling of hostile validation finding R9-F3.</remarks>
    private static string? WrongCaseStart(IReadOnlyList<string> arguments, string root, StringComparer comparer)
    {
        bool Inside(string path) =>
            !Segments(path).Any(segment => segment is "." or "..")
            && Segments(path).Length > Segments(root).Length
            && Segments(root).SequenceEqual(Segments(path).Take(Segments(root).Length), comparer);
        return ValueAfter(arguments, "--workspace-path") is { } workspace
            && ValueAfter(arguments, "--marker-file") is { } marker
            && Inside(workspace)
            && Inside(marker)
                ? null
                : "outside";
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-12 wrong health checks: status only, body only, the nonce as a substring of the body
    /// (the earlier shared check), and the nonce as the value of any JSON field.
    /// </summary>
    /// <param name="wrong">The name of the wrong check.</param>
    /// <returns>The wrong check.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-12: added "nonce substring" and "any field" for hostile validation finding R9-F2.</remarks>
    private static Func<int, string?, string, string?> WrongHealthCheck(string wrong) => wrong switch
    {
        "status only" => (status, _, _) => status == 200 ? null : "not 200",
        "body only" => (_, body, nonce) => body is not null && body.Contains(nonce, StringComparison.Ordinal) ? null : "no nonce",
        "nonce substring" => (status, body, nonce) => status == 200 && body is not null && body.Contains(nonce, StringComparison.Ordinal) ? null : "no nonce",
        "any field" => (status, body, nonce) => status == 200 && AnyFieldEquals(body, nonce) ? null : "no nonce",
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong health check."),
    };

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-12, for the wrong check "any field": whether any text field of a JSON body contains the nonce.</summary>
    /// <param name="body">The body, or null.</param>
    /// <param name="nonce">The nonce.</param>
    /// <returns>True when a text field of the body's top-level object contains the nonce.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-12: added for hostile validation finding R9-F2.</remarks>
    private static bool AnyFieldEquals(string? body, string nonce)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.EnumerateObject().Any(field =>
                    field.Value.ValueKind == JsonValueKind.String && (field.Value.GetString() ?? string.Empty).Contains(nonce, StringComparison.Ordinal));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool SegmentPrefix(string path, string root)
    {
        var pathSegments = Segments(path);
        var rootSegments = Segments(root);
        return pathSegments.Length > rootSegments.Length
            && rootSegments.SequenceEqual(pathSegments.Take(rootSegments.Length), StringComparer.Ordinal);
    }
}
