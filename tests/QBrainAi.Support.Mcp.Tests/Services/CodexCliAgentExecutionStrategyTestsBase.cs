using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-003; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008: the per-test state and helpers that both halves of CodexCliAgentExecutionStrategyTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGE-003; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from CodexCliAgentExecutionStrategyTests (PrimaryTempName, CreateRequest, CapturingProcessEnvironmentService, CapturingProcessSpawner and FakeSpawnedProcess) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class CodexCliAgentExecutionStrategyTestsBase
{
    /// <summary>
    /// The CLI temp variable the Codex strategy reads first; tests point it at the test folder and every other CLI temp
    /// variable at a fallback folder, so an output path under the test folder proves it won.
    /// </summary>
    internal const string PrimaryTempName = "MCPSERVER_CODEX_ONESHOT_TEMP";

    internal static AgentExecutionSessionRequest CreateRequest(
        string agentPath = "codex.cmd",
        Func<string, string, Task>? outputReceivedAsync = null) =>
        new(
            "rendered prompt",
            "Q:\\__mcp_unit_test__\\QBrainAi",
            "triage",
            "codex-cli",
            new AgentCliClientOptions
            {
                AgentPath = agentPath,
                Model = "model-triage",
                WorkingDirectory = "Q:\\__mcp_unit_test__\\QBrainAi",
                Timeout = TimeSpan.FromSeconds(30),
                AgentOutputReceivedAsync = outputReceivedAsync,
            });

    /// <summary>A process environment service that leaves every start info unchanged and resolves an executable to the name it is given, so a launch is captured as the strategy built it.</summary>
    /// <remarks>2026-11-07, TEST-MCP-TRIAGE-003; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: summary added (slice S8, hostile validation finding R14-F4): this helper type became internal when slice S7 moved it into this shared base class, so it is documented like the other shared members; its code is unchanged.</remarks>
    internal sealed class CapturingProcessEnvironmentService : IProcessEnvironmentService
    {
        public void ApplyGitHubToken(ProcessStartInfo psi, string? token)
        {
        }

        public void ApplyRunAsEnvironment(ProcessStartInfo psi, string? runAsUser)
        {
        }

        public void ApplyAll(ProcessStartInfo psi, string? runAsUser, string? gitHubToken)
        {
        }

        public string ResolveExecutable(ProcessStartInfo psi, string fileName) => fileName;
    }

    /// <summary>A process spawner that records the start info and the output paths of the launch and returns a scripted FakeSpawnedProcess with the given exit code and streams; no process starts.</summary>
    /// <remarks>2026-11-07, TEST-MCP-TRIAGE-003; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: summary added (slice S8, hostile validation finding R14-F4): this helper type became internal when slice S7 moved it into this shared base class, so it is documented like the other shared members; its code is unchanged.</remarks>
    internal sealed class CapturingProcessSpawner(
        int exitCode,
        string outputBody = "",
        string stdout = "",
        string stderr = "",
        bool waitForCancellation = false) : IProcessSpawner
    {
        public ProcessStartInfo? StartInfo { get; private set; }

        public string? OutputLastMessagePath { get; private set; }

        public string? OutputSchemaPath { get; private set; }

        public string? OutputSchemaJson { get; private set; }

        public string? CapturedStandardInput => LastProcess?.StandardInputText;

        private FakeSpawnedProcess? LastProcess { get; set; }

        public ISpawnedProcess Spawn(ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            OutputLastMessagePath = GetArgumentValue(startInfo, "-o");
            OutputSchemaPath = GetArgumentValue(startInfo, "--output-schema");
            if (!string.IsNullOrWhiteSpace(OutputSchemaPath) && File.Exists(OutputSchemaPath))
            {
                OutputSchemaJson = File.ReadAllText(OutputSchemaPath, Encoding.UTF8);
            }

            if (!string.IsNullOrWhiteSpace(OutputLastMessagePath) && outputBody.Length > 0)
            {
                File.WriteAllText(OutputLastMessagePath, outputBody, Encoding.UTF8);
            }

            LastProcess = new FakeSpawnedProcess(exitCode, stdout, stderr, waitForCancellation);
            return LastProcess;
        }

        public bool HasArgument(string name)
        {
            if (StartInfo is null)
                return false;

            return StartInfo.ArgumentList.Any(argument => string.Equals(argument, name, StringComparison.Ordinal))
                || StartInfo.ArgumentList.Any(argument => Regex.IsMatch(
                    argument,
                    "(^|\\s)" + Regex.Escape(name) + "(\\s|$)",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(100)));
        }

        private static string? GetArgumentValue(ProcessStartInfo startInfo, string name)
        {
            for (var index = 0; index < startInfo.ArgumentList.Count - 1; index++)
            {
                if (string.Equals(startInfo.ArgumentList[index], name, StringComparison.Ordinal))
                    return startInfo.ArgumentList[index + 1];
            }

            var pattern = Regex.Escape(name) + "\\s+(?:\"(?<quoted>[^\"]+)\"|(?<plain>\\S+))";
            foreach (var argument in startInfo.ArgumentList)
            {
                var match = Regex.Match(argument, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                if (match.Success)
                    return match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["plain"].Value;
            }

            return null;
        }
    }

    /// <summary>A spawned process double with a scripted exit code, standard output and standard error, which keeps what is written to its standard input and can wait for cancellation instead of exiting.</summary>
    /// <remarks>2026-11-07, TEST-MCP-TRIAGE-003; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: summary added (slice S8, hostile validation finding R14-F4): this helper type became internal when slice S7 moved it into this shared base class, so it is documented like the other shared members; its code is unchanged.</remarks>
    internal sealed class FakeSpawnedProcess : ISpawnedProcess
    {
        private readonly MemoryStream _standardInput = new();
        private bool _killed;

        public FakeSpawnedProcess(int exitCode, string stdout, string stderr, bool waitForCancellation)
        {
            ExitCode = exitCode;
            StandardOutput = CreateReader(stdout);
            StandardError = CreateReader(stderr);
            WaitForCancellation = waitForCancellation;
            StandardInput = new StreamWriter(_standardInput, Encoding.UTF8, leaveOpen: true);
        }

        public StreamReader StandardOutput { get; }

        public StreamReader StandardError { get; }

        public StreamWriter? StandardInput { get; }

        public int Id => 1234;

        public bool HasExited => !WaitForCancellation || _killed;

        public int ExitCode { get; }

        public string StandardInputText => Encoding.UTF8.GetString(_standardInput.ToArray()).TrimStart('\uFEFF');

        private bool WaitForCancellation { get; }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            WaitForCancellation ? Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken) : Task.CompletedTask;

        public void Kill()
        {
            _killed = true;
        }

        public void Dispose()
        {
            StandardInput?.Dispose();
            StandardOutput.Dispose();
            StandardError.Dispose();
            _standardInput.Dispose();
        }

        private static StreamReader CreateReader(string value) =>
            new(new MemoryStream(Encoding.UTF8.GetBytes(value)), Encoding.UTF8);
    }
}
