// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-TRANSCRIPT-008/TEST-QB-069: private draft binding of the same PS scenario
/// runner to actual diagnostic orchestration and scripted child dependencies.
/// No case names or expected observations enter this boundary; it is not compiled.
/// </summary>
public static class OwnedStdioDiagnosticFixture
{
    /// <summary>Executes actual orchestration, deriving observations from dependency effects.</summary>
    /// <param name="stimulusJson">Only scripted child identity, reader chunks and dependency faults.</param>
    /// <returns>Actual helper observations with no manufactured passing result.</returns>
    public static async Task<string> ObserveAsync(string stimulusJson)
    {
        using var document = JsonDocument.Parse(stimulusJson);
        var input = document.RootElement;
        var pid = input.GetProperty("ownedPid").GetInt32();
        var alive = false;
        var inputClosed = false;
        var stdout = "";
        var stderr = "";
        string? Text(string key) => input.GetProperty(key).GetString();
        static Exception Fault(string value)
        {
            var split = value.IndexOf(':');
            var type = value[..split];
            var message = value[(split + 1)..];
            return type switch
            {
                nameof(IOException) => new IOException(message),
                nameof(TimeoutException) => new TimeoutException(message),
                nameof(OperationCanceledException) => new OperationCanceledException(message),
                "Win32Exception" => new System.ComponentModel.Win32Exception(message),
                _ => new InvalidOperationException("Unknown scripted dependency fault: " + type),
            };
        }
        Task<bool> Step(string stage, int? seconds)
        {
            switch (stage)
            {
                case "start": alive = true; break;
                case "capture-both":
                    stdout = string.Concat(input.GetProperty("stdoutChunks").EnumerateArray().Select(value => value.GetString()));
                    stderr = string.Concat(input.GetProperty("stderrChunks").EnumerateArray().Select(value => value.GetString()));
                    break;
                case "send": if (Text("sendFailure") is { } send) throw Fault(send); break;
                case "wait-output": if (Text("outputFailure") is { } output) throw Fault(output); break;
                case "close-input": inputClosed = true; break;
                case "wait-exit":
                    if (Text("exitFailure") is { } exit) throw Fault(exit);
                    alive = false;
                    break;
                case "inspect-owned-exit":
                    if (input.GetProperty("exitedBeforeCleanup").GetBoolean()) alive = false;
                    return Task.FromResult(!alive);
                case "kill-owned-tree":
                    if (Text("killFailure") is { } kill) throw Fault(kill);
                    alive = false;
                    break;
                case "verify-owned-exit":
                    if (alive) throw new InvalidOperationException("Owned child remains alive");
                    break;
                case "drain": if (Text("captureFailure") is { } capture) throw Fault(capture); break;
                default: throw new InvalidOperationException("Unrecognized dependency operation: " + stage);
            }
            return Task.FromResult(false);
        }
        var observed = await OwnedStdioDiagnosticBinding.RunAsync(Step, () => pid, () => stdout, () => stderr, () => inputClosed, () => alive).ConfigureAwait(false);
        return JsonSerializer.Serialize(observed, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
