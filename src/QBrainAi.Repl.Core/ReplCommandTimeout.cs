// FR-MCP-REPL-001 ac-1: one parse rule for the REPL command timeouts configured through
// MCPSERVER_REPL_COMMAND_TIMEOUT_SECONDS (agent stdio requests) and MCPSERVER_REPL_STREAM_COMMAND_TIMEOUT_SECONDS
// (stream commands), so a misconfigured value never fails a request.

using System.Globalization;

namespace QBrainAi.Repl.Core;

/// <summary>
/// FR-MCP-REPL-001 ac-1: parses a configured REPL command timeout. A whole number of seconds from 1 to
/// <see cref="LargestSeconds"/>, the largest delay <see cref="CancellationTokenSource.CancelAfter(TimeSpan)"/> accepts,
/// is used; any other value (unset, empty, not a number, zero, negative or larger) gives <see cref="Default"/>.
/// </summary>
internal static class ReplCommandTimeout
{
    /// <summary>The timeout that applies when no valid value is configured.</summary>
    internal static readonly TimeSpan Default = TimeSpan.FromMinutes(2);

    /// <summary>
    /// The largest whole number of seconds the cancellation timer accepts: its limit is 4294967294 milliseconds.
    /// </summary>
    internal const long LargestSeconds = 4294967;

    /// <summary>Parses a configured value into the timeout that applies.</summary>
    /// <param name="configured">The configured value, or null when the variable is unset.</param>
    /// <returns>The configured timeout when it is valid; otherwise <see cref="Default"/>.</returns>
    internal static TimeSpan Parse(string? configured) =>
        long.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds is >= 1 and <= LargestSeconds
            ? TimeSpan.FromSeconds(seconds)
            : Default;

    /// <summary>Reads the variable that configures a timeout once and parses it; both call sites use this.</summary>
    /// <param name="readVariable">Reads one environment variable by name; the call sites default to the process environment.</param>
    /// <param name="variableName">The variable that configures the timeout.</param>
    /// <returns>The timeout that applies.</returns>
    internal static TimeSpan Read(Func<string, string?> readVariable, string variableName)
    {
        ArgumentNullException.ThrowIfNull(readVariable);
        return Parse(readVariable(variableName));
    }
}
