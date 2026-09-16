using System.Reflection;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-QUALITY / G1 provider diagnosis: proves <see cref="EphemeralPostgresFixture"/>
/// fail-closed initdb evidence captures sanitized invocation, stdout, and stderr.
/// </summary>
public sealed class EphemeralPostgresFixtureTests
{
    /// <summary>
    /// Overlay G1 leftover: when initdb exits non-zero, the fixture exception must retain
    /// sanitized executable path, arguments, cwd, exit code, stdout, and stderr. "initdb exited 1"
    /// alone is not sufficient. TEST-MCP-AIUNIT-002 adjacent provider diagnosis.
    /// </summary>
    [Fact]
    public void EphemeralPostgresFixture_InitdbFailure_CapturesSanitizedInvocationStdoutAndStderr()
    {
        var find = typeof(EphemeralPostgresFixture).GetMethod(
            "FindPostgresBinaries",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(find);
        var pgBin = find!.Invoke(null, null) as string;
        Assert.False(
            string.IsNullOrWhiteSpace(pgBin),
            "PostgreSQL binaries not found. Run InstallTestDependencies or install PostgreSQL locally. Do not skip.");

        var initdb = Path.Combine(pgBin!, "initdb.exe");
        Assert.True(File.Exists(initdb), $"initdb.exe missing under {pgBin}");

        var runTool = typeof(EphemeralPostgresFixture).GetMethod(
            "RunTool",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(runTool);

        var exception = Assert.Throws<TargetInvocationException>(
            () => runTool!.Invoke(null, [initdb, "--this-flag-does-not-exist", true]));
        var inner = Assert.IsType<InvalidOperationException>(exception.InnerException);
        var message = inner.Message;

        Assert.Contains("exit code", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stdout", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stderr", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("arguments", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cwd", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("initdb.exe", message, StringComparison.OrdinalIgnoreCase);
    }
}
