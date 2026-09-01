using System.Text.Json;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Middleware;

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): the shared REST error path
/// (<see cref="GlobalExceptionHandlerMiddleware"/>) SHALL map connection-class storage failures to
/// HTTP 503 with the stable machine-readable body <c>{"error":"backend_unavailable", ...}</c>,
/// replacing raw SqlClient/SQLite text and the generic 500 "unexpected error" response. Ordinary
/// exceptions keep the existing 500 <c>internal_server_error</c> mapping.
/// Fixture: the middleware invoked directly with a <see cref="DefaultHttpContext"/> whose request
/// services register <see cref="StorageBackendUnavailabilityDetector"/> as the
/// <see cref="IBackendUnavailabilityDetector"/>.
/// </summary>
public sealed class GlobalExceptionHandlerBackendUnavailableTests
{
    private static async Task<(int StatusCode, JsonDocument Body)> InvokeAsync(
        Exception exception, CancellationToken cancellationToken)
    {
        var provider = new ServiceCollection()
            .AddSingleton<IBackendUnavailabilityDetector, StorageBackendUnavailabilityDetector>()
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = "POST";
        context.Request.Path = "/mcpserver/sessionlog";
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw exception,
            NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context).ConfigureAwait(true);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(true);
        return (context.Response.StatusCode, JsonDocument.Parse(json));
    }

    /// <summary>
    /// AC (TR-MCP-HEALTH-003): a connection-class storage failure returns HTTP 503 with the
    /// stable <c>backend_unavailable</c> error code and no raw provider text anywhere in the body.
    /// </summary>
    [Fact]
    public async Task StorageFailure_Returns503_WithTypedBackendUnavailableBody()
    {
        var (statusCode, document) = await InvokeAsync(
            new SqliteException("unable to open database file", 14),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        using (document)
        {
            var body = document.RootElement.GetRawText();
            Assert.True(statusCode == StatusCodes.Status503ServiceUnavailable,
                $"Expected 503 with typed backend_unavailable body; actual status {statusCode}, body: {body}");
            Assert.Equal("backend_unavailable", document.RootElement.GetProperty("error").GetString());
            Assert.Equal("backend_unavailable", document.RootElement.GetProperty("code").GetString());
            Assert.True(document.RootElement.GetProperty("retryable").GetBoolean());
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, document.RootElement.GetProperty("status").GetInt32());
            Assert.DoesNotContain("SQLite Error", body, StringComparison.Ordinal);
            Assert.DoesNotContain("unable to open database file", body, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Guard: ordinary unhandled exceptions keep the existing 500 <c>internal_server_error</c>
    /// mapping so the typed 503 stays scoped to storage-connectivity failures.
    /// </summary>
    [Fact]
    public async Task OrdinaryFailure_Keeps500InternalServerError()
    {
        var (statusCode, document) = await InvokeAsync(
            new InvalidOperationException("turn validation failed"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        using (document)
        {
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
            Assert.Equal("internal_server_error", document.RootElement.GetProperty("error").GetString());
        }
    }
}
