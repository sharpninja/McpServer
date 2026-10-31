using System.Reflection;
using QBrainAi.Support.Mcp.Tests.TestSupport;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-QUALITY / G1 provider diagnosis: proves <see cref="EphemeralPostgresFixture"/>
/// fail-closed initdb evidence captures sanitized invocation, stdout, and stderr.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class EphemeralPostgresFixtureTests
{
    /// <summary>
    /// FR-MCP-TESTRESOURCE-001: explicitly supplied external-server data is adopted without provisioning.
    /// Integration because the case owns and restores MCP_TEST_POSTGRES_CONNECTION; no server is contacted.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public void EphemeralPostgresFixture_ExplicitOverride_IsAdoptedWithoutLocalProvisioning()
    {
        var previous = Environment.GetEnvironmentVariable("MCP_TEST_POSTGRES_CONNECTION");
        const string external = "Host=database.example;Port=55432;Username=fixture;Password=fixture-data;Database=postgres";
        try
        {
            Environment.SetEnvironmentVariable("MCP_TEST_POSTGRES_CONNECTION", external);
            using var fixture = new EphemeralPostgresFixture();
            Assert.True(string.Equals(external, fixture.ServerConnectionString, StringComparison.Ordinal),
                "The fixture did not adopt its explicitly owned external setting.");
            Assert.Null(GetPrivateField(fixture, "_pgBin"));
            Assert.Null(GetPrivateField(fixture, "_dataDir"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MCP_TEST_POSTGRES_CONNECTION", previous);
        }
    }

    /// <summary>
    /// Overlay G1 leftover: when initdb exits non-zero, the fixture exception must retain
    /// sanitized executable path, arguments, cwd, exit code, stdout, and stderr. "initdb exited 1"
    /// alone is not sufficient. TEST-MCP-AIUNIT-002 adjacent provider diagnosis. When
    /// the host has local PostgreSQL tools, this directly invokes its Windows initdb executable. Integration,
    /// Platform=Windows because it proves native executable diagnostics; adoption is a separate owned-variable case.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Platform", "Windows")]
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

    private static object? GetPrivateField(EphemeralPostgresFixture fixture, string name)
    {
        var field = typeof(EphemeralPostgresFixture).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return field!.GetValue(fixture);
    }
}
