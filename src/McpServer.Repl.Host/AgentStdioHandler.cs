// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Agent STDIO mode handler
// FR-MCP-REPL-002: REPL Lifecycle Management - Agent command loop and lifecycle
// TR-MCP-REPL-002: DI-Integrated REPL Host - Agent handler DI integration
// TR-MCP-REPL-003: Command Loop Lifecycle - Agent STDIO processing
// TEST-MCP-REPL-001: REPL host processes well-formed YAML command envelopes
// TEST-MCP-REPL-013: REPL host terminates gracefully on EOF or exit command

using System.Text;
using McpServer.Repl.Core;
using Microsoft.Extensions.Logging;

namespace McpServer.Repl.Host;

/// <summary>
/// Handles agent STDIO mode for MCP protocol communication. Thin shell over
/// <see cref="IAgentStdioProtocol"/> that wires <c>Console.In</c>/<c>Console.Out</c> into
/// the reusable stream-level loop living in <c>McpServer.Repl.Core</c>.
/// </summary>
public class AgentStdioHandler
{
    private static readonly UTF8Encoding ProtocolEncoding = new(encoderShouldEmitUTF8Identifier: false);
    private readonly ILogger<AgentStdioHandler> _logger;
    private readonly IAgentStdioProtocol _protocol;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentStdioHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <param name="protocol">Stream-level REPL protocol implementation.</param>
    public AgentStdioHandler(ILogger<AgentStdioHandler> logger, IAgentStdioProtocol protocol)
    {
        _logger = logger;
        _protocol = protocol;
    }

    /// <summary>
    /// Runs the agent STDIO loop, reading envelopes from stdin and writing responses to stdout.
    /// Continues until the input stream is closed or cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the loop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting agent STDIO mode");

        try
        {
            using var reader = new StreamReader(Console.OpenStandardInput(), ProtocolEncoding);
            using var writer = new StreamWriter(Console.OpenStandardOutput(), ProtocolEncoding) { AutoFlush = true };

            await _protocol.RunAsync(reader, writer, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("STDIN closed, exiting");
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
}
