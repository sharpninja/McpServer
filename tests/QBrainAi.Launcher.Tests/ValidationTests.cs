using QBrainAi.Launcher.Models;
using QBrainAi.Launcher.Validation;

namespace QBrainAi.Launcher.Tests;

/// <summary>
/// Tests for <see cref="RequestValidator"/>.
/// </summary>
public sealed class ValidationTests
{
    [Fact]
    public void ValidateValidRequestReturnsNoErrors()
    {
        var request = new ProcessLaunchRequest
        {
            ExecutablePath = "notepad.exe"
        };

        var errors = RequestValidator.Validate(request);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateMissingExecutablePathReturnsError(string? path)
    {
        var request = new ProcessLaunchRequest
        {
            ExecutablePath = path ?? string.Empty
        };

        var errors = RequestValidator.Validate(request);
        Assert.Contains(errors, e => e.Contains("ExecutablePath"));
    }

    [Fact]
    public void ValidateInvalidWorkingDirectoryReturnsError()
    {
        var request = new ProcessLaunchRequest
        {
            ExecutablePath = "notepad.exe",
            WorkingDirectory = @"C:\NonExistent_Path_12345"
        };

        var errors = RequestValidator.Validate(request);
        Assert.Contains(errors, e => e.Contains("WorkingDirectory"));
    }

    [Fact]
    public void ValidateNegativeTimeoutReturnsError()
    {
        var request = new ProcessLaunchRequest
        {
            ExecutablePath = "notepad.exe",
            WaitForExit = true,
            TimeoutMs = -1
        };

        var errors = RequestValidator.Validate(request);
        Assert.Contains(errors, e => e.Contains("TimeoutMs") && e.Contains("positive"));
    }

    [Fact]
    public void ValidateTimeoutWithoutWaitForExitReturnsError()
    {
        var request = new ProcessLaunchRequest
        {
            ExecutablePath = "notepad.exe",
            WaitForExit = false,
            TimeoutMs = 5000
        };

        var errors = RequestValidator.Validate(request);
        Assert.Contains(errors, e => e.Contains("WaitForExit"));
    }

    [Fact]
    public void ValidateValidTimeoutReturnsNoErrors()
    {
        var request = new ProcessLaunchRequest
        {
            ExecutablePath = "notepad.exe",
            WaitForExit = true,
            TimeoutMs = 5000
        };

        var errors = RequestValidator.Validate(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateInvalidWindowStyleReturnsError()
    {
        var request = new ProcessLaunchRequest
        {
            ExecutablePath = "notepad.exe",
            WindowStyle = (WindowStyleOption)99
        };

        var errors = RequestValidator.Validate(request);
        Assert.Contains(errors, e => e.Contains("WindowStyle"));
    }
}
