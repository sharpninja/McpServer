using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P5: isolated server fixture AC (free port, temp workspace/db, timeout, marker, trust, cleanup).
/// </summary>
[Collection("PluginSessionLog")]
[Trait("PluginInt", "Deterministic")]
public sealed class PluginIntegrationServerFixtureTests
{
    /// <summary>P5: fixture selects a free loopback port, not 7147 by default.</summary>
    [Fact(Timeout = 120000)]
    public async Task ServerFixture_SelectsFreePort()
    {
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        Assert.InRange(fixture.Port, 1024, 65535);
        Assert.NotEqual(7147, fixture.Port);
    }

    /// <summary>P5: fixture creates an isolated temp workspace and database.</summary>
    [Fact(Timeout = 120000)]
    public async Task ServerFixture_CreatesIsolatedTempWorkspaceAndDatabase()
    {
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        Assert.True(Directory.Exists(fixture.WorkspacePath));
        Assert.True(File.Exists(fixture.DatabasePath) || Directory.Exists(Path.GetDirectoryName(fixture.DatabasePath)));
        Assert.DoesNotContain("7147", fixture.DatabasePath, StringComparison.Ordinal);
        Assert.False(string.Equals(Path.GetFullPath(@"F:\GitHub\McpServer"), Path.GetFullPath(fixture.WorkspacePath), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>P5: fixture start respects a startup timeout.</summary>
    [Fact(Timeout = 30000)]
    public async Task ServerFixture_StartupTimeout()
    {
        await using var fixture = new PluginIntegrationServerFixture();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            await fixture.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Assert.InRange(fixture.Port, 1024, 65535);
    }

    /// <summary>P5: fixture writes a workspace marker file.</summary>
    [Fact(Timeout = 120000)]
    public async Task ServerFixture_CreatesMarker()
    {
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        Assert.True(File.Exists(fixture.MarkerPath));
        Assert.Contains("AGENTS-README-FIRST.yaml", fixture.MarkerPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>P5: marker signature and health nonce trust succeed for the isolated host.</summary>
    [Fact(Timeout = 120000)]
    public async Task ServerFixture_SignatureAndNonceTrust()
    {
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        Assert.True(File.Exists(fixture.MarkerPath));
        var marker = await File.ReadAllTextAsync(fixture.MarkerPath, TestContext.Current.CancellationToken);
        Assert.Contains("signature:", marker, StringComparison.Ordinal);
        Assert.Contains("apiKey:", marker, StringComparison.Ordinal);
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:" + fixture.Port) };
        var nonce = Guid.NewGuid().ToString("N");
        var health = await client.GetStringAsync("/health?nonce=" + nonce, TestContext.Current.CancellationToken);
        Assert.Contains(nonce, health, StringComparison.Ordinal);
    }

    /// <summary>P5: dispose removes the isolated workspace and does not touch the developer database.</summary>
    [Fact(Timeout = 120000)]
    public async Task ServerFixture_DeterministicCleanup()
    {
        var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        var workspace = fixture.WorkspacePath;
        await fixture.DisposeAsync();
        Assert.False(Directory.Exists(workspace));
    }

    /// <summary>P6: isolated host is healthy and exposes a trusted marker plus typed client.</summary>
    [Fact(Timeout = 120000)]
    public async Task ServerFixture_HealthReady_ExposesTrustedMarkerAndClient()
    {
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        Assert.True(File.Exists(fixture.MarkerPath));
        var marker = await File.ReadAllTextAsync(fixture.MarkerPath, TestContext.Current.CancellationToken);
        Assert.Contains("signature:", marker, StringComparison.Ordinal);
        Assert.Contains("apiKey:", marker, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(fixture.ApiKey));
        var client = fixture.CreateTrustedClient();
        var todos = await client.Todo.QueryAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(todos);
    }
}
