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

    private static SessionLogSanitizer CreateSanitizer(SessionLogSanitizationOptions? options = null)
    {
        return new SessionLogSanitizer(Microsoft.Extensions.Options.Options.Create(options ?? new SessionLogSanitizationOptions()));
    }
}
