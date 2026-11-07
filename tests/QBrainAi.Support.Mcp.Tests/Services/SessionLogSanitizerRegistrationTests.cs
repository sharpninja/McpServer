namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Source-level registration tests for the session-log sanitizer decorator.
/// </summary>
public sealed class SessionLogSanitizerRegistrationTests
{
    /// <summary>
    /// HTTP session logging must sanitize after local transaction gating, federation merge, and pagination.
    /// </summary>
    /// <remarks>Integration: reads the live HTTP or stdio host registration source (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Program_RegistersSessionLogSanitizerAsOutermostHttpDecorator()
    {
        var source = ReadRepoFile(Path.Combine("src", "QBrainAi.Support.Mcp", "Program.cs"));
        var sanitizerRegistration = source.IndexOf(
            "AddScoped<ISessionLogSanitizer, SessionLogSanitizer>()",
            StringComparison.Ordinal);
        var federatedConstruction = source.IndexOf(
            "new FederatedSessionLogService(",
            StringComparison.Ordinal);
        var sanitizerConstruction = source.IndexOf(
            "new SessionLogSanitizingService(",
            StringComparison.Ordinal);

        Assert.True(sanitizerRegistration >= 0, "Program.cs must register ISessionLogSanitizer.");
        Assert.True(federatedConstruction >= 0, "Program.cs must retain federated session-log merging.");
        Assert.True(sanitizerConstruction > federatedConstruction, "Program.cs must wrap the federated service with the sanitizer as the outermost decorator.");
    }

    /// <summary>
    /// Stdio session logging must sanitize after the transaction gate so all read transports share one behavior.
    /// </summary>
    /// <remarks>Integration: reads the live HTTP or stdio host registration source (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void McpStdioHost_RegistersSessionLogSanitizerAsOutermostStdioDecorator()
    {
        var source = ReadRepoFile(Path.Combine("src", "QBrainAi.Support.Mcp", "McpStdio", "McpStdioHost.cs"));
        var sanitizerRegistration = source.IndexOf(
            "AddScoped<ISessionLogSanitizer, SessionLogSanitizer>()",
            StringComparison.Ordinal);
        var transactionGateConstruction = source.IndexOf(
            "new TransactionGatedSessionLogService(",
            StringComparison.Ordinal);
        var sanitizerConstruction = source.IndexOf(
            "new SessionLogSanitizingService(",
            StringComparison.Ordinal);

        Assert.True(sanitizerRegistration >= 0, "McpStdioHost.cs must register ISessionLogSanitizer.");
        Assert.True(transactionGateConstruction >= 0, "McpStdioHost.cs must retain transaction-gated session logging.");
        Assert.True(sanitizerConstruction > transactionGateConstruction, "McpStdioHost.cs must wrap the transaction-gated service with the sanitizer as the outermost decorator.");
    }

    private static string ReadRepoFile(string relativePath) =>
        File.ReadAllText(QBrainAi.Tests.Shared.TestRepositoryRoot.Combine(relativePath));
}

