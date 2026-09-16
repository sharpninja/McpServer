using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>
/// TEST-MCP-USECASE-020: Windows creation-time process-tree containment contracts on shipped
/// <see cref="ProcessRunner"/>.
/// </summary>
[Collection(RepositoryRootProcessStateCollection.Name)]
public sealed class BugTriage139SixteenthWindowsProcessTests
{
    /// <summary>
    /// The compiled harness must expose its suspended native launcher rather than assigning a job
    /// after ordinary process start.
    /// </summary>
    [Fact]
    public void ProcessRunner_WindowsLaunchContract_UsesSuspendedCreation()
    {
        var launcher = typeof(ProcessRunner).GetNestedType(
            "WindowsSuspendedProcess",
            BindingFlags.NonPublic);

        Assert.NotNull(launcher);
        Assert.NotNull(
            launcher!.GetMethod(
                "Start",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        var source = File.ReadAllText(LocateWindowsProcessRunnerSource());
        Assert.Contains("CreateSuspended", source, StringComparison.Ordinal);
        Assert.Contains("AssignProcessToJobObject", source, StringComparison.Ordinal);
        Assert.Contains("RunWindowsJobContainedAsync", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// A process-creation boundary pause must leave the immediate-descendant helper unable to run
    /// until containment assignment has completed and the suspended primary thread is resumed.
    /// </summary>
    [Fact]
    public async Task ProcessRunner_AssignmentWindow_KeepsImmediateHelperSuspended()
    {
        var hookField = typeof(ProcessRunner).GetField(
            "s_afterWindowsProcessCreationBeforeAssignment",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.Null(hookField!.GetValue(null));

        var helperAssembly = Path.Combine(
            AppContext.BaseDirectory,
            "McpServer.ProcessTree.TestHelper.dll");
        var runtimeConfig = Path.Combine(
            AppContext.BaseDirectory,
            "McpServer.Support.Mcp.Tests.runtimeconfig.json");
        Assert.True(File.Exists(helperAssembly), helperAssembly);
        Assert.True(File.Exists(runtimeConfig), runtimeConfig);

        var pidFile = Path.Combine(
            Path.GetTempPath(),
            "bug139-process-assignment-" + Guid.NewGuid().ToString("N") + ".pid");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<Exception?>? operation = null;
        int? descendantId = null;
        try
        {
            hookField.SetValue(
                null,
                (Action)(() =>
                {
                    entered.Set();
                    release.Wait();
                }));
            operation = Task.Run(
                async () =>
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    return await Record.ExceptionAsync(
                            () => CreateRunner().RunAsync(
                                CreateHelperRequest(helperAssembly, runtimeConfig, pidFile),
                                timeout.Token))
                        .ConfigureAwait(false);
                },
                TestContext.Current.CancellationToken);

            Assert.True(
                entered.Wait(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken));
            await Task.Delay(
                    TimeSpan.FromMilliseconds(500),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.False(
                File.Exists(pidFile),
                "The helper executed before creation-time job assignment completed.");

            release.Set();
            var failure = await operation
                .WaitAsync(
                    TimeSpan.FromSeconds(8),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.IsType<TimeoutException>(failure);
            Assert.True(File.Exists(pidFile), "The resumed helper did not record its descendant.");
            descendantId = int.Parse(
                File.ReadAllText(pidFile),
                NumberStyles.None,
                CultureInfo.InvariantCulture);
            Assert.True(
                IsProcessGone(descendantId.Value),
                $"Descendant process {descendantId.Value} escaped timeout cleanup.");
        }
        finally
        {
            release.Set();
            hookField.SetValue(null, null);
            if (operation is not null)
            {
                _ = await operation
                    .WaitAsync(
                        TimeSpan.FromSeconds(8),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }

            if (descendantId is int processId && !IsProcessGone(processId))
            {
                using var process = Process.GetProcessById(processId);
                process.Kill(entireProcessTree: true);
                Assert.True(process.WaitForExit(5000));
            }

            if (File.Exists(pidFile))
                File.Delete(pidFile);
        }
    }

    /// <summary>
    /// A managed helper that immediately creates a native descendant must be contained and the
    /// recorded descendant must be gone after timeout cleanup.
    /// </summary>
    [Fact]
    public void ProcessRunner_ImmediateNativeDescendant_IsKilledWithinBound()
    {
        var helperAssembly = Path.Combine(
            AppContext.BaseDirectory,
            "McpServer.ProcessTree.TestHelper.dll");
        var runtimeConfig = Path.Combine(
            AppContext.BaseDirectory,
            "McpServer.Support.Mcp.Tests.runtimeconfig.json");
        Assert.True(File.Exists(helperAssembly), helperAssembly);
        Assert.True(File.Exists(runtimeConfig), runtimeConfig);

        var pidFile = Path.Combine(
            Path.GetTempPath(),
            "bug139-process-tree-" + Guid.NewGuid().ToString("N") + ".pid");
        int? descendantId = null;
        try
        {
            var stopwatch = Stopwatch.StartNew();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var failure = Record.Exception(
                () => CreateRunner().RunAsync(
                        CreateHelperRequest(helperAssembly, runtimeConfig, pidFile),
                        timeout.Token)
                    .GetAwaiter()
                    .GetResult());
            stopwatch.Stop();

            Assert.IsType<TimeoutException>(failure);
            Assert.True(
                stopwatch.Elapsed < TimeSpan.FromSeconds(7),
                $"Creation-time descendant cleanup took {stopwatch.Elapsed}.");
            Assert.True(File.Exists(pidFile), "The immediate descendant PID was not recorded.");
            descendantId = int.Parse(
                File.ReadAllText(pidFile),
                NumberStyles.None,
                CultureInfo.InvariantCulture);
            Assert.True(
                IsProcessGone(descendantId.Value),
                $"Descendant process {descendantId.Value} escaped timeout cleanup.");
        }
        finally
        {
            if (descendantId is int processId && !IsProcessGone(processId))
            {
                using var process = Process.GetProcessById(processId);
                process.Kill(entireProcessTree: true);
                Assert.True(process.WaitForExit(5000));
            }

            if (File.Exists(pidFile))
                File.Delete(pidFile);
        }
    }

    /// <summary>
    /// TEST-MCP-USECASE-020: raw <c>Arguments</c> must keep multiple tokens. Wrapping the entire
    /// string as one quoted argument makes <c>git rev-parse --is-inside-work-tree</c> fail.
    /// </summary>
    [Fact]
    public async Task ProcessRunner_RawArguments_PreservesMultipleTokens()
    {
        var runner = CreateRunner();
        var overload = await runner
            .RunAsync("git", "rev-parse --is-inside-work-tree", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var request = await runner
            .RunAsync(
                new ProcessRunRequest(
                    "git",
                    "rev-parse --is-inside-work-tree",
                    WorkingDirectory: SourceRepositoryRoot()),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(0, overload.ExitCode);
        Assert.Equal(0, request.ExitCode);
        Assert.Contains("true", overload.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("true", request.Stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-USECASE-020: raw <c>Arguments</c> and <c>ArgumentList</c> must preserve separate
    /// tokens, quoted spaces, empty arguments, embedded quotes, and trailing backslashes.
    /// </summary>
    [Fact]
    public async Task ProcessRunner_RawArguments_AndArgumentList_PreserveQuotedBoundaries()
    {
        var helperAssembly = Path.Combine(
            AppContext.BaseDirectory,
            "McpServer.ProcessTree.TestHelper.dll");
        var runtimeConfig = Path.Combine(
            AppContext.BaseDirectory,
            "McpServer.Support.Mcp.Tests.runtimeconfig.json");
        Assert.True(File.Exists(helperAssembly), helperAssembly);
        Assert.True(File.Exists(runtimeConfig), runtimeConfig);

        var payload = new[]
        {
            "first",
            "second space",
            string.Empty,
            "quote\"here",
            @"trailing\",
        };
        var listResult = await CreateRunner()
            .RunAsync(
                new ProcessRunRequest(
                    "dotnet",
                    string.Empty,
                    WorkingDirectory: SourceRepositoryRoot(),
                    ArgumentList:
                    [
                        "exec",
                        "--runtimeconfig",
                        runtimeConfig,
                        helperAssembly,
                        "--argv-echo",
                        .. payload,
                    ]),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(0, listResult.ExitCode);
        Assert.Equal(
            payload,
            System.Text.Json.JsonSerializer.Deserialize<string[]>(listResult.Stdout ?? string.Empty));

        var raw = string.Join(
            ' ',
            new[] { "exec", "--runtimeconfig", QuoteWindowsArgument(runtimeConfig), QuoteWindowsArgument(helperAssembly), "--argv-echo" }
                .Concat(payload.Select(QuoteWindowsArgument)));
        var rawResult = await CreateRunner()
            .RunAsync(
                new ProcessRunRequest(
                    "dotnet",
                    raw,
                    WorkingDirectory: SourceRepositoryRoot()),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(0, rawResult.ExitCode);
        Assert.Equal(
            payload,
            System.Text.Json.JsonSerializer.Deserialize<string[]>(rawResult.Stdout ?? string.Empty));
    }

    private static string QuoteWindowsArgument(string argument)
    {
        if (argument.Length > 0 &&
            argument.All(character => !char.IsWhiteSpace(character) && character != '"'))
        {
            return argument;
        }

        var quoted = new StringBuilder(argument.Length + 2);
        quoted.Append('"');
        var backslashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                quoted.Append('\\', backslashes * 2 + 1);
                quoted.Append('"');
                backslashes = 0;
                continue;
            }

            quoted.Append('\\', backslashes);
            quoted.Append(character);
            backslashes = 0;
        }

        quoted.Append('\\', backslashes * 2);
        quoted.Append('"');
        return quoted.ToString();
    }

    private static bool IsProcessGone(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.HasExited;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static string SourceRepositoryRoot()
    {
        var metadata = typeof(BugTriage139SixteenthWindowsProcessTests)
            .Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute =>
                string.Equals(
                    attribute.Key,
                    "McpRepositoryRoot",
                    StringComparison.Ordinal));
        return Path.GetFullPath(
            metadata.Value ??
            throw new InvalidOperationException("Build repository-root metadata is empty."));
    }

    private static string LocateWindowsProcessRunnerSource()
    {
        var path = Path.Combine(
            SourceRepositoryRoot(),
            "src",
            "McpServer.Services",
            "Services",
            "ProcessRunner.Windows.cs");
        Assert.True(File.Exists(path), path);
        return path;
    }

    private static ProcessRunner CreateRunner() =>
        new(
            new DirectProcessEnvironmentService(),
            MsOptions.Create(new ProcessRunnerOptions()),
            NullLogger<ProcessRunner>.Instance);

    private static ProcessRunRequest CreateHelperRequest(
        string helperAssembly,
        string runtimeConfig,
        string pidFile) =>
        new(
            "dotnet",
            string.Empty,
            WorkingDirectory: SourceRepositoryRoot(),
            ArgumentList:
            [
                "exec",
                "--runtimeconfig",
                runtimeConfig,
                helperAssembly,
                pidFile,
            ]);

    private sealed class DirectProcessEnvironmentService : IProcessEnvironmentService
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
}
