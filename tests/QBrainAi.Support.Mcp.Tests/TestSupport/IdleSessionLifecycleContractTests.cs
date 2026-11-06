using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Tests the contract an idle-session pool with a background cleanup is bound to (TR-MCP-USECASE-018 AC-006, appended
/// 2026-11-06, "This includes an age, expiry or staleness decision that a background callback in the code under test
/// makes"): two creates for the same agent and workspace reuse one session while its scripted idle age is eligible,
/// including across a cleanup the test triggers between them; no cleanup runs unless the test triggers it, even when
/// the scripted clock has passed the threshold; a triggered cleanup keeps a session exactly at the strict threshold and
/// removes it one step past it; and a create after the removal gives a new session. The trace is run at a reference far
/// before and one far after the real clock, so it gives the same result whatever the real time or elapsed time is.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-USECASE-018 AC-006 (and TR-MCP-USECASE-019 AC-008, which states these rules once with
/// it). Test data: the two references of <see cref="AgeBoundaryContractTests.References"/>, a 15-minute idle timeout and
/// a one-second step. The mock gate runs the trace against an independent scripted pool and rejects, inside a passing
/// harness, a pool whose cleanup reads a clock other than the scripted one, a pool whose cleanup runs on its own when
/// time passes (uncontrolled scheduling), and a pool whose cleanup compares inclusively where the contract is strict.
/// VoiceConversationServiceTests then binds the same trace to the production service through
/// <see cref="AssertLifecycleAsync"/>. No case reads the system clock or waits.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "This includes an age, expiry or staleness decision that a background callback in the code under test makes"): added because hostile validation round 13 finding R13-F1 found that the voice-session reuse unit test depended on a real cleanup timer and the live clock; the reviewer proposed the tests VoiceSessionReuse_DoesNotDependOnWallClockOrCleanupScheduling and IdleCleanup_UsesScriptedAgeBoundary, with negative controls for a live clock, uncontrolled cleanup scheduling and an inclusive comparison.</description></item>
/// </list>
/// </remarks>
public sealed class IdleSessionLifecycleContractTests
{
    /// <summary>The idle timeout the trace uses; the production binding configures the same value.</summary>
    internal static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(15);

    /// <summary>The instant the "another clock" negative control reads instead of the scripted clock.</summary>
    private static readonly DateTimeOffset AnotherClock = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A pool under the contract, driven by the trace: the test sets the scripted clock, creates sessions for one fixed
    /// agent and workspace, checks whether a session still exists, and triggers the background cleanup.
    /// </summary>
    internal interface IIdleSessionPool : IDisposable
    {
        /// <summary>Sets the scripted clock every later step reads.</summary>
        /// <param name="utcNow">The instant, with a zero offset.</param>
        void SetClock(DateTimeOffset utcNow);

        /// <summary>Creates (or reuses) the session for the fixed agent and workspace.</summary>
        /// <returns>The session id.</returns>
        Task<string> CreateAsync();

        /// <summary>Whether the session still exists, read without refreshing its idle age.</summary>
        /// <param name="sessionId">The session id.</param>
        /// <returns>True when it exists.</returns>
        Task<bool> ExistsAsync(string sessionId);

        /// <summary>Runs the background cleanup once, now.</summary>
        void TriggerCleanup();
    }

    /// <summary>
    /// Runs the lifecycle trace at both references against pools from <paramref name="createPool"/>, asserting every
    /// step. A pool is created per reference with its clock at that reference.
    /// </summary>
    /// <param name="createPool">Creates a pool whose scripted clock starts at the given reference.</param>
    /// <returns>A task that completes when every step has been asserted.</returns>
    internal static async Task AssertLifecycleAsync(Func<DateTimeOffset, IIdleSessionPool> createPool)
    {
        ArgumentNullException.ThrowIfNull(createPool);
        var step = AgeBoundaryContractTests.Step;
        foreach (var reference in AgeBoundaryContractTests.References)
        {
            using var pool = createPool(reference);
            pool.SetClock(reference);
            var first = await pool.CreateAsync().ConfigureAwait(true);

            pool.SetClock(reference + IdleTimeout);
            pool.TriggerCleanup();
            Assert.True(await pool.ExistsAsync(first).ConfigureAwait(true), $"At {reference:O}: a session exactly at the idle threshold is kept by a triggered cleanup (the comparison is strict).");
            var second = await pool.CreateAsync().ConfigureAwait(true);
            Assert.True(first == second, $"At {reference:O}: the second create reuses the idle session across an interleaved cleanup.");

            var pastThreshold = reference + IdleTimeout + IdleTimeout + step;
            pool.SetClock(pastThreshold);
            Assert.True(await pool.ExistsAsync(first).ConfigureAwait(true), $"At {reference:O}: no cleanup runs unless the test triggers it, even past the threshold.");
            var third = await pool.CreateAsync().ConfigureAwait(true);
            Assert.True(first == third, $"At {reference:O}: a create past the threshold reuses the session because no cleanup ran.");

            pool.SetClock(pastThreshold + IdleTimeout + step);
            pool.TriggerCleanup();
            Assert.False(await pool.ExistsAsync(first).ConfigureAwait(true), $"At {reference:O}: a triggered cleanup removes a session one step past the idle threshold.");
            var fourth = await pool.CreateAsync().ConfigureAwait(true);
            Assert.True(first != fourth, $"At {reference:O}: a create after the removal gives a new session.");
        }
    }

    /// <summary>Mock gate positive control: the independent scripted pool satisfies the trace.</summary>
    /// <returns>A task that completes when the trace finishes.</returns>
    [Fact]
    public Task ScriptedPool_SatisfiesTheContract() =>
        AssertLifecycleAsync(reference => new ScriptedPool(reference, ScriptedPool.CleanupClock.Scripted, selfScheduling: false, inclusive: false));

    /// <summary>Mock gate negative control: a pool whose cleanup reads a clock other than the scripted one is rejected.</summary>
    /// <returns>A task that completes when the rejection is observed.</returns>
    [Fact]
    public Task PoolOnAnotherClock_IsRejected() =>
        Assert.ThrowsAnyAsync<XunitException>(() =>
            AssertLifecycleAsync(reference => new ScriptedPool(reference, ScriptedPool.CleanupClock.Another, selfScheduling: false, inclusive: false)));

    /// <summary>Mock gate negative control: a pool whose cleanup runs on its own when time passes is rejected.</summary>
    /// <returns>A task that completes when the rejection is observed.</returns>
    [Fact]
    public Task PoolWithUncontrolledCleanupScheduling_IsRejected() =>
        Assert.ThrowsAnyAsync<XunitException>(() =>
            AssertLifecycleAsync(reference => new ScriptedPool(reference, ScriptedPool.CleanupClock.Scripted, selfScheduling: true, inclusive: false)));

    /// <summary>Mock gate negative control: a pool whose cleanup compares inclusively (removes at the threshold) is rejected.</summary>
    /// <returns>A task that completes when the rejection is observed.</returns>
    [Fact]
    public Task PoolWithAnInclusiveComparison_IsRejected() =>
        Assert.ThrowsAnyAsync<XunitException>(() =>
            AssertLifecycleAsync(reference => new ScriptedPool(reference, ScriptedPool.CleanupClock.Scripted, selfScheduling: false, inclusive: true)));

    /// <summary>Mock gate negative control: a pool that never reuses a session is rejected.</summary>
    /// <returns>A task that completes when the rejection is observed.</returns>
    [Fact]
    public Task PoolThatNeverReuses_IsRejected() =>
        Assert.ThrowsAnyAsync<XunitException>(() =>
            AssertLifecycleAsync(reference => new ScriptedPool(reference, ScriptedPool.CleanupClock.Scripted, selfScheduling: false, inclusive: false, reuse: false)));

    /// <summary>
    /// The independent scripted pool: one session per create unless an existing one is reused; a create or reuse records
    /// the scripted instant; a cleanup removes every session whose idle age is more than the timeout. The controls change
    /// one property each: the clock the cleanup reads, whether the cleanup runs on its own whenever the clock moves, the
    /// comparison, and whether creates reuse.
    /// </summary>
    private sealed class ScriptedPool : IIdleSessionPool
    {
        /// <summary>The recorded idle time of each session.</summary>
        private readonly Dictionary<string, DateTimeOffset> _sessions = new(StringComparer.Ordinal);

        /// <summary>Which clock the cleanup reads.</summary>
        private readonly CleanupClock _cleanupClock;

        /// <summary>Whether the cleanup runs on its own whenever the clock moves.</summary>
        private readonly bool _selfScheduling;

        /// <summary>Whether the cleanup removes a session exactly at the threshold.</summary>
        private readonly bool _inclusive;

        /// <summary>Whether a create reuses an existing session.</summary>
        private readonly bool _reuse;

        /// <summary>The scripted instant.</summary>
        private DateTimeOffset _now;

        /// <summary>The number of sessions created, for new ids.</summary>
        private int _created;

        /// <summary>Creates the pool with its clock at <paramref name="start"/>.</summary>
        /// <param name="start">The scripted start instant.</param>
        /// <param name="cleanupClock">The clock the cleanup reads.</param>
        /// <param name="selfScheduling">Whether the cleanup runs on its own whenever the clock moves.</param>
        /// <param name="inclusive">Whether the cleanup removes a session exactly at the threshold.</param>
        /// <param name="reuse">Whether a create reuses an existing session.</param>
        internal ScriptedPool(DateTimeOffset start, CleanupClock cleanupClock, bool selfScheduling, bool inclusive, bool reuse = true)
        {
            _now = start;
            _cleanupClock = cleanupClock;
            _selfScheduling = selfScheduling;
            _inclusive = inclusive;
            _reuse = reuse;
        }

        /// <summary>The clock a cleanup reads.</summary>
        internal enum CleanupClock
        {
            /// <summary>The scripted clock.</summary>
            Scripted,

            /// <summary>A fixed clock the test does not control.</summary>
            Another,
        }

        /// <inheritdoc />
        public void SetClock(DateTimeOffset utcNow)
        {
            _now = utcNow;
            if (_selfScheduling)
                TriggerCleanup();
        }

        /// <inheritdoc />
        public Task<string> CreateAsync()
        {
            if (_reuse && _sessions.Count > 0)
            {
                var existing = _sessions.Keys.First();
                _sessions[existing] = _now;
                return Task.FromResult(existing);
            }

            _created++;
            var id = $"session-{_created}";
            _sessions[id] = _now;
            return Task.FromResult(id);
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string sessionId) => Task.FromResult(_sessions.ContainsKey(sessionId));

        /// <inheritdoc />
        public void TriggerCleanup()
        {
            var now = _cleanupClock == CleanupClock.Scripted ? _now : AnotherClock;
            foreach (var (id, recorded) in _sessions.ToArray())
            {
                var age = now - recorded;
                if (_inclusive ? age >= IdleTimeout : age > IdleTimeout)
                    _sessions.Remove(id);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
