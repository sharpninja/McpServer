using System.Globalization;
using Xunit;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// Tests the parse rule for the REPL command timeouts configured through MCPSERVER_REPL_COMMAND_TIMEOUT_SECONDS and
/// MCPSERVER_REPL_STREAM_COMMAND_TIMEOUT_SECONDS: a whole number of seconds from 1 to the largest delay the runtime's
/// cancellation timer accepts is used, and any other value gives the two-minute default.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-REPL-001 ac-1 (hostile validation round 7: both variables at 2147483647 made 19 REPL
/// unit cases throw ArgumentOutOfRangeException from CancellationTokenSource.CancelAfter;
/// triage-report-c993b9446bb8464ba375f74fabd07dad), and through it TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019
/// AC-008 (a unit test may reach code that reads an environment variable only where its outcome does not change).
/// Test data: a table of scripted configured values with the timeout each must give, run against a parse function
/// passed in, so the contract is proved on a scripted reference parser with negative controls before the production
/// parser exists; no test reads or writes the process environment.
/// </remarks>
public sealed class ReplCommandTimeoutTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    private const long LargestSeconds = 4294967;

    /// <summary>
    /// The contract rows: each configured value and the timeout it must give. Unset, empty, not a number, zero,
    /// negative and larger than the timer's limit give the default; 1 to the limit, surrounding blanks allowed as
    /// before, give that many seconds.
    /// </summary>
    private static readonly (string? Configured, TimeSpan Expected)[] ContractRows =
    [
        (null, DefaultTimeout),
        (string.Empty, DefaultTimeout),
        ("   ", DefaultTimeout),
        ("not-a-number", DefaultTimeout),
        ("1.5", DefaultTimeout),
        ("0", DefaultTimeout),
        ("-1", DefaultTimeout),
        ("-2147483648", DefaultTimeout),
        ("1", TimeSpan.FromSeconds(1)),
        ("120", TimeSpan.FromSeconds(120)),
        (" 30 ", TimeSpan.FromSeconds(30)),
        ("4294966", TimeSpan.FromSeconds(4294966)),
        ("4294967", TimeSpan.FromSeconds(LargestSeconds)),
        ("4294968", DefaultTimeout),
        ("2147483647", DefaultTimeout),
        ("99999999999", DefaultTimeout),
    ];

    /// <summary>
    /// Asserts that <paramref name="parse"/> gives the expected timeout for every contract row, and that every timeout
    /// it gives is accepted by <see cref="CancellationTokenSource.CancelAfter(TimeSpan)"/>.
    /// </summary>
    /// <param name="parse">The parse function under test.</param>
    internal static void AssertParseContract(Func<string?, TimeSpan> parse)
    {
        ArgumentNullException.ThrowIfNull(parse);
        var wrong = new List<string>();
        foreach (var (configured, expected) in ContractRows)
        {
            var actual = parse(configured);
            if (actual != expected)
            {
                wrong.Add($"'{configured ?? "(unset)"}' gave {actual} instead of {expected}");
                continue;
            }

            using var source = new CancellationTokenSource();
            source.CancelAfter(actual);
        }

        Assert.Empty(wrong);
    }

    /// <summary>Runtime check: the cancellation timer accepts the largest whole-second delay and rejects one more.</summary>
    [Fact]
    public void CancelAfter_AcceptsTheLargestSecondsAndRejectsOneMore()
    {
        using var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromSeconds(LargestSeconds));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CancelAfter(TimeSpan.FromSeconds(LargestSeconds + 1L)));
    }

    /// <summary>
    /// Binding: the production parser, <see cref="ReplCommandTimeout.Parse"/>, satisfies the same contract rows the
    /// scripted reference passed and the wrong parsers failed, and its default and limit are the contract's.
    /// </summary>
    [Fact]
    public void ProductionParser_SatisfiesTheContract()
    {
        Assert.Equal(DefaultTimeout, ReplCommandTimeout.Default);
        Assert.Equal(LargestSeconds, ReplCommandTimeout.LargestSeconds);
        AssertParseContract(ReplCommandTimeout.Parse);
    }

    /// <summary>Mock gate positive control: the scripted reference parser satisfies every contract row.</summary>
    [Fact]
    public void ScriptedReferenceParser_SatisfiesTheContract() =>
        AssertParseContract(ScriptedReference);

    /// <summary>
    /// Mock gate negative controls, each rejected inside a passing harness: the parser before this fix (no upper bound,
    /// so 4294968 and 2147483647 pass through), one that caps a larger value at the limit instead of using the default,
    /// one that refuses the limit itself, and one that refuses 1.
    /// </summary>
    [Fact]
    public void WrongParsers_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertParseContract(Unbounded)));
        Assert.NotNull(Record.Exception(() => AssertParseContract(Capping)));
        Assert.NotNull(Record.Exception(() => AssertParseContract(configured => Bounded(configured, 1, LargestSeconds - 1))));
        Assert.NotNull(Record.Exception(() => AssertParseContract(configured => Bounded(configured, 2, LargestSeconds))));
    }

    /// <summary>The scripted reference parse, also used by <see cref="ReplCommandTimeoutCallSiteTests"/>.</summary>
    internal static TimeSpan ScriptedReference(string? configured) => Bounded(configured, 1, LargestSeconds);

    private static TimeSpan Bounded(string? configured, long smallest, long largest) =>
        long.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds >= smallest && seconds <= largest
            ? TimeSpan.FromSeconds(seconds)
            : DefaultTimeout;

    /// <summary>The parse from before the R7-F1 fix, a negative control here and in <see cref="ReplCommandTimeoutCallSiteTests"/>.</summary>
    internal static TimeSpan Unbounded(string? configured) =>
        int.TryParse(configured, out var seconds) && seconds > 0 ? TimeSpan.FromSeconds(seconds) : DefaultTimeout;

    private static TimeSpan Capping(string? configured) =>
        long.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? TimeSpan.FromSeconds(Math.Min(seconds, LargestSeconds))
            : DefaultTimeout;
}
