using System.Reflection;
using McpServer.Support.Mcp.Tests.Infrastructure;

namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>
/// TEST-MCP-USECASE-018: Fourteenth-review external-artifact repository-root regressions.
/// </summary>
[Collection(RepositoryRootProcessStateCollection.Name)]
public sealed class BugTriage139FourteenthHarnessTests
{
    /// <summary>
    /// A consumer launched from its true external output directory resolves the checkout without an
    /// environment override.
    /// </summary>
    [Fact]
    public void RepositoryRoot_ExternalBinaryDirectoryWithoutOverride_ResolvesCheckout()
    {
        var previousRoot = Environment.GetEnvironmentVariable("MCP_REPOSITORY_ROOT");
        var previousDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.SetEnvironmentVariable("MCP_REPOSITORY_ROOT", null);
            Environment.CurrentDirectory = AppContext.BaseDirectory;

            var actual = RepositoryEvidenceTestSupport.ResolveRepositoryRoot();

            Assert.Equal(SourceRepositoryRoot(), actual);
        }
        finally
        {
            Environment.CurrentDirectory = previousDirectory;
            Environment.SetEnvironmentVariable("MCP_REPOSITORY_ROOT", previousRoot);
        }
    }

    /// <summary>
    /// The built test assembly contains exactly the native cases for its current platform and no
    /// unsupported platform cases, so discovery cannot turn an unsupported source branch into a pass.
    /// </summary>
    [Fact]
    public void PlatformNativeTests_AreCompiledAndDiscoverableOnlyForCurrentPlatform()
    {
        Assert.True(
            OperatingSystem.IsWindows() || OperatingSystem.IsLinux(),
            "The native BUG-TRIAGE-139 inventory supports Windows and Linux only.");

        var windowsInventory = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["McpServer.Support.Mcp.Tests.Storage.BugTriage139FifteenthWindowsNativeTests"] = 3,
            ["McpServer.Support.Mcp.Tests.Storage.BugTriage139SixteenthIdentityWindowsTests"] = 3,
            ["McpServer.Support.Mcp.Tests.Documentation.BugTriage139SixteenthWindowsProcessTests"] = 5,
        };
        var linuxInventory = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["McpServer.Support.Mcp.Tests.Storage.BugTriage139FifteenthLinuxNativeTests"] = 3,
            ["McpServer.Support.Mcp.Tests.Storage.BugTriage139SixteenthLinuxTests"] = 7,
            ["McpServer.Support.Mcp.Tests.Storage.BugTriage139EighteenthLinuxTests"] = 15,
            ["McpServer.Support.Mcp.Tests.Services.RequirementsFourteenthLinuxNativeTests"] = 1,
        };
        var applicableInventory = OperatingSystem.IsWindows()
            ? windowsInventory
            : linuxInventory;
        var unsupportedInventory = OperatingSystem.IsWindows()
            ? linuxInventory
            : windowsInventory;
        var assembly = typeof(BugTriage139FourteenthHarnessTests).Assembly;

        foreach (var expected in applicableInventory)
        {
            var type = assembly.GetType(expected.Key, throwOnError: false);
            Assert.NotNull(type);
            var discoveredFacts = type!.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Count(method => method.GetCustomAttributesData().Any(
                    attribute => string.Equals(
                        attribute.AttributeType.FullName,
                        "Xunit.FactAttribute",
                        StringComparison.Ordinal)));
            Assert.Equal(expected.Value, discoveredFacts);
        }

        Assert.All(
            unsupportedInventory.Keys,
            typeName => Assert.Null(assembly.GetType(typeName, throwOnError: false)));
        Assert.Equal(
            OperatingSystem.IsWindows() ? 11 : 26,
            applicableInventory.Values.Sum());
    }

    /// <summary>
    /// A descendant that inherits redirected pipes cannot outlive the bounded process operation
    /// after its direct parent exits.
    /// </summary>
    [Fact]
    public void ProcessRunner_ExitedParentWithPipeHoldingDescendant_KillsTreeWithinBound()
    {
        string executable;
        IReadOnlyList<string> arguments;
        if (OperatingSystem.IsWindows())
        {
            executable = "pwsh.exe";
            var parentScript = string.Join(
                "; ",
                "$start = [Diagnostics.ProcessStartInfo]::new()",
                "$start.FileName = 'pwsh.exe'",
                "$start.UseShellExecute = $false",
                "$start.ArgumentList.Add('-NoProfile')",
                "$start.ArgumentList.Add('-NonInteractive')",
                "$start.ArgumentList.Add('-Command')",
                "$start.ArgumentList.Add('[Console]::Out.WriteLine(''descendant-ready''); " +
                "Start-Sleep -Seconds 30')",
                "$child = [Diagnostics.Process]::Start($start)",
                "$child.Dispose()");
            arguments =
            [
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                parentScript,
            ];
        }
        else
        {
            executable = "/bin/sh";
            arguments =
            [
                "-c",
                "/bin/sh -c 'printf \"descendant-ready\\n\"; sleep 30' &",
            ];
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exception = Record.Exception(
            () => RepositoryEvidenceTestSupport.RunProcess(
                SourceRepositoryRoot(),
                executable,
                arguments,
                standardInput: null,
                TimeSpan.FromSeconds(2)));

        Assert.IsType<TimeoutException>(exception);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(8),
            $"Descendant-held pipe cleanup took {stopwatch.Elapsed}.");
    }

    /// <summary>
    /// Short-lived real processes may exit between <see cref="System.Diagnostics.Process.Start()"/>
    /// and Windows job assignment without turning a successful command into a harness failure.
    /// </summary>
    [Fact]
    public void ProcessRunner_RepeatedQuickProcessesDoNotRaceJobAttachment()
    {
        for (var attempt = 0; attempt < 128; attempt++)
        {
            var executable = OperatingSystem.IsWindows()
                ? "git"
                : "/usr/bin/true";
            IReadOnlyList<string> arguments = OperatingSystem.IsWindows()
                ? ["rev-parse", "--is-inside-work-tree"]
                : [];
            var result = RepositoryEvidenceTestSupport.RunProcess(
                SourceRepositoryRoot(),
                executable,
                arguments,
                standardInput: null,
                TimeSpan.FromSeconds(2));

            Assert.Equal(0, result.ExitCode);
            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(
                    "true",
                    System.Text.Encoding.UTF8.GetString(result.StandardOutput).Trim());
            }
            else
            {
                Assert.Empty(result.StandardOutput);
            }
        }
    }

    private static string SourceRepositoryRoot()
    {
        var metadata = typeof(BugTriage139FourteenthHarnessTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(
                attribute => string.Equals(
                    attribute.Key,
                    "McpRepositoryRoot",
                    StringComparison.Ordinal));
        return Path.GetFullPath(
            metadata.Value ??
            throw new InvalidOperationException("Build repository-root metadata is empty."));
    }
}
