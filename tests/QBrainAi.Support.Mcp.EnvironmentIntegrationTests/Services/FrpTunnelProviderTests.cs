using System.Diagnostics;
using System.Reflection;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>Unit tests for FRP tunnel provider helper logic and failure paths.</summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/FrpTunnelProviderTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases and the helpers only they use (SetPrivateField, GetPrivateField and StartExitedDotnetProcess) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class FrpTunnelProviderTestsBase in Services/FrpTunnelProviderTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>

public sealed class FrpTunnelProviderTests : FrpTunnelProviderTestsBase
{
    /// <summary>Verifies GetStatusAsync_WhenTrackedProcessExited_ReturnsExitError using its native fixture.</summary>
    /// <remarks>Integration: starts and observes a native child process (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task GetStatusAsync_WhenTrackedProcessExited_ReturnsExitError()
    {
        var sut = CreateSut();
        using var exitedProcess = StartExitedDotnetProcess();

        SetPrivateField(sut, "_process", exitedProcess);
        SetPrivateField(sut, "_lastStderrLine", "simulated frpc startup failure");

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("exit code", status.Error, StringComparison.Ordinal);
        Assert.Contains("simulated frpc startup failure", status.Error, StringComparison.Ordinal);
    }

    /// <summary>Verifies Dispose_WhenTempConfigExists_DeletesFileAndClearsConfigPath using its native fixture.</summary>
    /// <remarks>Integration: creates and deletes a physical temporary config file (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Dispose_WhenTempConfigExists_DeletesFileAndClearsConfigPath()
    {
        var sut = CreateSut();
        var tempConfigPath = Path.Combine(Path.GetTempPath(), $"frpc-test-{Guid.NewGuid():N}.toml");
        File.WriteAllText(tempConfigPath, "test");
        Assert.True(File.Exists(tempConfigPath));

        SetPrivateField(sut, "_configPath", tempConfigPath);

        sut.Dispose();

        Assert.False(File.Exists(tempConfigPath));
        Assert.Null(GetPrivateField<string?>(sut, "_configPath"));
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {target.GetType().FullName}.");
        field.SetValue(target, value);
    }

    private static T? GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {target.GetType().FullName}.");
        return (T?)field.GetValue(target);
    }

    private static Process StartExitedDotnetProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "__mcpserver_unknown_test_command__",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start test process for exited-process status path.");

        Assert.True(process.WaitForExit(10_000), "Timed out waiting for test process to exit.");
        return process;
    }
}
