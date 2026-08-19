using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGEERR-001: <see cref="McpErrorClassifier"/> emits the four-field envelope
/// for validation, not-found, persistence (with inner), and backend_unavailable.
/// </summary>
public sealed class McpErrorClassifierTests
{
    /// <summary>Sqlite CANTOPEN is backend_unavailable and retryable.</summary>
    [Fact]
    public void Classify_SqliteCantOpen_IsBackendUnavailableRetryable()
    {
        var result = McpErrorClassifier.Classify(new SqliteException("unable to open database file", 14));

        Assert.Equal(McpErrorClassifier.BackendUnavailable, result.Code);
        Assert.Equal(McpErrorClassifier.BackendUnavailableMessage, result.Message);
        Assert.True(result.Retryable);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal("backend_unavailable", result.Details!["reason"]);
    }

    /// <summary>DbUpdateException includes innermost provider text in details.inner.</summary>
    [Fact]
    public void Classify_DbUpdateException_IncludesInnermostProviderText()
    {
        var inner = new SqliteException("UNIQUE constraint failed: SessionLogTurns.RequestId", 19);
        var exception = new DbUpdateException(
            "An error occurred while saving the entity changes. See the inner exception for details.",
            inner);

        var result = McpErrorClassifier.Classify(exception);

        Assert.Equal(McpErrorClassifier.Conflict, result.Code);
        Assert.False(result.Retryable);
        Assert.NotNull(result.Details);
        Assert.Equal(inner.Message, result.Details!["inner"]);
        Assert.DoesNotContain("See the inner exception", result.Message, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-005: SQLITE_BUSY is persistence_error and retryable.</summary>
    [Fact]
    public void Classify_SqliteBusy_IsRetryablePersistenceError()
    {
        var result = McpErrorClassifier.Classify(new SqliteException("database is locked", 5));

        Assert.Equal(McpErrorClassifier.PersistenceError, result.Code);
        Assert.True(result.Retryable);
        Assert.NotNull(result.Details);
        Assert.Contains("locked", result.Details!["inner"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>ArgumentException is validation_error and not retryable.</summary>
    [Fact]
    public void Classify_ArgumentException_IsValidationError()
    {
        var result = McpErrorClassifier.Classify(new ArgumentException("sourceType is required."));

        Assert.Equal(McpErrorClassifier.ValidationError, result.Code);
        Assert.False(result.Retryable);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("validation", result.Details!["reason"]);
    }

    /// <summary>KeyNotFoundException is not_found.</summary>
    [Fact]
    public void Classify_KeyNotFound_IsNotFound()
    {
        var result = McpErrorClassifier.Classify(new KeyNotFoundException("Turn not found."));

        Assert.Equal(McpErrorClassifier.NotFound, result.Code);
        Assert.Equal(404, result.StatusCode);
        Assert.False(result.Retryable);
        Assert.Equal("not_found", result.Details!["reason"]);
    }
}
