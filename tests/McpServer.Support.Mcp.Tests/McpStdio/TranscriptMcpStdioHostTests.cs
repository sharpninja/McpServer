using System.Diagnostics;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.McpStdio;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-TRANSCRIPT-008: verifies the real stdio host can expose and invoke transcript tools.
/// </summary>
public sealed class TranscriptMcpStdioHostTests
{
    /// <summary>Runs a JSON-RPC stdio session through the built host and normalizes a real Codex fixture.</summary>
    [Fact]
    public async Task SessionLogNormalizePath_ThroughStdioHost_ResolvesToolGraphAndWritesArtifacts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var executablePath = FindStdioExecutable(repositoryRoot);
        var fixturePath = Path.Combine(
            repositoryRoot,
            "tests",
            "McpServer.Support.Mcp.Tests",
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
        Directory.CreateDirectory(Path.GetDirectoryName(workspaceFixture)!);
        File.Copy(fixturePath, workspaceFixture, overwrite: true);

        using var process = StartStdioHost(executablePath, workspaceRoot, dataRoot, dbPath);
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
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

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
        startInfo.Environment["DataFolder"] = dataRoot;
        startInfo.Environment["Mcp__Database__Provider"] = "sqlite";
        startInfo.Environment["Mcp__Database__Sqlite__DataSource"] = dbPath;
        startInfo.Environment["Mcp__Database__Encryption__Enabled"] = "false";
        startInfo.Environment["Mcp__DatabaseProvider"] = "sqlite";
        startInfo.Environment["Mcp__DataSource"] = dbPath;
        startInfo.Environment["Mcp__DataDirectory"] = dataRoot;
        startInfo.Environment["Mcp__RepoRoot"] = repositoryRoot;
        startInfo.Environment["Mcp__TodoStorage__Provider"] = "database";
        startInfo.Environment["Mcp__TodoStorage__SqliteDataSource"] = dbPath;
        startInfo.Environment["Mcp__SessionsPath"] = Path.Combine(dataRoot, "sessions");
        startInfo.Environment["Logging__LogLevel__Default"] = "Error";
        startInfo.Environment["Logging__LogLevel__Microsoft"] = "Error";
        startInfo.Environment["Serilog__MinimumLevel__Default"] = "Error";

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

    private static string FindStdioExecutable(string repositoryRoot)
    {
        var fileName = OperatingSystem.IsWindows()
            ? "McpServer.Support.Mcp.exe"
            : "McpServer.Support.Mcp";
        var outputDirectory = Path.GetDirectoryName(typeof(FwhMcpTools).Assembly.Location)
            ?? throw new InvalidOperationException("Could not resolve support MCP assembly directory.");
        var copiedExecutable = Path.Combine(outputDirectory, fileName);
        if (File.Exists(copiedExecutable))
            return copiedExecutable;

        var sourceExecutable = Path.Combine(
            repositoryRoot,
            "src",
            "McpServer.Support.Mcp",
            "bin",
            "Debug",
            "net10.0",
            fileName);
        if (File.Exists(sourceExecutable))
            return sourceExecutable;

        throw new FileNotFoundException("Could not locate the stdio host executable.", copiedExecutable);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln"))
                && Directory.Exists(Path.Combine(directory.FullName, "src"))
                && Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate McpServer repository root.");
    }
}
