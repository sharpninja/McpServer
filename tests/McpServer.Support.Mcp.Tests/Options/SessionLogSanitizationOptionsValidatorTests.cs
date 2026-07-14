using McpServer.Support.Mcp.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Options;

/// <summary>TEST-MCP-SESSIONLOGSAN-001: validates session log sanitization option safety constraints before startup.</summary>
public sealed class SessionLogSanitizationOptionsValidatorTests
{
    /// <summary>Default sanitizer options are valid so deployments can enable safe defaults without custom regex rules.</summary>
    [Fact]
    public void Validate_ReturnsSuccess_ForDefaultOptions()
    {
        var validator = new SessionLogSanitizationOptionsValidator();
        var options = new SessionLogSanitizationOptions();

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    /// <summary>Duplicate rule IDs are rejected ignoring case so replacement tokens remain deterministic.</summary>
    [Fact]
    public void Validate_Fails_WhenRuleIdsAreDuplicateIgnoringCase()
    {
        var validator = new SessionLogSanitizationOptionsValidator();
        var options = new SessionLogSanitizationOptions
        {
            Rules =
            [
                new SessionLogRedactionRuleOptions { Id = "api-key", Pattern = "secret-one" },
                new SessionLogRedactionRuleOptions { Id = "API-KEY", Pattern = "secret-two" },
            ],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("duplicate", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Rules must name their redaction token ID.</summary>
    [Fact]
    public void Validate_Fails_WhenRuleIdIsMissing()
    {
        var validator = new SessionLogSanitizationOptionsValidator();
        var options = new SessionLogSanitizationOptions
        {
            Rules = [new SessionLogRedactionRuleOptions { Id = " ", Pattern = "secret" }],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Id", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>Rules must provide a regex pattern before the sanitizer can compile them.</summary>
    [Fact]
    public void Validate_Fails_WhenPatternIsMissing()
    {
        var validator = new SessionLogSanitizationOptionsValidator();
        var options = new SessionLogSanitizationOptions
        {
            Rules = [new SessionLogRedactionRuleOptions { Id = "missing-pattern", Pattern = " " }],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Pattern", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>Configured rule counts are bounded so startup cannot allocate an unbounded regex catalog.</summary>
    [Fact]
    public void Validate_Fails_WhenRuleCountExceedsMaximum()
    {
        var validator = new SessionLogSanitizationOptionsValidator();
        var options = new SessionLogSanitizationOptions
        {
            MaxRuleCount = 2,
            Rules =
            [
                new SessionLogRedactionRuleOptions { Id = "one", Pattern = "one" },
                new SessionLogRedactionRuleOptions { Id = "two", Pattern = "two" },
                new SessionLogRedactionRuleOptions { Id = "three", Pattern = "three" },
            ],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxRuleCount", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>Regex pattern text is bounded so configuration cannot load extremely large expressions.</summary>
    [Fact]
    public void Validate_Fails_WhenPatternLengthExceedsMaximum()
    {
        var validator = new SessionLogSanitizationOptionsValidator();
        var options = new SessionLogSanitizationOptions
        {
            MaxPatternLength = 5,
            Rules = [new SessionLogRedactionRuleOptions { Id = "long-pattern", Pattern = "123456" }],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxPatternLength", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>Regex match timeout bounds are validated before any expression can execute.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(60001)]
    public void Validate_Fails_WhenTimeoutIsOutsideBounds(int timeoutMilliseconds)
    {
        var validator = new SessionLogSanitizationOptionsValidator();
        var options = new SessionLogSanitizationOptions
        {
            RegexTimeoutMilliseconds = timeoutMilliseconds,
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("RegexTimeoutMilliseconds", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>Invalid regex syntax is rejected during startup validation and identified by rule ID only.</summary>
    [Fact]
    public void Validate_Fails_WhenPatternSyntaxIsInvalid()
    {
        var validator = new SessionLogSanitizationOptionsValidator();
        var options = new SessionLogSanitizationOptions
        {
            Rules = [new SessionLogRedactionRuleOptions { Id = "bad-regex", Pattern = "(" }],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("bad-regex", result.FailureMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("(", result.FailureMessage, StringComparison.Ordinal);
    }

    /// <summary>Replacement values cannot expand capture groups because replacements must not echo secret fragments.</summary>
    [Theory]
    [InlineData("$1")]
    [InlineData("${secret}")]
    public void Validate_Fails_WhenReplacementUsesRegexGroupExpansion(string replacement)
    {
        var validator = new SessionLogSanitizationOptionsValidator();
        var options = new SessionLogSanitizationOptions
        {
            Rules = [new SessionLogRedactionRuleOptions { Id = "group-replacement", Pattern = "secret", Replacement = replacement }],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Replacement", result.FailureMessage, StringComparison.Ordinal);
    }
}
