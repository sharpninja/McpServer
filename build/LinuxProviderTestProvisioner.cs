using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

/// <summary>
/// TR-MCP-NUKE-018: Production Linux provisioning of the provider test endpoints (operator decision 2026-10-11,
/// BUG-BASELINE-005). Each variable already set is adopted; otherwise it is loaded from the test-database env file and
/// its dedicated qbrain-test container is started when stopped. Every endpoint must accept TCP before tests run.
/// Actions and errors name variables, the file, containers and host:port only, never connection-string values.
/// </summary>
sealed class LinuxProviderTestProvisioner : IProviderTestProvisioner
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);
    private const int ProbeAttempts = 30;

    private static readonly (string Variable, string Container, int DefaultPort, bool IsSqlServer)[] Endpoints =
    [
        (ProviderTestNames.PostgresVariable, ProviderTestNames.PostgresContainer, 5432, false),
        (ProviderTestNames.SqlServerVariable, ProviderTestNames.SqlServerContainer, 1433, true),
    ];

    /// <inheritdoc />
    public ProviderTestProvisioningResult Provision(string envFilePath, IProviderTestHost host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(envFilePath);
        ArgumentNullException.ThrowIfNull(host);
        var actions = new List<string>();
        var errors = new List<string>();
        Dictionary<string, string>? fileValues = null;
        var fileMissing = false;

        foreach (var (variable, container, defaultPort, isSqlServer) in Endpoints)
        {
            var value = host.GetVariable(variable);
            var loadedFromFile = false;
            if (!string.IsNullOrWhiteSpace(value))
            {
                actions.Add("adopt " + variable + " (already set)");
            }
            else
            {
                if (fileValues is null && !fileMissing)
                {
                    var text = host.ReadFile(envFilePath);
                    if (text is null)
                    {
                        fileMissing = true;
                        errors.Add("test-database env file not found: " + envFilePath);
                    }
                    else
                    {
                        fileValues = ParseEnvFile(text);
                    }
                }

                if (fileMissing)
                {
                    continue;
                }

                if (!fileValues!.TryGetValue(variable, out value) || string.IsNullOrWhiteSpace(value))
                {
                    errors.Add(variable + " is not defined in " + envFilePath);
                    continue;
                }

                host.SetVariable(variable, value);
                actions.Add("load " + variable + " from " + envFilePath);
                loadedFromFile = true;
            }

            var startedNow = false;
            if (loadedFromFile)
            {
                var state = host.GetContainerState(container);
                if (state == ProviderTestContainerState.Missing)
                {
                    errors.Add("container " + container + " not found");
                    continue;
                }

                if (state == ProviderTestContainerState.Stopped)
                {
                    if (!host.StartContainer(container))
                    {
                        errors.Add("container " + container + " could not be started");
                        continue;
                    }

                    actions.Add("start " + container);
                    startedNow = true;
                }
            }

            if (!TryReadEndpoint(value, isSqlServer, defaultPort, out var endpointHost, out var port, out var problem))
            {
                errors.Add(variable + " " + problem);
                continue;
            }

            var endpoint = endpointHost + ":" + port.ToString(CultureInfo.InvariantCulture);
            var attempts = startedNow ? ProbeAttempts : 1;
            var reachable = false;
            for (var attempt = 0; attempt < attempts && !reachable; attempt++)
            {
                reachable = host.CanConnect(endpointHost, port, ProbeTimeout);
                if (!reachable && attempt + 1 < attempts)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }

            if (reachable)
            {
                actions.Add("endpoint " + endpoint + " reachable");
            }
            else
            {
                errors.Add("endpoint " + endpoint + " for " + variable + " is not accepting connections");
            }
        }

        return new ProviderTestProvisioningResult(actions, errors);
    }

    /// <summary>Parses KEY=VALUE lines; skips blanks and comments; strips an export prefix and matching quotes.</summary>
    internal static Dictionary<string, string> ParseEnvFile(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].TrimStart();
            }

            var equals = line.IndexOf('=', StringComparison.Ordinal);
            if (equals <= 0)
            {
                continue;
            }

            var key = line[..equals].Trim();
            var value = line[(equals + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
            {
                value = value[1..^1];
            }

            values[key] = value;
        }

        return values;
    }

    private static bool TryReadEndpoint(string connectionString, bool isSqlServer, int defaultPort, out string host, out int port, out string problem)
    {
        host = string.Empty;
        port = defaultPort;
        problem = string.Empty;
        var builder = new DbConnectionStringBuilder();
        try
        {
            builder.ConnectionString = connectionString;
        }
        catch (ArgumentException)
        {
            problem = "is not a valid connection string";
            return false;
        }

        if (isSqlServer)
        {
            var server = FirstValue(builder, "Server", "Data Source", "Address", "Addr", "Network Address");
            if (server is null)
            {
                problem = "names no server";
                return false;
            }

            if (server.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase))
            {
                problem = "names a LocalDB server, which has no TCP endpoint on Linux";
                return false;
            }

            if (server.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
            {
                server = server[4..];
            }

            var comma = server.IndexOf(',', StringComparison.Ordinal);
            if (comma >= 0)
            {
                if (!int.TryParse(server[(comma + 1)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out port))
                {
                    problem = "names a server with an unreadable port";
                    return false;
                }

                server = server[..comma];
            }

            host = server.Split('\\')[0].Trim();
        }
        else
        {
            var server = FirstValue(builder, "Host", "Server");
            if (server is null)
            {
                problem = "names no host";
                return false;
            }

            host = server.Split(',')[0].Trim();
            var portText = FirstValue(builder, "Port");
            if (portText is not null && !int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out port))
            {
                problem = "names an unreadable port";
                return false;
            }
        }

        if (host.Length == 0 || host == ".")
        {
            host = "localhost";
        }

        return true;
    }

    private static string? FirstValue(DbConnectionStringBuilder builder, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (builder.TryGetValue(key, out var value) && value is not null && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return value.ToString()!.Trim();
            }
        }

        return null;
    }
}

/// <summary>
/// TR-MCP-NUKE-018: Real host boundary: process environment, file system, docker CLI (inspect and start only) and TCP.
/// </summary>
sealed class SystemProviderTestHost : IProviderTestHost
{
    /// <inheritdoc />
    public string? GetVariable(string name) => Environment.GetEnvironmentVariable(name);

    /// <inheritdoc />
    public void SetVariable(string name, string value) => Environment.SetEnvironmentVariable(name, value);

    /// <inheritdoc />
    public string? ReadFile(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    /// <inheritdoc />
    public ProviderTestContainerState GetContainerState(string containerName)
    {
        var (exitCode, output) = Docker("inspect", "--format", "{{.State.Status}}", containerName);
        if (exitCode != 0)
        {
            return ProviderTestContainerState.Missing;
        }

        return string.Equals(output.Trim(), "running", StringComparison.Ordinal)
            ? ProviderTestContainerState.Running
            : ProviderTestContainerState.Stopped;
    }

    /// <inheritdoc />
    public bool StartContainer(string containerName)
        => Docker("start", containerName).ExitCode == 0
           && GetContainerState(containerName) == ProviderTestContainerState.Running;

    /// <inheritdoc />
    public bool CanConnect(string host, int port, TimeSpan timeout)
    {
        try
        {
            using var client = new TcpClient();
            return client.ConnectAsync(host, port).Wait(timeout) && client.Connected;
        }
        catch (Exception exception) when (exception is SocketException or AggregateException)
        {
            return false;
        }
    }

    private static (int ExitCode, string Output) Docker(params string[] arguments)
    {
        var start = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start docker.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
        {
            process.Kill(entireProcessTree: true);
            return (-1, string.Empty);
        }

        _ = error.GetAwaiter().GetResult();
        return (process.ExitCode, output.GetAwaiter().GetResult());
    }
}
