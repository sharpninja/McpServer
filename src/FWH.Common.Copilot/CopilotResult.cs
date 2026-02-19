namespace FWH.Common.Copilot;

/// <summary>TR-CLI-001: Overall state of a Copilot CLI invocation.</summary>
public enum CopilotResultState
{
    /// <summary>The CLI process exited with code 0.</summary>
    Success,

    /// <summary>The CLI process exited with a non-zero exit code.</summary>
    Error,

    /// <summary>The CLI process timed out.</summary>
    Timeout,

    /// <summary>The CLI process could not be spawned (e.g. binary not found).</summary>
    SpawnError,
}

/// <summary>TR-CLI-001: Detected content type of the CLI output.</summary>
public enum CopilotContentType
{
    /// <summary>Plain text output.</summary>
    Text,

    /// <summary>JSON output that was successfully deserialized.</summary>
    Json,

    /// <summary>YAML output that was successfully deserialized.</summary>
    Yaml,
}

/// <summary>TR-CLI-001: Structured result from invoking the Copilot CLI.</summary>
public sealed class CopilotResult
{
    /// <summary>Overall state of the invocation.</summary>
    public CopilotResultState State { get; init; }

    /// <summary>Raw stdout text captured from the CLI process.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Raw stderr text captured from the CLI process.</summary>
    public string Stderr { get; init; } = string.Empty;

    /// <summary>Process exit code, or null if the process did not exit normally.</summary>
    public int? ExitCode { get; init; }

    /// <summary>
    /// When the body is valid JSON or YAML, this contains the deserialized object.
    /// Null when the body could not be parsed or is plain text.
    /// </summary>
    public object? Parsed { get; init; }

    /// <summary>Detected content type of the body.</summary>
    public CopilotContentType ContentType { get; init; } = CopilotContentType.Text;
}

/// <summary>TR-CLI-001: Strongly-typed result with deserialized output.</summary>
/// <typeparam name="T">The expected deserialized type.</typeparam>
public sealed class CopilotResult<T>
{
    /// <summary>Overall state of the invocation.</summary>
    public CopilotResultState State { get; init; }

    /// <summary>Raw stdout text captured from the CLI process.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Raw stderr text captured from the CLI process.</summary>
    public string Stderr { get; init; } = string.Empty;

    /// <summary>Process exit code, or null if the process did not exit normally.</summary>
    public int? ExitCode { get; init; }

    /// <summary>
    /// When the body is valid JSON or YAML, this contains the deserialized object.
    /// Default value of T when the body could not be parsed or is plain text.
    /// </summary>
    public T? Parsed { get; init; }

    /// <summary>Detected content type of the body.</summary>
    public CopilotContentType ContentType { get; init; } = CopilotContentType.Text;
}
