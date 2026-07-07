using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// Boots an ephemeral PostgreSQL cluster with generated credentials on a free port for the
/// lifetime of a test class, so PostgreSQL migration tests run everywhere without skip mechanics
/// (Byrd V4: gates pass or fail directly). Honors <c>MCP_TEST_POSTGRES_CONNECTION</c> as an
/// external-server override; otherwise requires local PostgreSQL binaries (an installation under
/// Program Files, or the portable set provisioned by the <c>InstallTestDependencies</c> Nuke
/// target) and fails with that remediation hint when none are found.
/// </summary>
public sealed class EphemeralPostgresFixture : IDisposable
{
    private readonly string? _pgBin;
    private readonly string? _dataDir;

    /// <summary>Server-level connection string (no specific database).</summary>
    public string ServerConnectionString { get; }

    /// <summary>Starts the cluster (or adopts the externally supplied server).</summary>
    public EphemeralPostgresFixture()
    {
        var external = Environment.GetEnvironmentVariable("MCP_TEST_POSTGRES_CONNECTION");
        if (!string.IsNullOrWhiteSpace(external))
        {
            ServerConnectionString = external;
            return;
        }

        _pgBin = FindPostgresBinaries()
            ?? throw new InvalidOperationException(
                "PostgreSQL binaries not found (Program Files\\PostgreSQL\\*\\bin or the test-tools directory). " +
                "Run the 'InstallTestDependencies' Nuke target, or set MCP_TEST_POSTGRES_CONNECTION.");

        _dataDir = Path.Combine(Path.GetTempPath(), $"mcp-test-pgdata-{Guid.NewGuid():N}");
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        var port = GetFreeTcpPort();

        var passwordFile = Path.Combine(Path.GetTempPath(), $"mcp-test-pgpw-{Guid.NewGuid():N}.txt");
        File.WriteAllText(passwordFile, password);
        try
        {
            RunTool(Path.Combine(_pgBin, "initdb.exe"),
                $"-D \"{_dataDir}\" -U mcptest --auth=scram-sha-256 --pwfile=\"{passwordFile}\" -E UTF8");
        }
        finally
        {
            File.Delete(passwordFile);
        }

        // pg_ctl must run without output redirection: the postgres child inherits redirected
        // handles and the pipe never closes, hanging the caller even after -w returns.
        RunTool(Path.Combine(_pgBin, "pg_ctl.exe"),
            $"start -D \"{_dataDir}\" -w -o \"-p {port} -c listen_addresses=localhost\" -l \"{Path.Combine(_dataDir, "pg.log")}\"");

        ServerConnectionString = $"Host=localhost;Port={port};Username=mcptest;Password={password};Database=postgres";
    }

    /// <summary>Stops the cluster and deletes its data directory.</summary>
    public void Dispose()
    {
        if (_pgBin is null || _dataDir is null)
            return;
        try
        {
            RunTool(Path.Combine(_pgBin, "pg_ctl.exe"), $"stop -D \"{_dataDir}\" -m immediate -w");
        }
        catch (InvalidOperationException)
        {
            // Best-effort shutdown; the directory delete below surfaces persistent problems.
        }

        try
        {
            Directory.Delete(_dataDir, recursive: true);
        }
        catch (IOException)
        {
            // Uniquely named temp directory; leave for OS cleanup if a handle lingers.
        }
    }

    private static string? FindPostgresBinaries()
    {
        var installedRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL");
        if (Directory.Exists(installedRoot))
        {
            var installedBin = Directory
                .EnumerateDirectories(installedRoot)
                .OrderByDescending(d => d)
                .Select(d => Path.Combine(d, "bin"))
                .FirstOrDefault(bin => File.Exists(Path.Combine(bin, "initdb.exe")));
            if (installedBin is not null)
                return installedBin;
        }

        var toolsBin = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "McpServer", "test-tools", "pgsql", "bin");
        return File.Exists(Path.Combine(toolsBin, "initdb.exe")) ? toolsBin : null;
    }

    private static void RunTool(string fileName, string arguments)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} failed with exit code {process.ExitCode}.");
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
