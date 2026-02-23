using System;

namespace McpServer.Client;

/// <summary>Base exception for MCP Server client errors.</summary>
public class McpClientException : Exception
{
    /// <summary>HTTP status code returned by the server.</summary>
    public int StatusCode { get; }

    /// <summary>Initializes a new instance of <see cref="McpClientException"/>.</summary>
    public McpClientException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>Initializes a new instance of <see cref="McpClientException"/>.</summary>
    public McpClientException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

/// <summary>Thrown when the server returns 400 Bad Request.</summary>
public sealed class McpValidationException : McpClientException
{
    /// <summary>Initializes a new instance of <see cref="McpValidationException"/>.</summary>
    public McpValidationException(string message)
        : base(message, 400) { }
}

/// <summary>Thrown when the server returns 401 Unauthorized.</summary>
public sealed class McpUnauthorizedException : McpClientException
{
    /// <summary>Initializes a new instance of <see cref="McpUnauthorizedException"/>.</summary>
    public McpUnauthorizedException(string message)
        : base(message, 401) { }
}

/// <summary>Thrown when the server returns 404 Not Found.</summary>
public sealed class McpNotFoundException : McpClientException
{
    /// <summary>Initializes a new instance of <see cref="McpNotFoundException"/>.</summary>
    public McpNotFoundException(string message)
        : base(message, 404) { }
}

/// <summary>Thrown when the server returns 409 Conflict.</summary>
public sealed class McpConflictException : McpClientException
{
    /// <summary>Initializes a new instance of <see cref="McpConflictException"/>.</summary>
    public McpConflictException(string message)
        : base(message, 409) { }
}

/// <summary>Thrown when the server returns 500 or other unexpected status codes.</summary>
public sealed class McpServerException : McpClientException
{
    /// <summary>Initializes a new instance of <see cref="McpServerException"/>.</summary>
    public McpServerException(string message, int statusCode)
        : base(message, statusCode) { }
}
