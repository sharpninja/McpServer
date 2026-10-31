// FR-MCP-REPL-001 ac-1: the two call sites that apply a configured REPL command timeout

using System.Globalization;
using System.Text;
using NSubstitute;
using Xunit;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// Tests the two places that apply a configured REPL command timeout: the agent stdio protocol, which reads
/// MCPSERVER_REPL_COMMAND_TIMEOUT_SECONDS for each request, and the TODO stream commands of the dispatcher, which read
/// MCPSERVER_REPL_STREAM_COMMAND_TIMEOUT_SECONDS for each stream. Each call site reads only its own variable, once, and
/// applies the one parse rule bound in <see cref="ReplCommandTimeoutTests"/>: a value the rule refuses never fails the
/// request, and the timeout message names the timeout that applied.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-REPL-001 ac-1 (round 8 self-evaluation: the parse rule was bound, but neither call
/// site was, so the message clause and the call-site no-failure clause had no test), and through it
/// TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008. Test data: contract rows, each a pair of scripted variable
/// values given through a reader that records every name it is asked for (never the process environment) and a command
/// that either answers at once or runs until cancelled, with the outcome the call site must give. The contract is proved
/// on a scripted reference call site, with wrong call sites rejected inside a passing harness, before it is bound to
/// the production call sites.
/// </remarks>
public sealed class ReplCommandTimeoutCallSiteTests
{
    /// <summary>The variable the agent stdio protocol reads for each request.</summary>
    internal const string CommandVariable = "MCPSERVER_REPL_COMMAND_TIMEOUT_SECONDS";

    /// <summary>The variable the TODO stream commands read for each stream.</summary>
    internal const string StreamVariable = "MCPSERVER_REPL_STREAM_COMMAND_TIMEOUT_SECONDS";

    /// <summary>The protocol's timeout message, with the applied timeout in whole seconds.</summary>
    internal const string CommandMessageFormat = "Command timed out after {0} seconds.";

    /// <summary>The stream's timeout message, with the applied timeout in whole seconds.</summary>
    internal const string StreamMessageFormat = "Stream command timed out after {0} seconds.";

    /// <summary>The outcome kind of a command that finished normally.</summary>
    internal const string Completed = "completed";

    /// <summary>The outcome kind of a command stopped by the configured timeout.</summary>
    internal const string TimedOut = "timed-out";

    /// <summary>What one run of a call site produced: its outcome kind and, for a timeout, the message.</summary>
    /// <param name="Kind"><see cref="Completed"/>, <see cref="TimedOut"/>, or a description of anything else.</param>
    /// <param name="Message">The timeout message, when the kind is <see cref="TimedOut"/>.</param>
    internal sealed record CallSiteResult(string Kind, string? Message);

    /// <summary>One contract row: the call site's own variable value, the other variable's value, the command, and the outcome.</summary>
    private sealed record CallSiteRow(
        string Label,
        string? OwnValue,
        string? OtherValue,
        bool RunsUntilCancelled,
        string ExpectedKind,
        long? ExpectedSeconds);

    /// <summary>
    /// The contract rows. A valid own value stops a command that runs until cancelled after that many seconds and the
    /// message names it; a value the rule refuses (above the timer's limit, the round 7 value 2147483647, zero, not a
    /// number) or an unset own value never fails a command that answers at once, and neither does the largest valid
    /// value; the other call site's variable is never read.
    /// </summary>
    private static readonly CallSiteRow[] Rows =
    [
        new("own 1, command runs until cancelled", "1", null, true, TimedOut, 1),
        new("own 2, command runs until cancelled", "2", null, true, TimedOut, 2),
        new("own 2147483647, command answers at once", "2147483647", null, false, Completed, null),
        new("own 4294968, command answers at once", "4294968", null, false, Completed, null),
        new("own 4294967, command answers at once", "4294967", null, false, Completed, null),
        new("own 0, command answers at once", "0", null, false, Completed, null),
        new("own not-a-number, command answers at once", "not-a-number", null, false, Completed, null),
        new("own unset and other 1, command answers at once", null, "1", false, Completed, null),
    ];

    /// <summary>
    /// Asserts that <paramref name="run"/> gives the expected outcome for every contract row and reads exactly one
    /// variable, <paramref name="ownVariable"/>, once per run.
    /// </summary>
    /// <param name="ownVariable">The variable this call site must read.</param>
    /// <param name="otherVariable">The other call site's variable, which this call site must not read.</param>
    /// <param name="messageFormat">This call site's timeout message format.</param>
    /// <param name="run">Runs the call site once with a variable reader and a command that runs until cancelled or not.</param>
    internal static async Task AssertCallSiteContract(
        string ownVariable,
        string otherVariable,
        string messageFormat,
        Func<Func<string, string?>, bool, Task<CallSiteResult>> run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var wrong = new List<string>();
        foreach (var row in Rows)
        {
            var values = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [ownVariable] = row.OwnValue,
                [otherVariable] = row.OtherValue,
            };
            var namesRead = new List<string>();
            string? Read(string name)
            {
                lock (namesRead)
                {
                    namesRead.Add(name);
                }

                return values.GetValueOrDefault(name);
            }

            CallSiteResult result;
            try
            {
                result = await run(Read, row.RunsUntilCancelled).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                result = new CallSiteResult("exception " + ex.GetType().Name, ex.Message);
            }

            var expectedMessage = row.ExpectedSeconds is { } seconds
                ? string.Format(CultureInfo.InvariantCulture, messageFormat, seconds)
                : null;
            if (result.Kind != row.ExpectedKind || (expectedMessage is not null && result.Message != expectedMessage))
            {
                wrong.Add($"{row.Label}: gave {result.Kind} '{result.Message}' instead of {row.ExpectedKind} '{expectedMessage}'");
            }

            string[] read;
            lock (namesRead)
            {
                read = [.. namesRead];
            }

            if (read.Length != 1 || read[0] != ownVariable)
            {
                wrong.Add($"{row.Label}: read [{string.Join(", ", read)}] instead of [{ownVariable}] once");
            }
        }

        Assert.Empty(wrong);
    }

    /// <summary>
    /// Mock gate positive control: a scripted reference call site (one read of its own variable, the scripted contract
    /// parse, the runtime's cancellation timer, the message naming the applied timeout) satisfies the contract for
    /// both call sites.
    /// </summary>
    [Fact]
    public async Task ScriptedReferenceCallSite_SatisfiesTheContract_ForBothCallSites()
    {
        await AssertCallSiteContract(
            CommandVariable,
            StreamVariable,
            CommandMessageFormat,
            (read, runs) => ScriptedCallSite(read, runs, CommandVariable, CommandMessageFormat, ReplCommandTimeoutTests.ScriptedReference)).ConfigureAwait(true);
        await AssertCallSiteContract(
            StreamVariable,
            CommandVariable,
            StreamMessageFormat,
            (read, runs) => ScriptedCallSite(read, runs, StreamVariable, StreamMessageFormat, ReplCommandTimeoutTests.ScriptedReference)).ConfigureAwait(true);
    }

    /// <summary>
    /// Mock gate negative controls, each rejected inside a passing harness for each call site: the unbounded parse from
    /// before the R7-F1 fix (its timer throws for 2147483647), a call site that reads the other variable, one that reads
    /// its variable twice, one whose message names the two-minute default instead of the applied timeout, and one whose
    /// message always names one second.
    /// </summary>
    /// <param name="callSite">"command" for the protocol's contract, "stream" for the stream commands' contract.</param>
    [Theory]
    [InlineData("command")]
    [InlineData("stream")]
    public async Task WrongCallSites_AreRejectedInsidePassingHarness(string callSite)
    {
        var (own, other, format) = callSite == "command"
            ? (CommandVariable, StreamVariable, CommandMessageFormat)
            : (StreamVariable, CommandVariable, StreamMessageFormat);
        Func<string?, TimeSpan> parse = ReplCommandTimeoutTests.ScriptedReference;
        var wrongCallSites = new (string Label, Func<Func<string, string?>, bool, Task<CallSiteResult>> Run)[]
        {
            ("unbounded parse, as before the R7-F1 fix", (read, runs) => ScriptedCallSite(read, runs, own, format, ReplCommandTimeoutTests.Unbounded)),
            ("reads the other variable", (read, runs) => ScriptedCallSite(read, runs, other, format, parse)),
            ("reads its variable twice", (read, runs) =>
            {
                _ = read(own);
                return ScriptedCallSite(read, runs, own, format, parse);
            }),
            ("message names the default", async (read, runs) => WithMessage(await ScriptedCallSite(read, runs, own, format, parse).ConfigureAwait(true), format, 120)),
            ("message always names one second", async (read, runs) => WithMessage(await ScriptedCallSite(read, runs, own, format, parse).ConfigureAwait(true), format, 1)),
        };

        foreach (var (label, run) in wrongCallSites)
        {
            var failure = await Record.ExceptionAsync(() => AssertCallSiteContract(own, other, format, run)).ConfigureAwait(true);
            Assert.True(failure is not null, $"The {callSite} contract accepted a wrong call site: {label}.");
        }
    }

    /// <summary>
    /// Binding: the agent stdio protocol, given the scripted reader through its <c>ReadVariable</c> seam and a
    /// dispatcher whose command answers at once or runs until cancelled, satisfies the same contract rows the scripted
    /// reference passed and the wrong call sites failed. The two timeout rows wait 1 and 2 seconds of real time.
    /// </summary>
    [Fact]
    public async Task ProductionProtocol_SatisfiesTheContract() =>
        await AssertCallSiteContract(CommandVariable, StreamVariable, CommandMessageFormat, RunProtocolAsync).ConfigureAwait(true);

    /// <summary>
    /// Binding: a TODO stream command (workflow.todo.streamStatus) through the dispatcher, given the scripted reader
    /// through its <c>ReadVariable</c> seam and a workflow whose stream ends at once or runs until cancelled, satisfies
    /// the same contract rows. The two timeout rows wait 1 and 2 seconds of real time.
    /// </summary>
    [Fact]
    public async Task ProductionStreamCommand_SatisfiesTheContract() =>
        await AssertCallSiteContract(StreamVariable, CommandVariable, StreamMessageFormat, RunStreamCommandAsync).ConfigureAwait(true);

    private static async Task<CallSiteResult> RunProtocolAsync(Func<string, string?> read, bool runsUntilCancelled)
    {
        var serializer = new YamlSerializer();
        var sut = new AgentStdioProtocol(serializer, new ScriptedDispatcher(runsUntilCancelled)) { ReadVariable = read };
        var input = new StringBuilder()
            .AppendLine("type: request")
            .AppendLine("payload:")
            .AppendLine("  requestId: req-callsite-timeout")
            .AppendLine("  method: client.todo.QueryAsync")
            .AppendLine()
            .ToString();
        using var reader = new StringReader(input);
        using var writer = new StringWriter();
        await sut.RunAsync(reader, writer, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = serializer.Deserialize(FirstDocument(writer.ToString()));
        return response.Payload switch
        {
            ErrorPayload { Code: "command_timeout" } error => new CallSiteResult(TimedOut, error.Message),
            ErrorPayload error => new CallSiteResult("error " + error.Code, error.Message),
            ResultPayload => new CallSiteResult(Completed, null),
            _ => new CallSiteResult("envelope " + response.Type, null),
        };
    }

    private static async Task<CallSiteResult> RunStreamCommandAsync(Func<string, string?> read, bool runsUntilCancelled)
    {
        var todo = Substitute.For<ITodoWorkflow>();
        todo.StreamStatusAsync("MCP-TIMEOUT-001", Arg.Any<Func<IStreamingEvent, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => runsUntilCancelled
                ? Task.Delay(Timeout.Infinite, call.ArgAt<CancellationToken>(2))
                : Task.CompletedTask);
        var sut = new ReplCommandDispatcher(Substitute.For<IGenericClientPassthrough>(), todoWorkflow: todo) { ReadVariable = read };
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-callsite-stream",
                Method = TodoCommandShapes.StreamStatusMethod,
                Params = new Dictionary<string, object?> { ["id"] = "MCP-TIMEOUT-001" },
            },
        };

        var response = await sut.DispatchAsync(envelope, TestContext.Current.CancellationToken).ConfigureAwait(true);
        if (response.Payload is ErrorPayload error)
        {
            return new CallSiteResult("error " + error.Code, error.Message);
        }

        if (response.Payload is not ResultPayload { Result: IReadOnlyList<IStreamingEvent> events })
        {
            return new CallSiteResult("envelope " + response.Type, null);
        }

        var timeout = events.LastOrDefault(evt => evt.EventType == "stream.timeout");
        return timeout is null
            ? new CallSiteResult(Completed, null)
            : new CallSiteResult(TimedOut, (timeout.Data as IReadOnlyDictionary<string, object?>)?.GetValueOrDefault("message") as string);
    }

    /// <summary>The first YAML document of the protocol's output, which frames each envelope with a "---" line.</summary>
    private static string FirstDocument(string output)
    {
        var document = new StringBuilder();
        foreach (var line in output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (line.Trim() == "---")
            {
                if (document.Length == 0)
                {
                    continue;
                }

                break;
            }

            document.AppendLine(line);
        }

        return document.ToString();
    }

    /// <summary>A dispatcher whose one command answers at once, or runs until its token is cancelled.</summary>
    private sealed class ScriptedDispatcher(bool runsUntilCancelled) : IReplCommandDispatcher
    {
        public async Task<IYamlEnvelope> DispatchAsync(IYamlEnvelope envelope, CancellationToken cancellationToken)
        {
            if (runsUntilCancelled)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(true);
            }

            return new YamlEnvelope
            {
                Type = "result",
                Payload = new ResultPayload { RequestId = "req-callsite-timeout", Result = "answered" },
            };
        }
    }

    /// <summary>
    /// A scripted call site: reads <paramref name="variable"/> once, parses it with <paramref name="parse"/>, arms the
    /// runtime's cancellation timer with the result (which throws for an out-of-range delay, as the real timer does), and
    /// reports a timeout naming the applied seconds for a command that runs until cancelled.
    /// </summary>
    private static Task<CallSiteResult> ScriptedCallSite(
        Func<string, string?> read,
        bool runsUntilCancelled,
        string variable,
        string messageFormat,
        Func<string?, TimeSpan> parse)
    {
        var timeout = parse(read(variable));
        using var timer = new CancellationTokenSource();
        timer.CancelAfter(timeout);
        return Task.FromResult(runsUntilCancelled
            ? new CallSiteResult(TimedOut, string.Format(CultureInfo.InvariantCulture, messageFormat, timeout.TotalSeconds))
            : new CallSiteResult(Completed, null));
    }

    private static CallSiteResult WithMessage(CallSiteResult result, string messageFormat, long seconds) =>
        result.Kind == TimedOut
            ? result with { Message = string.Format(CultureInfo.InvariantCulture, messageFormat, seconds) }
            : result;
}
