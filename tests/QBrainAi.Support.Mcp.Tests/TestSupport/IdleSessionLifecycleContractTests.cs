using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Tests the contract an idle-session pool with a background cleanup is bound to (TR-MCP-USECASE-018 AC-006, appended
/// 2026-11-06, "This includes an age, expiry or staleness decision that a background callback in the code under test
/// makes"): two creates for the same agent and workspace reuse one session while its scripted idle age is eligible,
/// including across a cleanup the test triggers between them; no cleanup runs unless the test triggers it, even when
/// the scripted clock has passed the threshold; a triggered cleanup keeps a session exactly at the strict threshold and
/// removes it one step past it; and a create after the removal gives a new session. Each keep assertion after a trigger
/// is read only once the same triggered pass is seen to have run to its end: a sentinel session for a second agent and
/// workspace, one step older, is already removed. The trace is run at a reference far before and one far after the real
/// clock, so it gives the same result whatever the real time or elapsed time is.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-USECASE-018 AC-006 (and TR-MCP-USECASE-019 AC-008, which states these rules once with
/// it). Test data: the two references of <see cref="AgeBoundaryContractTests.References"/>, a 15-minute idle timeout and
/// a one-second step. The mock gate runs the trace against an independent scripted pool and rejects, inside a passing
/// harness, a pool whose cleanup reads a clock other than the scripted one, a pool whose cleanup runs on its own when
/// time passes (uncontrolled scheduling), a pool whose cleanup compares inclusively where the contract is strict, a pool
/// that never reuses, and a pool whose triggered pass runs only at the next trigger with an inclusive comparison (a pass
/// that finishes later). VoiceConversationServiceTests then binds the same trace to the production service through
/// <see cref="AssertLifecycleAsync"/>. No case reads the system clock or waits.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "This includes an age, expiry or staleness decision that a background callback in the code under test makes"): added because hostile validation round 13 finding R13-F1 found that the voice-session reuse unit test depended on a real cleanup timer and the live clock; the reviewer proposed the tests VoiceSessionReuse_DoesNotDependOnWallClockOrCleanupScheduling and IdleCleanup_UsesScriptedAgeBoundary, with negative controls for a live clock, uncontrolled cleanup scheduling and an inclusive comparison.</description></item>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): the trace now creates a sentinel session one step older than the session under test and reads each keep decision after a trigger only once the sentinel is seen removed by the same pass, the pool gains <see cref="IIdleSessionPool.CreateSentinelAsync"/>, and a negative control rejects a pool whose triggered pass runs only at the next trigger, because the fifteenth holistic read before hostile validation round 14 (finding d7) found that the trace read a keep decision right after a trigger without observing that the pass had finished, and the earlier trace accepted that pool (red mock gate ug-r14z13-mockgate-red-1).</description></item>
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
    /// agent and workspace and a sentinel for a second one, checks whether a session still exists, and triggers the
    /// background cleanup.
    /// </summary>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): <see cref="CreateSentinelAsync"/> added for the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
    internal interface IIdleSessionPool : IDisposable
    {
        /// <summary>Sets the scripted clock every later step reads.</summary>
        /// <param name="utcNow">The instant, with a zero offset.</param>
        void SetClock(DateTimeOffset utcNow);

        /// <summary>Creates (or reuses) the session for the fixed agent and workspace.</summary>
        /// <returns>The session id.</returns>
        Task<string> CreateAsync();

        /// <summary>
        /// Creates (or reuses) the sentinel session for a second fixed agent and workspace, which no
        /// <see cref="CreateAsync"/> reuses; the trace reads it to see that a triggered pass has run to its end.
        /// </summary>
        /// <returns>The sentinel session id.</returns>
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added for the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
        Task<string> CreateSentinelAsync();

        /// <summary>Whether the session still exists, read without refreshing its idle age.</summary>
        /// <param name="sessionId">The session id.</param>
        /// <returns>True when it exists.</returns>
        Task<bool> ExistsAsync(string sessionId);

        /// <summary>Runs the background cleanup once, now.</summary>
        void TriggerCleanup();
    }

    /// <summary>
    /// Runs the lifecycle trace at both references against pools from <paramref name="createPool"/>, asserting every
    /// step. A pool is created per reference with its clock at that reference; the sentinel is created at the reference
    /// and the session under test one step later.
    /// </summary>
    /// <param name="createPool">Creates a pool whose scripted clock starts at the given reference.</param>
    /// <returns>A task that completes when every step has been asserted.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): the trace creates the sentinel first and, after the trigger at the threshold, asserts it removed before it reads the keep decision, because the fifteenth holistic read before hostile validation round 14 (finding d7) found that a pass still running or running later could make that keep assertion pass.</remarks>
    internal static async Task AssertLifecycleAsync(Func<DateTimeOffset, IIdleSessionPool> createPool)
    {
        ArgumentNullException.ThrowIfNull(createPool);
        var step = AgeBoundaryContractTests.Step;
        foreach (var reference in AgeBoundaryContractTests.References)
        {
            using var pool = createPool(reference);
            pool.SetClock(reference);
            var sentinel = await pool.CreateSentinelAsync().ConfigureAwait(true);
            var start = reference + step;
            pool.SetClock(start);
            var first = await pool.CreateAsync().ConfigureAwait(true);

            pool.SetClock(start + IdleTimeout);
            pool.TriggerCleanup();
            Assert.False(await pool.ExistsAsync(sentinel).ConfigureAwait(true), $"At {reference:O}: the triggered pass has run to its end before its decisions are read: the sentinel, one step past the idle threshold, is already removed.");
            Assert.True(await pool.ExistsAsync(first).ConfigureAwait(true), $"At {reference:O}: the same triggered pass keeps a session exactly at the idle threshold (the comparison is strict).");
            var second = await pool.CreateAsync().ConfigureAwait(true);
            Assert.True(first == second, $"At {reference:O}: the second create reuses the idle session across an interleaved cleanup.");

            var pastThreshold = start + IdleTimeout + IdleTimeout + step;
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

    /// <summary>
    /// Mock gate negative control: a pool whose triggered cleanup pass runs only later (at the next trigger), with an
    /// inclusive comparison that the late pass hides, is rejected.
    /// </summary>
    /// <returns>A task that completes when the rejection is observed.</returns>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): added because the fifteenth holistic read before hostile validation round 14 (finding d7) found that the trace asserted right after a trigger without observing that the triggered pass had run to its end.</remarks>
    [Fact]
    public Task PoolWhoseTriggeredCleanupFinishesLater_IsRejected() =>
        Assert.ThrowsAnyAsync<XunitException>(() =>
            AssertLifecycleAsync(reference => new ScriptedPool(reference, ScriptedPool.CleanupClock.Scripted, selfScheduling: false, inclusive: true, deferred: true)));

    /// <summary>Mock gate negative control: a pool that never reuses a session is rejected.</summary>
    /// <returns>A task that completes when the rejection is observed.</returns>
    [Fact]
    public Task PoolThatNeverReuses_IsRejected() =>
        Assert.ThrowsAnyAsync<XunitException>(() =>
            AssertLifecycleAsync(reference => new ScriptedPool(reference, ScriptedPool.CleanupClock.Scripted, selfScheduling: false, inclusive: false, reuse: false)));

    /// <summary>
    /// The independent scripted pool: one session per create unless an existing one for the same key (the fixed session
    /// or the sentinel) is reused; a create or reuse records the scripted instant; a cleanup pass removes every session
    /// whose idle age is more than the timeout. The controls change one property each: the clock the cleanup reads,
    /// whether the cleanup runs on its own whenever the clock moves, the comparison, whether creates reuse, and whether a
    /// triggered pass runs before the trigger returns or only at the next trigger.
    /// </summary>
    /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "A test that triggers such a callback asserts its decisions only after it has observed that the pass it triggered has run to its end"): sessions are now kept per key so the sentinel is never reused for the fixed session, and the deferred-pass control was added, for the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
    private sealed class ScriptedPool : IIdleSessionPool
    {
        /// <summary>The key of the fixed session.</summary>
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added for the sentinel of the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
        private const string MainKey = "main";

        /// <summary>The key of the sentinel session.</summary>
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added for the sentinel of the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
        private const string SentinelKey = "sentinel";

        /// <summary>The key and recorded idle time of each session.</summary>
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: now holds each session's key with its recorded idle time, so a create reuses only a session of its own key (fifteenth holistic read before hostile validation round 14, finding d7).</remarks>
        private readonly Dictionary<string, (string Key, DateTimeOffset Recorded)> _sessions = new(StringComparer.Ordinal);

        /// <summary>Which clock the cleanup reads.</summary>
        private readonly CleanupClock _cleanupClock;

        /// <summary>Whether the cleanup runs on its own whenever the clock moves.</summary>
        private readonly bool _selfScheduling;

        /// <summary>Whether the cleanup removes a session exactly at the threshold.</summary>
        private readonly bool _inclusive;

        /// <summary>Whether a create reuses an existing session.</summary>
        private readonly bool _reuse;

        /// <summary>Whether a triggered pass runs only at the next trigger instead of before the trigger returns.</summary>
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added for the deferred-pass control of the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
        private readonly bool _deferred;

        /// <summary>Whether a deferred pass is waiting for the next trigger.</summary>
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added for the deferred-pass control of the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
        private bool _pending;

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
        /// <param name="deferred">Whether a triggered pass runs only at the next trigger, reading the clock then.</param>
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: the optional <paramref name="deferred"/> parameter was added for the fifteenth holistic read before hostile validation round 14, finding d7; the other parameters are unchanged.</remarks>
        internal ScriptedPool(DateTimeOffset start, CleanupClock cleanupClock, bool selfScheduling, bool inclusive, bool reuse = true, bool deferred = false)
        {
            _now = start;
            _cleanupClock = cleanupClock;
            _selfScheduling = selfScheduling;
            _inclusive = inclusive;
            _reuse = reuse;
            _deferred = deferred;
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
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: now creates or reuses the session of the fixed key only, so the sentinel is never reused for it (fifteenth holistic read before hostile validation round 14, finding d7).</remarks>
        public Task<string> CreateAsync() => Task.FromResult(Create(MainKey));

        /// <inheritdoc />
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added for the fifteenth holistic read before hostile validation round 14, finding d7.</remarks>
        public Task<string> CreateSentinelAsync() => Task.FromResult(Create(SentinelKey));

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string sessionId) => Task.FromResult(_sessions.ContainsKey(sessionId));

        /// <inheritdoc />
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: a deferred pool now runs a triggered pass only at the next trigger, for the control of the fifteenth holistic read before hostile validation round 14 (finding d7); every other pool runs the pass before the trigger returns, as before.</remarks>
        public void TriggerCleanup()
        {
            if (!_deferred)
            {
                RunPass();
                return;
            }

            if (_pending)
                RunPass();
            _pending = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>Creates a session for <paramref name="key"/>, or reuses the existing one when the pool reuses.</summary>
        /// <param name="key">The session key.</param>
        /// <returns>The session id.</returns>
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added, from the earlier create, so each key keeps its own session (fifteenth holistic read before hostile validation round 14, finding d7).</remarks>
        private string Create(string key)
        {
            if (_reuse)
            {
                foreach (var (existing, session) in _sessions)
                {
                    if (session.Key != key)
                        continue;
                    _sessions[existing] = (key, _now);
                    return existing;
                }
            }

            _created++;
            var id = $"session-{_created}";
            _sessions[id] = (key, _now);
            return id;
        }

        /// <summary>One cleanup pass: removes every session whose idle age, on the clock the pass reads, is past the timeout.</summary>
        /// <remarks>2026-11-06, TR-MCP-USECASE-018 AC-006: added, from the earlier cleanup body unchanged, so a deferred pool can run the pass later (fifteenth holistic read before hostile validation round 14, finding d7).</remarks>
        private void RunPass()
        {
            var now = _cleanupClock == CleanupClock.Scripted ? _now : AnotherClock;
            foreach (var (id, session) in _sessions.ToArray())
            {
                var age = now - session.Recorded;
                if (_inclusive ? age >= IdleTimeout : age > IdleTimeout)
                    _sessions.Remove(id);
            }
        }
    }
}
