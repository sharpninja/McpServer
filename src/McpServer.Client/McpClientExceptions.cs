using System;

namespace McpServer.Client;

/// <summary>
/// Base exception for all MCP Server client HTTP errors. Carries the
/// <see cref="StatusCode"/> from the server response. Derived classes provide
/// semantically typed exceptions for common HTTP status codes.
/// </summary>
/// <seealso cref="McpValidationException"/>
/// <seealso cref="McpUnauthorizedException"/>
/// <seealso cref="McpNotFoundException"/>
/// <seealso cref="McpConflictException"/>
/// <seealso cref="McpServerException"/>
public class McpClientException : Exception
{
    /// <summary>HTTP status code returned by the MCP Server (e.g. 400, 401, 404, 409, 500).</summary>
    public int StatusCode { get; }

    /// <summary>
    /// Initializes a new <see cref="McpClientException"/> with the server error
    /// <paramref name="message"/> and <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="message">Error message extracted from the server response body.</param>
    /// <param name="statusCode">HTTP status code from the response.</param>
    public McpClientException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new <see cref="McpClientException"/> wrapping an
    /// <paramref name="innerException"/> that caused the failure.
    /// </summary>
    /// <param name="message">Error message extracted from the server response body.</param>
    /// <param name="statusCode">HTTP status code from the response.</param>
    /// <param name="innerException">The underlying exception that triggered this error.</param>
    public McpClientException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Thrown when the MCP Server returns <c>400 Bad Request</c>, typically due to invalid
/// or missing request parameters (e.g. a TODO create request without a required field).
/// </summary>
public sealed class McpValidationException : McpClientException
{
    /// <inheritdoc />
    public McpValidationException(string message)
        : base(message, 400) { }
}

/// <summary>
/// Thrown when the MCP Server returns <c>401 Unauthorized</c>. Usually indicates that
/// the <c>X-Api-Key</c> header is missing, expired, or does not match the workspace token.
/// </summary>
public sealed class McpUnauthorizedException : McpClientException
{
    /// <inheritdoc />
    public McpUnauthorizedException(string message)
        : base(message, 401) { }
}

/// <summary>
/// Thrown when the MCP Server returns <c>404 Not Found</c> for a specific resource
/// (e.g. a TODO item, workspace, or tool that does not exist).
/// </summary>
public sealed class McpNotFoundException : McpClientException
{
    /// <inheritdoc />
    public McpNotFoundException(string message)
        : base(message, 404) { }
}

/// <summary>
/// Thrown when the MCP Server returns <c>409 Conflict</c>, typically when creating a
/// resource with an ID that already exists.
/// </summary>
public sealed class McpConflictException : McpClientException
{
    /// <inheritdoc />
    public McpConflictException(string message)
        : base(message, 409) { }
}

/// <summary>
/// Thrown when the MCP Server returns <c>500 Internal Server Error</c> or any other
/// unexpected HTTP status code not covered by the specific exception types above.
/// </summary>
public sealed class McpServerException : McpClientException
{
    /// <inheritdoc />
    public McpServerException(string message, int statusCode)
        : base(message, statusCode) { }
}
