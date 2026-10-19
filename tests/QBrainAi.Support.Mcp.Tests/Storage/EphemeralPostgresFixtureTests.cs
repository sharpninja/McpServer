using System.Reflection;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-QUALITY / G1 provider diagnosis: proves <see cref="EphemeralPostgresFixture"/>
/// fail-closed initdb evidence captures sanitized invocation, stdout, and stderr.
/// </summary>
public sealed class EphemeralPostgresFixtureTests
{
    /// <summary>
    /// Overlay G1 leftover: when initdb exits non-zero, the fixture exception must retain
    /// sanitized executable path, arguments, cwd, exit code, stdout, and stderr. "initdb exited 1"
    /// alone is not sufficient. TEST-MCP-AIUNIT-002 adjacent provider diagnosis. When
    /// MCP_TEST_POSTGRES_CONNECTION names a server (the container endpoint; operator decision
    /// 2026-10-11 for BUG-BASELINE-005), the fixture provisions nothing locally and initdb never
    /// runs, so the test proves that adoption instead: the fixture uses that server and holds no
    /// local binaries or data directory. The comparison never prints the connection string.
    /// </summary>
    [Fact]
    public void EphemeralPostgresFixture_InitdbFailure_CapturesSanitizedInvocationStdoutAndStderr()
    {
        var external = Environment.GetEnvironmentVariable("MCP_TEST_POSTGRES_CONNECTION");
        if (!string.IsNullOrWhiteSpace(external))
        {
            using var fixture = new EphemeralPostgresFixture();
            Assert.True(
                string.Equals(external, fixture.ServerConnectionString, StringComparison.Ordinal),
                "The fixture did not adopt MCP_TEST_POSTGRES_CONNECTION.");
            Assert.Null(GetPrivateField(fixture, "_pgBin"));
            Assert.Null(GetPrivateField(fixture, "_dataDir"));
            return;
        }

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

    private static object? GetPrivateField(EphemeralPostgresFixture fixture, string name)
    {
        var field = typeof(EphemeralPostgresFixture).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return field!.GetValue(fixture);
    }
}
