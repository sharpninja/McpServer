using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Options;

public sealed class AgentPoolOptionsValidatorTests
{
    [Fact]
    public void ValidateReturnsSuccessWhenPoolDisabled()
    {
        var validator = new AgentPoolOptionsValidator();
        var options = new AgentPoolOptions { Enabled = false };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateFailsWhenEnabledWithNoAgents()
    {
        var validator = new AgentPoolOptionsValidator();
        var options = new AgentPoolOptions { Enabled = true };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("at least one agent", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateFailsWhenAgentNamesAreDuplicateIgnoringCase()
    {
        var validator = new AgentPoolOptionsValidator();
        var options = new AgentPoolOptions
        {
            Enabled = true,
            Agents =
            [
                new AgentPoolDefinitionOptions { AgentName = "Planner", AgentPath = "agent.exe" },
                new AgentPoolDefinitionOptions { AgentName = "planner", AgentPath = "agent2.exe" },
            ],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("duplicate", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateFailsWhenMoreThanOnePlanDefaultAgentExists()
    {
        var validator = new AgentPoolOptionsValidator();
        var options = new AgentPoolOptions
        {
            Enabled = true,
            Agents =
            [
                new AgentPoolDefinitionOptions { AgentName = "PlannerA", AgentPath = "a.exe", IsTodoPlanDefault = true },
                new AgentPoolDefinitionOptions { AgentName = "PlannerB", AgentPath = "b.exe", IsTodoPlanDefault = true },
            ],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("IsTodoPlanDefault", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateFailsWhenExecutionStrategyIsUnknown()
    {
        var validator = new AgentPoolOptionsValidator();
        var options = new AgentPoolOptions
        {
            Enabled = true,
            Agents =
            [
                new AgentPoolDefinitionOptions { AgentName = "Planner", AgentPath = "p.exe", ExecutionStrategy = "unknown-strategy" },
            ],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ExecutionStrategy", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateReturnsSuccessWhenConfigurationIsValid()
    {
        var validator = new AgentPoolOptionsValidator();
        var options = new AgentPoolOptions
        {
            Enabled = true,
            Agents =
            [
                new AgentPoolDefinitionOptions { AgentName = "Interactive", AgentPath = "i.exe", IsInteractiveDefault = true },
                new AgentPoolDefinitionOptions { AgentName = "Planner", AgentPath = "p.exe", IsTodoPlanDefault = true },
                new AgentPoolDefinitionOptions { AgentName = "Status", AgentPath = "s.exe", IsTodoStatusDefault = true },
                new AgentPoolDefinitionOptions { AgentName = "Implement", AgentPath = "m.exe", IsTodoImplementDefault = true },
            ],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateReturnsSuccessWhenExecutionStrategyUsesLegacyHostedAgentFrameworkAlias()
    {
        var validator = new AgentPoolOptionsValidator();
        var options = new AgentPoolOptions
        {
            Enabled = true,
            Agents =
            [
                new AgentPoolDefinitionOptions
                {
                    AgentName = "Planner",
                    AgentPath = "p.exe",
                    ExecutionStrategy = AgentExecutionStrategyNames.HostedAgentFrameworkLegacy,
                },
            ],
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }
}
