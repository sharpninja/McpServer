using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

public sealed class WorkspaceTokenServiceTests
{
    private readonly WorkspaceTokenService _sut = new();

    [Fact]
    public void GenerateTokenReturnsNonEmptyToken()
    {
        var token = _sut.GenerateToken(@"C:\projects\test");
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void ValidateTokenMatchesGenerated()
    {
        var token = _sut.GenerateToken(@"C:\projects\test");
        Assert.True(_sut.ValidateToken(@"C:\projects\test", token));
    }

    [Fact]
    public void ValidateTokenRejectsMismatch()
    {
        _sut.GenerateToken(@"C:\projects\test");
        Assert.False(_sut.ValidateToken(@"C:\projects\test", "wrong-token"));
    }

    [Fact]
    public void GenerateDefaultTokenReturnsNonEmptyToken()
    {
        var token = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void ValidateDefaultTokenMatchesGenerated()
    {
        var token = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.True(_sut.ValidateDefaultToken(@"C:\projects\test", token));
    }

    [Fact]
    public void ValidateDefaultTokenRejectsMismatch()
    {
        _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.False(_sut.ValidateDefaultToken(@"C:\projects\test", "wrong"));
    }

    [Fact]
    public void IsDefaultTokenReturnsTrueForDefaultToken()
    {
        var token = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.True(_sut.IsDefaultToken(@"C:\projects\test", token));
    }

    [Fact]
    public void IsDefaultTokenReturnsFalseForFullToken()
    {
        var fullToken = _sut.GenerateToken(@"C:\projects\test");
        _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.False(_sut.IsDefaultToken(@"C:\projects\test", fullToken));
    }

    [Fact]
    public void FullAndDefaultTokensAreDifferent()
    {
        var full = _sut.GenerateToken(@"C:\projects\test");
        var def = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.NotEqual(full, def);
    }

    [Fact]
    public void GetDefaultTokenReturnsNullWhenNoneGenerated()
    {
        Assert.Null(_sut.GetDefaultToken(@"C:\projects\nonexistent"));
    }

    [Fact]
    public void FullTokenDoesNotValidateAsDefault()
    {
        var full = _sut.GenerateToken(@"C:\projects\test");
        Assert.False(_sut.ValidateDefaultToken(@"C:\projects\test", full));
    }

    [Fact]
    public void DefaultTokenDoesNotValidateAsFull()
    {
        var def = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.False(_sut.ValidateToken(@"C:\projects\test", def));
    }

    // --- Reverse lookup tests (TR-MCP-MT-002) ---

    [Fact]
    public void ResolveWorkspaceByTokenReturnsCorrectWorkspace()
    {
        var token = _sut.GenerateToken(@"C:\projects\test");
        var result = _sut.ResolveWorkspaceByToken(token);
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(@"C:\projects\test"), result, ignoreCase: true);
    }

    [Fact]
    public void ResolveWorkspaceByTokenReturnsNullForUnknownToken()
    {
        _sut.GenerateToken(@"C:\projects\test");
        Assert.Null(_sut.ResolveWorkspaceByToken("unknown-token-xyz"));
    }

    [Fact]
    public void ResolveWorkspaceByTokenReturnsNullForNullOrEmpty()
    {
        Assert.Null(_sut.ResolveWorkspaceByToken(null));
        Assert.Null(_sut.ResolveWorkspaceByToken(""));
        Assert.Null(_sut.ResolveWorkspaceByToken("   "));
    }

    [Fact]
    public void ResolveWorkspaceByTokenDefaultTokenReturnsCorrectWorkspace()
    {
        var token = _sut.GenerateDefaultToken(@"C:\projects\test");
        var result = _sut.ResolveWorkspaceByToken(token, out var isDefault);
        Assert.NotNull(result);
        Assert.True(isDefault);
    }

    [Fact]
    public void ResolveWorkspaceByTokenFullTokenIsNotDefault()
    {
        var token = _sut.GenerateToken(@"C:\projects\test");
        var result = _sut.ResolveWorkspaceByToken(token, out var isDefault);
        Assert.NotNull(result);
        Assert.False(isDefault);
    }

    [Fact]
    public void ResolveWorkspaceByTokenAfterTokenRotationOldTokenReturnsNull()
    {
        var oldToken = _sut.GenerateToken(@"C:\projects\test");
        var newToken = _sut.GenerateToken(@"C:\projects\test");

        Assert.Null(_sut.ResolveWorkspaceByToken(oldToken));
        Assert.NotNull(_sut.ResolveWorkspaceByToken(newToken));
    }

    [Fact]
    public void ResolveWorkspaceByTokenMultipleWorkspacesReturnsCorrectOne()
    {
        var tokenA = _sut.GenerateToken(@"C:\projects\alpha");
        var tokenB = _sut.GenerateToken(@"C:\projects\beta");
        var tokenC = _sut.GenerateToken(@"C:\projects\gamma");

        var resolvedA = _sut.ResolveWorkspaceByToken(tokenA);
        var resolvedB = _sut.ResolveWorkspaceByToken(tokenB);
        var resolvedC = _sut.ResolveWorkspaceByToken(tokenC);

        Assert.Contains("alpha", resolvedA!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("beta", resolvedB!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gamma", resolvedC!, StringComparison.OrdinalIgnoreCase);
    }
    // --- Readiness signal (TR-MCP-AUTH-011 / TEST-MCP-AUTH-012) ---

    /// <summary>TEST-MCP-AUTH-012: IsInitialized is false before any full token is generated.</summary>
    [Fact]
    public void IsInitializedNoTokensReturnsFalse()
    {
        Assert.False(_sut.IsInitialized);
    }

    /// <summary>TEST-MCP-AUTH-012: IsInitialized becomes true once a full token is generated.</summary>
    [Fact]
    public void IsInitializedAfterGenerateTokenReturnsTrue()
    {
        _sut.GenerateToken(@"C:\projects\test");
        Assert.True(_sut.IsInitialized);
    }

    /// <summary>TEST-MCP-AUTH-012: A default (anonymous) token alone does not mark the subsystem initialized.</summary>
    [Fact]
    public void IsInitializedDefaultTokenOnlyReturnsFalse()
    {
        _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.False(_sut.IsInitialized);
    }
}
