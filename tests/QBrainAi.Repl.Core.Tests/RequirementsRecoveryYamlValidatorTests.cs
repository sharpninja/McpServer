using System.Collections.Generic;
using Xunit;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>Recovery YAML schema: titleless TEST items are valid; FR/TR still require title.</summary>
public sealed class RequirementsRecoveryYamlValidatorTests
{
    [Fact]
    public void PlanRecovery_TitlelessTest_IsValid()
    {
        var request = new RequestPayload
        {
            RequestId = "req-titleless-test-001",
            Method = RequirementsCommandShapes.PlanRecoveryMethod,
            Params = new Dictionary<string, object?>
            {
                ["idempotencyKey"] = "titleless-001",
                ["items"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["kind"] = "test",
                        ["id"] = "TEST-MCP-REQRECOVERY-001",
                        ["title"] = string.Empty,
                        ["body"] = "condition only",
                    },
                },
            },
        };

        var result = ReplYamlMessageValidator.ValidateRequest(request);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void PlanRecovery_TitlelessFr_IsInvalid()
    {
        var request = new RequestPayload
        {
            RequestId = "req-titleless-fr-001",
            Method = RequirementsCommandShapes.PlanRecoveryMethod,
            Params = new Dictionary<string, object?>
            {
                ["idempotencyKey"] = "titleless-fr-001",
                ["items"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["kind"] = "fr",
                        ["id"] = "FR-MCP-REQRECOVERY-001",
                        ["title"] = string.Empty,
                        ["body"] = "body",
                    },
                },
            },
        };

        var result = ReplYamlMessageValidator.ValidateRequest(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("title", StringComparison.OrdinalIgnoreCase));
    }
}