using System.Diagnostics;
using System.Text;
using System.Text.Json;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.TestSupport.TemporaryInstance;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-TRANSCRIPT-008: verifies the real stdio host can expose and invoke transcript tools. The host is a temporary
/// instance in a test-owned data folder, started with <see cref="OwnedInstanceProcess"/> so no inherited product
/// setting selects another database for it (TR-MCP-NUKE-019 AC-6; hostile validation round 8b sibling sweep of R8B-F3).
/// </summary>
[Trait("Category", "Integration")]
public sealed class TranscriptMcpStdioHostTests
{
    /// <summary>Runs a JSON-RPC stdio session through the built host and normalizes a real Codex fixture.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact]
    public async Task SessionLogNormalizePath_ThroughStdioHost_ResolvesToolGraphAndWritesArtifacts()
    {
        var executablePath = FindStdioExecutable();
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Transcripts",
            "real",
            "codex",
            "session.jsonl");
        Assert.True(File.Exists(fixturePath), $"Fixture not found: {fixturePath}");

        var dataRoot = Path.Combine(Path.GetTempPath(), "mcp-stdio-transcript-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataRoot);
        var dbPath = Path.Combine(dataRoot, "mcp.db");
        var workspaceRoot = Path.Combine(dataRoot, "workspace");
        var relativeFixture = Path.Combine("transcripts", "codex", "session.jsonl");
        var workspaceFixture = Path.Combine(workspaceRoot, relativeFixture);
        Process? process = null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(workspaceFixture)!);
            File.Copy(fixturePath, workspaceFixture, overwrite: true);
            process = StartStdioHost(executablePath, workspaceRoot, dataRoot, dbPath);
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var stdoutTask = CaptureAsync(process.StandardOutput, stdout, TestContext.Current.CancellationToken);
            var stderrTask = CaptureAsync(process.StandardError, stderr, TestContext.Current.CancellationToken);

            foreach (var message in CreateMessages(workspaceRoot, relativeFixture))
            {
                await process.StandardInput.WriteLineAsync(message.AsMemory(), TestContext.Current.CancellationToken).ConfigureAwait(true);
                await process.StandardInput.FlushAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            }

            await WaitForOutputAsync(
                stdout,
                text => text.Contains("compatibilityArtifactPath", StringComparison.Ordinal),
                TimeSpan.FromSeconds(45),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            process.StandardInput.Close();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, timeout.Token);
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(true);

            await stdoutTask.ConfigureAwait(true);
            await stderrTask.ConfigureAwait(true);
            var stdoutText = stdout.ToString();
            var stderrText = stderr.ToString();

            Assert.Equal(0, process.ExitCode);
            Assert.DoesNotContain("threw an unhandled exception", stderrText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sessionlog_ingest_path", stdoutText, StringComparison.Ordinal);
            Assert.Contains("sessionlog_normalize_path", stdoutText, StringComparison.Ordinal);
            Assert.Contains("codex-real-fixture-session", stdoutText, StringComparison.Ordinal);
            Assert.Contains("compatibilityArtifactPath", stdoutText, StringComparison.Ordinal);
        }
        finally
        {
            // MCPT-7: this covers the output wait as well as the final exit wait, assertions and cancellation.
            try
            {
                if (process is not null && !process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    Assert.True(process.WaitForExit(10_000), "Owned stdio host did not exit after its cleanup kill.");
                }
            }
            finally
            {
                process?.Dispose();
                Directory.Delete(dataRoot, recursive: true);
            }
        }
    }

    private static async Task CaptureAsync(StreamReader reader, StringBuilder destination, CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                return;

            lock (destination)
                destination.Append(buffer, 0, read);
        }
    }

    private static async Task WaitForOutputAsync(
        StringBuilder output,
        Func<string, bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string snapshot;
            lock (output)
                snapshot = output.ToString();

            if (predicate(snapshot))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        }

        string finalSnapshot;
        lock (output)
            finalSnapshot = output.ToString();
        throw new TimeoutException($"Timed out waiting for stdio output. Current stdout: {finalSnapshot}");
    }

    private static Process StartStdioHost(string executablePath, string repositoryRoot, string dataRoot, string dbPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = repositoryRoot,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--transport");
        startInfo.ArgumentList.Add("stdio");

        // TR-MCP-NUKE-019 AC-6 (hostile validation finding R8B-F3): the host's database is the one in its owned data
        // folder, set in both the canonical and the legacy form; no inherited product setting reaches the process.
        OwnedInstanceProcess.ApplyOwnedEnvironment(
            startInfo,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["DataFolder"] = dataRoot,
                ["Mcp__Database__Provider"] = "sqlite",
                ["Mcp__Database__Sqlite__DataSource"] = dbPath,
                ["Mcp__Database__Sqlite__ConnectionString"] = $"Data Source={dbPath}",
                ["Mcp__Database__Encryption__Enabled"] = "false",
                ["Mcp__DatabaseProvider"] = "sqlite",
                ["Mcp__DataSource"] = dbPath,
                ["Mcp__DataDirectory"] = dataRoot,
                ["Mcp__RepoRoot"] = repositoryRoot,
                ["Mcp__TodoStorage__Provider"] = "database",
                ["Mcp__TodoStorage__SqliteDataSource"] = dbPath,
                ["Mcp__SessionsPath"] = Path.Combine(dataRoot, "sessions"),
                ["QBrainAi__Database__Provider"] = "sqlite",
                ["QBrainAi__Database__Sqlite__ConnectionString"] = $"Data Source={dbPath}",
                ["Logging__LogLevel__Default"] = "Error",
                ["Logging__LogLevel__Microsoft"] = "Error",
                ["Serilog__MinimumLevel__Default"] = "Error",
            },
            dataRoot);

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start stdio host.");
    }

    private static IEnumerable<string> CreateMessages(string repositoryRoot, string fixturePath)
    {
        yield return Serialize(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "initialize",
            ["params"] = new Dictionary<string, object?>
            {
                ["protocolVersion"] = "2025-03-26",
                ["capabilities"] = new Dictionary<string, object?>(),
                ["clientInfo"] = new Dictionary<string, object?>
                {
                    ["name"] = "mcpserver-stdio-test",
                    ["version"] = "1.0.0",
                },
            },
        });
        yield return Serialize(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "notifications/initialized",
            ["params"] = new Dictionary<string, object?>(),
        });
        yield return Serialize(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 2,
            ["method"] = "tools/list",
        });
        yield return Serialize(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 3,
            ["method"] = "tools/call",
            ["params"] = new Dictionary<string, object?>
            {
                ["name"] = "sessionlog_normalize_path",
                ["arguments"] = new Dictionary<string, object?>
                {
                    ["workspacePath"] = repositoryRoot,
                    ["path"] = fixturePath,
                    ["agent"] = "Codex",
                    ["targetProfile"] = "Grok",
                    ["source"] = "Codex",
                    ["recursive"] = false,
                    ["strict"] = true,
                    ["persist"] = false,
                },
            },
        });
    }

    private static string Serialize(Dictionary<string, object?> message)
    {
        return JsonSerializer.Serialize(message);
    }

    private static string FindStdioExecutable()
    {
        var fileName = OperatingSystem.IsWindows()
            ? "QBrainAi.Support.Mcp.exe"
            : "QBrainAi.Support.Mcp";
        var outputDirectory = Path.GetDirectoryName(typeof(FwhMcpTools).Assembly.Location)
            ?? throw new InvalidOperationException("Could not resolve support MCP assembly directory.");
        var copiedExecutable = Path.Combine(outputDirectory, fileName);
        if (File.Exists(copiedExecutable))
            return copiedExecutable;

        throw new FileNotFoundException("Could not locate the stdio host executable.", copiedExecutable);
    }

}
