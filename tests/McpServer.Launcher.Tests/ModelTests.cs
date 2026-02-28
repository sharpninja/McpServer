using McpServer.Launcher.Models;

namespace McpServer.Launcher.Tests;

/// <summary>
/// Tests for model types.
/// </summary>
public sealed class ModelTests
{
    [Theory]
    [InlineData(WindowStyleOption.Normal, 0)]
    [InlineData(WindowStyleOption.Hidden, 1)]
    [InlineData(WindowStyleOption.Minimized, 2)]
    [InlineData(WindowStyleOption.Maximized, 3)]
    public void WindowStyleOption_HasExpectedValues(WindowStyleOption style, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)style);
    }

    [Fact]
    public void ProcessLaunchResult_Defaults_SuccessIsFalse()
    {
        var result = new ProcessLaunchResult();
        Assert.False(result.Success);
        Assert.Null(result.ProcessId);
        Assert.Null(result.ExitCode);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void ProcessLaunchRequest_Defaults_AreCorrect()
    {
        var request = new ProcessLaunchRequest();
        Assert.Equal(string.Empty, request.ExecutablePath);
        Assert.Null(request.Arguments);
        Assert.Null(request.WorkingDirectory);
        Assert.Null(request.EnvironmentVariables);
        Assert.False(request.CreateNoWindow);
        Assert.Equal(WindowStyleOption.Normal, request.WindowStyle);
        Assert.False(request.WaitForExit);
        Assert.Null(request.TimeoutMs);
    }
}
