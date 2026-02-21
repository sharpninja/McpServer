namespace McpServer.Common.Copilot;

/// <summary>Client for invoking Copilot CLI.</summary>
public interface ICopilotClient
{
    /// <summary>Invokes the Copilot CLI with the given prompt.</summary>
    Task<CopilotResult> InvokeAsync(string prompt, CopilotClientOptions? options = null, CancellationToken ct = default);

    /// <summary>Invokes the Copilot CLI and deserializes the result.</summary>
    Task<CopilotResult<T>> InvokeAsync<T>(string prompt, CopilotClientOptions? options = null, CancellationToken ct = default);
}

/// <summary>Options for configuring the Copilot CLI invocation.</summary>
public sealed class CopilotClientOptions
{
    /// <summary>Path to the copilot agent script.</summary>
    public string? AgentPath { get; set; }
    /// <summary>Model to use.</summary>
    public string? Model { get; set; }
    /// <summary>Output format.</summary>
    public string? OutputFormat { get; set; }
    /// <summary>Timeout for the invocation.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
    /// <summary>Working directory for the process.</summary>
    public string? WorkingDirectory { get; set; }
    /// <summary>Additional environment variables.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
}

/// <summary>Result of a Copilot CLI invocation.</summary>
public sealed class CopilotResult
{
    /// <summary>Whether the invocation succeeded.</summary>
    public bool Success { get; set; }
    /// <summary>Raw stdout output.</summary>
    public string? Output { get; set; }
    /// <summary>Response body content.</summary>
    public string? Body { get; set; }
    /// <summary>Stderr output.</summary>
    public string? Stderr { get; set; }
    /// <summary>Error message if failed.</summary>
    public string? Error { get; set; }
    /// <summary>Process exit code.</summary>
    public int ExitCode { get; set; }
    /// <summary>State of the invocation.</summary>
    public CopilotResultState State { get; set; }
}

/// <summary>State of a Copilot CLI result.</summary>
public enum CopilotResultState
{
    /// <summary>Not yet run.</summary>
    Unknown,
    /// <summary>Completed successfully.</summary>
    Success,
    /// <summary>Failed.</summary>
    Failed,
    /// <summary>Timed out.</summary>
    TimedOut
}

/// <summary>Typed result of a Copilot CLI invocation.</summary>
public sealed class CopilotResult<T>
{
    /// <summary>Whether the invocation succeeded.</summary>
    public bool Success { get; set; }
    /// <summary>Deserialized result.</summary>
    public T? Data { get; set; }
    /// <summary>Raw output.</summary>
    public string? Output { get; set; }
    /// <summary>Error message.</summary>
    public string? Error { get; set; }
    /// <summary>Exit code.</summary>
    public int ExitCode { get; set; }
}

/// <summary>Default no-op Copilot client implementation.</summary>
public sealed class CopilotClient(Microsoft.Extensions.Options.IOptions<CopilotClientOptions> options, Microsoft.Extensions.Logging.ILogger<CopilotClient> logger) : ICopilotClient
{
    public Task<CopilotResult> InvokeAsync(string prompt, CopilotClientOptions? opts = null, CancellationToken ct = default)
        => Task.FromResult(new CopilotResult { Success = false, Error = "Copilot CLI not configured.", ExitCode = -1, State = CopilotResultState.Failed });

    public Task<CopilotResult<T>> InvokeAsync<T>(string prompt, CopilotClientOptions? opts = null, CancellationToken ct = default)
        => Task.FromResult(new CopilotResult<T> { Success = false, Error = "Copilot CLI not configured.", ExitCode = -1 });
}
