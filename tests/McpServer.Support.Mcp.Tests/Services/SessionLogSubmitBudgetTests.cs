using System.Diagnostics;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-007: session-log Submit uses a configurable SaveChanges budget
/// that defaults to 30 seconds. Triage intake stays on the 5 second shared default.
/// </summary>
public sealed class SessionLogSubmitBudgetTests
{
    private const string WorkspacePath = @"E:\tests\sessionlog-submit-budget";

    /// <summary>The options default and the recommended deploy value are 30 seconds.</summary>
    [Fact]
    public void DefaultSubmitBudget_IsThirtySeconds()
    {
        var options = new SessionLogSubmitOptions();
        Assert.Equal(30, options.SubmitCommandBudgetSeconds);
        Assert.Equal(TimeSpan.FromSeconds(30), options.GetSubmitCommandBudget());
        Assert.Equal(TimeSpan.FromSeconds(5), StorageCommandBudget.Default);
    }

    /// <summary>Missing configuration keeps the 30 second property default.</summary>
    [Fact]
    public void Bind_MissingSection_KeepsThirtySecondDefault()
    {
        var configuration = new ConfigurationBuilder().Build();
        var options = new SessionLogSubmitOptions();
        configuration.GetSection(SessionLogSubmitOptions.SectionName).Bind(options);

        Assert.Equal(SessionLogSubmitOptions.DefaultSubmitCommandBudgetSeconds, options.SubmitCommandBudgetSeconds);
    }

    /// <summary>Mcp:SessionLog:SubmitCommandBudgetSeconds binds onto the options object.</summary>
    [Fact]
    public void Bind_SubmitCommandBudgetSeconds_OverridesDefault()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SessionLogSubmitOptions.SectionName}:SubmitCommandBudgetSeconds"] = "45",
            })
            .Build();
        var options = new SessionLogSubmitOptions();
        configuration.GetSection(SessionLogSubmitOptions.SectionName).Bind(options);

        Assert.Equal(45, options.SubmitCommandBudgetSeconds);
        Assert.Equal(TimeSpan.FromSeconds(45), options.GetSubmitCommandBudget());
    }

    /// <summary>Values outside 1 through 300 fail options validation.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public void Validate_RejectsOutOfRangeBudget(int seconds)
    {
        var validator = new SessionLogSubmitOptionsValidator();
        var result = validator.Validate(null, new SessionLogSubmitOptions { SubmitCommandBudgetSeconds = seconds });

        Assert.True(result.Failed);
        Assert.Contains("SubmitCommandBudgetSeconds", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>The shipped appsettings knob and host registration use the 30 second Submit budget.</summary>
    [Fact]
    public void ShippedAppSettings_AndHosts_RegisterThirtySecondSubmitBudget()
    {
        var root = RepositoryEvidenceTestSupport.ResolveRepositoryRoot();
        var appSettings = File.ReadAllText(Path.Combine(root, "src", "McpServer.Support.Mcp", "appsettings.yaml"));
        var program = File.ReadAllText(Path.Combine(root, "src", "McpServer.Support.Mcp", "Program.cs"));
        var stdio = File.ReadAllText(Path.Combine(root, "src", "McpServer.Support.Mcp", "McpStdio", "McpStdioHost.cs"));

        Assert.Contains("SubmitCommandBudgetSeconds: 30", appSettings, StringComparison.Ordinal);
        Assert.Contains("AddOptions<SessionLogSubmitOptions>()", program, StringComparison.Ordinal);
        Assert.Contains("SessionLogSubmitOptionsValidator", program, StringComparison.Ordinal);
        Assert.Contains("AddOptions<SessionLogSubmitOptions>()", stdio, StringComparison.Ordinal);
        Assert.Contains("SessionLogSubmitOptionsValidator", stdio, StringComparison.Ordinal);
    }

    /// <summary>
    /// A Submit SaveChanges that outlasts the 5 second intake default completes when the
    /// service uses the 30 second default budget.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_SaveLongerThanFiveSeconds_CompletesUnderDefaultBudget()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogSubmitBudget_{Guid.NewGuid():N}")
            .AddInterceptors(new DelayedSaveChangesInterceptor(TimeSpan.FromSeconds(6)))
            .Options;
        await using var db = new McpDbContext(options);
        db.Database.EnsureCreated();
        db.OverrideWorkspaceId(WorkspacePath);
        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = WorkspacePath });

        var clock = Stopwatch.StartNew();
        var id = await sut.SubmitAsync(
            CreateSession("Cursor-20260928T215100Z-submit-budget"),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        clock.Stop();

        Assert.True(id > 0);
        Assert.True(clock.Elapsed >= TimeSpan.FromSeconds(5), $"Save finished before the old 5 second cancel: {clock.Elapsed}.");
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(20), $"Save exceeded the 30 second Submit budget: {clock.Elapsed}.");
    }

    /// <summary>Delays SaveChanges for a fixed duration unless the caller cancels.</summary>
    private sealed class DelayedSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly TimeSpan _delay;

        /// <summary>Initializes the interceptor.</summary>
        /// <param name="delay">How long SavingChanges waits.</param>
        public DelayedSaveChangesInterceptor(TimeSpan delay) => _delay = delay;

        /// <inheritdoc />
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    private static UnifiedSessionLogDto CreateSession(string sessionId)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = sessionId,
            Title = "Submit budget",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260928T215100Z-submit-budget",
                    Timestamp = "2026-09-28T21:51:00Z",
                    QueryText = "submit budget",
                    Status = "in_progress",
                    PlanFile = SessionLogTurnContextValidator.NoneSentinel,
                    TodoId = SessionLogTurnContextValidator.NoneSentinel,
                },
            ],
        };
    }
}
