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
}
