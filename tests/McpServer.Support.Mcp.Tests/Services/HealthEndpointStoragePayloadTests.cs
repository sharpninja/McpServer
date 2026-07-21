using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): the <c>/health</c> payload SHALL carry an explicit
/// <c>storage</c> reachability field (<c>reachable</c>|<c>unreachable</c>) while KEEPING liveness
/// semantics: a storage-only outage must not flip the top-level status away from Healthy and the
/// nonce echo must remain byte-for-byte intact (marker trust bootstrap depends on it).
/// Fixture: invokes the shared ServiceDefaults health response writer directly with a
/// <see cref="DefaultHttpContext"/> whose service provider registers a "storage"-tagged health
/// check, plus a live-only (empty) <see cref="HealthReport"/> exactly as <c>/health</c> produces
/// when only "live"-tagged checks run.
/// </summary>
public sealed class HealthEndpointStoragePayloadTests
{
    private static async Task<JsonDocument> InvokeHealthWriterAsync(
        IServiceProvider provider, string? nonceQuery, CancellationToken cancellationToken)
    {
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Path = "/health";
        if (nonceQuery is not null)
            context.Request.QueryString = new QueryString(nonceQuery);
        context.Response.Body = new MemoryStream();

        var writer = ServiceDefaultsExtensions.CreateHealthCheckResponseWriter(includeException: false);
        var liveOnlyReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(), TimeSpan.FromMilliseconds(1));

        await writer(context, liveOnlyReport).ConfigureAwait(true);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(true);
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// AC (TR-MCP-HEALTH-003): with an unreachable store the payload reports
    /// <c>storage: unreachable</c> while the top-level status stays Healthy and the nonce is
    /// echoed exactly. Red on the pre-fix writer: the payload has no <c>storage</c> field at all
    /// (plain Healthy), which this test quotes in its failure message.
    /// </summary>
    [Fact]
    public async Task HealthPayload_UnreachableStorage_ReportsStorageField_KeepsLivenessHealthy_AndEchoesNonce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck(
                "storage",
                () => HealthCheckResult.Unhealthy("Storage backend is unreachable."),
                tags: ["ready", "storage"]);
        await using var provider = services.BuildServiceProvider();

        using var document = await InvokeHealthWriterAsync(
            provider, "?nonce=nonce-echo-42", TestContext.Current.CancellationToken).ConfigureAwait(true);
        var root = document.RootElement;
        var payload = root.GetRawText();

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.Equal("nonce-echo-42", root.GetProperty("nonce").GetString());
        Assert.True(
            root.TryGetProperty("storage", out var storage),
            $"Expected an explicit 'storage' field on the /health payload; actual payload: {payload}");
        Assert.Equal("unreachable", storage.GetString());
    }

    /// <summary>
    /// AC (TR-MCP-HEALTH-003): with a reachable store the payload reports
    /// <c>storage: reachable</c> and the top-level status stays Healthy.
    /// </summary>
    [Fact]
    public async Task HealthPayload_ReachableStorage_ReportsStorageReachable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck(
                "storage",
                () => HealthCheckResult.Healthy("Storage backend is reachable."),
                tags: ["ready", "storage"]);
        await using var provider = services.BuildServiceProvider();

        using var document = await InvokeHealthWriterAsync(
            provider, nonceQuery: null, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var root = document.RootElement;
        var payload = root.GetRawText();

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.True(
            root.TryGetProperty("storage", out var storage),
            $"Expected an explicit 'storage' field on the /health payload; actual payload: {payload}");
        Assert.Equal("reachable", storage.GetString());
    }

    /// <summary>
    /// Compatibility guard (TR-MCP-HEALTH-003): services without a "storage"-tagged health check
    /// keep their existing payload shape; no <c>storage</c> field is invented. This guard passes
    /// both before and after the fix by design (it pins the backward-compatible shape).
    /// </summary>
    [Fact]
    public async Task HealthPayload_NoStorageCheckRegistered_OmitsStorageField()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks();
        await using var provider = services.BuildServiceProvider();

        using var document = await InvokeHealthWriterAsync(
            provider, nonceQuery: null, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var root = document.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.False(root.TryGetProperty("storage", out _), root.GetRawText());
    }
}
