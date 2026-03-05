using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AuditedCopilotClient"/>.
/// </summary>
public sealed class AuditedCopilotClientTests
{
    [Fact]
    public async Task InvokeAsync_WritesCopilotInvocationAuditEntry()
    {
        var workspacePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"audit-ws-{Guid.NewGuid():N}"));
        var sessionLogService = Substitute.For<ISessionLogService>();
        sessionLogService.SubmitAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        var services = BuildServices(workspacePath, sessionLogService);

        var inner = Substitute.For<ICopilotClient>();
        inner.InvokeAsync(Arg.Any<string>(), Arg.Any<CopilotClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new CopilotResult { State = CopilotResultState.Success, Body = "ok", ExitCode = 0 });

        var audited = new AuditedCopilotClient(
            inner,
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<IHttpContextAccessor>(),
            services.GetRequiredService<IOptions<IngestionOptions>>(),
            NullLogger<AuditedCopilotClient>.Instance);

        await audited.InvokeAsync(
            "test prompt",
            new CopilotClientOptions { WorkingDirectory = workspacePath },
            CancellationToken.None).ConfigureAwait(true);

        var submittedDtos = sessionLogService.ReceivedCalls()
            .Select(call => call.GetArguments()[0] as UnifiedSessionLogDto)
            .Where(dto => dto is not null)
            .Select(dto => dto!)
            .ToList();

        Assert.Contains(
            submittedDtos,
            static dto => HasCompletedCopilotInvocationAudit(dto));
    }

    [Fact]
    public async Task InvokeStreamingAsync_CompletionIsAudited()
    {
        var workspacePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"audit-stream-ws-{Guid.NewGuid():N}"));
        var sessionLogService = Substitute.For<ISessionLogService>();
        sessionLogService.SubmitAsync(Arg.Any<UnifiedSessionLogDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        var services = BuildServices(workspacePath, sessionLogService);

        var inner = Substitute.For<ICopilotClient>();
        inner.InvokeStreamingAsync(Arg.Any<string>(), Arg.Any<CopilotClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(StreamLines());

        var audited = new AuditedCopilotClient(
            inner,
            services.GetRequiredService<IServiceScopeFactory>(),
            services.GetRequiredService<IHttpContextAccessor>(),
            services.GetRequiredService<IOptions<IngestionOptions>>(),
            NullLogger<AuditedCopilotClient>.Instance);

        var lines = new List<string>();
        await foreach (var line in audited.InvokeStreamingAsync(
                           "test stream",
                           new CopilotClientOptions { WorkingDirectory = workspacePath },
                           CancellationToken.None))
        {
            lines.Add(line);
        }

        Assert.Equal(2, lines.Count);

        var submittedDtos = sessionLogService.ReceivedCalls()
            .Select(call => call.GetArguments()[0] as UnifiedSessionLogDto)
            .Where(dto => dto is not null)
            .Select(dto => dto!)
            .ToList();

        Assert.Contains(
            submittedDtos,
            static dto => HasCompletedStreamingAudit(dto));
    }

    private static ServiceProvider BuildServices(string repoRoot, ISessionLogService sessionLogService)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IOptions<IngestionOptions>>(Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = repoRoot }));
        services.AddDbContext<McpDbContext>(o => o.UseInMemoryDatabase($"AuditedCopilotTests_{Guid.NewGuid():N}"));
        services.AddScoped(_ => sessionLogService);
        return services.BuildServiceProvider();
    }

    private static bool HasCompletedCopilotInvocationAudit(UnifiedSessionLogDto? dto)
    {
        if (dto?.Entries is not { Count: 1 })
            return false;

        var entry = dto.Entries[0];
        if (entry.Actions is not { Count: > 0 })
            return false;

        return string.Equals(entry.Actions[0].Type, "copilot_invocation", StringComparison.Ordinal)
               && string.Equals(entry.Status, "completed", StringComparison.Ordinal);
    }

    private static bool HasCompletedStreamingAudit(UnifiedSessionLogDto? dto)
    {
        if (dto?.Entries is not { Count: 1 })
            return false;

        var entry = dto.Entries[0];
        return string.Equals(entry.Status, "completed", StringComparison.Ordinal)
               && string.Equals(entry.QueryTitle, "Copilot invoke_streaming", StringComparison.Ordinal);
    }

    private static async IAsyncEnumerable<string> StreamLines()
    {
        yield return "line 1";
        await Task.Yield();
        yield return "line 2";
    }
}
