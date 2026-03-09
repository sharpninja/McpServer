using System.Text;
using McpServer.AgentFramework;
using McpServer.AgentFramework.AgentFramework;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.AgentFramework.SampleHost;

internal static class SampleHostPreviewFactory
{
    private const string ApiKeyEnvironmentVariable = "MCP_SERVER_API_KEY";
    private const string BearerTokenEnvironmentVariable = "MCP_SERVER_BEARER_TOKEN";
    private const string BaseUrlEnvironmentVariable = "MCP_SERVER_BASE_URL";
    private const string WorkspacePathEnvironmentVariable = "MCP_SERVER_WORKSPACE_PATH";
    private const string AgentIdEnvironmentVariable = "MCP_AGENT_ID";
    private const string AgentNameEnvironmentVariable = "MCP_AGENT_NAME";
    private const string AgentDescriptionEnvironmentVariable = "MCP_AGENT_DESCRIPTION";
    private const string SourceTypeEnvironmentVariable = "MCP_AGENT_SOURCE_TYPE";

    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMcpServerAgentFramework(ConfigureOptionsFromEnvironment);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    public static SampleHostPreview CreatePreview(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var options = serviceProvider.GetRequiredService<IOptions<McpAgentFrameworkOptions>>().Value;
        var factory = serviceProvider.GetRequiredService<IMcpHostedAgentFactory>();
        var hostedAgent = factory.CreateHostedAgent();
        var registration = hostedAgent.Registration;
        var hostTool = AIFunctionFactory.Create(
            (Func<string>)(() => "sample-host-preview"),
            new AIFunctionFactoryOptions
            {
                Description = "Demonstrates host-supplied tools merging with the built-in MCP workflow tools.",
                Name = "sample_host_info",
            });
        var runOptions = hostedAgent.CreateRunOptions(
            new ChatClientAgentRunOptions
            {
                ChatOptions = new ChatOptions
                {
                    Tools = [hostTool],
                },
            });

        using var previewChatClient = new PreviewChatClient();
        var chatClientAgent = hostedAgent.CreateChatClientAgent(previewChatClient);

        var wrappedChatClient = runOptions.ChatClientFactory?.Invoke(new PreviewChatClient());
        var wrappedChatClientType = wrappedChatClient?.GetType().FullName ?? "<not configured>";

        if (wrappedChatClient is IDisposable disposable)
            disposable.Dispose();

        var authenticationMode = GetAuthenticationMode(hostedAgent.Client.ApiKey, hostedAgent.Client.BearerToken);
        var registrationToolNames = registration.Tools.Select(static tool => tool.Name).ToArray();
        var attachedRunToolNames = runOptions.ChatOptions?.Tools?.Select(static tool => tool.Name).ToArray() ?? [];

        return new SampleHostPreview(
            hostedAgent.Name,
            hostedAgent.AgentOptions.Id ?? hostedAgent.Name,
            hostedAgent.SourceType,
            hostedAgent.AgentOptions.Description ?? string.Empty,
            options.BaseUrl,
            hostedAgent.Client.WorkspacePath ?? "<unspecified>",
            authenticationMode,
            hostedAgent.Identifiers.CreateSessionId("sample-host"),
            hostedAgent.Identifiers.CreateRequestId("preview"),
            chatClientAgent.Name ?? hostedAgent.Name,
            chatClientAgent.Description ?? string.Empty,
            wrappedChatClientType,
            registrationToolNames,
            attachedRunToolNames);
    }

    private static void ConfigureOptionsFromEnvironment(McpAgentFrameworkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ApiKey = ReadEnvironmentVariable(ApiKeyEnvironmentVariable);
        options.BearerToken = ReadEnvironmentVariable(BearerTokenEnvironmentVariable);
        options.RequireAuthentication = !string.IsNullOrWhiteSpace(options.ApiKey)
            || !string.IsNullOrWhiteSpace(options.BearerToken);
        options.WorkspacePath = ReadEnvironmentVariable(WorkspacePathEnvironmentVariable)
            ?? Path.GetFullPath(Environment.CurrentDirectory);
        options.BaseUrl = ResolveBaseUrl();
        options.AgentId = ReadEnvironmentVariable(AgentIdEnvironmentVariable) ?? options.AgentId;
        options.AgentName = ReadEnvironmentVariable(AgentNameEnvironmentVariable) ?? options.AgentName;
        options.Description = ReadEnvironmentVariable(AgentDescriptionEnvironmentVariable) ?? options.Description;
        options.SourceType = ReadEnvironmentVariable(SourceTypeEnvironmentVariable) ?? options.SourceType;
    }

    private static Uri ResolveBaseUrl()
    {
        var configuredBaseUrl = ReadEnvironmentVariable(BaseUrlEnvironmentVariable);
        if (configuredBaseUrl is null)
            return new Uri("http://localhost:7147");

        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUrl))
            return baseUrl;

        throw new InvalidOperationException(
            $"Environment variable '{BaseUrlEnvironmentVariable}' must contain an absolute http/https URI.");
    }

    private static string? ReadEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string GetAuthenticationMode(string? apiKey, string? bearerToken)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
            return "API key";

        if (!string.IsNullOrWhiteSpace(bearerToken))
            return "Bearer token";

        return "None (preview mode)";
    }

    internal sealed record SampleHostPreview(
        string HostedAgentName,
        string HostedAgentId,
        string SourceType,
        string HostedAgentDescription,
        Uri BaseUrl,
        string WorkspacePath,
        string AuthenticationMode,
        string ExampleSessionId,
        string ExampleRequestId,
        string ChatClientAgentName,
        string ChatClientAgentDescription,
        string WrappedChatClientType,
        IReadOnlyList<string> RegistrationToolNames,
        IReadOnlyList<string> AttachedRunToolNames)
    {
        public string ToDisplayText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("MCP Server Agent Framework sample host");
            builder.AppendLine("-------------------------------------");
            builder.AppendLine($"Hosted agent: {HostedAgentName} ({HostedAgentId})");
            builder.AppendLine($"Description : {HostedAgentDescription}");
            builder.AppendLine($"Source type : {SourceType}");
            builder.AppendLine($"Base URL    : {BaseUrl}");
            builder.AppendLine($"Workspace   : {WorkspacePath}");
            builder.AppendLine($"Auth mode   : {AuthenticationMode}");
            builder.AppendLine();
            builder.AppendLine("Canonical identifier examples");
            builder.AppendLine($"- Session: {ExampleSessionId}");
            builder.AppendLine($"- Request: {ExampleRequestId}");
            builder.AppendLine();
            builder.AppendLine("Agent Framework adapter preview");
            builder.AppendLine($"- ChatClientAgent: {ChatClientAgentName}");
            builder.AppendLine($"- Agent description: {ChatClientAgentDescription}");
            builder.AppendLine($"- Run wrapper: {WrappedChatClientType}");
            builder.AppendLine($"- Built-in MCP tools ({RegistrationToolNames.Count}):");

            foreach (var toolName in RegistrationToolNames)
                builder.AppendLine($"  - {toolName}");

            builder.AppendLine("- Tools attached to sample run options:");
            foreach (var toolName in AttachedRunToolNames)
                builder.AppendLine($"  - {toolName}");

            builder.AppendLine();
            builder.AppendLine("Set MCP_SERVER_BASE_URL, MCP_SERVER_WORKSPACE_PATH, MCP_SERVER_API_KEY, or");
            builder.AppendLine("MCP_SERVER_BEARER_TOKEN to point the sample at a live MCP Server workspace.");
            builder.AppendLine("Without credentials, the sample stays in preview mode and only demonstrates");
            builder.AppendLine("registration, identifier generation, ChatClientAgent construction, and tool wiring.");
            return builder.ToString();
        }
    }

    private sealed class PreviewChatClient : IChatClient
    {
        public void Dispose()
        {
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException(
                "The sample preview chat client exists only to demonstrate ChatClientAgent and run-option construction.");

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException(
                "The sample preview chat client exists only to demonstrate ChatClientAgent and run-option construction.");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
