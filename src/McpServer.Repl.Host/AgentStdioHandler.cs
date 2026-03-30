using Microsoft.Extensions.Logging;
using System.Text;

namespace McpServer.Repl.Host;

/// <summary>
/// Handles agent STDIO mode for MCP protocol communication.
/// Implements the STDIO read/write loop for protocol envelope exchange.
/// </summary>
public class AgentStdioHandler
{
    private readonly ILogger<AgentStdioHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentStdioHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    public AgentStdioHandler(ILogger<AgentStdioHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Runs the agent STDIO loop, reading envelopes from stdin and writing responses to stdout.
    /// Continues until the input stream is closed or a cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the loop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting agent STDIO mode");

        try
        {
            using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
            using var writer = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8) { AutoFlush = true };

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    _logger.LogInformation("STDIN closed, exiting");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    await ProcessEnvelopeAsync(line, writer, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing envelope: {Line}", line);
                    await WriteErrorEnvelopeAsync(writer, "internal_error", ex.Message, null, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Agent STDIO mode cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in agent STDIO mode");
            throw;
        }
    }

    private async Task ProcessEnvelopeAsync(
        string envelopeJson,
        StreamWriter writer,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received envelope: {Envelope}", envelopeJson);
        await writer.WriteLineAsync($"{{\"type\":\"echo\",\"payload\":\"{envelopeJson}\"}}");
    }

    private async Task WriteErrorEnvelopeAsync(
        StreamWriter writer,
        string code,
        string message,
        string? requestId,
        CancellationToken cancellationToken)
    {
        var errorEnvelope = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "error",
            payload = new
            {
                requestId = requestId ?? "unknown",
                code,
                message
            }
        });

        await writer.WriteLineAsync(errorEnvelope);
    }
}
