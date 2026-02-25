using Xunit;

namespace McpServer.Cqrs.Tests;

/// <summary>Tests for <see cref="Result{T}"/> and <see cref="Result"/>.</summary>
public class ResultTests
{
    [Fact]
    public void Success_HasValue_And_IsSuccess()
    {
        var result = Result<int>.Success(42);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Failure_WithMessage_HasError()
    {
        var result = Result<int>.Failure("bad input");
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal("bad input", result.Error);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Failure_WithException_HasBoth()
    {
        var ex = new InvalidOperationException("boom");
        var result = Result<int>.Failure(ex);
        Assert.True(result.IsFailure);
        Assert.Equal("boom", result.Error);
        Assert.Same(ex, result.Exception);
    }

    [Fact]
    public void Failure_WithMessageAndException()
    {
        var ex = new InvalidOperationException("inner");
        var result = Result<int>.Failure("outer", ex);
        Assert.Equal("outer", result.Error);
        Assert.Same(ex, result.Exception);
    }

    [Fact]
    public void Bind_Success_Chains()
    {
        var result = Result<int>.Success(10)
            .Bind(v => Result<string>.Success($"value={v}"));
        Assert.True(result.IsSuccess);
        Assert.Equal("value=10", result.Value);
    }

    [Fact]
    public void Bind_Failure_Propagates()
    {
        var result = Result<int>.Failure("err")
            .Bind(v => Result<string>.Success($"value={v}"));
        Assert.True(result.IsFailure);
        Assert.Equal("err", result.Error);
    }

    [Fact]
    public void Map_Success_Transforms()
    {
        var result = Result<int>.Success(5).Map(v => v * 2);
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Map_Failure_Propagates()
    {
        var result = Result<int>.Failure("err").Map(v => v * 2);
        Assert.True(result.IsFailure);
        Assert.Equal("err", result.Error);
    }

    [Fact]
    public void GetValueOrDefault_Success_ReturnsValue()
    {
        var result = Result<int>.Success(42);
        Assert.Equal(42, result.GetValueOrDefault(0));
    }

    [Fact]
    public void GetValueOrDefault_Failure_ReturnsFallback()
    {
        var result = Result<int>.Failure("err");
        Assert.Equal(-1, result.GetValueOrDefault(-1));
    }

    [Fact]
    public void ToString_Success_Format()
    {
        var result = Result<int>.Success(42);
        Assert.Equal("Success(42)", result.ToString());
    }

    [Fact]
    public void ToString_Failure_Format()
    {
        var result = Result<int>.Failure("oops");
        Assert.Equal("Failure(oops)", result.ToString());
    }

    // Non-generic Result tests
    [Fact]
    public void NonGeneric_Success()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("Success", result.ToString());
    }

    [Fact]
    public void NonGeneric_Failure()
    {
        var result = Result.Failure("err");
        Assert.True(result.IsFailure);
        Assert.Equal("err", result.Error);
    }
}
