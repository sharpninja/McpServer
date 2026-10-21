using System.Text.RegularExpressions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// Tests that the pg_ctl calls of <see cref="EphemeralPostgresFixture"/> are silent (<c>-s</c>) and wait (<c>-w</c>).
/// pg_ctl runs without output redirection, so it writes to the test process's standard output, which carries the
/// xUnit v3 test framework's JSON messages to the VSTest adapter; its informational lines ("waiting for server to
/// start.... done", "server started") garbled those messages and lost test results in the Windows integration lane.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-NUKE-019 AC-2 (every integration case runs and passes in its host's lane at
/// 0 failed and 0 not executed). Found by the diagnostic lane ug-r3-win-lane-diag-2, whose testhost log shows
/// "Received unparseable output from test process: waiting for server to start....{"$type":"test-passed",...}";
/// fix chosen by the operator on 2026-10-20 ("Diagnose the loss in PR 87"). Test data: scripted pg_ctl argument
/// strings; the start and stop strings the fixture used before are the negative controls. No process is started.
/// </remarks>
public sealed class EphemeralPostgresFixtureArgumentsTests
{
    private static readonly Regex Silent = new(@"(^|\s)-s(\s|$)", RegexOptions.CultureInvariant);

    private static readonly Regex Wait = new(@"(^|\s)-w(\s|$)", RegexOptions.CultureInvariant);

    /// <summary>
    /// Asserts that a pg_ctl argument string runs the given action (<c>start</c> or <c>stop</c>) silently and waits
    /// for it to complete.
    /// </summary>
    private static void AssertSilentWaitingCall(string arguments, string action)
    {
        Assert.StartsWith(action + " ", arguments, StringComparison.Ordinal);
        Assert.Matches(Silent, arguments);
        Assert.Matches(Wait, arguments);
    }

    /// <summary>Mock gate positive control: silent, waiting start and stop strings pass the assertion.</summary>
    [Fact]
    public void ScriptedSilentCalls_PassTheAssertion()
    {
        AssertSilentWaitingCall("start -D \"C:\\data\" -w -s -o \"-p 5432 -c listen_addresses=localhost\" -l \"C:\\data\\pg.log\"", "start");
        AssertSilentWaitingCall("stop -D \"C:\\data\" -m immediate -w -s", "stop");
    }

    /// <summary>
    /// Mock gate negative controls: the start and stop strings the fixture used before (no <c>-s</c>), a silent call
    /// that does not wait, a silent stop asserted as a start, and an <c>-s</c> that only appears inside the quoted
    /// server options are each rejected, inside a passing harness.
    /// </summary>
    [Fact]
    public void LoudOrNonWaitingCalls_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertSilentWaitingCall("start -D \"C:\\data\" -w -o \"-p 5432 -c listen_addresses=localhost\" -l \"C:\\data\\pg.log\"", "start")));
        Assert.NotNull(Record.Exception(() => AssertSilentWaitingCall("stop -D \"C:\\data\" -m immediate -w", "stop")));
        Assert.NotNull(Record.Exception(() => AssertSilentWaitingCall("stop -D \"C:\\data\" -m immediate -s", "stop")));
        Assert.NotNull(Record.Exception(() => AssertSilentWaitingCall("stop -D \"C:\\data\" -m immediate -w -s", "start")));
        Assert.NotNull(Record.Exception(() => AssertSilentWaitingCall("start -D \"C:\\data\" -w -o \"-p 5432 -s\" -l \"C:\\data\\pg.log\"", "start")));
    }

    /// <summary>
    /// Binding: the fixture's own start and stop argument builders pass the same assertion, and the start arguments
    /// keep the port, the localhost listener and the server log file.
    /// </summary>
    [Fact]
    public void FixtureCalls_AreSilentAndWait()
    {
        var start = EphemeralPostgresFixture.PgCtlStartArguments(@"C:\data", 5432, @"C:\data\pg.log");
        AssertSilentWaitingCall(start, "start");
        Assert.Contains("-o \"-p 5432 -c listen_addresses=localhost\"", start, StringComparison.Ordinal);
        Assert.Contains("-l \"C:\\data\\pg.log\"", start, StringComparison.Ordinal);
        AssertSilentWaitingCall(EphemeralPostgresFixture.PgCtlStopArguments(@"C:\data"), "stop");
    }
}
