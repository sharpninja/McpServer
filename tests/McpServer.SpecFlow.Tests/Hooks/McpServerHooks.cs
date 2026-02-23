using Reqnroll;

namespace McpServer.SpecFlow.Tests.Hooks;

/// <summary>
/// Reqnroll (SpecFlow) hooks that manage the shared McpServer web application factory
/// for all integration scenarios.
/// </summary>
[Binding]
public sealed class McpServerHooks
{
    private static Support.McpSpecFlowWebApplicationFactory? _factory;

    /// <summary>Creates the web application factory once before the entire test run.</summary>
    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        _factory = new Support.McpSpecFlowWebApplicationFactory();
    }

    /// <summary>Disposes the web application factory after the entire test run.</summary>
    [AfterTestRun]
    public static void AfterTestRun()
    {
        _factory?.Dispose();
        _factory = null;
    }

    /// <summary>Returns the shared web application factory.</summary>
    public static Support.McpSpecFlowWebApplicationFactory Factory =>
        _factory ?? throw new InvalidOperationException("Factory not initialized. Ensure BeforeTestRun has been called.");

    /// <summary>Creates an HttpClient scoped to one scenario.</summary>
    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        var client = Factory.CreateClient();
        scenarioContext.Set(client, "HttpClient");
    }

    /// <summary>Disposes the scenario-scoped HttpClient.</summary>
    [AfterScenario]
    public void AfterScenario(ScenarioContext scenarioContext)
    {
        if (scenarioContext.TryGetValue("HttpClient", out HttpClient client))
        {
            client.Dispose();
        }
    }
}
