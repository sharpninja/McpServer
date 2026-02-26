using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class WorkspaceTokenServiceTests
{
    private readonly WorkspaceTokenService _sut = new();

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        var token = _sut.GenerateToken(@"C:\projects\test");
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void ValidateToken_MatchesGenerated()
    {
        var token = _sut.GenerateToken(@"C:\projects\test");
        Assert.True(_sut.ValidateToken(@"C:\projects\test", token));
    }

    [Fact]
    public void ValidateToken_RejectsMismatch()
    {
        _sut.GenerateToken(@"C:\projects\test");
        Assert.False(_sut.ValidateToken(@"C:\projects\test", "wrong-token"));
    }

    [Fact]
    public void GenerateDefaultToken_ReturnsNonEmptyToken()
    {
        var token = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void ValidateDefaultToken_MatchesGenerated()
    {
        var token = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.True(_sut.ValidateDefaultToken(@"C:\projects\test", token));
    }

    [Fact]
    public void ValidateDefaultToken_RejectsMismatch()
    {
        _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.False(_sut.ValidateDefaultToken(@"C:\projects\test", "wrong"));
    }

    [Fact]
    public void IsDefaultToken_ReturnsTrueForDefaultToken()
    {
        var token = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.True(_sut.IsDefaultToken(@"C:\projects\test", token));
    }

    [Fact]
    public void IsDefaultToken_ReturnsFalseForFullToken()
    {
        var fullToken = _sut.GenerateToken(@"C:\projects\test");
        _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.False(_sut.IsDefaultToken(@"C:\projects\test", fullToken));
    }

    [Fact]
    public void FullAndDefaultTokens_AreDifferent()
    {
        var full = _sut.GenerateToken(@"C:\projects\test");
        var def = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.NotEqual(full, def);
    }

    [Fact]
    public void GetDefaultToken_ReturnsNullWhenNoneGenerated()
    {
        Assert.Null(_sut.GetDefaultToken(@"C:\projects\nonexistent"));
    }

    [Fact]
    public void FullToken_DoesNotValidateAsDefault()
    {
        var full = _sut.GenerateToken(@"C:\projects\test");
        Assert.False(_sut.ValidateDefaultToken(@"C:\projects\test", full));
    }

    [Fact]
    public void DefaultToken_DoesNotValidateAsFull()
    {
        var def = _sut.GenerateDefaultToken(@"C:\projects\test");
        Assert.False(_sut.ValidateToken(@"C:\projects\test", def));
    }

    // --- Reverse lookup tests (TR-MCP-MT-002) ---

    [Fact]
    public void ResolveWorkspaceByToken_ReturnsCorrectWorkspace()
    {
        var token = _sut.GenerateToken(@"C:\projects\test");
        var result = _sut.ResolveWorkspaceByToken(token);
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath(@"C:\projects\test"), result, ignoreCase: true);
    }

    [Fact]
    public void ResolveWorkspaceByToken_ReturnsNull_ForUnknownToken()
    {
        _sut.GenerateToken(@"C:\projects\test");
        Assert.Null(_sut.ResolveWorkspaceByToken("unknown-token-xyz"));
    }

    [Fact]
    public void ResolveWorkspaceByToken_ReturnsNull_ForNullOrEmpty()
    {
        Assert.Null(_sut.ResolveWorkspaceByToken(null));
        Assert.Null(_sut.ResolveWorkspaceByToken(""));
        Assert.Null(_sut.ResolveWorkspaceByToken("   "));
    }

    [Fact]
    public void ResolveWorkspaceByToken_DefaultToken_ReturnsCorrectWorkspace()
    {
        var token = _sut.GenerateDefaultToken(@"C:\projects\test");
        var result = _sut.ResolveWorkspaceByToken(token, out var isDefault);
        Assert.NotNull(result);
        Assert.True(isDefault);
    }

    [Fact]
    public void ResolveWorkspaceByToken_FullToken_IsNotDefault()
    {
        var token = _sut.GenerateToken(@"C:\projects\test");
        var result = _sut.ResolveWorkspaceByToken(token, out var isDefault);
        Assert.NotNull(result);
        Assert.False(isDefault);
    }

    [Fact]
    public void ResolveWorkspaceByToken_AfterTokenRotation_OldTokenReturnsNull()
    {
        var oldToken = _sut.GenerateToken(@"C:\projects\test");
        var newToken = _sut.GenerateToken(@"C:\projects\test");

        Assert.Null(_sut.ResolveWorkspaceByToken(oldToken));
        Assert.NotNull(_sut.ResolveWorkspaceByToken(newToken));
    }

    [Fact]
    public void ResolveWorkspaceByToken_MultipleWorkspaces_ReturnsCorrectOne()
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
}
