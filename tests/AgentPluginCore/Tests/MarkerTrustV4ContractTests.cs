// MarkerTrustV4ContractTests.cs
// Comprehensive xUnit.v3 contract tests for v4 marker trust (signature + nonce).
// Mocks-first (NSubstitute on IV4FileSystem + IV4HealthClient) per Byrd v4 + PARITY-RESUME-004.
// All tests green against V4MarkerTrustStub before any real shared core implementation (TR-012, plan Phase 1).
// Full AC coverage from docs/plans/plan-agent-plugin-operational-parity-v1.0.md : upward search, HMAC success/fail (MCP_UNTRUSTED exact), nonce challenge.

namespace McpServer.AgentPluginCore.Tests.Tests;

using McpServer.AgentPluginCore.Tests.Contracts;
using McpServer.AgentPluginCore.Tests.Stubs;

/// <summary>
/// Tests the v4 marker trust bootstrap contract (TR-MCP-AGENT-PARITY-012).
/// Validates identical behavior required for all 8 plugins and shell/TS shims.
/// Uses mocks for filesystem and health endpoint; stub provides v4 HMAC+nonce logic.
/// </summary>
public class MarkerTrustV4ContractTests
{
    private readonly IV4FileSystem _fs;
    private readonly IV4HealthClient _health;
    private readonly V4MarkerTrustStub _sut;

    public MarkerTrustV4ContractTests()
    {
        _fs = Substitute.For<IV4FileSystem>();
        _health = Substitute.For<IV4HealthClient>();
        _sut = new V4MarkerTrustStub(_fs, _health);
    }

    /// <summary>
    /// Verifies upward directory walk finds AGENTS-README-FIRST.yaml (AC from plan).
    /// Fixture: marker at ancestor dir; start deep in tree.
    /// </summary>
    [Fact]
    public async Task FindMarkerFileAsync_UpwardWalk_FindsMarkerInAncestor()
    {
        _fs.FileExistsAsync(Arg.Any<string>()).Returns(false);
        _fs.FileExistsAsync(Arg.Is<string>(p => p.EndsWith("AGENTS-README-FIRST.yaml") && p.Contains("workspace"))).Returns(true);

        var found = await _sut.FindMarkerFileAsync(@"F:\workspaces\deep\nested\project");

        Assert.NotNull(found);
        Assert.EndsWith("AGENTS-README-FIRST.yaml", found);
        await _fs.Received().FileExistsAsync(Arg.Any<string>());
    }

    /// <summary>
    /// Valid HMAC-SHA256 signature (v4 binding) succeeds and returns marker data.
    /// Uses known-good signature computed by stub logic.
    /// </summary>
    [Fact]
    public async Task VerifySignatureAndParseAsync_ValidV4HmacSignature_ReturnsMarkerData()
    {
        var yaml = "workspacePath: /ws\nserverUrl: http://localhost:5177\napiKey: test-api-key-123\nsignature: PLACEHOLDER\nnonce: n-001";
        // Pre-compute correct v4 sig for this payload (apiKey|path|nonce)
        var correctSig = ComputeTestHmac("test-api-key-123", "test-api-key-123|/ws|n-001");
        yaml = yaml.Replace("PLACEHOLDER", correctSig);

        _fs.ReadAllTextAsync(Arg.Any<string>()).Returns(yaml);

        var data = await _sut.VerifySignatureAndParseAsync("/ws/AGENTS-README-FIRST.yaml");

        Assert.NotNull(data);
        Assert.Equal("/ws", data.WorkspacePath);
        Assert.Equal("test-api-key-123", data.ApiKey);
        Assert.Equal(correctSig, data.Signature);
    }

    /// <summary>
    /// Bad signature produces exact MCP_UNTRUSTED error (required observable contract).
    /// </summary>
    [Fact]
    public async Task VerifySignatureAndParseAsync_BadSignature_ThrowsMcpUntrusted()
    {
        var yaml = "workspacePath: /ws\nserverUrl: http://localhost:5177\napiKey: test-api-key-123\nsignature: deadbeefbad\nnonce: n-001";
        _fs.ReadAllTextAsync(Arg.Any<string>()).Returns(yaml);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.VerifySignatureAndParseAsync("/ws/AGENTS-README-FIRST.yaml"));

        Assert.Contains("MCP_UNTRUSTED", ex.Message);
        Assert.Contains("signature verification failed", ex.Message);
    }

    /// <summary>
    /// Nonce health challenge happy path (response contains nonce + ok).
    /// </summary>
    [Fact]
    public async Task PerformNonceHealthChallengeAsync_ValidResponse_ReturnsTrue()
    {
        var marker = Substitute.For<IV4MarkerData>();
        marker.ServerUrl.Returns("http://localhost:5177");
        marker.Nonce.Returns("abc123");

        _health.GetNonceResponseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns("{\"status\":\"ok\",\"nonce\":\"abc123\"}");

        var ok = await _sut.PerformNonceHealthChallengeAsync(marker);

        Assert.True(ok);
        await _health.Received(1).GetNonceResponseAsync(Arg.Is<string>(u => u.Contains("nonce=abc123")), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nonce challenge failure or bad response -> untrusted path.
    /// </summary>
    [Fact]
    public async Task PerformNonceHealthChallengeAsync_MissingNonceInResponse_ReturnsFalse()
    {
        var marker = Substitute.For<IV4MarkerData>();
        marker.ServerUrl.Returns("http://localhost:5177");
        marker.Nonce.Returns("xyz789");

        _health.GetNonceResponseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{\"status\":\"ok\"}");

        var ok = await _sut.PerformNonceHealthChallengeAsync(marker);

        Assert.False(ok);
    }

    /// <summary>
    /// Full bootstrap happy path (v4: find + sig + nonce) yields trusted result.
    /// </summary>
    [Fact]
    public async Task BootstrapTrustAsync_ValidMarkerAndNonce_ReturnsTrustedWithV4Method()
    {
        var yaml = "workspacePath: /trusted\nserverUrl: http://localhost:5177\napiKey: k1\nsignature: PLACEHOLDER\nnonce: n1";
        var sig = ComputeTestHmac("k1", "k1|/trusted|n1");
        yaml = yaml.Replace("PLACEHOLDER", sig);
        _fs.FileExistsAsync(Arg.Any<string>()).Returns(true);
        _fs.ReadAllTextAsync(Arg.Any<string>()).Returns(yaml);
        _health.GetNonceResponseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns("{\"status\":\"ok\",\"nonce\":\"n1\"}");

        var result = await _sut.BootstrapTrustAsync("/trusted/project");

        Assert.True(result.IsTrusted);
        Assert.Contains("signature_verified+nonce_v4", result.TrustMethod);
        Assert.NotNull(result.MarkerData);
    }

    /// <summary>
    /// Bad signature in bootstrap produces MCP_UNTRUSTED trust result (exact observable for plugins).
    /// </summary>
    [Fact]
    public async Task BootstrapTrustAsync_BadSignature_ProducesMcpUntrustedResult()
    {
        var yaml = "workspacePath: /bad\nserverUrl: http://localhost:5177\napiKey: k1\nsignature: wrong\nnonce: n1";
        _fs.FileExistsAsync(Arg.Any<string>()).Returns(true);
        _fs.ReadAllTextAsync(Arg.Any<string>()).Returns(yaml);

        var result = await _sut.BootstrapTrustAsync("/bad/project");

        Assert.False(result.IsTrusted);
        Assert.Equal("MCP_UNTRUSTED", result.TrustMethod);
        Assert.Contains("signature", result.DenialReason);
    }

    private static string ComputeTestHmac(string key, string data)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }
}
