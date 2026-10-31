namespace NukeBuild.Tests;

/// <summary>TR-MCP-NUKE-019: binds slice J's unchanged scripted contracts to the actual build helpers.</summary>
public sealed class IntegrationLaneExecutionBindingTests
{
    /// <summary>The real composition retains names, paths, filters, log messages and TRX names on both simulated hosts.</summary>
    [Fact]
    public void ProductionComposition_SatisfiesMockGatedContract() =>
        IntegrationLaneExecutionContractTests.AssertComposition((projects, host) => IntegrationLaneExecution.Compose(projects, host)
            .Select(step => new IntegrationLaneExecutionContractTests.Step(step.Name, step.Path, step.Filter, step.LogLine, step.TrxName)).ToArray());

    /// <summary>The actual result validator refuses empty, skipped, failed, inconsistent and malformed TRX evidence.</summary>
    [Fact]
    public void ProductionTrxCheck_SatisfiesMockGatedContract() =>
        IntegrationLaneExecutionContractTests.AssertTrx(xml => IntegrationLaneExecution.CheckTrx("project-under-test", xml) is null);

    /// <summary>The actual Linux worker start isolates all three handles and preserves exact argument boundaries.</summary>
    [Fact]
    public void ProductionLinuxWorker_SatisfiesMockGatedContract() =>
        IntegrationLaneExecutionContractTests.AssertLaunch(() =>
        {
            var start = OllamaDetachedLaunch.CreateLinuxStartInfo("tools/ollama", "owned/logs/ollama.log", "worker.ps1");
            return new(start.FileName, start.ArgumentList.ToArray(), start.UseShellExecute,
                start.RedirectStandardInput, start.RedirectStandardOutput, start.RedirectStandardError);
        });

    /// <summary>The actual invocation policy refuses a Windows lane before its callback and preserves standalone installs.</summary>
    [Fact]
    public void ProductionInvocationPolicy_SatisfiesMockGatedContract() =>
        IntegrationLaneExecutionContractTests.AssertInvocationPolicy(IntegrationLaneExecution.BeforeProvisioning);

    /// <summary>The real platform checks and selector interface satisfy the mock-gated unsupported-input and explicit-method cases.</summary>
    [Fact]
    public void ProductionPlatformAndInterface_SatisfyMockGatedContract()
    {
        IntegrationLaneExecutionContractTests.AssertPlatforms(IntegrationLaneExecution.HostName, IntegrationLaneExecution.PortableArchitectureFailure);
        IntegrationLaneExecutionContractTests.AssertExplicitClasses(typeof(IIntegrationLaneProjects));
    }
}
