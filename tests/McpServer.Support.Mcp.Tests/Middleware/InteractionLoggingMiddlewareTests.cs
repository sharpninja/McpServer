using System.Text;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Middleware;

/// <summary>TR-PLANNED-013: Unit tests for InteractionLoggingMiddleware.</summary>
public sealed class InteractionLoggingMiddlewareTests
{
    private static ILogger<McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware> CreateLogger() =>
        Substitute.For<ILogger<McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware>>();

    private static DefaultHttpContext CreateContext(string method = "GET", string path = "/test") =>
        new()
        {
            Request = { Method = method, Path = path },
            Response = { Body = new MemoryStream() }
        };

    /// <summary>InvokeAsync calls next and enqueues entry when LoggingServiceUrl is set and channel is provided.</summary>
    [Fact]
    public async Task InvokeAsync_CallsNext_AndEnqueuesWhenUrlConfigured()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = CreateLogger();
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions
        {
            LoggingServiceUrl = "https://log.example.com/ingest",
            IncludeQueryString = false,
            IncludeRequestBody = false,
            IncludeResponseBody = false
        });
        var channel = Substitute.For<IInteractionLogSubmissionChannel>();
        channel.TryEnqueue(Arg.Any<InteractionLogEntry>()).Returns(true);

        var middleware = new McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware(next, logger, options, channel);

        var context = CreateContext("POST", "/mcp/context/search");
        context.Response.StatusCode = 200;

        await middleware.InvokeAsync(context).ConfigureAwait(true);

        Assert.True(nextCalled);
        var requestId = context.TraceIdentifier;
        channel.Received(1).TryEnqueue(Arg.Is<InteractionLogEntry>(e =>
            e != null &&
            e.Method == "POST" &&
            e.Path == "/mcp/context/search" &&
            e.StatusCode == 200 &&
            e.RequestId == requestId));
    }

    /// <summary>InvokeAsync does not enqueue when LoggingServiceUrl is empty.</summary>
    [Fact]
    public async Task InvokeAsync_DoesNotEnqueue_WhenLoggingServiceUrlEmpty()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = CreateLogger();
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions
        {
            LoggingServiceUrl = "",
            IncludeRequestBody = false,
            IncludeResponseBody = false
        });
        var channel = Substitute.For<IInteractionLogSubmissionChannel>();

        var middleware = new McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware(next, logger, options, channel);
        var context = CreateContext("GET", "/health");
        context.Response.StatusCode = 200;

        await middleware.InvokeAsync(context).ConfigureAwait(true);

        channel.DidNotReceive().TryEnqueue(Arg.Any<InteractionLogEntry>());
    }

    /// <summary>InvokeAsync throws when context is null.</summary>
    [Fact]
    public async Task InvokeAsync_Throws_WhenContextNull()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = CreateLogger();
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions());
        var middleware = new McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware(next, logger, options);

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await middleware.InvokeAsync(null!).ConfigureAwait(true)).ConfigureAwait(true);
    }

    /// <summary>InvokeAsync captures request body when IncludeRequestBody is true.</summary>
    [Fact]
    public async Task InvokeAsync_CapturesRequestBody_WhenEnabled()
    {
        InteractionLogEntry? captured = null;
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = CreateLogger();
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions
        {
            LoggingServiceUrl = "https://log.example.com/ingest",
            IncludeRequestBody = true,
            IncludeResponseBody = false
        });
        var channel = Substitute.For<IInteractionLogSubmissionChannel>();
        channel.TryEnqueue(Arg.Do<InteractionLogEntry>(e => captured = e)).Returns(true);

        var middleware = new McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware(next, logger, options, channel);

        var context = CreateContext("POST", "/mcp/context/search");
        var bodyBytes = Encoding.UTF8.GetBytes("{\"query\":\"test\"}");
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;
        context.Request.ContentType = "application/json";

        await middleware.InvokeAsync(context).ConfigureAwait(true);

        Assert.NotNull(captured);
        Assert.Equal("{\"query\":\"test\"}", captured!.RequestBody);
    }

    /// <summary>InvokeAsync captures response body when IncludeResponseBody is true.</summary>
    [Fact]
    public async Task InvokeAsync_CapturesResponseBody_WhenEnabled()
    {
        InteractionLogEntry? captured = null;
        var responseJson = "{\"chunks\":[],\"sourceKeys\":[]}";
        RequestDelegate next = async ctx =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(responseJson).ConfigureAwait(false);
        };
        var logger = CreateLogger();
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions
        {
            LoggingServiceUrl = "https://log.example.com/ingest",
            IncludeRequestBody = false,
            IncludeResponseBody = true
        });
        var channel = Substitute.For<IInteractionLogSubmissionChannel>();
        channel.TryEnqueue(Arg.Do<InteractionLogEntry>(e => captured = e)).Returns(true);

        var middleware = new McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware(next, logger, options, channel);

        var context = CreateContext("GET", "/mcp/context/sources");

        await middleware.InvokeAsync(context).ConfigureAwait(true);

        Assert.NotNull(captured);
        Assert.Equal(responseJson, captured!.ResponseBody);
    }

    /// <summary>InvokeAsync captures both request and response bodies.</summary>
    [Fact]
    public async Task InvokeAsync_CapturesBothBodies_WhenEnabled()
    {
        InteractionLogEntry? captured = null;
        var responseJson = "{\"result\":\"ok\"}";
        RequestDelegate next = async ctx =>
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync(responseJson).ConfigureAwait(false);
        };
        var logger = CreateLogger();
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions
        {
            LoggingServiceUrl = "https://log.example.com/ingest",
            IncludeRequestBody = true,
            IncludeResponseBody = true
        });
        var channel = Substitute.For<IInteractionLogSubmissionChannel>();
        channel.TryEnqueue(Arg.Do<InteractionLogEntry>(e => captured = e)).Returns(true);

        var middleware = new McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware(next, logger, options, channel);

        var context = CreateContext("POST", "/mcp/repo/file");
        var bodyBytes = Encoding.UTF8.GetBytes("{\"force\":true}");
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;

        await middleware.InvokeAsync(context).ConfigureAwait(true);

        Assert.NotNull(captured);
        Assert.Equal("{\"force\":true}", captured!.RequestBody);
        Assert.Equal(responseJson, captured.ResponseBody);
    }

    /// <summary>InvokeAsync does not capture bodies when disabled.</summary>
    [Fact]
    public async Task InvokeAsync_DoesNotCaptureBodies_WhenDisabled()
    {
        InteractionLogEntry? captured = null;
        RequestDelegate next = async ctx =>
        {
            await ctx.Response.WriteAsync("{\"ok\":true}").ConfigureAwait(false);
        };
        var logger = CreateLogger();
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions
        {
            LoggingServiceUrl = "https://log.example.com/ingest",
            IncludeRequestBody = false,
            IncludeResponseBody = false
        });
        var channel = Substitute.For<IInteractionLogSubmissionChannel>();
        channel.TryEnqueue(Arg.Do<InteractionLogEntry>(e => captured = e)).Returns(true);

        var middleware = new McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware(next, logger, options, channel);

        var context = CreateContext("POST", "/mcp/repo/file");
        var bodyBytes = Encoding.UTF8.GetBytes("{\"path\":\"README.md\"}");
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;

        await middleware.InvokeAsync(context).ConfigureAwait(true);

        Assert.NotNull(captured);
        Assert.Null(captured!.RequestBody);
        Assert.Null(captured.ResponseBody);
    }

    /// <summary>InvokeAsync truncates large bodies at MaxBodyCaptureSize.</summary>
    [Fact]
    public async Task InvokeAsync_TruncatesLargeBody_AtMaxCaptureSize()
    {
        InteractionLogEntry? captured = null;
        var largePayload = new string('x', 200);
        RequestDelegate next = _ => Task.CompletedTask;
        var logger = CreateLogger();
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions
        {
            LoggingServiceUrl = "https://log.example.com/ingest",
            IncludeRequestBody = true,
            IncludeResponseBody = false,
            MaxBodyCaptureSize = 50
        });
        var channel = Substitute.For<IInteractionLogSubmissionChannel>();
        channel.TryEnqueue(Arg.Do<InteractionLogEntry>(e => captured = e)).Returns(true);

        var middleware = new McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware(next, logger, options, channel);

        var context = CreateContext("POST", "/mcp/context/search");
        var bodyBytes = Encoding.UTF8.GetBytes(largePayload);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;

        await middleware.InvokeAsync(context).ConfigureAwait(true);

        Assert.NotNull(captured);
        Assert.NotNull(captured!.RequestBody);
        Assert.EndsWith("...(truncated)", captured.RequestBody);
        Assert.Equal(50 + "...(truncated)".Length, captured.RequestBody.Length);
    }

    /// <summary>Response body is still written to the original stream even when captured.</summary>
    [Fact]
    public async Task InvokeAsync_ResponseBody_StillWrittenToOriginalStream()
    {
        var responseJson = "{\"data\":\"value\"}";
        RequestDelegate next = async ctx =>
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync(responseJson).ConfigureAwait(false);
        };
        var logger = CreateLogger();
        var options = Microsoft.Extensions.Options.Options.Create(new McpInteractionLoggingOptions
        {
            IncludeRequestBody = false,
            IncludeResponseBody = true
        });

        var middleware = new McpServer.Support.Mcp.Middleware.InteractionLoggingMiddleware(next, logger, options);

        var originalBody = new MemoryStream();
        var context = CreateContext("GET", "/mcp/repo/list");
        context.Response.Body = originalBody;

        await middleware.InvokeAsync(context).ConfigureAwait(true);

        // Verify the response was copied to the original stream
        originalBody.Position = 0;
        using var reader = new StreamReader(originalBody);
        var writtenContent = await reader.ReadToEndAsync().ConfigureAwait(true);
        Assert.Equal(responseJson, writtenContent);
    }
}
