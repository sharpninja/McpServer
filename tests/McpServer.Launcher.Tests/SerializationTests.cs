using System.Text.Encodings.Web;
using System.Text.Json;
using McpServer.Launcher.Models;

namespace McpServer.Launcher.Tests;

/// <summary>
/// Tests for JSON serialization round-trips of <see cref="ProcessLaunchRequest"/> and <see cref="ProcessLaunchResult"/>.
/// </summary>
public sealed class SerializationTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Fact]
    public void ProcessLaunchRequest_RoundTrip_PreservesAllFields()
    {
        var request = new ProcessLaunchRequest
        {
            ExecutablePath = @"C:\Windows\notepad.exe",
            Arguments = "test.txt",
            WorkingDirectory = @"C:\Temp",
            EnvironmentVariables = new Dictionary<string, string> { ["FOO"] = "bar" },
            CreateNoWindow = true,
            WindowStyle = WindowStyleOption.Hidden,
            WaitForExit = true,
            TimeoutMs = 5000
        };

        var json = JsonSerializer.Serialize(request, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProcessLaunchRequest>(json, s_jsonOptions);

        Assert.NotNull(deserialized);
        Assert.Contains("\"windowStyle\":\"Hidden\"", json);
        Assert.Equal(request.ExecutablePath, deserialized.ExecutablePath);
        Assert.Equal(request.Arguments, deserialized.Arguments);
        Assert.Equal(request.WorkingDirectory, deserialized.WorkingDirectory);
        Assert.Equal(request.EnvironmentVariables, deserialized.EnvironmentVariables);
        Assert.Equal(request.CreateNoWindow, deserialized.CreateNoWindow);
        Assert.Equal(request.WindowStyle, deserialized.WindowStyle);
        Assert.Equal(request.WaitForExit, deserialized.WaitForExit);
        Assert.Equal(request.TimeoutMs, deserialized.TimeoutMs);
    }

    [Fact]
    public void ProcessLaunchRequest_CamelCase_PropertyNames()
    {
        var request = new ProcessLaunchRequest { ExecutablePath = "test.exe" };
        var json = JsonSerializer.Serialize(request, s_jsonOptions);

        Assert.Contains("\"executablePath\"", json);
        Assert.Contains("\"createNoWindow\"", json);
        Assert.Contains("\"windowStyle\"", json);
        Assert.Contains("\"waitForExit\"", json);
    }

    [Fact]
    public void ProcessLaunchRequest_Defaults_Correct()
    {
        var json = """{"executablePath":"test.exe"}""";
        var request = JsonSerializer.Deserialize<ProcessLaunchRequest>(json, s_jsonOptions);

        Assert.NotNull(request);
        Assert.Equal("test.exe", request.ExecutablePath);
        Assert.Null(request.Arguments);
        Assert.Null(request.WorkingDirectory);
        Assert.Null(request.EnvironmentVariables);
        Assert.False(request.CreateNoWindow);
        Assert.Equal(WindowStyleOption.Normal, request.WindowStyle);
        Assert.False(request.WaitForExit);
        Assert.Null(request.TimeoutMs);
    }

    [Fact]
    public void ProcessLaunchResult_RoundTrip_PreservesAllFields()
    {
        var result = new ProcessLaunchResult
        {
            Success = true,
            ProcessId = 12345,
            ExitCode = 0,
            ErrorMessage = null,
            ErrorCode = null
        };

        var json = JsonSerializer.Serialize(result, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProcessLaunchResult>(json, s_jsonOptions);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.Equal(12345, deserialized.ProcessId);
        Assert.Equal(0, deserialized.ExitCode);
        Assert.Null(deserialized.ErrorMessage);
        Assert.Null(deserialized.ErrorCode);
    }

    [Fact]
    public void ProcessLaunchResult_FailureCase_PreservesErrorInfo()
    {
        var result = new ProcessLaunchResult
        {
            Success = false,
            ErrorMessage = "Access denied",
            ErrorCode = 5
        };

        var json = JsonSerializer.Serialize(result, s_jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ProcessLaunchResult>(json, s_jsonOptions);

        Assert.NotNull(deserialized);
        Assert.False(deserialized.Success);
        Assert.Null(deserialized.ProcessId);
        Assert.Equal("Access denied", deserialized.ErrorMessage);
        Assert.Equal(5, deserialized.ErrorCode);
    }
}
