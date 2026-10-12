using Xunit;

namespace QBrainAi.Cqrs.Tests;

/// <summary>Tests for <see cref="Result{T}"/> and <see cref="Result"/>.</summary>
public class ResultTests
{
    [Fact]
    public void SuccessHasValueAndIsSuccess()
    {
        var result = Result<int>.Success(42);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void FailureWithMessageHasError()
    {
        var result = Result<int>.Failure("bad input");
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal("bad input", result.Error);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void FailureWithExceptionHasBoth()
    {
        var ex = new InvalidOperationException("boom");
        var result = Result<int>.Failure(ex);
        Assert.True(result.IsFailure);
        Assert.Equal("boom", result.Error);
        Assert.Same(ex, result.Exception);
    }

    [Fact]
    public void FailureWithMessageAndException()
    {
        var ex = new InvalidOperationException("inner");
        var result = Result<int>.Failure("outer", ex);
        Assert.Equal("outer", result.Error);
        Assert.Same(ex, result.Exception);
    }

    [Fact]
    public void BindSuccessChains()
    {
        var result = Result<int>.Success(10)
            .Bind(v => Result<string>.Success($"value={v}"));
        Assert.True(result.IsSuccess);
        Assert.Equal("value=10", result.Value);
    }

    [Fact]
    public void BindFailurePropagates()
    {
        var result = Result<int>.Failure("err")
            .Bind(v => Result<string>.Success($"value={v}"));
        Assert.True(result.IsFailure);
        Assert.Equal("err", result.Error);
    }

    [Fact]
    public void MapSuccessTransforms()
    {
        var result = Result<int>.Success(5).Map(v => v * 2);
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void MapFailurePropagates()
    {
        var result = Result<int>.Failure("err").Map(v => v * 2);
        Assert.True(result.IsFailure);
        Assert.Equal("err", result.Error);
    }

    [Fact]
    public void GetValueOrDefaultSuccessReturnsValue()
    {
        var result = Result<int>.Success(42);
        Assert.Equal(42, result.GetValueOrDefault(0));
    }

    [Fact]
    public void GetValueOrDefaultFailureReturnsFallback()
    {
        var result = Result<int>.Failure("err");
        Assert.Equal(-1, result.GetValueOrDefault(-1));
    }

    [Fact]
    public void ToStringSuccessFormat()
    {
        var result = Result<int>.Success(42);
        var text = result.ToString();
        Assert.StartsWith("Success", text, StringComparison.Ordinal);
        Assert.Contains("42", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ToStringFailureFormat()
    {
        var result = Result<int>.Failure("oops");
        var text = result.ToString();
        Assert.StartsWith("Failure", text, StringComparison.Ordinal);
        Assert.Contains("oops", text, StringComparison.Ordinal);
    }

    // Non-generic Result tests
    [Fact]
    public void NonGenericSuccess()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("Success", result.ToString());
    }

    [Fact]
    public void NonGenericFailure()
    {
        var result = Result.Failure("err");
        Assert.True(result.IsFailure);
        Assert.Equal("err", result.Error);
    }
}
