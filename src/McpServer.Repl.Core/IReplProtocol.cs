namespace McpServer.Repl.Core;

/// <summary>
/// Defines the core REPL protocol contract for command routing, request/response correlation,
/// and lifecycle management. Implementations handle message dispatch, authentication,
/// and workspace context switching.
/// </summary>
public interface IReplProtocol
{
    /// <summary>
    /// Gets the current protocol version.
    /// Format: "major.minor" (e.g., "1.0").
    /// </summary>
    string ProtocolVersion { get; }

    /// <summary>
    /// Gets the active workspace path, if a workspace has been selected.
    /// Null if no workspace is active.
    /// </summary>
    string? CurrentWorkspace { get; }

    /// <summary>
    /// Gets a value indicating whether the protocol session is authenticated.
    /// Authentication is typically established after marker-file verification and key exchange.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets a value indicating whether the protocol session is connected and ready for command dispatch.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Initiates a protocol handshake with the server.
    /// Sends a "hello" envelope and waits for the server's hello response.
    /// </summary>
    /// <param name="capabilities">Optional client capabilities to declare.</param>
    /// <param name="metadata">Optional client metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The server's hello payload.</returns>
    /// <exception cref="InvalidOperationException">Thrown if already connected or handshake fails.</exception>
    Task<IHelloPayload> ConnectAsync(
        IEnumerable<string>? capabilities = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command request and waits for a result or error response.
    /// </summary>
    /// <param name="method">The command method name.</param>
    /// <param name="parameters">The command parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command result data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
    /// <exception cref="ReplProtocolException">Thrown if the server returns an error response.</exception>
    Task<object?> SendRequestAsync(
        string method,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command request with a typed result.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="method">The command method name.</param>
    /// <param name="parameters">The command parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command result as the specified type.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not connected or result type cannot be converted.</exception>
    /// <exception cref="ReplProtocolException">Thrown if the server returns an error response.</exception>
    Task<TResult?> SendRequestAsync<TResult>(
        string method,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a handler for server-initiated events.
    /// Multiple handlers can be registered for the same event name.
    /// </summary>
    /// <param name="eventName">The event name to listen for.</param>
    /// <param name="handler">The handler callback to invoke when the event is received.</param>
    void RegisterEventHandler(string eventName, Func<IEventPayload, Task> handler);

    /// <summary>
    /// Unregisters a previously registered event handler.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="handler">The handler to remove.</param>
    void UnregisterEventHandler(string eventName, Func<IEventPayload, Task> handler);

    /// <summary>
    /// Disconnects from the server and releases protocol resources.
    /// Pending requests are cancelled.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown when a REPL protocol operation fails with a server error response.
/// </summary>
public class ReplProtocolException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplProtocolException"/> class.
    /// </summary>
    /// <param name="errorPayload">The error payload from the server.</param>
    public ReplProtocolException(IErrorPayload errorPayload)
        : base(errorPayload.Message)
    {
        ErrorPayload = errorPayload;
    }

    /// <summary>
    /// Gets the error payload from the server.
    /// </summary>
    public IErrorPayload ErrorPayload { get; }

    /// <summary>
    /// Gets the error code from the server.
    /// </summary>
    public string Code => ErrorPayload.Code;

    /// <summary>
    /// Gets optional error details from the server.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Details => ErrorPayload.Details;
}
