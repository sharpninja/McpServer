using System.Text.Json;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace McpServer.Support.Mcp.DatabaseMaintenance;

/// <summary>
/// TR-MCP-SEC-004, TR-MCP-CFG-007: Parses and runs the database-encryption maintenance command without starting the HTTP or STDIO hosts.
/// </summary>
internal static class McpDatabaseEncryptionTransitionCommand
{
    private const string CommandName = "--database-encryption-transition";
    private enum OptionReadState
    {
        NotMatched,
        Matched,
        MissingValue,
    }

    /// <summary>
    /// Parses the database-encryption maintenance command from the supplied argument list.
    /// </summary>
    /// <param name="args">Raw process arguments.</param>
    /// <param name="options">Parsed command options when the command was present and valid.</param>
    /// <param name="error">Usage error text when parsing failed.</param>
    /// <returns><see langword="true"/> when the maintenance command was present, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(
        string[] args,
        out McpDatabaseEncryptionTransitionOptions? options,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = null;
        error = null;

        var markerIndex = Array.FindIndex(
            args,
            static value => value.StartsWith(CommandName, StringComparison.OrdinalIgnoreCase));
        if (markerIndex < 0)
        {
            return false;
        }

        string? operationToken = null;
        var commandToken = args[markerIndex];
        if (commandToken.Length > CommandName.Length && commandToken[CommandName.Length] == '=')
        {
            operationToken = commandToken[(CommandName.Length + 1)..];
        }

        var configurationArguments = new List<string>();
        string? instanceName = null;
        var execute = false;
        string? backupPath = null;
        string? reportPath = null;
        string? currentKey = null;
        string? targetKey = null;
        string? sqliteSeeToolPath = null;
        string? postgreSqlDumpToolPath = null;
        var sqlServerTimeout = TimeSpan.FromMinutes(10);

        for (var index = markerIndex + 1; index < args.Length; index++)
        {
            var argument = args[index];
            if (operationToken is null && !argument.StartsWith("--", StringComparison.Ordinal))
            {
                operationToken = argument;
                continue;
            }

            if (TryReadOption(argument, "--instance", args, ref index, out var instanceValue) is { } instanceState
                && instanceState != OptionReadState.NotMatched)
            {
                if (instanceState == OptionReadState.MissingValue)
                {
                    error = "The --instance option requires a value.";
                    return true;
                }

                instanceName = instanceValue;
                continue;
            }

            if (TryReadOption(argument, "--backup-path", args, ref index, out var backupValue) is { } backupState
                && backupState != OptionReadState.NotMatched)
            {
                if (backupState == OptionReadState.MissingValue)
                {
                    error = "The --backup-path option requires a value.";
                    return true;
                }

                backupPath = backupValue;
                continue;
            }

            if (TryReadOption(argument, "--report-path", args, ref index, out var reportValue) is { } reportState
                && reportState != OptionReadState.NotMatched)
            {
                if (reportState == OptionReadState.MissingValue)
                {
                    error = "The --report-path option requires a value.";
                    return true;
                }

                reportPath = reportValue;
                continue;
            }

            if (TryReadOption(argument, "--current-key", args, ref index, out var currentKeyValue) is { } currentKeyState
                && currentKeyState != OptionReadState.NotMatched)
            {
                if (currentKeyState == OptionReadState.MissingValue)
                {
                    error = "The --current-key option requires a value.";
                    return true;
                }

                currentKey = currentKeyValue;
                continue;
            }

            if (TryReadOption(argument, "--target-key", args, ref index, out var targetKeyValue) is { } targetKeyState
                && targetKeyState != OptionReadState.NotMatched)
            {
                if (targetKeyState == OptionReadState.MissingValue)
                {
                    error = "The --target-key option requires a value.";
                    return true;
                }

                targetKey = targetKeyValue;
                continue;
            }

            if (TryReadOption(argument, "--sqlite-see-tool-path", args, ref index, out var sqliteToolValue) is { } sqliteToolState
                && sqliteToolState != OptionReadState.NotMatched)
            {
                if (sqliteToolState == OptionReadState.MissingValue)
                {
                    error = "The --sqlite-see-tool-path option requires a value.";
                    return true;
                }

                sqliteSeeToolPath = sqliteToolValue;
                continue;
            }

            if (TryReadOption(argument, "--postgres-dump-tool-path", args, ref index, out var postgresDumpValue) is { } postgresDumpState
                && postgresDumpState != OptionReadState.NotMatched)
            {
                if (postgresDumpState == OptionReadState.MissingValue)
                {
                    error = "The --postgres-dump-tool-path option requires a value.";
                    return true;
                }

                postgreSqlDumpToolPath = postgresDumpValue;
                continue;
            }

            if (TryReadOption(argument, "--sqlserver-timeout-seconds", args, ref index, out var timeoutValue) is { } timeoutState
                && timeoutState != OptionReadState.NotMatched)
            {
                if (timeoutState == OptionReadState.MissingValue)
                {
                    error = "The --sqlserver-timeout-seconds option requires a value.";
                    return true;
                }

                if (!int.TryParse(timeoutValue, out var seconds) || seconds <= 0)
                {
                    error = "The --sqlserver-timeout-seconds option must be a positive integer.";
                    return true;
                }

                sqlServerTimeout = TimeSpan.FromSeconds(seconds);
                continue;
            }

            if (string.Equals(argument, "--execute", StringComparison.OrdinalIgnoreCase))
            {
                execute = true;
                continue;
            }

            configurationArguments.Add(argument);
        }

        if (!TryParseOperation(operationToken, out var operation))
        {
            error = GetUsageText();
            return true;
        }

        options = new McpDatabaseEncryptionTransitionOptions
        {
            Operation = operation,
            InstanceName = NormalizeOptionalValue(instanceName)
                ?? NormalizeOptionalValue(Environment.GetEnvironmentVariable("MCP_INSTANCE")),
            Execute = execute,
            BackupPath = backupPath,
            ReportPath = reportPath,
            CurrentKey = currentKey,
            TargetKey = targetKey,
            SqliteSeeToolPath = sqliteSeeToolPath,
            PostgreSqlDumpToolPath = postgreSqlDumpToolPath,
            SqlServerTimeout = sqlServerTimeout,
            ConfigurationArguments = configurationArguments,
        };
        return true;
    }

    /// <summary>
    /// Runs the parsed maintenance command and emits a report to stdout.
    /// </summary>
    /// <param name="options">Parsed command options.</param>
    /// <param name="cancellationToken">Cancellation token for async work.</param>
    /// <returns>Process exit code compatible with shell scripting.</returns>
    public static async Task<int> RunAsync(
        McpDatabaseEncryptionTransitionOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            var configuration = BuildConfiguration(options.ConfigurationArguments);
            ValidateConfiguration(configuration, options.InstanceName);
            var runtimeOptions = McpDatabaseConfigurationResolver.ResolveRuntimeOptions(configuration, options.InstanceName);
            var report = await McpDatabaseEncryptionTransitionRunner.RunAsync(
                runtimeOptions,
                options,
                cancellationToken).ConfigureAwait(false);

            WriteReport(report);

            if (!string.IsNullOrWhiteSpace(options.ReportPath))
            {
                var reportPath = Path.GetFullPath(options.ReportPath);
                var reportDirectory = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrWhiteSpace(reportDirectory))
                {
                    Directory.CreateDirectory(reportDirectory);
                }

                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    WriteIndented = true,
                });
                await File.WriteAllTextAsync(reportPath, json, cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"Report written to {reportPath}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Returns the user-facing usage text for the maintenance command.
    /// </summary>
    /// <returns>Command usage text.</returns>
    public static string GetUsageText()
        => """
        Usage:
          --database-encryption-transition <enable|disable|verify> [--execute]
              [--instance <name>]
              [--backup-path <path>]
              [--report-path <path>]
              [--current-key <key>]
              [--target-key <key>]
              [--sqlite-see-tool-path <path>]
              [--postgres-dump-tool-path <path>]
              [--sqlserver-timeout-seconds <seconds>]

        Notes:
          - Without --execute, the command emits a dry-run transition plan only.
          - When --instance is omitted, MCP_INSTANCE is used if it is set.
          - SQLite disable operations often require --current-key because the new disabled configuration no longer carries the old key.
          - SQL Server backup paths are evaluated by SQL Server on the database host, not by the client process.
        """;

    private static IConfiguration BuildConfiguration(IReadOnlyList<string> configurationArguments)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

        var builder = Host.CreateApplicationBuilder(configurationArguments.ToArray());
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: false)
            .AddYamlFile($"appsettings.{environmentName}.yaml", optional: true, reloadOnChange: false)
            .AddYamlFile(Path.Combine("src", "McpServer.Support.Mcp", "appsettings.yaml"), optional: true, reloadOnChange: false)
            .AddYamlFile(Path.Combine("src", "McpServer.Support.Mcp", $"appsettings.{environmentName}.yaml"), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        if (configurationArguments.Count > 0)
        {
            builder.Configuration.AddCommandLine(configurationArguments.ToArray());
        }

        return builder.Configuration;
    }

    private static bool TryParseOperation(string? value, out McpDatabaseEncryptionTransitionOperation operation)
    {
        operation = default;
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse(value, ignoreCase: true, out operation);
    }

    private static OptionReadState TryReadOption(
        string argument,
        string optionName,
        string[] args,
        ref int index,
        out string? value)
    {
        value = null;
        if (string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return OptionReadState.MissingValue;
            }

            value = args[++index];
            return string.IsNullOrWhiteSpace(value)
                ? OptionReadState.MissingValue
                : OptionReadState.Matched;
        }

        if (argument.StartsWith(optionName + "=", StringComparison.OrdinalIgnoreCase))
        {
            value = argument[(optionName.Length + 1)..];
            return string.IsNullOrWhiteSpace(value)
                ? OptionReadState.MissingValue
                : OptionReadState.Matched;
        }

        return OptionReadState.NotMatched;
    }

    private static string? NormalizeOptionalValue(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static void ValidateConfiguration(IConfiguration configuration, string? instanceName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        McpInstanceResolver.ValidateInstances(configuration);
        if (!string.IsNullOrWhiteSpace(instanceName)
            && !configuration.GetSection($"Mcp:Instances:{instanceName}").Exists())
        {
            throw new InvalidOperationException(
                $"The requested MCP instance '{instanceName}' was not found under Mcp:Instances. Use --instance, MCP_INSTANCE, or root-level database settings that exist.");
        }

        McpInstanceResolver.ValidateTodoStorage(configuration, instanceName);
    }

    private static void WriteReport(McpDatabaseEncryptionTransitionReport report)
    {
        Console.WriteLine($"Provider: {report.Provider}");
        Console.WriteLine($"Operation: {report.Operation}");
        Console.WriteLine($"Mode: {(report.Execute ? "execute" : "plan")}");
        if (!string.IsNullOrWhiteSpace(report.InstanceName))
        {
            Console.WriteLine($"Instance: {report.InstanceName}");
        }

        Console.WriteLine();
        foreach (var step in report.Steps)
        {
            Console.WriteLine($"[{step.Status}] {step.Title}");
            Console.WriteLine(step.Detail);
            if (!string.IsNullOrWhiteSpace(step.CommandText))
            {
                Console.WriteLine(step.CommandText);
            }

            Console.WriteLine();
        }

        foreach (var note in report.Notes)
        {
            Console.WriteLine($"Note: {note}");
        }

        foreach (var warning in report.Warnings)
        {
            Console.WriteLine($"Warning: {warning}");
        }

        if (!string.IsNullOrWhiteSpace(report.Summary))
        {
            Console.WriteLine();
            Console.WriteLine(report.Summary);
        }
    }
}
