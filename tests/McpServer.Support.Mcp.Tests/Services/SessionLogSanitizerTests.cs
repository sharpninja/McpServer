using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-SESSIONLOGSAN-001: string sanitizer coverage for default and caller-configured redaction rules.</summary>
public sealed class SessionLogSanitizerTests
{
    /// <summary>Default rules redact bearer tokens, JWTs, provider token prefixes, and PEM private keys.</summary>
    [Fact]
    public void SanitizeString_RedactsDefaultTokenAndKeyPatterns()
    {
        var sanitizer = CreateSanitizer();
        const string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        const string providerToken = "sk-1234567890abcdef1234567890abcdef";
        const string bearerSecret = "abcdefghijklmnopqrstuvwxyz123456";
        const string pem = "-----BEGIN PRIVATE KEY-----\nMIIEvAIBADANBgkqhkiG9w0BAQEFAASC\n-----END PRIVATE KEY-----";
        var input = $"Authorization: Bearer {bearerSecret}\nJWT: {jwt}\nProvider: {providerToken}\n{pem}";

        var output = sanitizer.SanitizeString(input);

        Assert.DoesNotContain(bearerSecret, output, StringComparison.Ordinal);
        Assert.DoesNotContain(jwt, output, StringComparison.Ordinal);
        Assert.DoesNotContain(providerToken, output, StringComparison.Ordinal);
        Assert.DoesNotContain("MIIEvAIBADAN", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:bearer-token]", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:jwt]", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:provider-token]", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:pem-private-key]", output, StringComparison.Ordinal);
        Assert.DoesNotContain(":timeout]", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// Default-rule redaction is deterministic across repeated passes of the same short input.
    /// A 5-second regex timeout isolates this from wall-clock load that previously renamed a
    /// correct secret-assignment token to [REDACTED:secret-assignment:timeout].
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void SanitizeString_RedactsDefaultTokenAndKeyPatterns_Repeated(int iteration)
    {
        _ = iteration;
        var sanitizer = CreateSanitizer();
        const string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        const string providerToken = "sk-1234567890abcdef1234567890abcdef";
        const string bearerSecret = "abcdefghijklmnopqrstuvwxyz123456";
        var input = $"Authorization: Bearer {bearerSecret}\nJWT: {jwt}\nProvider: {providerToken}\npassword=hunter2";

        var output = sanitizer.SanitizeString(input);

        Assert.Equal(sanitizer.SanitizeString(input), output);
        Assert.Contains("[REDACTED:bearer-token]", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:jwt]", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:provider-token]", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:secret-assignment]", output, StringComparison.Ordinal);
        Assert.DoesNotContain("[REDACTED:secret-assignment:timeout]", output, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", output, StringComparison.Ordinal);
    }

    /// <summary>Default rules redact password, secret, API-key assignments, and connection-string passwords.</summary>
    [Fact]
    public void SanitizeString_RedactsAssignmentsAndConnectionStringPasswords()
    {
        var sanitizer = CreateSanitizer();
        const string password = "hunter2";
        const string secret = "top-secret-value";
        const string apiKey = "abc123456789";
        const string connectionPassword = "DbPass123!";
        var input = $"password={password} secret:{secret} api_key={apiKey} Server=db;User Id=sa;Password={connectionPassword};TrustServerCertificate=True";

        var output = sanitizer.SanitizeString(input);

        Assert.DoesNotContain(password, output, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, output, StringComparison.Ordinal);
        Assert.DoesNotContain(apiKey, output, StringComparison.Ordinal);
        Assert.DoesNotContain(connectionPassword, output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:secret-assignment]", output, StringComparison.Ordinal);
        Assert.Contains("Password=[REDACTED:connection-string-password]", output, StringComparison.Ordinal);
    }

    /// <summary>Multiple secrets in one value are independently redacted.</summary>
    [Fact]
    public void SanitizeString_RedactsMultipleSecretsInOneValue()
    {
        var sanitizer = CreateSanitizer();
        var input = "token=local-secret Authorization: Bearer zyxwvutsrqponmlkjihgfedcba sk-abcdef1234567890abcdef1234567890";

        var output = sanitizer.SanitizeString(input);

        Assert.DoesNotContain("local-secret", output, StringComparison.Ordinal);
        Assert.DoesNotContain("zyxwvutsrqponmlkjihgfedcba", output, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-abcdef1234567890abcdef1234567890", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:secret-assignment]", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:bearer-token]", output, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:provider-token]", output, StringComparison.Ordinal);
    }

    /// <summary>Caller-configured overlapping rules run in configured order and produce deterministic replacement tokens.</summary>
    [Fact]
    public void SanitizeString_AppliesOverlappingConfiguredRulesInOrder()
    {
        var sanitizer = CreateSanitizer(new SessionLogSanitizationOptions
        {
            Rules =
            [
                new SessionLogRedactionRuleOptions { Id = "long-token", Pattern = "secret-token-[A-Z0-9]+" },
                new SessionLogRedactionRuleOptions { Id = "token-fragment", Pattern = "token-[A-Z0-9]+" },
            ],
        });

        var output = sanitizer.SanitizeString("secret-token-ABC token-XYZ");

        Assert.Equal("[REDACTED:long-token] [REDACTED:token-fragment]", output);
    }

    /// <summary>Null and empty values are preserved without allocating redaction text.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SanitizeString_PreservesNullAndEmptyValues(string? value)
    {
        var sanitizer = CreateSanitizer();

        var output = sanitizer.SanitizeString(value);

        Assert.Equal(value, output);
    }

    /// <summary>A second sanitizer pass leaves already-redacted values unchanged.</summary>
    [Fact]
    public void SanitizeString_IsIdempotent()
    {
        var sanitizer = CreateSanitizer();
        const string input = "password=hunter2 Authorization: Bearer abcdefghijklmnopqrstuvwxyz123456";

        var once = sanitizer.SanitizeString(input);
        var twice = sanitizer.SanitizeString(once);

        Assert.Equal(once, twice);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-008: clone copies planFile and todoId.</summary>
    [Fact]
    public void SanitizeTurn_CopiesPlanFileAndTodoId()
    {
        var sanitizer = CreateSanitizer();
        var source = new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "Cursor-20260304T113901Z-san",
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260304T113901Z-san",
                    PlanFile = "docs/plans/foo.md",
                    TodoId = "MCP-SESSIONLOG-002",
                },
            ],
        };

        var sanitized = sanitizer.SanitizeSessionLog(source);
        var turn = Assert.Single(sanitized!.Turns!);
        Assert.Equal("docs/plans/foo.md", turn.PlanFile);
        Assert.Equal("MCP-SESSIONLOG-002", turn.TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-008: sanitizer does not mutate the source turn.</summary>
    [Fact]
    public void SanitizeTurn_DoesNotMutateSource()
    {
        var sanitizer = CreateSanitizer();
        var sourceTurn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260304T113901Z-src",
            PlanFile = "docs/plans/foo.md",
            TodoId = "MCP-SESSIONLOG-002",
            Response = "password=hunter2",
        };
        var source = new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "Cursor-20260304T113901Z-src",
            Turns = [sourceTurn],
        };

        _ = sanitizer.SanitizeSessionLog(source);
        Assert.Equal("docs/plans/foo.md", sourceTurn.PlanFile);
        Assert.Equal("MCP-SESSIONLOG-002", sourceTurn.TodoId);
        Assert.Equal("password=hunter2", sourceTurn.Response);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-008: None is not rewritten.</summary>
    [Fact]
    public void SanitizeTurn_LeavesNoneUnchanged()
    {
        var sanitizer = CreateSanitizer();
        var source = new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = "Cursor-20260304T113901Z-none",
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260304T113901Z-none",
                    PlanFile = "None",
                    TodoId = "None",
                },
            ],
        };

        var sanitized = sanitizer.SanitizeSessionLog(source);
        var turn = Assert.Single(sanitized!.Turns!);
        Assert.Equal("None", turn.PlanFile);
        Assert.Equal("None", turn.TodoId);
    }

    private static SessionLogSanitizer CreateSanitizer(SessionLogSanitizationOptions? options = null)
    {
        options ??= new SessionLogSanitizationOptions();
        if (options.RegexTimeoutMilliseconds <= 250)
            options.RegexTimeoutMilliseconds = 5000;
        return new SessionLogSanitizer(Microsoft.Extensions.Options.Options.Create(options));
    }
}
