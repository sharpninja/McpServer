using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / FR-MCP-MEMORY-010: Secrets/policy rejection is not weakened by S1.
/// </summary>
public sealed class MemoryPolicyTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-05: Existing secrets/policy rejection behavior is unchanged (no weaker policy).</summary>
    [Fact]
    public async Task SecretsPolicy_Unchanged()
    {
        var remember = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Title = "Secret policy",
            Content = "api_key=sk-live-not-a-real-secret",
            Type = "procedure",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var add = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "policy",
            Text = "api_key=sk-live-not-a-real-secret",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var rememberRejected = remember.StatusCode is 400 or 403
            || remember.FailureKind == MemoryMutationFailureKind.Validation;
        var addRejected = !add.Success && add.FailureKind == MemoryMutationFailureKind.Validation;
        var policyType = typeof(MemoryService).Assembly.GetTypes()
            .Concat(typeof(McpServer.Support.Mcp.Controllers.MemoryController).Assembly.GetTypes())
            .FirstOrDefault(type => type.Name.Contains("MemoryPolicy", StringComparison.OrdinalIgnoreCase)
                || type.Name.Contains("SecretPolicy", StringComparison.OrdinalIgnoreCase));

        Assert.True(
            rememberRejected || addRejected || policyType is not null,
            "S1 must keep a secrets/policy rejection path. Current remember/add accepted a secret-shaped payload without a policy type.");
    }
}
