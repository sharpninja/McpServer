using McpServer.McpAgent.Hosting;
using McpServer.McpAgent.Todo;
using McpServer.Client;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.McpAgent.Tests;

/// <summary>
/// TEST-MCP-089: Verifies that the scaffolded registration surface resolves a placeholder hosted agent without workflow execution.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// TEST-MCP-089: Validates that an in-memory service collection configured with scaffold values resolves a hosted agent and projects those values into the MCP client.
    /// The test uses an in-memory service collection with a local workspace path and API key so the scaffold proves registration behavior without requiring a live MCP workflow runtime.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_RegistersScaffoldedHostedAgent()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.ApiKey = "scaffold-token";
            options.BaseUrl = new Uri("http://localhost:7147");
            options.Description = "Scaffolded MCP host agent";
            options.SourceType = "Copilot";
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();
        var hostedAgent = serviceProvider.GetRequiredService<IMcpHostedAgent>();
        var agentOptions = serviceProvider.GetRequiredService<ChatClientAgentOptions>();
        var clientOptions = serviceProvider.GetRequiredService<McpServerClientOptions>();
        var identifierFactory = serviceProvider.GetRequiredService<IMcpSessionIdentifierFactory>();
        var hostedAgentFactory = serviceProvider.GetRequiredService<IMcpHostedAgentFactory>();
        var todoWorkflow = serviceProvider.GetRequiredService<ITodoWorkflow>();
        var createdHostedAgent = hostedAgentFactory.CreateHostedAgent();

        Assert.Equal(McpHostedAgentDefaults.DefaultAgentName, hostedAgent.Name);
        Assert.Equal("Copilot", hostedAgent.SourceType);
        Assert.Equal(McpHostedAgentDefaults.DefaultAgentId, hostedAgent.AgentOptions.Id);
        Assert.Equal("Scaffolded MCP host agent", hostedAgent.AgentOptions.Description);
        Assert.Equal("scaffold-token", hostedAgent.Client.ApiKey);
        Assert.Equal(7147, hostedAgent.Client.Port);
        Assert.Equal(@"E:\github\McpServer", hostedAgent.Client.WorkspacePath);
        Assert.Equal(agentOptions.Id, hostedAgent.AgentOptions.Id);
        Assert.Equal(agentOptions.Name, hostedAgent.AgentOptions.Name);
        Assert.Equal(agentOptions.Description, hostedAgent.AgentOptions.Description);
        Assert.Equal("scaffold-token", clientOptions.ApiKey);
        Assert.Equal(@"E:\github\McpServer", clientOptions.WorkspacePath);
        Assert.Same(identifierFactory, hostedAgent.Identifiers);
        Assert.Equal("Copilot", hostedAgent.Identifiers.SourceType);
        Assert.NotSame(hostedAgent, createdHostedAgent);
        Assert.IsType<McpServer.McpAgent.SessionLog.SessionLogWorkflow>(hostedAgent.SessionLog);
        Assert.IsType<TodoWorkflow>(todoWorkflow);
        Assert.IsType<TodoWorkflow>(hostedAgent.Todo);
        Assert.Null(hostedAgent.SessionLog.Context);
        Assert.NotNull(hostedAgent.Registration);
        Assert.NotEmpty(hostedAgent.Registration.Functions);
        Assert.NotEmpty(hostedAgentFactory.CreateRegistration().Tools);
    }

    /// <summary>
    /// TEST-MCP-186: Verifies that the ACID profile projects stable Agent Framework metadata and
    /// strict invariants through normal dependency injection without changing default registration.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_RegistersAcidTightlyCoupledProfile()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.UseAcidTightlyCoupledProfile();
            options.ApiKey = "acid-token";
            options.BaseUrl = new Uri("http://localhost:7147");
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();
        var hostedAgent = serviceProvider.GetRequiredService<IMcpHostedAgent>();
        var agentOptions = serviceProvider.GetRequiredService<ChatClientAgentOptions>();
        var definition = QBAgentDefinition.Instance;

        Assert.Equal(McpAgentExecutionProfile.AcidTightlyCoupled, hostedAgent.ExecutionProfile);
        Assert.Equal(definition.AgentId, hostedAgent.AgentOptions.Id);
        Assert.Equal(definition.AgentName, hostedAgent.AgentOptions.Name);
        Assert.Equal(definition.Description, hostedAgent.AgentOptions.Description);
        Assert.Equal(definition.SourceType, hostedAgent.SourceType);
        Assert.Equal(definition.AgentId, agentOptions.Id);
        Assert.Equal("acid-token", hostedAgent.Client.ApiKey);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the registration surface exposes the options validator for fast-fail host startup behavior.
    /// The test resolves validators from a local service provider because this todo only covers registration wiring, not runtime workflow execution.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_RegistersOptionsValidator()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.ApiKey = "token";
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();
        var validators = serviceProvider.GetServices<IValidateOptions<McpAgentOptions>>();

        Assert.Contains(validators, static validator => validator is McpAgentOptionsValidator);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the hosted-agent factory is registered so hosts can create fresh
    /// stateful wrappers instead of reusing a single session-log workflow context across runs.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_RegistersHostedAgentFactory()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.ApiKey = "factory-token";
            options.SourceType = "Codex";
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IMcpHostedAgentFactory>();
        var hostedAgent = factory.CreateHostedAgent();

        Assert.NotNull(factory);
        Assert.NotNull(hostedAgent);
        Assert.Equal("Codex", hostedAgent.SourceType);
        Assert.NotEmpty(factory.CreateRegistration().Functions);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that authentication is required by default so hosted agents fail fast before making MCP requests.
    /// The test intentionally omits both ApiKey and BearerToken while keeping a valid localhost base URL and fully qualified workspace path to isolate the auth validation path.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_ThrowsWhenAuthenticationIsMissing()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.BaseUrl = new Uri("http://localhost:7147");
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMcpHostedAgent>());

        Assert.Contains("Either ApiKey or BearerToken must be configured", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that bearer-token-only registration is accepted for authenticated hosts that do not use API keys.
    /// The test uses a localhost HTTPS endpoint and a fake bearer token because no live MCP transport is needed to confirm DI registration behavior.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_AllowsBearerTokenAuthentication()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.BaseUrl = new Uri("https://localhost:7147");
            options.BearerToken = "bearer-token";
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();
        var hostedAgent = serviceProvider.GetRequiredService<IMcpHostedAgent>();

        Assert.Equal("bearer-token", hostedAgent.Client.BearerToken);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that invalid source types are rejected before the hosted agent is resolved.
    /// The test uses a hyphenated source type because later session-log workflows require canonical Pascal-case agent prefixes that match the shared naming rules.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_ThrowsWhenSourceTypeIsInvalid()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.ApiKey = "token";
            options.SourceType = "agent-framework";
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMcpHostedAgent>());

        Assert.Contains("SourceType must match ^[A-Z][A-Za-z0-9]*$", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that relative workspace paths are rejected so the client only sends stable workspace identifiers.
    /// The test uses a relative path string to isolate the workspace-header validation behavior without involving any filesystem access.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_ThrowsWhenWorkspacePathIsNotFullyQualified()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.ApiKey = "token";
            options.WorkspacePath = "relative\\workspace";
        });

        using var serviceProvider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMcpHostedAgent>());

        Assert.Contains("WorkspacePath must be fully qualified", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-186: Verifies that the ACID profile fails closed when the host omits the required
    /// workspace binding used for audit and transaction scope.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_ThrowsWhenAcidProfileWorkspacePathIsMissing()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.UseAcidTightlyCoupledProfile();
            options.ApiKey = "token";
        });

        using var serviceProvider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMcpHostedAgent>());

        Assert.Contains("WorkspacePath is required for the ACID tightly coupled", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that non-HTTP base URLs are rejected before the hosted transport client is constructed.
    /// The test uses an FTP URI to prove only the HTTP/S schemes accepted by the MCP client surface are allowed during options validation.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_ThrowsWhenBaseUrlSchemeIsInvalid()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.ApiKey = "token";
            options.BaseUrl = new Uri("ftp://localhost:7147");
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMcpHostedAgent>());

        Assert.Contains("BaseUrl must use the http or https scheme", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that hosts can explicitly disable authentication validation for safe unauthenticated scenarios.
    /// The test leaves credentials unset but supplies a valid localhost base URL and workspace path so only the opt-out authentication flag controls the result.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_AllowsMissingAuthenticationWhenDisabled()
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.BaseUrl = new Uri("http://localhost:7147");
            options.RequireAuthentication = false;
            options.WorkspacePath = @"E:\github\McpServer";
        });

        using var serviceProvider = services.BuildServiceProvider();
        var hostedAgent = serviceProvider.GetRequiredService<IMcpHostedAgent>();

        Assert.NotNull(hostedAgent);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that hosts can preconfigure <see cref="McpAgentOptions"/> through the standard options pipeline and then register the Agent Framework services.
    /// The test uses a locally configured options instance to prove the parameterless registration overload remains DI-friendly for hosts that bind configuration outside the extension method.
    /// </summary>
    [Fact]
    public void AddMcpServerMcpAgent_UsesPreconfiguredOptions()
    {
        var services = new ServiceCollection();
        services.AddOptions<McpAgentOptions>()
            .Configure(options =>
            {
                options.ApiKey = "preconfigured-token";
                options.AgentId = "preconfigured-agent";
                options.AgentName = "Preconfigured Agent";
                options.Description = "Configured before registration.";
                options.SourceType = "Codex";
                options.WorkspacePath = @"E:\github\McpServer";
            });

        services.AddMcpServerMcpAgent();

        using var serviceProvider = services.BuildServiceProvider();
        var hostedAgent = serviceProvider.GetRequiredService<IMcpHostedAgent>();

        Assert.Equal("Preconfigured Agent", hostedAgent.Name);
        Assert.Equal("Codex", hostedAgent.SourceType);
        Assert.Equal("preconfigured-agent", hostedAgent.AgentOptions.Id);
        Assert.Equal("preconfigured-token", hostedAgent.Client.ApiKey);
    }
}
