// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>TEST-MCP-TRANSCRIPT-008/TEST-QB-069: binds input-only scripted reader tasks to the actual fixture drain operation.</summary>
public static class OwnedStdioReaderFixture
{
    /// <summary>Runs actual Task and cancellation behavior without starting a process or accepting expected observations.</summary>
    /// <param name="stimulusJson">Only owned child state, initial cancellation state and two scripted reader responses.</param>
    /// <returns>Effects, actual exception descriptions and observed token state.</returns>
    public static async Task<string> ObserveAsync(string stimulusJson)
    {
        using var document = JsonDocument.Parse(stimulusJson);
        var input = document.RootElement;
        using var owned = new CancellationTokenSource();
        using var original = new CancellationTokenSource();
        if (input.GetProperty("initiallyRequested").GetBoolean())
            await owned.CancelAsync().ConfigureAwait(false);
        var effects = new List<string>();
        var failures = new List<Exception>();
        var readers = input.GetProperty("readers").EnumerateArray().ToArray();
        Task Read(int index)
        {
            var response = readers[index];
            var kind = response.GetProperty("kind").GetString();
            var message = response.GetProperty("message").GetString();
            if (kind == "ok") return Task.CompletedTask;
            Exception exception = kind switch
            {
                "io" => new IOException(message),
                "other" => new InvalidOperationException(message),
                "cancel" => new OperationCanceledException(message, null,
                    response.GetProperty("token").GetString() switch
                    {
                        "owned" => owned.Token,
                        "foreign" => original.Token,
                        "none" => CancellationToken.None,
                        _ => throw new InvalidOperationException("Unknown reader token response."),
                    }),
                _ => throw new InvalidOperationException("Unknown reader fault response."),
            };
            return Task.FromException(exception);
        }
        await OwnedStdioReaderDrain.DrainAsync(
            () => input.GetProperty("alive").GetBoolean(), owned,
            () => Read(0), () => Read(1), failures, effects.Add).ConfigureAwait(false);
        return JsonSerializer.Serialize(new
        {
            effects,
            failures = failures.Select(exception => exception.GetType().Name + ":" + exception.Message).ToArray(),
            cancelRequested = owned.IsCancellationRequested,
            originalCancelled = original.IsCancellationRequested,
        });
    }
}
