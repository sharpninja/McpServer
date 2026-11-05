namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-SERVICEUPDATE-001 for TR-MCP-SERVICEUPDATE-001 criterion failed-update-startable: the rows the recovery of a
/// Linux update that fails after the files were replaced must meet, proven against an independent reference recovery
/// and against wrong recoveries before the production failure path changes (BDPv4 mock gate). The same rows bind to the
/// production update in <see cref="ServiceUpdateTests"/>.
/// </summary>
/// <remarks>
/// <list type="table">
/// <item><term>2026-11-04</term><description>TR-MCP-SERVICEUPDATE-001 criterion failed-update-startable (slice Z3,
/// fifth holistic read before hostile validation round 12, finding d3): the redeploy of 2026-11-04 failed after the
/// replacement, and its recovery restored a unit file that still named the removed legacy apphost, so the service could
/// not start until a manual retarget. These rows state the recovery the criterion needs: restore the preserved state,
/// then retarget a unit file or drop-in that names the legacy apphost and reload systemd after that change; a unit that
/// already names the deployed apphost is restored unchanged with no extra reload.</description></item>
/// </list>
/// </remarks>
public sealed class ServiceRecoveryContractTests
{
    /// <summary>The legacy apphost name a 1.x unit may still run.</summary>
    internal const string Legacy = "McpServer.Support.Mcp";

    /// <summary>The apphost the completed replacement puts in the install.</summary>
    internal const string Current = "QBrainAi.Support.Mcp";

    /// <summary>The install folder the contract texts name; the bindings put the fixture's install in its place.</summary>
    internal const string InstallToken = "/srv/app/";

    /// <summary>Where the update fails; both points come after the replacement completed.</summary>
    internal enum FailurePoint
    {
        /// <summary>The runtime configuration copy after the replacement, before the success path restores and retargets.</summary>
        RuntimeConfiguration,

        /// <summary>The health check after the start, after the success path has retargeted and reloaded.</summary>
        Health,
    }

    /// <summary>A unit file that runs the legacy apphost.</summary>
    internal const string FragmentLegacy = "[Service]\nExecStart=" + InstallToken + Legacy + " --urls http://localhost:7147\n";

    /// <summary>The same unit file retargeted at the deployed apphost.</summary>
    internal const string FragmentRetargeted = "[Service]\nExecStart=" + InstallToken + Current + " --urls http://localhost:7147\n";

    /// <summary>A unit file that already runs the deployed apphost.</summary>
    internal const string FragmentCurrent = "[Service]\nExecStart=" + InstallToken + Current + "\n";

    /// <summary>A unit file whose ExecStart a drop-in overrides.</summary>
    internal const string FragmentOverridden = "[Service]\nExecStart=/usr/bin/true\n";

    /// <summary>A drop-in that runs the legacy apphost.</summary>
    internal const string DropInLegacy = "[Service]\nExecStart=\nExecStart=" + InstallToken + Legacy + "\n";

    /// <summary>The same drop-in retargeted at the deployed apphost.</summary>
    internal const string DropInRetargeted = "[Service]\nExecStart=\nExecStart=" + InstallToken + Current + "\n";

    /// <summary>One contract row: the archived unit texts, whether the unit's effective ExecStart names the legacy apphost,
    /// where the update fails, and the unit texts and recovery events the criterion requires after the failure.</summary>
    internal sealed record Row(
        string Name,
        FailurePoint Failure,
        bool NamesLegacy,
        string ArchivedFragment,
        string? ArchivedDropIn,
        string ExpectedFragment,
        string? ExpectedDropIn,
        string[] ExpectedRecovery);

    /// <summary>
    /// The contract rows. Expected texts and events come from the criterion: recovery stops the unit, restores the archive,
    /// and only when the unit names the legacy apphost retargets the files that hold that name and then reloads systemd.
    /// </summary>
    internal static readonly Row[] Rows =
    [
        new("runtime configuration fails, legacy unit file", FailurePoint.RuntimeConfiguration, true,
            FragmentLegacy, null, FragmentRetargeted, null, ["stop", "restore", "daemon-reload"]),
        new("health fails, legacy unit file", FailurePoint.Health, true,
            FragmentLegacy, null, FragmentRetargeted, null, ["stop", "restore", "daemon-reload"]),
        new("health fails, legacy drop-in", FailurePoint.Health, true,
            FragmentOverridden, DropInLegacy, FragmentOverridden, DropInRetargeted, ["stop", "restore", "daemon-reload"]),
        new("runtime configuration fails, legacy unit file and drop-in", FailurePoint.RuntimeConfiguration, true,
            FragmentLegacy, DropInLegacy, FragmentRetargeted, DropInRetargeted, ["stop", "restore", "daemon-reload"]),
        new("health fails, current unit file", FailurePoint.Health, false,
            FragmentCurrent, null, FragmentCurrent, null, ["stop", "restore"]),
        new("runtime configuration fails, current unit file", FailurePoint.RuntimeConfiguration, false,
            FragmentCurrent, null, FragmentCurrent, null, ["stop", "restore"]),
    ];

    /// <summary>The unit texts on disk when the failure happens and the texts the archive holds.</summary>
    /// <param name="Fragment">The unit file text.</param>
    /// <param name="DropIn">The drop-in text, or null when the unit has none.</param>
    internal sealed record UnitTexts(string Fragment, string? DropIn);

    /// <summary>What a recovery leaves: the final unit texts and the recovery events in order.</summary>
    /// <param name="Texts">The unit texts after the recovery.</param>
    /// <param name="Events">The recovery events, starting with the stop.</param>
    internal sealed record RecoveryOutcome(UnitTexts Texts, IReadOnlyList<string> Events);

    /// <summary>A recovery as the contract sees it: the texts at the failure, the archived texts and whether the unit
    /// names the legacy apphost.</summary>
    internal delegate RecoveryOutcome Recovery(UnitTexts atFailure, UnitTexts archived, bool namesLegacy);

    /// <summary>The texts on disk when the update fails: the success path has retargeted a legacy unit before the health
    /// check, never before the runtime configuration copy.</summary>
    /// <param name="row">The contract row.</param>
    /// <returns>The texts at the failure.</returns>
    internal static UnitTexts AtFailure(Row row)
        => row.Failure == FailurePoint.Health && row.NamesLegacy
            ? new(row.ArchivedFragment.Replace(Legacy, Current, StringComparison.Ordinal), row.ArchivedDropIn?.Replace(Legacy, Current, StringComparison.Ordinal))
            : new(row.ArchivedFragment, row.ArchivedDropIn);

    /// <summary>Independent reference recovery: stop, restore the archive, then retarget the legacy name and reload only
    /// when a file changed.</summary>
    private static RecoveryOutcome ReferenceRecovery(UnitTexts atFailure, UnitTexts archived, bool namesLegacy)
    {
        var events = new List<string> { "stop", "restore" };
        var fragment = archived.Fragment;
        var dropIn = archived.DropIn;
        if (namesLegacy)
        {
            var newFragment = fragment.Replace(Legacy, Current, StringComparison.Ordinal);
            var newDropIn = dropIn?.Replace(Legacy, Current, StringComparison.Ordinal);
            if (newFragment != fragment || newDropIn != dropIn) events.Add("daemon-reload");
            fragment = newFragment;
            dropIn = newDropIn;
        }
        return new(new(fragment, dropIn), events);
    }

    /// <summary>Wrong recoveries the rows must reject; the first is the failure path the redeploy met.</summary>
    internal static readonly (string Name, Recovery Recovery)[] WrongRecoveries =
    [
        ("restores and stops there", (_, archived, _) => new(archived, ["stop", "restore"])),
        ("retargets but never reloads", (atFailure, archived, legacy) =>
        {
            var outcome = ReferenceRecovery(atFailure, archived, legacy);
            return new(outcome.Texts, outcome.Events.Where(item => item != "daemon-reload").ToArray());
        }),
        ("retargets before the restore", (atFailure, archived, legacy) =>
        {
            var events = new List<string> { "stop" };
            if (legacy) events.Add("daemon-reload");
            events.Add("restore");
            return new(archived, events);
        }),
        ("retargets only the unit file", (atFailure, archived, legacy) =>
        {
            if (!legacy) return new(archived, ["stop", "restore"]);
            var fragment = archived.Fragment.Replace(Legacy, Current, StringComparison.Ordinal);
            var events = fragment != archived.Fragment ? new[] { "stop", "restore", "daemon-reload" } : new[] { "stop", "restore" };
            return new(new(fragment, archived.DropIn), events);
        }),
        ("reloads even when nothing changed", (atFailure, archived, legacy) =>
        {
            var outcome = ReferenceRecovery(atFailure, archived, legacy);
            return outcome.Events.Contains("daemon-reload") ? outcome : new(outcome.Texts, ["stop", "restore", "daemon-reload"]);
        }),
        ("skips the restore", (atFailure, _, legacy) =>
        {
            var fragment = legacy ? atFailure.Fragment.Replace(Legacy, Current, StringComparison.Ordinal) : atFailure.Fragment;
            var dropIn = legacy ? atFailure.DropIn?.Replace(Legacy, Current, StringComparison.Ordinal) : atFailure.DropIn;
            var events = legacy ? new[] { "stop", "daemon-reload" } : new[] { "stop" };
            return new(new(fragment, dropIn), events);
        }),
    ];

    /// <summary>True when a recovery leaves exactly the row's expected unit texts and recovery events.</summary>
    /// <param name="recovery">The recovery under test.</param>
    /// <param name="row">The contract row.</param>
    /// <returns>Whether the recovery meets the row.</returns>
    internal static bool Meets(Recovery recovery, Row row)
    {
        var outcome = recovery(AtFailure(row), new(row.ArchivedFragment, row.ArchivedDropIn), row.NamesLegacy);
        return string.Equals(outcome.Texts.Fragment, row.ExpectedFragment, StringComparison.Ordinal)
            && string.Equals(outcome.Texts.DropIn, row.ExpectedDropIn, StringComparison.Ordinal)
            && outcome.Events.SequenceEqual(row.ExpectedRecovery, StringComparer.Ordinal);
    }

    /// <summary>The reference recovery meets every contract row (rows: <see cref="Rows"/>).</summary>
    [Fact]
    public void ReferenceRecovery_MeetsEveryRow()
    {
        foreach (var row in Rows)
            Assert.True(Meets(ReferenceRecovery, row), row.Name);
    }

    /// <summary>Each wrong recovery fails at least one contract row, so the rows discriminate (negative controls).</summary>
    /// <param name="index">The wrong recovery's position in <see cref="WrongRecoveries"/>.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void WrongRecovery_FailsAtLeastOneRow(int index)
    {
        var (name, recovery) = WrongRecoveries[index];
        Assert.Contains(Rows, row => !Meets(recovery, row));
        Assert.False(string.IsNullOrEmpty(name));
    }

    /// <summary>The failure path the redeploy met fails exactly the rows whose unit names the legacy apphost.</summary>
    [Fact]
    public void OldRecovery_FailsOnlyTheLegacyRows()
    {
        var failed = Rows.Where(row => !Meets(WrongRecoveries[0].Recovery, row)).Select(row => row.Name).ToArray();
        Assert.Equal(Rows.Where(row => row.NamesLegacy).Select(row => row.Name).ToArray(), failed);
    }
}
