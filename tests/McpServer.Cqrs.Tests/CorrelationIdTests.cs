using Xunit;

namespace McpServer.Cqrs.Tests;

/// <summary>Tests for <see cref="CorrelationId"/>.</summary>
public class CorrelationIdTests
{
    [Fact]
    public void New_HasEightDigitBaseId()
    {
        var cid = new CorrelationId();
        Assert.InRange(cid.BaseId, 10000000, 99999999);
        Assert.Equal(0, cid.Counter);
    }

    [Fact]
    public void Current_Format()
    {
        var cid = new CorrelationId(12345678, 0);
        Assert.Equal("12345678.0", cid.Current);
        Assert.Equal("12345678.0", cid.ToString());
    }

    [Fact]
    public void Next_Increments()
    {
        var cid = new CorrelationId(12345678, 0);
        Assert.Equal("12345678.1", cid.Next());
        Assert.Equal("12345678.2", cid.Next());
        Assert.Equal(2, cid.Counter);
    }

    [Fact]
    public void Parse_Valid()
    {
        var cid = CorrelationId.Parse("48291735.3");
        Assert.Equal(48291735, cid.BaseId);
        Assert.Equal(3, cid.Counter);
    }

    [Fact]
    public void Parse_Invalid_Throws()
    {
        Assert.Throws<FormatException>(() => CorrelationId.Parse("invalid"));
        Assert.Throws<FormatException>(() => CorrelationId.Parse(".5"));
        Assert.Throws<FormatException>(() => CorrelationId.Parse("123."));
    }

    [Fact]
    public void TryParse_Null_ReturnsNull()
    {
        Assert.Null(CorrelationId.TryParse(null));
        Assert.Null(CorrelationId.TryParse(""));
        Assert.Null(CorrelationId.TryParse("  "));
    }

    [Fact]
    public void TryParse_Invalid_ReturnsNull()
    {
        Assert.Null(CorrelationId.TryParse("not-valid"));
    }

    [Fact]
    public void TryParse_Valid_ReturnsInstance()
    {
        var cid = CorrelationId.TryParse("11111111.7");
        Assert.NotNull(cid);
        Assert.Equal(11111111, cid.BaseId);
        Assert.Equal(7, cid.Counter);
    }

    [Fact]
    public async Task Next_IsThreadSafe()
    {
        var cid = new CorrelationId(10000000, 0);
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() => cid.Next())).ToArray();
        await Task.WhenAll(tasks);
        Assert.Equal(100, cid.Counter);
    }
}
