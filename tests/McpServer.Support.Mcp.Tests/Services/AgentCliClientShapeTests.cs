using System.Diagnostics;
using System.Text;

using McpServer.Common.AgentCli;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class AgentCliClientShapeTests
{
    [Fact]
    public async Task InvokeAsync_WithDefaultClineAgent_UsesClineCommandShape()
    {
        CapturingProcessSpawner spawner = new(stdout: "cline response");
        AgentCliClient client = new(
            new StaticOptionsMonitor<AgentCliClientOptions>(new AgentCliClientOptions
            {
                WorkingDirectory = "F:\\GitHub\\McpServer",
                Model = "model-should-not-be-sent-to-cline",
            }),
            new CapturingProcessEnvironmentService(),
            spawner,
            NullLogger<AgentCliClient>.Instance);

        AgentCliResult result = await client.InvokeAsync("rendered prompt");

        Assert.Equal(AgentCliResultState.Success, result.State);
        Assert.Equal("cline response", result.Body);
        Assert.NotNull(spawner.StartInfo);
        Assert.Equal("cline", spawner.StartInfo!.FileName);
        Assert.Contains("-p", spawner.StartInfo.ArgumentList);
        Assert.Equal("F:\\GitHub\\McpServer", GetArgumentAfter(spawner.StartInfo, "-c"));
        Assert.Equal("xhigh", GetArgumentAfter(spawner.StartInfo, "--thinking"));
        Assert.Contains("rendered prompt", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--model", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--silent", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--stream", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--yolo", spawner.StartInfo.ArgumentList);
    }

    [Fact]
    public void CreateInteractiveSession_WithDefaultClineAgent_UsesClineInteractiveCommandShape()
    {
        CapturingProcessSpawner spawner = new(stdout: "cline response");
        AgentCliClient client = new(
            new StaticOptionsMonitor<AgentCliClientOptions>(new AgentCliClientOptions
            {
                WorkingDirectory = "F:\\GitHub\\McpServer",
                Model = "model-should-not-be-sent-to-cline",
            }),
            new CapturingProcessEnvironmentService(),
            spawner,
            NullLogger<AgentCliClient>.Instance);

        using AgentCliInteractiveSession session = client.CreateInteractiveSession("rendered prompt");

        Assert.NotNull(session);
        Assert.NotNull(spawner.StartInfo);
        Assert.Equal("cline", spawner.StartInfo!.FileName);
        Assert.DoesNotContain("-p", spawner.StartInfo.ArgumentList);
        Assert.Equal("F:\\GitHub\\McpServer", GetArgumentAfter(spawner.StartInfo, "-c"));
        Assert.Equal("xhigh", GetArgumentAfter(spawner.StartInfo, "--thinking"));
        Assert.Contains("rendered prompt", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--model", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--silent", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--stream", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--yolo", spawner.StartInfo.ArgumentList);
    }

    private static string? GetArgumentAfter(ProcessStartInfo startInfo, string argument)
    {
        int index = startInfo.ArgumentList.IndexOf(argument);
        return index >= 0 && index + 1 < startInfo.ArgumentList.Count
            ? startInfo.ArgumentList[index + 1]
            : null;
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<T, string?> listener) => new NoopDisposable();
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class CapturingProcessEnvironmentService : IProcessEnvironmentService
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

    private sealed class CapturingProcessSpawner(string stdout = "", string stderr = "") : IProcessSpawner
    {
        public ProcessStartInfo? StartInfo { get; private set; }

        public ISpawnedProcess Spawn(ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            return new CapturingSpawnedProcess(stdout, stderr);
        }
    }

    private sealed class CapturingSpawnedProcess : ISpawnedProcess
    {
        public CapturingSpawnedProcess(string stdout, string stderr)
        {
            StandardOutput = CreateReader(stdout);
            StandardError = CreateReader(stderr);
        }

        public StreamReader StandardOutput { get; }

        public StreamReader StandardError { get; }

        public StreamWriter? StandardInput => null;

        public int Id => 1234;

        public bool HasExited => true;

        public int ExitCode { get; } = 0;

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Kill()
        {
        }

        public void Dispose()
        {
            StandardOutput.Dispose();
            StandardError.Dispose();
        }

        private static StreamReader CreateReader(string value) =>
            new(new MemoryStream(Encoding.UTF8.GetBytes(value)), Encoding.UTF8);
    }
}
