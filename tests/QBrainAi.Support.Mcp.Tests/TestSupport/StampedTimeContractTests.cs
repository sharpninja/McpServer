using System.Globalization;
using Xunit;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006 (appended "This covers every recorded time a unit test checks, including the creation and
/// update times a service stamps on a record it stores: the test supplies the service's clock as scripted times and
/// asserts that each stamp equals the scripted time the service read"), BDPv4 mock gate: a service that stores a record
/// stamps it from the clock it is given. Creating the record reads the clock once and sets both its creation and its
/// update time to that reading; each update reads the clock once and sets only the update time, keeping the creation
/// time. Fixed rows and an independent scripted reference stamper over <see cref="ScriptedTimeProvider"/>; wrong
/// stampers (one that reads the system clock, an update that keeps the old update time, an update that also rewrites
/// the creation time, and creation and update times swapped) are rejected by the same assertions inside this passing
/// harness. No real clock is read and nothing sleeps.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (appended "This covers every recorded time a unit test checks"): added the stamped-time contract, because the sibling sweep before round 11 of hostile validation round 10 finding R10-F3 found GraphRagEntityCrudTests comparing entity times with the live clock and sleeping to separate them, and MemoryModelTests sleeping to separate memory times (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// </list>
/// </remarks>
public sealed class StampedTimeContractTests
{
    /// <summary>The scripted creation time: a fixed instant in the past, so no row depends on the date it runs.</summary>
    internal static readonly DateTimeOffset Created = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The scripted time of the first update.</summary>
    internal static readonly DateTimeOffset FirstUpdate = Created.AddMinutes(5);

    /// <summary>The scripted time of the second update.</summary>
    internal static readonly DateTimeOffset SecondUpdate = Created.AddMinutes(9);

    /// <summary>
    /// The rows: a name, the number of updates after the create, the clock's script (one time per read), and the
    /// creation and update times the stored record must then hold.
    /// </summary>
    internal static readonly (string Name, int Updates, DateTimeOffset[] Script, DateTimeOffset ExpectedCreated, DateTimeOffset ExpectedUpdated)[] Rows =
    [
        ("create", 0, [Created], Created, Created),
        ("create, then update", 1, [Created, FirstUpdate], Created, FirstUpdate),
        ("create, then two updates", 2, [Created, FirstUpdate, SecondUpdate], Created, SecondUpdate),
    ];

    /// <summary>The scripted reference stamper satisfies every row.</summary>
    /// <returns>The test task.</returns>
    [Fact]
    public Task ScriptedReferenceStamper_SatisfiesEveryRow() => AssertStampsAsync(ScriptedReferenceAsync);

    /// <summary>Each wrong stamper fails the same assertions.</summary>
    /// <param name="fault">The deliberate fault.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData("system clock")]
    [InlineData("update keeps the old update time")]
    [InlineData("update rewrites the creation time")]
    [InlineData("swapped")]
    public Task WrongStampers_AreRejected(string fault) =>
        Assert.ThrowsAnyAsync<XunitException>(() => AssertStampsAsync((clock, updates) => WrongStamperAsync(fault, clock, updates)));

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006: asserts every row against the code under test. For each row a new
    /// <see cref="ScriptedTimeProvider"/> holds the row's script; <paramref name="subject"/> creates one record with that
    /// clock, updates it the row's number of times, and returns the record's stored creation and update times, which
    /// must equal the row's times exactly (instant and offset), with the clock read exactly once per scripted time.
    /// </summary>
    /// <param name="subject">The code under test: the clock and the number of updates in, the stored creation and update times out.</param>
    /// <returns>The assertion task.</returns>
    internal static async Task AssertStampsAsync(Func<TimeProvider, int, Task<(DateTimeOffset Created, DateTimeOffset Updated)>> subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        foreach (var row in Rows)
        {
            var clock = new ScriptedTimeProvider(row.Script);
            var (created, updated) = await subject(clock, row.Updates).ConfigureAwait(true);
            Assert.True(Exact(row.ExpectedCreated) == Exact(created), $"{row.Name}: creation time '{Exact(created)}', expected '{Exact(row.ExpectedCreated)}'");
            Assert.True(Exact(row.ExpectedUpdated) == Exact(updated), $"{row.Name}: update time '{Exact(updated)}', expected '{Exact(row.ExpectedUpdated)}'");
            Assert.True(clock.Reads == row.Script.Length, $"{row.Name}: the clock was read {clock.Reads} times, {row.Script.Length} expected");
        }
    }

    /// <summary>TR-MCP-USECASE-018 AC-006: a UTC <see cref="DateTime"/> stamp as an instant with a zero offset.</summary>
    /// <param name="value">The stamp, stored as UTC.</param>
    /// <returns>The instant.</returns>
    internal static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    /// <summary>TR-MCP-USECASE-018 AC-006: the round-trip text of a time, so instant and offset are compared together.</summary>
    /// <param name="value">The time.</param>
    /// <returns>The round-trip text.</returns>
    private static string Exact(DateTimeOffset value) => value.ToString("o", CultureInfo.InvariantCulture);

    /// <summary>TR-MCP-USECASE-018 AC-006 scripted reference: stamps an in-memory record from the clock it is given.</summary>
    /// <param name="clock">The clock.</param>
    /// <param name="updates">The number of updates after the create.</param>
    /// <returns>The record's creation and update times.</returns>
    private static Task<(DateTimeOffset Created, DateTimeOffset Updated)> ScriptedReferenceAsync(TimeProvider clock, int updates)
    {
        var created = clock.GetUtcNow();
        var updated = created;
        for (var update = 0; update < updates; update++)
            updated = clock.GetUtcNow();
        return Task.FromResult((created, updated));
    }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 wrong stampers: the system clock, an update that keeps the old update time, an update
    /// that also rewrites the creation time, and creation and update times swapped.
    /// </summary>
    /// <param name="fault">The deliberate fault.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="updates">The number of updates after the create.</param>
    /// <returns>The wrong creation and update times.</returns>
    private static Task<(DateTimeOffset Created, DateTimeOffset Updated)> WrongStamperAsync(string fault, TimeProvider clock, int updates)
    {
        switch (fault)
        {
            case "system clock":
            {
                var now = TimeProvider.System.GetUtcNow();
                return Task.FromResult((now, now));
            }

            case "update keeps the old update time":
            {
                var created = clock.GetUtcNow();
                for (var update = 0; update < updates; update++)
                    _ = clock.GetUtcNow();
                return Task.FromResult((created, created));
            }

            case "update rewrites the creation time":
            {
                var created = clock.GetUtcNow();
                var updated = created;
                for (var update = 0; update < updates; update++)
                {
                    updated = clock.GetUtcNow();
                    created = updated;
                }

                return Task.FromResult((created, updated));
            }

            case "swapped":
            {
                var created = clock.GetUtcNow();
                var updated = created;
                for (var update = 0; update < updates; update++)
                    updated = clock.GetUtcNow();
                return Task.FromResult((updated, created));
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(fault));
        }
    }
}
