namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-SERVICEUPDATE-001 for TR-MCP-SERVICEUPDATE-001 criterion startup-health-wait: the rows a Linux update's wait
/// for HTTP health after the start must meet, proven against an independent reference wait and against wrong waits
/// before the production wait changes (BDPv4 mock gate). The unit state, the health probe and the sleep are scripted,
/// so no row waits on real time. The same rows bind to the production wait in <see cref="ServiceUpdateTests"/>.
/// </summary>
/// <remarks>
/// <list type="table">
/// <item><term>2026-11-04</term><description>TR-MCP-SERVICEUPDATE-001 criteria startup-health-wait and lifecycle (slice Z3):
/// the redeploy of 2026-11-04 stopped a server that was starting normally, because the update probed health 10 times 2 s
/// apart while the server registers every workspace before it listens (93 s at 14:27). These rows state the wait the
/// criterion needs: probe while the unit stays active, up to the allowed probes; stop at once when the unit leaves the
/// active state; never stop a unit that answers within the allowed probes.</description></item>
/// </list>
/// </remarks>
public sealed class ServiceStartupHealthContractTests
{
    /// <summary>The outcomes a wait reports, by name.</summary>
    internal const string Healthy = "Healthy";

    /// <summary>The unit left the active state before it answered health.</summary>
    internal const string UnitStopped = "UnitStopped";

    /// <summary>The allowed probes were spent without a healthy answer.</summary>
    internal const string TimedOut = "TimedOut";

    /// <summary>
    /// One contract row: the unit states and probe answers the scripts return in order (the last one repeats), the
    /// allowed probes, and the outcome, probe count and sleep count the criterion requires.
    /// </summary>
    internal sealed record Row(string Name, string[] States, bool[] Answers, int MaxProbes, string Outcome, int Probes, int Sleeps);

    /// <summary>A wait as the contract sees it: it returns the outcome name and the probes it made.</summary>
    internal delegate (string Outcome, int Probes) StartupWait(Func<string> activeState, Func<bool> probe, Action<TimeSpan> sleep, int maxProbes, TimeSpan interval);

    /// <summary>The contract rows; expected values come from the criterion, never from a wait under test.</summary>
    internal static readonly Row[] Rows =
    [
        new("healthy at once", ["active"], [true], 120, Healthy, 1, 0),
        new("a server that answers on the 19th probe (95 s at 5 s)", ["active"], [false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true], 120, Healthy, 19, 18),
        new("the unit fails while starting", ["active", "active", "failed"], [false], 120, UnitStopped, 2, 2),
        new("the unit is inactive before the first probe", ["inactive"], [false], 120, UnitStopped, 0, 0),
        new("never healthy", ["active"], [false], 3, TimedOut, 3, 2),
        new("healthy on the last allowed probe", ["active"], [false, false, true], 3, Healthy, 3, 2),
    ];

    /// <summary>Independent reference wait: a bounded for loop that checks the unit before each probe.</summary>
    private static (string Outcome, int Probes) ReferenceWait(Func<string> activeState, Func<bool> probe, Action<TimeSpan> sleep, int maxProbes, TimeSpan interval)
    {
        for (var made = 0; made < maxProbes; made++)
        {
            if (!string.Equals(activeState(), "active", StringComparison.Ordinal)) return (UnitStopped, made);
            if (probe()) return (Healthy, made + 1);
            if (made + 1 < maxProbes) sleep(interval);
        }
        return (TimedOut, maxProbes);
    }

    /// <summary>Wrong waits each row set must reject; the first is the window the redeploy met.</summary>
    internal static readonly (string Name, StartupWait Wait)[] WrongWaits =
    [
        ("gives up after 10 probes", (state, probe, sleep, max, interval) => ReferenceWait(state, probe, sleep, Math.Min(max, 10), interval)),
        ("ignores the unit state", (_, probe, sleep, max, interval) => ReferenceWait(() => "active", probe, sleep, max, interval)),
        ("checks the unit only before the first probe", (state, probe, sleep, max, interval) =>
        {
            var first = state();
            return ReferenceWait(() => first, probe, sleep, max, interval);
        }),
        ("sleeps after the last probe too", (state, probe, sleep, max, interval) =>
        {
            var result = ReferenceWait(state, probe, sleep, max, interval);
            if (result.Item1 == TimedOut) sleep(interval);
            return result;
        }),
        ("probes before it checks the unit", (state, probe, sleep, max, interval) =>
        {
            var made = 0;
            while (made < max)
            {
                var answer = probe();
                made++;
                if (!string.Equals(state(), "active", StringComparison.Ordinal)) return (UnitStopped, made);
                if (answer) return (Healthy, made);
                if (made < max) sleep(interval);
            }
            return (TimedOut, max);
        }),
    ];

    /// <summary>Runs a wait over a row's scripts and reports whether the outcome, probes and sleeps all match.</summary>
    internal static bool Meets(StartupWait wait, Row row)
    {
        var stateIndex = 0;
        var probeCalls = 0;
        var sleeps = 0;
        string State() => row.States[Math.Min(stateIndex++, row.States.Length - 1)];
        bool Probe() => row.Answers[Math.Min(probeCalls++, row.Answers.Length - 1)];
        var (outcome, probes) = wait(State, Probe, _ => sleeps++, row.MaxProbes, TimeSpan.FromSeconds(5));
        return outcome == row.Outcome && probes == row.Probes && probeCalls == row.Probes && sleeps == row.Sleeps;
    }

    /// <summary>The reference wait meets every contract row (rows: <see cref="Rows"/>).</summary>
    [Fact]
    public void ReferenceWait_MeetsEveryRow()
    {
        foreach (var row in Rows)
            Assert.True(Meets(ReferenceWait, row), row.Name);
    }

    /// <summary>Each wrong wait fails at least one contract row, so the rows discriminate (negative controls).</summary>
    /// <param name="index">The wrong wait's position in <see cref="WrongWaits"/>.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void WrongWait_FailsAtLeastOneRow(int index)
    {
        var (name, wait) = WrongWaits[index];
        Assert.False(string.IsNullOrEmpty(name));
        Assert.Contains(Rows, row => !Meets(wait, row));
    }

    /// <summary>The 10-probe window the redeploy met fails exactly the row of a server that needs 19 probes.</summary>
    [Fact]
    public void OldWindow_FailsOnlyTheSlowStartRow()
    {
        var failed = Rows.Where(row => !Meets(WrongWaits[0].Wait, row)).Select(row => row.Name).ToArray();
        Assert.Equal(new[] { "a server that answers on the 19th probe (95 s at 5 s)" }, failed);
    }
}
