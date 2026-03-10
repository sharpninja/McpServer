using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.McpAgent.Tests;

/// <summary>
/// TEST-MCP-089: Verifies canonical hosted-agent session and request identifier helpers.
/// </summary>
public sealed class McpSessionIdentifierFactoryTests
{
    /// <summary>
    /// TEST-MCP-089: Verifies that the factory creates canonical session identifiers using the configured source type, UTC timestamp format, and normalized suffix slug.
    /// </summary>
    [Fact]
    public void CreateSessionId_ReturnsCanonicalSessionIdentifier()
    {
        var factory = CreateFactory("Copilot", new DateTimeOffset(2026, 03, 04, 11, 39, 01, TimeSpan.Zero));

        var sessionId = factory.CreateSessionId("Plan NamingConventions 001");

        Assert.Equal("Copilot-20260304T113901Z-plan-namingconventions-001", sessionId);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the factory creates canonical request identifiers using the shared request prefix, UTC timestamp format, and normalized slug token.
    /// </summary>
    [Fact]
    public void CreateRequestId_ReturnsCanonicalRequestIdentifier()
    {
        var factory = CreateFactory("Copilot", new DateTimeOffset(2026, 03, 04, 11, 39, 01, TimeSpan.Zero));

        var requestId = factory.CreateRequestId("Plan NamingConventions 001");

        Assert.Equal("req-20260304T113901Z-plan-namingconventions-001", requestId);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the static helper normalizes slug tokens using the canonical lowercase-and-hyphen rules shared with the PowerShell session helpers.
    /// </summary>
    [Fact]
    public void SanitizeSlugToken_NormalizesCanonicalSlug()
    {
        var slug = McpSessionIdentifiers.SanitizeSlugToken("  Claude.Sonnet 4/20250514  ");

        Assert.Equal("claude-sonnet-4-20250514", slug);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that canonical session validation succeeds only when the identifier matches both the required format and the exact configured source-type prefix.
    /// </summary>
    [Fact]
    public void TryValidateSessionId_ReturnsExpectedResults()
    {
        var factory = CreateFactory("Copilot", new DateTimeOffset(2026, 03, 04, 11, 39, 01, TimeSpan.Zero));

        var valid = factory.TryValidateSessionId("Copilot-20260304T113901Z-plan-namingconventions-001", out var validError);
        var invalidPrefix = factory.TryValidateSessionId("Cursor-20260304T113901Z-plan-namingconventions-001", out var invalidPrefixError);
        var invalidFormat = factory.TryValidateSessionId("Copilot-2026-03-04-plan", out var invalidFormatError);

        Assert.True(valid);
        Assert.Null(validError);
        Assert.False(invalidPrefix);
        Assert.Equal("SessionId must start with the exact SourceType prefix 'Copilot-'.", invalidPrefixError);
        Assert.False(invalidFormat);
        Assert.Equal(
            "SessionId must match <Agent>-<yyyyMMddTHHmmssZ>-<suffix> (example: Copilot-20260304T113901Z-namingconv).",
            invalidFormatError);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that canonical request validation accepts the shared request format and rejects malformed request identifiers.
    /// </summary>
    [Fact]
    public void TryValidateRequestId_ReturnsExpectedResults()
    {
        var factory = CreateFactory("Copilot", new DateTimeOffset(2026, 03, 04, 11, 39, 01, TimeSpan.Zero));

        var valid = factory.TryValidateRequestId("req-20260304T113901Z-plan-namingconventions-001", out var validError);
        var invalid = factory.TryValidateRequestId("request-20260304T113901Z-plan-001", out var invalidError);

        Assert.True(valid);
        Assert.Null(validError);
        Assert.False(invalid);
        Assert.Equal(
            "RequestId must match req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal> (example: req-20260304T113901Z-plan-namingconventions-001).",
            invalidError);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that DI registration exposes the identifier factory as a singleton dependency and through the hosted-agent surface.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_RegistersIdentifierFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 03, 04, 11, 39, 01, TimeSpan.Zero)));
        services.AddMcpServerMcpAgent(options =>
        {
            options.ApiKey = "token";
            options.SourceType = "Copilot";
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IMcpSessionIdentifierFactory>();
        var hostedAgent = serviceProvider.GetRequiredService<Hosting.IMcpHostedAgent>();

        Assert.Same(factory, hostedAgent.Identifiers);
        Assert.Equal("req-20260304T113901Z-plan-001", factory.CreateRequestId("Plan 001"));
    }

    private static IMcpSessionIdentifierFactory CreateFactory(string sourceType, DateTimeOffset now)
    {
        var options = Options.Create(new McpAgentOptions
        {
            ApiKey = "token",
            BaseUrl = new Uri("http://localhost:7147"),
            SourceType = sourceType,
            WorkspacePath = @"E:\github\McpServer",
        });

        return new McpSessionIdentifierFactory(options, new FixedTimeProvider(now));
    }

    /// <summary>
    /// TEST-MCP-089: Provides a deterministic clock for identifier-helper tests.
    /// </summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        /// <summary>
        /// TEST-MCP-089: Initializes the deterministic test clock with a fixed UTC timestamp.
        /// </summary>
        /// <param name="utcNow">The fixed UTC timestamp returned by <see cref="GetUtcNow"/>.</param>
        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow.ToUniversalTime();
        }

        /// <summary>
        /// TEST-MCP-089: Returns the fixed UTC timestamp configured for the test.
        /// </summary>
        /// <returns>The fixed UTC timestamp.</returns>
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
