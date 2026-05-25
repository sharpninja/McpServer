// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Stream-level envelope loop
// FR-MCP-REPL-002: REPL Lifecycle Management - Read/accumulate/dispatch loop
// TR-MCP-REPL-003: Command Loop Lifecycle - Multi-line YAML framing
// TEST-MCP-REPL-001: REPL host processes well-formed multi-line YAML envelopes end-to-end

using System.Text;

namespace McpServer.Repl.Core;

/// <summary>
/// Runs the REPL agent-stdio read/write loop: reads envelope lines from a <see cref="TextReader"/>,
/// accumulates them into complete YAML documents (terminated by a blank line or the
/// <c>---</c> document separator), dispatches each via <see cref="IReplCommandDispatcher"/>,
/// and writes the response envelope back to the <see cref="TextWriter"/>.
/// </summary>
public interface IAgentStdioProtocol
{
    /// <summary>
    /// Runs the agent-stdio loop until the reader reports end-of-stream or cancellation.
    /// </summary>
    /// <param name="reader">Inbound envelope stream.</param>
    /// <param name="writer">Outbound response stream. The implementation flushes after each envelope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RunAsync(TextReader reader, TextWriter writer, CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IAgentStdioProtocol"/> implementation. Accumulates inbound lines into
/// YAML documents using the blank-line and <c>---</c> conventions, delegates parsing to
/// <see cref="IYamlSerializer"/>, and routes each envelope through
/// <see cref="IReplCommandDispatcher"/>. Malformed documents are reported as
/// <c>invalid_envelope</c> errors and the loop continues.
/// </summary>
public sealed class AgentStdioProtocol : IAgentStdioProtocol
{
    private const string CommandTimeoutEnvVar = "MCPSERVER_REPL_COMMAND_TIMEOUT_SECONDS";
    private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromMinutes(2);

    private readonly IYamlSerializer _serializer;
    private readonly IReplCommandDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new <see cref="AgentStdioProtocol"/>.
    /// </summary>
    /// <param name="serializer">YAML serializer used to parse and emit envelopes.</param>
    /// <param name="dispatcher">Command dispatcher invoked once per complete inbound envelope.</param>
    public AgentStdioProtocol(IYamlSerializer serializer, IReplCommandDispatcher dispatcher)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc />
    public async Task RunAsync(TextReader reader, TextWriter writer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(writer);

        var buffer = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                // End of stream — flush any accumulated envelope before exiting.
                await FlushAsync(buffer, writer, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (IsDocumentBoundary(line))
            {
                await FlushAsync(buffer, writer, cancellationToken).ConfigureAwait(false);
                continue;
            }

            buffer.AppendLine(line);
        }

        // Cancellation requested — attempt a final flush so callers don't lose the last envelope.
        await FlushAsync(buffer, writer, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsDocumentBoundary(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return true;
        }

        var trimmed = line.TrimEnd();
        return trimmed == "---";
    }

    private async Task FlushAsync(StringBuilder buffer, TextWriter writer, CancellationToken cancellationToken)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        var document = buffer.ToString();
        buffer.Clear();

        if (string.IsNullOrWhiteSpace(document))
        {
            return;
        }

        IYamlEnvelope envelope;
        try
        {
            envelope = _serializer.Deserialize(document);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            var errorEnvelope = new YamlEnvelope
            {
                Type = "error",
                Payload = new ErrorPayload
                {
                    RequestId = TryExtractRequestId(document),
                    Code = "invalid_envelope",
                    Message = ex.Message,
                },
            };
            await WriteEnvelopeAsync(errorEnvelope, writer, cancellationToken).ConfigureAwait(false);
            return;
        }

        IYamlEnvelope response;
        using var commandCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        commandCts.CancelAfter(ResolveCommandTimeout());

        try
        {
            if (_dispatcher is IStreamingReplCommandDispatcher streamingDispatcher)
            {
                response = await streamingDispatcher.DispatchAsync(
                    envelope,
                    async evt => await WriteEnvelopeAsync(evt, writer, cancellationToken).ConfigureAwait(false),
                    commandCts.Token).ConfigureAwait(false);
            }
            else
            {
                response = await _dispatcher.DispatchAsync(envelope, commandCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            response = new YamlEnvelope
            {
                Type = "error",
                Payload = new ErrorPayload
                {
                    RequestId = (envelope.Payload as IRequestPayload)?.RequestId ?? "unknown",
                    Code = "command_timeout",
                    Message = $"Command timed out after {ResolveCommandTimeout().TotalSeconds:0} seconds.",
                },
            };
        }
        catch (Exception ex)
        {
            response = new YamlEnvelope
            {
                Type = "error",
                Payload = new ErrorPayload
                {
                    RequestId = (envelope.Payload as IRequestPayload)?.RequestId ?? "unknown",
                    Code = "dispatch_error",
                    Message = ex.Message,
                },
            };
        }

        await WriteEnvelopeAsync(response, writer, cancellationToken).ConfigureAwait(false);
    }

    private static TimeSpan ResolveCommandTimeout()
    {
        var configured = Environment.GetEnvironmentVariable(CommandTimeoutEnvVar);
        if (int.TryParse(configured, out var seconds) && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return DefaultCommandTimeout;
    }

    private async Task WriteEnvelopeAsync(IYamlEnvelope envelope, TextWriter writer, CancellationToken cancellationToken)
    {
        var yaml = _serializer.Serialize(envelope);
        // Envelopes are framed by a blank line, matching the inbound convention.
        await writer.WriteAsync(yaml.AsMemory(), cancellationToken).ConfigureAwait(false);
        if (!yaml.EndsWith('\n'))
        {
            await writer.WriteLineAsync().ConfigureAwait(false);
        }
        await writer.WriteLineAsync().ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string TryExtractRequestId(string document)
    {
        // Best-effort recovery of a requestId from a malformed document so callers can correlate.
        foreach (var rawLine in document.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("requestId:", StringComparison.OrdinalIgnoreCase))
            {
                return line["requestId:".Length..].Trim().Trim('"');
            }
        }

        return "unknown";
    }
}
