using System.Diagnostics;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-007: session-log Submit uses a configurable SaveChanges budget
/// that defaults to 30 seconds. Triage intake stays on the 5 second shared default.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESTORE-007; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (ShippedAppSettings_AndHosts_RegisterThirtySecondSubmitBudget and SubmitAsync_SaveLongerThanFiveSeconds_CompletesUnderDefaultBudget) and the helpers only they use (WorkspacePath, DelayedSaveChangesInterceptor and CreateSession) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
public sealed class SessionLogSubmitBudgetTests
{
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
}
