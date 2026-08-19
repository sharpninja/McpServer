using System.Text.Json;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-TRIAGEERR-001: SessionLogController maps persistence failures to the four-field envelope.
/// </summary>
public sealed class SessionLogControllerErrorTests
{
    /// <summary>DbUpdateException on submit returns persistence/conflict fields, not a bare EF message.</summary>
    [Fact]
    public async Task SubmitAsync_DbUpdateException_ReturnsPersistenceProblem()
    {
        var service = Substitute.For<ISessionLogService>();
        var inner = new SqliteException("UNIQUE constraint failed: SessionLogs.SessionId", 19);
        service.SubmitAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new DbUpdateException(
                "An error occurred while saving the entity changes. See the inner exception for details.",
                inner));

        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);
        var dto = new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "Cursor-20260818T200000Z-controller-err",
        };

        var result = await controller.SubmitAsync(dto, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, objectResult.StatusCode);
        var json = JsonSerializer.Serialize(objectResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("conflict", document.RootElement.GetProperty("code").GetString());
        Assert.False(document.RootElement.GetProperty("retryable").GetBoolean());
        Assert.Equal(inner.Message, document.RootElement.GetProperty("details").GetProperty("inner").GetString());
        Assert.DoesNotContain("See the inner exception", json, StringComparison.Ordinal);
    }

    /// <summary>REST validation returns the four-field envelope, not ProblemDetails-only.</summary>
    [Fact]
    public async Task SubmitAsync_MissingSourceType_ReturnsValidationEnvelope()
    {
        var service = Substitute.For<ISessionLogService>();
        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);
        var dto = new UnifiedSessionLogDto
        {
            SessionId = "Cursor-20260818T220000Z-validation",
        };

        var result = await controller.SubmitAsync(dto, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
        var json = JsonSerializer.Serialize(objectResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("validation_error", document.RootElement.GetProperty("code").GetString());
        Assert.False(document.RootElement.GetProperty("retryable").GetBoolean());
        Assert.Contains("sourceType", document.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.Equal("validation", document.RootElement.GetProperty("details").GetProperty("reason").GetString());
    }

    /// <summary>REST storage budget expiry returns backend_unavailable retryable true.</summary>
    [Fact]
    public async Task SubmitAsync_StorageBudgetExceeded_ReturnsBackendUnavailableEnvelope()
    {
        var service = Substitute.For<ISessionLogService>();
        service.SubmitAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new StorageCommandBudgetExceededException());

        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);
        var dto = new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "Cursor-20260818T220000Z-budget",
        };

        var result = await controller.SubmitAsync(dto, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
        var json = JsonSerializer.Serialize(objectResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("backend_unavailable", document.RootElement.GetProperty("code").GetString());
        Assert.True(document.RootElement.GetProperty("retryable").GetBoolean());
        Assert.Equal("backend_unavailable", document.RootElement.GetProperty("details").GetProperty("reason").GetString());
    }

    /// <summary>REST missing session/turn returns classified not_found with code/retryable.</summary>
    [Fact]
    public async Task DeleteSessionAsync_MissingSession_ReturnsNotFoundEnvelope()
    {
        var service = Substitute.For<ISessionLogService>();
        service.DeleteSessionAsync("Cursor", "Cursor-20260818T220000Z-missing", Arg.Any<CancellationToken>())
            .Returns(false);
        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);

        var result = await controller.DeleteSessionAsync(
            "Cursor",
            "Cursor-20260818T220000Z-missing",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
        var json = JsonSerializer.Serialize(objectResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("not_found", document.RootElement.GetProperty("code").GetString());
        Assert.False(document.RootElement.GetProperty("retryable").GetBoolean());
        Assert.Equal("not_found", document.RootElement.GetProperty("details").GetProperty("reason").GetString());
    }

    /// <summary>
    /// BUG-TRIAGE-144: HTTP replace_section storage outage returns 503 backend_unavailable
    /// with retryable true so Streamable HTTP clients can retry.
    /// </summary>
    [Fact]
    public async Task ReplaceTurnSectionAsync_StorageUnreachable_Returns503Retryable()
    {
        var service = Substitute.For<ISessionLogService>();
        service.ReplaceTurnSectionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<UnifiedRequestEntryDto>(),
                Arg.Any<CancellationToken>())
            .Throws(new SqliteException("unable to open database file", 14));
        var controller = new SessionLogController(service, NullLogger<SessionLogController>.Instance);

        var result = await controller.ReplaceTurnSectionAsync(
            "Cursor",
            "Cursor-20260819T220000Z-replace-503",
            "req-20260819T220000Z-entry-001",
            "tags",
            new UnifiedRequestEntryDto { Tags = ["retry"] },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
        var json = JsonSerializer.Serialize(objectResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("backend_unavailable", document.RootElement.GetProperty("code").GetString());
        Assert.True(document.RootElement.GetProperty("retryable").GetBoolean());
    }
}
